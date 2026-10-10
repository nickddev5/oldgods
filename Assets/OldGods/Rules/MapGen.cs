using System;

namespace OldGods.Rules
{
    /// <summary>How a biome's ground is shaped. PLACEHOLDER numbers.</summary>
    [Serializable]
    public sealed class TerrainProfile
    {
        public int Cells = 128;
        public float CellSize = 2f;
        /// <summary>Peak height of the rolling hills.</summary>
        public float HillHeight = 10f;
        /// <summary>World units per noise unit for the hills; larger is broader.</summary>
        public float HillScale = 60f;
        public int HillOctaves = 4;
        /// <summary>Height of long ridges and the valleys between them; 0 disables them.</summary>
        public float RidgeHeight = 0f;
        public float RidgeScale = 110f;
        /// <summary>Height of terraced cliff steps; 0 disables cliffs.</summary>
        public float CliffStep = 0f;
        /// <summary>0..1, how much of the map is terraced.</summary>
        public float CliffAmount = 0f;
        public float CliffScale = 90f;
        /// <summary>Radius of the flattened area around the map centre where the player starts.</summary>
        public float SpawnFlatRadius = 12f;
        /// <summary>Height of the raised rim that keeps the player on the map.</summary>
        public float RimHeight = 14f;
        public float RimWidth = 16f;
        /// <summary>Height of shallow water over low ground; below -100 means no water.</summary>
        public float WaterLevel = -1000f;
    }

    public static class TerrainGenerator
    {
        /// <summary>Builds the ground for one map. The map is centred on the world origin.</summary>
        public static HeightField Generate(TerrainProfile p, Rng rng)
        {
            float size = p.Cells * p.CellSize;
            var field = new HeightField(p.Cells, p.CellSize, -size * 0.5f, -size * 0.5f);
            var hills = new Noise2D(rng);
            var cliffs = new Noise2D(rng);
            var cliffMask = new Noise2D(rng);
            var ridges = new Noise2D(rng);

            for (int iz = 0; iz <= p.Cells; iz++)
                for (int ix = 0; ix <= p.Cells; ix++)
                {
                    float x = field.OriginX + ix * p.CellSize;
                    float z = field.OriginZ + iz * p.CellSize;
                    float h = (hills.Fbm(x / p.HillScale, z / p.HillScale, p.HillOctaves) * 0.5f + 0.5f) * p.HillHeight;
                    if (p.RidgeHeight > 0f)
                    {
                        // Ridged noise: sharp crests where the noise crosses zero, broad valleys between.
                        float r = 1f - Math.Abs(ridges.Fbm(x / p.RidgeScale, z / p.RidgeScale, 3));
                        h += r * r * p.RidgeHeight;
                    }

                    if (p.CliffStep > 0f && p.CliffAmount > 0f)
                    {
                        float mask = cliffMask.Fbm(x / (p.CliffScale * 1.7f), z / (p.CliffScale * 1.7f), 2) * 0.5f + 0.5f;
                        if (mask > 1f - p.CliffAmount)
                        {
                            float c = (cliffs.Fbm(x / p.CliffScale, z / p.CliffScale, 3) * 0.5f + 0.5f) * p.CliffStep * 3f;
                            float terraced = (float)Math.Floor(c / p.CliffStep) * p.CliffStep;
                            float blend = Math.Min(1f, (mask - (1f - p.CliffAmount)) * 8f);
                            h += terraced * blend;
                        }
                    }

                    field[ix, iz] = h;
                }

            FlattenDisc(field, 0f, 0f, p.SpawnFlatRadius, p.SpawnFlatRadius * 0.6f);
            RaiseRim(field, p.RimWidth, p.RimHeight);
            return field;
        }

        /// <summary>Blends heights toward the disc's average inside radius, fading over falloff.</summary>
        public static void FlattenDisc(HeightField f, float cx, float cz, float radius, float falloff)
        {
            float target = f.Sample(cx, cz);
            float outer = radius + falloff;
            for (int iz = 0; iz <= f.Cells; iz++)
                for (int ix = 0; ix <= f.Cells; ix++)
                {
                    float x = f.OriginX + ix * f.CellSize - cx;
                    float z = f.OriginZ + iz * f.CellSize - cz;
                    float d = (float)Math.Sqrt(x * x + z * z);
                    if (d >= outer) continue;
                    float t = d <= radius ? 1f : 1f - (d - radius) / Math.Max(0.001f, falloff);
                    t = t * t * (3f - 2f * t);
                    f[ix, iz] = f[ix, iz] + (target - f[ix, iz]) * t;
                }
        }

        /// <summary>Raises a rim along the square edge so the playable area is bowl-shaped.</summary>
        public static void RaiseRim(HeightField f, float width, float height)
        {
            if (width <= 0f || height <= 0f) return;
            for (int iz = 0; iz <= f.Cells; iz++)
                for (int ix = 0; ix <= f.Cells; ix++)
                {
                    float x = ix * f.CellSize, z = iz * f.CellSize;
                    float edge = Math.Min(Math.Min(x, z), Math.Min(f.Size - x, f.Size - z));
                    if (edge >= width) continue;
                    float t = 1f - edge / width;
                    f[ix, iz] += t * t * height;
                }
        }

        /// <summary>Distance from a point to the inside of the rim; the playable square.</summary>
        public static bool InPlayableArea(HeightField f, float x, float z, float rimWidth)
        {
            return x > f.MinX + rimWidth && x < f.MaxX - rimWidth && z > f.MinZ + rimWidth && z < f.MaxZ - rimWidth;
        }

        /// <summary>
        /// How far in from the map's edge the wall partway up the rim stands. The player and the
        /// horde can both walk up the rim this far, so the horde can always follow.
        /// </summary>
        public static float WallInset(float rimWidth) => rimWidth * 0.45f;
    }
}
