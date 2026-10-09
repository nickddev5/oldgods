using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class MetaRulesTests
    {
        static ContentSet Content()
        {
            var set = new ContentSet();
            for (int i = 0; i < 6; i++) set.Gods.Add(new GodDef { Id = "g" + i, Name = "G" + i, Order = i, Cost = i == 0 ? 0 : 100 });
            set.Gods.Add(new GodDef { Id = LastTest.EliasId, Name = "Elias", Order = 6, Cost = 300, IsLast = true });
            set.Weapons.Add(new WeaponDef { Id = "w.free" });
            set.Weapons.Add(new WeaponDef { Id = "w.locked", UnlockId = "w.locked" });
            set.Items.Add(new ItemDef { Id = "i.locked", UnlockId = "i.locked" });
            return set;
        }

        [Test]
        public void PowerupsCostMoreEachLevelAndCap()
        {
            var save = new SaveData { currency = 10000 };
            var def = MetaCatalog.Powerups[0];
            int spent = 0;
            for (int i = 0; i < def.MaxLevel; i++)
            {
                int cost = MetaRules.PowerupCost(def, i);
                Assert.IsTrue(MetaRules.TryBuyPowerup(save, def));
                spent += cost;
            }
            Assert.IsFalse(MetaRules.TryBuyPowerup(save, def), "capped");
            Assert.AreEqual(10000 - spent, save.currency);
            var stats = StatBlock.Default();
            stats.ApplyAll(MetaRules.PowerupMods(save));
            Assert.AreEqual(100f + def.MaxLevel * def.PerLevel.Add, stats.Value(StatId.MaxHealth), 1e-3f);
        }

        [Test]
        public void PowerupNeedsEnoughEmbers()
        {
            var save = new SaveData { currency = 1 };
            Assert.IsFalse(MetaRules.TryBuyPowerup(save, MetaCatalog.Powerups[0]));
            Assert.AreEqual(1, save.currency);
        }

        [Test]
        public void TreeListsLockedContentAndGods()
        {
            var tree = MetaRules.Tree(Content(), id => 50);
            Assert.AreEqual(6 + 2, tree.Count, "six paid gods, one weapon, one item");
            Assert.IsFalse(tree.Any(e => e.Id == "w.free"));
            Assert.IsTrue(tree.Any(e => e.Kind == UnlockKind.Item && e.Cost == 50));
        }

        [Test]
        public void UnlockingRespectsOrderAndCurrency()
        {
            var content = Content();
            var tree = MetaRules.Tree(content, id => 50);
            var save = new SaveData { currency = 150 };
            var g2 = tree.First(e => e.Id == "g2");
            var g1 = tree.First(e => e.Id == "g1");
            Assert.IsFalse(MetaRules.TryUnlock(save, g2, content.Gods), "out of order");
            Assert.IsTrue(MetaRules.TryUnlock(save, g1, content.Gods));
            Assert.AreEqual(50, save.currency);
            Assert.IsFalse(MetaRules.TryUnlock(save, g2, content.Gods), "too poor");
            Assert.IsFalse(MetaRules.TryUnlock(save, g1, content.Gods), "already owned");
        }

        [Test]
        public void QuestsAccumulateOrTakeTheBestAndPayOnce()
        {
            var save = new SaveData();
            var run = new RunSummary { Kills = 600, StagesCleared = 1, Level = 12 };
            var done = MetaRules.PayRun(save, run, 40);
            Assert.IsTrue(done.Any(q => q.Id == "quest.first_steps"));
            Assert.IsFalse(done.Any(q => q.Id == "quest.slayer"));
            int afterFirst = save.currency;
            Assert.AreEqual(40 + 30, afterFirst);

            done = MetaRules.PayRun(save, new RunSummary { Kills = 500, StagesCleared = 1, Level = 5 }, 0);
            Assert.IsTrue(done.Any(q => q.Id == "quest.slayer"), "1100 kills in total");
            Assert.IsFalse(done.Any(q => q.Id == "quest.first_steps"), "pays once");
            var ascend = MetaCatalog.Quests.First(q => q.Id == "quest.ascendant");
            Assert.AreEqual(12, MetaRules.QuestProgressValue(save, ascend), "best run, not a sum");
        }

        [Test]
        public void WinningAsEliasCompletesTheThrone()
        {
            var save = new SaveData();
            MetaRules.PayRun(save, new RunSummary { Won = true, GodId = "god.storm", ReachedThrone = true }, 0);
            Assert.IsFalse(save.completedQuests.Contains("quest.the_throne"));
            MetaRules.PayRun(save, new RunSummary { Won = true, GodId = LastTest.EliasId, ReachedThrone = true }, 0);
            Assert.IsTrue(save.completedQuests.Contains("quest.the_throne"));
            Assert.AreEqual(2, save.runsWon);
        }

        [Test]
        public void ModifiersAddPayout()
        {
            Assert.AreEqual(0f, MetaRules.PayoutBonus(new string[0]));
            float both = MetaRules.PayoutBonus(new[] { "mod.hardened", "mod.frail", "nonsense" });
            Assert.AreEqual(0.5f, both, 1e-4f);
            var a = RunRewards.Embers(new RunSummary { StagesCleared = 2, BossesKilled = 2 });
            var b = RunRewards.Embers(new RunSummary { StagesCleared = 2, BossesKilled = 2, DifficultyBonus = 0.5f });
            Assert.Greater(b, a);
        }

        [Test]
        public void RewardsGrowWithProgressAndSwarm()
        {
            int early = RunRewards.Embers(new RunSummary { StagesCleared = 0, Kills = 100 });
            int mid = RunRewards.Embers(new RunSummary { StagesCleared = 2, BossesKilled = 2, Kills = 800 });
            int won = RunRewards.Embers(new RunSummary { StagesCleared = 3, BossesKilled = 4, Kills = 2000, ReachedThrone = true, Won = true });
            int swarm = RunRewards.Embers(new RunSummary { StagesCleared = 2, BossesKilled = 2, Kills = 800, BestSwarmSeconds = 90f });
            Assert.Less(early, mid);
            Assert.Less(mid, won);
            Assert.Greater(swarm, mid);
        }

        /// <summary>
        /// The plan's progression check: from a fresh save, winning runs alone pay enough to buy
        /// every god in order and reach Elias in a reasonable number of runs.
        /// </summary>
        [Test]
        public void FreshSaveReachesEliasByPlaying()
        {
            var content = Content();
            content.Gods.Clear();
            int[] costs = { 0, 150, 300, 500, 750, 1000 };
            for (int i = 0; i < 6; i++) content.Gods.Add(new GodDef { Id = "g" + i, Order = i, Cost = costs[i] });
            content.Gods.Add(new GodDef { Id = LastTest.EliasId, Order = 6, Cost = 1500, IsLast = true });
            var save = new SaveData();
            var typicalWin = new RunSummary { StagesCleared = 3, BossesKilled = 4, Kills = 2200, ReachedThrone = true, Won = true, Level = 40, ChestsOpened = 9, ShrinesUsed = 8 };
            int runs = 0;
            var tree = MetaRules.Tree(content, id => 0);
            while (!save.IsUnlocked(LastTest.EliasId) && runs < 100)
            {
                runs++;
                MetaRules.PayRun(save, typicalWin, RunRewards.Embers(typicalWin));
                foreach (var e in tree) MetaRules.TryUnlock(save, e, content.Gods);
            }
            Assert.IsTrue(save.IsUnlocked(LastTest.EliasId));
            Assert.That(runs, Is.InRange(5, 30), $"took {runs} winning runs");
        }
    }
}
