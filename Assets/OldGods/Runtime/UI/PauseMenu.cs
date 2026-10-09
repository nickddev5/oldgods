using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>Esc or Start in a run: resume, settings, give up, or return to the menu.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        RectTransform root, buttons, settings;
        RunController run;
        float savedScale = 1f;

        public bool IsOpen => root != null && root.gameObject.activeSelf;

        public static PauseMenu Create(RunController run)
        {
            var canvas = UiKit.Canvas("Pause", 70);
            var menu = canvas.gameObject.AddComponent<PauseMenu>();
            menu.run = run;
            menu.Build(canvas.transform as RectTransform);
            return menu;
        }

        void Build(RectTransform canvas)
        {
            root = UiKit.Panel(canvas, "Dim", new Color(0f, 0f, 0f, 0.65f));
            UiKit.Stretch(root, 0f);
            var title = UiKit.Text(root, "Paused", 60, TextAlignmentOptions.Center);
            UiKit.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(800f, 80f));
            buttons = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            buttons.SetParent(root, false);
            UiKit.Anchor(buttons, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(420f, 320f));
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            UiKit.Button(buttons, "Resume", 30, Close).name = "Resume";
            UiKit.Button(buttons, "Settings", 30, OpenSettings);
            UiKit.Button(buttons, "Give up the run", 30, () => { Close(); run.EndRun(false); });
            if (ResultsScreen.ToMenu != null) UiKit.Button(buttons, "Leave to the menu", 30, () => { Close(); ResultsScreen.ToMenu(); });
            root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (run == null || run.IsOver) return;
            if (!GameInput.Pressed(GameInput.Pause)) return;
            if (IsOpen)
            {
                if (settings != null) CloseSettings();
                else Close();
            }
            else if (Time.timeScale > 0f) Open();
        }

        public void Open()
        {
            savedScale = Time.timeScale;
            Time.timeScale = 0f;
            GameInput.SetCursorLocked(false);
            root.gameObject.SetActive(true);
            buttons.gameObject.SetActive(true);
            Select(buttons.Find("Resume"));
        }

        public void Close()
        {
            if (settings != null) CloseSettings();
            root.gameObject.SetActive(false);
            Time.timeScale = savedScale > 0f ? savedScale : 1f;
            if (!run.IsOver) GameInput.SetCursorLocked(true);
        }

        void OpenSettings()
        {
            buttons.gameObject.SetActive(false);
            settings = SettingsPanel.Build(root, new Vector2(0.5f, 0.5f), Vector2.zero, CloseSettings);
        }

        void CloseSettings()
        {
            if (settings != null) Destroy(settings.gameObject);
            settings = null;
            buttons.gameObject.SetActive(true);
            Select(buttons.Find("Resume"));
        }

        static void Select(Transform t)
        {
            if (EventSystem.current != null && t != null) EventSystem.current.SetSelectedGameObject(t.gameObject);
        }
    }

    /// <summary>Short camera kicks for hits and slams, scaled by the Screen shake setting.</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        public static float Strength = 1f;
        static CameraShake instance;
        CinemachineCameraOffset offset;
        float amount;
        System.Random rng = new System.Random(3);

        public static void Attach(GameObject rig)
        {
            var shake = rig.AddComponent<CameraShake>();
            shake.offset = rig.GetComponent<CinemachineCameraOffset>();
            if (shake.offset == null) shake.offset = rig.AddComponent<CinemachineCameraOffset>();
            instance = shake;
        }

        public static void Kick(float size)
        {
            if (instance == null) return;
            instance.amount = Mathf.Min(1.2f, instance.amount + size * Strength);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            amount = Mathf.MoveTowards(amount, 0f, dt * 3f);
            if (offset == null) return;
            float a = amount * amount;
            offset.Offset = new Vector3((float)(rng.NextDouble() * 2 - 1) * a, (float)(rng.NextDouble() * 2 - 1) * a, 0f) * 0.5f;
        }
    }
}
