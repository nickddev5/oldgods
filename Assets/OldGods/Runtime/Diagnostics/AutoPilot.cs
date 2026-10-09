using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -autoplay. A simple bot plays stage 1 for a balance check: it kites away from
    /// nearby enemies, drifts toward gems, takes the first draft card, and goes to the boss gate
    /// after -bossAt seconds (default 480). Logs a line every 30 seconds and writes a JSON summary
    /// to -autoplayOut. Runs at -speed times real time (default 3). Never touches the real save.
    /// </summary>
    public sealed class AutoPilot : MonoBehaviour
    {
        float speed = 3f, bossAt = 480f, limit = 900f;
        string outPath;
        readonly StringBuilder log = new StringBuilder();
        readonly int[] near = new int[64];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!CommandLine.Has("-autoplay") || FindAnyObjectByType<AutoPilot>() != null) return;
            SaveStore.FolderOverride = Path.Combine(Application.temporaryCachePath, "autoplay-save");
            // Keep running when the window loses focus, so scripted checks do not stall.
            Application.runInBackground = true;
            var go = new GameObject("AutoPilot");
            DontDestroyOnLoad(go);
            var a = go.AddComponent<AutoPilot>();
            if (float.TryParse(CommandLine.Value("-speed"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s)) a.speed = s;
            if (float.TryParse(CommandLine.Value("-bossAt"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b)) a.bossAt = b;
            a.outPath = CommandLine.Value("-autoplayOut");
            LevelUpScreen.AutoPick = true;
        }

        IEnumerator Start()
        {
            if (MenuController.Instance != null) MenuController.StartRun(RunSetup.GodId);
            while (RunController.Instance == null || RunController.Instance.Combat == null) yield return null;
            var run = RunController.Instance;
            DamageNumbers.Enabled = false;
            float nextLog = 30f;
            bool goingToGate = false;
            int stageAtStart = run.StageIndex;
            while (!run.IsOver && run.Elapsed < limit)
            {
                if (Time.timeScale > 0f && Time.timeScale != speed) Time.timeScale = speed;
                var p = run.Player.transform.position;
                Vector3 dir = Vector3.zero;

                // Run from enemies, harder the closer they are.
                int n = run.Horde.QueryCircle(p, 10f, near);
                for (int k = 0; k < n; k++)
                {
                    Vector3 away = p - run.Horde.Position(near[k]);
                    away.y = 0f;
                    float d = Mathf.Max(0.5f, away.magnitude);
                    dir += away / (d * d);
                }
                dir = dir.sqrMagnitude > 0f ? dir.normalized * 1.5f : Vector3.zero;

                // Keep well clear of a boss and out of its telegraphs' reach.
                var boss = BossController.Active;
                if (boss != null)
                {
                    Vector3 away = p - boss.transform.position;
                    away.y = 0f;
                    float d = away.magnitude;
                    if (d < 14f) dir += away.normalized * (14f - d) * 0.5f;
                }

                // With nothing close, go and collect gems.
                bool threatened = run.Horde.QueryCircle(p, 4f, near) > 0;
                if (!threatened && run.Pickups.Nearest(p, 18f, out var gem))
                {
                    Vector3 to = gem - p;
                    to.y = 0f;
                    dir += to.normalized * 1.2f;
                }

                // Keep moving in a wide circle so the horde trails behind, and stay off the rim.
                Vector3 tangent = Vector3.Cross(Vector3.up, new Vector3(p.x, 0f, p.z)).normalized;
                if (tangent.sqrMagnitude < 0.1f) tangent = Vector3.forward;
                dir += tangent * 0.8f;
                float r = new Vector2(p.x, p.z).magnitude;
                if (r > 70f) dir += -new Vector3(p.x, 0f, p.z).normalized * ((r - 70f) * 0.2f);
                if (r < 25f) dir += new Vector3(p.x, 0f, p.z).normalized * 0.5f;

                // After bossAt, head for the gate, wake the boss, and take the portal when it opens.
                if (run.Elapsed > bossAt && !run.BossDefeated)
                {
                    var gate = FindAnyObjectByType<BossGate>();
                    if (gate != null && gate.CanUse)
                    {
                        goingToGate = true;
                        Vector3 to = gate.transform.position - p;
                        to.y = 0f;
                        dir = to.normalized * 2f + dir * 0.4f;
                        if (to.magnitude < gate.Range) gate.Use(run.Combat);
                    }
                }
                if (run.BossDefeated)
                {
                    var portal = FindAnyObjectByType<NextPortal>();
                    if (portal != null)
                    {
                        Vector3 to = portal.transform.position - p;
                        to.y = 0f;
                        dir = to.normalized * 2f + dir * 0.3f;
                        if (to.magnitude < portal.Range) { Line(run, "took the portal"); portal.Use(run.Combat); break; }
                    }
                }

                // Convert the world direction into camera-relative input.
                var cam = run.Player.ViewYaw;
                Vector3 f = cam != null ? cam.forward : Vector3.forward;
                f.y = 0f;
                f.Normalize();
                Vector3 right = new Vector3(f.z, 0f, -f.x);
                Vector3 want = Vector3.ClampMagnitude(dir, 1f);
                GameInput.MoveOverride = new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, f));

                if (run.Elapsed >= nextLog)
                {
                    nextLog += 30f;
                    Line(run, goingToGate ? "heading to the gate" : "");
                }
                yield return null;
            }
            GameInput.MoveOverride = null;
            Time.timeScale = 1f;
            Line(run, run.IsOver ? "died" : run.StageIndex > stageAtStart ? "cleared stage 1" : "time limit");
            var s = run.Summary;
            string json = System.FormattableString.Invariant(
                $"{{ \"god\": \"{run.GodId}\", \"seed\": \"{run.Seed}\", \"seconds\": {run.Elapsed:0}, \"died\": {(run.IsOver ? "true" : "false")}, \"stageCleared\": {(run.StageIndex > stageAtStart ? "true" : "false")}, \"level\": {run.Combat.Xp.Level}, \"kills\": {run.Kills}, \"bosses\": {s.BossesKilled}, \"log\": [\n{log}  ] }}\n");
            Debug.Log("OldGods autoplay:\n" + json);
            if (!string.IsNullOrEmpty(outPath))
            {
                try { File.WriteAllText(outPath, json); } catch (System.Exception e) { Debug.LogWarning(e.Message); }
            }
            Application.Quit(0);
        }

        void Line(RunController run, string note)
        {
            var h = run.PlayerHealth.Health;
            var weapons = new StringBuilder();
            foreach (var w in run.Combat.Loadout.Weapons) weapons.Append(w.Def.Name).Append(' ').Append(w.Level).Append(", ");
            if (log.Length > 0) log.Append(",\n");
            log.Append(System.FormattableString.Invariant(
                $"    \"{run.Elapsed:0}s hp {h.Current:0}/{h.Max:0} lv {run.Combat.Xp.Level} kills {run.Kills} alive {run.Horde.AliveCount} gold {run.Economy.Wallet.Gold} | {weapons}{note}\""));
        }
    }
}
