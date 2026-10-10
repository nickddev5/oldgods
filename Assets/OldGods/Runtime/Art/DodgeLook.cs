using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Shows a god's own slide on a built-in model: hands the slide pose (DodgePose) to the shader,
    /// raises what the god rides on (a cloud, a wave, a ring of light, a bow wave of rock) under its
    /// feet and leaves its trail behind. The blend comes from the Motor's slide, or from
    /// ManualBlend when posed without one (the model gallery). The whole-model pitch, yaw and
    /// height are applied by PlayerMotor. Purely visual: nothing in the game reads it.
    /// </summary>
    public sealed class DodgeLook : MonoBehaviour
    {
        public DodgeStyle Style;
        public PlayerMotor Motor;
        [Tooltip("When 0 or more, the slide blend to show without a Motor.")]
        public float ManualBlend = -1f;
        [Tooltip("World velocity to show without a Motor: it turns the mount and drives the trail.")]
        public Vector3 ManualVelocity;

        static readonly int SlideId = Shader.PropertyToID("_SlidePose");
        static readonly int LegId = Shader.PropertyToID("_DodgeLeg");
        static readonly int ArmId = Shader.PropertyToID("_DodgeArm");
        static readonly int MiscId = Shader.PropertyToID("_DodgeMisc");
        static readonly int ExtraId = Shader.PropertyToID("_DodgeExtra");
        static readonly Dictionary<DodgeMount, Mesh> mounts = new Dictionary<DodgeMount, Mesh>();
        static Mesh puff, chip;

        DodgePose pose;
        MeshRenderer body;
        MaterialPropertyBlock props;
        CapeSway cape;
        Transform mark, mount;
        Vector3 markRest;
        Quaternion markRestRotation;
        float hip = 0.85f, heading, emit, age;

        public DodgePose Pose => pose;

        void Start()
        {
            pose = Dodges.Pose(Style);
            body = GetComponent<MeshRenderer>();
            props = new MaterialPropertyBlock();
            cape = GetComponent<CapeSway>();
            var filter = GetComponent<MeshFilter>();
            if (filter != null) hip = MeshKit.HipHeight(filter.sharedMesh);
            mark = transform.Find("Mark");
            if (mark != null)
            {
                markRest = mark.localPosition;
                markRestRotation = mark.localRotation;
            }
            var forward = Motor != null ? Motor.Facing : transform.forward;
            heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (pose.Mount != DodgeMount.None) mount = BuildMount();
        }

        void OnDestroy()
        {
            if (mount != null) Destroy(mount.gameObject);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            age += dt;
            float blend = ManualBlend >= 0f ? ManualBlend : Motor != null ? Motor.SlideBlend : 0f;
            Vector3 velocity = ManualBlend >= 0f || Motor == null ? ManualVelocity : Motor.Velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.25f) heading = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;

            if (body != null)
            {
                body.GetPropertyBlock(props);
                props.SetVector(LegId, new Vector4(pose.HipLeft, pose.HipRight, pose.KneeLeft, pose.KneeRight));
                props.SetVector(ArmId, new Vector4(pose.ShoulderLeft, pose.ShoulderRight, pose.RaiseLeft, pose.RaiseRight));
                props.SetVector(MiscId, new Vector4(pose.ElbowLeft, pose.ElbowRight, pose.Waist, pose.Nod));
                props.SetVector(ExtraId, new Vector4(pose.Gallop, hip, 0f, 0f));
                if (ManualBlend >= 0f) props.SetFloat(SlideId, blend);
                body.SetPropertyBlock(props);
            }
            float waist = pose.Waist * blend;
            if (cape != null) cape.TorsoPitch = waist;
            if (mark != null)
            {
                // The mark on the chest follows the bend at the waist.
                var bendAt = Quaternion.Euler(waist * Mathf.Rad2Deg, 0f, 0f);
                var pivot = new Vector3(0f, hip, 0f);
                mark.localPosition = pivot + bendAt * (markRest - pivot);
                mark.localRotation = bendAt * markRestRotation;
            }

            if (mount != null) PlaceMount(blend);
            if (pose.Trail != DodgeTrail.None && blend > 0.6f && velocity.magnitude > 2f) Trail(dt, velocity.magnitude);
            else emit = 0f;
        }

        Vector3 Ground => transform.parent != null ? transform.parent.position : transform.position;

        // ---- Mounts ----

        Transform BuildMount()
        {
            var go = new GameObject("Dodge " + pose.Mount);
            go.transform.SetParent(transform.parent, false);
            go.layer = gameObject.layer;
            go.AddComponent<MeshFilter>().sharedMesh = MountMesh(pose.Mount);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = pose.Mount == DodgeMount.Halo ? Fx.Glow(new Color(1.7f, 1.2f, 0.45f)) : body != null ? body.sharedMaterial : null;
            r.shadowCastingMode = pose.Mount == DodgeMount.Halo ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            go.SetActive(false);
            return go.transform;
        }

        void PlaceMount(float blend)
        {
            bool show = blend > 0.02f;
            if (mount.gameObject.activeSelf != show) mount.gameObject.SetActive(show);
            if (!show) return;
            // Rises out of nothing as the slide starts and sinks away as it ends.
            float grow = Mathf.SmoothStep(0f, 1f, blend);
            float bob = 0f, spin = 0f;
            Vector3 scale = Vector3.one * grow;
            switch (pose.Mount)
            {
                case DodgeMount.Cloud:
                    bob = Mathf.Sin(age * 6f) * 0.03f;
                    scale = new Vector3(grow, grow * (1f + Mathf.Sin(age * 9f) * 0.05f), grow);
                    break;
                case DodgeMount.Wave:
                    bob = Mathf.Sin(age * 5f) * 0.025f;
                    scale = new Vector3(grow, grow * (1f + Mathf.Sin(age * 7f) * 0.06f), grow);
                    break;
                case DodgeMount.Halo:
                    bob = 0.06f;
                    spin = age * 40f;
                    break;
                case DodgeMount.Furrow:
                    scale = new Vector3(grow, grow * (0.9f + Mathf.Abs(Mathf.Sin(age * 14f)) * 0.15f), grow);
                    break;
            }
            mount.SetPositionAndRotation(Ground + Vector3.up * bob, Quaternion.Euler(0f, heading + spin, 0f));
            mount.localScale = scale;
        }

        static Mesh MountMesh(DodgeMount kind)
        {
            if (mounts.TryGetValue(kind, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (kind)
            {
                case DodgeMount.Cloud:
                {
                    // A storm cloud the god stands on: a flat-topped heap of puffs, pale above and
                    // slate beneath, longest along the direction of travel, trailing wisps behind.
                    var top = new Color(0.86f, 0.88f, 0.92f);
                    var under = new Color(0.42f, 0.46f, 0.56f);
                    k.Ball(new Vector3(0f, 0.2f, 0.05f), new Vector3(0.62f, 0.24f, 0.78f), top, 9, 5, null, 0.12f, 1);
                    k.Ball(new Vector3(0f, 0.08f, 0f), new Vector3(0.7f, 0.16f, 0.9f), under, 9, 4, null, 0.1f, 2);
                    k.Ball(new Vector3(0.42f, 0.18f, 0.32f), new Vector3(0.3f, 0.2f, 0.3f), top, 7, 4, null, 0.15f, 3);
                    k.Ball(new Vector3(-0.45f, 0.17f, 0.18f), new Vector3(0.32f, 0.2f, 0.34f), top, 7, 4, null, 0.15f, 4);
                    k.Ball(new Vector3(0.3f, 0.16f, -0.5f), new Vector3(0.3f, 0.18f, 0.32f), top, 7, 4, null, 0.15f, 5);
                    k.Ball(new Vector3(-0.28f, 0.15f, -0.6f), new Vector3(0.28f, 0.17f, 0.3f), top, 7, 4, null, 0.15f, 6);
                    k.Ball(new Vector3(0f, 0.12f, -1.0f), new Vector3(0.36f, 0.12f, 0.34f), top, 7, 4, null, 0.18f, 7);
                    k.Ball(new Vector3(0.1f, 0.1f, -1.38f), new Vector3(0.22f, 0.09f, 0.24f), top, 6, 3, null, 0.2f, 8);
                    k.Ball(new Vector3(0f, 0.02f, 0.1f), new Vector3(0.48f, 0.1f, 0.6f), under * 0.85f, 8, 3, null, 0.1f, 9);
                    break;
                }
                case DodgeMount.Wave:
                {
                    // A breaking wave the god surfs: a swell rising from the front to a curling crest
                    // behind, deep teal below and pale water near the lip, with foam along the crest.
                    var deep = new Color(0.1f, 0.38f, 0.45f);
                    var pale = new Color(0.35f, 0.7f, 0.72f);
                    var foam = new Color(0.92f, 0.96f, 0.95f);
                    var profile = new[]
                    {
                        new Vector2(1.0f, 0f), new Vector2(0.5f, 0.14f), new Vector2(0f, 0.28f), new Vector2(-0.4f, 0.48f),
                        new Vector2(-0.7f, 0.78f), new Vector2(-0.68f, 1.02f), new Vector2(-0.45f, 1.12f), new Vector2(-0.2f, 1.04f),
                    };
                    const float half = 0.85f;
                    for (int i = 0; i < profile.Length - 1; i++)
                    {
                        var c = Color.Lerp(deep, pale, i / (float)(profile.Length - 2));
                        if (i >= profile.Length - 3) c = foam;
                        var a = profile[i];
                        var b = profile[i + 1];
                        // Narrower toward the lip so the crest reads as curling over.
                        float wa = half * (1f - i * 0.05f), wb = half * (1f - (i + 1) * 0.05f);
                        Vector3 p0 = new Vector3(-wa, a.y, a.x), p1 = new Vector3(wa, a.y, a.x);
                        Vector3 p2 = new Vector3(wb, b.y, b.x), p3 = new Vector3(-wb, b.y, b.x);
                        k.Quad(p0, p1, p2, p3, c);
                        k.Quad(p3, p2, p1, p0, c * 0.8f);
                        // Side walls down to the water.
                        Vector3 l0 = new Vector3(-wa, 0f, a.x), l1 = new Vector3(-wb, 0f, b.x);
                        Vector3 r0 = new Vector3(wa, 0f, a.x), r1 = new Vector3(wb, 0f, b.x);
                        var wall = Color.Lerp(deep, pale, 0.35f);
                        k.Quad(l0, p0, p3, l1, wall);
                        k.Quad(l1, p3, p0, l0, wall);
                        k.Quad(r1, p2, p1, r0, wall);
                        k.Quad(r0, p1, p2, r1, wall);
                    }
                    k.Smooth = false;
                    for (int i = 0; i < 5; i++)
                        k.Ball(new Vector3(-0.6f + i * 0.3f, 1.08f, -0.42f + (i % 2) * 0.08f), new Vector3(0.16f, 0.1f, 0.14f), foam, 6, 3, null, 0.2f, 10 + i);
                    k.Ball(new Vector3(0f, 0.02f, 0.1f), new Vector3(1.1f, 0.06f, 1.2f), pale, 10, 3, null, 0.1f, 20);
                    break;
                }
                case DodgeMount.Furrow:
                {
                    // Ground broken open around the god: slabs of rock heaved up in a bow wave ahead
                    // and to the sides, earth churned between them.
                    var rock = new Color(0.5f, 0.46f, 0.42f);
                    var earth = new Color(0.36f, 0.27f, 0.2f);
                    k.Smooth = false;
                    k.Ball(Vector3.zero, new Vector3(0.62f, 0.12f, 0.7f), earth, 8, 3, null, 0.25f, 30);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = Mathf.Lerp(-115f, 115f, i / 8f);
                        var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                        float r = 0.5f + (i % 2) * 0.08f;
                        float h = 0.22f + 0.18f * Mathf.Cos(a * Mathf.Deg2Rad) + (i % 3) * 0.04f;
                        var tilt = Quaternion.LookRotation(dir) * Quaternion.Euler(-35f, 0f, (i % 2 == 0 ? 8f : -8f));
                        k.Block(dir * r + Vector3.up * h * 0.35f, new Vector3(0.2f + (i % 2) * 0.06f, h, 0.12f), i % 3 == 0 ? rock * 0.85f : rock, 0.25f, tilt);
                    }
                    break;
                }
                case DodgeMount.Halo:
                {
                    // Two flat rings of light, one inside the other, with eight marks between them.
                    var c = Color.white;
                    AddRing(k, 0.55f, 0.62f, 0f, c);
                    AddRing(k, 0.36f, 0.39f, 0.01f, c);
                    for (int i = 0; i < 8; i++)
                    {
                        var o = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                        k.Box(o * 0.47f, new Vector3(0.03f, 0.01f, 0.1f), c, 1f, Quaternion.LookRotation(o));
                    }
                    break;
                }
            }
            m = k.Build("Dodge" + kind);
            mounts[kind] = m;
            return m;
        }

        static void AddRing(MeshKit k, float inner, float outer, float y, Color c)
        {
            const int seg = 32;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                var o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                var up = Vector3.up * y;
                k.Quad(o0 * inner + up, o1 * inner + up, o1 * outer + up, o0 * outer + up, c);
                k.Quad(o0 * outer + up, o1 * outer + up, o1 * inner + up, o0 * inner + up, c);
            }
        }

        // ---- Trails ----

        void Trail(float dt, float speed)
        {
            float rate = pose.Trail == DodgeTrail.Sparks || pose.Trail == DodgeTrail.Embers ? 48f : 22f;
            emit += dt * rate * Mathf.Clamp(speed / 10f, 0.6f, 1.5f);
            var rot = Quaternion.Euler(0f, heading, 0f);
            Vector3 back = rot * Vector3.back, side = rot * Vector3.right;
            Vector3 g = Ground;
            while (emit >= 1f)
            {
                emit -= 1f;
                float s = Random.Range(-1f, 1f);
                var spin = Random.rotation;
                switch (pose.Trail)
                {
                    case DodgeTrail.Mist:
                        Effects.Burst(Puff, g + back * Random.Range(0.6f, 1.3f) + side * s * 0.45f + Vector3.up * Random.Range(0.1f, 0.3f), spin,
                            Vector3.one * 0.25f, Vector3.one * Random.Range(0.6f, 0.9f), new Color(0.85f, 0.87f, 0.92f, 0.55f), 0.6f);
                        break;
                    case DodgeTrail.Sparks:
                        Effects.Burst(Chip, g + back * Random.Range(0.1f, 0.5f) + side * s * 0.25f + Vector3.up * Random.Range(0.02f, 0.2f), spin,
                            Vector3.one * 0.11f, Vector3.one * 0.02f, new Color(2.4f, 0.9f, 0.2f, 1f), Random.Range(0.25f, 0.4f));
                        break;
                    case DodgeTrail.Spray:
                        Effects.Burst(Puff, g + back * Random.Range(0.4f, 1.1f) + side * s * 0.9f + Vector3.up * Random.Range(0.2f, 0.9f), spin,
                            Vector3.one * 0.12f, Vector3.one * Random.Range(0.3f, 0.5f), new Color(0.85f, 0.97f, 0.97f, 0.6f), 0.45f);
                        break;
                    case DodgeTrail.Dust:
                        Effects.Burst(Puff, g + back * Random.Range(0.4f, 1.0f) + side * s * 0.35f + Vector3.up * Random.Range(0.05f, 0.2f), spin,
                            Vector3.one * 0.2f, Vector3.one * Random.Range(0.6f, 0.85f), new Color(0.62f, 0.52f, 0.4f, 0.45f), 0.55f);
                        break;
                    case DodgeTrail.Embers:
                        if (Random.value < 0.45f)
                            Effects.Burst(Puff, g + back * Random.Range(0.2f, 0.9f) + side * s * 0.3f + Vector3.up * Random.Range(0.6f, 1.3f), spin,
                                Vector3.one * 0.35f, Vector3.one * 0.08f, new Color(2.4f, 0.75f, 0.2f, 0.7f), 0.35f);
                        else
                            Effects.Burst(Chip, g + back * Random.Range(0.3f, 1.6f) + side * s * 0.45f + Vector3.up * Random.Range(0.4f, 1.6f), spin,
                                Vector3.one * 0.08f, Vector3.one * 0.01f, new Color(3f, 1.6f, 0.4f, 1f), Random.Range(0.4f, 0.7f));
                        break;
                    case DodgeTrail.Rocks:
                        if (Random.value < 0.5f)
                            Effects.Burst(Chip, g + back * Random.Range(0.2f, 0.7f) + side * Mathf.Sign(s) * Random.Range(0.4f, 0.8f) + Vector3.up * Random.Range(0.05f, 0.4f), spin,
                                Vector3.one * Random.Range(0.1f, 0.18f), Vector3.one * 0.04f, new Color(0.45f, 0.38f, 0.32f, 1f), 0.45f);
                        else
                            Effects.Burst(Puff, g + back * Random.Range(0.5f, 1.1f) + side * s * 0.4f + Vector3.up * 0.1f, spin,
                                Vector3.one * 0.25f, Vector3.one * 0.7f, new Color(0.5f, 0.42f, 0.34f, 0.4f), 0.5f);
                        break;
                    case DodgeTrail.Motes:
                        Effects.Burst(Chip, g + back * Random.Range(0.2f, 1.2f) + side * s * 0.5f + Vector3.up * Random.Range(0.1f, 1.6f), spin,
                            Vector3.one * 0.07f, Vector3.zero, new Color(1.9f, 1.35f, 0.5f, 1f), Random.Range(0.6f, 0.9f));
                        break;
                }
            }
        }

        static Mesh Puff
        {
            get
            {
                if (puff != null) return puff;
                var k = new MeshKit { Smooth = false };
                k.Ball(Vector3.zero, Vector3.one * 0.5f, Color.white, 7, 4, null, 0.15f, 3);
                return puff = k.Build("DodgePuff");
            }
        }

        static Mesh Chip
        {
            get
            {
                if (chip != null) return chip;
                var k = new MeshKit();
                k.Gem(Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), Color.white);
                return chip = k.Build("DodgeChip");
            }
        }
    }
}
