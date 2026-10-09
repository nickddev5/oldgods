using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Drives the shader walk (legs, arms, bob) on a built-in model from how fast a transform
    /// moves across the ground. Used for the player, bosses and the menu preview. With a Motor
    /// it also blends in the jump and slide poses.
    /// </summary>
    public sealed class WalkAnimator : MonoBehaviour
    {
        public Transform Tracked;
        public PlayerMotor Motor;
        [Tooltip("Stride: walk cycles per metre travelled, before the model's scale.")]
        public float StepsPerMetre = 0.75f;
        public float MaxSwing = 0.2f;
        [Tooltip("Swing kept while standing still, so idle figures breathe.")]
        public float IdleSwing = 0.02f;
        public float RunSpeed = 8f;

        static readonly int PhaseId = Shader.PropertyToID("_AnimPhase");
        static readonly int SwingId = Shader.PropertyToID("_WalkSwing");
        static readonly int AirId = Shader.PropertyToID("_AirPose");
        static readonly int SlideId = Shader.PropertyToID("_SlidePose");
        Renderer[] renderers;
        MaterialPropertyBlock props;
        Vector3 last;
        float phase, swing;

        void Start()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            props = new MaterialPropertyBlock();
            if (Tracked == null) Tracked = transform;
            last = Tracked.position;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 p = Tracked.position;
            Vector3 d = p - last;
            d.y = 0f;
            last = p;
            float scale = Mathf.Max(0.1f, transform.lossyScale.y);
            float speed = dt > 0f ? d.magnitude / dt : 0f;
            phase += d.magnitude / scale * StepsPerMetre * Mathf.PI * 2f;
            if (speed < 0.2f) phase += dt * 1.2f; // slow breathing at rest
            if (phase > 1000f) phase -= Mathf.PI * 2f * 159f;
            float target = Mathf.Lerp(IdleSwing, MaxSwing, Mathf.Clamp01(speed / Mathf.Max(0.1f, RunSpeed * scale / 1.8f)));
            float air = Motor != null ? Motor.AirBlend : 0f;
            float slide = Motor != null ? Motor.SlideBlend : 0f;
            // Legs stop striding in the air and in a slide; the poses take over.
            target *= 1f - Mathf.Max(air, slide) * 0.85f;
            swing = Mathf.MoveTowards(swing, target, dt * 2.5f);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(props);
                props.SetFloat(PhaseId, phase);
                props.SetFloat(SwingId, swing);
                props.SetFloat(AirId, air);
                props.SetFloat(SlideId, slide);
                r.SetPropertyBlock(props);
            }
        }
    }
}
