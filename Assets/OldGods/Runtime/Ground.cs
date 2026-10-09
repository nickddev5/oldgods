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

        public static void Set(HeightField field, float rimWidth)
        {
            Field = field;
            RimWidth = rimWidth;
        }

        public static void Clear() => Field = null;

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
        public float CliffSlope = 38f;
    }

    /// <summary>Turns a height field into chunked, flat-shaded meshes with colliders.</summary>
    public static class TerrainMesh
    {
        public const int ChunkCells = 32;

        public static GameObject Build(HeightField f, Material material, GroundPalette palette, float hillHeight, Transform parent)
        {
            var root = new GameObject("Ground");
            root.transform.SetParent(parent, false);
            root.layer = Layers.Ground;
            var noise = new System.Random(1);
            for (int cz = 0; cz < f.Cells; cz += ChunkCells)
                for (int cx = 0; cx < f.Cells; cx += ChunkCells)
                {
                    var mesh = BuildChunk(f, cx, cz, Mathf.Min(ChunkCells, f.Cells - cx), Mathf.Min(ChunkCells, f.Cells - cz), palette, hillHeight, noise);
                    var go = new GameObject($"Chunk_{cx}_{cz}");
                    go.layer = Layers.Ground;
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = material;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
            return root;
        }

        static Mesh BuildChunk(HeightField f, int x0, int z0, int w, int h, GroundPalette pal, float hillHeight, System.Random noise)
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
                else col = Color.Lerp(pal.Low, pal.High, Mathf.Clamp01(cy / Mathf.Max(1f, hillHeight)));
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
