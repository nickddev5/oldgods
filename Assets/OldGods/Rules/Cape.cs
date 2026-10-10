using System;

namespace OldGods.Rules
{
    /// <summary>
    /// How a cape hangs and moves. Angles in radians: Pitch lifts the hem backward (positive)
    /// or forward toward the legs (negative); Roll swings it to the wearer's right (positive).
    /// </summary>
    [Serializable]
    public struct CapeTuning
    {
        public float Length;     // shoulders to hem, metres (after the model's scale)
        public float Drag;       // air push per (m/s)^2; sets how high the cape flies at a run
        public float Damping;    // per second; how quickly a swing settles
        public float MaxBack;    // highest backward lift
        public float MaxForward; // furthest it may swing toward the legs
        public float MaxSide;

        public static CapeTuning Default => new CapeTuning
        {
            Length = 1.1f, Drag = 0.13f, Damping = 3.2f, MaxBack = 1.1f, MaxForward = 0.12f, MaxSide = 0.8f,
        };
    }

    public struct CapeState
    {
        public float Pitch, Roll, PitchSpeed, RollSpeed;
    }

    /// <summary>
    /// A cape as two pendulums hung from the shoulders, one front-to-back and one side-to-side,
    /// worked out in the wearer's own frame. The shoulders' acceleration swings it (it lags when
    /// the wearer sets off, swings forward when they stop), still air drags it back while moving,
    /// and a jump's fall lets it float. Vectors are in the wearer's frame: x right, y up, z forward.
    /// </summary>
    public static class CapeSim
    {
        public const float Gravity = 9.81f;
        // Fixed substeps keep the swing the same at any frame rate.
        public const float StepTime = 1f / 120f;
        const int MaxSteps = 12;

        /// <summary>
        /// Advances the cape by dt seconds. accel and velocity are the shoulders' motion through
        /// the air (subtract any wind from velocity), in the wearer's frame.
        /// </summary>
        public static void Step(ref CapeState s, CapeTuning t, float ax, float ay, float az, float vx, float vy, float vz, float dt)
        {
            if (dt <= 0f) return;
            int steps = Math.Min(MaxSteps, (int)Math.Ceiling(dt / StepTime));
            float h = Math.Min(dt, MaxSteps * StepTime) / steps;
            float length = Math.Max(0.05f, t.Length);
            // Falling lightens the pull down; never fully, so a long fall still settles.
            float g = Math.Max(Gravity * 0.15f, Gravity + ay);
            float airSpeed = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
            for (int i = 0; i < steps; i++)
            {
                float sp = (float)Math.Sin(s.Pitch), cp = (float)Math.Cos(s.Pitch);
                // The hem moves along (0, sin, -cos) as pitch grows; the air meets it at -velocity.
                float airAlong = -vy * sp + vz * cp;
                float pitchAccel = (-g * sp + az * cp + t.Drag * airSpeed * airAlong) / length - t.Damping * s.PitchSpeed;
                s.PitchSpeed += pitchAccel * h;
                s.Pitch += s.PitchSpeed * h;
                Limit(ref s.Pitch, ref s.PitchSpeed, -t.MaxForward, t.MaxBack);

                float sr = (float)Math.Sin(s.Roll), cr = (float)Math.Cos(s.Roll);
                // The hem moves along (cos, sin, 0) as roll grows.
                float sideAlong = -vx * cr - vy * sr;
                float rollAccel = (-g * sr - ax * cr + t.Drag * airSpeed * sideAlong) / length - t.Damping * s.RollSpeed;
                s.RollSpeed += rollAccel * h;
                s.Roll += s.RollSpeed * h;
                Limit(ref s.Roll, ref s.RollSpeed, -t.MaxSide, t.MaxSide);
            }
        }

        /// <summary>The steady backward lift at a constant speed through still air.</summary>
        public static float SteadyPitch(CapeTuning t, float speed) =>
            Math.Min(t.MaxBack, (float)Math.Atan(t.Drag * speed * speed / Gravity));

        /// <summary>How much the hem ripples, 0 to 1, from the speed through the air.</summary>
        public static float Flutter(float airSpeed) => Math.Min(1f, Math.Max(0f, (airSpeed - 0.5f) / 7f));

        // Stops at the limit and loses most of the swing, like cloth meeting the body.
        static void Limit(ref float angle, ref float speed, float min, float max)
        {
            if (angle < min) { angle = min; if (speed < 0f) speed *= -0.2f; }
            else if (angle > max) { angle = max; if (speed > 0f) speed *= -0.2f; }
        }
    }
}
