using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// The current map's ground. Enemies, pickups and placement read heights from the
    /// height field directly (cheap, exact); the player collides with the mesh.
    /// </summary>
    public static class Ground
    {
        public static HeightField Field { get; private set; }
        public static float RimWidth { get; private set; }
        /// <summary>The map's set pieces; null on the final arena.</summary>
        public static LevelLayout Layout { get; private set; }
        /// <summary>Solid dressing the horde walks around; null when there is none.</summary>
        public static Obstacles Obstacles { get; private set; }
        /// <summary>The horde's paths to the player over this map.</summary>
        public static FlowField Flow { get; private set; }

        /// <summary>Sets the current map. Call after the layout's obstacles are complete; the flow field is built from them.</summary>
        public static void Set(HeightField field, float rimWidth, LevelLayout layout = null)
        {
            Field = field;
            RimWidth = rimWidth;
            Layout = layout;
            Obstacles = layout?.Obstacles;
            Flow = field != null ? FlowField.FromTerrain(field, Obstacles, rimWidth) : null;
        }

        public static void Clear()
        {
            Field = null;
            Layout = null;
            Obstacles = null;
            Flow = null;
        }

        public static float Height(float x, float z) => Field != null ? Field.Sample(x, z) : 0f;

        public static Vector3 Snap(Vector3 p) => new Vector3(p.x, Height(p.x, p.z), p.z);

        public static Vector3 Normal(float x, float z)
        {
            if (Field == null) return Vector3.up;
            Field.Normal(x, z, out float nx, out float ny, out float nz);
            return new Vector3(nx, ny, nz);
        }

        /// <summary>Clamps a point into the playable square inside the rim.</summary>
        public static Vector3 ClampToPlayable(Vector3 p, float margin = 0f)
        {
            if (Field == null) return p;
            float m = RimWidth + margin;
            p.x = Mathf.Clamp(p.x, Field.MinX + m, Field.MaxX - m);
            p.z = Mathf.Clamp(p.z, Field.MinZ + m, Field.MaxZ - m);
            return p;
        }
    }

    /// <summary>Palette for a biome's ground, by height band and slope.</summary>
    [System.Serializable]
    public sealed class GroundPalette
    {
        public Color Low = new Color(0.36f, 0.45f, 0.30f);
        public Color High = new Color(0.55f, 0.58f, 0.40f);
        public Color Cliff = new Color(0.45f, 0.42f, 0.38f);
        public Color Rim = new Color(0.32f, 0.30f, 0.28f);
        /// <summary>The old road's worn paving.</summary>
        public Color Road = new Color(0.56f, 0.53f, 0.48f);
        /// <summary>Temple floors and fort tops.</summary>
        public Color Stone = new Color(0.6f, 0.58f, 0.54f);
        /// <summary>The biome's own mark: meadow in a stone circle, embers, weed.</summary>
        public Color Accent = new Color(0.52f, 0.56f, 0.34f);
        /// <summary>Broad patches mixed into open ground (dry grass, bare earth) so it is not one flat colour.</summary>
        public Color Patch = new Color(0.55f, 0.52f, 0.36f);
        public float PatchAmount = 0.5f;
        [Header("Clutter")]
        [Tooltip("Tufts, flowers and pebbles per 100 square metres of open ground.")]
        public float ClutterDensity = 6f;
        public Color Tuft = new Color(0.42f, 0.55f, 0.3f);
        public Color Flower = new Color(0.85f, 0.8f, 0.55f);
        public Color Pebble = new Color(0.62f, 0.6f, 0.56f);
        public float CliffSlope = 38f;
    }

    /// <summary>Turns a height field into chunked, flat-shaded meshes with colliders.</summary>
    public static class TerrainMesh
    {
        public const int ChunkCells = 32;

        public static GameObject Build(HeightField f, Material material, GroundPalette palette, float hillHeight, Transform parent, LevelLayout layout = null, float waterLevel = -1000f)
        {
            var root = new GameObject("Ground");
            root.transform.SetParent(parent, false);
            root.layer = Layers.Ground;
            var noise = new System.Random(1);
            patches = new Noise2D(new Rng(77));
            for (int cz = 0; cz < f.Cells; cz += ChunkCells)
                for (int cx = 0; cx < f.Cells; cx += ChunkCells)
                {
                    var mesh = BuildChunk(f, cx, cz, Mathf.Min(ChunkCells, f.Cells - cx), Mathf.Min(ChunkCells, f.Cells - cz), palette, hillHeight, noise, layout);
                    var go = new GameObject($"Chunk_{cx}_{cz}");
                    go.layer = Layers.Ground;
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = material;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;

                    var clutter = BuildClutter(f, cx, cz, Mathf.Min(ChunkCells, f.Cells - cx), Mathf.Min(ChunkCells, f.Cells - cz), palette, layout, waterLevel);
                    if (clutter != null)
                    {
                        var cgo = new GameObject("Clutter");
                        cgo.transform.SetParent(go.transform, false);
                        cgo.AddComponent<MeshFilter>().sharedMesh = clutter;
                        var cr = cgo.AddComponent<MeshRenderer>();
                        cr.sharedMaterial = material;
                        cr.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
            return root;
        }

        static Noise2D patches;

        /// <summary>
        /// Small detail merged into one mesh per chunk, with no collider: grass tufts, a few
        /// flowers and pebbles on open, gentle ground. Seeded per chunk so it is the same every visit.
        /// </summary>
        static Mesh BuildClutter(HeightField f, int x0, int z0, int w, int h, GroundPalette pal, LevelLayout layout, float waterLevel)
        {
            if (pal.ClutterDensity <= 0f) return null;
            var rnd = new System.Random(x0 * 7919 + z0 * 104729 + 13);
            float area = w * h * f.CellSize * f.CellSize;
            int count = Mathf.RoundToInt(area / 100f * pal.ClutterDensity);
            var k = new MeshKit { Smooth = false };
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int i = 0; i < count; i++)
            {
                float x = f.OriginX + (x0 + R(0f, w)) * f.CellSize, z = f.OriginZ + (z0 + R(0f, h)) * f.CellSize;
                if (!TerrainGenerator.InPlayableArea(f, x, z, Ground.RimWidth)) continue;
                if (f.SlopeDegrees(x, z) > 28f) continue;
                float y = f.Sample(x, z);
                if (y < waterLevel + 0.2f) continue;
                var paint = layout != null ? layout.PaintAt(x, z) : GroundPaint.None;
                if (paint == GroundPaint.Road || paint == GroundPaint.Stone) continue;
                if (layout != null && layout.Obstacles.Overlaps(x, z, 0.3f)) continue;
                var at = new Vector3(x, y - 0.02f, z);
                float roll = R(0f, 1f);
                if (roll < 0.72f)
                {
                    // A tuft: a fan of thin blades.
                    int blades = 3 + rnd.Next(3);
                    float tall = R(0.25f, 0.55f);
                    Color c = pal.Tuft * R(0.85f, 1.15f);
                    for (int b = 0; b < blades; b++)
                    {
                        float a = R(0f, Mathf.PI * 2f);
                        var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        var side = new Vector3(-dir.z, 0f, dir.x) * 0.06f;
                        var tip = at + dir * R(0.08f, 0.2f) + Vector3.up * tall * R(0.7f, 1.1f);
                        k.Triangle(at - side, tip, at + side, c);
                        k.Triangle(at + side, tip, at - side, c * 0.85f);
                    }
                    if (roll < 0.1f)
                        k.Gem(at + Vector3.up * tall * 0.9f, new Vector3(0.07f, 0.06f, 0.07f), pal.Flower * R(0.9f, 1.1f));
                }
                else
                {
                    float s = R(0.08f, 0.22f);
                    k.Gem(at + Vector3.up * s * 0.25f, new Vector3(s, s * 0.45f, s * R(0.7f, 1f)), pal.Pebble * R(0.85f, 1.1f));
                }
            }
            return k.VertexCount > 0 ? k.Build("Clutter") : null;
        }

        static Mesh BuildChunk(HeightField f, int x0, int z0, int w, int h, GroundPalette pal, float hillHeight, System.Random noise, LevelLayout layout)
        {
            int tris = w * h * 2;
            var verts = new List<Vector3>(tris * 3);
            var normals = new List<Vector3>(tris * 3);
            var colors = new List<Color>(tris * 3);
            var idx = new List<int>(tris * 3);

            Vector3 V(int ix, int iz) => new Vector3(f.OriginX + ix * f.CellSize, f[ix, iz], f.OriginZ + iz * f.CellSize);

            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                float slope = Vector3.Angle(n, Vector3.up);
                float cy = (a.y + b.y + c.y) / 3f;
                float cx = (a.x + b.x + c.x) / 3f, cz = (a.z + b.z + c.z) / 3f;
                Color col;
                if (!TerrainGenerator.InPlayableArea(f, cx, cz, Ground.RimWidth * 0.7f)) col = pal.Rim;
                else if (slope > pal.CliffSlope) col = pal.Cliff;
                else
                {
                    var paint = layout != null ? layout.PaintAt(cx, cz) : GroundPaint.None;
                    col = paint == GroundPaint.Road ? pal.Road
                        : paint == GroundPaint.Stone ? pal.Stone
                        : paint == GroundPaint.Accent ? pal.Accent
                        : Color.Lerp(pal.Low, pal.High, Mathf.Clamp01(cy / Mathf.Max(1f, hillHeight)));
                    if (paint == GroundPaint.None && pal.PatchAmount > 0f)
                    {
                        float pn = patches.Fbm(cx / 28f, cz / 28f, 2) * 1.6f;
                        col = Color.Lerp(col, pal.Patch, Mathf.Clamp01(pn) * pal.PatchAmount);
                    }
                }
                float jitter = 0.94f + (float)noise.NextDouble() * 0.08f;
                col = new Color(col.r * jitter, col.g * jitter, col.b * jitter, 1f).linear;
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                normals.Add(n); normals.Add(n); normals.Add(n);
                colors.Add(col); colors.Add(col); colors.Add(col);
                idx.Add(i); idx.Add(i + 1); idx.Add(i + 2);
            }

            for (int iz = z0; iz < z0 + h; iz++)
                for (int ix = x0; ix < x0 + w; ix++)
                {
                    Vector3 p00 = V(ix, iz), p10 = V(ix + 1, iz), p01 = V(ix, iz + 1), p11 = V(ix + 1, iz + 1);
                    // Same diagonal as HeightField.Sample: (0,0) to (1,1). Clockwise from above.
                    Tri(p00, p11, p10);
                    Tri(p00, p01, p11);
                }

            var mesh = new Mesh { name = $"Ground_{x0}_{z0}" };
            if (verts.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public static class Layers
    {
        public const int Default = 0;
        public const int Ground = 0;
        public const int Player = 2; // Ignore Raycast, so camera and probes skip the player
    }
}
