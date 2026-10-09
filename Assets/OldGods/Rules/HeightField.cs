using System;

namespace OldGods.Rules
{
    /// <summary>Seeded 2D gradient noise. Same seed, same field, on every platform.</summary>
    public sealed class Noise2D
    {
        readonly int[] perm = new int[512];
        static readonly float[] GradX = { 1f, -1f, 0f, 0f, 0.7071f, -0.7071f, 0.7071f, -0.7071f };
        static readonly float[] GradZ = { 0f, 0f, 1f, -1f, 0.7071f, 0.7071f, -0.7071f, -0.7071f };

        public Noise2D(Rng rng)
        {
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            rng.Shuffle(p);
            for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        float Grad(int hash, float x, float z)
        {
            int g = hash & 7;
            return GradX[g] * x + GradZ[g] * z;
        }

        /// <summary>Gradient noise, roughly in [-1, 1].</summary>
        public float Sample(float x, float z)
        {
            int xi = (int)Math.Floor(x), zi = (int)Math.Floor(z);
            float xf = x - xi, zf = z - zi;
            int X = xi & 255, Z = zi & 255;
            int aa = perm[perm[X] + Z], ab = perm[perm[X] + Z + 1];
            int ba = perm[perm[X + 1] + Z], bb = perm[perm[X + 1] + Z + 1];
            float u = Fade(xf), v = Fade(zf);
            float x1 = Lerp(Grad(aa, xf, zf), Grad(ba, xf - 1f, zf), u);
            float x2 = Lerp(Grad(ab, xf, zf - 1f), Grad(bb, xf - 1f, zf - 1f), u);
            return Lerp(x1, x2, v) * 1.414f;
        }

        /// <summary>Fractal sum of octaves, normalised to roughly [-1, 1].</summary>
        public float Fbm(float x, float z, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Sample(x * freq + o * 17.3f, z * freq - o * 9.1f) * amp;
                norm += amp;
                amp *= gain;
                freq *= lacunarity;
            }
            return norm > 0f ? sum / norm : 0f;
        }
    }

    /// <summary>
    /// A square grid of heights over the ground plane. Each cell is split into two
    /// triangles along the same diagonal the terrain mesh uses, so Sample returns the
    /// exact height of the rendered, flat-shaded surface.
    /// </summary>
    public sealed class HeightField
    {
        public readonly int Cells;        // cells per side
        public readonly float CellSize;
        public readonly float OriginX, OriginZ;
        public readonly float[] Heights;  // (Cells + 1)^2, row-major by z

        public HeightField(int cells, float cellSize, float originX, float originZ)
        {
            if (cells < 1) throw new ArgumentOutOfRangeException(nameof(cells));
            Cells = cells;
            CellSize = cellSize;
            OriginX = originX;
            OriginZ = originZ;
            Heights = new float[(cells + 1) * (cells + 1)];
        }

        public float Size => Cells * CellSize;
        public int Stride => Cells + 1;

        public float this[int ix, int iz]
        {
            get => Heights[iz * Stride + ix];
            set => Heights[iz * Stride + ix] = value;
        }

        public bool Contains(float x, float z) =>
            x >= OriginX && z >= OriginZ && x <= OriginX + Size && z <= OriginZ + Size;

        public float MinX => OriginX;
        public float MinZ => OriginZ;
        public float MaxX => OriginX + Size;
        public float MaxZ => OriginZ + Size;

        /// <summary>Height at a world point, clamped to the field's edge.</summary>
        public float Sample(float x, float z)
        {
            float fx = (x - OriginX) / CellSize, fz = (z - OriginZ) / CellSize;
            if (fx < 0f) fx = 0f; else if (fx > Cells) fx = Cells;
            if (fz < 0f) fz = 0f; else if (fz > Cells) fz = Cells;
            int ix = Math.Min((int)fx, Cells - 1), iz = Math.Min((int)fz, Cells - 1);
            float u = fx - ix, v = fz - iz;
            float h00 = this[ix, iz], h10 = this[ix + 1, iz], h01 = this[ix, iz + 1], h11 = this[ix + 1, iz + 1];
            // Diagonal from (0,0) to (1,1): lower-right triangle when u >= v.
            if (u >= v) return h00 + (h10 - h00) * u + (h11 - h10) * v;
            return h00 + (h11 - h01) * u + (h01 - h00) * v;
        }

        /// <summary>Surface normal (x, y, z) of the triangle under the point.</summary>
        public void Normal(float x, float z, out float nx, out float ny, out float nz)
        {
            float e = CellSize * 0.25f;
            float hL = Sample(x - e, z), hR = Sample(x + e, z), hD = Sample(x, z - e), hU = Sample(x, z + e);
            nx = hL - hR;
            ny = 2f * e;
            nz = hD - hU;
            float m = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            nx /= m; ny /= m; nz /= m;
        }

        /// <summary>Slope angle in degrees at the point.</summary>
        public float SlopeDegrees(float x, float z)
        {
            Normal(x, z, out _, out float ny, out _);
            return (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, ny))) * 180.0 / Math.PI);
        }
    }
}
