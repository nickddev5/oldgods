using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Drives the shader walk (legs, arms, bob) on a built-in model from how fast a transform
    /// moves across the ground. Used for the player, bosses and the menu preview. Cadence and
    /// swing come from Gait, scaled to the model's hip height, so stride matches ground covered.
    /// With a Motor it also blends in the jump and slide poses.
    /// </summary>
    public sealed class WalkAnimator : MonoBehaviour
    {
        public Transform Tracked;
        public PlayerMotor Motor;
        [Tooltip("Largest swing the run reaches; past it, strides lengthen no further and quicken instead.")]
        public float MaxSwing = 0.28f;
        [Tooltip("Swing kept while standing still, so idle figures breathe.")]
        public float IdleSwing = 0.02f;

        static readonly int PhaseId = Shader.PropertyToID("_AnimPhase");
        static readonly int SwingId = Shader.PropertyToID("_WalkSwing");
        static readonly int AirId = Shader.PropertyToID("_AirPose");
        static readonly int SlideId = Shader.PropertyToID("_SlidePose");
        Renderer[] renderers;
        MaterialPropertyBlock props;
        Vector3 last;
        float phase, swing, speed, hip = 0.85f;

        void Start()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            props = new MaterialPropertyBlock();
            if (Tracked == null) Tracked = transform;
            last = Tracked.position;
            var filter = GetComponent<MeshFilter>();
            if (filter != null) hip = MeshKit.HipHeight(filter.sharedMesh);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 p = Tracked.position;
            Vector3 d = p - last;
            d.y = 0f;
            last = p;
            float legLength = hip * Mathf.Max(0.1f, transform.lossyScale.y);
            // Smooth the measured speed so frame-time jitter does not flicker the legs.
            float measured = dt > 0f ? d.magnitude / dt : 0f;
            speed = Mathf.Lerp(speed, measured, 1f - Mathf.Exp(-12f * dt));
            float air = Motor != null ? Motor.AirBlend : 0f;
            float slide = Motor != null ? Motor.SlideBlend : 0f;
            float moving = speed > 0.2f ? speed : 0f;
            phase += Gait.PhaseStep(moving, legLength, dt);
            if (moving <= 0f) phase += dt * 1.2f; // slow breathing at rest
            if (phase > 1000f) phase -= Mathf.PI * 2f * 159f;
            float target = Gait.Swing(moving, legLength, IdleSwing, MaxSwing);
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
