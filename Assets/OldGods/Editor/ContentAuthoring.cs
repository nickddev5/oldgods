using OldGods.Runtime;
using UnityEditor;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>
    /// Creates the starting content assets the first time, then leaves their values alone
    /// so tuning done in the Inspector is kept. New entries are added when missing.
    /// </summary>
    public static class ContentAuthoring
    {
        public static void Populate(ContentLibrary library)
        {
            library.Enemies.RemoveAll(e => e == null);
            Enemy(library, "Husk", e =>
            {
                e.Id = "enemy.husk"; e.DisplayName = "Husk";
                e.MaxHealth = 12f; e.MoveSpeed = 3.6f; e.Radius = 0.4f; e.ContactDamage = 6f; e.XpValue = 1;
                e.Color = new Color(0.55f, 0.32f, 0.26f);
            });
        }

        static EnemyDefinition Enemy(ContentLibrary library, string file, System.Action<EnemyDefinition> init)
        {
            string path = $"{ProjectBuilder.EnemiesDir}/{file}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (asset == null)
            {
                ProjectBuilder.EnsureFolder(ProjectBuilder.EnemiesDir);
                asset = ScriptableObject.CreateInstance<EnemyDefinition>();
                init(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            if (!library.Enemies.Contains(asset)) library.Enemies.Add(asset);
            return asset;
        }
    }
}
