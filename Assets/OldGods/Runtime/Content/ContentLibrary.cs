using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Every content asset the game uses. Load() turns them into an engine-free
    /// ContentSet for the rules, and keeps the links back to the assets for visuals.
    /// </summary>
    [CreateAssetMenu(menuName = "Old Gods/Content Library", fileName = "ContentLibrary")]
    public sealed class ContentLibrary : ScriptableObject
    {
        public List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
        public List<WeaponDefinition> Weapons = new List<WeaponDefinition>();
        public List<PassiveDefinition> Passives = new List<PassiveDefinition>();
        public List<ItemDefinition> Items = new List<ItemDefinition>();
        public List<BossDefinition> Bosses = new List<BossDefinition>();
        public List<GodDefinition> Gods = new List<GodDefinition>();

        public ContentSet Set { get; private set; }
        readonly Dictionary<string, EnemyDefinition> enemyAssets = new Dictionary<string, EnemyDefinition>();
        readonly Dictionary<string, WeaponDefinition> weaponAssets = new Dictionary<string, WeaponDefinition>();
        readonly Dictionary<string, PassiveDefinition> passiveAssets = new Dictionary<string, PassiveDefinition>();

        public ContentSet Load()
        {
            var set = new ContentSet();
            enemyAssets.Clear();
            foreach (var e in Enemies)
            {
                if (e == null) continue;
                set.Add(e.ToDef());
                enemyAssets[e.Id] = e;
            }
            weaponAssets.Clear();
            foreach (var w in Weapons)
            {
                if (w == null) continue;
                set.Weapons.Add(w.ToDef());
                weaponAssets[w.Id] = w;
            }
            passiveAssets.Clear();
            foreach (var p in Passives)
            {
                if (p == null) continue;
                set.Passives.Add(p.ToDef());
                passiveAssets[p.Id] = p;
            }
            foreach (var it in Items)
                if (it != null) set.Items.Add(it.ToDef());
            foreach (var g in Gods)
                if (g != null) set.Gods.Add(g.ToDef());
            foreach (var problem in set.Validate()) Debug.LogWarning($"OldGods content: {problem}");
            Set = set;
            return set;
        }

        public EnemyDefinition EnemyAsset(string id) => enemyAssets.TryGetValue(id, out var a) ? a : null;
        public WeaponDefinition WeaponAsset(string id) => weaponAssets.TryGetValue(id, out var a) ? a : null;
        public PassiveDefinition PassiveAsset(string id) => passiveAssets.TryGetValue(id, out var a) ? a : null;
        public GodDefinition GodAsset(string id) => Gods.Find(g => g != null && g.Id == id);
    }
}
