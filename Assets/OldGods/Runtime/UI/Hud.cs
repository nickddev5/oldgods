using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>The in-run overlay: health, timer, kills, and the death message.</summary>
    public sealed class Hud : MonoBehaviour
    {
        RunController run;
        Image healthFill, xpFill;
        TextMeshProUGUI healthText, timerText, killsText, infoText, centerText, levelText, loadoutText;
        float fpsSmoothed;

        public static Hud Create(RunController run)
        {
            var canvas = UiKit.Canvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<Hud>();
            hud.run = run;
            hud.Build(canvas.transform as RectTransform);
            run.RunOver += hud.OnRunOver;
            return hud;
        }

        void Build(RectTransform root)
        {
            var xpBar = UiKit.Panel(root, "XP", new Color(0f, 0f, 0f, 0.5f));
            xpBar.anchorMin = new Vector2(0f, 1f);
            xpBar.anchorMax = new Vector2(1f, 1f);
            xpBar.pivot = new Vector2(0.5f, 1f);
            xpBar.offsetMin = new Vector2(0f, -10f);
            xpBar.offsetMax = Vector2.zero;
            xpFill = UiKit.Panel(xpBar, "Fill", new Color(0.35f, 0.7f, 1f)).GetComponent<Image>();
            UiKit.Stretch(xpFill.rectTransform, 0f);
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.sprite = UiKit.WhiteSprite;

            levelText = UiKit.Text(root, "Lv 1", 24, TextAlignmentOptions.TopLeft);
            UiKit.Anchor(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -64f), new Vector2(300f, 30f));

            loadoutText = UiKit.Text(root, "", 20, TextAlignmentOptions.BottomLeft);
            UiKit.Anchor(loadoutText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 50f), new Vector2(900f, 120f));

            var bar = UiKit.Panel(root, "Health", new Color(0f, 0f, 0f, 0.55f));
            UiKit.Anchor(bar, new Vector2(0f, 1f), new Vector2(24f, -26f), new Vector2(360f, 26f));
            healthFill = UiKit.Panel(bar, "Fill", new Color(0.75f, 0.18f, 0.16f)).GetComponent<Image>();
            UiKit.Stretch(healthFill.rectTransform, 3f);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.sprite = UiKit.WhiteSprite;
            healthText = UiKit.Text(bar, "100 / 100", 18, TextAlignmentOptions.Center);
            UiKit.Stretch(healthText.rectTransform, 0f);

            timerText = UiKit.Text(root, "0:00", 40, TextAlignmentOptions.Top);
            UiKit.Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(300f, 50f));

            killsText = UiKit.Text(root, "", 22, TextAlignmentOptions.TopRight);
            UiKit.Anchor(killsText.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(300f, 30f));

            infoText = UiKit.Text(root, "", 16, TextAlignmentOptions.BottomLeft);
            infoText.color = new Color(1f, 1f, 1f, 0.6f);
            UiKit.Anchor(infoText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(700f, 24f));

            centerText = UiKit.Text(root, "", 44, TextAlignmentOptions.Center);
            UiKit.Anchor(centerText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 200f));
        }

        void Update()
        {
            if (run == null) return;
            var h = run.PlayerHealth != null ? run.PlayerHealth.Health : null;
            if (h != null)
            {
                healthFill.fillAmount = h.Max > 0f ? h.Current / h.Max : 0f;
                healthText.text = $"{Mathf.CeilToInt(h.Current)} / {Mathf.CeilToInt(h.Max)}";
            }
            int secs = Mathf.FloorToInt(run.Elapsed);
            timerText.text = $"{secs / 60}:{secs % 60:00}";
            var combat = PlayerCombat.Instance;
            if (combat != null)
            {
                xpFill.fillAmount = combat.Xp.Fraction;
                levelText.text = $"Lv {combat.Xp.Level}";
                var sb = new System.Text.StringBuilder();
                foreach (var w in combat.Loadout.Weapons) sb.Append($"<color=#ffd27a>{w.Def.Name} {w.Level}</color>   ");
                sb.AppendLine();
                foreach (var p in combat.Loadout.Passives) sb.Append($"<color=#a8d8ff>{p.Def.Name} {p.Level}</color>   ");
                loadoutText.text = sb.ToString();
            }
            killsText.text = $"Kills {run.Kills}";

            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fpsSmoothed = Mathf.Lerp(fpsSmoothed, 1f / dt, 0.05f);
            int alive = run.Horde != null ? run.Horde.AliveCount : 0;
            infoText.text = $"seed {run.Seed}   enemies {alive}   {fpsSmoothed:0} fps";
        }

        void OnRunOver()
        {
            centerText.text = "You have fallen\n<size=24>Press Jump to rise again</size>";
        }
    }
}
