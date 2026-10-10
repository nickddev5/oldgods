using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Swings the cape on a built-in model: follows the shoulders through the world, runs the
    /// cape pendulum (CapeSim) and hands its angles to the shader, which bends the vertices
    /// tagged BodyPart.Cape. Costs a few floats per character; models without a cape are unaffected.
    /// </summary>
    public sealed class CapeSway : MonoBehaviour
    {
        [Tooltip("Where the cape hangs from, in the model's own space, before its scale.")]
        public Vector3 Anchor = new Vector3(0f, 1.3f, -0.12f);
        public CapeTuning Tuning = CapeTuning.Default;
        [Tooltip("Air moving past in world space, metres per second.")]
        public Vector3 Wind;
        [Tooltip("How far the shoulders are bent forward of the model in radians (a slide's waist bend); set by DodgeLook.")]
        public float TorsoPitch;

        static readonly int CapeId = Shader.PropertyToID("_CapeSwing");
        Renderer[] renderers;
        MaterialPropertyBlock props;
        CapeState state;
        Vector3 lastAnchor, velocity;
        bool primed;

        void Start()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            props = new MaterialPropertyBlock();
        }

        void OnEnable() => primed = false;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 anchor = transform.TransformPoint(Anchor);
            if (!primed)
            {
                lastAnchor = anchor;
                velocity = Vector3.zero;
                primed = true;
            }
            Vector3 measured = (anchor - lastAnchor) / dt;
            lastAnchor = anchor;
            // A jump this fast is a teleport (respawn, stage change), not motion.
            if (measured.sqrMagnitude > 50f * 50f) measured = velocity;
            // Smooth over frame-time jitter, then take the acceleration from the smoothed speed.
            Vector3 smoothed = Vector3.Lerp(velocity, measured, 1f - Mathf.Exp(-20f * dt));
            Vector3 accel = (smoothed - velocity) / dt;
            velocity = smoothed;

            // Work in the shoulders' frame. When that frame is tipped (a lean, a bend at the waist),
            // gravity no longer points down it; CapeSim assumes it does, so the difference goes in
            // as an acceleration and a cape on a back bent level lies along the back.
            var torso = transform.rotation * Quaternion.Euler(TorsoPitch * Mathf.Rad2Deg, 0f, 0f);
            var toLocal = Quaternion.Inverse(torso);
            Vector3 tilt = new Vector3(0f, -CapeSim.Gravity, 0f) - toLocal * new Vector3(0f, -CapeSim.Gravity, 0f);
            Vector3 a = toLocal * accel + tilt, v = toLocal * (velocity - Wind);
            var t = Tuning;
            float scale = Mathf.Max(0.1f, transform.lossyScale.y);
            t.Length *= scale;
            CapeSim.Step(ref state, t, a.x, a.y, a.z, v.x, v.y, v.z, dt);

            var swing = new Vector4(state.Pitch, state.Roll, CapeSim.Flutter(v.magnitude / scale), 0f);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(props);
                props.SetVector(CapeId, swing);
                r.SetPropertyBlock(props);
            }
        }
    }
}
