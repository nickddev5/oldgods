using System.Collections;
using System.IO;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by the -smoke switch. Plays a scripted session: start a run, check the
    /// horde spawns, die, restart, check the new run, quit. Exit code 0 means every step
    /// passed; 1 means a step failed or timed out. Writes a log to -smokeOut if given.
    /// </summary>
    public sealed class SmokeRunner : MonoBehaviour
    {
        const float Timeout = 90f;
        readonly System.Text.StringBuilder log = new System.Text.StringBuilder();
        float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!CommandLine.Has("-smoke")) return;
            if (FindAnyObjectByType<SmokeRunner>() != null) return;
            var go = new GameObject("Smoke Runner");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeRunner>();
        }

        void Start()
        {
            started = Time.realtimeSinceStartup;
            LevelUpScreen.AutoPick = true;
            SaveStore.FolderOverride = Path.Combine(Application.temporaryCachePath, "smoke-save");
            StartCoroutine(Run());
        }

        void Step(string message)
        {
            string line = $"SMOKE {Time.realtimeSinceStartup - started:0.0}s: {message}";
            Debug.Log(line);
            log.AppendLine(line);
        }

        IEnumerator WaitFor(System.Func<bool> condition, string what, float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > until) { Fail($"timed out waiting for {what}"); yield break; }
                yield return null;
            }
            Step($"ok: {what}");
        }

        IEnumerator Run()
        {
            yield return SmokeFlow.Begin(this);
            if (failed) yield break;

            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Player != null, "run started", 30f);
            if (failed) yield break;
            var first = RunController.Instance;
            yield return WaitFor(() => first.Horde != null && first.Horde.AliveCount > 0, "horde spawned", 20f);
            if (failed) yield break;

            first.PlayerHealth.Kill();
            yield return WaitFor(() => first.IsOver, "player died", 5f);
            if (failed) yield break;

            first.Restart();
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance != first && RunController.Instance.Elapsed > 1f, "run restarted", 30f);
            if (failed) yield break;

            yield return SmokeFlow.AfterRestart(this);
            if (failed) yield break;

            Finish(0);
        }

        bool failed;

        public void Fail(string why)
        {
            if (failed) return;
            failed = true;
            Step("FAILED: " + why);
            Finish(1);
        }

        public void Note(string message) => Step(message);
        public bool Failed => failed;

        void Update()
        {
            if (!failed && Time.realtimeSinceStartup - started > Timeout) Fail("overall timeout");
        }

        void Finish(int code)
        {
            Step(code == 0 ? "PASSED" : "exit 1");
            string outPath = CommandLine.Value("-smokeOut");
            if (!string.IsNullOrEmpty(outPath))
            {
                try { File.WriteAllText(outPath, log.ToString()); } catch (System.Exception e) { Debug.LogWarning(e.Message); }
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }
    }

    /// <summary>Hooks for later milestones to extend the smoke flow (menus, character select).</summary>
    public static class SmokeFlow
    {
        public static System.Func<SmokeRunner, IEnumerator> BeginHook;
        public static System.Func<SmokeRunner, IEnumerator> AfterRestartHook;

        public static IEnumerator Begin(SmokeRunner r)
        {
            if (BeginHook != null) yield return BeginHook(r);
        }

        public static IEnumerator AfterRestart(SmokeRunner r)
        {
            if (AfterRestartHook != null) yield return AfterRestartHook(r);
        }
    }
}
