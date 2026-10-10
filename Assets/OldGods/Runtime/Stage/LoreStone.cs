using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>A carved stone with a fragment of the old gods' story. Reading it records it in the save.</summary>
    public sealed class LoreStone : Interactable
    {
        public StoryText.Fragment Fragment;
        bool read;

        public override string Prompt => "Read the stone";
        public override bool CanUse => !read;
        public override string MapLabel => read ? null : "Lore";
        public override Color MapColor => new Color(0.85f, 0.8f, 1f);

        void Awake()
        {
            Range = 3f;
            HoldSeconds = 0.3f;
        }

        public override void Use(PlayerCombat player)
        {
            read = true;
            var save = SaveStore.Current;
            if (!save.lore.Contains(Fragment.Id))
            {
                save.lore.Add(Fragment.Id);
                try { SaveStore.Save(save); } catch (System.Exception e) { Debug.LogWarning(e.Message); }
            }
            ReadScreen.Show(Fragment.Title, Fragment.Text);
            foreach (Transform c in transform) if (c.name == "Glow") c.gameObject.SetActive(false);
        }

        public static void Place(RunController run, Vector3 at, Transform parent, int stageIndex)
        {
            var story = run.Assets.Story;
            if (story == null) return;
            var candidates = story.Fragments.FindAll(f => f.BiomeId == run.Biome.Id);
            if (candidates.Count == 0) return;
            var unread = candidates.FindAll(f => !SaveStore.Current.lore.Contains(f.Id));
            var pool = unread.Count > 0 ? unread : candidates;
            var fragment = pool[run.Seed.Stream("lore", stageIndex).Range(0, pool.Count)];
            var go = WorldBuilder.CreateProp("Lore Stone", PlaceholderMeshes.Monolith(), WorldBuilder.Tinted(run.Assets.LowPoly, new Color(0.5f, 0.5f, 0.55f)),
                Ground.Snap(at), Quaternion.Euler(0f, Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg, 0f), new Vector3(0.45f, 0.6f, 0.45f), parent, true);
            go.AddComponent<LoreStone>().Fragment = fragment;
            Features.AddGlow(go.transform, new Vector3(0f, 4.6f, 0f), new Vector3(0.8f, 1f, 0.8f), new Color(1.2f, 1.1f, 2f));
        }
    }

    /// <summary>A paused page of text with a Continue button.</summary>
    public sealed class ReadScreen : MonoBehaviour
    {
        static ReadScreen instance;
        RectTransform root;
        TextMeshProUGUI title, body;
        Button close;
        float savedScale = 1f;

        public static bool IsOpen => instance != null && instance.root.gameObject.activeSelf;
        /// <summary>Play-bot hook: closes every page as soon as it opens.</summary>
        public static bool AutoClose;

        public static void Show(string heading, string text)
        {
            if (instance == null)
            {
                var canvas = UiKit.Canvas("Read", 58);
                instance = canvas.gameObject.AddComponent<ReadScreen>();
                instance.Build(canvas.transform as RectTransform);
            }
            instance.Open(heading, text);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Build(RectTransform canvas)
        {
            root = UiKit.Panel(canvas, "Dim", new Color(0f, 0f, 0f, 0.75f));
            UiKit.Stretch(root, 0f);
            var page = UiKit.Panel(root, "Page", new Color(0.12f, 0.11f, 0.1f, 0.95f));
            UiKit.Anchor(page, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 640f));
            title = UiKit.Text(page, "", 44, TextAlignmentOptions.Top);
            UiKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 60f));
            body = UiKit.Text(page, "", 28, TextAlignmentOptions.TopLeft);
            UiKit.Anchor(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(860f, 400f));
            close = UiKit.Button(page, "Continue", 28, Close);
            UiKit.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 64f));
            root.gameObject.SetActive(false);
        }

        void Open(string heading, string text)
        {
            title.text = heading;
            body.text = text;
            savedScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            GameInput.SetCursorLocked(false);
            root.gameObject.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }

        void Update()
        {
            if (IsOpen && (LevelUpScreen.AutoPick || AutoClose || GameInput.Pressed(GameInput.Pause))) Close();
        }

        void Close()
        {
            if (!IsOpen) return;
            root.gameObject.SetActive(false);
            Time.timeScale = savedScale;
            if (RunController.Instance != null && !RunController.Instance.IsOver) GameInput.SetCursorLocked(true);
        }
    }
}
