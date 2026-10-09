using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// Builds flat-shaded low-poly meshes from simple parts. Every face gets its own
    /// vertices so normals are per-face, and every vertex carries a colour.
    /// </summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> tris = new List<int>();

        public int VertexCount => verts.Count;

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            // Vertex colours are not converted by the pipeline; author in sRGB, store linear.
            Color lin = color.linear;
            colors.Add(lin); colors.Add(lin); colors.Add(lin);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        /// <summary>Quad a-b-c-d, wound clockwise when seen from the front (Unity's front face).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Triangle(a, b, c, color);
            Triangle(a, c, d, color);
        }

        /// <summary>A box, optionally tapered: bottom is size.x by size.z, top is scaled by topScale.</summary>
        public void Box(Vector3 center, Vector3 size, Color color, float topScale = 1f, Quaternion? rotation = null)
        {
            Quaternion r = rotation ?? Quaternion.identity;
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z)
            {
                float s = y > 0f ? topScale : 1f;
                return center + r * new Vector3(x * h.x * s, y * h.y, z * h.z * s);
            }
            Vector3 b0 = P(-1, -1, -1), b1 = P(1, -1, -1), b2 = P(1, -1, 1), b3 = P(-1, -1, 1);
            Vector3 t0 = P(-1, 1, -1), t1 = P(1, 1, -1), t2 = P(1, 1, 1), t3 = P(-1, 1, 1);
            Quad(t0, t3, t2, t1, color);          // top
            Quad(b0, b1, b2, b3, color * 0.85f);  // bottom
            Quad(b0, t0, t1, b1, color);          // -z
            Quad(b1, t1, t2, b2, color * 0.95f);  // +x
            Quad(b2, t2, t3, b3, color);          // +z
            Quad(b3, t3, t0, b0, color * 0.95f);  // -x
        }

        /// <summary>An n-sided prism (a low-poly cylinder) standing on y.</summary>
        public void Prism(Vector3 baseCenter, float radius, float height, int sides, Color color, float topScale = 1f)
        {
            var top = baseCenter + Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = baseCenter + d0 * radius, b1 = baseCenter + d1 * radius;
                Vector3 t0 = top + d0 * radius * topScale, t1 = top + d1 * radius * topScale;
                Quad(b0, t0, t1, b1, color * (0.9f + 0.1f * Mathf.Cos(a0)));
                if (topScale > 0f) Triangle(top, t1, t0, color);
                Triangle(baseCenter, b0, b1, color * 0.8f);
            }
        }

        /// <summary>A faceted ball: an octahedron, optionally squashed.</summary>
        public void Gem(Vector3 center, Vector3 radii, Color color)
        {
            Vector3 up = center + Vector3.up * radii.y, down = center - Vector3.up * radii.y;
            var ring = new[]
            {
                center + Vector3.right * radii.x, center + Vector3.forward * radii.z,
                center - Vector3.right * radii.x, center - Vector3.forward * radii.z,
            };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = ring[i], b = ring[(i + 1) % 4];
                Triangle(up, b, a, color);
                Triangle(down, a, b, color * 0.8f);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>Placeholder low-poly models until real art lands in milestone 9.</summary>
    public static class PlaceholderMeshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = make();
            cache[key] = m;
            return m;
        }

        /// <summary>
        /// A hunched husk about 1.6 units tall, feet at y = 0, facing +z. Legs sit at
        /// |x| > 0.05 and below y = 0.7 so the horde shader can swing them.
        /// </summary>
        public static Mesh Husk() => Cached("husk", () =>
        {
            var k = new MeshKit();
            var skin = new Color(0.85f, 0.85f, 0.85f);
            var dark = new Color(0.55f, 0.55f, 0.55f);
            k.Box(new Vector3(-0.16f, 0.35f, 0f), new Vector3(0.18f, 0.7f, 0.2f), dark, 0.9f);
            k.Box(new Vector3(0.16f, 0.35f, 0f), new Vector3(0.18f, 0.7f, 0.2f), dark, 0.9f);
            k.Box(new Vector3(0f, 1.02f, 0.04f), new Vector3(0.56f, 0.66f, 0.34f), skin, 1.25f, Quaternion.Euler(12f, 0f, 0f));
            k.Box(new Vector3(-0.38f, 0.95f, 0.12f), new Vector3(0.14f, 0.6f, 0.14f), dark, 0.8f, Quaternion.Euler(-35f, 0f, 8f));
            k.Box(new Vector3(0.38f, 0.95f, 0.12f), new Vector3(0.14f, 0.6f, 0.14f), dark, 0.8f, Quaternion.Euler(-35f, 0f, -8f));
            k.Gem(new Vector3(0f, 1.48f, 0.14f), new Vector3(0.17f, 0.17f, 0.17f), skin);
            return k.Build("Husk");
        });

        /// <summary>The player stand-in: a robed figure about 1.8 tall with a pale mask, facing +z.</summary>
        public static Mesh God() => Cached("god", () =>
        {
            var k = new MeshKit();
            var robe = new Color(0.9f, 0.9f, 0.9f);
            var trim = new Color(0.7f, 0.7f, 0.7f);
            k.Prism(Vector3.zero, 0.42f, 1.15f, 7, robe, 0.55f);
            k.Box(new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 0.35f, 0.32f), trim, 0.8f);
            k.Gem(new Vector3(0f, 1.66f, 0.02f), new Vector3(0.17f, 0.2f, 0.17f), Color.white);
            k.Box(new Vector3(0f, 1.66f, 0.17f), new Vector3(0.18f, 0.12f, 0.04f), new Color(1f, 0.85f, 0.4f));
            return k.Build("GodPlaceholder");
        });

        public static Mesh XpGem() => Cached("xpgem", () =>
        {
            var k = new MeshKit();
            k.Gem(Vector3.zero, new Vector3(0.18f, 0.3f, 0.18f), Color.white);
            return k.Build("XpGem");
        });

        public static Mesh Shard() => Cached("shard", () =>
        {
            var k = new MeshKit();
            k.Gem(Vector3.zero, new Vector3(0.12f, 0.12f, 0.45f), Color.white);
            return k.Build("Shard");
        });

        /// <summary>Placeholder boss bodies, about 1.7 units tall before the boss's scale, facing +z.</summary>
        public static Mesh Boss(BossModel model) => Cached("boss" + model, () =>
        {
            var k = new MeshKit();
            var main = new Color(0.9f, 0.9f, 0.9f);
            var dark = new Color(0.6f, 0.6f, 0.6f);
            switch (model)
            {
                case BossModel.Warden:
                    k.Box(new Vector3(-0.25f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.35f), dark);
                    k.Box(new Vector3(0.25f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.35f), dark);
                    k.Box(new Vector3(0f, 0.95f, 0f), new Vector3(1f, 0.8f, 0.6f), main, 1.15f);
                    k.Box(new Vector3(-0.7f, 0.85f, 0.1f), new Vector3(0.35f, 0.9f, 0.35f), dark, 1.2f);
                    k.Box(new Vector3(0.7f, 0.85f, 0.1f), new Vector3(0.35f, 0.9f, 0.35f), dark, 1.2f);
                    k.Box(new Vector3(0f, 1.52f, 0.1f), new Vector3(0.38f, 0.32f, 0.38f), main);
                    k.Box(new Vector3(0f, 1.52f, 0.3f), new Vector3(0.24f, 0.06f, 0.02f), new Color(1f, 0.6f, 0.2f));
                    break;
                case BossModel.Stag:
                    k.Box(new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.5f, 1.3f), main, 0.9f);
                    foreach (var (x, z) in new[] { (-0.22f, 0.45f), (0.22f, 0.45f), (-0.22f, -0.45f), (0.22f, -0.45f) })
                        k.Box(new Vector3(x, 0.3f, z), new Vector3(0.14f, 0.6f, 0.14f), dark);
                    k.Box(new Vector3(0f, 1.25f, 0.65f), new Vector3(0.28f, 0.5f, 0.28f), main, 0.8f, Quaternion.Euler(-25f, 0f, 0f));
                    k.Box(new Vector3(0f, 1.5f, 0.85f), new Vector3(0.24f, 0.22f, 0.4f), main);
                    k.Box(new Vector3(-0.3f, 1.85f, 0.75f), new Vector3(0.06f, 0.6f, 0.06f), dark, 1f, Quaternion.Euler(0f, 0f, 30f));
                    k.Box(new Vector3(0.3f, 1.85f, 0.75f), new Vector3(0.06f, 0.6f, 0.06f), dark, 1f, Quaternion.Euler(0f, 0f, -30f));
                    break;
                case BossModel.Mother:
                    k.Prism(Vector3.zero, 0.6f, 1.2f, 8, dark, 0.45f);
                    k.Box(new Vector3(0f, 1.35f, 0f), new Vector3(0.5f, 0.4f, 0.4f), main, 0.8f);
                    k.Gem(new Vector3(0f, 1.75f, 0f), new Vector3(0.22f, 0.26f, 0.22f), main);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        k.Box(new Vector3(Mathf.Cos(a) * 0.55f, 0.15f, Mathf.Sin(a) * 0.55f), new Vector3(0.12f, 0.3f, 0.5f), dark, 0.5f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
                    }
                    break;
                default:
                    k.Prism(Vector3.zero, 0.5f, 0.4f, 6, dark, 0.8f);
                    k.Gem(new Vector3(0f, 1.05f, 0f), new Vector3(0.6f, 0.65f, 0.6f), main);
                    k.Gem(new Vector3(0f, 1.05f, 0.45f), new Vector3(0.12f, 0.12f, 0.12f), new Color(1f, 0.9f, 0.5f));
                    k.Box(new Vector3(-0.85f, 1.1f, 0f), new Vector3(0.25f, 0.25f, 0.25f), dark);
                    k.Box(new Vector3(0.85f, 1.1f, 0f), new Vector3(0.25f, 0.25f, 0.25f), dark);
                    break;
            }
            return k.Build("Boss" + model);
        });

        /// <summary>A standing portal ring about 4 units tall, facing +z.</summary>
        public static Mesh Portal() => Cached("portal", () =>
        {
            var k = new MeshKit();
            var c = Color.white;
            int seg = 14;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                var p0 = new Vector3(Mathf.Cos(a0) * 1.6f, 2f + Mathf.Sin(a0) * 2f, 0f);
                var p1 = new Vector3(Mathf.Cos(a1) * 1.6f, 2f + Mathf.Sin(a1) * 2f, 0f);
                var mid = (p0 + p1) * 0.5f;
                k.Box(mid, new Vector3(Vector3.Distance(p0, p1) + 0.05f, 0.35f, 0.35f), c, 1f,
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg));
            }
            return k.Build("Portal");
        });

        /// <summary>A standing stone slab, the boss gate's frame.</summary>
        public static Mesh Monolith() => Cached("monolith", () =>
        {
            var k = new MeshKit();
            k.Box(new Vector3(-1.4f, 1.6f, 0f), new Vector3(0.7f, 3.2f, 0.8f), Color.white, 0.85f);
            k.Box(new Vector3(1.4f, 1.6f, 0f), new Vector3(0.7f, 3.2f, 0.8f), Color.white, 0.85f);
            k.Box(new Vector3(0f, 3.45f, 0f), new Vector3(3.9f, 0.6f, 0.95f), Color.white);
            k.Box(new Vector3(0f, 0.1f, 0f), new Vector3(4.2f, 0.2f, 1.6f), new Color(0.8f, 0.8f, 0.8f));
            return k.Build("Monolith");
        });

        /// <summary>A low-poly conifer about 4 units tall.</summary>
        public static Mesh Tree() => Cached("tree", () =>
        {
            var k = new MeshKit();
            k.Prism(Vector3.zero, 0.18f, 1.2f, 5, new Color(0.55f, 0.42f, 0.32f));
            k.Prism(new Vector3(0f, 0.9f, 0f), 1.2f, 1.7f, 6, Color.white, 0f);
            k.Prism(new Vector3(0f, 2.1f, 0f), 0.85f, 1.6f, 6, new Color(0.92f, 0.92f, 0.92f), 0f);
            return k.Build("Tree");
        });

        public static Mesh Rock() => Cached("rock", () =>
        {
            var k = new MeshKit();
            k.Prism(Vector3.zero, 1f, 1.4f, 6, Color.white, 0.55f);
            return k.Build("Rock");
        });
    }
}
