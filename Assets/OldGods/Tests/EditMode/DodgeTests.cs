using System;
using System.Collections.Generic;
using NUnit.Framework;
using OldGods.Rules;
using OldGods.Runtime;

namespace OldGods.Tests.EditMode
{
    public class DodgeTests
    {
        [Test]
        public void EveryGodHasItsOwnSlide()
        {
            var seen = new HashSet<DodgeStyle>();
            foreach (GodLook look in Enum.GetValues(typeof(GodLook)))
            {
                var style = GodModels.Dodge(look);
                Assert.AreNotEqual(DodgeStyle.Slide, style, look.ToString());
                Assert.IsTrue(seen.Add(style), $"{look} shares a slide");
            }
        }

        [Test]
        public void StormRidesACloudAboveTheGround()
        {
            var p = Dodges.Pose(DodgeStyle.StormCloud);
            Assert.AreEqual(DodgeMount.Cloud, p.Mount);
            Assert.Greater(p.Height, 0.2f);
        }

        [Test]
        public void BeastDropsToAllFoursAndBounds()
        {
            var p = Dodges.Pose(DodgeStyle.BeastBound);
            // The chest comes down near level, the arms reach for the ground and the stride gallops.
            Assert.Greater(p.Waist, 1.2f);
            Assert.Less(p.ShoulderLeft, -1.1f);
            Assert.Less(p.ShoulderRight, -1.1f);
            Assert.Greater(p.Gallop, 0f);
            Assert.Less(p.Height, 0f);
        }

        [Test]
        public void EarthSinksIntoTheGroundAndTheOthersRiseOrLower()
        {
            Assert.Less(Dodges.Pose(DodgeStyle.EarthBurrow).Height, -0.6f);
            Assert.AreEqual(DodgeMount.Wave, Dodges.Pose(DodgeStyle.TideWave).Mount);
            Assert.AreEqual(DodgeMount.Halo, Dodges.Pose(DodgeStyle.EliasGlide).Mount);
        }

        [Test]
        public void EveryGodLeavesATrailExceptThePlainSlide()
        {
            Assert.AreEqual(DodgeTrail.None, Dodges.Pose(DodgeStyle.Slide).Trail);
            foreach (DodgeStyle style in Enum.GetValues(typeof(DodgeStyle)))
                if (style != DodgeStyle.Slide) Assert.AreNotEqual(DodgeTrail.None, Dodges.Pose(style).Trail, style.ToString());
        }

        [Test]
        public void TheSlidingMotionDoesNotDependOnTheStyle()
        {
            // The pose has no say in speed, length or hitbox: those come from MotorTuning alone.
            foreach (var f in typeof(DodgePose).GetFields())
                StringAssert.DoesNotContain("Speed", f.Name);
            var t = new MotorTuning();
            Assert.AreEqual(t.SlideMaxSpeed, PlayerRules.StepSlide(t.SlideMaxSpeed + 5f, 1f, t, 0.1f));
        }

        [Test]
        public void ACapeOnABackBentLevelLiesAlongTheBack()
        {
            // The shoulders bent 90 degrees forward: world up is the frame's -z, so gravity's
            // difference from the frame's own down is g * (0, -1, -1). Moving forward is along +y.
            var t = CapeTuning.Default;
            var s = new CapeState();
            for (int i = 0; i < 360; i++)
                CapeSim.Step(ref s, t, 0f, -CapeSim.Gravity, -CapeSim.Gravity, 0f, 10f, 0f, 1f / 60f);
            Assert.AreEqual(-t.MaxForward, s.Pitch, 0.02f);
        }
    }
}
