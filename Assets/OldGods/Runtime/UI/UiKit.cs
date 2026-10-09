using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>Small helpers for building uGUI screens in code.</summary>
    public static class UiKit
    {
        static Sprite white;

        public static Sprite WhiteSprite
        {
            get
            {
                if (white == null)
                {
                    var tex = Texture2D.whiteTexture;
                    white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                return white;
            }
        }

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = WhiteSprite;
            return go.GetComponent<RectTransform>();
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, float fontSize, System.Action onClick)
        {
            var rt = Panel(parent, "Button", new Color(0.12f, 0.11f, 0.1f, 0.92f));
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
            colors.selectedColor = new Color(1f, 0.85f, 0.55f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.5f);
            button.colors = colors;
            var t = Text(rt, label, fontSize, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 8f);
            button.onClick.AddListener(() => Audio.Play(Sfx.Click, 0.6f, 0.05f));
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>A labelled horizontal slider row, keyboard and gamepad navigable.</summary>
        public static Slider Slider(Transform parent, string label, float min, float max, float value, System.Action<float> changed)
        {
            var row = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            var text = Text(row, label, 26, TextAlignmentOptions.Left);
            text.rectTransform.anchorMin = new Vector2(0f, 0f);
            text.rectTransform.anchorMax = new Vector2(0.45f, 1f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            var area = Panel(row, "Slider", new Color(0f, 0f, 0f, 0.6f));
            area.anchorMin = new Vector2(0.48f, 0.3f);
            area.anchorMax = new Vector2(1f, 0.7f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(area, false);
            Stretch(fillArea, 0f);
            var fill = Panel(fillArea, "Fill", new Color(1f, 0.72f, 0.38f));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(area, false);
            Stretch(handleArea, 0f);
            var handle = Panel(handleArea, "Handle", Color.white);
            handle.sizeDelta = new Vector2(18f, 0f);
            handle.anchorMin = new Vector2(0f, -0.4f);
            handle.anchorMax = new Vector2(0f, 1.4f);

            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(value, min, max);
            var colors = slider.colors;
            colors.selectedColor = new Color(1f, 0.85f, 0.55f);
            colors.highlightedColor = new Color(1f, 0.9f, 0.7f);
            slider.colors = colors;
            slider.onValueChanged.AddListener(v => changed(v));
            return slider;
        }

        /// <summary>A labelled on/off row.</summary>
        public static Toggle Toggle(Transform parent, string label, bool value, System.Action<bool> changed)
        {
            var row = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            var box = Panel(row, "Box", new Color(0f, 0f, 0f, 0.6f));
            box.anchorMin = box.anchorMax = new Vector2(0.48f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.sizeDelta = new Vector2(36f, 36f);
            var check = Panel(box, "Check", new Color(1f, 0.72f, 0.38f));
            Stretch(check, 6f);
            var text = Text(row, label, 26, TextAlignmentOptions.Left);
            text.rectTransform.anchorMin = new Vector2(0f, 0f);
            text.rectTransform.anchorMax = new Vector2(0.45f, 1f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = value;
            var colors = toggle.colors;
            colors.selectedColor = new Color(1f, 0.85f, 0.55f);
            toggle.colors = colors;
            toggle.onValueChanged.AddListener(v => changed(v));
            return toggle;
        }

        /// <summary>Anchors and pivots at the same normalized point, then offsets.</summary>
        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
