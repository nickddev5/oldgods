using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class HeightFieldTests
    {
        [Test]
        public void SampleMatchesCornersAndTriangles()
        {
            var f = new HeightField(1, 2f, 0f, 0f);
            f[0, 0] = 0f; f[1, 0] = 2f; f[0, 1] = 4f; f[1, 1] = 6f;
            Assert.AreEqual(0f, f.Sample(0f, 0f), 1e-4f);
            Assert.AreEqual(2f, f.Sample(2f, 0f), 1e-4f);
            Assert.AreEqual(4f, f.Sample(0f, 2f), 1e-4f);
            Assert.AreEqual(6f, f.Sample(2f, 2f), 1e-4f);
            // A planar quad: both triangles agree with the plane h = x + 2z.
            Assert.AreEqual(1f + 1f, f.Sample(1f, 0.5f), 1e-4f);
            Assert.AreEqual(0.5f + 3f, f.Sample(0.5f, 1.5f), 1e-4f);
        }

        [Test]
        public void SampleFollowsTheDiagonalSplit()
        {
            // Only the far corner is raised; along the diagonal the surface rises linearly,
            // off it the lower-right triangle stays flat in u.
            var f = new HeightField(1, 1f, 0f, 0f);
            f[1, 1] = 1f;
            Assert.AreEqual(0.5f, f.Sample(0.5f, 0.5f), 1e-4f);
            Assert.AreEqual(0.25f, f.Sample(0.75f, 0.25f), 1e-4f);
            Assert.AreEqual(0.25f, f.Sample(0.25f, 0.75f), 1e-4f);
        }

        [Test]
        public void SampleClampsOutsideTheField()
        {
            var f = new HeightField(2, 1f, -1f, -1f);
            f[0, 0] = 3f;
            Assert.AreEqual(3f, f.Sample(-50f, -50f), 1e-4f);
        }

        [Test]
        public void FlatGroundHasUpNormalAndZeroSlope()
        {
            var f = new HeightField(4, 1f, 0f, 0f);
            f.Normal(2f, 2f, out float nx, out float ny, out float nz);
            Assert.AreEqual(0f, nx, 1e-4f);
            Assert.AreEqual(1f, ny, 1e-4f);
            Assert.AreEqual(0f, nz, 1e-4f);
            Assert.AreEqual(0f, f.SlopeDegrees(2f, 2f), 1e-3f);
        }
    }

    public class TerrainGeneratorTests
    {
        [Test]
        public void SameSeedSameMap()
        {
            var p = new TerrainProfile { Cells = 32 };
            var a = TerrainGenerator.Generate(p, new RunSeed(5).Stream(RunSeed.Map));
            var b = TerrainGenerator.Generate(p, new RunSeed(5).Stream(RunSeed.Map));
            CollectionAssert.AreEqual(a.Heights, b.Heights);
            var c = TerrainGenerator.Generate(p, new RunSeed(6).Stream(RunSeed.Map));
            CollectionAssert.AreNotEqual(a.Heights, c.Heights);
        }

        [Test]
        public void SpawnAreaIsFlat()
        {
            var p = new TerrainProfile { Cells = 64, CellSize = 2f, HillHeight = 20f, SpawnFlatRadius = 10f };
            var f = TerrainGenerator.Generate(p, new Rng(42));
            float centre = f.Sample(0f, 0f);
            for (float x = -8f; x <= 8f; x += 2f)
                for (float z = -8f; z <= 8f; z += 2f)
                    if (x * x + z * z <= 64f)
                        Assert.AreEqual(centre, f.Sample(x, z), 0.05f);
        }

        [Test]
        public void RimIsHigherThanTheInterior()
        {
            var p = new TerrainProfile { Cells = 64, CellSize = 2f, HillHeight = 4f, RimHeight = 15f, RimWidth = 12f };
            var f = TerrainGenerator.Generate(p, new Rng(1));
            Assert.Greater(f.Sample(f.MinX, 0f), f.Sample(0f, 0f) + 8f);
            Assert.IsTrue(TerrainGenerator.InPlayableArea(f, 0f, 0f, p.RimWidth));
            Assert.IsFalse(TerrainGenerator.InPlayableArea(f, f.MinX + 1f, 0f, p.RimWidth));
        }

        [Test]
        public void NoiseIsBoundedAndSeeded()
        {
            var n1 = new Noise2D(new Rng(9));
            var n2 = new Noise2D(new Rng(9));
            for (int i = 0; i < 1000; i++)
            {
                float x = i * 0.37f, z = i * 0.91f;
                float v = n1.Fbm(x, z, 4);
                Assert.That(v, Is.InRange(-1.5f, 1.5f));
                Assert.AreEqual(v, n2.Fbm(x, z, 4));
            }
        }
    }
}
