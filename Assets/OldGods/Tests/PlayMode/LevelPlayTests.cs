using System.Collections;
using System.IO;
using NUnit.Framework;
using OldGods.Rules;
using OldGods.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OldGods.Tests.PlayMode
{
    /// <summary>The horde on real stage maps: up ramps, round walls, never inside the dressing.</summary>
    public class LevelPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveStore.FolderOverride = Path.Combine(Application.temporaryCachePath, "playmode-save");
            SaveStore.Forget();
            RunSetup.GodId = null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameInput.MoveOverride = null;
            SaveStore.FolderOverride = null;
        }

        static IEnumerator StartRun()
        {
            SceneManager.LoadScene("Run");
            float until = Time.realtimeSinceStartup + 20f;
            while (RunController.Instance == null || RunController.Instance.Player == null || Ground.Layout == null)
            {
                if (Time.realtimeSinceStartup > until) Assert.Fail("run did not start");
                yield return null;
            }
            var run = RunController.Instance;
            run.PlayerHealth.Invincible = true;
            run.Director.Paused = true;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator HordeClimbsTheRampsToAPlayerOnAHillFort()
        {
            yield return StartRun();
            var run = RunController.Instance;
            var layout = Ground.Layout;
            Landmark fort = default;
            bool found = false;
            foreach (var l in layout.Landmarks)
                if (l.Kind == LandmarkKind.HillFort) { fort = l; found = true; break; }
            if (!found) Assert.Inconclusive("this seed has no hill fort");

            run.Player.Teleport(new Vector3(fort.X, fort.Top + 0.3f, fort.Z));
            run.Horde.Clear();
            const int count = 24;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var p = new Vector3(fort.X + Mathf.Cos(a) * (fort.Radius + 6f), 0f, fort.Z + Mathf.Sin(a) * (fort.Radius + 6f));
                run.Horde.Spawn(0, p, healthMultiplier: 10000f);
            }

            Time.timeScale = 4f;
            float until = Time.realtimeSinceStartup + 20f;
            int onTop = 0;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                onTop = 0;
                for (int i = 0; i < run.Horde.HighWater; i++)
                {
                    if (!run.Horde.Alive[i]) continue;
                    Assert.IsFalse(Ground.Obstacles.Overlaps(run.Horde.X[i], run.Horde.Z[i], 0f), "an enemy is inside the dressing");
                    float dx = run.Horde.X[i] - fort.X, dz = run.Horde.Z[i] - fort.Z;
                    if (Mathf.Abs(run.Horde.Y[i] - fort.Top) < 1f && dx * dx + dz * dz < 14f * 14f) onTop++;
                }
                if (onTop >= count / 2) break;
            }
            Time.timeScale = 1f;
            Assert.GreaterOrEqual(onTop, count / 2, $"only {onTop} of {count} enemies found the way up");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EveryStageBuildsItsLandmarks()
        {
            yield return StartRun();
            var run = RunController.Instance;
            for (int stage = 0; stage < run.StageCount; stage++)
            {
                run.BuildStage(stage);
                yield return null;
                Assert.Greater(Ground.Layout.Landmarks.Count, 5, $"stage {stage + 1} has landmarks");
                Assert.Greater(Ground.Layout.Pieces.Count, 20, $"stage {stage + 1} has ruins");
                Assert.IsNotNull(GameObject.Find("Landmark " + Ground.Layout.Landmarks[0].Kind), $"stage {stage + 1} built its first landmark");
                Assert.IsNotNull(Object.FindAnyObjectByType<BossGate>(), $"stage {stage + 1} has a boss gate");
            }
        }
    }
}
