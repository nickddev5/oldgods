using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class GodRulesTests
    {
        static List<GodDef> Gods()
        {
            var list = new List<GodDef>();
            for (int i = 0; i < 6; i++) list.Add(new GodDef { Id = "g" + i, Order = i, Cost = i == 0 ? 0 : 100 * i });
            list.Add(new GodDef { Id = "elias", Order = 6, Cost = 1000, IsLast = true });
            return list;
        }

        [Test]
        public void FirstGodIsFreeOthersAreLocked()
        {
            var gods = Gods();
            var owned = new HashSet<string>();
            Assert.IsTrue(GodRules.IsUnlocked(gods[0], owned.Contains));
            Assert.IsFalse(GodRules.IsUnlocked(gods[1], owned.Contains));
            Assert.AreEqual("g0", GodRules.Default(gods, owned.Contains).Id);
        }

        [Test]
        public void GodsUnlockInOrder()
        {
            var gods = Gods();
            var owned = new HashSet<string>();
            Assert.IsTrue(GodRules.CanUnlock(gods[1], gods, owned.Contains));
            Assert.IsFalse(GodRules.CanUnlock(gods[2], gods, owned.Contains), "g1 first");
            owned.Add("g1");
            Assert.IsTrue(GodRules.CanUnlock(gods[2], gods, owned.Contains));
            Assert.IsFalse(GodRules.CanUnlock(gods[1], gods, owned.Contains), "already owned");
        }

        [Test]
        public void EliasNeedsEveryOtherGod()
        {
            var gods = Gods();
            var owned = new HashSet<string> { "g1", "g2", "g3", "g4" };
            var elias = gods.Single(g => g.IsLast);
            Assert.IsFalse(GodRules.CanUnlock(elias, gods, owned.Contains));
            owned.Add("g5");
            Assert.IsTrue(GodRules.CanUnlock(elias, gods, owned.Contains));
        }

        [Test]
        public void OrderedPutsEliasLast()
        {
            var gods = Gods();
            gods.Reverse();
            var ordered = GodRules.Ordered(gods);
            Assert.AreEqual("g0", ordered[0].Id);
            Assert.AreEqual("elias", ordered.Last().Id);
        }

        [Test]
        public void OnlyEliasTakesTheThrone()
        {
            Assert.IsTrue(LastTest.TakesTheThrone(LastTest.EliasId));
            Assert.IsFalse(LastTest.TakesTheThrone("god.storm"));
            Assert.IsFalse(LastTest.TakesTheThrone(null));
        }
    }
}
