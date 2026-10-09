using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class GaitTests
    {
        [Test]
        public void StandingStillDoesNotStep()
        {
            Assert.AreEqual(0f, Gait.CyclesPerSecond(0f, 0.86f));
            Assert.AreEqual(0f, Gait.PhaseStep(0f, 0.86f, 0.1f));
        }

        [Test]
        public void PlayerRunStepsAtAHumanCadence()
        {
            // The player runs at 8 m/s with 0.86 m legs: a sprint is about 4 to 5 steps a second.
            float steps = Gait.CyclesPerSecond(8f, 0.86f) * 2f;
            Assert.That(steps, Is.InRange(3.5f, 5f));
        }

        [Test]
        public void WalkingHumanStepsAboutTwiceASecond()
        {
            float steps = Gait.CyclesPerSecond(1.4f, 0.9f) * 2f;
            Assert.That(steps, Is.InRange(1.5f, 2.2f));
        }

        [Test]
        public void LongerLegsStepMoreSlowlyAndFurther()
        {
            Assert.Less(Gait.CyclesPerSecond(4f, 2f), Gait.CyclesPerSecond(4f, 0.8f));
            Assert.Greater(Gait.StrideLength(4f, 2f), Gait.StrideLength(4f, 0.8f));
        }

        [Test]
        public void FasterMeansLongerStridesNotOnlyQuickerOnes()
        {
            Assert.Greater(Gait.StrideLength(8f, 0.86f), Gait.StrideLength(2f, 0.86f));
            Assert.Greater(Gait.CyclesPerSecond(8f, 0.86f), Gait.CyclesPerSecond(2f, 0.86f));
        }

        [Test]
        public void PhaseCoversOneCycleEveryStride()
        {
            float stride = Gait.StrideLength(5f, 0.8f);
            float dt = stride / 5f;
            Assert.AreEqual(System.Math.PI * 2.0, Gait.PhaseStep(5f, 0.8f, dt), 1e-4);
        }

        [Test]
        public void RunningSpendsLessOfTheCycleOnTheGround()
        {
            Assert.AreEqual(Gait.WalkDuty, Gait.DutyFactor(0.5f, 0.86f), 1e-5);
            Assert.AreEqual(Gait.RunDuty, Gait.DutyFactor(8f, 0.86f), 1e-5);
        }

        [Test]
        public void SwingGrowsWithSpeedWithinItsClamp()
        {
            float walk = Gait.Swing(1.5f, 0.86f, 0.02f, 1f);
            float run = Gait.Swing(6f, 0.86f, 0.02f, 1f);
            Assert.Greater(run, walk);
            Assert.AreEqual(0.02f, Gait.Swing(0f, 0.86f, 0.02f, 0.3f), 1e-6);
            Assert.AreEqual(0.3f, Gait.Swing(30f, 0.86f, 0.02f, 0.3f), 1e-6);
        }

        [Test]
        public void SwingKeepsThePlantedFootWithTheGround()
        {
            // Foot sweep while planted (2 L sin hip) equals ground covered while planted.
            float speed = 3f, leg = 0.86f;
            float swing = Gait.Swing(speed, leg, 0f, 10f);
            float sweep = 2f * leg * (float)System.Math.Sin(swing * Gait.HipPerSwing);
            float covered = Gait.StrideLength(speed, leg) * Gait.DutyFactor(speed, leg);
            Assert.AreEqual(covered, sweep, 1e-3);
        }
    }
}
