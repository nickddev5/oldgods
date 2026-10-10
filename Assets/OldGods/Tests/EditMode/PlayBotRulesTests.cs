using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class PlayBotRulesTests
    {
        static DraftOption Card(DraftKind kind, string id, Rarity rarity = Rarity.Common) => new DraftOption { Kind = kind, Id = id, Rarity = rarity };

        [Test]
        public void SmartPicksANewWeaponWhileSlotsAreFree()
        {
            var loadout = new Loadout();
            loadout.AddWeapon(new WeaponDef { Id = "w.start" });
            var cards = new List<DraftOption> { Card(DraftKind.NewPassive, "p.a"), Card(DraftKind.NewWeapon, "w.b"), Card(DraftKind.UpgradePassive, "p.c") };
            for (ulong seed = 1; seed < 20; seed++)
                Assert.AreEqual(1, PlayBotRules.PickDraft(cards, loadout, 1f, BotPicks.Smart, new Rng(seed)));
        }

        [Test]
        public void SmartTakesRestoreOnlyWhenHurt()
        {
            var loadout = new Loadout();
            var cards = new List<DraftOption> { Card(DraftKind.UpgradeWeapon, "w.a"), Card(DraftKind.Restore, "restore") };
            Assert.AreEqual(1, PlayBotRules.PickDraft(cards, loadout, 0.3f, BotPicks.Smart, new Rng(5)));
            Assert.AreEqual(0, PlayBotRules.PickDraft(cards, loadout, 0.9f, BotPicks.Smart, new Rng(5)));
        }

        [Test]
        public void SeedsVaryCloseChoicesButFirstIsAlwaysFirst()
        {
            var loadout = new Loadout();
            var cards = new List<DraftOption> { Card(DraftKind.UpgradeWeapon, "w.a"), Card(DraftKind.UpgradeWeapon, "w.b", Rarity.Uncommon) };
            var picks = new HashSet<int>();
            for (ulong seed = 1; seed < 40; seed++)
            {
                picks.Add(PlayBotRules.PickDraft(cards, loadout, 1f, BotPicks.Smart, new Rng(seed)));
                Assert.AreEqual(0, PlayBotRules.PickDraft(cards, loadout, 1f, BotPicks.First, new Rng(seed)));
            }
            Assert.AreEqual(2, picks.Count);
        }

        [Test]
        public void ParsePicksDefaultsToSmart()
        {
            Assert.AreEqual(BotPicks.Random, PlayBotRules.ParsePicks("random"));
            Assert.AreEqual(BotPicks.First, PlayBotRules.ParsePicks("FIRST"));
            Assert.AreEqual(BotPicks.Smart, PlayBotRules.ParsePicks(null));
            Assert.AreEqual(BotPicks.Smart, PlayBotRules.ParsePicks("nonsense"));
        }

        static (List<UnlockEntry> tree, List<GodDef> gods) Tree()
        {
            var set = new ContentSet();
            set.Gods.Add(new GodDef { Id = "g0", Name = "G0", Order = 0, Cost = 0 });
            set.Gods.Add(new GodDef { Id = "g1", Name = "G1", Order = 1, Cost = 100 });
            set.Gods.Add(new GodDef { Id = "g2", Name = "G2", Order = 2, Cost = 200 });
            set.Weapons.Add(new WeaponDef { Id = "w.locked", Name = "Locked", UnlockId = "w.locked" });
            return (MetaRules.Tree(set, _ => 25), set.Gods);
        }

        [Test]
        public void SpendBuysTheNextGodFirst()
        {
            var (tree, gods) = Tree();
            var save = new SaveData { currency = 130 };
            var bought = PlayBotRules.Spend(save, tree, gods);
            Assert.AreEqual("G1", bought[0].Name);
            Assert.IsTrue(save.IsUnlocked("g1"));
            // 30 left, saving for G2 (200): nothing else is bought.
            Assert.AreEqual(1, bought.Count);
            Assert.AreEqual(30, save.currency);
        }

        [Test]
        public void SpendUsesOnlyWhatIsAboveTheNextGodsPrice()
        {
            var (tree, gods) = Tree();
            var save = new SaveData { currency = 160 };
            save.Unlock("g1");
            Assert.IsEmpty(PlayBotRules.Spend(save, tree, gods));
            save.currency = 260;
            var bought = PlayBotRules.Spend(save, tree, gods);
            Assert.AreEqual("G2", bought[0].Name);
            Assert.That(bought.Skip(1).All(p => p.Unlock == null || p.Unlock.Kind != UnlockKind.God));
            Assert.Greater(bought.Count, 1);
            Assert.GreaterOrEqual(save.currency, 0);
        }

        [Test]
        public void StuckWatchStallsThenReportsStuckOnce()
        {
            var w = new StuckWatch { Window = 0.5f, StuckAfter = 3 };
            int stalls = 0, stucks = 0, detours = 0;
            for (int i = 0; i < 200; i++)
            {
                w.Step(10f, 10f, true, 0.05f);
                if (w.JustStalled) stalls++;
                if (w.FirstStuck) stucks++;
                if (w.JustStuck) detours++;
            }
            Assert.AreEqual(1, stalls);
            Assert.AreEqual(1, stucks);
            Assert.Greater(detours, 1);
        }

        [Test]
        public void StuckWatchIgnoresStandingStillOnPurposeAndMoving()
        {
            var w = new StuckWatch { Window = 0.5f, StuckAfter = 2 };
            for (int i = 0; i < 100; i++)
            {
                w.Step(0f, 0f, false, 0.05f);
                Assert.IsFalse(w.JustStalled);
            }
            for (int i = 0; i < 100; i++)
            {
                w.Step(i * 0.5f, 0f, true, 0.05f);
                Assert.IsFalse(w.JustStalled);
            }
        }
    }
}
