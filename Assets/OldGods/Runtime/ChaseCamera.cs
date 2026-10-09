using Unity.Cinemachine;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Drives a Cinemachine orbital camera behind and above the player. The mouse or
    /// right stick turns it; when the player stops looking and runs, it swings back
    /// behind the direction of travel after a short lag.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public CinemachineOrbitalFollow Orbit;
        public PlayerMotor Player;

        [Tooltip("Degrees of yaw per unit of look input.")]
        public float YawSpeed = 3.2f;
        public float PitchSpeed = 1.6f;
        public float Sensitivity = 1f;
        public bool InvertY;
        [Tooltip("Seconds without look input before the camera swings behind the player.")]
        public float AutoAlignDelay = 0.9f;
        [Tooltip("Degrees per second at full run speed.")]
        public float AutoAlignSpeed = 110f;
        public float DefaultPitch = 26f;

        float idle;

        void Update()
        {
            if (Orbit == null) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale <= 0f) return;

            Vector2 look = GameInput.LookValue * Sensitivity;
            if (look.sqrMagnitude > 0.0001f)
            {
                idle = 0f;
                Orbit.HorizontalAxis.Value = Mathf.Repeat(Orbit.HorizontalAxis.Value + look.x * YawSpeed + 180f, 360f) - 180f;
                float dy = look.y * PitchSpeed * (InvertY ? 1f : -1f);
                var v = Orbit.VerticalAxis;
                Orbit.VerticalAxis.Value = Mathf.Clamp(v.Value + dy, v.Range.x, v.Range.y);
                return;
            }

            idle += dt;
            if (Player == null || idle < AutoAlignDelay) return;
            float speed = Player.HorizontalSpeed;
            if (speed < 1f) return;

            Vector3 vel = Player.Velocity;
            float heading = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
            float current = Orbit.HorizontalAxis.Value;
            float delta = Mathf.DeltaAngle(current, heading);
            // Do not whip around when the player runs straight at the camera.
            if (Mathf.Abs(delta) > 150f) return;
            float rate = AutoAlignSpeed * Mathf.Clamp01(speed / 8f);
            Orbit.HorizontalAxis.Value = Mathf.Repeat(current + Mathf.Clamp(delta, -rate * dt, rate * dt) + 180f, 360f) - 180f;
            Orbit.VerticalAxis.Value = Mathf.MoveTowards(Orbit.VerticalAxis.Value, DefaultPitch, 10f * dt);
        }

        /// <summary>Puts the camera straight behind the player, e.g. at stage start.</summary>
        public void SnapBehind()
        {
            if (Orbit == null || Player == null) return;
            Vector3 f = Player.Facing;
            Orbit.HorizontalAxis.Value = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            Orbit.VerticalAxis.Value = DefaultPitch;
        }
    }
}
