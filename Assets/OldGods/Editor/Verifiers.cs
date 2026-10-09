using System.Collections.Generic;
using OldGods.Runtime;
using UnityEditor;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>Checks that generated assets, content and settings are in a shippable state.</summary>
    public static class Verifiers
    {
        [MenuItem("Old Gods/Verify/All", priority = 100)]
        public static void VerifyAllMenu()
        {
            var problems = VerifyAll();
            if (problems.Count == 0) Debug.Log("OldGods verify: all checks passed");
            foreach (var p in problems) Debug.LogError("OldGods verify: " + p);
        }

        public static List<string> VerifyAll()
        {
            var problems = new List<string>();
            problems.AddRange(VerifyContent());
            problems.AddRange(SettingsGuard.Check());
            return problems;
        }

        public static List<string> VerifyContent()
        {
            var problems = new List<string>();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(ProjectBuilder.GameAssetsPath);
            if (assets == null)
            {
                problems.Add("GameAssets is missing; run Old Gods > Build > Everything");
                return problems;
            }
            if (assets.LowPoly == null) problems.Add("GameAssets.LowPoly material is not set");
            if (assets.Horde == null) problems.Add("GameAssets.Horde material is not set");
            if (assets.Content == null)
            {
                problems.Add("GameAssets.Content is not set");
                return problems;
            }
            try
            {
                var set = assets.Content.Load();
                problems.AddRange(set.Validate());
                if (set.Enemies.Count == 0) problems.Add("No enemies in the content library");
            }
            catch (System.Exception e)
            {
                problems.Add("Content failed to load: " + e.Message);
            }
            return problems;
        }
    }
}
