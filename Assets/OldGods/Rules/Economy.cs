using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    /// <summary>A found item: stat changes that stack with every copy. No slots.</summary>
    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public Rarity Rarity;
        public List<StatMod> Mods = new List<StatMod>();
        /// <summary>Named runtime effect for items that are more than stats (e.g. "heal_on_kill"); null for none.</summary>
        public string Special;
        public float SpecialValue;
        public string UnlockId;
    }

    /// <summary>Items owned this run, with counts.</summary>
    public sealed class Inventory
    {
        readonly List<(ItemDef item, int count)> items = new List<(ItemDef, int)>();
        public string LastAdded { get; private set; }

        public IReadOnlyList<(ItemDef item, int count)> Items => items;

        public int Count(string id) => items.FirstOrDefault(x => x.item.Id == id).count;
        public int Total => items.Sum(x => x.count);

        public void Add(ItemDef item, int n = 1)
        {
            int i = items.FindIndex(x => x.item.Id == item.Id);
            if (i >= 0) items[i] = (item, items[i].count + n);
            else items.Add((item, n));
            LastAdded = item.Id;
        }

        public IEnumerable<StatMod> Mods()
        {
            foreach (var (item, count) in items)
                foreach (var m in item.Mods) yield return m.Scaled(count);
        }

        /// <summary>Sum of SpecialValue over copies of items with this special.</summary>
        public float Special(string name)
        {
            float v = 0f;
            foreach (var (item, count) in items)
                if (item.Special == name) v += item.SpecialValue * count;
            return v;
        }

        public ItemDef Find(string id) => items.FirstOrDefault(x => x.item.Id == id).item;
    }

    /// <summary>Gold, chests, the merchant and the duplicator. PLACEHOLDER numbers.</summary>
    public static class EconomyRules
    {
        /// <summary>Price of the next chest after n paid chests this run.</summary>
        public static int ChestPrice(int paidChestsOpened, int stageIndex) =>
            (int)Math.Round((12.0 + 12.0 * Math.Pow(paidChestsOpened, 1.35)) * (1.0 + stageIndex * 0.25));

        public static int MerchantPrice(Rarity rarity, int paidChestsOpened, int stageIndex) =>
            (int)Math.Round(ChestPrice(paidChestsOpened, stageIndex) * (1.2 + (int)rarity * 0.45));

        public static int DuplicatorPrice(int paidChestsOpened, int stageIndex) => ChestPrice(paidChestsOpened, stageIndex);

        /// <summary>Gold dropped by one enemy, before Gold Gain.</summary>
        public static int GoldDrop(bool elite, int stageIndex) => (elite ? 12 : 1) * (1 + stageIndex);

        public static int ApplyGoldGain(float amount, float goldGain) => Math.Max(1, (int)Math.Round(amount * goldGain));

        /// <summary>Chance that a common enemy drops a healing morsel or a magnet.</summary>
        public const float HealDropChance = 0.006f;
        public const float MagnetDropChance = 0.0015f;

        static readonly float[] ItemRarityWeights = { 55f, 28f, 12f, 4f, 1f };

        public static Rarity RollItemRarity(Rng rng, float luck, Rarity minimum = Rarity.Common)
        {
            var w = new float[ItemRarityWeights.Length];
            float k = 1f + Math.Max(0f, luck);
            for (int i = 0; i < w.Length; i++) w[i] = i < (int)minimum ? 0f : ItemRarityWeights[i] * (float)Math.Pow(k, i * 1.5);
            return (Rarity)rng.PickWeighted(w);
        }

        /// <summary>Rolls a rarity, then an item of that rarity (falling back to nearer rarities if none exist).</summary>
        public static ItemDef RollItem(IReadOnlyList<ItemDef> pool, Rng rng, float luck, Rarity minimum = Rarity.Common)
        {
            if (pool.Count == 0) return null;
            var r = RollItemRarity(rng, luck, minimum);
            for (int step = 0; step < 5; step++)
            {
                foreach (int d in step == 0 ? new[] { 0 } : new[] { -step, step })
                {
                    int target = (int)r + d;
                    if (target < (int)minimum || target > (int)Rarity.Legendary) continue;
                    var of = pool.Where(i => (int)i.Rarity == target).ToList();
                    if (of.Count > 0) return of[rng.Range(0, of.Count)];
                }
            }
            return pool[rng.Range(0, pool.Count)];
        }
    }

    /// <summary>Gold held during a run. Never banked between runs.</summary>
    public sealed class Wallet
    {
        public int Gold { get; private set; }
        public int Earned { get; private set; }
        public int Spent { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            Earned += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            Spent += amount;
            return true;
        }
    }

    public enum ShrineKind { Charge, Item, Greed, BossCurse, Challenge, Magnet }

    public static class ShrineRules
    {
        /// <summary>Which shrine the nth shrine on a map is: every kind once, then Charge shrines.</summary>
        public static ShrineKind KindFor(int index)
        {
            var order = new[] { ShrineKind.Charge, ShrineKind.Item, ShrineKind.Greed, ShrineKind.BossCurse, ShrineKind.Challenge, ShrineKind.Magnet };
            return index < order.Length ? order[index] : ShrineKind.Charge;
        }

        public const float ChargeSeconds = 5f;
        public const float ChargeDecayPerSecond = 0.5f;

        /// <summary>Charge after dt: fills while standing inside, drains slowly outside. Returns 0..1.</summary>
        public static float StepCharge(float charge, bool inside, float dt) =>
            Math.Max(0f, Math.Min(1f, inside ? charge + dt / ChargeSeconds : charge - dt * ChargeDecayPerSecond / ChargeSeconds));

        static readonly StatMod[] ChargeOffers =
        {
            new StatMod(StatId.MaxHealth, 20f), new StatMod(StatId.Damage, 0.08f), new StatMod(StatId.AttackSpeed, 0.07f),
            new StatMod(StatId.MoveSpeed, 0.06f), new StatMod(StatId.Area, 0.08f), new StatMod(StatId.Armor, 1f),
            new StatMod(StatId.Regen, 0.3f), new StatMod(StatId.Luck, 0.06f), new StatMod(StatId.CritChance, 0.04f),
            new StatMod(StatId.PickupRange, 1f), new StatMod(StatId.XpGain, 0.06f), new StatMod(StatId.Duration, 0.08f),
        };

        /// <summary>Three different stat boosts for a charged shrine, each with a rolled rarity.</summary>
        public static List<(StatMod mod, Rarity rarity)> ChargeChoices(Rng rng, float luck)
        {
            var pool = ChargeOffers.ToList();
            rng.Shuffle(pool);
            var result = new List<(StatMod, Rarity)>();
            for (int i = 0; i < 3; i++)
            {
                var r = DraftRules.RollRarity(rng, luck);
                result.Add((DraftRules.ScalePassive(pool[i], r), r));
            }
            return result;
        }

        /// <summary>Greed: each use makes the stage harder and gold richer.</summary>
        public const float GreedDifficulty = 0.15f;
        public const float GreedGold = 0.25f;

        /// <summary>Item shrine: the health it costs, as a fraction of max health.</summary>
        public const float ItemShrineHealthCost = 0.2f;

        /// <summary>Boss Curse: each one adds another boss and another chest when the boss falls.</summary>
        public static int BossesForCurses(int curses) => 1 + curses;
    }
}
