using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>Every stat a god, passive, shrine or powerup can change.</summary>
    public enum StatId
    {
        MaxHealth,
        Regen,          // health per second
        Armor,          // flat damage removed from each hit
        Evasion,        // 0..1 chance to ignore a hit
        MoveSpeed,      // multiplier, 1 = base
        Damage,         // multiplier
        AttackSpeed,    // multiplier on fire rate (cooldowns divide by it)
        Area,           // multiplier on weapon size
        ProjectileCount,// added to every weapon's count
        ProjectileSpeed,// multiplier
        Duration,       // multiplier on effect lifetimes
        PickupRange,    // world units
        Luck,           // 0 = none; shifts rarity rolls
        XpGain,         // multiplier
        GoldGain,       // multiplier
        Knockback,      // multiplier
        CritChance,     // 0..1
        CritDamage,     // multiplier applied on a crit
        JumpHeight,     // multiplier
        ExtraJumps,     // count
    }

    /// <summary>One change to a stat: added to the base, then the sum of percents applied.</summary>
    [Serializable]
    public struct StatMod
    {
        public StatId Stat;
        public float Add;
        /// <summary>Fraction: 0.1 is +10% of the base-plus-adds.</summary>
        public float Percent;

        public StatMod(StatId stat, float add, float percent = 0f)
        {
            Stat = stat;
            Add = add;
            Percent = percent;
        }

        public StatMod Scaled(float k) => new StatMod(Stat, Add * k, Percent * k);

        public override string ToString() => StatText.Describe(this);
    }

    /// <summary>Base stats plus a list of mods; Value(stat) = (base + adds) * (1 + percents), clamped.</summary>
    public sealed class StatBlock
    {
        readonly float[] baseValues = new float[Enum.GetValues(typeof(StatId)).Length];
        readonly float[] adds = new float[Enum.GetValues(typeof(StatId)).Length];
        readonly float[] percents = new float[Enum.GetValues(typeof(StatId)).Length];

        public static StatBlock Default()
        {
            var b = new StatBlock();
            b.SetBase(StatId.MaxHealth, 100f);
            b.SetBase(StatId.MoveSpeed, 1f);
            b.SetBase(StatId.Damage, 1f);
            b.SetBase(StatId.AttackSpeed, 1f);
            b.SetBase(StatId.Area, 1f);
            b.SetBase(StatId.ProjectileSpeed, 1f);
            b.SetBase(StatId.Duration, 1f);
            b.SetBase(StatId.PickupRange, 3.5f);
            b.SetBase(StatId.XpGain, 1f);
            b.SetBase(StatId.GoldGain, 1f);
            b.SetBase(StatId.Knockback, 1f);
            b.SetBase(StatId.CritChance, 0.05f);
            b.SetBase(StatId.CritDamage, 1.5f);
            b.SetBase(StatId.JumpHeight, 1f);
            return b;
        }

        public void SetBase(StatId s, float v) => baseValues[(int)s] = v;
        public float Base(StatId s) => baseValues[(int)s];

        public void Clear()
        {
            Array.Clear(adds, 0, adds.Length);
            Array.Clear(percents, 0, percents.Length);
        }

        public void Apply(StatMod m)
        {
            adds[(int)m.Stat] += m.Add;
            percents[(int)m.Stat] += m.Percent;
        }

        public void ApplyAll(IEnumerable<StatMod> mods)
        {
            foreach (var m in mods) Apply(m);
        }

        public float Value(StatId s)
        {
            int i = (int)s;
            float v = (baseValues[i] + adds[i]) * (1f + percents[i]);
            return Clamp(s, v);
        }

        public int IntValue(StatId s) => (int)Math.Floor(Value(s) + 1e-4f);

        static float Clamp(StatId s, float v)
        {
            switch (s)
            {
                case StatId.Evasion: return Math.Max(0f, Math.Min(0.75f, v));
                case StatId.CritChance: return Math.Max(0f, Math.Min(1f, v));
                case StatId.MaxHealth: return Math.Max(1f, v);
                case StatId.MoveSpeed:
                case StatId.AttackSpeed:
                case StatId.Area:
                case StatId.Damage:
                case StatId.ProjectileSpeed:
                case StatId.Duration:
                    return Math.Max(0.1f, v);
                default: return Math.Max(0f, v);
            }
        }
    }

    public static class StatText
    {
        public static string Name(StatId s)
        {
            switch (s)
            {
                case StatId.MaxHealth: return "Max Health";
                case StatId.Regen: return "Regeneration";
                case StatId.Armor: return "Armour";
                case StatId.Evasion: return "Evasion";
                case StatId.MoveSpeed: return "Move Speed";
                case StatId.Damage: return "Damage";
                case StatId.AttackSpeed: return "Attack Speed";
                case StatId.Area: return "Area";
                case StatId.ProjectileCount: return "Projectiles";
                case StatId.ProjectileSpeed: return "Projectile Speed";
                case StatId.Duration: return "Duration";
                case StatId.PickupRange: return "Pickup Range";
                case StatId.Luck: return "Luck";
                case StatId.XpGain: return "XP Gain";
                case StatId.GoldGain: return "Gold Gain";
                case StatId.Knockback: return "Knockback";
                case StatId.CritChance: return "Crit Chance";
                case StatId.CritDamage: return "Crit Damage";
                case StatId.JumpHeight: return "Jump Height";
                case StatId.ExtraJumps: return "Extra Jumps";
                default: return s.ToString();
            }
        }

        /// <summary>Stats whose Add is shown as a percentage (they are fractions or multipliers).</summary>
        public static bool AddIsPercent(StatId s) =>
            s == StatId.Evasion || s == StatId.CritChance || s == StatId.Luck || s == StatId.MoveSpeed ||
            s == StatId.Damage || s == StatId.AttackSpeed || s == StatId.Area || s == StatId.ProjectileSpeed ||
            s == StatId.Duration || s == StatId.XpGain || s == StatId.GoldGain || s == StatId.Knockback ||
            s == StatId.CritDamage || s == StatId.JumpHeight;

        public static string Describe(StatMod m)
        {
            var parts = new List<string>(2);
            if (Math.Abs(m.Add) > 1e-5f)
            {
                string v = AddIsPercent(m.Stat) ? $"{m.Add * 100f:+0.#;-0.#}%" : $"{m.Add:+0.##;-0.##}";
                parts.Add(v);
            }
            if (Math.Abs(m.Percent) > 1e-5f) parts.Add($"{m.Percent * 100f:+0.#;-0.#}%");
            return $"{string.Join(" ", parts)} {Name(m.Stat)}";
        }
    }
}
