using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum DraftKind { NewWeapon, NewPassive, UpgradeWeapon, UpgradePassive, Restore }

    /// <summary>One card in the level-up draft. Upgrades are rolled when the card is dealt, so the card can show them.</summary>
    public sealed class DraftOption
    {
        public DraftKind Kind;
        public string Id;
        public Rarity Rarity;
        public int NextLevel;
        public readonly List<WeaponUpgrade> WeaponRolls = new List<WeaponUpgrade>();
        public StatMod PassiveRoll;
        public string Title;
        public string Description;

        public override string ToString() => $"{Kind} {Id} {Rarity} L{NextLevel}";
    }

    /// <summary>What the player carries: weapons, passives, slots and what has been banished.</summary>
    public sealed class Loadout
    {
        public int WeaponSlots = 4;
        public int PassiveSlots = 4;
        public int MaxLevel = 8;
        public readonly List<WeaponState> Weapons = new List<WeaponState>();
        public readonly List<PassiveState> Passives = new List<PassiveState>();
        public readonly HashSet<string> Banished = new HashSet<string>();

        public WeaponState Weapon(string id) => Weapons.FirstOrDefault(w => w.Def.Id == id);
        public PassiveState Passive(string id) => Passives.FirstOrDefault(p => p.Def.Id == id);

        public void AddWeapon(WeaponDef def)
        {
            if (Weapon(def.Id) == null) Weapons.Add(new WeaponState(def));
        }

        public void AddPassive(PassiveDef def)
        {
            if (Passive(def.Id) == null) Passives.Add(new PassiveState(def));
        }

        public IEnumerable<StatMod> PassiveMods()
        {
            foreach (var p in Passives)
                foreach (var m in p.Mods) yield return m;
        }
    }

    /// <summary>Refresh, Skip and Banish uses left this run.</summary>
    public sealed class DraftCharges
    {
        public int Refresh = 2;
        public int Skip = 2;
        public int Banish = 1;
    }

    public static class DraftRules
    {
        static readonly float[] BaseRarityWeights = { 60f, 25f, 10f, 4f, 1f };
        static readonly float[] RarityMultipliers = { 1f, 1.25f, 1.6f, 2f, 2.6f };

        public static float Multiplier(Rarity r) => RarityMultipliers[(int)r];

        /// <summary>
        /// Rarity weights after Luck. Each step up the ladder is multiplied by (1 + luck)^tier,
        /// so luck 0 gives the base table and higher luck moves weight toward Legendary.
        /// </summary>
        public static float[] RarityWeights(float luck)
        {
            var w = new float[BaseRarityWeights.Length];
            float k = 1f + Math.Max(0f, luck);
            for (int i = 0; i < w.Length; i++) w[i] = BaseRarityWeights[i] * (float)Math.Pow(k, i * 1.5);
            return w;
        }

        public static Rarity RollRarity(Rng rng, float luck) => (Rarity)rng.PickWeighted(RarityWeights(luck));

        /// <summary>
        /// Deals up to count distinct cards. Owned items that can still level are three times as
        /// likely as new ones. New items need a free slot and must not be banished or excluded.
        /// When nothing is left to offer, a single Restore card is dealt.
        /// </summary>
        public static List<DraftOption> Roll(Loadout loadout, IReadOnlyList<WeaponDef> weapons, IReadOnlyList<PassiveDef> passives,
            Rng rng, float luck, int count = 3, ICollection<string> exclude = null)
        {
            var candidates = new List<(DraftKind kind, string id, float weight)>();
            foreach (var w in loadout.Weapons)
                if (w.Level < loadout.MaxLevel && w.Def.UpgradePool.Count > 0 && !Excluded(w.Def.Id, loadout, exclude))
                    candidates.Add((DraftKind.UpgradeWeapon, w.Def.Id, 3f));
            foreach (var p in loadout.Passives)
                if (p.Level < loadout.MaxLevel && !Excluded(p.Def.Id, loadout, exclude))
                    candidates.Add((DraftKind.UpgradePassive, p.Def.Id, 3f));
            if (loadout.Weapons.Count < loadout.WeaponSlots)
                foreach (var w in weapons)
                    if (loadout.Weapon(w.Id) == null && !Excluded(w.Id, loadout, exclude))
                        candidates.Add((DraftKind.NewWeapon, w.Id, 1f));
            if (loadout.Passives.Count < loadout.PassiveSlots)
                foreach (var p in passives)
                    if (loadout.Passive(p.Id) == null && !Excluded(p.Id, loadout, exclude))
                        candidates.Add((DraftKind.NewPassive, p.Id, 1f));

            var result = new List<DraftOption>(count);
            var weights = new List<float>(candidates.Count);
            foreach (var c in candidates) weights.Add(c.weight);
            for (int n = 0; n < count; n++)
            {
                int pick = rng.PickWeighted(weights);
                if (pick < 0) break;
                weights[pick] = 0f;
                var c = candidates[pick];
                result.Add(Deal(c.kind, c.id, loadout, weapons, passives, rng, luck));
            }
            if (result.Count == 0)
                result.Add(new DraftOption { Kind = DraftKind.Restore, Id = "restore", Title = "Restore", Description = "Heal 30% of max health", Rarity = Rarity.Common });
            return result;
        }

        static bool Excluded(string id, Loadout l, ICollection<string> exclude) => l.Banished.Contains(id) || (exclude != null && exclude.Contains(id));

        static DraftOption Deal(DraftKind kind, string id, Loadout loadout, IReadOnlyList<WeaponDef> weapons, IReadOnlyList<PassiveDef> passives, Rng rng, float luck)
        {
            var o = new DraftOption { Kind = kind, Id = id };
            switch (kind)
            {
                case DraftKind.NewWeapon:
                {
                    var def = weapons.First(w => w.Id == id);
                    o.Rarity = Rarity.Common;
                    o.NextLevel = 1;
                    o.Title = def.Name;
                    o.Description = def.Description;
                    break;
                }
                case DraftKind.NewPassive:
                {
                    var def = passives.First(p => p.Id == id);
                    o.Rarity = Rarity.Common;
                    o.NextLevel = 1;
                    o.PassiveRoll = def.PerLevel;
                    o.Title = def.Name;
                    o.Description = StatText.Describe(def.PerLevel);
                    break;
                }
                case DraftKind.UpgradeWeapon:
                {
                    var w = loadout.Weapon(id);
                    o.Rarity = RollRarity(rng, luck);
                    o.NextLevel = w.Level + 1;
                    int rolls = o.Rarity >= Rarity.Epic ? 2 : 1;
                    var pool = new List<WeaponUpgrade>(w.Def.UpgradePool);
                    for (int i = 0; i < rolls && pool.Count > 0; i++)
                    {
                        int k = rng.Range(0, pool.Count);
                        var up = pool[k];
                        pool.RemoveAt(k);
                        o.WeaponRolls.Add(new WeaponUpgrade(up.Stat, ScaleUpgrade(up, o.Rarity)));
                    }
                    o.Title = $"{w.Def.Name} {o.NextLevel}";
                    o.Description = string.Join(", ", o.WeaponRolls.Select(DescribeUpgrade));
                    break;
                }
                case DraftKind.UpgradePassive:
                {
                    var p = loadout.Passive(id);
                    o.Rarity = RollRarity(rng, luck);
                    o.NextLevel = p.Level + 1;
                    o.PassiveRoll = ScalePassive(p.Def.PerLevel, o.Rarity);
                    o.Title = $"{p.Def.Name} {o.NextLevel}";
                    o.Description = StatText.Describe(o.PassiveRoll);
                    break;
                }
            }
            return o;
        }

        static bool IsWhole(WeaponStat s) => s == WeaponStat.Count || s == WeaponStat.Pierce;

        public static float ScaleUpgrade(WeaponUpgrade up, Rarity r)
        {
            if (IsWhole(up.Stat))
                return Math.Max(1f, (float)Math.Floor(up.Amount * (r >= Rarity.Epic ? 2f : 1f)));
            return up.Amount * Multiplier(r);
        }

        static bool IsWholeStat(StatId s) => s == StatId.ProjectileCount || s == StatId.ExtraJumps;

        public static StatMod ScalePassive(StatMod m, Rarity r)
        {
            if (IsWholeStat(m.Stat)) return m;
            return m.Scaled(Multiplier(r));
        }

        public static string DescribeUpgrade(WeaponUpgrade u)
        {
            switch (u.Stat)
            {
                case WeaponStat.Damage: return $"+{u.Amount:0.#} Damage";
                case WeaponStat.Cooldown: return $"+{u.Amount * 100f:0}% Fire Rate";
                case WeaponStat.Count: return $"+{u.Amount:0} Count";
                case WeaponStat.Size: return $"+{u.Amount * 100f:0}% Size";
                case WeaponStat.Pierce: return $"+{u.Amount:0} Pierce";
                case WeaponStat.Speed: return $"+{u.Amount * 100f:0}% Speed";
                case WeaponStat.Duration: return $"+{u.Amount * 100f:0}% Duration";
                case WeaponStat.Range: return $"+{u.Amount * 100f:0}% Range";
                case WeaponStat.Knockback: return $"+{u.Amount * 100f:0}% Knockback";
                case WeaponStat.CritChance: return $"+{u.Amount * 100f:0}% Crit Chance";
                default: return u.Stat.ToString();
            }
        }

        /// <summary>Takes a card. Returns false if the card no longer fits (slot full, already maxed).</summary>
        public static bool Apply(Loadout loadout, DraftOption o, IReadOnlyList<WeaponDef> weapons, IReadOnlyList<PassiveDef> passives)
        {
            switch (o.Kind)
            {
                case DraftKind.NewWeapon:
                    if (loadout.Weapons.Count >= loadout.WeaponSlots || loadout.Weapon(o.Id) != null) return false;
                    loadout.AddWeapon(weapons.First(w => w.Id == o.Id));
                    return true;
                case DraftKind.NewPassive:
                    if (loadout.Passives.Count >= loadout.PassiveSlots || loadout.Passive(o.Id) != null) return false;
                    loadout.AddPassive(passives.First(p => p.Id == o.Id));
                    return true;
                case DraftKind.UpgradeWeapon:
                {
                    var w = loadout.Weapon(o.Id);
                    if (w == null || w.Level >= loadout.MaxLevel) return false;
                    w.Level++;
                    foreach (var u in o.WeaponRolls) w.AddBonus(u.Stat, u.Amount);
                    return true;
                }
                case DraftKind.UpgradePassive:
                {
                    var p = loadout.Passive(o.Id);
                    if (p == null || p.Level >= loadout.MaxLevel) return false;
                    p.Level++;
                    p.Mods.Add(o.PassiveRoll);
                    return true;
                }
                case DraftKind.Restore:
                    return true;
            }
            return false;
        }

        /// <summary>Removes an item from the rest of the run's drafts.</summary>
        public static bool Banish(Loadout loadout, DraftCharges charges, string id)
        {
            if (charges.Banish <= 0 || string.IsNullOrEmpty(id) || id == "restore") return false;
            charges.Banish--;
            loadout.Banished.Add(id);
            return true;
        }
    }

    public static class XpRules
    {
        /// <summary>XP needed to go from level to level + 1. PLACEHOLDER curve: 5 + 4 * (level - 1)^1.25.</summary>
        public static int Required(int level)
        {
            if (level < 1) level = 1;
            return (int)Math.Round(5.0 + 4.0 * Math.Pow(level - 1, 1.25));
        }

        /// <summary>XP a Skip in the draft grants: a fifth of the current level's requirement.</summary>
        public static float SkipReward(int level) => Required(level) * 0.2f;
    }

    /// <summary>Level and XP within a run.</summary>
    public sealed class XpTracker
    {
        public int Level { get; private set; } = 1;
        public float Xp { get; private set; }
        public int Required => XpRules.Required(Level);
        public float Fraction => Required > 0 ? Xp / Required : 0f;

        /// <summary>Adds XP and returns how many levels were gained.</summary>
        public int Add(float amount)
        {
            if (amount <= 0f) return 0;
            Xp += amount;
            int gained = 0;
            while (Xp >= Required)
            {
                Xp -= Required;
                Level++;
                gained++;
            }
            return gained;
        }
    }
}
