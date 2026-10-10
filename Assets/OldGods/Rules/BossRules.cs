using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    public enum BossAttack
    {
        Slam,       // telegraphed circle under the player, then a heavy hit
        Charge,     // telegraphed line, then a fast dash along it
        Shockwave,  // an expanding ring the player must jump over or outrun
        Summon,     // calls a ring of minions
        Volley,     // telegraphed circles rain around the player
    }

    [Serializable]
    public struct BossAttackDef
    {
        public BossAttack Attack;
        public float Weight;
        public float Cooldown;
        public float Damage;
        /// <summary>Seconds of warning before the hit lands.</summary>
        public float Telegraph;
        /// <summary>Radius of a slam or volley circle, length of a charge, or max radius of a shockwave.</summary>
        public float Size;
    }

    public sealed class BossDef
    {
        public string Id;
        public string Name;
        public float MaxHealth = 2500f;
        public float MoveSpeed = 3f;
        public float Scale = 3f;
        public float Radius = 1.4f;
        public float ContactDamage = 15f;
        /// <summary>Seconds between attacks, before attack cooldowns.</summary>
        public float Rest = 1.2f;
        public string MinionId = "enemy.husk";
        public List<BossAttackDef> Attacks = new List<BossAttackDef>();
        /// <summary>Below this health fraction the boss enrages: rests half as long.</summary>
        public float EnrageAt = 0.35f;
    }

    /// <summary>Chooses the boss's next attack: weighted among attacks whose cooldown has passed.</summary>
    public sealed class BossPattern
    {
        readonly BossDef def;
        readonly float[] readyAt;
        float restUntil;

        public BossPattern(BossDef def)
        {
            this.def = def;
            readyAt = new float[def.Attacks.Count];
        }

        public float RestTime(float healthFraction) => healthFraction <= def.EnrageAt ? def.Rest * 0.5f : def.Rest;

        /// <summary>Index of the next attack at time now, or -1 if resting or nothing is ready.</summary>
        public int Next(float now, float healthFraction, Rng rng)
        {
            if (now < restUntil) return -1;
            var weights = new List<float>(def.Attacks.Count);
            for (int i = 0; i < def.Attacks.Count; i++) weights.Add(now >= readyAt[i] ? def.Attacks[i].Weight : 0f);
            int pick = rng.PickWeighted(weights);
            if (pick < 0) return -1;
            readyAt[pick] = now + def.Attacks[pick].Cooldown;
            return pick;
        }

        /// <summary>Call when an attack finishes so the boss rests before the next.</summary>
        public void Finished(float now, float healthFraction) => restUntil = now + RestTime(healthFraction);
    }

    /// <summary>Boss strength by stage. PLACEHOLDER.</summary>
    public static class BossScaling
    {
        /// <summary>Coefficient at which the difficulty starts to add boss health.</summary>
        public const float DifficultyPivot = 2f;

        /// <summary>
        /// Extra boss health from the run's difficulty coefficient when the boss wakes:
        /// sqrt(coefficient / 2), never below 1. About 1 on the Grey Steppe, 1.7 in the Ash Wood,
        /// 2.3 on the Drowned Coast and 3 in The Last Test. Aimed weapons take the boss first, so
        /// without this the late fights ended in 20-35 s and The Last Test stopped being a test.
        /// </summary>
        public static float DifficultyFactor(float coefficient) => (float)Math.Max(1.0, Math.Sqrt(Math.Max(0f, coefficient) / DifficultyPivot));

        public static float Health(BossDef def, int stageIndex, int extraCurses, float coefficient = 1f) =>
            def.MaxHealth * (1f + stageIndex * 1.4f) * (1f + extraCurses * 0.5f) * DifficultyFactor(coefficient);

        public static float Damage(float baseDamage, int stageIndex) => baseDamage * (1f + stageIndex * 0.4f);
    }
}
