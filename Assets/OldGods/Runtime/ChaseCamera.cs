using OldGods.Rules;
using Unity.Cinemachine;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Drives a Cinemachine orbital camera behind and above the player. The mouse or right
    /// stick turns it; pitch stays exactly where the player puts it. When the player stops
    /// looking and runs, the camera swings back behind the direction of travel after a short
    /// lag (yaw only). Near a boss it climbs and pulls back on top of the player's pitch.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public CinemachineOrbitalFollow Orbit;
        public PlayerMotor Player;
        public CameraAnchor Anchor;

        [Tooltip("Degrees of yaw per unit of look input.")]
        public float YawSpeed = 3.2f;
        [Tooltip("Degrees of pitch per unit of look input, before the vertical sensitivity setting.")]
        public float PitchSpeed = 1.4f;
        public float Sensitivity = 1f;
        public float VerticalSensitivity = 1f;
        public bool InvertY;
        [Tooltip("Seconds without look input before the camera swings behind the player.")]
        public float AutoAlignDelay = 1.2f;
        [Tooltip("Degrees per second at full run speed.")]
        public float AutoAlignSpeed = 90f;
        public float DefaultPitch = 15f;
        public float MinPitch = 6f, MaxPitch = 60f;
        public float NormalRadius = 9f;
        public float BossRadius = 13f;
        [Tooltip("Degrees added to the player's pitch while a boss is close.")]
        public float BossPitchBoost = 16f;

        float idle;
        float pitch = -1f;
        float bossBlend;

        void Awake()
        {
            // -cameraPitch N starts the camera at N degrees, for screenshots of the horizon.
            if (float.TryParse(CommandLine.Value("-cameraPitch"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float p))
                DefaultPitch = Mathf.Clamp(p, MinPitch, MaxPitch);
        }

        void Update()
        {
            if (Orbit == null) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale <= 0f) return;
            if (pitch < 0f) pitch = DefaultPitch;

            var boss = BossController.Active;
            bool bossNear = boss != null && Player != null && Vector3.Distance(boss.transform.position, Player.transform.position) < 22f;
            bossBlend = Mathf.MoveTowards(bossBlend, bossNear ? 1f : 0f, dt * 0.8f);
            float ease = bossBlend * bossBlend * (3f - 2f * bossBlend);
            Orbit.Radius = Mathf.Lerp(NormalRadius, BossRadius, ease);

            Vector2 look = GameInput.LookValue * (Sensitivity * LookScale.PerFrame(GameInput.LookIsRate, dt));
            if (look.sqrMagnitude > 0.0001f)
            {
                idle = 0f;
                Orbit.HorizontalAxis.Value = Wrap(Orbit.HorizontalAxis.Value + look.x * YawSpeed);
                pitch = Mathf.Clamp(pitch + look.y * PitchSpeed * VerticalSensitivity * (InvertY ? 1f : -1f), MinPitch, MaxPitch);
            }
            else
            {
                idle += dt;
                AutoAlign(dt);
            }

            Orbit.VerticalAxis.Value = Mathf.Clamp(pitch + BossPitchBoost * ease, MinPitch, MaxPitch + BossPitchBoost);
        }

        void AutoAlign(float dt)
        {
            if (Player == null || idle < AutoAlignDelay) return;
            float speed = Player.HorizontalSpeed;
            if (speed < 1f) return;
            Vector3 vel = Player.Velocity;
            float heading = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
            float current = Orbit.HorizontalAxis.Value;
            float delta = Mathf.DeltaAngle(current, heading);
            // Do not whip around when the player runs straight at the camera or strafes.
            if (Mathf.Abs(delta) > 120f) return;
            float rate = AutoAlignSpeed * Mathf.Clamp01(speed / 8f) * Mathf.Clamp01((idle - AutoAlignDelay) * 2f);
            Orbit.HorizontalAxis.Value = Wrap(current + Mathf.Clamp(delta, -rate * dt, rate * dt));
        }

        static float Wrap(float degrees) => Mathf.Repeat(degrees + 180f, 360f) - 180f;

        /// <summary>Puts the camera straight behind the player, e.g. at stage start.</summary>
        public void SnapBehind()
        {
            if (Orbit == null || Player == null) return;
            if (Anchor != null) Anchor.Snap();
            Vector3 f = Player.Facing;
            Orbit.HorizontalAxis.Value = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            if (pitch < 0f) pitch = DefaultPitch;
            Orbit.VerticalAxis.Value = pitch;
        }
    }

    /// <summary>
    /// The point the camera follows and looks at: exactly over the player on the ground plane,
    /// with its height smoothed so steps, slopes and landings do not jolt the view.
    /// </summary>
    [DefaultExecutionOrder(-200)] // before the Cinemachine brain's LateUpdate
    public sealed class CameraAnchor : MonoBehaviour
    {
        public Transform Target;
        public float Height = 1.5f;
        [Tooltip("Seconds to settle after the player's height changes.")]
        public float HeightSmoothing = 0.12f;
        float velocity;

        void LateUpdate()
        {
            if (Target == null) return;
            var p = Target.position;
            float y = Mathf.SmoothDamp(transform.position.y, p.y + Height, ref velocity, HeightSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(y - (p.y + Height)) > 6f) y = p.y + Height; // teleports
            transform.position = new Vector3(p.x, y, p.z);
        }

        public void Snap()
        {
            if (Target == null) return;
            transform.position = Target.position + Vector3.up * Height;
            velocity = 0f;
        }
    }
}
