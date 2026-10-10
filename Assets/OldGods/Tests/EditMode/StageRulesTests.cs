using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    public class MapPlacementTests
    {
        static HeightField Flat(int cells = 100, float cell = 2f)
        {
            float size = cells * cell;
            return new HeightField(cells, cell, -size / 2f, -size / 2f);
        }

        static readonly FeatureRequest[] Requests =
        {
            new FeatureRequest(FeatureKind.BossGate, 1, 0f),
            new FeatureRequest(FeatureKind.Chest, 8, 20f),
            new FeatureRequest(FeatureKind.Shrine, 6, 25f),
            new FeatureRequest(FeatureKind.Merchant, 1, 0f),
        };

        [Test]
        public void PlacesEveryRequestOnOpenGround()
        {
            var placed = MapPlacement.Place(Flat(), 10f, new PlacementRules(), Requests, new Rng(1));
            Assert.AreEqual(1, placed.Count(p => p.Kind == FeatureKind.BossGate));
            Assert.AreEqual(8, placed.Count(p => p.Kind == FeatureKind.Chest));
            Assert.AreEqual(6, placed.Count(p => p.Kind == FeatureKind.Shrine));
            Assert.AreEqual(1, placed.Count(p => p.Kind == FeatureKind.Merchant));
        }

        [Test]
        public void RespectsSpacingSpawnDistanceAndEdges()
        {
            var rules = new PlacementRules();
            var field = Flat();
            float rim = 10f;
            var placed = MapPlacement.Place(field, rim, rules, Requests, new Rng(7));
            for (int i = 0; i < placed.Count; i++)
            {
                var a = placed[i];
                float fromSpawn = new Vec2(a.X, a.Z).Magnitude;
                Assert.GreaterOrEqual(fromSpawn, a.Kind == FeatureKind.BossGate ? rules.BossGateMinFromSpawn : rules.MinFromSpawn);
                Assert.Greater(a.X, field.MinX + rim);
                Assert.Less(a.X, field.MaxX - rim);
                for (int j = i + 1; j < placed.Count; j++)
                {
                    var b = placed[j];
                    float d = Vec2.Distance(new Vec2(a.X, a.Z), new Vec2(b.X, b.Z));
                    Assert.GreaterOrEqual(d, rules.MinSpacing - 1e-3f);
                    if (a.Kind == b.Kind && a.Kind == FeatureKind.Chest) Assert.GreaterOrEqual(d, 20f - 1e-3f);
                }
            }
        }

        [Test]
        public void AvoidsSteepGround()
        {
            var field = Flat();
            // A steep ramp over the whole +x half.
            for (int iz = 0; iz <= field.Cells; iz++)
                for (int ix = field.Cells / 2; ix <= field.Cells; ix++)
                    field[ix, iz] = (ix - field.Cells / 2) * 3f;
            var rules = new PlacementRules();
            var placed = MapPlacement.Place(field, 10f, rules, Requests, new Rng(3));
            Assert.IsTrue(placed.All(p => field.SlopeDegrees(p.X, p.Z) <= rules.MaxSlope));
            Assert.IsTrue(placed.All(p => p.X < 4f));
        }

        [Test]
        public void SameSeedSamePlacement()
        {
            var a = MapPlacement.Place(Flat(), 10f, new PlacementRules(), Requests, new Rng(5));
            var b = MapPlacement.Place(Flat(), 10f, new PlacementRules(), Requests, new Rng(5));
            CollectionAssert.AreEqual(a.Select(p => p.ToString()), b.Select(p => p.ToString()));
        }
    }

    public class TimelineTests
    {
        [Test]
        public void GreyboxTimelineIsValid()
        {
            CollectionAssert.IsEmpty(TimelineEvaluator.Validate(DefaultTimelines.Greybox()));
        }

        [Test]
        public void ValidateFindsGapsAndStrayEvents()
        {
            var def = new StageTimelineDef { Duration = 100f };
            def.Phases.Add(new SpawnPhase { Start = 0f, End = 40f, Mix = { new MixEntry("a", 1f) } });
            def.Phases.Add(new SpawnPhase { Start = 50f, End = 90f, Mix = { new MixEntry("a", 1f) } });
            def.Events.Add(new StageEvent { At = 120f });
            var problems = TimelineEvaluator.Validate(def);
            Assert.AreEqual(3, problems.Count, string.Join("; ", problems));
        }

        [Test]
        public void DensityRisesWithinAndAcrossPhases()
        {
            var def = DefaultTimelines.Greybox();
            int prev = -1;
            for (float t = 0f; t < def.Duration; t += 15f)
            {
                int n = TimelineEvaluator.TargetAlive(def, t, 1f);
                Assert.GreaterOrEqual(n, prev, $"at {t}");
                prev = n;
            }
            Assert.Greater(TimelineEvaluator.TargetAlive(def, 100f, Difficulty.Density(Difficulty.Coefficient(20f, 2))),
                TimelineEvaluator.TargetAlive(def, 100f, Difficulty.Density(Difficulty.Coefficient(2f, 0))));
        }

        [Test]
        public void DifficultyClimbsWithTimeAndStagesWithoutCap()
        {
            Assert.AreEqual(1f, Difficulty.Coefficient(0f, 0), 1e-5f);
            Assert.AreEqual(1f + 5f * Difficulty.TimeRate, Difficulty.Coefficient(5f, 0), 1e-4f);
            Assert.AreEqual((1f + 5f * Difficulty.TimeRate) * Difficulty.StageGrowth * Difficulty.StageGrowth, Difficulty.Coefficient(5f, 2), 1e-4f);
            Assert.AreEqual(1f + 5f * Difficulty.TimeRate * 2f, Difficulty.Coefficient(5f, 0, 2f), 1e-4f, "tier scales the time rate");
            float prev = 0f;
            for (float m = 0f; m < 300f; m += 10f)
            {
                float c = Difficulty.Coefficient(m, (int)(m / 9f));
                Assert.Greater(c, prev, $"minute {m}");
                prev = c;
            }
            Assert.Greater(prev, 1000f, "no cap");
            Assert.AreEqual(1f, Difficulty.Damage(1f), 1e-5f);
            Assert.AreEqual(1f, Difficulty.Density(1f), 1e-5f);
            Assert.Greater(Difficulty.Damage(3f), Difficulty.Damage(2f));
            Assert.Greater(Difficulty.Density(3f), Difficulty.Density(2f));
        }

        [Test]
        public void GreySteppePlaysAsBeforeAndLaterStagesClimbHarder()
        {
            // The old tables: health (1 + 0.18 * stage minutes) * (1 + 1.1 * stage).
            float Old(float stageMinutes, int stage) => (1f + 0.18f * stageMinutes) * (1f + 1.1f * stage);
            for (float m = 0f; m <= 10f; m += 1f)
                Assert.AreEqual(Old(m, 0), Difficulty.Health(Difficulty.Coefficient(m, 0)), 0.05f, $"Steppe minute {m}");
            // A typical run: about 8.5 minutes per stage.
            Assert.Greater(Difficulty.Health(Difficulty.Coefficient(8.5f, 1)), Old(0f, 1), "Ash Wood starts harder");
            Assert.Greater(Difficulty.Health(Difficulty.Coefficient(17f, 1)), Old(8.5f, 1), "Ash Wood ends harder");
            Assert.Greater(Difficulty.Health(Difficulty.Coefficient(26f, 2)), Old(9.5f, 2), "Drowned Coast ends harder");
            Assert.Greater(Difficulty.Health(Difficulty.Coefficient(26f, 3)), 2f * Old(0f, 3), "The Last Test's stream is tougher");
        }

        [Test]
        public void EventsFireOnceAcrossFrames()
        {
            var def = DefaultTimelines.Greybox();
            var fired = new List<StageEvent>();
            float prev = 0f;
            for (float t = 0.016f; t <= def.Duration; t += 0.016f)
            {
                fired.AddRange(TimelineEvaluator.EventsBetween(def, prev, t));
                prev = t;
            }
            fired.AddRange(TimelineEvaluator.EventsBetween(def, prev, def.Duration));
            Assert.AreEqual(def.Events.Count, fired.Count);
            Assert.AreEqual(def.Events.Count, fired.Distinct().Count());
        }

        [Test]
        public void PickEnemyFollowsTheMix()
        {
            var phase = new SpawnPhase { Mix = { new MixEntry("a", 3f), new MixEntry("b", 1f) } };
            var rng = new Rng(2);
            int a = 0;
            for (int i = 0; i < 4000; i++) if (TimelineEvaluator.PickEnemy(phase, rng) == "a") a++;
            Assert.That(a / 4000f, Is.InRange(0.7f, 0.8f));
        }

        [Test]
        public void FinalSwarmClimbsAndPaysMore()
        {
            Assert.Greater(FinalSwarm.TargetAlive(60f, 1f), FinalSwarm.TargetAlive(0f, 1f));
            Assert.Greater(FinalSwarm.HealthMultiplier(60f, 2.8f), FinalSwarm.HealthMultiplier(0f, 2.8f));
            // On the Grey Steppe (coefficient 2.8 at ten minutes) the swarm matches the old 1.5 + s / 20.
            Assert.AreEqual(1.5f + 60f / 20f, FinalSwarm.HealthMultiplier(60f, Difficulty.Coefficient(10f, 0)), 0.1f);
            Assert.AreEqual(1f, FinalSwarm.SurvivalMultiplier(29f));
            Assert.AreEqual(1.25f, FinalSwarm.SurvivalMultiplier(30f));
            Assert.AreEqual(1.5f, FinalSwarm.SurvivalMultiplier(75f));
            var def = DefaultTimelines.Greybox();
            Assert.IsFalse(TimelineEvaluator.InFinalSwarm(def, 599f));
            Assert.IsTrue(TimelineEvaluator.InFinalSwarm(def, 600f));
            Assert.AreEqual(0f, TimelineEvaluator.Remaining(def, 700f));
        }
    }

    public class BossRulesTests
    {
        static BossDef Def() => new BossDef
        {
            Rest = 1f,
            Attacks =
            {
                new BossAttackDef { Attack = BossAttack.Slam, Weight = 1f, Cooldown = 5f },
                new BossAttackDef { Attack = BossAttack.Charge, Weight = 1f, Cooldown = 5f },
            },
        };

        [Test]
        public void AttacksRespectCooldownsAndRest()
        {
            var p = new BossPattern(Def());
            var rng = new Rng(1);
            int first = p.Next(0f, 1f, rng);
            Assert.GreaterOrEqual(first, 0);
            p.Finished(0.5f, 1f);
            Assert.AreEqual(-1, p.Next(1f, 1f, rng), "resting");
            int second = p.Next(1.6f, 1f, rng);
            Assert.AreEqual(1 - first, second, "the other attack, since the first is cooling down");
            p.Finished(2f, 1f);
            Assert.AreEqual(-1, p.Next(3.5f, 1f, rng), "both on cooldown");
            Assert.GreaterOrEqual(p.Next(5.1f, 1f, rng), 0);
        }

        [Test]
        public void EnrageShortensRest()
        {
            var def = Def();
            var p = new BossPattern(def);
            Assert.Less(p.RestTime(0.2f), p.RestTime(0.9f));
        }

        [Test]
        public void ScalingGrowsWithStageAndCurses()
        {
            var def = Def();
            Assert.Greater(BossScaling.Health(def, 1, 0), BossScaling.Health(def, 0, 0));
            Assert.Greater(BossScaling.Health(def, 0, 1), BossScaling.Health(def, 0, 0));
            Assert.Greater(BossScaling.Damage(10f, 2), 10f);
        }
    }
}
