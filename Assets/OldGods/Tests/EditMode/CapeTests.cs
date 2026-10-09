using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class CapeTests
    {
        static CapeState Run(CapeState s, CapeTuning t, float seconds, float ax = 0f, float az = 0f, float vx = 0f, float vz = 0f, float ay = 0f, float vy = 0f)
        {
            for (float time = 0f; time < seconds; time += 1f / 60f)
                CapeSim.Step(ref s, t, ax, ay, az, vx, vy, vz, 1f / 60f);
            return s;
        }

        [Test]
        public void AtRestTheCapeHangsStraightDown()
        {
            var s = Run(new CapeState { Pitch = 0.6f, Roll = -0.4f }, CapeTuning.Default, 6f);
            Assert.AreEqual(0f, s.Pitch, 0.01f);
            Assert.AreEqual(0f, s.Roll, 0.01f);
        }

        [Test]
        public void RunningForwardLiftsTheCapeBehindToTheSteadyAngle()
        {
            var t = CapeTuning.Default;
            var s = Run(default, t, 6f, vz: 8f);
            Assert.Greater(s.Pitch, 0.5f);
            Assert.AreEqual(CapeSim.SteadyPitch(t, 8f), s.Pitch, 0.02f);
            Assert.AreEqual(0f, s.Roll, 0.001f);
        }

        [Test]
        public void FasterRunsLiftItHigherButNeverPastTheLimit()
        {
            var t = CapeTuning.Default;
            float walk = Run(default, t, 6f, vz: 3f).Pitch;
            float run = Run(default, t, 6f, vz: 7f).Pitch;
            float dash = Run(default, t, 6f, vz: 40f).Pitch;
            Assert.Greater(run, walk);
            Assert.LessOrEqual(dash, t.MaxBack + 1e-4f);
        }

        [Test]
        public void SettingOffSwingsItBackFirstAndStoppingSwingsItForward()
        {
            var t = CapeTuning.Default;
            // Speeding up at 20 m/s^2 for a moment, from standing.
            var start = Run(default, t, 0.15f, az: 20f, vz: 1.5f);
            Assert.Greater(start.Pitch, 0.05f);
            // Braking hard from a run: the cape swings past straight down toward the legs, but stops there.
            var running = Run(default, t, 6f, vz: 7f);
            var stop = Run(running, t, 0.6f, az: -12f);
            Assert.Less(stop.Pitch, running.Pitch);
            var stopped = Run(running, t, 0.6f, az: -40f);
            Assert.GreaterOrEqual(stopped.Pitch, -t.MaxForward - 1e-4f);
        }

        [Test]
        public void MovingSidewaysSwingsItTheOtherWay()
        {
            var right = Run(default, CapeTuning.Default, 4f, vx: 5f);
            var left = Run(default, CapeTuning.Default, 4f, vx: -5f);
            Assert.Less(right.Roll, -0.2f);
            Assert.Greater(left.Roll, 0.2f);
        }

        [Test]
        public void FallingLetsTheCapeFloatHigher()
        {
            var t = CapeTuning.Default;
            var ground = Run(default, t, 4f, vz: 5f);
            var falling = Run(ground, t, 0.5f, vz: 5f, ay: -CapeSim.Gravity, vy: -4f);
            Assert.Greater(falling.Pitch, ground.Pitch + 0.1f);
        }

        [Test]
        public void TheSwingIsTheSameAtAnyFrameRate()
        {
            var t = CapeTuning.Default;
            var fast = new CapeState();
            var slow = new CapeState();
            for (int i = 0; i < 240; i++) CapeSim.Step(ref fast, t, 0f, 0f, 3f, 0f, 0f, 4f, 1f / 240f);
            for (int i = 0; i < 30; i++) CapeSim.Step(ref slow, t, 0f, 0f, 3f, 0f, 0f, 4f, 1f / 30f);
            Assert.AreEqual(fast.Pitch, slow.Pitch, 0.03f);
        }

        [Test]
        public void ALongHitchStaysStable()
        {
            var s = new CapeState();
            CapeSim.Step(ref s, CapeTuning.Default, 300f, 0f, 300f, 20f, 0f, 20f, 2f);
            Assert.IsFalse(float.IsNaN(s.Pitch) || float.IsNaN(s.Roll));
            Assert.LessOrEqual(System.Math.Abs(s.Roll), CapeTuning.Default.MaxSide + 1e-4f);
        }

        [Test]
        public void FlutterGrowsWithSpeed()
        {
            Assert.AreEqual(0f, CapeSim.Flutter(0f));
            Assert.Greater(CapeSim.Flutter(6f), CapeSim.Flutter(2f));
            Assert.AreEqual(1f, CapeSim.Flutter(50f));
        }
    }
}
