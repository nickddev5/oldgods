using System.Collections.Generic;
using System.Linq;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>
    /// The level-up draft: pauses the run and deals three cards. Refresh rerolls them,
    /// Skip takes a little XP instead, Banish removes one card's item from the run.
    /// Mouse, keyboard (1-3, R, Q, X) and gamepad all work.
    /// </summary>
    public sealed class LevelUpScreen : MonoBehaviour
    {
        public static LevelUpScreen Instance { get; private set; }

        public static readonly Color[] RarityColors =
        {
            new Color(0.85f, 0.85f, 0.85f), new Color(0.45f, 0.85f, 0.45f), new Color(0.4f, 0.65f, 1f),
            new Color(0.75f, 0.45f, 1f), new Color(1f, 0.7f, 0.25f),
        };

        PlayerCombat combat;
        Rng rng;
        RectTransform root, cardRow;
        TextMeshProUGUI header, footer;
        readonly List<Button> cards = new List<Button>();
        List<DraftOption> dealt = new List<DraftOption>();
        bool banishing;
        float savedTimeScale = 1f;

        public bool IsOpen => root != null && root.gameObject.activeSelf;
        /// <summary>Test and smoke hook: picks the first card automatically.</summary>
        public static bool AutoPick;
        /// <summary>Play-bot hook: chooses a card from the dealt options. Wins over AutoPick.</summary>
        public static System.Func<IReadOnlyList<DraftOption>, int> Picker;
        /// <summary>Every card taken: the options dealt and the one taken.</summary>
        public static event System.Action<IReadOnlyList<DraftOption>, DraftOption> Taken;

        public static LevelUpScreen Create(PlayerCombat combat, Rng draftRng)
        {
            var canvas = UiKit.Canvas("Level Up", 50);
            var screen = canvas.gameObject.AddComponent<LevelUpScreen>();
            screen.combat = combat;
            screen.rng = draftRng;
            screen.Build(canvas.transform as RectTransform);
            Instance = screen;
            return screen;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build(RectTransform canvas)
        {
            root = UiKit.Panel(canvas, "Dim", new Color(0f, 0f, 0f, 0.6f));
            UiKit.Stretch(root, 0f);
            header = UiKit.Text(root, "Level Up", 54, TextAlignmentOptions.Center);
            UiKit.Anchor(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1200f, 80f));

            var row = new GameObject("Cards", typeof(RectTransform));
            cardRow = row.GetComponent<RectTransform>();
            cardRow.SetParent(root, false);
            UiKit.Anchor(cardRow, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1320f, 440f));
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 36f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            footer = UiKit.Text(root, "", 26, TextAlignmentOptions.Center);
            UiKit.Anchor(footer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -280f), new Vector2(1400f, 60f));

            var buttons = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            buttons.SetParent(root, false);
            UiKit.Anchor(buttons, new Vector2(0.5f, 0.5f), new Vector2(0f, -360f), new Vector2(900f, 64f));
            var bl = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bl.spacing = 24f;
            bl.childControlWidth = bl.childControlHeight = true;
            bl.childForceExpandWidth = bl.childForceExpandHeight = true;
            UiKit.Button(buttons, "Refresh (R)", 26, Refresh);
            UiKit.Button(buttons, "Skip (Q)", 26, Skip);
            UiKit.Button(buttons, "Banish (X)", 26, ToggleBanish);

            root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (combat == null) return;
            if (!IsOpen)
            {
                if (combat.PendingLevelUps > 0 && !ChoiceScreen.IsOpen && !ReadScreen.IsOpen && !(RunController.Instance != null && RunController.Instance.IsOver)) Open();
                return;
            }
            if (Picker != null) { Take(Picker(dealt)); return; }
            if (AutoPick) { Take(0); return; }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Choose(0);
                else if (kb.digit2Key.wasPressedThisFrame) Choose(1);
                else if (kb.digit3Key.wasPressedThisFrame) Choose(2);
            }
            if (GameInput.Pressed(GameInput.Refresh)) Refresh();
            else if (GameInput.Pressed(GameInput.Skip)) Skip();
            else if (GameInput.Pressed(GameInput.Banish)) ToggleBanish();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && cards.Count > 0)
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        void Open()
        {
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            GameInput.SetCursorLocked(false);
            root.gameObject.SetActive(true);
            banishing = false;
            Audio.Play(Sfx.LevelUp, 0.8f, 0f);
            if (combat != null)
                Effects.Burst(Fx.Ring(0.8f, 40), combat.transform.position + Vector3.up * 0.2f, Quaternion.identity, Vector3.one, Vector3.one * 6f,
                    new Color(0.5f, 0.85f, 1.6f, 0.8f), 0.6f);
            Deal(null);
        }

        void Close()
        {
            root.gameObject.SetActive(false);
            Time.timeScale = savedTimeScale;
            GameInput.SetCursorLocked(true);
        }

        void Deal(ICollection<string> exclude)
        {
            dealt = DraftRules.Roll(combat.Loadout, Available(combat.Content.Weapons), Available(combat.Content.Passives),
                rng, combat.Stats.Value(StatId.Luck), 3, exclude);
            ShowCards();
        }

        static IReadOnlyList<T> Available<T>(List<T> all) where T : class
        {
            var save = SaveStore.Current;
            return all.Where(x =>
            {
                string unlock = x is WeaponDef w ? w.UnlockId : (x as PassiveDef)?.UnlockId;
                return string.IsNullOrEmpty(unlock) || save.IsUnlocked(unlock);
            }).ToList();
        }

        void ShowCards()
        {
            foreach (var c in cards) Destroy(c.gameObject);
            cards.Clear();
            for (int i = 0; i < dealt.Count; i++)
            {
                int index = i;
                var o = dealt[i];
                var b = UiKit.Button(cardRow, "", 24, () => Choose(index));
                var label = b.GetComponentInChildren<TextMeshProUGUI>();
                var rc = RarityColors[(int)o.Rarity];
                string rarity = o.Kind == DraftKind.UpgradeWeapon || o.Kind == DraftKind.UpgradePassive ? o.Rarity.ToString() : KindLabel(o.Kind);
                label.text = $"<size=22><color=#{ColorUtility.ToHtmlStringRGB(rc)}>{rarity}</color></size>\n\n" +
                             $"<size=36><b>{o.Title}</b></size>\n\n{o.Description}\n\n<size=20><color=#999999>[{i + 1}]</color></size>";
                label.alignment = TextAlignmentOptions.Center;
                var img = b.GetComponent<Image>();
                img.color = Color.Lerp(new Color(0.1f, 0.09f, 0.08f, 0.95f), rc, 0.12f);
                var outline = b.gameObject.AddComponent<Outline>();
                outline.effectColor = rc;
                outline.effectDistance = new Vector2(3f, -3f);
                cards.Add(b);
            }
            var ch = combat.Charges;
            header.text = banishing ? "Banish which?" : $"Level {combat.Xp.Level}";
            footer.text = $"Refresh {ch.Refresh}    Skip {ch.Skip}    Banish {ch.Banish}" + (combat.PendingLevelUps > 1 ? $"    ({combat.PendingLevelUps - 1} more)" : "");
            if (EventSystem.current != null && cards.Count > 0) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        }

        static string KindLabel(DraftKind k) => k == DraftKind.NewWeapon ? "New Weapon" : k == DraftKind.NewPassive ? "New Passive" : "Restore";

        void Choose(int index)
        {
            if (index < 0 || index >= dealt.Count) return;
            if (banishing)
            {
                if (DraftRules.Banish(combat.Loadout, combat.Charges, dealt[index].Id))
                {
                    banishing = false;
                    var keep = dealt.Where((_, i) => i != index).Select(d => d.Id).ToList();
                    var replacement = DraftRules.Roll(combat.Loadout, Available(combat.Content.Weapons), Available(combat.Content.Passives),
                        rng, combat.Stats.Value(StatId.Luck), 1, keep);
                    dealt[index] = replacement[0];
                    ShowCards();
                }
                return;
            }
            Take(index);
        }

        void Take(int index)
        {
            var o = dealt[Mathf.Clamp(index, 0, dealt.Count - 1)];
            Taken?.Invoke(dealt, o);
            DraftRules.Apply(combat.Loadout, o, combat.Content.Weapons, combat.Content.Passives);
            if (o.Kind == DraftKind.Restore)
            {
                var h = combat.GetComponent<PlayerHealth>();
                if (h != null) h.Health.Heal(h.Health.Max * 0.3f);
            }
            combat.SyncDrivers();
            combat.RecomputeStats();
            Next();
        }

        void Refresh()
        {
            if (combat.Charges.Refresh <= 0) return;
            combat.Charges.Refresh--;
            Deal(dealt.Select(d => d.Id).ToList());
        }

        void Skip()
        {
            if (combat.Charges.Skip <= 0) return;
            combat.Charges.Skip--;
            float reward = XpRules.SkipReward(combat.Xp.Level);
            Next();
            combat.AddXp(reward);
        }

        void ToggleBanish()
        {
            if (combat.Charges.Banish <= 0) return;
            banishing = !banishing;
            ShowCards();
        }

        void Next()
        {
            combat.PendingLevelUps = Mathf.Max(0, combat.PendingLevelUps - 1);
            if (combat.PendingLevelUps > 0) Deal(null);
            else Close();
        }
    }
}
