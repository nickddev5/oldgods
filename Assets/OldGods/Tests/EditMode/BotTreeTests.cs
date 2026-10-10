using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class BotTreeTests
    {
        static BotSituation Calm() => new BotSituation { Health = 1f, StageClock = 0.2f, BossAt = 0.7f, CloseReach = 12f, GateUsable = true };

        static BotGoal Goal(BotSituation s, out string path) => BotTrees.Goal.Decide(s, out path);

        [Test]
        public void ExploresWhenNothingElseCalls()
        {
            Assert.AreEqual(BotGoal.Explore, Goal(Calm(), out string path));
            StringAssert.Contains("explore", path);
        }

        [Test]
        public void TheLastTestAndAWakeBossComeFirst()
        {
            var s = Calm();
            s.BossAwake = true;
            s.Health = 0.1f;
            Assert.AreEqual(BotGoal.Boss, Goal(s, out _));
            s.FinalArena = true;
            Assert.AreEqual(BotGoal.Arena, Goal(s, out _));
        }

        [Test]
        public void AfterTheBossTheChestThenThePortal()
        {
            var s = Calm();
            s.BossDown = true;
            s.FreeChestNear = true;
            s.PortalOpen = true;
            Assert.AreEqual(BotGoal.Feature, Goal(s, out _));
            s.FreeChestNear = false;
            Assert.AreEqual(BotGoal.Portal, Goal(s, out string path));
            Assert.AreEqual("Portal open yes: take the portal", path);
        }

        [Test]
        public void WakesTheBossOnTimeUnlessHurt()
        {
            var s = Calm();
            s.StageClock = 0.75f;
            Assert.AreEqual(BotGoal.Gate, Goal(s, out _));
            s.Health = 0.4f;
            Assert.AreNotEqual(BotGoal.Gate, Goal(s, out _));
            s.StageClock = 0.95f; // the final swarm is close: go anyway
            Assert.AreEqual(BotGoal.Gate, Goal(s, out _));
            s.Health = 0.2f;
            Assert.AreEqual(BotGoal.Recover, Goal(s, out _));
        }

        [Test]
        public void FeaturesBeforeGemsAndChargeShrinesByName()
        {
            var s = Calm();
            s.GemNear = true;
            Assert.AreEqual(BotGoal.Gem, Goal(s, out _));
            s.Threatened = true;
            Assert.AreEqual(BotGoal.Explore, Goal(s, out _));
            s.FeatureWanted = true;
            Assert.AreEqual(BotGoal.Feature, Goal(s, out _));
            s.FeatureIsCharge = true;
            Assert.AreEqual(BotGoal.Charge, Goal(s, out _));
        }

        [Test]
        public void CloseRangeKitsStayCloseUntilHurtOrSwarmed()
        {
            var s = Calm();
            Assert.AreEqual(BotStance.Kite, BotTrees.Stance.Decide(s, out _));
            s.CloseReach = 2.6f; // Rending Claws
            Assert.AreEqual(BotStance.Close, BotTrees.Stance.Decide(s, out string path));
            Assert.AreEqual("Hurt or swarmed no: stay close", path);
            s.Touching = BotTrees.Swarmed;
            Assert.AreEqual(BotStance.Kite, BotTrees.Stance.Decide(s, out _));
            s.Touching = 0;
            s.Health = 0.3f;
            Assert.AreEqual(BotStance.Kite, BotTrees.Stance.Decide(s, out _));
        }

        [Test]
        public void ReachFollowsTheWeaponShape()
        {
            var e = new EffectiveWeapon { Size = 2.6f, Range = 140f };
            Assert.AreEqual(2.6f, BotTrees.Reach(WeaponShape.Swipe, e), 1e-4f);
            Assert.AreEqual(2.6f, BotTrees.Reach(WeaponShape.Aura, e), 1e-4f);
            Assert.AreEqual(12f, BotTrees.Reach(WeaponShape.Projectile, new EffectiveWeapon { Range = 12f }), 1e-4f);
            Assert.AreEqual(3f, BotTrees.Reach(WeaponShape.Area, new EffectiveWeapon { Size = 2f }), 1e-4f);
            Assert.Less(BotTrees.HoldDistance(2.6f), 2.6f);
            Assert.GreaterOrEqual(BotTrees.HoldDistance(0.5f), 1.2f);
        }

        [Test]
        public void EveryLeafIsNamed()
        {
            var leaves = BotTrees.Goal.Leaves();
            CollectionAssert.Contains(leaves, "wake the boss");
            CollectionAssert.Contains(leaves, "recover");
            Assert.That(leaves, Has.None.Null.And.None.Empty);
            CollectionAssert.AreEqual(new[] { "back off", "stay close", "kite" }, BotTrees.Stance.Leaves());
        }
    }
}
