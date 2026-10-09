using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>
    /// The in-run overlay: health, XP, stage clock, kills, loadout, boss bar,
    /// announcements, the interaction prompt and the end-of-run message.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        RunController run;
        Image healthFill, xpFill, bossFill, promptFill;
        RectTransform bossBar, prompt;
        TextMeshProUGUI healthText, timerText, killsText, infoText, centerText, levelText, loadoutText, bossName, promptText, headline, subline, goldText;
        float fpsSmoothed, announceUntil;

        public RectTransform Root { get; private set; }

        /// <summary>Extra HUD line set by the economy (gold) and later systems.</summary>
        public static System.Func<string> ExtraLine;

        public static Hud Create(RunController run)
        {
            var canvas = UiKit.Canvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<Hud>();
            hud.run = run;
            hud.Root = canvas.transform as RectTransform;
            hud.Build(hud.Root);
            run.RunOver += hud.OnRunOver;
            run.Announced += hud.Show;
            return hud;
        }

        static Image Bar(RectTransform parent, string name, Color fill, out RectTransform bar)
        {
            bar = UiKit.Panel(parent, name, new Color(0f, 0f, 0f, 0.55f));
            var f = UiKit.Panel(bar, "Fill", fill).GetComponent<Image>();
            UiKit.Stretch(f.rectTransform, 3f);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.sprite = UiKit.WhiteSprite;
            return f;
        }

        void Build(RectTransform root)
        {
            xpFill = Bar(root, "XP", new Color(0.35f, 0.7f, 1f), out var xpBar);
            xpBar.anchorMin = new Vector2(0f, 1f);
            xpBar.anchorMax = new Vector2(1f, 1f);
            xpBar.pivot = new Vector2(0.5f, 1f);
            xpBar.offsetMin = new Vector2(0f, -10f);
            xpBar.offsetMax = Vector2.zero;
            UiKit.Stretch(xpFill.rectTransform, 0f);

            healthFill = Bar(root, "Health", new Color(0.75f, 0.18f, 0.16f), out var hb);
            UiKit.Anchor(hb, new Vector2(0f, 1f), new Vector2(24f, -26f), new Vector2(360f, 26f));
            healthText = UiKit.Text(hb, "100 / 100", 18, TextAlignmentOptions.Center);
            UiKit.Stretch(healthText.rectTransform, 0f);

            levelText = UiKit.Text(root, "Lv 1", 24, TextAlignmentOptions.TopLeft);
            UiKit.Anchor(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -60f), new Vector2(300f, 30f));
            goldText = UiKit.Text(root, "", 22, TextAlignmentOptions.TopLeft);
            goldText.color = new Color(1f, 0.85f, 0.4f);
            UiKit.Anchor(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -90f), new Vector2(400f, 30f));

            timerText = UiKit.Text(root, "10:00", 40, TextAlignmentOptions.Top);
            UiKit.Anchor(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(500f, 50f));

            killsText = UiKit.Text(root, "", 22, TextAlignmentOptions.TopRight);
            UiKit.Anchor(killsText.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(300f, 30f));

            loadoutText = UiKit.Text(root, "", 20, TextAlignmentOptions.BottomLeft);
            UiKit.Anchor(loadoutText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 50f), new Vector2(900f, 120f));

            infoText = UiKit.Text(root, "", 16, TextAlignmentOptions.BottomLeft);
            infoText.color = new Color(1f, 1f, 1f, 0.6f);
            UiKit.Anchor(infoText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(900f, 24f));

            bossFill = Bar(root, "Boss", new Color(0.8f, 0.3f, 0.15f), out bossBar);
            UiKit.Anchor(bossBar, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 22f));
            bossName = UiKit.Text(bossBar, "", 24, TextAlignmentOptions.Bottom);
            UiKit.Anchor(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 34f), new Vector2(900f, 34f));
            bossBar.gameObject.SetActive(false);

            headline = UiKit.Text(root, "", 46, TextAlignmentOptions.Center);
            UiKit.Anchor(headline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1400f, 60f));
            subline = UiKit.Text(root, "", 26, TextAlignmentOptions.Center);
            subline.color = new Color(1f, 1f, 1f, 0.8f);
            UiKit.Anchor(subline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1400f, 40f));

            prompt = UiKit.Panel(root, "Prompt", new Color(0f, 0f, 0f, 0.6f));
            UiKit.Anchor(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(460f, 48f));
            promptFill = UiKit.Panel(prompt, "Hold", new Color(1f, 0.85f, 0.5f, 0.35f)).GetComponent<Image>();
            UiKit.Stretch(promptFill.rectTransform, 0f);
            promptFill.type = Image.Type.Filled;
            promptFill.fillMethod = Image.FillMethod.Horizontal;
            promptFill.sprite = UiKit.WhiteSprite;
            promptText = UiKit.Text(prompt, "", 24, TextAlignmentOptions.Center);
            UiKit.Stretch(promptText.rectTransform, 4f);
            prompt.gameObject.SetActive(false);

            centerText = UiKit.Text(root, "", 44, TextAlignmentOptions.Center);
            UiKit.Anchor(centerText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f));
        }

        public void Show(string head, string sub)
        {
            headline.text = head ?? "";
            subline.text = sub ?? "";
            announceUntil = Time.unscaledTime + 4f;
        }

        static string Clock(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
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

            var d = run.Director;
            if (run.IsFinal) timerText.text = "";
            else if (d != null && d.Timeline != null)
            {
                if (d.InFinalSwarm)
                {
                    timerText.text = $"<color=#ff8866>Swarm +{Clock(d.SwarmSeconds)}</color>  x{FinalSwarm.SurvivalMultiplier(d.SwarmSeconds):0.00}";
                }
                else timerText.text = Clock(d.Remaining);
            }

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
            goldText.text = ExtraLine != null ? ExtraLine() : "";
            killsText.text = $"Kills {run.Kills}";

            var boss = BossController.Active;
            bossBar.gameObject.SetActive(boss != null && !boss.Dead);
            if (boss != null)
            {
                bossFill.fillAmount = boss.MaxHealth > 0f ? boss.Health / boss.MaxHealth : 0f;
                bossName.text = boss.DisplayName;
            }

            var interaction = run.Player != null ? run.Player.GetComponent<InteractionDriver>() : null;
            var current = interaction != null ? interaction.Current : null;
            prompt.gameObject.SetActive(current != null && !run.IsOver && Time.timeScale > 0f);
            if (current != null)
            {
                promptText.text = $"<color=#ffd27a>[E]</color> {current.Prompt}";
                promptFill.fillAmount = interaction.HoldProgress;
            }

            float fade = Mathf.Clamp01(announceUntil - Time.unscaledTime);
            headline.alpha = fade;
            subline.alpha = fade;

            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fpsSmoothed = Mathf.Lerp(fpsSmoothed, 1f / dt, 0.05f);
            int alive = run.Horde != null ? run.Horde.AliveCount : 0;
            infoText.text = $"seed {run.Seed}   stage {run.StageIndex + 1}   enemies {alive}   {fpsSmoothed:0} fps";
        }

        void OnRunOver()
        {
            centerText.text = "";
            prompt.gameObject.SetActive(false);
            ResultsScreen.Show(run);
        }
    }
}
