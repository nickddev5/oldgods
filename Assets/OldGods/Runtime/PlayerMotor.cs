using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Run, jump, slide and air control on a CharacterController. Movement is relative
    /// to the camera's yaw. Numbers come from MotorTuning and the player's stats.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public MotorTuning Tuning = new MotorTuning();
        [Tooltip("Movement is relative to this transform's yaw (the main camera).")]
        public Transform ViewYaw;
        [Tooltip("The model, rotated to face movement and squashed while sliding.")]
        public Transform Visual;

        public float SpeedMultiplier = 1f;
        public float JumpMultiplier = 1f;
        public int ExtraJumps;
        public bool InputEnabled = true;

        public bool Grounded { get; private set; }
        public bool IsSliding { get; private set; }
        public Vector3 Velocity => new Vector3(horizontal.X, verticalSpeed, horizontal.Z);
        public float HorizontalSpeed => horizontal.Magnitude;
        public Vector3 Facing { get; private set; } = Vector3.forward;
        /// <summary>0 on the ground, rising to 1 shortly after leaving it; drives the jump pose.</summary>
        public float AirBlend { get; private set; }
        /// <summary>0 to 1 while sliding; drives the slide pose.</summary>
        public float SlideBlend { get; private set; }

        CharacterController cc;
        PlayerHealth health;
        Vec2 horizontal;
        float verticalSpeed;
        float coyote, jumpBuffer;
        int airJumpsUsed;
        float slideSpeed;
        Vector3 slideDir;
        bool wasGrounded = true;
        float standHeight;
        Vector3 standCenter;
        bool jumpHeld;
        float squash;       // landing squash (+) or take-off stretch (-), springs back to 0
        float squashVel;
        float lean, roll, yaw;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<PlayerHealth>();
            standHeight = cc.height;
            standCenter = cc.center;
            GameInput.Ensure();
        }

        /// <summary>Moves the player to a point without carrying velocity, e.g. at stage start.</summary>
        public void Teleport(Vector3 position)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            horizontal = default;
            verticalSpeed = 0f;
            EndSlide();
        }

        /// <summary>Turns the player to face a direction at once, e.g. in a cutscene.</summary>
        public void FaceTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) return;
            Facing = direction.normalized;
            yaw = Mathf.Atan2(Facing.x, Facing.z) * Mathf.Rad2Deg;
            if (Visual != null) Visual.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var t = Tuning;

            Vector2 input = InputEnabled ? GameInput.MoveValue : Vector2.zero;
            Vector3 fwd = ViewYaw != null ? ViewYaw.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            Vector3 wish = Vector3.ClampMagnitude(fwd * input.y + right * input.x, 1f);

            Grounded = cc.isGrounded;
            if (Grounded)
            {
                coyote = t.CoyoteTime;
                airJumpsUsed = 0;
            }
            else
            {
                coyote -= dt;
            }

            if (InputEnabled && GameInput.Pressed(GameInput.Jump)) jumpBuffer = t.JumpBuffer;
            else jumpBuffer -= dt;

            // Slide: hold to slide while grounded and moving; release or slow down to stop.
            bool slideHeld = InputEnabled && GameInput.Held(GameInput.Slide);
            if (!IsSliding && slideHeld && Grounded && horizontal.Magnitude > 2f) StartSlide();
            if (IsSliding)
            {
                if (!slideHeld || !(Grounded || NearGround())) EndSlide();
            }

            if (IsSliding)
            {
                var n = Ground.Normal(transform.position.x, transform.position.z);
                float downhill = n.x * slideDir.x + n.z * slideDir.z;
                slideSpeed = PlayerRules.StepSlide(slideSpeed, downhill, t, dt);
                if (wish.sqrMagnitude > 0.01f)
                    slideDir = Vector3.RotateTowards(slideDir, wish.normalized, Mathf.Deg2Rad * 100f * dt, 0f);
                horizontal = new Vec2(slideDir.x * slideSpeed, slideDir.z * slideSpeed);
                if (PlayerRules.SlideEnded(slideSpeed, t)) EndSlide();
            }
            else
            {
                horizontal = PlayerRules.StepRun(horizontal, new Vec2(wish.x, wish.z), Grounded, SpeedMultiplier, t, dt);
            }

            // Jump, with coyote time, a buffer and optional air jumps. Jumping out of a
            // slide keeps the slide's speed.
            if (jumpBuffer > 0f)
            {
                bool canGroundJump = coyote > 0f;
                bool canAirJump = !canGroundJump && airJumpsUsed < ExtraJumps;
                if (canGroundJump || canAirJump)
                {
                    if (canAirJump) airJumpsUsed++;
                    verticalSpeed = PlayerRules.JumpVelocity(t.JumpHeight * JumpMultiplier, t.Gravity);
                    coyote = 0f;
                    jumpBuffer = 0f;
                    if (IsSliding) EndSlide();
                    Grounded = false;
                    jumpHeld = true;
                    squash = -0.12f;
                }
            }
            // Releasing jump while still rising cuts the jump short.
            if (jumpHeld && !(InputEnabled && GameInput.Held(GameInput.Jump)))
            {
                jumpHeld = false;
                if (verticalSpeed > 0f) verticalSpeed *= t.JumpCutMultiplier;
            }
            if (verticalSpeed <= 0f) jumpHeld = false;

            if (Grounded && verticalSpeed < 0f) verticalSpeed = -2f;
            // Hug the ground while sliding so downhill slides do not hop off the slope.
            if (IsSliding && verticalSpeed <= 0f) verticalSpeed = Mathf.Min(verticalSpeed, -slideSpeed * 0.6f);
            verticalSpeed -= PlayerRules.GravityFor(verticalSpeed, t) * dt;
            float fallSpeedBefore = -verticalSpeed;

            var flags = cc.Move(new Vector3(horizontal.X, verticalSpeed, horizontal.Z) * dt);
            bool nowGrounded = (flags & CollisionFlags.Below) != 0;

            if (nowGrounded && !wasGrounded && fallSpeedBefore > 0f)
            {
                float dmg = PlayerRules.FallDamage(fallSpeedBefore, t);
                if (dmg > 0f && health != null) health.TakeTrueDamage(dmg);
                if (fallSpeedBefore > 4f) squash = Mathf.Max(squash, Mathf.Clamp(fallSpeedBefore * 0.012f, 0.06f, 0.22f));
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            wasGrounded = nowGrounded;

            // The map edge: the rim slopes up, and a wall partway up it stops the player.
            // Walking, sliding or jumping into it stops the outward part of the motion.
            var pos = transform.position;
            var inside = Ground.ClampToPlayable(pos, -Ground.RimWidth * 0.55f);
            if ((inside - pos).sqrMagnitude > 1e-6f)
            {
                cc.Move(new Vector3(inside.x - pos.x, 0f, inside.z - pos.z));
                if (Mathf.Abs(inside.x - pos.x) > 1e-4f) horizontal.X = 0f;
                if (Mathf.Abs(inside.z - pos.z) > 1e-4f) horizontal.Z = 0f;
                if (IsSliding) EndSlide();
            }

            // Out of the world: put the player back on the ground inside the map.
            if (transform.position.y < Ground.Height(transform.position.x, transform.position.z) - 10f)
                Teleport(Ground.Snap(Ground.ClampToPlayable(transform.position, 2f)) + Vector3.up * 1.5f);

            if (horizontal.SqrMagnitude > 0.25f)
                Facing = new Vector3(horizontal.X, 0f, horizontal.Z).normalized;
            UpdateVisual(dt, t);
        }

        /// <summary>
        /// Turns the model toward the motion, leans it into runs and turns, springs it on take-off
        /// and landing, and lowers it into the slide. The limbs are posed by the shader (WalkAnimator).
        /// </summary>
        void UpdateVisual(float dt, MotorTuning t)
        {
            bool airborne = !Grounded && !NearGround();
            AirBlend = Mathf.MoveTowards(AirBlend, airborne ? 1f : 0f, dt * (airborne ? 5f : 10f));
            SlideBlend = Mathf.MoveTowards(SlideBlend, IsSliding ? 1f : 0f, dt * 8f);
            if (Visual == null) return;

            // Yaw toward the motion; roll by how fast it turns.
            float yawBefore = yaw;
            float targetYaw = Mathf.Atan2(Facing.x, Facing.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, t.TurnSpeedDegrees * dt);
            float turnRate = Mathf.DeltaAngle(yawBefore, yaw) / dt;
            float speed01 = Mathf.Clamp01(horizontal.Magnitude / Mathf.Max(0.1f, t.RunSpeed * SpeedMultiplier));
            float k = 1f - Mathf.Exp(-10f * dt);
            roll = Mathf.Lerp(roll, Mathf.Clamp(-turnRate * 0.025f * speed01, -14f, 14f), k);
            lean = Mathf.Lerp(lean, speed01 * 7f * (1f - SlideBlend) - SlideBlend * 16f, k);

            // Squash and stretch on a spring.
            squashVel += (-squash * 260f - squashVel * 18f) * dt;
            squash += squashVel * dt;
            float sq = Mathf.Clamp(squash, -0.2f, 0.3f);

            Visual.localRotation = Quaternion.Euler(lean, yaw - transform.eulerAngles.y, roll);
            Visual.localScale = new Vector3(1f + sq * 0.5f, 1f - sq, 1f + sq * 0.5f);
            Visual.localPosition = new Vector3(0f, -SlideBlend * 0.32f, 0f);
        }

        bool NearGround() => transform.position.y - Ground.Height(transform.position.x, transform.position.z) < 0.35f;

        void StartSlide()
        {
            IsSliding = true;
            slideDir = new Vector3(horizontal.X, 0f, horizontal.Z).normalized;
            slideSpeed = Mathf.Min(Tuning.SlideMaxSpeed, horizontal.Magnitude + Tuning.SlideStartBoost);
            cc.height = standHeight * 0.55f;
            cc.center = new Vector3(standCenter.x, standCenter.y - standHeight * 0.225f, standCenter.z);
        }

        void EndSlide()
        {
            if (!IsSliding) return;
            IsSliding = false;
            if (cc == null) return;
            cc.height = standHeight;
            cc.center = standCenter;
        }
    }
}
