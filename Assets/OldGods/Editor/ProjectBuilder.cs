using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldGods.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OldGods.Editor
{
    /// <summary>
    /// Generates the project's assets and scenes from code, so a fresh clone can rebuild
    /// them. Safe to run again: existing assets are updated in place, keeping their GUIDs.
    /// Scenes are built additively and closed, so the scenes Nick has open are untouched.
    /// </summary>
    public static class ProjectBuilder
    {
        public const string Root = "Assets/OldGods";
        public const string MaterialsDir = Root + "/Content/Materials";
        public const string EnemiesDir = Root + "/Content/Enemies";
        public const string ScenesDir = Root + "/Scenes";
        public const string GameAssetsPath = Root + "/Resources/GameAssets.asset";
        public const string ContentPath = Root + "/Content/ContentLibrary.asset";
        public const string RunScene = ScenesDir + "/Run.unity";

        [MenuItem("Old Gods/Build/Everything", priority = 0)]
        public static void BuildEverything()
        {
            ImportTmpEssentials();
            var assets = BuildAssets();
            BuildScenes(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("OldGods: build everything done");
        }

        /// <summary>Batch-mode entry point: Unity.exe -batchmode -executeMethod OldGods.Editor.ProjectBuilder.BatchBuildEverything -quit</summary>
        public static void BatchBuildEverything()
        {
            BuildEverything();
            var problems = Verifiers.VerifyAll();
            if (problems.Count > 0)
            {
                foreach (var p in problems) Debug.LogError("OldGods verify: " + p);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Old Gods/Build/Import TMP Essentials", priority = 20)]
        public static void ImportTmpEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(package))
            {
                Debug.LogError("OldGods: TMP Essential Resources package not found at " + package);
                return;
            }
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static Material Mat(string name, string shader, Color color, Color emission = default)
        {
            EnsureFolder(MaterialsDir);
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) throw new System.Exception("Shader not found: " + shader);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = sh;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_EmissionColor", emission);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        [MenuItem("Old Gods/Build/Assets", priority = 1)]
        public static GameAssets BuildAssets()
        {
            var lowPoly = Mat("LowPoly", "OldGods/LowPoly", Color.white);
            var horde = Mat("Horde", "OldGods/HordeInstanced", Color.white);
            horde.SetFloat("_WalkSwing", 0.22f);
            horde.SetFloat("_OutlineWidth", 2.5f);
            // Coarse-texel noise on ground, stone and props; characters turn it off with their outline.
            lowPoly.SetFloat("_SurfaceNoise", 0.16f);
            var glow = Mat("Glow", "OldGods/LowPoly", Color.white, new Color(1.2f, 1.1f, 0.8f));
            var unlit = Mat("UnlitGlow", "OldGods/UnlitGlow", Color.white);
            var fade = Mat("UnlitFade", "OldGods/UnlitGlow", Color.white);
            fade.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fade.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fade.SetFloat("_ZWrite", 0f);
            fade.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            fade.SetOverrideTag("RenderType", "Transparent");
            fade.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var sky = Mat("Sky", "OldGods/Sky", Color.white);

            var content = LoadOrCreate<ContentLibrary>(ContentPath);
            ContentAuthoring.Populate(content);
            EditorUtility.SetDirty(content);

            var assets = LoadOrCreate<GameAssets>(GameAssetsPath);
            assets.LowPoly = lowPoly;
            assets.Horde = horde;
            assets.Glow = glow;
            assets.UnlitGlow = unlit;
            assets.UnlitFade = fade;
            assets.Sky = sky;
            assets.Content = content;
            if (assets.GreyboxBiome == null) assets.GreyboxBiome = ContentAuthoring.GreyboxBiome();
            ContentAuthoring.Biomes(assets, content);
            ContentAuthoring.Migrate(assets, content);
            if (assets.Story == null) assets.Story = StoryAuthoring.Create();
            EditorUtility.SetDirty(content);
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            return assets;
        }

        [MenuItem("Old Gods/Build/Scenes", priority = 2)]
        public static void BuildScenesMenu() => BuildScenes(BuildAssets());

        public static void BuildScenes(GameAssets assets)
        {
            EnsureFolder(ScenesDir);
            var scenes = new List<string>();
            foreach (var (path, setup) in SceneRecipes.All(assets))
            {
                BuildScene(path, setup);
                scenes.Add(path);
            }
            EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        }

        /// <summary>Creates or replaces one scene without touching the scenes that are open.</summary>
        static void BuildScene(string path, System.Action setup)
        {
            // Batch mode starts with one untitled, unchanged scene; replacing it loses nothing.
            bool onlyUntitled = true, anyDirty = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(s.path)) onlyUntitled = false;
                if (s.isDirty) anyDirty = true;
            }
            if (onlyUntitled && !anyDirty)
            {
                var single = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                setup();
                EditorSceneManager.SaveScene(single, path);
                return;
            }
            if (onlyUntitled)
                throw new System.InvalidOperationException("OldGods: save or discard the untitled scene first; the builder never touches open scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            setup();
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
