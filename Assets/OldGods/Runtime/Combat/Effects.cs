using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// Short-lived visual effects: flashes, telegraph discs, beams. Pooled GameObjects
    /// that grow and fade over their lifetime. Purely visual; no gameplay reads them.
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        sealed class Fx1
        {
            public GameObject Go;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public LineRenderer Line;
            public float Age, Life;
            public Vector3 StartScale, EndScale;
            public Color Color;
            public bool Active;
            public Transform Follow;
            public Vector3 Offset;
        }

        readonly List<Fx1> pool = new List<Fx1>();
        MaterialPropertyBlock props;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        Material fadeMat;

        void Awake()
        {
            Instance = this;
            props = new MaterialPropertyBlock();
            fadeMat = Fx.Fade(Color.white);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Fx1 Take(bool line)
        {
            foreach (var f in pool)
                if (!f.Active && (f.Line != null) == line) { f.Active = true; f.Go.SetActive(true); return f; }
            var go = new GameObject(line ? "FxLine" : "Fx");
            go.transform.SetParent(transform, false);
            var fx = new Fx1 { Go = go, Active = true };
            if (line)
            {
                fx.Line = go.AddComponent<LineRenderer>();
                fx.Line.sharedMaterial = fadeMat;
                fx.Line.shadowCastingMode = ShadowCastingMode.Off;
                fx.Line.receiveShadows = false;
                fx.Line.numCapVertices = 2;
            }
            else
            {
                fx.Filter = go.AddComponent<MeshFilter>();
                fx.Renderer = go.AddComponent<MeshRenderer>();
                fx.Renderer.sharedMaterial = fadeMat;
                fx.Renderer.shadowCastingMode = ShadowCastingMode.Off;
                fx.Renderer.receiveShadows = false;
            }
            pool.Add(fx);
            return fx;
        }

        /// <summary>A mesh at a point that scales from start to end and fades out.</summary>
        public static void Burst(Mesh mesh, Vector3 at, Quaternion rot, Vector3 startScale, Vector3 endScale, Color color, float life, Transform follow = null)
        {
            if (Instance == null) return;
            var f = Instance.Take(false);
            f.Filter.sharedMesh = mesh;
            f.Go.transform.SetPositionAndRotation(at, rot);
            f.StartScale = startScale;
            f.EndScale = endScale;
            f.Go.transform.localScale = startScale;
            f.Color = color;
            f.Age = 0f;
            f.Life = Mathf.Max(0.01f, life);
            f.Follow = follow;
            f.Offset = follow != null ? at - follow.position : Vector3.zero;
            Instance.Apply(f, 0f);
        }

        /// <summary>A jagged line between points, for chain lightning and beams.</summary>
        public static void Bolt(Vector3 a, Vector3 b, Color color, float width, float life, int kinks = 4)
        {
            if (Instance == null) return;
            var f = Instance.Take(true);
            f.Line.positionCount = kinks + 2;
            f.Line.SetPosition(0, a);
            for (int i = 1; i <= kinks; i++)
            {
                float t = i / (float)(kinks + 1);
                f.Line.SetPosition(i, Vector3.Lerp(a, b, t) + Random.insideUnitSphere * 0.35f);
            }
            f.Line.SetPosition(kinks + 1, b);
            f.Line.startWidth = width;
            f.Line.endWidth = width * 0.6f;
            f.Color = color;
            f.Age = 0f;
            f.Life = life;
            f.Follow = null;
            Instance.Apply(f, 0f);
        }

        void Apply(Fx1 f, float k)
        {
            var c = f.Color;
            c.a *= 1f - k * k;
            props.SetColor(ColorId, c);
            if (f.Renderer != null)
            {
                f.Renderer.SetPropertyBlock(props);
                f.Go.transform.localScale = Vector3.Lerp(f.StartScale, f.EndScale, 1f - (1f - k) * (1f - k));
            }
            else
            {
                f.Line.SetPropertyBlock(props);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var f in pool)
            {
                if (!f.Active) continue;
                f.Age += dt;
                if (f.Age >= f.Life)
                {
                    f.Active = false;
                    f.Go.SetActive(false);
                    continue;
                }
                if (f.Follow != null) f.Go.transform.position = f.Follow.position + f.Offset;
                Apply(f, f.Age / f.Life);
            }
        }
    }
}
