using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>A solid shape enemies cannot walk through: a segment with a radius (a circle when a equals b).</summary>
    public struct Capsule
    {
        public float AX, AZ, BX, BZ, Radius;

        public Capsule(float ax, float az, float bx, float bz, float radius)
        {
            AX = ax; AZ = az; BX = bx; BZ = bz; Radius = radius;
        }

        public static Capsule Circle(float x, float z, float radius) => new Capsule(x, z, x, z, radius);
    }

    /// <summary>
    /// The solid parts of the level dressing (walls, columns, statues), bucketed on a coarse
    /// grid so each enemy checks only the few near it.
    /// </summary>
    public sealed class Obstacles
    {
        public readonly List<Capsule> Shapes = new List<Capsule>();
        readonly float bucket;
        readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();

        public Obstacles(float bucketSize = 8f) { bucket = bucketSize; }

        public int Count => Shapes.Count;

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public void Add(Capsule c)
        {
            int index = Shapes.Count;
            Shapes.Add(c);
            float minX = Math.Min(c.AX, c.BX) - c.Radius, maxX = Math.Max(c.AX, c.BX) + c.Radius;
            float minZ = Math.Min(c.AZ, c.BZ) - c.Radius, maxZ = Math.Max(c.AZ, c.BZ) + c.Radius;
            for (int x = (int)Math.Floor(minX / bucket); x <= (int)Math.Floor(maxX / bucket); x++)
                for (int z = (int)Math.Floor(minZ / bucket); z <= (int)Math.Floor(maxZ / bucket); z++)
                {
                    if (!grid.TryGetValue(Key(x, z), out var list)) grid[Key(x, z)] = list = new List<int>(4);
                    list.Add(index);
                }
        }

        /// <summary>True when a circle at (x, z) touches any shape.</summary>
        public bool Overlaps(float x, float z, float radius)
        {
            int x0 = (int)Math.Floor((x - radius) / bucket), x1 = (int)Math.Floor((x + radius) / bucket);
            int z0 = (int)Math.Floor((z - radius) / bucket), z1 = (int)Math.Floor((z + radius) / bucket);
            for (int bx = x0; bx <= x1; bx++)
                for (int bz = z0; bz <= z1; bz++)
                {
                    if (!grid.TryGetValue(Key(bx, bz), out var list)) continue;
                    foreach (int i in list)
                    {
                        var c = Shapes[i];
                        if (Landforms.SegmentDistance(x, z, c.AX, c.AZ, c.BX, c.BZ, out _, out _) < c.Radius + radius) return true;
                    }
                }
            return false;
        }

        /// <summary>Pushes a circle at (x, z) out of every shape it overlaps. Returns true if it moved.</summary>
        public bool PushOut(ref float x, ref float z, float radius)
        {
            int x0 = (int)Math.Floor((x - radius) / bucket), x1 = (int)Math.Floor((x + radius) / bucket);
            int z0 = (int)Math.Floor((z - radius) / bucket), z1 = (int)Math.Floor((z + radius) / bucket);
            bool moved = false;
            for (int bx = x0; bx <= x1; bx++)
                for (int bz = z0; bz <= z1; bz++)
                {
                    if (!grid.TryGetValue(Key(bx, bz), out var list)) continue;
                    foreach (int i in list) moved |= PushOutOf(Shapes[i], ref x, ref z, radius);
                }
            return moved;
        }

        static bool PushOutOf(Capsule c, ref float x, ref float z, float radius)
        {
            float d = Landforms.SegmentDistance(x, z, c.AX, c.AZ, c.BX, c.BZ, out float qx, out float qz);
            float need = c.Radius + radius;
            if (d >= need) return false;
            if (d < 1e-4f)
            {
                // Dead centre: push out sideways from the segment, or along x for a circle.
                float lx = c.BX - c.AX, lz = c.BZ - c.AZ;
                float l = (float)Math.Sqrt(lx * lx + lz * lz);
                if (l > 1e-4f) { x = qx - lz / l * need; z = qz + lx / l * need; }
                else x = qx + need;
            }
            else
            {
                x = qx + (x - qx) / d * need;
                z = qz + (z - qz) / d * need;
            }
            return true;
        }
    }

    /// <summary>How hard a ground cell is for the horde to cross.</summary>
    public enum CellClass : byte
    {
        /// <summary>A cliff or the rim: impassable, and enemies cannot step onto it.</summary>
        Blocked = 0,
        Open = 1,
        /// <summary>Steep ground: passable at a cost and at half speed.</summary>
        Climb = 2,
        /// <summary>Under solid dressing: paths avoid it, but the obstacle itself does the colliding.</summary>
        Solid = 3,
    }

    /// <summary>
    /// Shortest paths to the player over the map's cells, so the horde walks around walls and
    /// cliffs and up ramps instead of into them. Cells match the height field's cells. Open
    /// ground costs 1, steep ground the horde can scramble up costs ClimbCost, and cliffs and
    /// solid dressing are blocked.
    /// </summary>
    public sealed class FlowField
    {
        public const int ClimbCost = 4;
        const int Unreached = int.MaxValue;

        public readonly int Cells;
        public readonly float CellSize, OriginX, OriginZ;
        public readonly CellClass[] Class;
        readonly int[] dist, edge;
        readonly float[] dirX, dirZ;
        readonly int[] heap;
        int heapCount;

        public int TargetCell { get; private set; } = -1;

        public FlowField(int cells, float cellSize, float originX, float originZ)
        {
            Cells = cells;
            CellSize = cellSize;
            OriginX = originX;
            OriginZ = originZ;
            int n = cells * cells;
            Class = new CellClass[n];
            for (int i = 0; i < n; i++) Class[i] = CellClass.Open;
            dist = new int[n];
            edge = new int[n];
            dirX = new float[n];
            dirZ = new float[n];
            heap = new int[n * 16 + 16];
        }

        /// <summary>
        /// Classifies each cell by how much its corners rise: up to walkRise is open, up to
        /// climbRise is a scramble, more is a cliff. The rim's slope up to its wall is open
        /// ground, since the player can stand there; cells past the wall are blocked, and
        /// cells under obstacles are solid.
        /// </summary>
        public static FlowField FromTerrain(HeightField f, Obstacles obstacles, float rimWidth, float walkRise = 1.3f, float climbRise = 3f)
        {
            var ff = new FlowField(f.Cells, f.CellSize, f.OriginX, f.OriginZ);
            // A cell is on the walkable rim when any part of it is inside the wall.
            float wall = TerrainGenerator.WallInset(rimWidth) - f.CellSize * 0.5f;
            for (int iz = 0; iz < f.Cells; iz++)
                for (int ix = 0; ix < f.Cells; ix++)
                {
                    float a = f[ix, iz], b = f[ix + 1, iz], c = f[ix, iz + 1], d = f[ix + 1, iz + 1];
                    float rise = Math.Max(Math.Max(a, b), Math.Max(c, d)) - Math.Min(Math.Min(a, b), Math.Min(c, d));
                    float x = f.OriginX + (ix + 0.5f) * f.CellSize, z = f.OriginZ + (iz + 0.5f) * f.CellSize;
                    CellClass k = rise <= walkRise ? CellClass.Open : rise <= climbRise ? CellClass.Climb : CellClass.Blocked;
                    if (!TerrainGenerator.InPlayableArea(f, x, z, rimWidth))
                        k = TerrainGenerator.InPlayableArea(f, x, z, wall) ? CellClass.Open : CellClass.Blocked;
                    if (k != CellClass.Blocked && obstacles != null && obstacles.Overlaps(x, z, f.CellSize * 0.4f)) k = CellClass.Solid;
                    ff.Class[iz * f.Cells + ix] = k;
                }
            return ff;
        }

        public static bool Passable(CellClass c) => c == CellClass.Open || c == CellClass.Climb;

        public int CellOf(float x, float z)
        {
            int ix = (int)Math.Floor((x - OriginX) / CellSize), iz = (int)Math.Floor((z - OriginZ) / CellSize);
            if (ix < 0 || iz < 0 || ix >= Cells || iz >= Cells) return -1;
            return iz * Cells + ix;
        }

        public CellClass ClassAt(float x, float z)
        {
            int c = CellOf(x, z);
            return c < 0 ? CellClass.Blocked : Class[c];
        }

        /// <summary>Path cost from the cell to the target in metres, or infinity when unreachable.</summary>
        public float Distance(float x, float z)
        {
            int c = CellOf(x, z);
            if (c < 0 || dist[c] == Unreached) return float.PositiveInfinity;
            return dist[c] * 0.1f * CellSize;
        }

        public bool Reachable(float x, float z)
        {
            int c = CellOf(x, z);
            return c >= 0 && dist[c] != Unreached;
        }

        /// <summary>Recomputes every cell's path to the target at (x, z). Costs are tenths of a cell.</summary>
        public void Solve(float x, float z)
        {
            int n = Cells * Cells;
            for (int i = 0; i < n; i++) { dist[i] = Unreached; edge[i] = Unreached; dirX[i] = 0f; dirZ[i] = 0f; }
            int start = CellOf(x, z);
            TargetCell = start;
            if (start < 0) return;
            heapCount = 0;
            dist[start] = 0;
            Push(start, 0);
            while (heapCount > 0)
            {
                Pop(out int cell, out int d);
                if (d > dist[cell]) continue;
                int cx = cell % Cells, cz = cell / Cells;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], nz = cz + DZ[k];
                    if (nx < 0 || nz < 0 || nx >= Cells || nz >= Cells) continue;
                    int ncell = nz * Cells + nx;
                    var cls = Class[ncell];
                    if (!Passable(cls)) continue;
                    bool diagonal = k >= 4;
                    // No cutting corners past a blocked cell.
                    if (diagonal && (!Passable(Class[cz * Cells + nx]) || !Passable(Class[nz * Cells + cx]))) continue;
                    int step = (diagonal ? 14 : 10) * (cls == CellClass.Climb ? ClimbCost : 1);
                    int nd = d + step;
                    if (nd >= dist[ncell]) continue;
                    dist[ncell] = nd;
                    Push(ncell, nd);
                }
            }
            // Cells under dressing or on cliffs are never walked through, but an enemy pressed
            // against a wall stands in one: give them the way out to their cheapest open neighbour.
            for (int cell = 0; cell < n; cell++)
            {
                if (Passable(Class[cell]) || dist[cell] != Unreached) continue;
                int cx = cell % Cells, cz = cell / Cells, best = Unreached;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], nz = cz + DZ[k];
                    if (nx < 0 || nz < 0 || nx >= Cells || nz >= Cells) continue;
                    int ncell = nz * Cells + nx;
                    if (!Passable(Class[ncell]) || dist[ncell] == Unreached) continue;
                    best = Math.Min(best, dist[ncell] + (k >= 4 ? 14 : 10));
                }
                edge[cell] = best;
            }
            for (int cell = 0; cell < n; cell++)
                if (edge[cell] != Unreached && !Passable(Class[cell]) && dist[cell] == Unreached) dist[cell] = edge[cell];

            // Each cell points at its cheapest neighbour.
            for (int cell = 0; cell < n; cell++)
            {
                if (dist[cell] == Unreached || cell == start) continue;
                int cx = cell % Cells, cz = cell / Cells;
                int best = dist[cell], bk = -1;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + DX[k], nz = cz + DZ[k];
                    if (nx < 0 || nz < 0 || nx >= Cells || nz >= Cells) continue;
                    int ncell = nz * Cells + nx;
                    if (!Passable(Class[ncell]) && ncell != start) continue;
                    if (dist[ncell] >= best) continue;
                    if (k >= 4 && (!Passable(Class[cz * Cells + nx]) || !Passable(Class[nz * Cells + cx]))) continue;
                    best = dist[ncell];
                    bk = k;
                }
                if (bk < 0) continue;
                float len = bk >= 4 ? 0.70710678f : 1f;
                dirX[cell] = DX[bk] * len;
                dirZ[cell] = DZ[bk] * len;
            }
        }

        /// <summary>
        /// Which way to walk from (x, z), blended between the four nearest cells so paths curve
        /// instead of stepping. False when no path is known here.
        /// </summary>
        public bool Direction(float x, float z, out float dx, out float dz)
        {
            dx = 0f; dz = 0f;
            float fx = (x - OriginX) / CellSize - 0.5f, fz = (z - OriginZ) / CellSize - 0.5f;
            int x0 = (int)Math.Floor(fx), z0 = (int)Math.Floor(fz);
            float u = fx - x0, v = fz - z0;
            float weight = 0f;
            for (int k = 0; k < 4; k++)
            {
                int ix = x0 + (k & 1), iz = z0 + (k >> 1);
                if (ix < 0 || iz < 0 || ix >= Cells || iz >= Cells) continue;
                int c = iz * Cells + ix;
                if (dist[c] == Unreached || (dirX[c] == 0f && dirZ[c] == 0f)) continue;
                float w = ((k & 1) == 1 ? u : 1f - u) * ((k >> 1) == 1 ? v : 1f - v);
                dx += dirX[c] * w;
                dz += dirZ[c] * w;
                weight += w;
            }
            float m = (float)Math.Sqrt(dx * dx + dz * dz);
            if (weight <= 0f || m < 1e-4f) return false;
            dx /= m; dz /= m;
            return true;
        }

        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        // A binary min-heap of (distance, cell) pairs packed into one int array.
        void Push(int cell, int d)
        {
            if (heapCount * 2 + 1 >= heap.Length) return; // each cell is pushed at most 8 times, so this never trips
            int i = heapCount++;
            heap[i * 2] = d;
            heap[i * 2 + 1] = cell;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heap[p * 2] <= heap[i * 2]) break;
                Swap(i, p);
                i = p;
            }
        }

        void Pop(out int cell, out int d)
        {
            d = heap[0];
            cell = heap[1];
            heapCount--;
            if (heapCount == 0) return;
            heap[0] = heap[heapCount * 2];
            heap[1] = heap[heapCount * 2 + 1];
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < heapCount && heap[l * 2] < heap[m * 2]) m = l;
                if (r < heapCount && heap[r * 2] < heap[m * 2]) m = r;
                if (m == i) break;
                Swap(i, m);
                i = m;
            }
        }

        void Swap(int a, int b)
        {
            int d = heap[a * 2], c = heap[a * 2 + 1];
            heap[a * 2] = heap[b * 2]; heap[a * 2 + 1] = heap[b * 2 + 1];
            heap[b * 2] = d; heap[b * 2 + 1] = c;
        }
    }
}
