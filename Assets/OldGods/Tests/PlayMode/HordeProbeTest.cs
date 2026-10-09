using System.Collections;
using System.IO;
using NUnit.Framework;
using OldGods.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OldGods.Tests.PlayMode
{
    /// <summary>
    /// Runs the horde probe inside the Editor and writes TestResults/horde-probe-editor.json.
    /// The player-build numbers (Tools/smoke.py --probe) are the ones that count; this one
    /// catches regressions in CI-like runs.
    /// </summary>
    public class HordeProbeTest
    {
        [UnityTest, Timeout(300000)]
        public IEnumerator ProbeCompletesAndStaysWithinBudget()
        {
            SceneManager.LoadScene("Run");
            float until = Time.realtimeSinceStartup + 20f;
            while (RunController.Instance == null || RunController.Instance.Horde == null)
            {
                if (Time.realtimeSinceStartup > until) Assert.Fail("run did not start");
                yield return null;
            }

            var go = new GameObject("Probe");
            var probe = go.AddComponent<HordeProbe>();
            probe.QuitWhenDone = false;
            probe.WarmupFrames = 30;
            probe.MeasureFrames = 120;
            probe.OutputPath = Path.GetFullPath("TestResults/horde-probe-editor.json");
            while (!probe.Done) yield return null;

            StringAssert.Contains("\"count\": 1000", probe.ResultJson);
            // The horde loop for 1000 enemies must stay well inside a 60 fps frame.
            Assert.Less(RunController.Instance.Horde.LastSimulateMs, 8f);
            Object.Destroy(go);
            GameInput.MoveOverride = null;
        }
    }
}
