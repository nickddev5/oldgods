using System;
using System.Collections.Generic;
using OldGods.Runtime;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>What goes into each generated scene. Scenes stay thin: the runtime builds the world.</summary>
    public static class SceneRecipes
    {
        public const string MenuScene = ProjectBuilder.ScenesDir + "/Menu.unity";

        public static IEnumerable<(string path, Action setup)> All(GameAssets assets)
        {
            yield return (MenuScene, () =>
            {
                var go = new GameObject("Menu");
                go.AddComponent<MenuController>().Assets = assets;
            });
            yield return (ProjectBuilder.RunScene, () =>
            {
                var go = new GameObject("Run");
                var run = go.AddComponent<RunController>();
                run.Assets = assets;
            });
        }
    }
}
