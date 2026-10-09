using System;
using OldGods.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OldGods.Runtime
{
    /// <summary>
    /// Hosts one run: builds the world from the run seed, owns the player and horde,
    /// and ends the run when the player dies. The only component a run scene needs.
    /// </summary>
    public sealed class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }

        public GameAssets Assets;
        [Tooltip("Hex seed for repeatable runs; empty picks a new one.")]
        public string FixedSeed = "";
        [Tooltip("Greybox: how many enemies the test spawner keeps alive.")]
        public int GreyboxEnemyTarget = 120;

        public RunSeed Seed { get; private set; }
        public PlayerMotor Player { get; private set; }
        public PlayerHealth PlayerHealth { get; private set; }
        public ChaseCamera Camera { get; private set; }
        public HordeManager Horde { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool IsOver { get; private set; }

        public event Action RunOver;

        Transform worldRoot;

        void Awake()
        {
            Instance = this;
            if (Assets == null) Assets = GameAssets.Load();
            GameInput.Ensure();
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
            BuildGreybox();
        }

        void BuildGreybox()
        {
            worldRoot = new GameObject("World").transform;
            var profile = Assets.GreyboxTerrain;
            var field = TerrainGenerator.Generate(profile, Seed.Stream(RunSeed.Map, 0));
            Ground.Set(field, profile.RimWidth);
            TerrainMesh.Build(field, WorldBuilder.Tinted(Assets.LowPoly, Color.white), Assets.GreyboxPalette, profile.HillHeight, worldRoot);
            ScatterRocks(field, Seed.Stream(RunSeed.Map, 100));

            WorldBuilder.CreateSun(worldRoot, new Color(1f, 0.95f, 0.85f), 1.3f, new Vector3(50f, -35f, 0f));

            Player = WorldBuilder.CreatePlayer(Assets, Ground.Snap(Vector3.zero) + Vector3.up * 0.2f, new Color(0.85f, 0.82f, 0.7f), null);
            PlayerHealth = Player.GetComponent<PlayerHealth>();
            PlayerHealth.Died += OnPlayerDied;
            Camera = WorldBuilder.CreateCamera(Player, null);
            WorldBuilder.SetAtmosphere(new Color(0.62f, 0.68f, 0.78f), new Color(0.5f, 0.5f, 0.48f), new Color(0.25f, 0.24f, 0.22f),
                new Color(0.66f, 0.70f, 0.74f), 40f, 170f);

            Horde = WorldBuilder.CreateHorde(Assets, Player, null);
            Horde.SpawnRng = Seed.Stream(RunSeed.Spawns);
            Horde.EnemyKilled += (_, _, byPlayer) => { if (byPlayer) Kills++; };
            if (Assets.Content != null)
            {
                Assets.Content.Load();
                foreach (var e in Assets.Content.Enemies)
                    if (e != null) Horde.RegisterType(e.ToDef(), e.MeshOrPlaceholder, e.Color);
            }

            new GameObject("Damage Numbers").AddComponent<DamageNumbers>();
            var spawner = new GameObject("Greybox Spawner").AddComponent<GreyboxSpawner>();
            spawner.Horde = Horde;
            spawner.Target = GreyboxEnemyTarget;
            Hud.Create(this);
            Camera.SnapBehind();
            GameInput.SetCursorLocked(true);
        }

        void ScatterRocks(HeightField field, Rng rng)
        {
            var mat = WorldBuilder.Tinted(Assets.LowPoly, new Color(0.55f, 0.53f, 0.5f));
            var rock = PlaceholderMeshes.Rock();
            for (int i = 0; i < 70; i++)
            {
                float x = rng.Range(field.MinX + 20f, field.MaxX - 20f);
                float z = rng.Range(field.MinZ + 20f, field.MaxZ - 20f);
                if (x * x + z * z < 15f * 15f) continue;
                float s = rng.Range(0.8f, 2.6f);
                var pos = new Vector3(x, field.Sample(x, z) - 0.3f, z);
                WorldBuilder.CreateProp("Rock", rock, mat, pos, Quaternion.Euler(0f, rng.Range(0f, 360f), rng.Range(-8f, 8f)),
                    new Vector3(s, s * rng.Range(0.8f, 1.6f), s), worldRoot, true);
            }
        }

        void Update()
        {
            if (!IsOver) Elapsed += Time.deltaTime;
            if (IsOver && (GameInput.Pressed(GameInput.Jump) || GameInput.Pressed(GameInput.Interact))) Restart();
        }

        void OnPlayerDied()
        {
            if (IsOver) return;
            IsOver = true;
            Player.InputEnabled = false;
            GameInput.SetCursorLocked(false);
            RunOver?.Invoke();
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
