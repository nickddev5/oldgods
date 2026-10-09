using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class EconomyTests
    {
        static List<ItemDef> Pool() => new List<ItemDef>
        {
            new ItemDef { Id = "c1", Rarity = Rarity.Common, Mods = { new StatMod(StatId.Damage, 0.1f) } },
            new ItemDef { Id = "c2", Rarity = Rarity.Common },
            new ItemDef { Id = "u1", Rarity = Rarity.Uncommon },
            new ItemDef { Id = "r1", Rarity = Rarity.Rare },
            new ItemDef { Id = "l1", Rarity = Rarity.Legendary, Special = "heal_on_kill", SpecialValue = 0.1f },
        };

        [Test]
        public void ChestPriceRisesWithChestsAndStage()
        {
            Assert.AreEqual(15, EconomyRules.ChestPrice(0, 0));
            for (int n = 0; n < 30; n++) Assert.Greater(EconomyRules.ChestPrice(n + 1, 0), EconomyRules.ChestPrice(n, 0));
            Assert.Greater(EconomyRules.ChestPrice(3, 2), EconomyRules.ChestPrice(3, 0));
            Assert.Greater(EconomyRules.MerchantPrice(Rarity.Rare, 2, 0), EconomyRules.MerchantPrice(Rarity.Common, 2, 0));
        }

        [Test]
        public void WalletNeverGoesNegative()
        {
            var w = new Wallet();
            w.Add(10);
            Assert.IsFalse(w.TrySpend(11));
            Assert.IsTrue(w.TrySpend(10));
            Assert.AreEqual(0, w.Gold);
            Assert.AreEqual(10, w.Earned);
            Assert.AreEqual(10, w.Spent);
            w.Add(-5);
            Assert.AreEqual(0, w.Gold);
        }

        [Test]
        public void GoldGainRoundsAndNeverDropsToZero()
        {
            Assert.AreEqual(1, EconomyRules.ApplyGoldGain(1f, 0.1f));
            Assert.AreEqual(15, EconomyRules.ApplyGoldGain(12f, 1.25f));
            Assert.Greater(EconomyRules.GoldDrop(true, 0), EconomyRules.GoldDrop(false, 0));
        }

        [Test]
        public void ItemRollRespectsMinimumAndFallsBack()
        {
            var rng = new Rng(4);
            var pool = Pool();
            for (int i = 0; i < 300; i++)
            {
                var item = EconomyRules.RollItem(pool, rng, 0f, Rarity.Rare);
                Assert.GreaterOrEqual((int)item.Rarity, (int)Rarity.Rare);
            }
            // No Epic items exist; an Epic roll falls back to a neighbouring rarity.
            var onlyCommon = new List<ItemDef> { new ItemDef { Id = "c", Rarity = Rarity.Common } };
            Assert.AreEqual("c", EconomyRules.RollItem(onlyCommon, rng, 5f).Id);
        }

        [Test]
        public void InventoryStacksModsAndSpecials()
        {
            var inv = new Inventory();
            var pool = Pool();
            inv.Add(pool[0]);
            inv.Add(pool[0]);
            inv.Add(pool[4]);
            Assert.AreEqual(2, inv.Count("c1"));
            Assert.AreEqual(3, inv.Total);
            var stats = StatBlock.Default();
            stats.ApplyAll(inv.Mods());
            Assert.AreEqual(1.2f, stats.Value(StatId.Damage), 1e-4f);
            Assert.AreEqual(0.1f, inv.Special("heal_on_kill"), 1e-4f);
            Assert.AreEqual("l1", inv.LastAdded);
        }
    }

    public class ShrineTests
    {
        [Test]
        public void EveryShrineKindAppearsOnceFirst()
        {
            var kinds = Enumerable.Range(0, 6).Select(ShrineRules.KindFor).ToList();
            CollectionAssert.AllItemsAreUnique(kinds);
            Assert.AreEqual(ShrineKind.Charge, ShrineRules.KindFor(9));
        }

        [Test]
        public void ChargeFillsInsideAndDrainsOutside()
        {
            float c = 0f;
            for (int i = 0; i < 60; i++) c = ShrineRules.StepCharge(c, true, 0.05f);
            Assert.AreEqual(0.6f, c, 1e-3f);
            float drained = ShrineRules.StepCharge(c, false, 1f);
            Assert.Less(drained, c);
            Assert.Greater(drained, 0f);
            for (int i = 0; i < 200; i++) c = ShrineRules.StepCharge(c, true, 0.05f);
            Assert.AreEqual(1f, c);
        }

        [Test]
        public void ChargeChoicesAreThreeDifferentStats()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var choices = ShrineRules.ChargeChoices(new Rng((ulong)seed), 0f);
                Assert.AreEqual(3, choices.Count);
                Assert.AreEqual(3, choices.Select(c => c.mod.Stat).Distinct().Count());
            }
        }

        [Test]
        public void CursesAddBosses()
        {
            Assert.AreEqual(1, ShrineRules.BossesForCurses(0));
            Assert.AreEqual(3, ShrineRules.BossesForCurses(2));
        }
    }
}
