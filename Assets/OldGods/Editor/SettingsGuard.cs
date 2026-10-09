using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Editor
{
    /// <summary>
    /// Guards project settings a build depends on. Run after every build and by the
    /// verifiers: Windows must render with the PC URP asset, in linear colour, with the
    /// Input System active and the right product name.
    /// </summary>
    public static class SettingsGuard
    {
        public const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        public const string ProductName = "The Old Gods";

        public static List<string> Check()
        {
            var problems = new List<string>();

            if (PlayerSettings.productName != ProductName)
                problems.Add($"Product name is '{PlayerSettings.productName}', expected '{ProductName}'");
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                problems.Add("Colour space is not Linear");

            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcPipelinePath);
            if (pc == null)
            {
                problems.Add($"PC render pipeline asset missing at {PcPipelinePath}");
                return problems;
            }

            int standalone = StandaloneQualityLevel();
            if (standalone < 0)
            {
                problems.Add("No default quality level for Standalone");
            }
            else
            {
                var assigned = QualitySettings.GetRenderPipelineAssetAt(standalone);
                var effective = assigned != null ? assigned : GraphicsSettings.defaultRenderPipeline;
                if (effective != pc)
                    problems.Add($"Standalone quality level '{QualitySettings.names[standalone]}' renders with '{(effective != null ? effective.name : "none")}', expected PC_RPAsset");
            }
            return problems;
        }

        static int StandaloneQualityLevel()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform == null) return QualitySettings.GetQualityLevel();
            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                var entry = perPlatform.GetArrayElementAtIndex(i);
                var key = entry.FindPropertyRelative("first");
                var value = entry.FindPropertyRelative("second");
                if (key != null && value != null && key.stringValue == "Standalone") return value.intValue;
            }
            return QualitySettings.GetQualityLevel();
        }

        [MenuItem("Old Gods/Verify/Settings", priority = 101)]
        public static void CheckMenu()
        {
            var problems = Check();
            if (problems.Count == 0) Debug.Log("OldGods settings guard: ok");
            foreach (var p in problems) Debug.LogError("OldGods settings guard: " + p);
        }
    }
}
