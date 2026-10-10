using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum DraftKind { NewWeapon, NewPassive, UpgradeWeapon, UpgradePassive, Restore, Boon }

    /// <summary>One card in the level-up draft. Upgrades are rolled when the card is dealt, so the card can show them.</summary>
    public sealed class DraftOption
    {
        public DraftKind Kind;
        public string Id;
        public Rarity Rarity;
        public int NextLevel;
        public readonly List<WeaponUpgrade> WeaponRolls = new List<WeaponUpgrade>();
        public StatMod PassiveRoll;
        /// <summary>Gold a Spoils boon pays.</summary>
        public int Gold;
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
        /// <summary>Boons taken this run, by boon id.</summary>
        public readonly Dictionary<string, int> Boons = new Dictionary<string, int>();

        public int BoonCount(string id) => Boons.TryGetValue(id, out int n) ? n : 0;

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

        /// <summary>Every stat change from passives and from stat boons.</summary>
        public IEnumerable<StatMod> PassiveMods()
        {
            foreach (var p in Passives)
                foreach (var m in p.Mods) yield return m;
            foreach (var b in BoonRules.StatBoons)
            {
                int n = BoonCount(b.Id);
                if (n > 0) yield return new StatMod(b.Stat, BoonRules.Total(b, n));
            }
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
        /// When nothing is left to offer, small boons are dealt instead (see BoonRules).
        /// </summary>
        public static List<DraftOption> Roll(Loadout loadout, IReadOnlyList<WeaponDef> weapons, IReadOnlyList<PassiveDef> passives,
            Rng rng, float luck, int count = 3, ICollection<string> exclude = null, int level = 1)
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
            if (result.Count == 0) result.AddRange(BoonRules.Deal(loadout, rng, count, level, exclude));
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
                case DraftKind.Boon:
                    loadout.Boons[o.Id] = loadout.BoonCount(o.Id) + 1;
                    return true;
            }
            return false;
        }

        /// <summary>Removes an item from the rest of the run's drafts.</summary>
        public static bool Banish(Loadout loadout, DraftCharges charges, string id)
        {
            if (charges.Banish <= 0 || string.IsNullOrEmpty(id) || BoonRules.IsBoon(id)) return false;
            charges.Banish--;
            loadout.Banished.Add(id);
            return true;
        }
    }

    /// <summary>A stat boon: each one taken adds less, and all of them together never reach Cap.</summary>
    public sealed class StatBoonDef
    {
        public string Id, Name;
        public StatId Stat;
        /// <summary>The most these boons can ever add, however many are taken.</summary>
        public float Cap;
        /// <summary>How fast the total nears Cap: total = Cap * (1 - 1 / (1 + Rate * taken)).</summary>
        public float Rate;
    }

    /// <summary>
    /// Small rewards dealt once every slot is full and every item maxed, so late level-ups still
    /// count without letting the build run away. PLACEHOLDER names and numbers.
    /// </summary>
    public static class BoonRules
    {
        public const string RestoreId = "restore";
        public const string SpoilsId = "boon.spoils";
        public const float RestoreFraction = 0.3f;

        public static readonly IReadOnlyList<StatBoonDef> StatBoons = new[]
        {
            new StatBoonDef { Id = "boon.strength", Name = "Strength", Stat = StatId.Damage, Cap = 0.4f, Rate = 0.1f },
            new StatBoonDef { Id = "boon.endurance", Name = "Endurance", Stat = StatId.MaxHealth, Cap = 80f, Rate = 0.1f },
            new StatBoonDef { Id = "boon.quickness", Name = "Quickness", Stat = StatId.AttackSpeed, Cap = 0.3f, Rate = 0.1f },
        };

        public static bool IsBoon(string id) => id == RestoreId || (id != null && id.StartsWith("boon.", StringComparison.Ordinal));

        /// <summary>Hyperbolic stacking: what n of this boon add in all. Never reaches Cap.</summary>
        public static float Total(StatBoonDef b, int taken) => taken <= 0 ? 0f : b.Cap * (1f - 1f / (1f + b.Rate * taken));

        /// <summary>Gold a Spoils boon pays at this level.</summary>
        public static int SpoilsGold(int level) => 10 + 2 * Math.Max(1, level);

        /// <summary>
        /// Up to count boons: Restore, Spoils and one stat boon at random, then the other stat
        /// boons if more are asked for. Excluded ids are left out.
        /// </summary>
        public static List<DraftOption> Deal(Loadout loadout, Rng rng, int count, int level, ICollection<string> exclude = null)
        {
            var stats = new List<StatBoonDef>(StatBoons);
            rng.Shuffle(stats);
            var order = new List<DraftOption> { Restore(), Spoils(level) };
            foreach (var b in stats) order.Add(Stat(loadout, b));
            var dealt = order.Where(o => exclude == null || !exclude.Contains(o.Id)).Take(Math.Max(1, count)).ToList();
            if (dealt.Count == 0) dealt.Add(Restore());
            return dealt;
        }

        static DraftOption Restore() => new DraftOption
        {
            Kind = DraftKind.Restore, Id = RestoreId, Rarity = Rarity.Common,
            Title = "Restore", Description = $"Heal {RestoreFraction * 100f:0}% of max health",
        };

        static DraftOption Spoils(int level)
        {
            int gold = SpoilsGold(level);
            return new DraftOption { Kind = DraftKind.Boon, Id = SpoilsId, Rarity = Rarity.Common, Gold = gold, Title = "Spoils", Description = $"+{gold} gold" };
        }

        static DraftOption Stat(Loadout loadout, StatBoonDef b)
        {
            int n = loadout.BoonCount(b.Id);
            float now = Total(b, n), next = Total(b, n + 1);
            var step = new StatMod(b.Stat, next - now);
            return new DraftOption
            {
                Kind = DraftKind.Boon, Id = b.Id, Rarity = Rarity.Common, PassiveRoll = step,
                Title = b.Name, Description = $"{StatText.Describe(step)} ({StatText.Describe(new StatMod(b.Stat, next))} in all)",
            };
        }
    }

    public static class XpRules
    {
        /// <summary>
        /// XP needed to go from level to level + 1. PLACEHOLDER curve:
        /// (5 + 4 * (level - 1)^1.25) * (1 + (level / 50)^6). The second factor stays near 1 for
        /// the first 30 levels and steepens after 45, so the 64-card draft fills around the
        /// Drowned Coast rather than in the middle of the Ash Wood.
        /// </summary>
        public static int Required(int level)
        {
            if (level < 1) level = 1;
            return (int)Math.Round((5.0 + 4.0 * Math.Pow(level - 1, 1.25)) * (1.0 + Math.Pow(level / 50.0, 6)));
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
