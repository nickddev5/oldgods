using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>Which body part a vertex belongs to; the shaders swing limbs by part.</summary>
    public enum BodyPart { Body = 0, LeftLeg = 1, RightLeg = 2, LeftArm = 3, RightArm = 4, Head = 5, Cape = 6 }

    /// <summary>
    /// Builds flat-shaded low-poly meshes from simple parts. Every face gets its own
    /// vertices so normals are per-face, and every vertex carries a colour. Vertices also
    /// carry their body part (uv0.x), the height of the knee or elbow (uv0.y) and the joint it swings around (uv1), so the shaders
    /// can animate legs and arms without a rig.
    /// </summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<Vector2> parts = new List<Vector2>();
        readonly List<Vector3> pivots = new List<Vector3>();
        readonly List<int> tris = new List<int>();

        BodyPart part;
        Vector3 pivot;
        float bend;
        // When set, rounded primitives give each vertex its own normal (smooth shading).
        System.Func<Vector3, Vector3, Vector3> normalAt;

        /// <summary>Rounded shapes (balls, limbs, lathes) shade smoothly; blocks and boxes stay faceted.</summary>
        public bool Smooth = true;

        Matrix4x4 place = Matrix4x4.identity;
        bool placing;

        /// <summary>
        /// Moves everything added from now on by this transform, so many pieces can be built
        /// into one mesh in their own local space. Identity turns it off.
        /// </summary>
        public Matrix4x4 Placement
        {
            get => place;
            set { place = value; placing = !value.isIdentity; }
        }

        public int VertexCount => verts.Count;
        public int TriangleCount => tris.Count / 3;

        /// <summary>
        /// Everything added until the next call belongs to this part, swinging around the joint.
        /// bendHeight is the height of the knee or elbow, where the limb bends; 0 for a stiff limb.
        /// </summary>
        public void Part(BodyPart p, Vector3 joint, float bendHeight = 0f)
        {
            part = p;
            pivot = joint;
            bend = bendHeight;
        }

        public void Body() => Part(BodyPart.Body, Vector3.zero);

        /// <summary>
        /// Hip height of a built mesh in its own units: the highest leg joint. Falls back to half
        /// the mesh's height when it has no legs or cannot be read.
        /// </summary>
        public static float HipHeight(Mesh mesh)
        {
            if (mesh == null) return 0.85f;
            float fallback = Mathf.Max(0.1f, mesh.bounds.size.y * 0.5f);
            if (!mesh.isReadable) return fallback;
            var p = new List<Vector2>();
            var j = new List<Vector3>();
            mesh.GetUVs(0, p);
            mesh.GetUVs(1, j);
            if (p.Count == 0 || p.Count != j.Count) return fallback;
            float hip = 0f;
            for (int i = 0; i < p.Count; i++)
                if (p[i].x > 0.5f && p[i].x < 2.5f) hip = Mathf.Max(hip, j[i].y);
            return hip > 0.05f ? hip : fallback;
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return;
            n.Normalize();
            int i = verts.Count;
            Vector3 na = n, nb = n, nc = n;
            if (normalAt != null) { na = normalAt(a, n); nb = normalAt(b, n); nc = normalAt(c, n); }
            if (placing)
            {
                a = place.MultiplyPoint3x4(a); b = place.MultiplyPoint3x4(b); c = place.MultiplyPoint3x4(c);
                na = place.MultiplyVector(na).normalized; nb = place.MultiplyVector(nb).normalized; nc = place.MultiplyVector(nc).normalized;
            }
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(na); normals.Add(nb); normals.Add(nc);
            // Vertex colours are not converted by the pipeline; author in sRGB, store linear.
            Color lin = color.linear;
            colors.Add(lin); colors.Add(lin); colors.Add(lin);
            var pp = new Vector2((float)part, bend);
            parts.Add(pp); parts.Add(pp); parts.Add(pp);
            pivots.Add(pivot); pivots.Add(pivot); pivots.Add(pivot);
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

        /// <summary>
        /// A box with its edges cut at 45 degrees (a chamfered block): reads as carved stone or
        /// plate instead of a crate. bevel is the cut size as a fraction of the smallest half-size.
        /// </summary>
        public void Block(Vector3 center, Vector3 size, Color color, float bevel = 0.3f, Quaternion? rotation = null)
        {
            Quaternion r = rotation ?? Quaternion.identity;
            Vector3 h = size * 0.5f;
            float b = Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * Mathf.Clamp01(bevel);
            // An octagonal prism along y with chamfered top and bottom rims.
            var ring = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float sx = (i == 0 || i == 1 || i == 6 || i == 7) ? 1f : -1f;
                float sz = (i < 4) ? 1f : -1f;
                bool alongX = i % 2 == 0;
                float x = alongX ? sx * (h.x - b) : sx * h.x;
                float z = alongX ? sz * h.z : sz * (h.z - b);
                ring[i] = new Vector3(x, 0f, z);
            }
            // Order the ring around the y axis.
            System.Array.Sort(ring, (p, q) => Mathf.Atan2(p.z, p.x).CompareTo(Mathf.Atan2(q.z, q.x)));
            Vector3 L(Vector3 v, float y, float inset)
            {
                var flat = new Vector3(v.x, 0f, v.z);
                var shrink = new Vector3(flat.x - Mathf.Sign(flat.x) * inset, 0f, flat.z - Mathf.Sign(flat.z) * inset);
                return center + r * new Vector3(shrink.x, y, shrink.z);
            }
            float yTop = h.y, yBot = -h.y;
            for (int i = 0; i < 8; i++)
            {
                var a = ring[i];
                var c = ring[(i + 1) % 8];
                // side band
                Quad(L(a, yBot + b, 0f), L(a, yTop - b, 0f), L(c, yTop - b, 0f), L(c, yBot + b, 0f), color * (0.92f + 0.08f * Mathf.Cos(i)));
                // chamfers
                Quad(L(a, yTop - b, 0f), L(a, yTop, b), L(c, yTop, b), L(c, yTop - b, 0f), color);
                Quad(L(a, yBot, b), L(a, yBot + b, 0f), L(c, yBot + b, 0f), L(c, yBot, b), color * 0.85f);
                // caps
                Triangle(center + r * new Vector3(0f, yTop, 0f), L(c, yTop, b), L(a, yTop, b), color);
                Triangle(center + r * new Vector3(0f, yBot, 0f), L(a, yBot, b), L(c, yBot, b), color * 0.8f);
            }
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

        /// <summary>
        /// A stack of rings along y with a radius per ring: robes, tree trunks, vases.
        /// profile is (height, radius) pairs from bottom to top; the last radius may be 0 for a point.
        /// </summary>
        public void Lathe(Vector3 baseCenter, Vector2[] profile, int sides, Color color, float squashZ = 1f, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            Quaternion inv = Quaternion.Inverse(q);
            Vector3 W(Vector3 local) => baseCenter + q * local;
            if (Smooth)
            {
                // Per profile point: the outward direction in the (radius, height) plane,
                // averaged over the segments that meet there, so rings share one normal.
                var bend = new Vector2[profile.Length];
                for (int k = 0; k < profile.Length; k++)
                {
                    Vector2 sum = Vector2.zero;
                    if (k > 0) sum += Outward(profile[k - 1], profile[k]);
                    if (k < profile.Length - 1) sum += Outward(profile[k], profile[k + 1]);
                    bend[k] = sum.sqrMagnitude > 1e-10f ? sum.normalized : new Vector2(1f, 0f);
                }
                normalAt = (v, face) =>
                {
                    var l = inv * (v - baseCenter);
                    var radial = new Vector3(l.x, 0f, l.z / Mathf.Max(0.05f, squashZ * squashZ));
                    if (radial.sqrMagnitude < 1e-8f) return face;
                    radial.Normalize();
                    int best = 0;
                    float bestD = float.MaxValue;
                    for (int k = 0; k < profile.Length; k++)
                    {
                        float d = Mathf.Abs(profile[k].x - l.y);
                        if (d < bestD) { bestD = d; best = k; }
                    }
                    var n = q * (radial * bend[best].x + Vector3.up * bend[best].y).normalized;
                    return Vector3.Dot(n, face) < 0f ? -n : n;
                };
            }
            Vector3 D(float a, float rad, float y) => W(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad * squashZ));
            for (int r = 0; r < profile.Length - 1; r++)
            {
                float y0 = profile[r].x, y1 = profile[r + 1].x;
                float r0 = profile[r].y, r1 = profile[r + 1].y;
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                    var shade = Smooth ? color : color * (0.9f + 0.1f * Mathf.Cos(a0 + 0.6f));
                    // Profiles may run downward (capes, brims); flip the winding so faces point out.
                    if (y1 >= y0)
                    {
                        if (r1 > 1e-4f && r0 > 1e-4f) Quad(D(a0, r0, y0), D(a0, r1, y1), D(a1, r1, y1), D(a1, r0, y0), shade);
                        else if (r1 <= 1e-4f) Triangle(D(a0, r0, y0), W(Vector3.up * y1), D(a1, r0, y0), shade);
                        else Triangle(W(Vector3.up * y0), D(a0, r1, y1), D(a1, r1, y1), shade);
                    }
                    else
                    {
                        if (r1 > 1e-4f && r0 > 1e-4f) Quad(D(a1, r0, y0), D(a1, r1, y1), D(a0, r1, y1), D(a0, r0, y0), shade);
                        else if (r1 <= 1e-4f) Triangle(D(a1, r0, y0), W(Vector3.up * y1), D(a0, r0, y0), shade);
                        else Triangle(W(Vector3.up * y0), D(a1, r1, y1), D(a0, r1, y1), shade);
                    }
                }
            }
            normalAt = null;
            var first = profile[0];
            if (first.y > 1e-4f)
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                    Triangle(W(Vector3.up * first.x), D(a0, first.y, first.x), D(a1, first.y, first.x), color * 0.8f);
                }
            var last = profile[profile.Length - 1];
            if (last.y > 1e-4f)
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                    Triangle(W(Vector3.up * last.x), D(a1, last.y, last.x), D(a0, last.y, last.x), color);
                }
        }

        // Outward normal of a profile segment in (radius, height) space.
        static Vector2 Outward(Vector2 from, Vector2 to)
        {
            var d = new Vector2(to.y - from.y, to.x - from.x); // (dr, dy)
            var n = new Vector2(d.y, -d.x);                     // rotate: (dy, -dr)
            if (to.x < from.x) n = -n;                          // downward profiles
            return n.sqrMagnitude > 1e-12f ? n.normalized : Vector2.zero;
        }

        /// <summary>
        /// A faceted ellipsoid (a low-poly sphere): heads, muscles, boulders. jitter roughens it
        /// deterministically for rocks.
        /// </summary>
        public void Ball(Vector3 center, Vector3 radii, Color color, int segments = 8, int rings = 5, Quaternion? rotation = null, float jitter = 0f, int seed = 0)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            var rnd = new System.Random(seed);
            if (Smooth && jitter <= 0f)
            {
                var inv = Quaternion.Inverse(q);
                normalAt = (v, face) =>
                {
                    var local = inv * (v - center);
                    var n = new Vector3(local.x / Mathf.Max(1e-4f, radii.x * radii.x), local.y / Mathf.Max(1e-4f, radii.y * radii.y), local.z / Mathf.Max(1e-4f, radii.z * radii.z));
                    return n.sqrMagnitude > 1e-10f ? (q * n).normalized : face;
                };
            }
            var grid = new Vector3[rings + 1, segments];
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings;
                float phi = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, v);
                for (int x = 0; x < segments; x++)
                {
                    float theta = x * Mathf.PI * 2f / segments;
                    var p = new Vector3(Mathf.Cos(phi) * Mathf.Cos(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(theta));
                    if (jitter > 0f && y > 0 && y < rings) p *= 1f + ((float)rnd.NextDouble() * 2f - 1f) * jitter;
                    grid[y, x] = center + q * Vector3.Scale(p, radii);
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segments; x++)
                {
                    int x1 = (x + 1) % segments;
                    var shade = Smooth ? color : color * (0.85f + 0.15f * (y / (float)rings));
                    Vector3 a = grid[y, x], b = grid[y + 1, x], c = grid[y + 1, x1], d = grid[y, x1];
                    if (y == 0) Triangle(a, b, c, shade);
                    else if (y == rings - 1) Triangle(a, b, d, shade);
                    else Quad(a, b, c, d, shade);
                }
            normalAt = null;
        }

        /// <summary>A tapered limb between two points with flat ends: arms, legs, horns, branches.</summary>
        public void Limb(Vector3 from, Vector3 to, float radiusFrom, float radiusTo, Color color, int sides = 6)
        {
            Vector3 axis = to - from;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            Vector3 dir = axis / len;
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(side, dir);
            for (int i = 0; i < sides; i++)
            {
                if (Smooth)
                    normalAt = (v, face) =>
                    {
                        var rel = v - from;
                        var radial = rel - dir * Vector3.Dot(rel, dir);
                        if (radial.sqrMagnitude < 1e-10f) return face;
                        float slope = (radiusFrom - radiusTo) / len;
                        return (radial.normalized + dir * slope).normalized;
                    };
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 o0 = side * Mathf.Cos(a0) + up * Mathf.Sin(a0);
                Vector3 o1 = side * Mathf.Cos(a1) + up * Mathf.Sin(a1);
                var shade = Smooth ? color : color * (0.88f + 0.12f * Mathf.Sin(a0 + 0.8f));
                Quad(from + o0 * radiusFrom, to + o0 * radiusTo, to + o1 * radiusTo, from + o1 * radiusFrom, shade);
                normalAt = null;
                if (radiusTo > 1e-4f) Triangle(to, to + o1 * radiusTo, to + o0 * radiusTo, color);
                if (radiusFrom > 1e-4f) Triangle(from, from + o0 * radiusFrom, from + o1 * radiusFrom, color * 0.85f);
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
            mesh.SetUVs(0, parts);
            mesh.SetUVs(1, pivots);
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
