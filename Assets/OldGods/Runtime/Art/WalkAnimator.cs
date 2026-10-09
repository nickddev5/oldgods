using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Drives the shader walk (legs, arms, bob) on a built-in model from how fast a transform
    /// moves across the ground. Used for the player, bosses and the menu preview.
    /// </summary>
    public sealed class WalkAnimator : MonoBehaviour
    {
        public Transform Tracked;
        [Tooltip("Stride: walk cycles per metre travelled, before the model's scale.")]
        public float StepsPerMetre = 0.75f;
        public float MaxSwing = 0.2f;
        [Tooltip("Swing kept while standing still, so idle figures breathe.")]
        public float IdleSwing = 0.02f;
        public float RunSpeed = 8f;

        static readonly int PhaseId = Shader.PropertyToID("_AnimPhase");
        static readonly int SwingId = Shader.PropertyToID("_WalkSwing");
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
            swing = Mathf.MoveTowards(swing, target, dt * 1.5f);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(props);
                props.SetFloat(PhaseId, phase);
                props.SetFloat(SwingId, swing);
                r.SetPropertyBlock(props);
            }
        }
    }
}
