using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    [Serializable]
    public struct MixEntry
    {
        public string EnemyId;
        public float Weight;

        public MixEntry(string id, float weight)
        {
            EnemyId = id;
            Weight = weight;
        }
    }

    /// <summary>A stretch of the stage clock with its own spawn rate and enemy mix.</summary>
    [Serializable]
    public sealed class SpawnPhase
    {
        /// <summary>Seconds since stage start.</summary>
        public float Start, End;
        /// <summary>How many enemies the director keeps alive, at the start and end of the phase.</summary>
        public int AliveFrom = 30, AliveTo = 60;
        /// <summary>Enemies spawned per second while below the alive target.</summary>
        public float Rate = 6f;
        public List<MixEntry> Mix = new List<MixEntry>();
    }

    public enum StageEventKind
    {
        Miniboss,   // one strong enemy announced on screen
        Swarm,      // a burst of enemies in a ring all at once
        EliteWave,  // a handful of stronger enemies
    }

    [Serializable]
    public sealed class StageEvent
    {
        public float At;
        public StageEventKind Kind;
        public string EnemyId;
        public int Count = 1;
    }

    /// <summary>The script of one stage. PLACEHOLDER numbers until play-tested.</summary>
    [Serializable]
    public sealed class StageTimelineDef
    {
        public float Duration = 600f;
        public List<SpawnPhase> Phases = new List<SpawnPhase>();
        public List<StageEvent> Events = new List<StageEvent>();
        /// <summary>Ghost enemy used for the final swarm.</summary>
        public string SwarmEnemyId = "enemy.ghost";
    }

    /// <summary>
    /// Run-wide difficulty, shaped like Risk of Rain 2's: it climbs with every minute of the run
    /// and jumps with every stage cleared, and it has no cap. Enemy health, damage and numbers,
    /// the final swarm and The Last Test's stream all read it; run modifiers and Greed multiply
    /// on top. PLACEHOLDER numbers, calibrated so the Grey Steppe plays as it did with the old
    /// per-stage tables (health +18% per stage minute) and later stages climb harder.
    /// </summary>
    public static class Difficulty
    {
        /// <summary>Coefficient gained per run minute, before the tier scalar.</summary>
        public const float TimeRate = 0.18f;
        /// <summary>The coefficient is multiplied by this for each stage cleared.</summary>
        public const float StageGrowth = 1.4f;
        /// <summary>Share of the coefficient's growth that reaches enemy damage.</summary>
        public const float DamageShare = 0.15f;
        /// <summary>Share of the coefficient's growth that reaches how many enemies are kept alive.</summary>
        public const float DensityShare = 0.06f;

        /// <summary>(1 + runMinutes * TimeRate * tier) * StageGrowth^stagesCleared. Tier 1 is the normal time rate.</summary>
        public static float Coefficient(float runMinutes, int stagesCleared, float tier = 1f) =>
            (1f + Math.Max(0f, runMinutes) * TimeRate * Math.Max(0f, tier)) * (float)Math.Pow(StageGrowth, Math.Max(0, stagesCleared));

        public static float Health(float coefficient) => coefficient;
        public static float Damage(float coefficient) => 1f + DamageShare * (coefficient - 1f);
        public static float Density(float coefficient) => 1f + DensityShare * (coefficient - 1f);
    }

    /// <summary>Answers "what should be happening now" for a stage timeline.</summary>
    public static class TimelineEvaluator
    {

        public static SpawnPhase PhaseAt(StageTimelineDef def, float t)
        {
            SpawnPhase last = null;
            foreach (var p in def.Phases)
            {
                if (t >= p.Start && t < p.End) return p;
                if (p.Start <= t) last = p;
            }
            return last;
        }

        /// <summary>Enemies to keep alive at time t, interpolated across the phase and scaled by density (Difficulty.Density).</summary>
        public static int TargetAlive(StageTimelineDef def, float t, float density)
        {
            var p = PhaseAt(def, t);
            if (p == null) return 0;
            float span = Math.Max(1f, p.End - p.Start);
            float k = Math.Max(0f, Math.Min(1f, (t - p.Start) / span));
            float alive = p.AliveFrom + (p.AliveTo - p.AliveFrom) * k;
            return (int)Math.Round(alive * density);
        }

        /// <summary>Events with At in (from, to]. Call once per frame with the previous and current time.</summary>
        public static List<StageEvent> EventsBetween(StageTimelineDef def, float from, float to)
        {
            var list = new List<StageEvent>();
            foreach (var e in def.Events)
                if (e.At > from && e.At <= to) list.Add(e);
            return list;
        }

        /// <summary>Picks an enemy id from the phase mix.</summary>
        public static string PickEnemy(SpawnPhase phase, Rng rng)
        {
            if (phase == null || phase.Mix.Count == 0) return null;
            var weights = new List<float>(phase.Mix.Count);
            foreach (var m in phase.Mix) weights.Add(m.Weight);
            int i = rng.PickWeighted(weights);
            return i >= 0 ? phase.Mix[i].EnemyId : null;
        }

        public static float Remaining(StageTimelineDef def, float t) => Math.Max(0f, def.Duration - t);
        public static bool InFinalSwarm(StageTimelineDef def, float t) => t >= def.Duration;

        /// <summary>Validation for authored timelines: phases ordered and covering the stage, events inside it.</summary>
        public static List<string> Validate(StageTimelineDef def)
        {
            var problems = new List<string>();
            if (def.Duration <= 0f) problems.Add("Duration must be positive");
            if (def.Phases.Count == 0) problems.Add("No spawn phases");
            float cursor = 0f;
            foreach (var p in def.Phases)
            {
                if (Math.Abs(p.Start - cursor) > 0.01f) problems.Add($"Phase starting at {p.Start} leaves a gap or overlap at {cursor}");
                if (p.End <= p.Start) problems.Add($"Phase at {p.Start} ends before it starts");
                if (p.Mix.Count == 0) problems.Add($"Phase at {p.Start} has no enemies");
                cursor = p.End;
            }
            if (def.Phases.Count > 0 && cursor < def.Duration) problems.Add($"Phases end at {cursor}, before the stage ends at {def.Duration}");
            foreach (var e in def.Events)
                if (e.At < 0f || e.At > def.Duration) problems.Add($"Event at {e.At} is outside the stage");
            return problems;
        }
    }

    /// <summary>The endless ghost waves after the stage clock runs out.</summary>
    public static class FinalSwarm
    {
        /// <summary>Enemies kept alive: starts high and keeps climbing. density is Difficulty.Density.</summary>
        public static int TargetAlive(float secondsInSwarm, float density) =>
            (int)Math.Round((120f + secondsInSwarm * 4f) * density);

        /// <summary>
        /// Ghost health climbs fast so the swarm always wins eventually: the run's coefficient
        /// (still rising with time) times a factor that starts near 0.55 and gains 1 every 55 s.
        /// On the Grey Steppe this matches the old 1.5 + s/20.
        /// </summary>
        public static float HealthMultiplier(float secondsInSwarm, float coefficient) =>
            coefficient * (0.55f + Math.Max(0f, secondsInSwarm) / 55f);

        /// <summary>Currency multiplier for surviving: +25% per full 30 seconds.</summary>
        public static float SurvivalMultiplier(float secondsInSwarm) => 1f + (float)Math.Floor(Math.Max(0f, secondsInSwarm) / 30f) * 0.25f;
    }

    /// <summary>The default greybox timeline; biome timelines in content override it.</summary>
    public static class DefaultTimelines
    {
        /// <summary>
        /// The standard ten-minute stage from four enemy roles: the basic foe, a fast one,
        /// a tank, and the stage's champion.
        /// </summary>
        public static StageTimelineDef Standard(string basic, string fast, string tank, string champion, string swarmer = null)
        {
            swarmer ??= fast;
            var def = new StageTimelineDef { Duration = 600f };
            def.Phases.Add(new SpawnPhase { Start = 0f, End = 60f, AliveFrom = 15, AliveTo = 35, Rate = 5f, Mix = { new MixEntry(basic, 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 60f, End = 180f, AliveFrom = 35, AliveTo = 70, Rate = 8f, Mix = { new MixEntry(basic, 3f), new MixEntry(fast, 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 180f, End = 360f, AliveFrom = 70, AliveTo = 130, Rate = 12f, Mix = { new MixEntry(basic, 3f), new MixEntry(fast, 2f), new MixEntry(tank, 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 360f, End = 600f, AliveFrom = 130, AliveTo = 220, Rate = 18f, Mix = { new MixEntry(basic, 2f), new MixEntry(fast, 2f), new MixEntry(tank, 2f) } });
            def.Events.Add(new StageEvent { At = 90f, Kind = StageEventKind.Swarm, EnemyId = swarmer, Count = 30 });
            def.Events.Add(new StageEvent { At = 150f, Kind = StageEventKind.Miniboss, EnemyId = champion, Count = 1 });
            def.Events.Add(new StageEvent { At = 270f, Kind = StageEventKind.EliteWave, EnemyId = tank, Count = 8 });
            def.Events.Add(new StageEvent { At = 330f, Kind = StageEventKind.Miniboss, EnemyId = champion, Count = 2 });
            def.Events.Add(new StageEvent { At = 420f, Kind = StageEventKind.Swarm, EnemyId = basic, Count = 60 });
            def.Events.Add(new StageEvent { At = 510f, Kind = StageEventKind.Miniboss, EnemyId = champion, Count = 3 });
            return def;
        }

        /// <summary>The Last Test: no clock, a thin stream of ghosts and husks around the fight.</summary>
        public static StageTimelineDef FinalArena()
        {
            var def = new StageTimelineDef { Duration = 3600f };
            def.Phases.Add(new SpawnPhase { Start = 0f, End = 3600f, AliveFrom = 20, AliveTo = 60, Rate = 4f, Mix = { new MixEntry("enemy.ghost", 2f), new MixEntry("enemy.husk", 1f) } });
            return def;
        }

        public static StageTimelineDef Greybox()
        {
            var def = new StageTimelineDef { Duration = 600f };
            def.Phases.Add(new SpawnPhase { Start = 0f, End = 60f, AliveFrom = 15, AliveTo = 35, Rate = 5f, Mix = { new MixEntry("enemy.husk", 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 60f, End = 180f, AliveFrom = 35, AliveTo = 70, Rate = 8f, Mix = { new MixEntry("enemy.husk", 3f), new MixEntry("enemy.runner", 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 180f, End = 360f, AliveFrom = 70, AliveTo = 130, Rate = 12f, Mix = { new MixEntry("enemy.husk", 3f), new MixEntry("enemy.runner", 2f), new MixEntry("enemy.brute", 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 360f, End = 600f, AliveFrom = 130, AliveTo = 220, Rate = 18f, Mix = { new MixEntry("enemy.husk", 2f), new MixEntry("enemy.runner", 2f), new MixEntry("enemy.brute", 2f) } });
            def.Events.Add(new StageEvent { At = 90f, Kind = StageEventKind.Swarm, EnemyId = "enemy.runner", Count = 30 });
            def.Events.Add(new StageEvent { At = 150f, Kind = StageEventKind.Miniboss, EnemyId = "enemy.champion", Count = 1 });
            def.Events.Add(new StageEvent { At = 270f, Kind = StageEventKind.EliteWave, EnemyId = "enemy.brute", Count = 8 });
            def.Events.Add(new StageEvent { At = 330f, Kind = StageEventKind.Miniboss, EnemyId = "enemy.champion", Count = 2 });
            def.Events.Add(new StageEvent { At = 420f, Kind = StageEventKind.Swarm, EnemyId = "enemy.husk", Count = 60 });
            def.Events.Add(new StageEvent { At = 510f, Kind = StageEventKind.Miniboss, EnemyId = "enemy.champion", Count = 3 });
            return def;
        }
    }
}
