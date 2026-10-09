using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Horde performance probe. Started by the -probe switch in a player build, or by
    /// the PlayMode probe test. Spawns 250, 500 and 1000 enemies around a player who
    /// runs in a circle, and records frame times and horde CPU time for each count.
    /// </summary>
    public sealed class HordeProbe : MonoBehaviour
    {
        public static readonly int[] Counts = { 250, 500, 1000 };
        public int WarmupFrames = 90;
        public int MeasureFrames = 600;
        public bool QuitWhenDone = true;
        public string OutputPath;

        public bool Done { get; private set; }
        public string ResultJson { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!CommandLine.Has("-probe")) return;
            if (FindAnyObjectByType<HordeProbe>() != null) return;
            SaveStore.FolderOverride = System.IO.Path.Combine(Application.temporaryCachePath, "probe-save");
            var go = new GameObject("Horde Probe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<HordeProbe>();
            probe.OutputPath = CommandLine.Value("-probeOut");
        }

        void Start() => StartCoroutine(Run());

        IEnumerator Run()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            DamageNumbers.Enabled = false;
            LevelUpScreen.AutoPick = true;

            while (RunController.Instance == null || RunController.Instance.Horde == null || RunController.Instance.Horde.Types.Count == 0)
                yield return null;
            var run = RunController.Instance;
            var horde = run.Horde;
            run.PlayerHealth.Invincible = true;
            if (run.Director != null) run.Director.Paused = true;

            var results = new List<string>();
            float angle = 0f;
            foreach (int count in Counts)
            {
                horde.Clear();
                for (int i = 0; i < count; i++)
                {
                    float a = i * 2.399963f;
                    float r = 6f + 30f * Mathf.Sqrt((i + 0.5f) / count);
                    var p = run.Player.transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    horde.Spawn(i % horde.Types.Count, p);
                }

                var frames = new List<float>(MeasureFrames);
                var sim = new List<float>(MeasureFrames);
                for (int f = 0; f < WarmupFrames + MeasureFrames; f++)
                {
                    angle += Time.deltaTime * 0.6f;
                    GameInput.MoveOverride = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    // Top up so the count stays constant if anything dies.
                    while (horde.AliveCount < count && horde.Spawn(0, run.Player.transform.position + new Vector3(30f, 0f, 0f)) >= 0) { }
                    yield return null;
                    if (f >= WarmupFrames)
                    {
                        frames.Add(Time.unscaledDeltaTime * 1000f);
                        sim.Add(horde.LastSimulateMs);
                    }
                }
                results.Add(Summarise(count, frames, sim));
            }
            GameInput.MoveOverride = null;

            var sb = new StringBuilder();
            sb.Append("{\n  \"unity\": \"").Append(Application.unityVersion).Append("\",\n");
            sb.Append("  \"device\": \"").Append(Escape(SystemInfo.processorType)).Append(" / ").Append(Escape(SystemInfo.graphicsDeviceName)).Append("\",\n");
            sb.Append("  \"editor\": ").Append(Application.isEditor ? "true" : "false").Append(",\n");
            sb.Append("  \"batchMode\": ").Append(Application.isBatchMode ? "true" : "false").Append(",\n");
            sb.Append("  \"screen\": \"").Append(Screen.width).Append('x').Append(Screen.height).Append("\",\n");
            sb.Append("  \"results\": [\n    ").Append(string.Join(",\n    ", results)).Append("\n  ]\n}\n");
            ResultJson = sb.ToString();
            Debug.Log("OldGods horde probe:\n" + ResultJson);

            string path = string.IsNullOrEmpty(OutputPath) ? Path.Combine(Application.persistentDataPath, "oldgods", "horde-probe.json") : OutputPath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, ResultJson);
            }
            catch (System.Exception e) { Debug.LogWarning("OldGods probe: could not write " + path + ": " + e.Message); }

            Done = true;
            if (QuitWhenDone)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(0);
#endif
            }
        }

        static string Summarise(int count, List<float> frames, List<float> sim)
        {
            frames.Sort();
            float avg = 0f;
            foreach (var f in frames) avg += f;
            avg /= Mathf.Max(1, frames.Count);
            float p99 = frames.Count > 0 ? frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * 0.99f))] : 0f;
            float simAvg = 0f;
            foreach (var s in sim) simAvg += s;
            simAvg /= Mathf.Max(1, sim.Count);
            return System.FormattableString.Invariant($"{{ \"count\": {count}, \"avgMs\": {avg:0.00}, \"onePercentLowMs\": {p99:0.00}, \"avgFps\": {1000f / Mathf.Max(0.001f, avg):0.0}, \"hordeCpuMs\": {simAvg:0.000} }}");
        }

        static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
