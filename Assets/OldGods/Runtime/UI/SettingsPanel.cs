using System;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>
    /// Volume, camera and display settings, built into any parent (the menu page or the pause
    /// screen). Changes apply at once and are saved when the panel closes.
    /// </summary>
    public static class SettingsPanel
    {
        /// <summary>Applies saved settings to audio, camera and screen. Call at startup and after changes.</summary>
        public static void Apply(Settings s)
        {
            Audio.ApplySettings(s);
            var run = RunController.Instance;
            if (run != null && run.Camera != null)
            {
                run.Camera.Sensitivity = s.cameraSensitivity;
                run.Camera.InvertY = s.invertY;
            }
            CameraShake.Strength = s.screenShake;
        }

        public static RectTransform Build(Transform parent, Vector2 anchor, Vector2 offset, Action onClose)
        {
            var s = SaveStore.Current.settings;
            var panel = UiKit.Panel(parent, "Settings", new Color(0.08f, 0.07f, 0.07f, 0.92f));
            UiKit.Anchor(panel, anchor, offset, new Vector2(820f, 640f));
            var title = UiKit.Text(panel, "Settings", 44, TextAlignmentOptions.Top);
            UiKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 60f));

            var col = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
            col.SetParent(panel, false);
            UiKit.Anchor(col, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(720f, 430f));
            var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var first = UiKit.Slider(col, "Master volume", 0f, 1f, s.masterVolume, v => { s.masterVolume = v; Apply(s); });
            UiKit.Slider(col, "Music", 0f, 1f, s.musicVolume, v => { s.musicVolume = v; Apply(s); });
            UiKit.Slider(col, "Effects", 0f, 1f, s.sfxVolume, v => { s.sfxVolume = v; Apply(s); });
            UiKit.Slider(col, "Camera sensitivity", 0.2f, 3f, s.cameraSensitivity, v => { s.cameraSensitivity = v; Apply(s); });
            UiKit.Slider(col, "Screen shake", 0f, 1.5f, s.screenShake, v => { s.screenShake = v; Apply(s); });
            UiKit.Toggle(col, "Invert camera Y", s.invertY, v => { s.invertY = v; Apply(s); });
            UiKit.Toggle(col, "Fullscreen", Screen.fullScreen, v => Screen.fullScreen = v);

            var done = UiKit.Button(panel, "Done", 30, () =>
            {
                try { SaveStore.Save(); } catch (Exception e) { Debug.LogWarning(e.Message); }
                onClose?.Invoke();
            });
            UiKit.Anchor(done.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(260f, 60f));
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
            return panel;
        }
    }
}
