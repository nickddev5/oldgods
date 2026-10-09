using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>End of run: what was reached, the seed, and the Embers earned.</summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        /// <summary>Set by the menu milestone: what the second button does. Null hides it.</summary>
        public static System.Action ToMenu;

        public static ResultsScreen Show(RunController run)
        {
            var canvas = UiKit.Canvas("Results", 60);
            var screen = canvas.gameObject.AddComponent<ResultsScreen>();
            screen.Build(canvas.transform as RectTransform, run);
            return screen;
        }

        static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        void Build(RectTransform root, RunController run)
        {
            var dim = UiKit.Panel(root, "Dim", new Color(0f, 0f, 0f, 0.7f));
            UiKit.Stretch(dim, 0f);
            var s = run.Summary;
            string title = s.Won
                ? (OldGods.Rules.LastTest.TakesTheThrone(s.GodId) ? "The throne is taken" : "The Last Test is broken")
                : "You have fallen";
            var head = UiKit.Text(dim, title, 60, TextAlignmentOptions.Center);
            UiKit.Anchor(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1400f, 80f));

            string reached = s.Won ? "The throne" : s.ReachedThrone ? "The Last Test" : $"Stage {run.StageIndex + 1}: {run.Biome.DisplayName}";
            var body = UiKit.Text(dim,
                $"Reached  <b>{reached}</b>\n" +
                $"Time  <b>{Clock(s.Seconds)}</b>     Level  <b>{s.Level}</b>     Kills  <b>{s.Kills}</b>\n" +
                $"Bosses  <b>{s.BossesKilled}</b>     Chests  <b>{s.ChestsOpened}</b>     Shrines  <b>{s.ShrinesUsed}</b>\n" +
                (s.BestSwarmSeconds > 0f ? $"Longest final swarm  <b>{Clock(s.BestSwarmSeconds)}</b>  (x{OldGods.Rules.FinalSwarm.SurvivalMultiplier(s.BestSwarmSeconds):0.00})\n" : "") +
                $"\n<size=44><color=#ffb860>+{run.EmbersEarned} Embers</color></size>\n\n" +
                $"<size=20><color=#999999>seed {s.Seed}</color></size>",
                30, TextAlignmentOptions.Center);
            UiKit.Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1400f, 440f));

            var row = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(dim, false);
            UiKit.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(ToMenu != null ? 720f : 340f, 70f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var again = UiKit.Button(row, "Run again", 30, run.Restart);
            if (ToMenu != null) UiKit.Button(row, "To the menu", 30, () => ToMenu());
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(again.gameObject);
        }
    }
}
