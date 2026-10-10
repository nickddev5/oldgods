using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class TargetingTests
    {
        [Test]
        public void ChooseTakesTheBossInRange()
        {
            Assert.AreEqual(7, Targeting.Choose(3, 7));
            Assert.AreEqual(3, Targeting.Choose(3, -1));
            Assert.AreEqual(-1, Targeting.Choose(-1, -1));
        }

        [Test]
        public void BossFirstMovesAListedBossToTheFront()
        {
            var list = new[] { 4, 5, 9, 6 };
            Assert.AreEqual(4, Targeting.BossFirst(list, 4, 9));
            CollectionAssert.AreEqual(new[] { 9, 4, 5, 6 }, list);
        }

        [Test]
        public void BossFirstInsertsAnUnlistedBoss()
        {
            var full = new[] { 1, 2, 3 };
            Assert.AreEqual(3, Targeting.BossFirst(full, 3, 9), "a full list drops its farthest");
            CollectionAssert.AreEqual(new[] { 9, 1, 2 }, full);

            var room = new[] { 1, 2, 0, 0 };
            Assert.AreEqual(3, Targeting.BossFirst(room, 2, 9));
            CollectionAssert.AreEqual(new[] { 9, 1, 2 }, new[] { room[0], room[1], room[2] });

            var empty = new int[2];
            Assert.AreEqual(1, Targeting.BossFirst(empty, 0, 9), "a boss alone is still a target");
            Assert.AreEqual(9, empty[0]);
        }

        [Test]
        public void NoBossLeavesTheListAlone()
        {
            var list = new[] { 1, 2, 3 };
            Assert.AreEqual(2, Targeting.BossFirst(list, 2, -1));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
        }
    }
}
