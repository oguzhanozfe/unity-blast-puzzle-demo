using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OguzhanOzdemir.BlastPuzzle.Editor
{
    public static class DemoProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/BlastPuzzleDemo.unity";

        [MenuItem("Demo/Create clean scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Oguzhan Ozdemir";
            PlayerSettings.productName = "Blast Puzzle Demo";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "dev.oguzhanozdemir.blastpuzzle");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Demo/Build macOS")]
        public static void BuildMac()
        {
            CreateScene();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Argument("-buildOutput") ?? Path.Combine(root, "Builds", "macOS", "BlastPuzzleDemo.app");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed with " + report.summary.totalErrors + " errors.");
            Debug.Log("Demo build succeeded: " + output);
        }

        private static string Argument(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
                if (arguments[i] == key) return arguments[i + 1];
            return null;
        }
    }
}

