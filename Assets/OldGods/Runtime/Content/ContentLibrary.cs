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

        public ContentSet Set { get; private set; }
        readonly Dictionary<string, EnemyDefinition> enemyAssets = new Dictionary<string, EnemyDefinition>();

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
            foreach (var problem in set.Validate()) Debug.LogWarning($"OldGods content: {problem}");
            Set = set;
            return set;
        }

        public EnemyDefinition EnemyAsset(string id) => enemyAssets.TryGetValue(id, out var a) ? a : null;
    }
}
