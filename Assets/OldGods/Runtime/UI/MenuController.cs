using System.Collections.Generic;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>
    /// The menu scene: a lit stage with the selected god on a plinth, the main menu and
    /// character select. Later milestones add more pages through AddPage.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        public const string MenuScene = "Menu";
        public const string RunScene = "Run";

        public static MenuController Instance { get; private set; }

        public GameAssets Assets;

        public ContentSet Content { get; private set; }
        public RectTransform Root { get; private set; }
        RectTransform mainPage, selectPage, cardGrid, detail;
        TextMeshProUGUI detailText;
        Button playButton;
        Transform preview;
        MeshFilter previewMesh;
        MeshRenderer previewRenderer;
        readonly Dictionary<string, RectTransform> pages = new Dictionary<string, RectTransform>();
        string selected;

        /// <summary>Extra main-menu buttons added by later milestones: (label, page builder).</summary>
        public static readonly List<(string label, System.Func<MenuController, RectTransform> build)> ExtraPages = new List<(string, System.Func<MenuController, RectTransform>)>();

        void Awake()
        {
            Instance = this;
            if (Assets == null) Assets = GameAssets.Load();
            Time.timeScale = 1f;
            GameInput.Ensure();
            GameInput.SetCursorLocked(false);
            Fx.Init(Assets);
            Content = Assets.Content.Load();
            RunSetup.Direct = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // Diagnostic runs skip the menu; the smoke test drives it on purpose.
            if ((CommandLine.Has("-probe") || CommandLine.Has("-shots")) && !CommandLine.Has("-shotMenu"))
            {
                StartRun(RunSetup.GodId);
                return;
            }
            BuildStage();
            var canvas = UiKit.Canvas("Menu", 10);
            Root = canvas.transform as RectTransform;
            mainPage = BuildMain();
            selectPage = BuildSelect();
            foreach (var (label, build) in ExtraPages)
            {
                var page = build(this);
                page.gameObject.SetActive(false);
                pages[label] = page;
            }
            var first = GodRules.Default(Content.Gods, SaveStore.Current.IsUnlocked);
            if (first != null) ShowPreview(first.Id);
            ShowMain();
            var save = SaveStore.Current;
            if (!save.seenPremise && Assets.Story != null && !CommandLine.Has("-smoke"))
            {
                save.seenPremise = true;
                try { SaveStore.Save(save); } catch (System.Exception e) { Debug.LogWarning(e.Message); }
                ReadScreen.Show("Before the throne", Assets.Story.Premise);
            }
        }

        void BuildStage()
        {
            var cam = new GameObject("Menu Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 2.4f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            cam.fieldOfView = 45f;
            cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            WorldBuilder.CreateSun(null, new Color(1f, 0.9f, 0.75f), 1.4f, new Vector3(35f, -40f, 0f));
            WorldBuilder.SetAtmosphere(new Color(0.35f, 0.33f, 0.32f), new Color(0.25f, 0.23f, 0.22f), new Color(0.1f, 0.1f, 0.1f),
                new Color(0.12f, 0.11f, 0.11f), 10f, 40f);
            var stone = WorldBuilder.Tinted(Assets.LowPoly, new Color(0.55f, 0.53f, 0.5f));
            WorldBuilder.CreateProp("Floor", Fx.Disc(24), stone, Vector3.zero, Quaternion.identity, new Vector3(30f, 1f, 30f), null, false);
            WorldBuilder.CreateProp("Plinth", Fx.Column(), stone, new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(1.2f, 0.4f, 1.2f), null, false);
            preview = new GameObject("Preview").transform;
            preview.position = new Vector3(0f, 0.4f, 0f);
            preview.localScale = Vector3.one * 1.3f;
            previewMesh = preview.gameObject.AddComponent<MeshFilter>();
            previewRenderer = preview.gameObject.AddComponent<MeshRenderer>();
        }

        void Update()
        {
            if (preview != null) preview.rotation = Quaternion.Euler(0f, 180f + Mathf.Sin(Time.time * 0.6f) * 35f, 0f);
            if (GameInput.Pressed(GameInput.Pause) && !mainPage.gameObject.activeSelf) ShowMain();
        }

        void ShowPreview(string godId)
        {
            var g = Assets.Content.GodAsset(godId);
            if (g == null) return;
            previewMesh.sharedMesh = PlaceholderMeshes.God(g.Look);
            previewRenderer.sharedMaterial = WorldBuilder.Tinted(Assets.LowPoly, g.Robe);
        }

        RectTransform Page(string name)
        {
            var p = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            p.SetParent(Root, false);
            UiKit.Stretch(p, 0f);
            return p;
        }

        public RectTransform NewPage(string name, string title)
        {
            var p = Page(name);
            var head = UiKit.Text(p, title, 56, TextAlignmentOptions.TopLeft);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(80f, -60f), new Vector2(1200f, 80f));
            var back = UiKit.Button(p, "Back", 28, ShowMain);
            UiKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(80f, 60f), new Vector2(220f, 60f));
            return p;
        }

        RectTransform BuildMain()
        {
            var p = Page("Main");
            var title = UiKit.Text(p, "THE OLD GODS", 92, TextAlignmentOptions.Left);
            UiKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -150f), new Vector2(1000f, 120f));
            var sub = UiKit.Text(p, "Before the throne", 30, TextAlignmentOptions.Left);
            sub.color = new Color(1f, 1f, 1f, 0.7f);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(104f, -260f), new Vector2(1000f, 50f));

            var col = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            col.SetParent(p, false);
            int count = 2 + ExtraPages.Count;
            UiKit.Anchor(col, new Vector2(0f, 0.5f), new Vector2(100f, -60f), new Vector2(420f, 78f * count));
            var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            var begin = UiKit.Button(col, "Begin", 34, ShowSelect);
            foreach (var (label, _) in ExtraPages)
            {
                string l = label;
                UiKit.Button(col, l, 30, () => ShowPage(l));
            }
            UiKit.Button(col, "Leave", 30, Application.Quit);
            begin.name = "Begin";
            return p;
        }

        RectTransform BuildSelect()
        {
            var p = NewPage("Select", "Choose a god");
            var grid = new GameObject("Gods", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(p, false);
            UiKit.Anchor(grid, new Vector2(0f, 1f), new Vector2(80f, -170f), new Vector2(640f, 760f));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(300f, 92f);
            gl.spacing = new Vector2(20f, 16f);
            cardGrid = grid;

            detail = UiKit.Panel(p, "Detail", new Color(0f, 0f, 0f, 0.55f));
            UiKit.Anchor(detail, new Vector2(1f, 0.5f), new Vector2(-80f, 120f), new Vector2(600f, 500f));
            detailText = UiKit.Text(detail, "", 26, TextAlignmentOptions.TopLeft);
            UiKit.Stretch(detailText.rectTransform, 28f);
            MetaPages.AddModifierToggles(p);
            playButton = UiKit.Button(p, "Walk the road", 34, Play);
            UiKit.Anchor(playButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-80f, 60f), new Vector2(380f, 74f));
            if (Debug.isDebugBuild || Application.isEditor)
            {
                var dev = UiKit.Button(p, "Unlock all (development)", 20, () =>
                {
                    foreach (var g in Content.Gods) SaveStore.Current.Unlock(GodRules.UnlockId(g));
                    SaveStore.Save();
                    RefreshCards();
                });
                UiKit.Anchor(dev.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(340f, 50f));
            }
            return p;
        }

        void RefreshCards()
        {
            foreach (Transform c in cardGrid) Destroy(c.gameObject);
            var save = SaveStore.Current;
            Button first = null;
            foreach (var g in GodRules.Ordered(Content.Gods))
            {
                bool open = GodRules.IsUnlocked(g, save.IsUnlocked);
                var god = g;
                var b = UiKit.Button(cardGrid, open ? g.Name : $"<color=#777777>{g.Name}</color>\n<size=18><color=#777777>Locked</color></size>", 28, () => Select(god.Id));
                var asset = Assets.Content.GodAsset(g.Id);
                if (asset != null) b.GetComponent<Image>().color = Color.Lerp(new Color(0.1f, 0.09f, 0.08f, 0.92f), asset.Robe, open ? 0.35f : 0.08f);
                if (first == null && open) first = b;
            }
            if (selected == null || Content.God(selected) == null)
            {
                var def = GodRules.Default(Content.Gods, save.IsUnlocked);
                selected = def != null ? def.Id : null;
            }
            Select(selected);
            if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void Select(string id)
        {
            selected = id;
            var g = Content.God(id);
            if (g == null) return;
            var save = SaveStore.Current;
            bool open = GodRules.IsUnlocked(g, save.IsUnlocked);
            string weapon = Content.Weapons.Find(w => w.Id == g.StartingWeapon)?.Name ?? g.StartingWeapon;
            string passive = Content.Passives.Find(x => x.Id == g.StartingPassive)?.Name ?? g.StartingPassive;
            var kit = new List<string>();
            foreach (var m in g.Kit) kit.Add(StatText.Describe(m));
            string lockLine = open ? "" : g.IsLast
                ? $"\n\n<color=#ffb860>Locked. Unlock every other god, then {g.Cost} Embers.</color>"
                : $"\n\n<color=#ffb860>Locked. {g.Cost} Embers at the Shrine of Embers.</color>";
            detailText.text = $"<size=48><b>{g.Name}</b></size>\n<i>{g.Lore}</i>\n\nWeapon  <b>{weapon}</b>\nPassive  <b>{passive}</b>\n{string.Join("\n", kit)}{lockLine}";
            playButton.interactable = open;
            ShowPreview(id);
        }

        public void ShowMain()
        {
            mainPage.gameObject.SetActive(true);
            selectPage.gameObject.SetActive(false);
            foreach (var p in pages.Values) p.gameObject.SetActive(false);
            var begin = mainPage.Find("Buttons/Begin");
            if (EventSystem.current != null && begin != null) EventSystem.current.SetSelectedGameObject(begin.gameObject);
        }

        public void ShowSelect()
        {
            mainPage.gameObject.SetActive(false);
            selectPage.gameObject.SetActive(true);
            RefreshCards();
        }

        public void ShowPage(string label)
        {
            mainPage.gameObject.SetActive(false);
            selectPage.gameObject.SetActive(false);
            foreach (var kv in pages) kv.Value.gameObject.SetActive(kv.Key == label);
            PageShown?.Invoke(label);
        }

        public static event System.Action<string> PageShown;

        /// <summary>Starts a run with the selected god.</summary>
        public void Play()
        {
            var g = Content.God(selected);
            if (g == null || !GodRules.IsUnlocked(g, SaveStore.Current.IsUnlocked)) return;
            StartRun(g.Id);
        }

        public static void StartRun(string godId)
        {
            RunSetup.GodId = godId;
            SceneManager.LoadScene(RunScene);
        }
    }
}
