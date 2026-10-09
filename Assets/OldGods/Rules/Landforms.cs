using System;

namespace OldGods.Rules
{
    /// <summary>
    /// Shapes stamped into a height field after the noise: plateaus with cliff sides,
    /// ramps up to them, mounds, ravines and flattened strips. Heights are absolute.
    /// </summary>
    public static class Landforms
    {
        static float Smooth(float t) => t <= 0f ? 0f : t >= 1f ? 1f : t * t * (3f - 2f * t);

        /// <summary>
        /// A flat-topped ellipse at height top. The side drops to the old ground over edge
        /// metres, so a short edge makes a cliff and a long one a slope.
        /// </summary>
        public static void Plateau(HeightField f, float cx, float cz, float rx, float rz, float yawRad, float top, float edge)
        {
            float cos = (float)Math.Cos(yawRad), sin = (float)Math.Sin(yawRad);
            float reach = Math.Max(rx, rz) + edge + f.CellSize;
            ForEachNear(f, cx, cz, reach, (ix, iz, x, z) =>
            {
                float dx = x - cx, dz = z - cz;
                float lx = dx * cos - dz * sin, lz = dx * sin + dz * cos;
                // Distance outside the ellipse, roughly in metres.
                float q = (float)Math.Sqrt(lx * lx / (rx * rx) + lz * lz / (rz * rz));
                float outside = (q - 1f) * Math.Min(rx, rz);
                float h = f[ix, iz];
                if (outside <= 0f) f[ix, iz] = top;
                else if (outside < edge) f[ix, iz] = h + (top - h) * (1f - outside / edge);
            });
        }

        /// <summary>A flat-topped rectangle at height top, half-sizes hx by hz, sides falling over edge metres.</summary>
        public static void Platform(HeightField f, float cx, float cz, float hx, float hz, float yawRad, float top, float edge)
        {
            float cos = (float)Math.Cos(yawRad), sin = (float)Math.Sin(yawRad);
            float reach = (float)Math.Sqrt(hx * hx + hz * hz) + edge + f.CellSize;
            ForEachNear(f, cx, cz, reach, (ix, iz, x, z) =>
            {
                float dx = x - cx, dz = z - cz;
                float lx = dx * cos - dz * sin, lz = dx * sin + dz * cos;
                float outside = Math.Max(Math.Abs(lx) - hx, Math.Abs(lz) - hz);
                float h = f[ix, iz];
                if (outside <= 0f) f[ix, iz] = top;
                else if (outside < edge) f[ix, iz] = h + (top - h) * (1f - outside / edge);
            });
        }

        /// <summary>
        /// A straight ramp from (ax, az) on the ground to (bx, bz) at height top, width metres wide.
        /// The ground under the ramp is replaced; its sides fall to the old ground over one cell.
        /// </summary>
        public static void Ramp(HeightField f, float ax, float az, float bx, float bz, float top, float width)
        {
            float ha = f.Sample(ax, az);
            float lx = bx - ax, lz = bz - az;
            float len = (float)Math.Sqrt(lx * lx + lz * lz);
            if (len < 0.01f) return;
            lx /= len; lz /= len;
            float half = width * 0.5f, blend = f.CellSize;
            float midX = (ax + bx) * 0.5f, midZ = (az + bz) * 0.5f;
            ForEachNear(f, midX, midZ, len * 0.5f + half + blend + f.CellSize, (ix, iz, x, z) =>
            {
                float px = x - ax, pz = z - az;
                float along = px * lx + pz * lz;
                if (along < -blend || along > len + blend) return;
                float across = Math.Abs(px * -lz + pz * lx);
                if (across > half + blend) return;
                float s = Math.Max(0f, Math.Min(1f, along / len));
                float target = ha + (top - ha) * s;
                float t = across <= half ? 1f : 1f - (across - half) / blend;
                if (along < 0f) t *= 1f + along / blend;
                else if (along > len) t *= 1f - (along - len) / blend;
                float h = f[ix, iz];
                f[ix, iz] = h + (target - h) * t;
            });
        }

        /// <summary>A rounded mound (a barrow): adds height with a cosine profile.</summary>
        public static void Mound(HeightField f, float cx, float cz, float radius, float height)
        {
            ForEachNear(f, cx, cz, radius + f.CellSize, (ix, iz, x, z) =>
            {
                float dx = x - cx, dz = z - cz;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d >= radius) return;
                f[ix, iz] += height * 0.5f * (1f + (float)Math.Cos(Math.PI * d / radius));
            });
        }

        /// <summary>A bowl: lowers the ground with a cosine profile.</summary>
        public static void Hollow(HeightField f, float cx, float cz, float radius, float depth) => Mound(f, cx, cz, radius, -depth);

        /// <summary>
        /// A trench from a to b with a floor width wide and depth deep. Its sides are cliffs; its
        /// ends slope up to the old ground over rampLength metres, so it can be entered only there.
        /// </summary>
        public static void Ravine(HeightField f, float ax, float az, float bx, float bz, float width, float depth, float rampLength)
        {
            float lx = bx - ax, lz = bz - az;
            float len = (float)Math.Sqrt(lx * lx + lz * lz);
            if (len < 0.01f) return;
            lx /= len; lz /= len;
            float half = width * 0.5f, wall = f.CellSize;
            ForEachNear(f, (ax + bx) * 0.5f, (az + bz) * 0.5f, len * 0.5f + half + wall + f.CellSize, (ix, iz, x, z) =>
            {
                float px = x - ax, pz = z - az;
                float along = px * lx + pz * lz;
                if (along < 0f || along > len) return;
                float across = Math.Abs(px * -lz + pz * lx);
                if (across > half + wall) return;
                float ends = Math.Min(1f, Math.Min(along, len - along) / Math.Max(0.01f, rampLength));
                float sides = across <= half ? 1f : 1f - (across - half) / wall;
                f[ix, iz] -= depth * ends * sides;
            });
        }

        /// <summary>
        /// Evens out a strip along a polyline: across the strip the ground takes the height of
        /// its centre line, so roads and causeways are level side to side. Raises to minHeight if set.
        /// </summary>
        public static void Strip(HeightField f, float[] xs, float[] zs, float width, float minHeight = float.NegativeInfinity)
        {
            float half = width * 0.5f, blend = f.CellSize * 1.5f;
            var copy = (float[])f.Heights.Clone();
            float Orig(float x, float z)
            {
                // Sample the unmodified field, so earlier segments do not feed later ones.
                float fx = (x - f.OriginX) / f.CellSize, fz = (z - f.OriginZ) / f.CellSize;
                fx = Math.Max(0f, Math.Min(f.Cells, fx)); fz = Math.Max(0f, Math.Min(f.Cells, fz));
                int ix = Math.Min((int)fx, f.Cells - 1), iz = Math.Min((int)fz, f.Cells - 1);
                float u = fx - ix, v = fz - iz;
                int s = f.Stride;
                float h00 = copy[iz * s + ix], h10 = copy[iz * s + ix + 1], h01 = copy[(iz + 1) * s + ix], h11 = copy[(iz + 1) * s + ix + 1];
                return (h00 * (1 - u) + h10 * u) * (1 - v) + (h01 * (1 - u) + h11 * u) * v;
            }
            for (int iz = 0; iz <= f.Cells; iz++)
                for (int ix = 0; ix <= f.Cells; ix++)
                {
                    float x = f.OriginX + ix * f.CellSize, z = f.OriginZ + iz * f.CellSize;
                    float best = float.MaxValue, cxBest = 0f, czBest = 0f;
                    for (int i = 0; i + 1 < xs.Length; i++)
                    {
                        float d = SegmentDistance(x, z, xs[i], zs[i], xs[i + 1], zs[i + 1], out float qx, out float qz);
                        if (d < best) { best = d; cxBest = qx; czBest = qz; }
                    }
                    if (best > half + blend) continue;
                    float target = Math.Max(minHeight, Orig(cxBest, czBest));
                    float t = best <= half ? 1f : Smooth(1f - (best - half) / blend);
                    float h = copy[iz * f.Stride + ix];
                    f[ix, iz] = h + (target - h) * t;
                }
        }

        /// <summary>Distance from a point to segment ab, and the closest point on it.</summary>
        public static float SegmentDistance(float x, float z, float ax, float az, float bx, float bz, out float qx, out float qz)
        {
            float lx = bx - ax, lz = bz - az;
            float l2 = lx * lx + lz * lz;
            float t = l2 > 1e-6f ? Math.Max(0f, Math.Min(1f, ((x - ax) * lx + (z - az) * lz) / l2)) : 0f;
            qx = ax + lx * t; qz = az + lz * t;
            float dx = x - qx, dz = z - qz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static void ForEachNear(HeightField f, float cx, float cz, float reach, Action<int, int, float, float> visit)
        {
            int x0 = Math.Max(0, (int)Math.Floor((cx - reach - f.OriginX) / f.CellSize));
            int x1 = Math.Min(f.Cells, (int)Math.Ceiling((cx + reach - f.OriginX) / f.CellSize));
            int z0 = Math.Max(0, (int)Math.Floor((cz - reach - f.OriginZ) / f.CellSize));
            int z1 = Math.Min(f.Cells, (int)Math.Ceiling((cz + reach - f.OriginZ) / f.CellSize));
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                    visit(ix, iz, f.OriginX + ix * f.CellSize, f.OriginZ + iz * f.CellSize);
        }
    }
}
