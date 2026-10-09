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

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            float start = Time.realtimeSinceStartup;
            bool hold = CommandLine.Has("-shotHold");
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
