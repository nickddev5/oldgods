using System.Collections;
using System.IO;
using NUnit.Framework;
using OldGods.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OldGods.Tests.PlayMode
{
    /// <summary>Loads the real run scene and plays it the way the smoke test does.</summary>
    public class RunFlowProbe
    {
        [SetUp]
        public void SetUp()
        {
            SaveStore.FolderOverride = Path.Combine(Application.temporaryCachePath, "playmode-save");
            DamageNumbers.Enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameInput.MoveOverride = null;
            SaveStore.FolderOverride = null;
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > until) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RunStartsSpawnsDiesAndRestarts()
        {
            SceneManager.LoadScene("Run");
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Player != null, 20f, "run start");
            var first = RunController.Instance;
            Assert.IsNotNull(Ground.Field, "ground was generated");

            yield return WaitFor(() => first.Horde.AliveCount > 0, 10f, "horde spawn");

            // The player moves when input says so.
            Vector3 start = first.Player.transform.position;
            GameInput.MoveOverride = Vector2.up;
            yield return new WaitForSeconds(0.6f);
            GameInput.MoveOverride = null;
            Assert.Greater(Vector3.Distance(start, first.Player.transform.position), 1f, "player moved");

            first.PlayerHealth.Kill();
            yield return null;
            Assert.IsTrue(first.IsOver);

            first.Restart();
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance != first && RunController.Instance.Player != null, 20f, "restart");
        }

        [UnityTest]
        public IEnumerator WeaponsKillDropXpAndTheDraftLevelsUp()
        {
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
                var run = RunController.Instance;
                run.PlayerHealth.Invincible = true;
                Assert.AreEqual(1, run.Combat.Loadout.Weapons.Count, "starts with one weapon");

                yield return WaitFor(() => run.Kills > 0, 30f, "first kill");
                yield return WaitFor(() => run.Combat.Xp.Level >= 3, 60f, "two level-ups");
                yield return null;
                int progress = 0;
                foreach (var w in run.Combat.Loadout.Weapons) progress += w.Level;
                foreach (var p in run.Combat.Loadout.Passives) progress += p.Level;
                Assert.GreaterOrEqual(progress, 3, "each draft pick added or levelled something");
                Assert.AreEqual(0, run.Combat.PendingLevelUps);
                Assert.AreEqual(1f, Time.timeScale, "draft closed and the run resumed");
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
            }
        }
    }
}
