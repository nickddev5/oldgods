using System.Collections;
using System.IO;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -shots DIR. Steers the player in a slow circle and saves screenshots
    /// at fixed times, then quits. Lets an agent see a build without a person at the screen.
    /// -shotTimes "2,6,12" overrides the times; -shotHold keeps the player still.
    /// </summary>
    public sealed class ScreenshotRunner : MonoBehaviour
    {
        string dir;
        float[] times = { 3f, 8f, 15f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string d = CommandLine.Value("-shots");
            if (string.IsNullOrEmpty(d) || FindAnyObjectByType<ScreenshotRunner>() != null) return;
            if (CommandLine.Has("-elias")) RunSetup.GodId = OldGods.Rules.LastTest.EliasId;
            var go = new GameObject("Screenshot Runner");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<ScreenshotRunner>();
            r.dir = d;
            string t = CommandLine.Value("-shotTimes");
            if (!string.IsNullOrEmpty(t))
            {
                var parts = t.Split(',');
                r.times = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out r.times[i]);
            }
        }

        /// <summary>-shotBoss: wakes the stage boss in front of an invincible player.</summary>
        IEnumerator WakeBoss()
        {
            while (RunController.Instance == null || RunController.Instance.Player == null) yield return null;
            yield return new WaitForSeconds(2f);
            var run = RunController.Instance;
            run.PlayerHealth.Invincible = true;
            run.StartBoss(run.Player.transform.position + run.Player.Facing * 14f);
        }

        /// <summary>-shotStage 1|2|final|ending: jumps to a stage, the arena, or straight to the throne ending.</summary>
        IEnumerator JumpTo(string stage)
        {
            while (RunController.Instance == null || RunController.Instance.Player == null) yield return null;
            yield return new WaitForSeconds(0.5f);
            var run = RunController.Instance;
            run.PlayerHealth.Invincible = true;
            if (stage == "final" || stage == "ending")
            {
                run.BuildStage(run.StageCount, final: true);
                if (stage == "ending")
                {
                    while (BossController.Active == null) yield return null;
                    yield return new WaitForSeconds(1.5f);
                    run.Horde.Damage(BossController.Active.Slot, BossController.Active.MaxHealth * 2f);
                }
            }
            else if (int.TryParse(stage, out int n)) run.BuildStage(n);
        }

        IEnumerator OpenSelect()
        {
            while (MenuController.Instance == null) yield return null;
            yield return new WaitForSeconds(2f);
            MenuController.Instance.ShowSelect();
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            if (CommandLine.Has("-autopick")) LevelUpScreen.AutoPick = true;
            float start = Time.realtimeSinceStartup;
            bool hold = CommandLine.Has("-shotHold");
            if (CommandLine.Has("-shotBoss")) StartCoroutine(WakeBoss());
            if (CommandLine.Has("-shotMenu")) StartCoroutine(OpenSelect());
            string stage = CommandLine.Value("-shotStage");
            if (!string.IsNullOrEmpty(stage)) StartCoroutine(JumpTo(stage));
            for (int i = 0; i < times.Length; i++)
            {
                while (Time.realtimeSinceStartup - start < times[i])
                {
                    if (!hold)
                    {
                        float a = (Time.realtimeSinceStartup - start) * 0.35f;
                        GameInput.MoveOverride = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    }
                    yield return null;
                }
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(dir, $"shot_{i:00}_{times[i]:0}s.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("OldGods: screenshot " + path);
                yield return null;
                yield return null;
            }
            GameInput.MoveOverride = null;
            if (!CommandLine.Has("-smoke") && !CommandLine.Has("-probe"))
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Application.Quit(0);
            }
        }
    }
}
