using System.Collections.Generic;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class RunSeedTests
    {
        [Test]
        public void SameSeedAndStreamGiveSameSequence()
        {
            var a = new RunSeed(1234).Stream(RunSeed.Map);
            var b = new RunSeed(1234).Stream(RunSeed.Map);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void StreamsAreIndependent()
        {
            var seed = new RunSeed(1234);
            var map = seed.Stream(RunSeed.Map);
            var spawns = seed.Stream(RunSeed.Spawns);
            Assert.AreNotEqual(map.NextULong(), spawns.NextULong());

            // Drawing from one stream does not shift another.
            var draftA = seed.Stream(RunSeed.Draft);
            var lootBurn = seed.Stream(RunSeed.Loot);
            for (int i = 0; i < 50; i++) lootBurn.NextULong();
            var draftB = new RunSeed(1234).Stream(RunSeed.Draft);
            Assert.AreEqual(draftA.NextULong(), draftB.NextULong());
        }

        [Test]
        public void IndexedStreamsDiffer()
        {
            var seed = new RunSeed(99);
            Assert.AreNotEqual(seed.Stream(RunSeed.Map, 0).NextULong(), seed.Stream(RunSeed.Map, 1).NextULong());
        }

        [Test]
        public void SeedRoundTripsThroughText()
        {
            var seed = new RunSeed(0xDEADBEEF12345678UL);
            Assert.IsTrue(RunSeed.TryParse(seed.ToString(), out var parsed));
            Assert.AreEqual(seed.Value, parsed.Value);
            Assert.IsFalse(RunSeed.TryParse("not hex", out _));
        }

        [Test]
        public void RangesStayInBounds()
        {
            var rng = new Rng(7);
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.Range(-3, 5);
                Assert.That(v, Is.InRange(-3, 4));
                float f = rng.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void PickWeightedRespectsWeights()
        {
            var rng = new Rng(11);
            var weights = new List<float> { 0f, 1f, 3f };
            var counts = new int[3];
            for (int i = 0; i < 20000; i++) counts[rng.PickWeighted(weights)]++;
            Assert.AreEqual(0, counts[0]);
            Assert.That(counts[2] / (float)counts[1], Is.InRange(2.7f, 3.3f));
            Assert.AreEqual(-1, rng.PickWeighted(new List<float> { 0f, 0f }));
        }
    }

    public class HordeMathTests
    {
        [Test]
        public void SpatialHashFindsNeighboursAcrossCells()
        {
            var xs = new[] { 0f, 0.9f, 1.1f, 10f, -0.5f };
            var zs = new[] { 0f, 0f, 0.2f, 10f, -0.5f };
            var hash = new SpatialHash(1f);
            hash.Build(xs, zs, null, xs.Length);
            var found = new int[16];
            int n = hash.Query(0f, 0f, 1.2f, found);
            var set = new HashSet<int>();
            for (int i = 0; i < n; i++) set.Add(found[i]);
            Assert.IsTrue(set.Contains(0));
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(4));
            Assert.IsFalse(set.Contains(3));
        }

        [Test]
        public void SpatialHashSkipsDeadEntries()
        {
            var xs = new[] { 0f, 0.1f };
            var zs = new[] { 0f, 0.1f };
            var hash = new SpatialHash(1f);
            hash.Build(xs, zs, new[] { true, false }, 2);
            var found = new int[4];
            int n = hash.Query(0f, 0f, 1f, found);
            Assert.AreEqual(1, n);
            Assert.AreEqual(0, found[0]);
        }

        [Test]
        public void SteeringSeeksTargetWhenAlone()
        {
            var xs = new[] { 0f };
            var zs = new[] { 0f };
            var hash = new SpatialHash(1f);
            hash.Build(xs, zs, null, 1);
            var v = HordeSteering.Desired(0, xs, zs, new Vec2(10f, 0f), 3f, 1f, 1f, hash, new int[8]);
            Assert.AreEqual(3f, v.X, 1e-4f);
            Assert.AreEqual(0f, v.Z, 1e-4f);
        }

        [Test]
        public void SteeringSeparatesOverlappingEnemies()
        {
            var xs = new[] { 0f, 0.2f };
            var zs = new[] { 0f, 0f };
            var hash = new SpatialHash(1f);
            hash.Build(xs, zs, null, 2);
            // Target is straight up the z axis; separation should push enemy 0 toward -x.
            var v = HordeSteering.Desired(0, xs, zs, new Vec2(0f, 10f), 3f, 1f, 1f, hash, new int[8]);
            Assert.Less(v.X, 0f);
            Assert.Greater(v.Z, 0f);
        }

        [Test]
        public void ExactOverlapStillSeparates()
        {
            var xs = new[] { 1f, 1f };
            var zs = new[] { 1f, 1f };
            var hash = new SpatialHash(1f);
            hash.Build(xs, zs, null, 2);
            var a = HordeSteering.Desired(0, xs, zs, new Vec2(1f, 1f), 3f, 1f, 1f, hash, new int[8]);
            var b = HordeSteering.Desired(1, xs, zs, new Vec2(1f, 1f), 3f, 1f, 1f, hash, new int[8]);
            Assert.Greater(a.Magnitude, 0f);
            Assert.Greater((a - b).Magnitude, 0f);
        }

        [Test]
        public void AccelerateIsLimited()
        {
            var v = HordeSteering.Accelerate(default, new Vec2(10f, 0f), 5f, 0.1f);
            Assert.AreEqual(0.5f, v.X, 1e-4f);
            var w = HordeSteering.Accelerate(new Vec2(9.9f, 0f), new Vec2(10f, 0f), 5f, 0.1f);
            Assert.AreEqual(10f, w.X, 1e-4f);
        }

        [Test]
        public void RingPointIsInsideRing()
        {
            var rng = new Rng(3);
            for (int i = 0; i < 500; i++)
            {
                var p = HordeSteering.RingPoint(new Vec2(5f, 5f), 20f, 30f, rng);
                float d = Vec2.Distance(p, new Vec2(5f, 5f));
                Assert.That(d, Is.InRange(19.999f, 30.001f));
            }
        }
    }

    public class PlayerRulesTests
    {
        [Test]
        public void JumpVelocityReachesHeight()
        {
            float g = 28f, h = 2.2f;
            float v = PlayerRules.JumpVelocity(h, g);
            Assert.AreEqual(h, v * v / (2f * g), 1e-4f);
        }

        [Test]
        public void NoFallDamageBelowSafeSpeed()
        {
            var t = new MotorTuning();
            Assert.AreEqual(0f, PlayerRules.FallDamage(t.SafeFallSpeed - 0.1f, t));
            Assert.AreEqual(2f * t.FallDamagePerSpeed, PlayerRules.FallDamage(t.SafeFallSpeed + 2f, t), 1e-4f);
        }

        [Test]
        public void FallsHurtButNeverKill()
        {
            var t = new MotorTuning();
            float hard = t.SafeFallSpeed + 30f;
            Assert.AreEqual(PlayerRules.FallDamage(hard, t), PlayerRules.FallDamage(hard, t, 500f), 1e-4f, "full damage with health to spare");
            Assert.AreEqual(39f, PlayerRules.FallDamage(hard, t, 40f), 1e-4f, "leaves 1 health");
            Assert.AreEqual(0f, PlayerRules.FallDamage(hard, t, 1f), 1e-4f);
            Assert.AreEqual(0f, PlayerRules.FallDamage(t.SafeFallSpeed - 1f, t, 40f), 1e-4f);
        }

        [Test]
        public void AirControlIsWeakerThanGround()
        {
            var t = new MotorTuning();
            var ground = PlayerRules.StepRun(default, new Vec2(1f, 0f), true, 1f, t, 0.02f);
            var air = PlayerRules.StepRun(default, new Vec2(1f, 0f), false, 1f, t, 0.02f);
            Assert.Greater(ground.X, air.X);
            Assert.Greater(air.X, 0f);
        }

        [Test]
        public void AirKeepsSlideMomentumAndSteers()
        {
            var t = new MotorTuning();
            var fast = new Vec2(t.RunSpeed * 2f, 0f);
            var coast = PlayerRules.StepRun(fast, default, false, 1f, t, 0.1f);
            Assert.Greater(coast.X, t.RunSpeed * 1.9f, "no input keeps nearly all the speed");
            var steer = PlayerRules.StepRun(fast, new Vec2(0f, 1f), false, 1f, t, 0.1f);
            Assert.Greater(steer.Z, 0f, "input turns the motion");
            Assert.Greater(steer.Magnitude, t.RunSpeed * 1.9f, "turning does not brake");
            var ground = PlayerRules.StepRun(fast, new Vec2(1f, 0f), true, 1f, t, 0.1f);
            Assert.Less(ground.Magnitude, coast.Magnitude, "on the ground it settles back to a run");
        }

        [Test]
        public void FallingIsFasterThanRising()
        {
            var t = new MotorTuning();
            Assert.Greater(PlayerRules.GravityFor(-1f, t), PlayerRules.GravityFor(1f, t));
        }

        [Test]
        public void SlideGainsDownhillAndStopsOnFlat()
        {
            var t = new MotorTuning();
            float downhill = PlayerRules.StepSlide(10f, 0.5f, t, 0.1f);
            float flat = PlayerRules.StepSlide(10f, 0f, t, 0.1f);
            Assert.Greater(downhill, flat);
            Assert.Less(flat, 10f);
            float s = 10f;
            int steps = 0;
            while (!PlayerRules.SlideEnded(s, t) && steps++ < 1000) s = PlayerRules.StepSlide(s, 0f, t, 0.02f);
            Assert.Less(steps, 1000);
            Assert.LessOrEqual(PlayerRules.StepSlide(t.SlideMaxSpeed, 1f, t, 1f), t.SlideMaxSpeed);
        }

        [Test]
        public void HealthHonoursInvulnerability()
        {
            var h = new Health(100f) { HitInvulnerability = 0.5f };
            Assert.AreEqual(10f, h.Damage(10f));
            Assert.AreEqual(0f, h.Damage(10f));
            h.Tick(0.6f);
            Assert.AreEqual(10f, h.Damage(10f));
            Assert.AreEqual(80f, h.Current);
            h.Heal(500f);
            Assert.AreEqual(100f, h.Current);
            h.Tick(1f);
            h.Damage(1000f);
            Assert.IsTrue(h.IsDead);
            h.Heal(10f);
            Assert.IsTrue(h.IsDead);
        }
    }

    public class SaveModelTests
    {
        [Test]
        public void CurrentVersionMigratesAndFillsNulls()
        {
            var data = new SaveData { unlocked = null, settings = null };
            Assert.IsTrue(SaveMigration.Migrate(data));
            Assert.IsNotNull(data.unlocked);
            Assert.IsNotNull(data.settings);
        }

        [Test]
        public void VersionOneRenamesTheHuntGodToBeast()
        {
            var data = new SaveData { version = 1, unlocked = new List<string> { "god.storm", "god.hunt" } };
            data.lastRun.god = "god.hunt";
            Assert.IsTrue(SaveMigration.Migrate(data));
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            CollectionAssert.AreEquivalent(new[] { "god.storm", "god.beast" }, data.unlocked);
            Assert.AreEqual("god.beast", data.lastRun.god);
        }

        [Test]
        public void UnknownVersionsAreRejected()
        {
            Assert.IsFalse(SaveMigration.Migrate(new SaveData { version = 0 }));
            Assert.IsFalse(SaveMigration.Migrate(new SaveData { version = SaveData.CurrentVersion + 1 }));
            Assert.IsFalse(SaveMigration.Migrate(null));
        }

        [Test]
        public void UnlockIsIdempotent()
        {
            var data = new SaveData();
            data.Unlock("god.storm");
            data.Unlock("god.storm");
            Assert.AreEqual(1, data.unlocked.Count);
            Assert.IsTrue(data.IsUnlocked("god.storm"));
        }
    }

    public class ContentSetTests
    {
        [Test]
        public void DuplicateIdsAreRejected()
        {
            var set = new ContentSet();
            set.Add(new EnemyDef { Id = "a" });
            Assert.Throws<System.ArgumentException>(() => set.Add(new EnemyDef { Id = "a" }));
        }

        [Test]
        public void ValidateFindsBadStats()
        {
            var set = new ContentSet();
            set.Add(new EnemyDef { Id = "bad", MaxHealth = 0f });
            Assert.AreEqual(1, set.Validate().Count);
        }
    }
}
