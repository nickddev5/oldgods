using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// The sky behind every scene: a gradient dome from the fog colour at the horizon to a
    /// deeper zenith, two rings of far hills that fade into the haze, and a few flat clouds.
    /// One mesh, one draw, drawn first without depth (OldGods/Sky). It follows the camera's
    /// position, so it never gets closer.
    /// </summary>
    public sealed class SkyDome : MonoBehaviour
    {
        const float Radius = 100f;
        static readonly int ZenithId = Shader.PropertyToID("_Zenith");
        static readonly int HorizonId = Shader.PropertyToID("_Horizon");
        static readonly int HillsId = Shader.PropertyToID("_Hills");
        static readonly int CloudId = Shader.PropertyToID("_Cloud");

        static Mesh mesh;
        Material material;
        Transform follow;

        /// <summary>The sky for this camera, created on first use.</summary>
        public static SkyDome For(Camera cam)
        {
            var existing = Object.FindAnyObjectByType<SkyDome>();
            if (existing != null) { existing.follow = cam.transform; return existing; }
            var shader = Shader.Find("OldGods/Sky");
            var assets = GameAssets.Load();
            Material src = assets != null && assets.Sky != null ? assets.Sky : shader != null ? new Material(shader) : null;
            if (src == null) return null;
            var go = new GameObject("Sky");
            var sky = go.AddComponent<SkyDome>();
            sky.follow = cam.transform;
            sky.material = new Material(src) { name = "Sky (run)" };
            go.AddComponent<MeshFilter>().sharedMesh = Mesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sky.material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            sky.LateUpdate();
            return sky;
        }

        /// <summary>
        /// Colours the sky from the scene's atmosphere. The horizon is the fog, so distant
        /// terrain melts into it; the zenith is the sky ambient, deepened and more saturated.
        /// </summary>
        public void SetColors(Color skyAmbient, Color fog, Color groundAmbient)
        {
            var c = SkyColors.From(skyAmbient, fog, groundAmbient);
            material.SetColor(ZenithId, c.Zenith);
            material.SetColor(HorizonId, c.Horizon);
            material.SetColor(HillsId, c.Hills);
            material.SetColor(CloudId, c.Cloud);
        }

        void LateUpdate()
        {
            if (follow != null) transform.position = follow.position;
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        static Mesh Mesh()
        {
            if (mesh != null) return mesh;
            var k = new SkyMesh();
            // Dome: rings from just below the horizon to the top. r = height up the dome.
            const int seg = 48, rings = 10;
            for (int ring = 0; ring < rings; ring++)
            {
                float e0 = Mathf.Lerp(-0.25f, 1f, ring / (float)rings) * Mathf.PI * 0.5f;
                float e1 = Mathf.Lerp(-0.25f, 1f, (ring + 1) / (float)rings) * Mathf.PI * 0.5f;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    k.QuadColors(Dir(a0, e0), Dir(a1, e0), Dir(a1, e1), Dir(a0, e1),
                        SkyVertex(e0), SkyVertex(e0), SkyVertex(e1), SkyVertex(e1));
                }
            }
            // Far hills, then nearer hills: jagged ridgelines that sink into the horizon haze.
            var rng = new System.Random(7);
            HillRing(k, rng, 96, 0.035f, 0.11f, haze: 0.65f);
            HillRing(k, rng, 64, 0.02f, 0.08f, haze: 0.35f);
            // Flat clouds, low over the horizon where the camera looks.
            for (int i = 0; i < 14; i++)
            {
                float yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
                float elev = Mathf.Lerp(0.07f, 0.26f, (float)rng.NextDouble());
                float width = Mathf.Lerp(0.18f, 0.42f, (float)rng.NextDouble());
                Cloud(k, rng, yaw, elev, width, haze: 1f - elev * 3f);
            }
            mesh = k.Build();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return mesh;
        }

        static Vector3 Dir(float yaw, float elev) =>
            new Vector3(Mathf.Cos(yaw) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Sin(yaw) * Mathf.Cos(elev)) * Radius;

        static Color SkyVertex(float elev) => new Color(Mathf.Max(0f, Mathf.Sin(elev)), 0f, 0f, 0f);

        static void HillRing(SkyMesh k, System.Random rng, int seg, float minH, float maxH, float haze)
        {
            var hill = new Color(0f, 1f, 0f, haze);
            var heights = new float[seg];
            // Two octaves of smooth bumps plus a jagged edge, as a far ridgeline.
            float p1 = (float)rng.NextDouble() * 6f, p2 = (float)rng.NextDouble() * 6f;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                float t = 0.5f + 0.3f * Mathf.Sin(a * 3f + p1) + 0.2f * Mathf.Sin(a * 7f + p2);
                heights[i] = Mathf.Lerp(minH, maxH, Mathf.Clamp01(t + ((float)rng.NextDouble() - 0.5f) * 0.25f));
            }
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                float h0 = heights[i], h1 = heights[(i + 1) % seg];
                k.QuadColors(Dir(a0, -0.1f), Dir(a1, -0.1f), Dir(a1, h1), Dir(a0, h0), hill, hill, hill, hill);
            }
        }

        static void Cloud(SkyMesh k, System.Random rng, float yaw, float elev, float width, float haze)
        {
            var col = new Color(0f, 0f, 1f, Mathf.Clamp01(haze));
            // A flat-bottomed bank of overlapping round puffs on the dome.
            int puffs = 3 + rng.Next(3);
            for (int p = 0; p < puffs; p++)
            {
                float u = (p + 0.5f) / puffs - 0.5f;
                float r = width * (0.22f + 0.18f * (float)rng.NextDouble()) * (1f - Mathf.Abs(u) * 0.8f);
                float cy = elev + r * 0.45f;
                float cx = yaw + u * width;
                const int fan = 12;
                var centre = Dir(cx, cy);
                for (int i = 0; i < fan; i++)
                {
                    float t0 = i * Mathf.PI * 2f / fan, t1 = (i + 1) * Mathf.PI * 2f / fan;
                    // Flatten the underside to the bank's base.
                    float y0 = Mathf.Max(cy + Mathf.Sin(t0) * r * 0.6f, elev);
                    float y1 = Mathf.Max(cy + Mathf.Sin(t1) * r * 0.6f, elev);
                    k.TriColors(centre, Dir(cx + Mathf.Cos(t1) * r, y1), Dir(cx + Mathf.Cos(t0) * r, y0), col, col, col);
                }
            }
        }
    }

    /// <summary>Triangles whose vertex colours are data for the sky shader, not colours.</summary>
    sealed class SkyMesh
    {
        readonly System.Collections.Generic.List<Vector3> verts = new System.Collections.Generic.List<Vector3>();
        readonly System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();

        public void TriColors(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            verts.Add(a); verts.Add(b); verts.Add(c);
            colors.Add(ca); colors.Add(cb); colors.Add(cc);
        }

        public void QuadColors(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            TriColors(a, b, c, ca, cb, cc);
            TriColors(a, c, d, ca, cc, cd);
        }

        public Mesh Build()
        {
            var m = new Mesh { name = "Sky Dome" };
            m.SetVertices(verts);
            m.SetColors(colors);
            var idx = new int[verts.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            m.SetTriangles(idx, 0);
            return m;
        }
    }

    /// <summary>The sky's four colours, derived from a scene's ambient and fog colours.</summary>
    public struct SkyColors
    {
        public Color Zenith, Horizon, Hills, Cloud;

        public static SkyColors From(Color skyAmbient, Color fog, Color groundAmbient)
        {
            Color.RGBToHSV(Color.Lerp(skyAmbient, fog, 0.3f), out float h, out float s, out float v);
            return new SkyColors
            {
                Horizon = fog,
                Zenith = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.35f + 0.12f), v * 0.82f),
                Hills = Color.Lerp(fog, groundAmbient, 0.4f),
                Cloud = Color.Lerp(fog, Color.white, 0.6f),
            };
        }
    }
}
