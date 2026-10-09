using System.Collections;
using System.IO;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -shots DIR. Steers the player in a slow circle and saves screenshots
    /// at fixed times, then quits. Lets an agent see a build without a person at the screen.
    /// -shotTimes "2,6,12" overrides the times; -shotHold keeps the player still.
    /// -shotTour frames the map from above for the first shot, then one landmark per shot.
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
            // Scripted runs never touch the real save.
            SaveStore.FolderOverride = System.IO.Path.Combine(Application.temporaryCachePath, "shots-save");
            if (CommandLine.Has("-elias")) RunSetup.GodId = OldGods.Rules.LastTest.EliasId;
            // Keep running when the window loses focus, so scripted checks do not stall.
            Application.runInBackground = true;
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

        Camera tourCam;
        bool? fogWas;

        /// <summary>Shot 0 looks down on the whole map; later shots each frame one landmark.</summary>
        void Tour(int shot)
        {
            var layout = Ground.Layout;
            if (layout == null || Ground.Field == null) return;
            if (tourCam == null)
            {
                var go = new GameObject("Tour Camera");
                tourCam = go.AddComponent<Camera>();
                tourCam.depth = 50f;
                tourCam.fieldOfView = 55f;
                tourCam.farClipPlane = 900f;
                go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
                fogWas = RenderSettings.fog;
            }
            var t = tourCam.transform;
            if (shot == 0)
            {
                RenderSettings.fog = false;
                float size = Ground.Field.Size;
                t.position = new Vector3(0f, size * 0.95f, -size * 0.62f);
                t.LookAt(new Vector3(0f, 0f, size * 0.04f));
                return;
            }
            RenderSettings.fog = fogWas ?? true;
            if (layout.Landmarks.Count == 0) return;
            var l = layout.Landmarks[(shot - 1) % layout.Landmarks.Count];
            var toward = new Vector3(-l.X, 0f, -l.Z);
            toward = toward.sqrMagnitude > 1f ? toward.normalized : Vector3.back;
            var focus = new Vector3(l.X, Ground.Height(l.X, l.Z) + 2f, l.Z);
            t.position = focus + toward * (l.Radius + 12f) + Vector3.up * (8f + l.Radius * 0.35f);
            t.LookAt(focus);
            Debug.Log($"OldGods: tour shot {shot} {l}");
        }

        IEnumerator OpenSelect()
        {
            while (MenuController.Instance == null) yield return null;
            yield return new WaitForSeconds(2f);
            string page = CommandLine.Value("-shotPage");
            if (string.IsNullOrEmpty(page)) MenuController.Instance.ShowSelect();
            else MenuController.Instance.ShowPage(page);
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
                if (CommandLine.Has("-shotTour")) Tour(i);
                yield return null;
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
