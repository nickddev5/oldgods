using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class LandformTests
    {
        static HeightField Flat(int cells = 64, float cell = 2f)
        {
            float size = cells * cell;
            return new HeightField(cells, cell, -size / 2f, -size / 2f);
        }

        [Test]
        public void PlateauIsFlatOnTopWithACliffEdge()
        {
            var f = Flat();
            Landforms.Plateau(f, 0f, 0f, 10f, 10f, 0f, 5f, 1.6f);
            Assert.AreEqual(5f, f.Sample(0f, 0f), 1e-4f);
            Assert.AreEqual(5f, f.Sample(8f, 0f), 1e-4f);
            Assert.AreEqual(0f, f.Sample(14f, 0f), 1e-4f);
            Assert.Greater(f.SlopeDegrees(10.5f, 0f), 50f, "the side is a cliff");
        }

        [Test]
        public void RampClimbsSteadilyToThePlateau()
        {
            var f = Flat();
            Landforms.Plateau(f, 0f, 0f, 10f, 10f, 0f, 5f, 1.6f);
            Landforms.Ramp(f, 22f, 0f, 8.5f, 0f, 5f, 4.6f);
            float last = -1f;
            for (float x = 22f; x >= 9f; x -= 1f)
            {
                float h = f.Sample(x, 0f);
                Assert.GreaterOrEqual(h, last - 1e-3f, $"the ramp never dips (x = {x})");
                last = h;
                Assert.Less(f.SlopeDegrees(x, 0f), 32f, $"the ramp is walkable (x = {x})");
            }
            Assert.AreEqual(5f, f.Sample(9f, 0f), 0.3f);
        }

        [Test]
        public void RavineIsDeepInTheMiddleAndOpenAtTheEnds()
        {
            var f = Flat();
            Landforms.Ravine(f, 0f, -20f, 0f, 20f, 7f, 5f, 12f);
            Assert.AreEqual(-5f, f.Sample(0f, 0f), 0.05f);
            Assert.AreEqual(0f, f.Sample(0f, -20f), 0.05f);
            Assert.AreEqual(0f, f.Sample(8f, 0f), 0.05f);
        }

        [Test]
        public void StripRaisesACausewayAboveWater()
        {
            var f = Flat();
            Landforms.Strip(f, new[] { -30f, 30f }, new[] { 0f, 0f }, 5f, 2f);
            Assert.AreEqual(2f, f.Sample(0f, 0f), 1e-3f);
            Assert.AreEqual(0f, f.Sample(0f, 20f), 1e-3f);
        }
    }

    public class FlowFieldTests
    {
        static HeightField Flat(int cells = 40, float cell = 2f)
        {
            float size = cells * cell;
            return new HeightField(cells, cell, -size / 2f, -size / 2f);
        }

        [Test]
        public void PushOutLeavesTheCircleTouchingTheShape()
        {
            var o = new Obstacles();
            o.Add(new Capsule(-5f, 0f, 5f, 0f, 1f));
            float x = 1f, z = 0.4f;
            Assert.IsTrue(o.PushOut(ref x, ref z, 0.5f));
            Assert.AreEqual(1.5f, z, 1e-3f);
            Assert.IsFalse(o.Overlaps(x, z, 0.49f));
            Assert.IsTrue(o.Overlaps(0f, 0f, 0.1f));
            Assert.IsFalse(o.Overlaps(0f, 10f, 0.5f));
        }

        [Test]
        public void OpenGroundPointsStraightAtTheTarget()
        {
            var f = Flat();
            var flow = FlowField.FromTerrain(f, null, 0f);
            flow.Solve(0f, 0f);
            Assert.IsTrue(flow.Direction(-20f, 0.5f, out float dx, out float dz));
            Assert.Greater(dx, 0.9f);
            Assert.AreEqual(20f, flow.Distance(-20f, 0.5f), 2.5f);
        }

        [Test]
        public void PathsGoThroughTheGapInAWall()
        {
            // A wall along x = 0 with a gap near z = 30; the target is on the far side.
            var f = Flat();
            var o = new Obstacles();
            o.Add(new Capsule(0f, -40f, 0f, 24f, 0.7f));
            var flow = FlowField.FromTerrain(f, o, 0f);
            flow.Solve(10f, 0f);
            Assert.AreEqual(CellClass.Solid, flow.ClassAt(0.5f, 0f));
            Assert.IsTrue(flow.Reachable(-10f, 0f));
            Assert.Greater(flow.Distance(-10f, 0f), 50f, "the way round is long");
            Assert.IsTrue(flow.Direction(-10f, 0f, out _, out float dz));
            Assert.Greater(dz, 0.5f, "head for the gap");
        }

        [Test]
        public void CliffsBlockAndRampsOpenAPlateau()
        {
            var f = Flat();
            Landforms.Plateau(f, 0f, 0f, 10f, 10f, 0f, 6f, 1.6f);
            var flow = FlowField.FromTerrain(f, null, 0f);
            flow.Solve(0f, 0f);
            Assert.IsFalse(flow.Reachable(-30f, 0f), "no way up without a ramp");

            Landforms.Ramp(f, 24f, 0f, 8.5f, 0f, 6f, 4.6f);
            flow = FlowField.FromTerrain(f, null, 0f);
            flow.Solve(0f, 0f);
            Assert.IsTrue(flow.Reachable(-30f, 0f));
            Assert.IsTrue(flow.Direction(30f, 0f, out float dx, out _));
            Assert.Less(dx, -0.7f, "walk up the ramp");
        }

        [Test]
        public void CellsAgainstAWallStillKnowTheWay()
        {
            var f = Flat();
            var o = new Obstacles();
            o.Add(new Capsule(0f, -40f, 0f, 24f, 0.7f));
            var flow = FlowField.FromTerrain(f, o, 0f);
            flow.Solve(10f, 0f);
            Assert.IsTrue(flow.Reachable(-0.9f, 0f), "an enemy pressed to the wall has a path");
            Assert.IsTrue(flow.Direction(-0.9f, 0f, out _, out _));
        }

        [Test]
        public void TheHordeFollowsThePlayerUpTheRimToTheWall()
        {
            var f = Flat(64);
            TerrainGenerator.RaiseRim(f, 14f, 20f);
            var flow = FlowField.FromTerrain(f, null, 14f);
            float wallX = f.MaxX - TerrainGenerator.WallInset(14f) - 0.01f;
            flow.Solve(wallX, 0f);
            Assert.AreNotEqual(CellClass.Blocked, flow.ClassAt(wallX, 0f), "the player's spot at the wall is walkable");
            Assert.IsTrue(flow.Reachable(0f, 0f), "the arena has a path up to a player at the wall");
            Assert.AreEqual(wallX, flow.Distance(0f, 0f), 3f, "straight up the slope, not around");
            Assert.IsTrue(flow.Direction(f.MaxX - 16f, 0f, out float dx, out _));
            Assert.Greater(dx, 0.9f, "climb towards the player");
            Assert.AreEqual(CellClass.Blocked, flow.ClassAt(f.MaxX - 2f, 0f), "beyond the wall is blocked");
        }
    }

    public class LevelLayoutTests
    {
        static readonly int[] Seeds = { 1, 2, 3, 7, 11, 42 };

        static (HeightField, LevelLayout) Build(Func<TerrainProfile> terrain, Func<LayoutProfile> layout, int seed)
        {
            var t = terrain();
            var f = TerrainGenerator.Generate(t, new Rng((ulong)seed));
            var l = LayoutGenerator.Build(f, t.RimWidth, layout(), new Rng((ulong)seed * 31 + 5));
            return (f, l);
        }

        static TerrainProfile Steppe() => new TerrainProfile { HillHeight = 18f, HillScale = 65f, CliffStep = 3f, CliffAmount = 0.1f, RidgeHeight = 9f, RidgeScale = 120f };
        static TerrainProfile Wood() => new TerrainProfile { HillHeight = 22f, HillScale = 40f, CliffStep = 3.5f, CliffAmount = 0.25f, RidgeHeight = 8f, RidgeScale = 70f };
        static TerrainProfile Coast() => new TerrainProfile { HillHeight = 16f, HillScale = 60f, CliffStep = 4f, CliffAmount = 0.35f, WaterLevel = 6.5f };

        static IEnumerable<(string, Func<TerrainProfile>, Func<LayoutProfile>)> Biomes()
        {
            yield return ("steppe", Steppe, LayoutPresets.GreySteppe);
            yield return ("wood", Wood, LayoutPresets.AshWood);
            yield return ("coast", Coast, () => LayoutPresets.DrownedCoast(6.5f));
        }

        [Test]
        public void SameSeedSameLayout()
        {
            var (fa, a) = Build(Steppe, LayoutPresets.GreySteppe, 9);
            var (fb, b) = Build(Steppe, LayoutPresets.GreySteppe, 9);
            CollectionAssert.AreEqual(fa.Heights, fb.Heights);
            Assert.AreEqual(a.Pieces.Count, b.Pieces.Count);
            Assert.AreEqual(a.Landmarks.Select(l => l.ToString()), b.Landmarks.Select(l => l.ToString()));
        }

        [Test]
        public void EveryBiomeGetsItsLandmarksWithoutOverlap()
        {
            foreach (var (name, terrain, profile) in Biomes())
                foreach (int seed in Seeds)
                {
                    var (_, l) = Build(terrain, profile, seed);
                    int wanted = profile().Landmarks.Sum(c => c.Count);
                    Assert.GreaterOrEqual(l.Landmarks.Count, wanted * 3 / 4, $"{name} seed {seed} placed {l.Landmarks.Count} of {wanted}");
                    for (int i = 0; i < l.Landmarks.Count; i++)
                        for (int j = i + 1; j < l.Landmarks.Count; j++)
                        {
                            var a = l.Landmarks[i];
                            var b = l.Landmarks[j];
                            float d = (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
                            Assert.Greater(d, LayoutGenerator.Footprint(a.Kind) + LayoutGenerator.Footprint(b.Kind) - 0.01f, $"{name} seed {seed}: {a} overlaps {b}");
                        }
                    Assert.IsTrue(l.HasGate);
                    Assert.Greater(Math.Sqrt(l.GateX * l.GateX + l.GateZ * l.GateZ), 65.0, "the gate must be found");
                }
        }

        [Test]
        public void TheStartStaysFlatAndClear()
        {
            foreach (var (name, terrain, profile) in Biomes())
            {
                var (f, l) = Build(terrain, profile, 5);
                float centre = f.Sample(0f, 0f);
                for (float x = -8f; x <= 8f; x += 2f)
                    Assert.AreEqual(centre, f.Sample(x, 0f), 0.6f, $"{name}: the start is flat");
                Assert.IsFalse(l.Obstacles.Overlaps(0f, 0f, 6f), $"{name}: nothing solid at the start");
            }
        }

        [Test]
        public void TheHordeCanReachEveryDetourAndTheGate()
        {
            foreach (var (name, terrain, profile) in Biomes())
                foreach (int seed in Seeds)
                {
                    var (f, l) = Build(terrain, profile, seed);
                    var flow = FlowField.FromTerrain(f, l.Obstacles, terrain().RimWidth);
                    // Paths lead to the start, so a cell is reachable when the player could walk from it.
                    flow.Solve(0f, 0f);
                    Assert.IsTrue(flow.Reachable(l.GateX, l.GateZ), $"{name} seed {seed}: the gate is cut off");
                    int reached = l.Spots.Count(s => flow.Reachable(s.X, s.Z));
                    Assert.GreaterOrEqual(reached, l.Spots.Count - 1, $"{name} seed {seed}: {l.Spots.Count - reached} spots are cut off");
                }
        }

        [Test]
        public void FeaturesTakeTheDetourSpotsAndAvoidTheDressing()
        {
            var (f, l) = Build(Steppe, LayoutPresets.GreySteppe, 3);
            var requests = new[]
            {
                new FeatureRequest(FeatureKind.BossGate, 1, 0f),
                new FeatureRequest(FeatureKind.Shrine, 8, 24f),
                new FeatureRequest(FeatureKind.Chest, 10, 22f),
            };
            var placed = MapPlacement.Place(f, 16f, new PlacementRules(), requests, new Rng(4), l);
            var gate = placed.Single(p => p.Kind == FeatureKind.BossGate);
            Assert.AreEqual(l.GateX, gate.X, 1e-3f);
            int onSpots = placed.Count(p => l.Spots.Any(s => Math.Abs(s.X - p.X) < 1e-3f && Math.Abs(s.Z - p.Z) < 1e-3f));
            Assert.GreaterOrEqual(onSpots, 4, "chests and shrines sit at the landmarks");
            foreach (var p in placed)
                Assert.IsFalse(l.Obstacles.Overlaps(p.X, p.Z, 1f), $"{p} is inside the dressing");
        }

        [Test]
        public void SteppeHasARoadAndAWallWithAGate()
        {
            var (_, l) = Build(Steppe, LayoutPresets.GreySteppe, 2);
            Assert.Greater(l.RoadX.Length, 1);
            Assert.IsTrue(l.Pieces.Any(p => p.Kind == PieceKind.Arch));
            Assert.Greater(l.Pieces.Count(p => p.Kind == PieceKind.Wall), 6);
            Assert.AreEqual(GroundPaint.Road, l.PaintAt((l.RoadX[1] + l.RoadX[2]) * 0.5f, (l.RoadZ[1] + l.RoadZ[2]) * 0.5f));
        }
    }
}
