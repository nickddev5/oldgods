using System.Collections.Generic;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>Menu pages for meta-progression: the Shrine of Embers (unlocks and powerups) and quests.</summary>
    public static class MetaPages
    {
        public const string Shrine = "Shrine of Embers";
        public const string Quests = "Quests";
        public const string Lore = "Lore";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            MenuController.ExtraPages.RemoveAll(p => p.label == Shrine || p.label == Quests || p.label == Lore);
            MenuController.ExtraPages.Add((Shrine, BuildShrine));
            MenuController.ExtraPages.Add((Quests, BuildQuests));
            MenuController.ExtraPages.Add((Lore, BuildLore));
        }

        static RectTransform Column(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var scroll = UiKit.Panel(parent, name, new Color(0f, 0f, 0f, 0.5f));
            UiKit.Anchor(scroll, anchor, offset, size);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(scroll, false);
            UiKit.Stretch(content, 14f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return content;
        }

        static RectTransform BuildShrine(MenuController menu)
        {
            var page = menu.NewPage("Shrine", Shrine);
            var balance = UiKit.Text(page, "", 34, TextAlignmentOptions.TopRight);
            UiKit.Anchor(balance.rectTransform, new Vector2(1f, 1f), new Vector2(-80f, -70f), new Vector2(600f, 50f));
            var unlocks = Column(page, "Unlocks", new Vector2(0f, 1f), new Vector2(80f, -160f), new Vector2(860f, 760f));
            var powers = Column(page, "Powerups", new Vector2(1f, 1f), new Vector2(-80f, -160f), new Vector2(760f, 520f));

            void Refresh()
            {
                var save = SaveStore.Current;
                balance.text = $"<color=#ffb860>{save.currency} Embers</color>";
                foreach (Transform c in unlocks) Object.Destroy(c.gameObject);
                foreach (Transform c in powers) Object.Destroy(c.gameObject);
                var lib = menu.Assets.Content;
                var tree = MetaRules.Tree(menu.Content, lib.UnlockCost);
                foreach (var e in tree)
                {
                    bool owned = save.IsUnlocked(e.Id);
                    bool can = MetaRules.CanUnlock(save, e, menu.Content.Gods);
                    string state = owned ? "<color=#88dd88>Unlocked</color>" : $"{e.Cost} Embers";
                    var entry = e;
                    var b = UiKit.Button(unlocks, $"{e.Kind}: <b>{e.Name}</b>    {state}", 22, () =>
                    {
                        if (MetaRules.TryUnlock(SaveStore.Current, entry, menu.Content.Gods)) { SaveStore.Save(); Refresh(); }
                    });
                    b.interactable = can;
                    b.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
                }
                foreach (var p in MetaCatalog.Powerups)
                {
                    int level = MetaRules.PowerupLevel(save, p.Id);
                    int cost = MetaRules.PowerupCost(p, level);
                    string next = level >= p.MaxLevel ? "<color=#88dd88>Full</color>" : $"{cost} Embers";
                    var def = p;
                    var b = UiKit.Button(powers, $"<b>{p.Name}</b> {level}/{p.MaxLevel}   {StatText.Describe(p.PerLevel)} each    {next}", 22, () =>
                    {
                        if (MetaRules.TryBuyPowerup(SaveStore.Current, def)) { SaveStore.Save(); Refresh(); }
                    });
                    b.interactable = level < p.MaxLevel && save.currency >= cost;
                    b.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
                }
            }

            MenuController.PageShown += label => { if (label == Shrine && page != null) Refresh(); };
            Refresh();
            return page;
        }

        static RectTransform BuildQuests(MenuController menu)
        {
            var page = menu.NewPage("Quests", Quests);
            var list = Column(page, "List", new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1100f, 780f));

            void Refresh()
            {
                var save = SaveStore.Current;
                foreach (Transform c in list) Object.Destroy(c.gameObject);
                foreach (var q in MetaCatalog.Quests)
                {
                    bool done = save.completedQuests.Contains(q.Id);
                    int value = Mathf.Min(MetaRules.QuestProgressValue(save, q), q.Target);
                    string progress = done ? "<color=#88dd88>Done</color>" : $"{value} / {q.Target}";
                    var t = UiKit.Text(list, $"<b>{q.Name}</b>   {q.Description}{(q.BestRun ? " (in one run)" : "")}\n<size=20>{progress}    reward {q.Reward} Embers</size>", 24, TextAlignmentOptions.Left);
                    t.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
                    if (done) t.color = new Color(1f, 1f, 1f, 0.6f);
                }
            }

            MenuController.PageShown += label => { if (label == Quests && page != null) Refresh(); };
            Refresh();
            return page;
        }

        static RectTransform BuildLore(MenuController menu)
        {
            var page = menu.NewPage("Lore", Lore);
            var list = Column(page, "List", new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1200f, 780f));

            void Refresh()
            {
                foreach (Transform c in list) Object.Destroy(c.gameObject);
                var story = menu.Assets.Story;
                if (story == null) return;
                var premise = UiKit.Text(list, $"<b>Before the throne</b>\n<size=22>{story.Premise}</size>", 26, TextAlignmentOptions.TopLeft);
                premise.gameObject.AddComponent<LayoutElement>().preferredHeight = 230f;
                var save = SaveStore.Current;
                int found = 0;
                foreach (var f in story.Fragments)
                {
                    bool has = save.lore.Contains(f.Id);
                    if (has) found++;
                    var t = UiKit.Text(list, has ? $"<b>{f.Title}</b>\n<size=20>{f.Text}</size>" : "<color=#777777>A stone not yet read</color>", 24, TextAlignmentOptions.TopLeft);
                    t.gameObject.AddComponent<LayoutElement>().preferredHeight = has ? 92f : 34f;
                }
                var count = UiKit.Text(list, $"<size=20>{found} of {story.Fragments.Count} stones read</size>", 20, TextAlignmentOptions.Left);
                count.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            }

            MenuController.PageShown += label => { if (label == Lore && page != null) Refresh(); };
            Refresh();
            return page;
        }

        /// <summary>Toggle row for difficulty modifiers on the character select page.</summary>
        public static void AddModifierToggles(RectTransform selectPage)
        {
            var row = new GameObject("Modifiers", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(selectPage, false);
            UiKit.Anchor(row, new Vector2(1f, 0f), new Vector2(-80f, 160f), new Vector2(600f, 200f));
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var buttons = new List<(Button b, ModifierDef m)>();

            void Paint()
            {
                foreach (var (b, m) in buttons)
                {
                    bool on = RunSetup.Modifiers.Contains(m.Id);
                    b.GetComponentInChildren<TextMeshProUGUI>().text = $"{(on ? "<color=#ffb860>[x]</color>" : "[ ]")} {m.Name}: {m.Description}  <color=#ffb860>+{m.Payout * 100f:0}%</color>";
                }
            }

            foreach (var m in MetaCatalog.Modifiers)
            {
                var mod = m;
                Button b = null;
                b = UiKit.Button(row, "", 18, () =>
                {
                    if (!RunSetup.Modifiers.Remove(mod.Id)) RunSetup.Modifiers.Add(mod.Id);
                    Paint();
                });
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                buttons.Add((b, m));
            }
            Paint();
        }
    }
}
