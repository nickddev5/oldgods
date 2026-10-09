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
            problems.AddRange(VerifyStory());
            problems.AddRange(SettingsGuard.Check());
            return problems;
        }

        /// <summary>Every god and stage has a line, every stage has lore, both endings exist.</summary>
        public static List<string> VerifyStory()
        {
            var problems = new List<string>();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(ProjectBuilder.GameAssetsPath);
            if (assets == null) return problems;
            var story = assets.Story;
            if (story == null)
            {
                problems.Add("GameAssets.Story is not set");
                return problems;
            }
            if (string.IsNullOrWhiteSpace(story.Premise)) problems.Add("Story premise is empty");
            if (story.EliasEnding.Count == 0) problems.Add("Story has no Elias ending");
            if (story.RefusedEnding.Count == 0) problems.Add("Story has no refused ending");
            foreach (var g in assets.Content.Gods)
                if (g != null && string.IsNullOrEmpty(story.GodLine(g.Id))) problems.Add($"Story has no line for {g.Id}");
            foreach (var b in assets.Stages)
            {
                if (b == null) continue;
                if (string.IsNullOrEmpty(story.BiomeLine(b.Id))) problems.Add($"Story has no line for {b.Id}");
                if (!story.Fragments.Exists(f => f.BiomeId == b.Id)) problems.Add($"Story has no lore stone for {b.Id}");
            }
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
                var biomes = new List<BiomeDefinition>(assets.Stages);
                if (assets.GreyboxBiome != null) biomes.Add(assets.GreyboxBiome);
                foreach (var b in biomes)
                {
                    if (b == null) { problems.Add("A stage slot has no biome"); continue; }
                    foreach (var p in OldGods.Rules.TimelineEvaluator.Validate(b.Timeline)) problems.Add($"{b.name} timeline: {p}");
                    foreach (var phase in b.Timeline.Phases)
                        foreach (var m in phase.Mix)
                            if (!set.Enemies.ContainsKey(m.EnemyId)) problems.Add($"{b.name}: unknown enemy '{m.EnemyId}'");
                    foreach (var e in b.Timeline.Events)
                        if (!set.Enemies.ContainsKey(e.EnemyId)) problems.Add($"{b.name}: event enemy '{e.EnemyId}' unknown");
                    if (!set.Enemies.ContainsKey(b.Timeline.SwarmEnemyId)) problems.Add($"{b.name}: swarm enemy '{b.Timeline.SwarmEnemyId}' unknown");
                    if (b.Boss == null) problems.Add($"{b.name} has no boss");
                    else if (b.Boss.Attacks.Count == 0) problems.Add($"{b.Boss.name} has no attacks");
                }
            }
            catch (System.Exception e)
            {
                problems.Add("Content failed to load: " + e.Message);
            }
            return problems;
        }
    }
}
