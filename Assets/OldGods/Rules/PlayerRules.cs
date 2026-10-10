using System;

namespace OldGods.Rules
{
    /// <summary>
    /// Look input arrives either as a distance (mouse delta, already per frame) or as a held
    /// rate (a stick). A rate is scaled by elapsed time so the camera turns at the same speed at
    /// any frame rate; 60 frames per second keeps the existing tuning.
    /// </summary>
    public static class LookScale
    {
        public const float ReferenceFrameRate = 60f;

        public static float PerFrame(bool isRate, float deltaTime) =>
            isRate ? Math.Max(0f, deltaTime) * ReferenceFrameRate : 1f;
    }

    /// <summary>Movement tuning for the player. PLACEHOLDER numbers for play-testing.</summary>
    [Serializable]
    public sealed class MotorTuning
    {
        public float RunSpeed = 8f;
        public float GroundAccel = 60f;
        public float GroundDecel = 50f;
        public float AirControl = 0.6f;
        /// <summary>Speed lost per second in the air while faster than a run (after a slide jump).</summary>
        public float AirDrag = 2.5f;
        public float Gravity = 28f;
        /// <summary>Gravity is multiplied by this while falling, so jumps rise slowly and land quickly.</summary>
        public float FallGravityMultiplier = 1.45f;
        public float JumpHeight = 2.2f;
        /// <summary>Upward speed is multiplied by this when jump is released early: a short hop.</summary>
        public float JumpCutMultiplier = 0.5f;
        public float CoyoteTime = 0.12f;
        public float JumpBuffer = 0.12f;
        public float SlideStartBoost = 4f;
        public float SlideFriction = 5f;
        public float SlideSlopeGain = 18f;
        public float SlideMinSpeed = 4f;
        public float SlideMaxSpeed = 22f;
        public float TurnSpeedDegrees = 720f;
        /// <summary>Downward speed at landing below which there is no fall damage.</summary>
        public float SafeFallSpeed = 20f;
        /// <summary>Damage per unit of landing speed above SafeFallSpeed.</summary>
        public float FallDamagePerSpeed = 4f;
    }

    public static class PlayerRules
    {
        /// <summary>Upward speed that reaches the given jump height under the given gravity.</summary>
        public static float JumpVelocity(float height, float gravity) => (float)Math.Sqrt(2f * gravity * Math.Max(0f, height));

        /// <summary>Damage taken on landing at downward speed impactSpeed (positive number).</summary>
        public static float FallDamage(float impactSpeed, MotorTuning t)
        {
            float over = impactSpeed - t.SafeFallSpeed;
            return over > 0f ? over * t.FallDamagePerSpeed : 0f;
        }

        /// <summary>
        /// Horizontal velocity after one step of grounded or airborne control.
        /// input is the desired direction (length 0..1) already in world space.
        /// </summary>
        public static Vec2 StepRun(Vec2 velocity, Vec2 input, bool grounded, float speedMultiplier, MotorTuning t, float dt)
        {
            float control = grounded ? 1f : t.AirControl;
            float run = t.RunSpeed * speedMultiplier;
            float speed = velocity.Magnitude;
            if (!grounded && speed > run)
            {
                // Momentum: in the air, speed above a run (from a slide) is kept and only bleeds
                // off slowly; input steers it rather than braking it.
                float keep = Math.Max(run, speed - t.AirDrag * dt);
                if (input.SqrMagnitude < 0.0001f) return velocity * (keep / speed);
                Vec2 steered = HordeSteering.Accelerate(velocity, input.Normalized * speed, t.GroundAccel * control, dt);
                float m = steered.Magnitude;
                return m > 1e-6f ? steered * (keep / m) : steered;
            }
            Vec2 desired = input * run;
            float rate = (input.SqrMagnitude > 0.0001f ? t.GroundAccel : t.GroundDecel) * control;
            return HordeSteering.Accelerate(velocity, desired, rate, dt);
        }

        /// <summary>
        /// Slide speed after one step. downhill is the slope's downhill component along the
        /// slide direction: sin(angle), positive when sliding downhill.
        /// </summary>
        public static float StepSlide(float speed, float downhill, MotorTuning t, float dt)
        {
            speed += downhill * t.SlideSlopeGain * dt;
            speed -= t.SlideFriction * dt;
            return Math.Min(speed, t.SlideMaxSpeed);
        }

        public static bool SlideEnded(float speed, MotorTuning t) => speed < t.SlideMinSpeed;

        /// <summary>Gravity this frame: stronger while falling.</summary>
        public static float GravityFor(float verticalSpeed, MotorTuning t) => verticalSpeed < 0f ? t.Gravity * t.FallGravityMultiplier : t.Gravity;
    }

    /// <summary>Health with invulnerability after each hit. Pure state, driven by the runtime.</summary>
    public sealed class Health
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float InvulnerableFor { get; private set; }
        public float HitInvulnerability = 0.5f;
        public bool IsDead => Current <= 0f;

        public Health(float max)
        {
            Max = max;
            Current = max;
        }

        public void Tick(float dt) => InvulnerableFor = Math.Max(0f, InvulnerableFor - dt);

        /// <summary>Applies damage unless invulnerable or dead. Returns the damage actually dealt.</summary>
        public float Damage(float amount, bool ignoreInvulnerability = false)
        {
            if (IsDead || amount <= 0f) return 0f;
            if (!ignoreInvulnerability && InvulnerableFor > 0f) return 0f;
            float dealt = Math.Min(Current, amount);
            Current -= dealt;
            InvulnerableFor = HitInvulnerability;
            return dealt;
        }

        /// <summary>Starts the invulnerability window without taking damage (an evaded hit).</summary>
        public void Guard() => InvulnerableFor = HitInvulnerability;

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Math.Min(Max, Current + amount);
        }

        public void SetMax(float max, bool keepRatio)
        {
            float ratio = Max > 0f ? Current / Max : 1f;
            Max = Math.Max(1f, max);
            Current = keepRatio ? ratio * Max : Math.Min(Current, Max);
        }

        public void Reset()
        {
            Current = Max;
            InvulnerableFor = 0f;
        }
    }
}
