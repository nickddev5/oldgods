using System;
using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Runs one stage's clock: keeps the horde at the timeline's density, fires timed
    /// events (swarms, elite waves, minibosses) and starts the final swarm at 0:00.
    /// </summary>
    public sealed class StageDirector : MonoBehaviour
    {
        public static StageDirector Instance { get; private set; }

        public StageTimelineDef Timeline;
        public int StageIndex;
        public HordeManager Horde;
        public Rng Rng;
        /// <summary>Difficulty from run modifiers; lasts the whole run.</summary>
        public float RunDifficulty;
        /// <summary>Difficulty from Greed shrines; resets each stage.</summary>
        public float StageDifficulty;
        /// <summary>Extra enemy density and health: 0.3 is +30%.</summary>
        public float DifficultyBonus => RunDifficulty + StageDifficulty;
        /// <summary>Scales how fast the run's difficulty climbs with time; 1 is normal.</summary>
        public float Tier = 1f;
        /// <summary>Run seconds when this stage began, so the difficulty keeps the run's clock.</summary>
        public float RunSecondsAtStart { get; private set; }
        public float RunSeconds => RunSecondsAtStart + Elapsed;
        /// <summary>The run-wide difficulty coefficient now (see Difficulty).</summary>
        public float Coefficient => Difficulty.Coefficient(RunSeconds / 60f, StageIndex, Tier);
        /// <summary>The coefficient when this stage began; damage and numbers read this one.</summary>
        public float StageCoefficient => Difficulty.Coefficient(RunSecondsAtStart / 60f, StageIndex, Tier);

        public float Elapsed { get; private set; }
        public float Remaining => TimelineEvaluator.Remaining(Timeline, Elapsed);
        public bool InFinalSwarm => TimelineEvaluator.InFinalSwarm(Timeline, Elapsed);
        public float SwarmSeconds => Mathf.Max(0f, Elapsed - Timeline.Duration);
        public bool Paused;

        /// <summary>Announcements for the HUD: (headline, subline).</summary>
        public event Action<string, string> Announce;
        public event Action FinalSwarmStarted;

        readonly Dictionary<string, int> typeById = new Dictionary<string, int>();
        float bank;
        bool swarmAnnounced;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>stageIndex counts the stages cleared before this one; The Last Test follows them all.</summary>
        public void Begin(StageTimelineDef timeline, int stageIndex, HordeManager horde, Rng rng, float runSeconds)
        {
            RunSecondsAtStart = runSeconds;
            Timeline = timeline;
            StageIndex = stageIndex;
            Horde = horde;
            Rng = rng;
            Elapsed = 0f;
            bank = 0f;
            StageDifficulty = 0f;
            swarmAnnounced = false;
            typeById.Clear();
            for (int i = 0; i < horde.Types.Count; i++)
                if (!typeById.ContainsKey(horde.Types[i].Id)) typeById[horde.Types[i].Id] = i;
        }

        /// <summary>Development shortcut: moves the clock forward.</summary>
        public void DebugSkip(float seconds) => Elapsed += seconds;

        int TypeIndex(string id) => id != null && typeById.TryGetValue(id, out int t) ? t : -1;

        float Health() => Difficulty.Health(Coefficient) * (1f + DifficultyBonus);
        float Damage() => Difficulty.Damage(StageCoefficient);
        float Density() => Difficulty.Density(StageCoefficient);

        public int SpawnEnemy(string id, Vector3 at, float extraHealth = 1f)
        {
            int type = TypeIndex(id);
            if (type < 0) return -1;
            return Horde.Spawn(type, at, Health() * extraHealth, 1f, Damage());
        }

        int SpawnOnRing(string id, float extraHealth = 1f)
        {
            int type = TypeIndex(id);
            if (type < 0) return -1;
            return Horde.SpawnOnRing(type, Health() * extraHealth, 1f, Damage());
        }

        void Update()
        {
            if (Timeline == null || Horde == null || Paused) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float before = Elapsed;
            Elapsed += dt;

            foreach (var e in TimelineEvaluator.EventsBetween(Timeline, before, Elapsed)) Fire(e);

            if (InFinalSwarm)
            {
                if (!swarmAnnounced)
                {
                    swarmAnnounced = true;
                    Announce?.Invoke("The final swarm", "Survive, or leave through the portal");
                    FinalSwarmStarted?.Invoke();
                }
                Keep(FinalSwarm.TargetAlive(SwarmSeconds, Density()), 40f, () =>
                    Horde.SpawnOnRing(TypeIndex(Timeline.SwarmEnemyId), FinalSwarm.HealthMultiplier(SwarmSeconds, Coefficient), 1.15f, Damage()), dt);
                return;
            }

            var phase = TimelineEvaluator.PhaseAt(Timeline, Elapsed);
            if (phase == null) return;
            int target = Mathf.RoundToInt(TimelineEvaluator.TargetAlive(Timeline, Elapsed, Density()) * (1f + DifficultyBonus));
            Keep(target, phase.Rate * Density(), () => SpawnOnRing(TimelineEvaluator.PickEnemy(phase, Rng)), dt);
        }

        void Keep(int target, float rate, Func<int> spawn, float dt)
        {
            if (Horde.CommonAlive >= target)
            {
                bank = 0f;
                return;
            }
            bank += rate * dt;
            int guard = 64;
            while (bank >= 1f && Horde.CommonAlive < target && guard-- > 0)
            {
                bank -= 1f;
                if (spawn() < 0) break;
            }
        }

        void Fire(StageEvent e)
        {
            switch (e.Kind)
            {
                case StageEventKind.Swarm:
                    for (int i = 0; i < e.Count; i++) SpawnOnRing(e.EnemyId);
                    Announce?.Invoke("A swarm closes in", null);
                    break;
                case StageEventKind.EliteWave:
                    for (int i = 0; i < e.Count; i++) SpawnOnRing(e.EnemyId, 1.5f);
                    Announce?.Invoke("Elites approach", null);
                    break;
                case StageEventKind.Miniboss:
                    for (int i = 0; i < e.Count; i++) SpawnOnRing(e.EnemyId);
                    int type = TypeIndex(e.EnemyId);
                    string name = type >= 0 ? Horde.Types[type].DisplayName : "A champion";
                    Announce?.Invoke(e.Count > 1 ? $"{e.Count} champions approach" : $"{name} approaches", null);
                    break;
            }
        }
    }
}
