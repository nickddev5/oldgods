using System;

namespace OldGods.Rules
{
    /// <summary>A point or direction on the ground plane (world x and z).</summary>
    public struct Vec2
    {
        public float X, Z;

        public Vec2(float x, float z) { X = x; Z = z; }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);

        public float SqrMagnitude => X * X + Z * Z;
        public float Magnitude => (float)Math.Sqrt(X * X + Z * Z);

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                return m > 1e-6f ? new Vec2(X / m, Z / m) : default;
            }
        }

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;
        public override string ToString() => $"({X:0.##}, {Z:0.##})";
    }

    /// <summary>
    /// Uniform grid over the ground plane, rebuilt every frame with a counting sort.
    /// Cells hash into a fixed bucket table, so the map has no bounds.
    /// </summary>
    public sealed class SpatialHash
    {
        public readonly float CellSize;
        readonly int bucketMask;
        readonly int[] bucketStart;
        readonly int[] bucketCount;
        int[] sorted = new int[0];
        int count;

        public SpatialHash(float cellSize, int bucketBits = 12)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            CellSize = cellSize;
            int buckets = 1 << bucketBits;
            bucketMask = buckets - 1;
            bucketStart = new int[buckets];
            bucketCount = new int[buckets];
        }

        int Cell(float v) => (int)Math.Floor(v / CellSize);

        int Bucket(int cx, int cz)
        {
            unchecked
            {
                int h = cx * 73856093 ^ cz * 19349663;
                return h & bucketMask;
            }
        }

        public void Build(float[] xs, float[] zs, bool[] alive, int n)
        {
            count = n;
            if (sorted.Length < n) sorted = new int[Math.Max(n, sorted.Length * 2)];
            Array.Clear(bucketCount, 0, bucketCount.Length);
            for (int i = 0; i < n; i++)
            {
                if (alive != null && !alive[i]) continue;
                bucketCount[Bucket(Cell(xs[i]), Cell(zs[i]))]++;
            }
            int run = 0;
            for (int b = 0; b < bucketStart.Length; b++)
            {
                bucketStart[b] = run;
                run += bucketCount[b];
                bucketCount[b] = 0;
            }
            for (int i = 0; i < n; i++)
            {
                if (alive != null && !alive[i]) continue;
                int b = Bucket(Cell(xs[i]), Cell(zs[i]));
                sorted[bucketStart[b] + bucketCount[b]++] = i;
            }
        }

        /// <summary>
        /// Calls visit(index) for every entry in the cells overlapping the circle.
        /// Entries from colliding buckets may be included; callers check distance.
        /// </summary>
        public void Query(float x, float z, float radius, Action<int> visit)
        {
            int x0 = Cell(x - radius), x1 = Cell(x + radius);
            int z0 = Cell(z - radius), z1 = Cell(z + radius);
            for (int cx = x0; cx <= x1; cx++)
                for (int cz = z0; cz <= z1; cz++)
                {
                    int b = Bucket(cx, cz);
                    int start = bucketStart[b], end = start + bucketCount[b];
                    for (int k = start; k < end; k++) visit(sorted[k]);
                }
        }

        /// <summary>Allocation-free query: fills results with candidate indices, returns how many.</summary>
        public int Query(float x, float z, float radius, int[] results)
        {
            int found = 0;
            int x0 = Cell(x - radius), x1 = Cell(x + radius);
            int z0 = Cell(z - radius), z1 = Cell(z + radius);
            for (int cx = x0; cx <= x1; cx++)
                for (int cz = z0; cz <= z1; cz++)
                {
                    int b = Bucket(cx, cz);
                    int start = bucketStart[b], end = start + bucketCount[b];
                    for (int k = start; k < end && found < results.Length; k++) results[found++] = sorted[k];
                }
            return found;
        }

        public int Count => count;
    }

    /// <summary>Steering for the horde: seek the target, push away from neighbours.</summary>
    public static class HordeSteering
    {
        /// <summary>
        /// Desired velocity for one enemy. Seek at full speed toward the target,
        /// plus separation from neighbours inside separationRadius, scaled by weight.
        /// The result never exceeds speed * (1 + separationWeight).
        /// </summary>
        public static Vec2 Desired(int self, float[] xs, float[] zs, Vec2 target, float speed,
            float separationRadius, float separationWeight, SpatialHash hash, int[] scratch)
        {
            var pos = new Vec2(xs[self], zs[self]);
            Vec2 seek = (target - pos).Normalized * speed;

            Vec2 push = default;
            int n = hash.Query(pos.X, pos.Z, separationRadius, scratch);
            float r2 = separationRadius * separationRadius;
            for (int k = 0; k < n; k++)
            {
                int j = scratch[k];
                if (j == self) continue;
                float dx = pos.X - xs[j], dz = pos.Z - zs[j];
                float d2 = dx * dx + dz * dz;
                if (d2 >= r2) continue;
                if (d2 < 1e-6f)
                {
                    // Exactly overlapping: push apart along a direction derived from the index.
                    float a = (self * 2.399963f) % (2f * (float)Math.PI);
                    push += new Vec2((float)Math.Cos(a), (float)Math.Sin(a));
                    continue;
                }
                float d = (float)Math.Sqrt(d2);
                float strength = 1f - d / separationRadius;
                push += new Vec2(dx / d, dz / d) * strength;
            }

            float pm = push.Magnitude;
            if (pm > 1f) push = push * (1f / pm);
            return seek + push * (speed * separationWeight);
        }

        /// <summary>Moves current velocity toward desired by at most accel * dt.</summary>
        public static Vec2 Accelerate(Vec2 current, Vec2 desired, float accel, float dt)
        {
            Vec2 delta = desired - current;
            float max = accel * dt;
            float m = delta.Magnitude;
            if (m <= max || m < 1e-6f) return desired;
            return current + delta * (max / m);
        }

        /// <summary>A point on a ring around center, used to spawn enemies off-screen.</summary>
        public static Vec2 RingPoint(Vec2 center, float minRadius, float maxRadius, Rng rng)
        {
            float angle = rng.Range(0f, 2f * (float)Math.PI);
            float radius = rng.Range(minRadius, maxRadius);
            return new Vec2(center.X + (float)Math.Cos(angle) * radius, center.Z + (float)Math.Sin(angle) * radius);
        }
    }
}
