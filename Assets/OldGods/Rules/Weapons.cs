using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    public enum WeaponShape
    {
        Projectile, // fires at the nearest enemies
        Aura,       // damages everything around the player on a tick
        Area,       // strikes points on or near enemies after a short delay
        Orbit,      // objects circle the player and hit what they touch
        Pull,       // a vortex drags enemies in and damages them
        Chain,      // hits the nearest enemy, then jumps to the next
    }

    /// <summary>Weapon numbers. What an upgrade amount means depends on the stat; see WeaponState.Effective.</summary>
    public enum WeaponStat
    {
        Damage,     // upgrade: flat damage added
        Cooldown,   // upgrade: fraction faster (0.1 = 10% shorter cooldown)
        Count,      // upgrade: whole projectiles, orbiters, strikes or jumps
        Size,       // upgrade: fraction larger
        Pierce,     // upgrade: whole extra enemies a projectile passes through
        Speed,      // upgrade: fraction faster projectiles or orbit
        Duration,   // upgrade: fraction longer
        Range,      // upgrade: fraction further reach
        Knockback,  // upgrade: fraction more
        CritChance, // upgrade: added chance (0.05 = +5%)
    }

    [Serializable]
    public struct WeaponUpgrade
    {
        public WeaponStat Stat;
        /// <summary>Amount at Common rarity.</summary>
        public float Amount;

        public WeaponUpgrade(WeaponStat stat, float amount)
        {
            Stat = stat;
            Amount = amount;
        }
    }

    /// <summary>Base numbers for a weapon at level 1.</summary>
    [Serializable]
    public struct WeaponStats
    {
        public float Damage;
        public float Cooldown;
        public float Count;
        public float Size;
        public float Pierce;
        public float Speed;
        public float Duration;
        public float Range;
        public float Knockback;
        public float CritChance;
    }

    public sealed class WeaponDef
    {
        public string Id;
        public string Name;
        public string Description;
        public WeaponShape Shape;
        public WeaponStats Base;
        public List<WeaponUpgrade> UpgradePool = new List<WeaponUpgrade>();
        /// <summary>Seconds hit enemies are slowed; 0 for none.</summary>
        public float SlowSeconds;
        /// <summary>Content that must be unlocked before this weapon enters the draft; null if always available.</summary>
        public string UnlockId;
    }

    /// <summary>The numbers a weapon fires with after its upgrades and the player's stats.</summary>
    public struct EffectiveWeapon
    {
        public float Damage, Cooldown, Size, Speed, Duration, Range, Knockback, CritChance, CritMultiplier;
        public int Count, Pierce;
    }

    /// <summary>One weapon the player owns: its level and the upgrades it has taken.</summary>
    public sealed class WeaponState
    {
        public readonly WeaponDef Def;
        public int Level = 1;
        readonly float[] bonus = new float[Enum.GetValues(typeof(WeaponStat)).Length];

        public WeaponState(WeaponDef def) { Def = def; }

        public float Bonus(WeaponStat s) => bonus[(int)s];
        public void AddBonus(WeaponStat s, float amount) => bonus[(int)s] += amount;

        public EffectiveWeapon Effective(StatBlock player)
        {
            var b = Def.Base;
            float cooldownCut = Math.Min(0.75f, Bonus(WeaponStat.Cooldown));
            var e = new EffectiveWeapon
            {
                Damage = (b.Damage + Bonus(WeaponStat.Damage)) * player.Value(StatId.Damage),
                Cooldown = Math.Max(0.08f, b.Cooldown * (1f - cooldownCut) / player.Value(StatId.AttackSpeed)),
                Size = b.Size * (1f + Bonus(WeaponStat.Size)) * player.Value(StatId.Area),
                Speed = b.Speed * (1f + Bonus(WeaponStat.Speed)) * player.Value(StatId.ProjectileSpeed),
                Duration = b.Duration * (1f + Bonus(WeaponStat.Duration)) * player.Value(StatId.Duration),
                Range = b.Range * (1f + Bonus(WeaponStat.Range)),
                Knockback = b.Knockback * (1f + Bonus(WeaponStat.Knockback)) * player.Value(StatId.Knockback),
                CritChance = Math.Min(1f, b.CritChance + Bonus(WeaponStat.CritChance) + player.Value(StatId.CritChance)),
                CritMultiplier = player.Value(StatId.CritDamage),
                Pierce = (int)Math.Round(b.Pierce + Bonus(WeaponStat.Pierce)),
            };
            int count = (int)Math.Round(b.Count + Bonus(WeaponStat.Count));
            // Auras are a single field; extra projectiles from stats do not apply to them.
            if (Def.Shape != WeaponShape.Aura) count += player.IntValue(StatId.ProjectileCount);
            e.Count = Math.Max(1, count);
            return e;
        }

        /// <summary>Rolls a hit's damage, applying a crit when the roll is under the chance.</summary>
        public static float RollDamage(in EffectiveWeapon e, float roll01, out bool crit)
        {
            crit = roll01 < e.CritChance;
            return crit ? e.Damage * e.CritMultiplier : e.Damage;
        }
    }

    public sealed class PassiveDef
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>What one level gives at Common rarity.</summary>
        public StatMod PerLevel;
        public string UnlockId;
    }

    public sealed class PassiveState
    {
        public readonly PassiveDef Def;
        public int Level = 1;
        public readonly List<StatMod> Mods = new List<StatMod>();

        public PassiveState(PassiveDef def)
        {
            Def = def;
            Mods.Add(def.PerLevel);
        }
    }
}
