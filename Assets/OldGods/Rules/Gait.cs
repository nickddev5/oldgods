using System;

namespace OldGods.Rules
{
    /// <summary>
    /// How fast a two-legged figure's legs cycle, and how far they swing, for a ground speed.
    /// Cadence follows dynamic similarity, as animals do: cycles per second grow with the square
    /// root of speed over leg length, so long legs step slowly and small creatures scurry. The
    /// swing is then whatever keeps the planted foot moving with the ground, so feet do not slide.
    /// One cycle is two steps, one per foot.
    /// </summary>
    public static class Gait
    {
        /// <summary>Cycles per second at speed = leg length per second. PLACEHOLDER.</summary>
        public const float Cadence = 0.7f;
        /// <summary>The shader swings the hip by this many radians per unit of swing.</summary>
        public const float HipPerSwing = 2.6f;
        /// <summary>Share of a cycle each foot is on the ground: walking, then running.</summary>
        public const float WalkDuty = 0.6f, RunDuty = 0.35f;
        const float Gravity = 9.81f;

        /// <summary>Stride cycles per second at a ground speed (m/s) for a hip height (m).</summary>
        public static float CyclesPerSecond(float speed, float legLength)
        {
            if (speed <= 0f) return 0f;
            return Cadence * (float)Math.Sqrt(speed / Math.Max(0.05f, legLength));
        }

        /// <summary>Phase (radians) the walk cycle advances over dt seconds.</summary>
        public static float PhaseStep(float speed, float legLength, float dt) =>
            CyclesPerSecond(speed, legLength) * dt * (float)(Math.PI * 2.0);

        /// <summary>Metres covered in one full cycle (two steps).</summary>
        public static float StrideLength(float speed, float legLength)
        {
            float f = CyclesPerSecond(speed, legLength);
            return f > 0f ? speed / f : 0f;
        }

        /// <summary>
        /// Share of the cycle a foot is planted. Figures walk below a Froude speed of about 0.5
        /// and run above about 1.5, with flight between strides.
        /// </summary>
        public static float DutyFactor(float speed, float legLength)
        {
            float froude = speed / (float)Math.Sqrt(Gravity * Math.Max(0.05f, legLength));
            float t = Math.Min(1f, Math.Max(0f, froude - 0.5f));
            return WalkDuty + (RunDuty - WalkDuty) * t;
        }

        /// <summary>
        /// The shader swing that sweeps a planted foot back exactly as far as the body moves
        /// while it is down, clamped to [min, max].
        /// </summary>
        public static float Swing(float speed, float legLength, float min, float max)
        {
            float l = Math.Max(0.05f, legLength);
            float sweep = StrideLength(speed, l) * DutyFactor(speed, l) / (2f * l);
            float hip = (float)Math.Asin(Math.Min(0.95f, sweep));
            return Math.Min(max, Math.Max(min, hip / HipPerSwing));
        }
    }
}
