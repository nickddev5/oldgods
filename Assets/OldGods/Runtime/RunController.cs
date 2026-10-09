using System;
using System.Collections.Generic;
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

        public int StageIndex { get; private set; }
        public BiomeDefinition Biome { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool IsOver { get; private set; }
        public bool Won { get; private set; }
        public bool BossActive => BossController.Active != null;
        public bool BossDefeated { get; private set; }
        public bool IsLastStage => StageIndex >= StageCount - 1;
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
            BuildPersistent();
            BuildStage(0);
        }

        /// <summary>Things that live for the whole run: player, camera, horde, combat, UI.</summary>
        void BuildPersistent()
        {
            Player = WorldBuilder.CreatePlayer(Assets, Vector3.up * 50f, new Color(0.85f, 0.82f, 0.7f), null);
            PlayerHealth = Player.GetComponent<PlayerHealth>();
            PlayerHealth.Died += OnPlayerDied;
            Camera = WorldBuilder.CreateCamera(Player, null);
            sun = WorldBuilder.CreateSun(null, Color.white, 1.3f, new Vector3(50f, -35f, 0f));

            Horde = WorldBuilder.CreateHorde(Assets, Player, null);
            Horde.SpawnRng = Seed.Stream(RunSeed.Spawns);
            Content = Assets.Content.Load();
            foreach (var e in Assets.Content.Enemies)
                if (e != null) Horde.RegisterType(Content.Enemy(e.Id), e.MeshOrPlaceholder, e.Color, e.Emission);

            var systems = new GameObject("Combat Systems");
            Pickups = systems.AddComponent<Pickups>();
            Pickups.Player = Player.transform;
            systems.AddComponent<Projectiles>();
            systems.AddComponent<Effects>();
            Director = systems.AddComponent<StageDirector>();
            Director.Announce += Announce;
            Horde.EnemyKilled += OnEnemyKilled;
            Pickups.Collected += OnPickup;

            Combat = Player.gameObject.AddComponent<PlayerCombat>();
            Combat.Init(Assets.Content, Content, Seed);
            if (!string.IsNullOrEmpty(Assets.StartingWeapon)) Combat.GiveWeapon(Assets.StartingWeapon);
            var interaction = Player.gameObject.AddComponent<InteractionDriver>();
            interaction.Player = Combat;

            LevelUpScreen.Create(Combat, Seed.Stream(RunSeed.Draft));
            new GameObject("Damage Numbers").AddComponent<DamageNumbers>();
            Hud = Hud.Create(this);
            Minimap = Minimap.Create(Hud.Root);
            GameInput.SetCursorLocked(true);
        }

        public BiomeDefinition BiomeFor(int stage)
        {
            if (Assets.Stages != null && Assets.Stages.Count > 0)
                return Assets.Stages[Mathf.Clamp(stage, 0, Assets.Stages.Count - 1)];
            return Assets.GreyboxBiome;
        }

        /// <summary>Builds stage n's world and puts the player at its start.</summary>
        public void BuildStage(int stage)
        {
            StageIndex = stage;
            Biome = BiomeFor(stage);
            BossDefeated = false;
            BossCurses = 0;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            if (BossController.Active != null) Destroy(BossController.Active.gameObject);
            worldRoot = new GameObject($"World {stage + 1}").transform;

            Horde.Clear();
            Pickups.Clear();
            Projectiles.Instance.Clear();

            var profile = Biome.Terrain;
            var field = TerrainGenerator.Generate(profile, Seed.Stream(RunSeed.Map, stage));
            Ground.Set(field, profile.RimWidth);
            TerrainMesh.Build(field, WorldBuilder.Tinted(Assets.LowPoly, Color.white), Biome.Palette, profile.HillHeight, worldRoot);
            ScatterProps(field, Seed.Stream(RunSeed.Map, 100 + stage));

            sun.color = Biome.Sun;
            sun.intensity = Biome.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(Biome.SunEuler);
            WorldBuilder.SetAtmosphere(Biome.AmbientSky, Biome.AmbientEquator, Biome.AmbientGround, Biome.Fog, Biome.FogStart, Biome.FogEnd);

            PlaceFeatures(field, Seed.Stream(RunSeed.Map, 200 + stage));

            Player.Teleport(Ground.Snap(Vector3.zero) + Vector3.up * 0.3f);
            Camera.SnapBehind();
            Director.Begin(Biome.Timeline, stage, Horde, Seed.Stream(RunSeed.Spawns, stage));
            Minimap.SetGround(field, Biome.Palette, profile.HillHeight);
            StageStarted?.Invoke(stage);
            Announce($"{Biome.DisplayName}", $"Stage {stage + 1} of {StageCount}");
        }

        /// <summary>Feature kinds for this milestone; later milestones extend the list.</summary>
        public static readonly List<FeatureRequest> FeatureRequests = new List<FeatureRequest>
        {
            new FeatureRequest(FeatureKind.BossGate, 1, 0f),
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
            var rock = PlaceholderMeshes.Rock();
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
                var tree = PlaceholderMeshes.Tree();
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
            var boss = BossController.Spawn(Biome.Boss, at, StageIndex, BossCurses, Assets, Horde, Player, Seed.Stream("boss", StageIndex));
            boss.Defeated += OnBossDefeated;
            Announce(Biome.Boss.DisplayName, Biome.Boss.Epithet);
        }

        void OnBossDefeated(BossController boss)
        {
            BossDefeated = true;
            Vector3 at = boss.transform.position;
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

        /// <summary>Through the portal: the next stage, or the end of the road.</summary>
        public void LeaveStage()
        {
            if (IsOver) return;
            if (IsLastStage)
            {
                if (ReachedThrone != null) ReachedThrone(this);
                else EndRun(true);
                return;
            }
            BuildStage(StageIndex + 1);
        }

        /// <summary>Set by milestone 5: what happens after the last stage (The Last Test).</summary>
        public static Action<RunController> ReachedThrone;

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
            if (IsOver && (GameInput.Pressed(GameInput.Jump) || GameInput.Pressed(GameInput.Interact))) Restart();
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
            RunOver?.Invoke();
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
