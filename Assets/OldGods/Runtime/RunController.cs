using System;
using System.Collections.Generic;
using System.Linq;
using OldGods.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OldGods.Runtime
{
    /// <summary>
    /// Hosts one run: builds each stage's world from the run seed, owns the player and
    /// horde, starts bosses, moves between stages and ends the run. The only component a
    /// run scene needs.
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }

        public GameAssets Assets;
        [Tooltip("Hex seed for repeatable runs; empty picks a new one.")]
        public string FixedSeed = "";
        public int StageCount = 3;

        public RunSeed Seed { get; private set; }
        public PlayerMotor Player { get; private set; }
        public PlayerHealth PlayerHealth { get; private set; }
        public ChaseCamera Camera { get; private set; }
        public HordeManager Horde { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public Pickups Pickups { get; private set; }
        public StageDirector Director { get; private set; }
        public ContentSet Content { get; private set; }
        public Minimap Minimap { get; private set; }
        public Hud Hud { get; private set; }
        public RunEconomy Economy { get; private set; }
        public Transform WorldRoot => worldRoot;

        public int StageIndex { get; private set; }
        public BiomeDefinition Biome { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool IsOver { get; private set; }
        public bool Won { get; private set; }
        public bool BossActive => BossController.Active != null;
        public bool BossDefeated { get; private set; }
        public bool IsLastStage => StageIndex >= StageCount - 1;
        /// <summary>True in The Last Test's arena, after the last stage.</summary>
        public bool IsFinal { get; private set; }
        public string GodId { get; private set; }
        public GodDef God { get; private set; }
        public RunSummary Summary { get; } = new RunSummary();
        public int EmbersEarned { get; private set; }
        public List<QuestDef> QuestsCompleted { get; private set; } = new List<QuestDef>();
        /// <summary>Boss Curse shrines taken this stage: each makes the boss stronger.</summary>
        public int BossCurses { get; set; }

        public event Action RunOver;
        public event Action<int> StageStarted;
        public event Action<string, string> Announced;

        Transform worldRoot;
        Light sun;

        void Awake()
        {
            Instance = this;
            if (Assets == null) Assets = GameAssets.Load();
            Time.timeScale = 1f;
            GameInput.Ensure();
            Fx.Init(Assets);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Ground.Clear();
        }

        void Start()
        {
            string seedText = CommandLine.Value("-seed") ?? FixedSeed;
            Seed = RunSeed.TryParse(seedText, out var s) ? s : RunSeed.FromEntropy(DateTime.UtcNow.Ticks, Environment.TickCount);
            Debug.Log($"OldGods: run seed {Seed}");
            Content = Assets.Content.Load();
            var save = SaveStore.Current;
            var god = RunSetup.GodId != null ? Content.God(RunSetup.GodId) : null;
            if (god == null || !GodRules.IsUnlocked(god, save.IsUnlocked)) god = GodRules.Default(Content.Gods, save.IsUnlocked);
            God = god;
            GodId = god != null ? god.Id : RunSetup.GodId;
            Summary.Seed = Seed.ToString();
            Summary.GodId = GodId ?? "";
            if (Application.CanStreamedLevelBeLoaded(MenuController.MenuScene))
                ResultsScreen.ToMenu = () => SceneManager.LoadScene(MenuController.MenuScene);
            BuildPersistent();
            BuildStage(0);
        }

        /// <summary>Things that live for the whole run: player, camera, horde, combat, UI.</summary>
        void BuildPersistent()
        {
            var godAsset = God != null ? Assets.Content.GodAsset(God.Id) : null;
            Player = godAsset != null
                ? WorldBuilder.CreatePlayer(Assets, Vector3.up * 50f, godAsset.Robe, null, PlaceholderMeshes.God(godAsset.Look), godAsset.Mark)
                : WorldBuilder.CreatePlayer(Assets, Vector3.up * 50f, new Color(0.85f, 0.82f, 0.7f), null);
            PlayerHealth = Player.GetComponent<PlayerHealth>();
            PlayerHealth.Died += OnPlayerDied;
            Camera = WorldBuilder.CreateCamera(Player, null);
            sun = WorldBuilder.CreateSun(null, Color.white, 1.3f, new Vector3(50f, -35f, 0f));

            Horde = WorldBuilder.CreateHorde(Assets, Player, null);
            Horde.SpawnRng = Seed.Stream(RunSeed.Spawns);
            foreach (var e in Assets.Content.Enemies)
                if (e != null) Horde.RegisterType(Content.Enemy(e.Id), e.MeshOrPlaceholder, e.Color, e.Emission, e.WalkSwing);

            var systems = new GameObject("Combat Systems");
            Pickups = systems.AddComponent<Pickups>();
            Pickups.Player = Player.transform;
            systems.AddComponent<Projectiles>();
            systems.AddComponent<Effects>();
            Director = systems.AddComponent<StageDirector>();
            Director.Announce += Announce;
            Economy = systems.AddComponent<RunEconomy>();
            Horde.EnemyKilled += OnEnemyKilled;
            Pickups.Collected += OnPickup;

            Combat = Player.gameObject.AddComponent<PlayerCombat>();
            Combat.Init(Assets.Content, Content, Seed);
            if (God != null)
            {
                Combat.ExternalMods.AddRange(God.Kit);
                if (!string.IsNullOrEmpty(God.StartingWeapon)) Combat.GiveWeapon(God.StartingWeapon);
                if (!string.IsNullOrEmpty(God.StartingPassive)) Combat.GivePassive(God.StartingPassive);
            }
            else if (!string.IsNullOrEmpty(Assets.StartingWeapon)) Combat.GiveWeapon(Assets.StartingWeapon);
            ApplyMeta();
            Economy.Init(this);
            var interaction = Player.gameObject.AddComponent<InteractionDriver>();
            interaction.Player = Combat;

            LevelUpScreen.Create(Combat, Seed.Stream(RunSeed.Draft));
            new GameObject("Damage Numbers").AddComponent<DamageNumbers>();
            Hud = Hud.Create(this);
            Minimap = Minimap.Create(Hud.Root);
            PauseMenu.Create(this);
            SettingsPanel.Apply(SaveStore.Current.settings);
            GameInput.SetCursorLocked(true);
        }

        /// <summary>Permanent powerups and the chosen difficulty modifiers.</summary>
        void ApplyMeta()
        {
            var save = SaveStore.Current;
            save.runsStarted++;
            Combat.ExternalMods.AddRange(MetaRules.PowerupMods(save));
            foreach (var id in RunSetup.Modifiers)
            {
                var m = MetaCatalog.Modifier(id);
                if (m == null) continue;
                Director.RunDifficulty += m.EnemyHealthAndDensity;
                Horde.GlobalSpeed *= 1f + m.EnemySpeed;
                if (m.PlayerMaxHealthPercent != 0f) Combat.ExternalMods.Add(new StatMod(StatId.MaxHealth, 0f, m.PlayerMaxHealthPercent));
                if (m.NoDraftCharges) { Combat.Charges.Refresh = 0; Combat.Charges.Skip = 0; Combat.Charges.Banish = 0; }
            }
            Summary.DifficultyBonus = MetaRules.PayoutBonus(RunSetup.Modifiers);
            Combat.RecomputeStats();
            PlayerHealth.Health.Reset();
        }

        public BiomeDefinition BiomeFor(int stage)
        {
            if (Assets.Stages != null && Assets.Stages.Count > 0)
                return Assets.Stages[Mathf.Clamp(stage, 0, Assets.Stages.Count - 1)];
            return Assets.GreyboxBiome;
        }

        /// <summary>Builds stage n's world and puts the player at its start.</summary>
        public void BuildStage(int stage, bool final = false)
        {
            StageIndex = stage;
            IsFinal = final;
            Biome = final && Assets.FinalArena != null ? Assets.FinalArena : BiomeFor(stage);
            BossDefeated = false;
            BossCurses = 0;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            foreach (var b in BossController.All.ToArray()) Destroy(b.gameObject);
            BossController.All.Clear();
            worldRoot = new GameObject($"World {stage + 1}").transform;

            Horde.Clear();
            Pickups.Clear();
            Projectiles.Instance.Clear();

            var profile = Biome.Terrain;
            var field = TerrainGenerator.Generate(profile, Seed.Stream(RunSeed.Map, stage));
            Ground.Set(field, profile.RimWidth);
            TerrainMesh.Build(field, WorldBuilder.Tinted(Assets.LowPoly, Color.white), Biome.Palette, profile.HillHeight, worldRoot);
            if (!final) ScatterProps(field, Seed.Stream(RunSeed.Map, 100 + stage));
            if (profile.WaterLevel > -100f) BuildWater(field, profile.WaterLevel);

            Audio.Music(Biome.Id);
            sun.color = Biome.Sun;
            sun.intensity = Biome.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(Biome.SunEuler);
            WorldBuilder.SetAtmosphere(Biome.AmbientSky, Biome.AmbientEquator, Biome.AmbientGround, Biome.Fog, Biome.FogStart, Biome.FogEnd);

            if (final) FinalArena.Build(this, worldRoot);
            else PlaceFeatures(field, Seed.Stream(RunSeed.Map, 200 + stage));

            Player.Teleport(Ground.Snap(Vector3.zero) + Vector3.up * 0.3f);
            Camera.SnapBehind();
            Director.Begin(Biome.Timeline, stage, Horde, Seed.Stream(RunSeed.Spawns, stage));
            Minimap.SetGround(field, Biome.Palette, profile.HillHeight);
            StageStarted?.Invoke(stage);
            if (final)
            {
                Announce(Biome.DisplayName, "The throne waits");
                StartCoroutine(WakeTheLastTest());
            }
            else
            {
                string line = Assets.Story != null ? Assets.Story.BiomeLine(Biome.Id) : null;
                if (stage == 0 && God != null && Assets.Story != null)
                {
                    Announce(God.Name, Assets.Story.GodLine(God.Id));
                    StartCoroutine(AnnounceLater(4.5f, Biome.DisplayName, line ?? $"Stage {stage + 1} of {StageCount}"));
                }
                else Announce(Biome.DisplayName, line ?? $"Stage {stage + 1} of {StageCount}");
            }
        }

        System.Collections.IEnumerator AnnounceLater(float seconds, string head, string sub)
        {
            yield return new WaitForSeconds(seconds);
            if (!IsOver) Announce(head, sub);
        }

        System.Collections.IEnumerator WakeTheLastTest()
        {
            yield return new WaitForSeconds(4f);
            var arena = FinalArena.Instance;
            if (arena != null && !IsOver) StartBoss(Ground.Snap(arena.Throne.position + arena.Throne.forward * 16f));
        }

        void BuildWater(HeightField field, float level)
        {
            var go = new GameObject("Water");
            go.transform.SetParent(worldRoot, false);
            go.transform.position = new Vector3(0f, level, 0f);
            go.transform.localScale = new Vector3(field.Size * 0.5f, 1f, field.Size * 0.5f);
            go.AddComponent<MeshFilter>().sharedMesh = Fx.Disc(48);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Fx.Fade(Biome.WaterColor);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Feature kinds for this milestone; later milestones extend the list.</summary>
        public static readonly List<FeatureRequest> FeatureRequests = new List<FeatureRequest>
        {
            new FeatureRequest(FeatureKind.BossGate, 1, 0f),
            new FeatureRequest(FeatureKind.Merchant, 1, 0f),
            new FeatureRequest(FeatureKind.Duplicator, 1, 0f),
            new FeatureRequest(FeatureKind.Shrine, 8, 24f),
            new FeatureRequest(FeatureKind.Chest, 10, 22f),
            new FeatureRequest(FeatureKind.LorePickup, 1, 0f),
        };

        /// <summary>Hook for later milestones to build chests, shrines and the merchant.</summary>
        public static event Action<RunController, Placement, Transform> FeaturePlaced;

        void PlaceFeatures(HeightField field, Rng rng)
        {
            var placed = MapPlacement.Place(field, Biome.Terrain.RimWidth, new PlacementRules(), FeatureRequests, rng);
            foreach (var p in placed)
            {
                var at = new Vector3(p.X, 0f, p.Z);
                if (p.Kind == FeatureKind.BossGate)
                {
                    float yaw = Mathf.Atan2(-p.X, -p.Z) * Mathf.Rad2Deg;
                    BossGate.Create(at, yaw, Assets, Biome.Boss != null ? Biome.Boss.Accent : Color.white, worldRoot);
                }
                else if (p.Kind == FeatureKind.LorePickup) LoreStone.Place(this, at, worldRoot, StageIndex);
                else Features.Build(this, p, worldRoot);
                FeaturePlaced?.Invoke(this, p, worldRoot);
            }
            if (!placed.Exists(p => p.Kind == FeatureKind.BossGate))
            {
                // Rough maps can starve the dart throw; the gate must exist, so put it on the ring.
                Debug.LogWarning("OldGods: boss gate fell back to a fixed spot");
                var at = new Vector3(0f, 0f, field.MaxZ - Biome.Terrain.RimWidth - 20f);
                BossGate.Create(at, 180f, Assets, Color.white, worldRoot);
            }
        }

        void ScatterProps(HeightField field, Rng rng)
        {
            var rockMat = WorldBuilder.Tinted(Assets.LowPoly, Biome.RockColor);
            var rock = PropModels.Get(Biome.RockModel);
            for (int i = 0; i < Biome.RockCount; i++)
            {
                float x = rng.Range(field.MinX + 20f, field.MaxX - 20f);
                float z = rng.Range(field.MinZ + 20f, field.MaxZ - 20f);
                if (x * x + z * z < 15f * 15f) continue;
                float s = rng.Range(0.8f, 2.6f);
                var pos = new Vector3(x, field.Sample(x, z) - 0.3f, z);
                WorldBuilder.CreateProp("Rock", rock, rockMat, pos, Quaternion.Euler(0f, rng.Range(0f, 360f), rng.Range(-8f, 8f)),
                    new Vector3(s, s * rng.Range(0.8f, 1.6f), s), worldRoot, true);
            }
            if (Biome.TreeCount > 0)
            {
                var treeMat = WorldBuilder.Tinted(Assets.LowPoly, Biome.TreeColor);
                var tree = PropModels.Get(Biome.TreeModel);
                for (int i = 0; i < Biome.TreeCount; i++)
                {
                    float x = rng.Range(field.MinX + 20f, field.MaxX - 20f);
                    float z = rng.Range(field.MinZ + 20f, field.MaxZ - 20f);
                    if (x * x + z * z < 15f * 15f || field.SlopeDegrees(x, z) > 30f) continue;
                    float s = rng.Range(0.9f, 1.6f);
                    WorldBuilder.CreateProp("Tree", tree, treeMat, new Vector3(x, field.Sample(x, z) - 0.2f, z),
                        Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), Vector3.one * s, worldRoot, true);
                }
            }
        }

        public void Announce(string headline, string subline) => Announced?.Invoke(headline, subline);

        /// <summary>Wakes this stage's boss at a point (from the boss gate).</summary>
        public void StartBoss(Vector3 at)
        {
            if (BossActive || Biome.Boss == null) return;
            int count = ShrineRules.BossesForCurses(BossCurses);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * 10f;
                var boss = BossController.Spawn(Biome.Boss, at + offset, StageIndex, BossCurses, Assets, Horde, Player, Seed.Stream("boss", StageIndex * 10 + i));
                boss.Defeated += OnBossDefeated;
            }
            Announce(Biome.Boss.DisplayName, count > 1 ? $"{count} of them wake" : Biome.Boss.Epithet);
        }

        void OnBossDefeated(BossController boss)
        {
            Summary.BossesKilled++;
            Vector3 at = boss.transform.position;
            if (IsFinal)
            {
                BossDefeated = true;
                StartCoroutine(Ending());
                return;
            }
            Features.DropFreeChest(this, at + Vector3.right * 4f, worldRoot);
            if (BossController.All.Count > 0) return; // more cursed guardians still stand
            BossDefeated = true;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                Pickups.Spawn(PickupKind.Xp, at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f, 4f + StageIndex * 4f);
            }
            BossFell?.Invoke(boss, at, worldRoot);
            NextPortal.Create(at + Vector3.forward * 0.1f, worldRoot);
            Announce($"{boss.DisplayName} has fallen", IsLastStage ? "The road to the throne is open" : "The way onward is open");
        }

        /// <summary>Hook for later milestones (boss chest, quests).</summary>
        public static event Action<BossController, Vector3, Transform> BossFell;

        /// <summary>Through the portal: the next stage, or The Last Test after the last one.</summary>
        public void LeaveStage()
        {
            if (IsOver || IsFinal) return;
            NoteSwarm();
            if (BossDefeated) Summary.StagesCleared++;
            if (IsLastStage)
            {
                Summary.ReachedThrone = true;
                BuildStage(StageCount, final: true);
                return;
            }
            BuildStage(StageIndex + 1);
        }

        void NoteSwarm()
        {
            if (Director != null && Director.Timeline != null && !IsFinal)
                Summary.BestSwarmSeconds = Mathf.Max(Summary.BestSwarmSeconds, Director.SwarmSeconds);
        }

        /// <summary>Lines for the endings; milestone 8 replaces these with the story text.</summary>
        public static Func<bool, IList<string>> EndingLines = takesThrone => takesThrone
            ? new[] { "Elias climbs to the empty throne and sits.", "The power of the old gods passes into him.", "They are gone. The age of the throne begins." }
            : new[] { "The Last Test is broken.", "The throne does not answer you.", "It waits for another." };

        System.Collections.IEnumerator Ending()
        {
            Director.Paused = true;
            Horde.KillAll(false);
            PlayerHealth.Invincible = true;
            bool takes = LastTest.TakesTheThrone(GodId);
            var arena = FinalArena.Instance;
            if (arena != null)
            {
                IList<string> lines = EndingLines(takes);
                var story = Assets.Story;
                if (story != null && (takes ? story.EliasEnding.Count : story.RefusedEnding.Count) > 0)
                    lines = takes ? story.EliasEnding : story.RefusedEnding;
                yield return takes ? arena.EliasEnding(this, lines) : arena.RefusedEnding(this, lines);
            }
            EndRun(true);
        }

        void OnEnemyKilled(Vector3 at, EnemyDef def, bool byPlayer)
        {
            if (!byPlayer) return;
            Kills++;
            if (def.XpValue > 0) Pickups.Spawn(PickupKind.Xp, at, def.XpValue);
        }

        void OnPickup(PickupKind kind, float amount)
        {
            switch (kind)
            {
                case PickupKind.Xp: Combat.AddXp(amount); break;
                case PickupKind.Heal: PlayerHealth.Health.Heal(amount); break;
            }
            PickedUp?.Invoke(kind, amount);
        }

        /// <summary>Hook for the economy (gold) in milestone 4.</summary>
        public event Action<PickupKind, float> PickedUp;

        void Update()
        {
            if (!IsOver && Time.timeScale > 0f) Elapsed += Time.deltaTime;
            if (Debug.isDebugBuild || Application.isEditor) DebugKeys();
        }

        /// <summary>Development shortcuts: F1 level up, F2 heal, F3 kill every enemy, F4 invincible, F5 skip 60 s, F6 wake boss, F7 next stage.</summary>
        void DebugKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || IsOver) return;
            if (kb.f1Key.wasPressedThisFrame && Combat != null) Combat.AddXp(Combat.Xp.Required - Combat.Xp.Xp + 0.01f);
            if (kb.f2Key.wasPressedThisFrame) PlayerHealth.Health.Heal(PlayerHealth.Health.Max);
            if (kb.f3Key.wasPressedThisFrame) Horde.KillAll(true);
            if (kb.f4Key.wasPressedThisFrame) PlayerHealth.Invincible = !PlayerHealth.Invincible;
            if (kb.f5Key.wasPressedThisFrame) Director.DebugSkip(60f);
            if (kb.f6Key.wasPressedThisFrame && !BossActive && !BossDefeated) StartBoss(Player.transform.position + Player.Facing * 12f);
            if (kb.f7Key.wasPressedThisFrame) LeaveStage();
        }

        void OnPlayerDied()
        {
            if (IsOver) return;
            EndRun(false);
        }

        /// <summary>Ends the run, won or lost. Results and payout hang off RunOver.</summary>
        public void EndRun(bool won)
        {
            if (IsOver) return;
            IsOver = true;
            Won = won;
            Player.InputEnabled = false;
            Director.Paused = true;
            GameInput.SetCursorLocked(false);
            NoteSwarm();
            Summary.Won = won;
            Summary.Kills = Kills;
            Summary.Level = Combat.Xp.Level;
            Summary.Seconds = Elapsed;
            Summary.ChestsOpened = Economy.ChestsOpened;
            Summary.ShrinesUsed = Economy.ShrinesUsed;
            Summary.GoldEarned = Economy.Wallet.Earned;
            Summary.ItemsFound = Combat.Items.Total;
            EmbersEarned = RunRewards.Embers(Summary);
            var save = SaveStore.Current;
            QuestsCompleted = MetaRules.PayRun(save, Summary, EmbersEarned);
            try { SaveStore.Save(save); }
            catch (Exception e) { Debug.LogError("OldGods: could not write the save: " + e.Message); }
            Paid?.Invoke(this);
            RunOver?.Invoke();
        }

        /// <summary>Milestone 7 pays the run into the save here, before results show.</summary>
        public static event Action<RunController> Paid;

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
