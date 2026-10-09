using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>The set pieces a map can hold. PLACEHOLDER names.</summary>
    public enum LandmarkKind
    {
        HillFort,     // a cliff-sided plateau with two ramps and a broken wall ring on top
        StoneCircle,  // a ring of standing stones on flattened ground
        Barrow,       // a burial mound with a stone doorway
        FallenGod,    // a toppled colossus: head, hand, torso and staff
        Colonnade,    // a raised temple floor with two rows of columns
        Ravine,       // a trench with cliff sides, open only at its sloped ends
        Ridge,        // a long plateau with a ramp at each end
        GreatStump,   // the burnt stump of a giant tree
        EmberHollow,  // a smouldering bowl ringed by charred columns
        Wreck,        // a ship's hull on its side
        SeaTower,     // a headland plateau with a broken round tower
        RuinedHouse,  // three broken walls round a floor, open on one side
        Outcrop,      // a low rock shelf the player can jump onto and the horde must scramble up
    }

    /// <summary>One piece of dressing. The runtime draws it; its solid shape is in the layout's obstacles.</summary>
    public enum PieceKind
    {
        Column, BrokenColumn, FallenColumn, Wall, Rubble, Arch, Altar, Monolith, Lintel, Doorway,
        ColossusHead, ColossusHand, ColossusTorso, Stump, Hull, Tower, Milestone, Boulder,
        /// <summary>Glowing coals on the ground; not solid.</summary>
        Embers,
    }

    /// <summary>Ground colouring under the dressing, per height-field cell.</summary>
    public enum GroundPaint : byte { None = 0, Road = 1, Stone = 2, Accent = 3 }

    public struct Piece
    {
        public PieceKind Kind;
        public float X, Z;
        /// <summary>Degrees about y; the piece's length runs along its local x.</summary>
        public float Yaw;
        /// <summary>Size along local x, y and z in metres.</summary>
        public float Width, Height, Depth;
        /// <summary>Degrees of tilt about the piece's local x (fallen and leaning pieces).</summary>
        public float Lean;
        /// <summary>Metres above the ground the piece's base sits (a lintel on its stones).</summary>
        public float Lift;
        public int Variant;
        /// <summary>Which landmark the piece belongs to; -1 for road and wall dressing.</summary>
        public int Landmark;

        public override string ToString() => $"{Kind} ({X:0.#}, {Z:0.#}) {Width:0.#}x{Height:0.#}x{Depth:0.#}";
    }

    public struct Landmark
    {
        public LandmarkKind Kind;
        public float X, Z, Yaw, Radius;
        /// <summary>Height of the top for raised landmarks, else the ground height at the centre.</summary>
        public float Top;

        public override string ToString() => $"{Kind} ({X:0.#}, {Z:0.#}) r{Radius:0.#}";
    }

    /// <summary>A place worth the detour, where the map prefers to put a feature.</summary>
    public struct Spot
    {
        public float X, Z;
        public FeatureKind Prefer;
        public int Landmark;
    }

    [Serializable]
    public struct LandmarkCount
    {
        public LandmarkKind Kind;
        public int Count;

        public LandmarkCount(LandmarkKind kind, int count) { Kind = kind; Count = count; }
    }

    /// <summary>What a biome's maps are dressed with. PLACEHOLDER numbers.</summary>
    [Serializable]
    public sealed class LayoutProfile
    {
        /// <summary>An old paved road from the start to the boss gate.</summary>
        public bool Road;
        public float RoadWidth = 6f;
        /// <summary>The road is raised to at least this height, making a causeway over water.</summary>
        public float RoadMinHeight = -1000f;
        /// <summary>A long broken wall across the road, with a gate arch where they cross. Needs Road.</summary>
        public bool Wall;
        public List<LandmarkCount> Landmarks = new List<LandmarkCount>();
        /// <summary>Clear ground kept between landmarks.</summary>
        public float Spacing = 10f;
        /// <summary>Landmarks keep at least this far from the player's start.</summary>
        public float MinFromSpawn = 26f;
    }

    /// <summary>The landmark sets that give each stage biome its own maps. PLACEHOLDER counts.</summary>
    public static class LayoutPresets
    {
        /// <summary>The first road, and the wall that guards it: forts, barrows, stone circles, a fallen god.</summary>
        public static LayoutProfile GreySteppe() => new LayoutProfile
        {
            Road = true,
            Wall = true,
            Landmarks = new List<LandmarkCount>
            {
                new LandmarkCount(LandmarkKind.HillFort, 2), new LandmarkCount(LandmarkKind.FallenGod, 1),
                new LandmarkCount(LandmarkKind.Colonnade, 1), new LandmarkCount(LandmarkKind.StoneCircle, 2),
                new LandmarkCount(LandmarkKind.Barrow, 3), new LandmarkCount(LandmarkKind.RuinedHouse, 3),
                new LandmarkCount(LandmarkKind.Outcrop, 4),
            },
        };

        /// <summary>It burned, and it has not stopped burning: ridges, ravines, ember hollows, giant stumps.</summary>
        public static LayoutProfile AshWood() => new LayoutProfile
        {
            Landmarks = new List<LandmarkCount>
            {
                new LandmarkCount(LandmarkKind.Ridge, 2), new LandmarkCount(LandmarkKind.Ravine, 3),
                new LandmarkCount(LandmarkKind.EmberHollow, 2), new LandmarkCount(LandmarkKind.Colonnade, 1),
                new LandmarkCount(LandmarkKind.GreatStump, 3), new LandmarkCount(LandmarkKind.RuinedHouse, 2),
                new LandmarkCount(LandmarkKind.Outcrop, 4),
            },
        };

        /// <summary>The sea takes, and keeps: a causeway over the shallows, sea towers, wrecks, drowned temples.</summary>
        public static LayoutProfile DrownedCoast(float waterLevel) => new LayoutProfile
        {
            Road = true,
            RoadWidth = 5f,
            RoadMinHeight = waterLevel + 0.7f,
            Landmarks = new List<LandmarkCount>
            {
                new LandmarkCount(LandmarkKind.SeaTower, 2), new LandmarkCount(LandmarkKind.FallenGod, 1),
                new LandmarkCount(LandmarkKind.Colonnade, 2), new LandmarkCount(LandmarkKind.Wreck, 2),
                new LandmarkCount(LandmarkKind.RuinedHouse, 2), new LandmarkCount(LandmarkKind.Outcrop, 5),
            },
        };
    }

    /// <summary>One map's set pieces: where they are, what they are made of and what they changed.</summary>
    public sealed class LevelLayout
    {
        public readonly List<Landmark> Landmarks = new List<Landmark>();
        public readonly List<Piece> Pieces = new List<Piece>();
        public readonly List<Spot> Spots = new List<Spot>();
        public readonly Obstacles Obstacles = new Obstacles();
        readonly HeightField field;
        // Painted shapes in order; a later shape paints over an earlier one.
        readonly List<(Func<float, float, bool> inside, float minX, float minZ, float maxX, float maxZ, GroundPaint paint)> paints =
            new List<(Func<float, float, bool>, float, float, float, float, GroundPaint)>();

        public bool HasGate;
        public float GateX, GateZ, GateYaw;
        public float[] RoadX = Array.Empty<float>(), RoadZ = Array.Empty<float>();
        public float RoadWidth;
        public float[] WallX = Array.Empty<float>(), WallZ = Array.Empty<float>();

        public LevelLayout(HeightField f)
        {
            field = f;
        }

        /// <summary>The ground marking at an exact point (the terrain mesh asks per triangle).</summary>
        public GroundPaint PaintAt(float x, float z)
        {
            for (int i = paints.Count - 1; i >= 0; i--)
            {
                var p = paints[i];
                if (x < p.minX || x > p.maxX || z < p.minZ || z > p.maxZ) continue;
                if (p.inside(x, z)) return p.paint;
            }
            return GroundPaint.None;
        }

        /// <summary>True when (x, z) is away from every landmark, the road, the wall and the gate by margin.</summary>
        public bool IsClear(float x, float z, float margin)
        {
            foreach (var l in Landmarks)
            {
                float dx = x - l.X, dz = z - l.Z, r = l.Radius + margin;
                if (dx * dx + dz * dz < r * r) return false;
            }
            if (HasGate)
            {
                float dx = x - GateX, dz = z - GateZ, r = 8f + margin;
                if (dx * dx + dz * dz < r * r) return false;
            }
            if (PolylineDistance(x, z, RoadX, RoadZ) < RoadWidth * 0.5f + margin) return false;
            if (PolylineDistance(x, z, WallX, WallZ) < 1.5f + margin) return false;
            return !Obstacles.Overlaps(x, z, margin);
        }

        public static float PolylineDistance(float x, float z, float[] xs, float[] zs)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i + 1 < xs.Length; i++)
                best = Math.Min(best, Landforms.SegmentDistance(x, z, xs[i], zs[i], xs[i + 1], zs[i + 1], out _, out _));
            return best;
        }

        internal void PaintDisc(float cx, float cz, float radius, GroundPaint paint) =>
            PaintWhere((x, z) => (x - cx) * (x - cx) + (z - cz) * (z - cz) <= radius * radius, paint, cx, cz, radius);

        /// <summary>Paints where inside is true, within reach of (cx, cz); reach bounds the test for speed.</summary>
        internal void PaintWhere(Func<float, float, bool> inside, GroundPaint paint, float cx, float cz, float reach) =>
            paints.Add((inside, cx - reach, cz - reach, cx + reach, cz + reach, paint));

        internal void PaintWhere(Func<float, float, bool> inside, GroundPaint paint) =>
            paints.Add((inside, float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity, paint));
    }

    /// <summary>
    /// Dresses a generated height field: picks the boss gate's site, lays the road and wall,
    /// then places each landmark on clear ground, stamping its shape into the terrain and
    /// listing its pieces, obstacles and detour spots.
    /// </summary>
    public static class LayoutGenerator
    {
        public static LevelLayout Build(HeightField f, float rimWidth, LayoutProfile p, Rng rng)
        {
            var layout = new LevelLayout(f);
            if (p == null) return layout;
            float half = f.Size * 0.5f - rimWidth - 10f;
            if (half <= p.MinFromSpawn + 20f) return layout;

            PlaceGate(f, layout, half, rng);
            if (p.Road) LayRoad(f, layout, p, rng);
            if (p.Road && p.Wall) BuildWall(f, layout, half, rng);

            var queue = new List<LandmarkKind>();
            foreach (var c in p.Landmarks)
                for (int i = 0; i < c.Count; i++) queue.Add(c.Kind);
            // Big pieces first, while there is room for them.
            queue.Sort((a, b) => Footprint(b).CompareTo(Footprint(a)));
            foreach (var kind in queue) TryPlace(f, layout, p, kind, half, rng);
            return layout;
        }

        /// <summary>Rough radius a landmark needs, used for spacing before its exact size is rolled.</summary>
        public static float Footprint(LandmarkKind k)
        {
            switch (k)
            {
                case LandmarkKind.HillFort: return 27f;
                case LandmarkKind.Ridge: return 32f;
                case LandmarkKind.Ravine: return 30f;
                case LandmarkKind.SeaTower: return 26f;
                case LandmarkKind.FallenGod: return 16f;
                case LandmarkKind.Colonnade: return 14f;
                case LandmarkKind.EmberHollow: return 14f;
                case LandmarkKind.Barrow: return 15f;
                case LandmarkKind.StoneCircle: return 12f;
                case LandmarkKind.Wreck: return 11f;
                case LandmarkKind.Outcrop: return 9f;
                case LandmarkKind.RuinedHouse: return 8f;
                default: return 10f;
            }
        }

        static void PlaceGate(HeightField f, LevelLayout layout, float half, Rng rng)
        {
            float a = rng.Range(0f, (float)Math.PI * 2f);
            float d = rng.Range(0.68f, 0.85f) * half;
            float gx = (float)Math.Cos(a) * d, gz = (float)Math.Sin(a) * d;
            TerrainGenerator.FlattenDisc(f, gx, gz, 7f, 5f);
            layout.HasGate = true;
            layout.GateX = gx;
            layout.GateZ = gz;
            layout.GateYaw = (float)(Math.Atan2(-gx, -gz) * 180.0 / Math.PI);
        }

        static void LayRoad(HeightField f, LevelLayout layout, LayoutProfile p, Rng rng)
        {
            // From the start to just short of the gate, bending gently.
            float ex = layout.GateX, ez = layout.GateZ;
            float len = (float)Math.Sqrt(ex * ex + ez * ez);
            float ux = ex / len, uz = ez / len;
            const int points = 6;
            var xs = new float[points];
            var zs = new float[points];
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float along = 6f + (len - 14f) * t;
                float bend = (i == 0 || i == points - 1) ? 0f : rng.Range(-0.12f, 0.12f) * len;
                xs[i] = ux * along - uz * bend;
                zs[i] = uz * along + ux * bend;
            }
            Landforms.Strip(f, xs, zs, p.RoadWidth, p.RoadMinHeight);
            layout.RoadX = xs;
            layout.RoadZ = zs;
            layout.RoadWidth = p.RoadWidth;
            float halfWidth = p.RoadWidth * 0.5f;
            layout.PaintWhere((x, z) => LevelLayout.PolylineDistance(x, z, xs, zs) < halfWidth, GroundPaint.Road);

            // Milestones along the verge.
            float walked = 0f, next = 22f;
            for (int i = 0; i + 1 < points; i++)
            {
                float sx = xs[i + 1] - xs[i], sz = zs[i + 1] - zs[i];
                float sl = (float)Math.Sqrt(sx * sx + sz * sz);
                while (next <= walked + sl)
                {
                    float t = (next - walked) / sl;
                    float side = rng.Chance(0.5f) ? 1f : -1f;
                    float nx = -sz / sl * side, nz = sx / sl * side;
                    float px = xs[i] + sx * t + nx * (halfWidth + 1.2f), pz = zs[i] + sz * t + nz * (halfWidth + 1.2f);
                    AddPiece(layout, PieceKind.Milestone, px, pz, Deg(sx, sz), 0.6f, rng.Range(1.1f, 1.5f), 0.45f, rng.Chance(0.25f) ? rng.Range(10f, 25f) : 0f, -1, rng);
                    layout.Obstacles.Add(Capsule.Circle(px, pz, 0.45f));
                    next += rng.Range(20f, 30f);
                }
                walked += sl;
            }
        }

        static void BuildWall(HeightField f, LevelLayout layout, float half, Rng rng)
        {
            // Cross the road a little past halfway, square to it.
            float[] xs = layout.RoadX, zs = layout.RoadZ;
            float total = 0f;
            for (int i = 0; i + 1 < xs.Length; i++) total += Dist(xs[i], zs[i], xs[i + 1], zs[i + 1]);
            float want = total * rng.Range(0.5f, 0.62f), walked = 0f;
            float cx = xs[0], cz = zs[0], dx = 1f, dz = 0f;
            for (int i = 0; i + 1 < xs.Length; i++)
            {
                float sl = Dist(xs[i], zs[i], xs[i + 1], zs[i + 1]);
                if (walked + sl >= want)
                {
                    float t = (want - walked) / sl;
                    cx = xs[i] + (xs[i + 1] - xs[i]) * t;
                    cz = zs[i] + (zs[i + 1] - zs[i]) * t;
                    dx = (xs[i + 1] - xs[i]) / sl;
                    dz = (zs[i + 1] - zs[i]) / sl;
                    break;
                }
                walked += sl;
            }
            float wx = -dz, wz = dx; // along the wall
            float yaw = Deg(wx, wz);
            float reachA = rng.Range(35f, 55f), reachB = rng.Range(35f, 55f);
            const float seg = 4f, gap = 3.4f;

            // The gate arch over the road.
            AddPiece(layout, PieceKind.Arch, cx, cz, yaw, gap * 2f + 1.6f, 5.2f, 1.6f, 0f, -1, rng);
            // Its pillars stand at the edges of the road.
            layout.Obstacles.Add(Capsule.Circle(cx + wx * gap, cz + wz * gap, 0.95f));
            layout.Obstacles.Add(Capsule.Circle(cx - wx * gap, cz - wz * gap, 0.95f));
            layout.Spots.Add(new Spot { X = cx - dx * 7f + wx * 6f, Z = cz - dz * 7f + wz * 6f, Prefer = FeatureKind.Shrine, Landmark = -1 });

            float limit = half + 4f;
            foreach (float sign in new[] { 1f, -1f })
            {
                float reach = sign > 0f ? reachA : reachB;
                for (float s = gap + 1.2f; s + seg <= reach; s += seg)
                {
                    float mid = s + seg * 0.5f;
                    float px = cx + wx * mid * sign, pz = cz + wz * mid * sign;
                    if (Math.Abs(px) > limit || Math.Abs(pz) > limit) break;
                    float decay = mid / reach; // the wall crumbles toward its ends
                    float roll = rng.NextFloat();
                    if (roll < 0.1f + 0.15f * decay) continue; // a breach
                    bool rubble = roll < 0.3f + 0.35f * decay;
                    float h = rubble ? rng.Range(0.7f, 1.1f) : rng.Range(2.8f, 3.8f);
                    AddPiece(layout, rubble ? PieceKind.Rubble : PieceKind.Wall, px, pz, yaw, seg + 0.1f, h, 1.3f, 0f, -1, rng);
                    float ax = px - wx * seg * 0.5f, az = pz - wz * seg * 0.5f, bx = px + wx * seg * 0.5f, bz = pz + wz * seg * 0.5f;
                    layout.Obstacles.Add(new Capsule(ax, az, bx, bz, 0.7f));
                }
            }
            layout.WallX = new[] { cx - wx * reachB, cx + wx * reachA };
            layout.WallZ = new[] { cz - wz * reachB, cz + wz * reachA };
        }

        static bool TryPlace(HeightField f, LevelLayout layout, LayoutProfile p, LandmarkKind kind, float half, Rng rng)
        {
            float r = Footprint(kind);
            for (int attempt = 0; attempt < 160; attempt++)
            {
                float x = rng.Range(-half + r, half - r), z = rng.Range(-half + r, half - r);
                if (x * x + z * z < (p.MinFromSpawn + r) * (p.MinFromSpawn + r)) continue;
                if (!layout.IsClear(x, z, r + p.Spacing * 0.5f)) continue;
                bool spaced = true;
                foreach (var l in layout.Landmarks)
                    if (Dist(x, z, l.X, l.Z) < r + l.Radius + p.Spacing) { spaced = false; break; }
                if (!spaced) continue;
                float yaw = rng.Range(0f, 360f);
                if (Raised(kind) && Roughness(f, x, z, r * 0.6f) > 7f) continue;
                Stamp(f, layout, kind, x, z, yaw, rng);
                return true;
            }
            return false;
        }

        static bool Raised(LandmarkKind k) =>
            k == LandmarkKind.HillFort || k == LandmarkKind.Ridge || k == LandmarkKind.SeaTower || k == LandmarkKind.Colonnade || k == LandmarkKind.Outcrop;

        /// <summary>Height range of the ground over a disc, sampled on two rings.</summary>
        static float Roughness(HeightField f, float x, float z, float radius)
        {
            float lo = f.Sample(x, z), hi = lo;
            for (int i = 0; i < 12; i++)
            {
                float a = i * (float)Math.PI / 6f;
                foreach (float k in new[] { 0.5f, 1f })
                {
                    float h = f.Sample(x + (float)Math.Cos(a) * radius * k, z + (float)Math.Sin(a) * radius * k);
                    lo = Math.Min(lo, h); hi = Math.Max(hi, h);
                }
            }
            return hi - lo;
        }

        static float MeanHeight(HeightField f, float x, float z, float radius)
        {
            float sum = f.Sample(x, z);
            for (int i = 0; i < 8; i++)
            {
                float a = i * (float)Math.PI / 4f;
                sum += f.Sample(x + (float)Math.Cos(a) * radius, z + (float)Math.Sin(a) * radius);
            }
            return sum / 9f;
        }

        static void Stamp(HeightField f, LevelLayout layout, LandmarkKind kind, float x, float z, float yaw, Rng rng)
        {
            int id = layout.Landmarks.Count;
            float yr = yaw * (float)Math.PI / 180f;
            // Local axes: right (along the landmark's x) and forward (its z).
            float rx = (float)Math.Cos(yr), rz = -(float)Math.Sin(yr);
            float fx = (float)Math.Sin(yr), fz = (float)Math.Cos(yr);
            float WX(float lx, float lz) => x + rx * lx + fx * lz;
            float WZ(float lx, float lz) => z + rz * lx + fz * lz;
            var lm = new Landmark { Kind = kind, X = x, Z = z, Yaw = yaw, Radius = Footprint(kind), Top = f.Sample(x, z) };

            switch (kind)
            {
                case LandmarkKind.HillFort:
                {
                    float ra = rng.Range(10f, 13f), rb = ra * rng.Range(0.75f, 0.95f), height = rng.Range(4.5f, 6f);
                    float top = MeanHeight(f, x, z, ra) + height;
                    Landforms.Plateau(f, x, z, ra, rb, yr, top, 2.6f);
                    float rampLen = height * 2.3f;
                    float a1 = rng.Range(0f, (float)Math.PI * 2f), a2 = a1 + (float)Math.PI + rng.Range(-0.7f, 0.7f);
                    foreach (float a in new[] { a1, a2 })
                    {
                        float ex = (float)Math.Cos(a), ez = (float)Math.Sin(a);
                        float edge = EllipseRadius(ra, rb, a);
                        float bx = WX(ex * (edge - 1.5f), ez * (edge - 1.5f)), bz = WZ(ex * (edge - 1.5f), ez * (edge - 1.5f));
                        float ax = WX(ex * (edge + rampLen), ez * (edge + rampLen)), az = WZ(ex * (edge + rampLen), ez * (edge + rampLen));
                        Landforms.Ramp(f, ax, az, bx, bz, top, 4.6f);
                    }
                    // A broken ring wall along the top edge, open where the ramps arrive.
                    int segs = 16;
                    for (int i = 0; i < segs; i++)
                    {
                        float a = i * (float)Math.PI * 2f / segs + 0.1f;
                        if (AngleGap(a, a1) < 0.45f || AngleGap(a, a2) < 0.45f) continue;
                        if (rng.Chance(0.3f)) continue;
                        float er = EllipseRadius(ra, rb, a) - 1.2f;
                        float lx = (float)Math.Cos(a) * er, lz = (float)Math.Sin(a) * er;
                        float px = WX(lx, lz), pz = WZ(lx, lz);
                        float tx = WX(-(float)Math.Sin(a), (float)Math.Cos(a)) - x, tz = WZ(-(float)Math.Sin(a), (float)Math.Cos(a)) - z;
                        float segLen = er * (float)Math.PI * 2f / segs * 0.9f;
                        bool rubble = rng.Chance(0.35f);
                        AddPiece(layout, rubble ? PieceKind.Rubble : PieceKind.Wall, px, pz, Deg(tx, tz), segLen, rubble ? rng.Range(0.6f, 1f) : rng.Range(1.4f, 2.2f), 1f, 0f, id, rng);
                        layout.Obstacles.Add(new Capsule(px - tx * segLen * 0.5f, pz - tz * segLen * 0.5f, px + tx * segLen * 0.5f, pz + tz * segLen * 0.5f, 0.55f));
                    }
                    AddPiece(layout, PieceKind.Monolith, WX(0f, -rb * 0.45f), WZ(0f, -rb * 0.45f), yaw, 1.2f, 3.6f, 0.7f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(0f, -rb * 0.45f), WZ(0f, -rb * 0.45f), 0.7f));
                    layout.PaintWhere((px, pz) => InEllipse(px - x, pz - z, yr, ra - 2.5f, rb - 2.5f), GroundPaint.Stone, x, z, ra);
                    layout.Spots.Add(new Spot { X = WX(0f, rb * 0.15f), Z = WZ(0f, rb * 0.15f), Prefer = FeatureKind.Chest, Landmark = id });
                    lm.Top = top;
                    lm.Radius = ra + rampLen;
                    break;
                }
                case LandmarkKind.StoneCircle:
                {
                    float r = rng.Range(7f, 9.5f);
                    TerrainGenerator.FlattenDisc(f, x, z, r + 2f, 4f);
                    int n = rng.Range(9, 13);
                    int trilithon = rng.Range(0, n);
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * (float)Math.PI * 2f / n;
                        float px = x + (float)Math.Cos(a) * r, pz = z + (float)Math.Sin(a) * r;
                        float face = Deg(-(float)Math.Sin(a), (float)Math.Cos(a));
                        bool fallen = i != trilithon && i != (trilithon + 1) % n && rng.Chance(0.2f);
                        float h = i == trilithon || i == (trilithon + 1) % n ? 3.8f : rng.Range(2.8f, 4.4f);
                        if (fallen)
                        {
                            float lean = rng.Chance(0.5f) ? 84f : -84f;
                            AddPiece(layout, PieceKind.Monolith, px, pz, face, 1.2f, h, 0.7f, lean, id, rng);
                            // It fell along its local z, toward or away from the centre.
                            float fall = Math.Sign(lean) * h * 0.9f;
                            float fdx = (float)Math.Cos(a) * -1f, fdz = (float)Math.Sin(a) * -1f;
                            layout.Obstacles.Add(new Capsule(px, pz, px + fdx * fall, pz + fdz * fall, 0.6f));
                        }
                        else
                        {
                            AddPiece(layout, PieceKind.Monolith, px, pz, face, 1.2f, h, 0.7f, rng.Range(-6f, 6f), id, rng);
                            layout.Obstacles.Add(Capsule.Circle(px, pz, 0.75f));
                        }
                    }
                    {
                        float a = (trilithon + 0.5f) * (float)Math.PI * 2f / n;
                        float span = 2f * r * (float)Math.Sin(Math.PI / n) + 1.2f;
                        AddPiece(layout, PieceKind.Lintel, x + (float)Math.Cos(a) * r * (float)Math.Cos(Math.PI / n), z + (float)Math.Sin(a) * r * (float)Math.Cos(Math.PI / n),
                            Deg(-(float)Math.Sin(a), (float)Math.Cos(a)), span, 0.7f, 0.8f, 0f, id, rng);
                        var lintel = layout.Pieces[layout.Pieces.Count - 1];
                        lintel.Lift = 3.65f;
                        layout.Pieces[layout.Pieces.Count - 1] = lintel;
                    }
                    layout.PaintDisc(x, z, r - 1.5f, GroundPaint.Accent);
                    layout.Spots.Add(new Spot { X = x, Z = z, Prefer = FeatureKind.Shrine, Landmark = id });
                    lm.Radius = r + 3f;
                    break;
                }
                case LandmarkKind.Barrow:
                {
                    float r = rng.Range(10f, 12f), h = rng.Range(4.5f, 5.2f);
                    Landforms.Mound(f, x, z, r, h);
                    float dr = r * 0.8f;
                    AddPiece(layout, PieceKind.Doorway, WX(0f, dr), WZ(0f, dr), yaw, 3.4f, 3f, 1.4f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(-1.4f, dr), WZ(-1.4f, dr), 0.5f));
                    layout.Obstacles.Add(Capsule.Circle(WX(1.4f, dr), WZ(1.4f, dr), 0.5f));
                    layout.PaintWhere((px, pz) => Landforms.SegmentDistance(px, pz, WX(0f, dr), WZ(0f, dr), WX(0f, r + 3f), WZ(0f, r + 3f), out _, out _) < 1.4f,
                        GroundPaint.Stone, x, z, r + 4f);
                    // A kerb of low stones round the foot of the mound, broken at the door.
                    int kerb = rng.Range(10, 15);
                    for (int i = 0; i < kerb; i++)
                    {
                        float a = i * (float)Math.PI * 2f / kerb + 0.2f;
                        float kx = (float)Math.Cos(a) * (r + 0.6f), kz = (float)Math.Sin(a) * (r + 0.6f);
                        if (kz > r * 0.7f && Math.Abs(kx) < 3f) continue;
                        if (rng.Chance(0.2f)) continue;
                        AddPiece(layout, PieceKind.Monolith, WX(kx, kz), WZ(kx, kz), yaw + rng.Range(0f, 360f), 0.9f, rng.Range(0.8f, 1.4f), 0.6f, rng.Range(-10f, 10f), id, rng);
                        layout.Obstacles.Add(Capsule.Circle(WX(kx, kz), WZ(kx, kz), 0.5f));
                    }
                    AddPiece(layout, PieceKind.Monolith, WX(0f, -2.2f), WZ(0f, -2.2f), yaw, 1f, 2.6f, 0.6f, rng.Range(-8f, 8f), id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(0f, -2.2f), WZ(0f, -2.2f), 0.6f));
                    layout.Spots.Add(new Spot { X = WX(0f, 1.2f), Z = WZ(0f, 1.2f), Prefer = FeatureKind.Chest, Landmark = id });
                    lm.Top = f.Sample(x, z);
                    lm.Radius = r + 3f;
                    break;
                }
                case LandmarkKind.FallenGod:
                {
                    TerrainGenerator.FlattenDisc(f, x, z, 11f, 6f);
                    AddPiece(layout, PieceKind.ColossusHead, WX(0f, 6f), WZ(0f, 6f), yaw + rng.Range(-20f, 20f), 5f, 5f, 6f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(0f, 6f), WZ(0f, 6f), 3.4f));
                    // The torso lies along the landmark's z, its length on the piece's own x.
                    AddPiece(layout, PieceKind.ColossusTorso, WX(0f, -3f), WZ(0f, -3f), yaw + 90f, 9f, 4.2f, 5f, 0f, id, rng);
                    layout.Obstacles.Add(new Capsule(WX(0f, -6.2f), WZ(0f, -6.2f), WX(0f, 0.2f), WZ(0f, 0.2f), 2.3f));
                    AddPiece(layout, PieceKind.ColossusHand, WX(7.5f, 1f), WZ(7.5f, 1f), yaw + rng.Range(-40f, 40f), 4f, 2f, 5f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(7.5f, 1f), WZ(7.5f, 1f), 2.1f));
                    // The broken staff it held.
                    AddPiece(layout, PieceKind.FallenColumn, WX(-7f, 2f), WZ(-7f, 2f), yaw + 70f, 14f, 1.1f, 1.1f, 0f, id, rng);
                    float sy = (yaw + 70f) * (float)Math.PI / 180f;
                    float sx = (float)Math.Cos(sy), sz = -(float)Math.Sin(sy);
                    layout.Obstacles.Add(new Capsule(WX(-7f, 2f) - sx * 6.5f, WZ(-7f, 2f) - sz * 6.5f, WX(-7f, 2f) + sx * 6.5f, WZ(-7f, 2f) + sz * 6.5f, 0.6f));
                    layout.Spots.Add(new Spot { X = WX(-5f, 9f), Z = WZ(-5f, 9f), Prefer = FeatureKind.Chest, Landmark = id });
                    break;
                }
                case LandmarkKind.Colonnade:
                {
                    float w = rng.Range(10f, 13f), d = rng.Range(18f, 24f);
                    float top = MeanHeight(f, x, z, d * 0.5f) + 1f;
                    Landforms.Platform(f, x, z, w * 0.5f + 0.5f, d * 0.5f + 0.5f, yr, top, 2.4f);
                    layout.PaintWhere((px, pz) => InBox(px - x, pz - z, yr, w * 0.5f, d * 0.5f), GroundPaint.Stone, x, z, d);
                    for (float lz = -d * 0.5f + 1.5f; lz <= d * 0.5f - 1.5f; lz += 3.2f)
                        foreach (float side in new[] { -1f, 1f })
                        {
                            float lx = side * (w * 0.5f - 1.2f);
                            float px = WX(lx, lz), pz = WZ(lx, lz);
                            float roll = rng.NextFloat();
                            if (roll < 0.55f)
                                AddPiece(layout, PieceKind.Column, px, pz, yaw, 1.1f, rng.Range(5.2f, 6f), 1.1f, 0f, id, rng);
                            else if (roll < 0.85f)
                                AddPiece(layout, PieceKind.BrokenColumn, px, pz, yaw, 1.1f, rng.Range(1.4f, 3.4f), 1.1f, 0f, id, rng);
                            else
                            {
                                AddPiece(layout, PieceKind.BrokenColumn, px, pz, yaw, 1.1f, 0.8f, 1.1f, 0f, id, rng);
                                float fallYaw = yaw + side * rng.Range(60f, 120f);
                                float fy = fallYaw * (float)Math.PI / 180f, cx = (float)Math.Cos(fy), cz = -(float)Math.Sin(fy);
                                AddPiece(layout, PieceKind.FallenColumn, px + cx * 3.6f, pz + cz * 3.6f, fallYaw, 5f, 1f, 1f, 0f, id, rng);
                                layout.Obstacles.Add(new Capsule(px + cx * 1.4f, pz + cz * 1.4f, px + cx * 5.8f, pz + cz * 5.8f, 0.55f));
                            }
                            layout.Obstacles.Add(Capsule.Circle(px, pz, 0.65f));
                        }
                    AddPiece(layout, PieceKind.Altar, WX(0f, d * 0.5f - 2.5f), WZ(0f, d * 0.5f - 2.5f), yaw, 2.4f, 1.1f, 1.4f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(WX(0f, d * 0.5f - 2.5f), WZ(0f, d * 0.5f - 2.5f), 1.2f));
                    layout.Spots.Add(new Spot { X = x, Z = z, Prefer = FeatureKind.Shrine, Landmark = id });
                    lm.Top = top;
                    lm.Radius = d * 0.5f + 3f;
                    break;
                }
                case LandmarkKind.Ravine:
                {
                    float len = rng.Range(40f, 54f), depth = rng.Range(4.5f, 6f), width = 7f;
                    float ax = WX(0f, -len * 0.5f), az = WZ(0f, -len * 0.5f), bx = WX(0f, len * 0.5f), bz = WZ(0f, len * 0.5f);
                    float ramp = depth * 2.4f;
                    Landforms.Ravine(f, ax, az, bx, bz, width, depth, ramp);
                    layout.PaintWhere((px, pz) =>
                    {
                        float dd = Landforms.SegmentDistance(px, pz, ax, az, bx, bz, out float qx, out float qz);
                        float along = Dist(qx, qz, ax, az);
                        return dd < width * 0.5f - 0.6f && along > ramp * 0.6f && along < len - ramp * 0.6f;
                    }, GroundPaint.Accent, x, z, len * 0.5f + width);
                    // Charred logs fallen across the floor give a little cover.
                    for (int i = 0; i < 3; i++)
                    {
                        float lz = rng.Range(-len * 0.25f, len * 0.25f), lx = rng.Range(-1.2f, 1.2f);
                        float lyaw = yaw + 90f + rng.Range(-30f, 30f);
                        AddPiece(layout, PieceKind.FallenColumn, WX(lx, lz), WZ(lx, lz), lyaw, 4f, 0.8f, 0.8f, 0f, id, rng);
                        float ly = lyaw * (float)Math.PI / 180f, cx = (float)Math.Cos(ly), cz = -(float)Math.Sin(ly);
                        layout.Obstacles.Add(new Capsule(WX(lx, lz) - cx * 1.8f, WZ(lx, lz) - cz * 1.8f, WX(lx, lz) + cx * 1.8f, WZ(lx, lz) + cz * 1.8f, 0.45f));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        float lz = rng.Range(-len * 0.3f, len * 0.3f);
                        AddPiece(layout, PieceKind.Embers, WX(rng.Range(-2f, 2f), lz), WZ(rng.Range(-2f, 2f), lz), 0f, 2.4f, 0.3f, 2.4f, 0f, id, rng);
                    }
                    layout.Spots.Add(new Spot { X = WX(1.5f, len * 0.12f), Z = WZ(1.5f, len * 0.12f), Prefer = FeatureKind.Chest, Landmark = id });
                    lm.Top = f.Sample(x, z);
                    lm.Radius = len * 0.5f + 3f;
                    break;
                }
                case LandmarkKind.Ridge:
                {
                    float ra = rng.Range(6f, 7.5f), rb = rng.Range(14f, 19f), height = rng.Range(4f, 5.5f);
                    float top = MeanHeight(f, x, z, rb * 0.7f) + height;
                    Landforms.Plateau(f, x, z, ra, rb, yr, top, 2.6f);
                    float rampLen = height * 2.3f;
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float bx = WX(0f, s * (rb - 1.5f)), bz = WZ(0f, s * (rb - 1.5f));
                        float ax = WX(s * 1.5f, s * (rb + rampLen)), az = WZ(s * 1.5f, s * (rb + rampLen));
                        Landforms.Ramp(f, ax, az, bx, bz, top, 4.4f);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        float lx = rng.Range(-ra * 0.5f, ra * 0.5f), lz = rng.Range(-rb * 0.6f, rb * 0.6f);
                        AddPiece(layout, PieceKind.BrokenColumn, WX(lx, lz), WZ(lx, lz), rng.Range(0f, 360f), 1f, rng.Range(1.2f, 3f), 1f, 0f, id, rng);
                        layout.Obstacles.Add(Capsule.Circle(WX(lx, lz), WZ(lx, lz), 0.6f));
                    }
                    layout.Spots.Add(new Spot { X = x, Z = z, Prefer = FeatureKind.Shrine, Landmark = id });
                    lm.Top = top;
                    lm.Radius = rb + rampLen;
                    break;
                }
                case LandmarkKind.GreatStump:
                {
                    float r = rng.Range(3.4f, 4.4f);
                    TerrainGenerator.FlattenDisc(f, x, z, r + 3f, 4f);
                    AddPiece(layout, PieceKind.Stump, x, z, yaw, r * 2f, rng.Range(6f, 8.5f), r * 2f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(x, z, r + 0.4f));
                    layout.PaintDisc(x, z, r + 5f, GroundPaint.Accent);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = rng.Range(0f, (float)Math.PI * 2f), dd = r + rng.Range(1.5f, 4f);
                        AddPiece(layout, PieceKind.Embers, x + (float)Math.Cos(a) * dd, z + (float)Math.Sin(a) * dd, 0f, 2f, 0.3f, 2f, 0f, id, rng);
                    }
                    layout.Spots.Add(new Spot { X = WX(0f, r + 3f), Z = WZ(0f, r + 3f), Prefer = FeatureKind.Chest, Landmark = id });
                    lm.Radius = r + 6f;
                    break;
                }
                case LandmarkKind.EmberHollow:
                {
                    float r = rng.Range(10f, 12.5f);
                    TerrainGenerator.FlattenDisc(f, x, z, r, 3f);
                    Landforms.Hollow(f, x, z, r, rng.Range(2.6f, 3.4f));
                    layout.PaintDisc(x, z, r * 0.55f, GroundPaint.Accent);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.257f + rng.Range(0f, 0.6f), dd = rng.Range(2.5f, r * 0.5f);
                        AddPiece(layout, PieceKind.Embers, x + (float)Math.Cos(a) * dd, z + (float)Math.Sin(a) * dd, 0f, 2.6f, 0.3f, 2.6f, 0f, id, rng);
                    }
                    int n = rng.Range(4, 7);
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * (float)Math.PI * 2f / n + rng.Range(-0.2f, 0.2f);
                        float px = x + (float)Math.Cos(a) * r * 0.82f, pz = z + (float)Math.Sin(a) * r * 0.82f;
                        AddPiece(layout, PieceKind.BrokenColumn, px, pz, rng.Range(0f, 360f), 1f, rng.Range(1.6f, 4.2f), 1f, rng.Range(-8f, 8f), id, rng);
                        layout.Obstacles.Add(Capsule.Circle(px, pz, 0.6f));
                    }
                    layout.Spots.Add(new Spot { X = x, Z = z, Prefer = FeatureKind.Shrine, Landmark = id });
                    lm.Top = f.Sample(x, z);
                    lm.Radius = r + 2f;
                    break;
                }
                case LandmarkKind.Wreck:
                {
                    AddPiece(layout, PieceKind.Hull, x, z, yaw, 16f, 4.5f, 5f, rng.Range(14f, 24f), id, rng);
                    float hx = rx, hz = rz;
                    layout.Obstacles.Add(new Capsule(x - hx * 6f, z - hz * 6f, x + hx * 6f, z + hz * 6f, 2.4f));
                    layout.Spots.Add(new Spot { X = WX(2f, 5.5f), Z = WZ(2f, 5.5f), Prefer = FeatureKind.Chest, Landmark = id });
                    break;
                }
                case LandmarkKind.RuinedHouse:
                {
                    float w = rng.Range(6.5f, 8.5f), d = rng.Range(5.5f, 7f);
                    TerrainGenerator.FlattenDisc(f, x, z, Math.Max(w, d) * 0.6f, 3f);
                    layout.PaintWhere((px, pz) => InBox(px - x, pz - z, yr, w * 0.5f - 0.4f, d * 0.5f - 0.4f), GroundPaint.Stone, x, z, w);
                    // Back wall and two side walls; the front (local +z) stands open.
                    var sides = new[] { (0f, -d * 0.5f, w, 0f), (-w * 0.5f, 0f, d, 90f), (w * 0.5f, 0f, d, 90f) };
                    foreach (var (lx, lz, len, turn) in sides)
                    {
                        int segs = Math.Max(1, (int)Math.Round(len / 2.6f));
                        float seg = len / segs;
                        for (int i = 0; i < segs; i++)
                        {
                            float t = -len * 0.5f + seg * (i + 0.5f);
                            float px = turn == 0f ? lx + t : lx, pz = turn == 0f ? lz : lz + t;
                            if (rng.Chance(0.2f)) continue;
                            bool rubble = rng.Chance(0.3f);
                            float wx = WX(px, pz), wz = WZ(px, pz);
                            AddPiece(layout, rubble ? PieceKind.Rubble : PieceKind.Wall, wx, wz, yaw + turn, seg + 0.05f, rubble ? rng.Range(0.6f, 1f) : rng.Range(1.3f, 3f), 0.8f, 0f, id, rng);
                            float ty = (yaw + turn) * (float)Math.PI / 180f, tx = (float)Math.Cos(ty) * seg * 0.5f, tz = -(float)Math.Sin(ty) * seg * 0.5f;
                            layout.Obstacles.Add(new Capsule(wx - tx, wz - tz, wx + tx, wz + tz, 0.45f));
                        }
                    }
                    layout.Spots.Add(new Spot { X = x, Z = z, Prefer = rng.Chance(0.6f) ? FeatureKind.Chest : FeatureKind.Shrine, Landmark = id });
                    lm.Radius = Math.Max(w, d) * 0.5f + 3f;
                    break;
                }
                case LandmarkKind.Outcrop:
                {
                    float ra = rng.Range(5f, 8f), rb = rng.Range(4f, 6f), height = rng.Range(1.6f, 2.1f);
                    float top = MeanHeight(f, x, z, ra) + height;
                    Landforms.Plateau(f, x, z, ra, rb, yr, top, 1.2f);
                    int n = rng.Range(1, 3);
                    for (int i = 0; i < n; i++)
                    {
                        float lx = rng.Range(-ra * 0.5f, ra * 0.5f), lz = rng.Range(-rb * 0.4f, rb * 0.4f), s = rng.Range(1.2f, 2f);
                        AddPiece(layout, PieceKind.Boulder, WX(lx, lz), WZ(lx, lz), rng.Range(0f, 360f), s * 2f, s * 1.4f, s * 1.7f, 0f, id, rng);
                        layout.Obstacles.Add(Capsule.Circle(WX(lx, lz), WZ(lx, lz), s * 0.85f));
                    }
                    if (rng.Chance(0.35f))
                    {
                        float lx = rng.Range(-ra * 0.3f, ra * 0.3f);
                        layout.Spots.Add(new Spot { X = WX(lx, rb * 0.45f), Z = WZ(lx, rb * 0.45f), Prefer = FeatureKind.Chest, Landmark = id });
                    }
                    lm.Top = top;
                    lm.Radius = ra + 2f;
                    break;
                }
                case LandmarkKind.SeaTower:
                {
                    float r = rng.Range(8f, 10f), height = rng.Range(5f, 7f);
                    float top = MeanHeight(f, x, z, r) + height;
                    Landforms.Plateau(f, x, z, r, r, 0f, top, 2.6f);
                    float rampLen = height * 2.3f;
                    float a = rng.Range(0f, (float)Math.PI * 2f);
                    float ex = (float)Math.Cos(a), ez = (float)Math.Sin(a);
                    Landforms.Ramp(f, x + ex * (r + rampLen), z + ez * (r + rampLen), x + ex * (r - 1.5f), z + ez * (r - 1.5f), top, 4.4f);
                    float tx = x - ex * r * 0.35f, tz = z - ez * r * 0.35f;
                    AddPiece(layout, PieceKind.Tower, tx, tz, rng.Range(0f, 360f), 5.4f, rng.Range(9f, 12f), 5.4f, 0f, id, rng);
                    layout.Obstacles.Add(Capsule.Circle(tx, tz, 2.9f));
                    layout.PaintDisc(x, z, r - 1.5f, GroundPaint.Stone);
                    layout.Spots.Add(new Spot { X = x + ex * r * 0.4f, Z = z + ez * r * 0.4f, Prefer = FeatureKind.Chest, Landmark = id });
                    lm.Top = top;
                    lm.Radius = r + rampLen;
                    break;
                }
            }
            layout.Landmarks.Add(lm);
        }

        static void AddPiece(LevelLayout layout, PieceKind kind, float x, float z, float yaw, float w, float h, float d, float lean, int landmark, Rng rng)
        {
            layout.Pieces.Add(new Piece { Kind = kind, X = x, Z = z, Yaw = yaw, Width = w, Height = h, Depth = d, Lean = lean, Variant = rng.Range(0, 1 << 16), Landmark = landmark });
        }

        static float EllipseRadius(float a, float b, float angle)
        {
            float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
            return a * b / (float)Math.Sqrt(b * b * c * c + a * a * s * s);
        }

        static bool InEllipse(float dx, float dz, float yawRad, float a, float b)
        {
            float cos = (float)Math.Cos(yawRad), sin = (float)Math.Sin(yawRad);
            float lx = dx * cos - dz * sin, lz = dx * sin + dz * cos;
            return lx * lx / (a * a) + lz * lz / (b * b) <= 1f;
        }

        static bool InBox(float dx, float dz, float yawRad, float a, float b)
        {
            float cos = (float)Math.Cos(yawRad), sin = (float)Math.Sin(yawRad);
            float lx = dx * cos - dz * sin, lz = dx * sin + dz * cos;
            return Math.Abs(lx) <= a && Math.Abs(lz) <= b;
        }

        static float AngleGap(float a, float b)
        {
            float d = (float)Math.IEEERemainder(a - b, Math.PI * 2.0);
            return Math.Abs(d);
        }

        /// <summary>Yaw in degrees for a direction on the ground, matching Unity's y rotation.</summary>
        static float Deg(float dx, float dz) => (float)(Math.Atan2(dz, dx) * -180.0 / Math.PI);

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
