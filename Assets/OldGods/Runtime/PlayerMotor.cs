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
                }
            }

            if (Grounded && verticalSpeed < 0f) verticalSpeed = -2f;
            // Hug the ground while sliding so downhill slides do not hop off the slope.
            if (IsSliding && verticalSpeed <= 0f) verticalSpeed = Mathf.Min(verticalSpeed, -slideSpeed * 0.6f);
            verticalSpeed -= t.Gravity * dt;
            float fallSpeedBefore = -verticalSpeed;

            var flags = cc.Move(new Vector3(horizontal.X, verticalSpeed, horizontal.Z) * dt);
            bool nowGrounded = (flags & CollisionFlags.Below) != 0;

            if (nowGrounded && !wasGrounded && fallSpeedBefore > 0f)
            {
                float dmg = PlayerRules.FallDamage(fallSpeedBefore, t);
                if (dmg > 0f && health != null) health.TakeTrueDamage(dmg);
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            wasGrounded = nowGrounded;

            // Out of the world: put the player back on the ground at the same x/z.
            if (transform.position.y < Ground.Height(transform.position.x, transform.position.z) - 10f)
                Teleport(Ground.Snap(transform.position) + Vector3.up * 1.5f);

            if (horizontal.SqrMagnitude > 0.25f)
                Facing = new Vector3(horizontal.X, 0f, horizontal.Z).normalized;
            if (Visual != null)
            {
                var targetRot = Quaternion.LookRotation(Facing, Vector3.up);
                Visual.rotation = Quaternion.RotateTowards(Visual.rotation, targetRot, t.TurnSpeedDegrees * dt);
                var targetScale = IsSliding ? new Vector3(1.1f, 0.55f, 1.1f) : Vector3.one;
                Visual.localScale = Vector3.Lerp(Visual.localScale, targetScale, 1f - Mathf.Exp(-18f * dt));
            }
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
