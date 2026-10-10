using System;
using System.Collections.Generic;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>A paused pick-one-of-N screen for shrines and other offers.</summary>
    public sealed class ChoiceScreen : MonoBehaviour
    {
        public struct Option
        {
            public string Title;
            public string Description;
            public Rarity Rarity;
        }

        static ChoiceScreen instance;
        RectTransform root, row;
        TextMeshProUGUI header;
        readonly List<Button> buttons = new List<Button>();
        Action<int> onPick;
        float savedScale = 1f;

        public static bool IsOpen => instance != null && instance.root.gameObject.activeSelf;
        /// <summary>Play-bot hook: chooses an option. Wins over LevelUpScreen.AutoPick.</summary>
        public static Func<IList<Option>, int> Picker;
        IList<Option> shown;

        public static void Show(string title, IList<Option> options, Action<int> pick)
        {
            if (instance == null)
            {
                var canvas = UiKit.Canvas("Choice", 55);
                instance = canvas.gameObject.AddComponent<ChoiceScreen>();
                instance.Build(canvas.transform as RectTransform);
            }
            instance.Open(title, options, pick);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Build(RectTransform canvas)
        {
            root = UiKit.Panel(canvas, "Dim", new Color(0f, 0f, 0f, 0.6f));
            UiKit.Stretch(root, 0f);
            header = UiKit.Text(root, "", 48, TextAlignmentOptions.Center);
            UiKit.Anchor(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1200f, 70f));
            var go = new GameObject("Options", typeof(RectTransform));
            row = go.GetComponent<RectTransform>();
            row.SetParent(root, false);
            UiKit.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1200f, 360f));
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 32f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            root.gameObject.SetActive(false);
        }

        void Open(string title, IList<Option> options, Action<int> pick)
        {
            onPick = pick;
            shown = options;
            header.text = title;
            foreach (var b in buttons) Destroy(b.gameObject);
            buttons.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var o = options[i];
                var b = UiKit.Button(row, "", 24, () => Pick(index));
                var rc = LevelUpScreen.RarityColors[(int)o.Rarity];
                b.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"<size=22><color=#{ColorUtility.ToHtmlStringRGB(rc)}>{o.Rarity}</color></size>\n\n<size=34><b>{o.Title}</b></size>\n\n{o.Description}\n\n<size=20><color=#999999>[{i + 1}]</color></size>";
                b.GetComponent<Image>().color = Color.Lerp(new Color(0.1f, 0.09f, 0.08f, 0.95f), rc, 0.12f);
                buttons.Add(b);
            }
            savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            GameInput.SetCursorLocked(false);
            root.gameObject.SetActive(true);
            if (EventSystem.current != null && buttons.Count > 0) EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (Picker != null) { Pick(Picker(shown)); return; }
            if (LevelUpScreen.AutoPick) { Pick(0); return; }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) Pick(0);
            else if (kb.digit2Key.wasPressedThisFrame) Pick(1);
            else if (kb.digit3Key.wasPressedThisFrame) Pick(2);
        }

        void Pick(int index)
        {
            if (index < 0 || index >= buttons.Count) return;
            root.gameObject.SetActive(false);
            Time.timeScale = savedScale;
            GameInput.SetCursorLocked(true);
            var cb = onPick;
            onPick = null;
            cb?.Invoke(index);
        }
    }
}
