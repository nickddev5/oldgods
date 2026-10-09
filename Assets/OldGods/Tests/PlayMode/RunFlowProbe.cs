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
    }
}
