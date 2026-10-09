using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>
    /// Windows player build. Batch entry point for Tools/build_windows.py:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod OldGods.Editor.Build.Windows [-buildOut path]
    /// Exits 0 on success, 1 on a failed build or a settings-guard problem.
    /// </summary>
    public static class Build
    {
        public const string DefaultOutput = "Builds/Windows/TheOldGods.exe";

        [MenuItem("Old Gods/Build/Windows Player", priority = 40)]
        public static void WindowsMenu() => Run(DefaultOutput, development: false);

        public static void Windows()
        {
            string output = Arg("-buildOut") ?? DefaultOutput;
            bool dev = Environment.GetCommandLineArgs().Contains("-development");
            int code = Run(output, dev) ? 0 : 1;
            EditorApplication.Exit(code);
        }

        public static bool Run(string output, bool development)
        {
            var before = SettingsGuard.Check();
            foreach (var p in before) Debug.LogError("OldGods build: settings guard: " + p);
            if (before.Count > 0) return false;

            ProjectBuilder.BuildEverything();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("OldGods build: no scenes in build settings");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"OldGods build: {report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");

            var after = SettingsGuard.Check();
            foreach (var p in after) Debug.LogError("OldGods build: settings guard after build: " + p);
            return report.summary.result == BuildResult.Succeeded && after.Count == 0;
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
