using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Editor
{
    /// <summary>
    /// Isolated build entry point for the unmodified AnyRPG Features Demo Game.
    /// This does not replace AnyRPG gameplay, controllers, or project settings.
    /// </summary>
    public static class AnyRpgA1Build
    {
        private static readonly string[] DemoScenes =
        {
            "Assets/AnyRPG/Core/Games/FeaturesDemoGame/Scenes/Game/FeaturesDemoGame/FeaturesDemoGame.unity",
            "Assets/AnyRPG/Core/Games/FeaturesDemoGame/Scenes/Game/FeaturesDemoMainMenu/FeaturesDemoMainMenu.unity",
            "Assets/AnyRPG/Core/Games/FeaturesDemoGame/Scenes/Content/FeaturesDemoZone/FeaturesDemoZone.unity",
            "Assets/AnyRPG/Core/Games/FeaturesDemoGame/Scenes/Content/FeaturesDemoDungeon/FeaturesDemoDungeon.unity",
            "Assets/AnyRPG/Core/Games/FeaturesDemoGame/Scenes/Content/FeaturesDemoCutsceneZone/FeaturesDemoCutsceneZone.unity"
        };

        public static void BuildWebGL()
        {
            foreach (string scene in DemoScenes)
            {
                if (!File.Exists(scene))
                {
                    throw new FileNotFoundException("AnyRPG demo scene missing", scene);
                }
            }

            const string outputDirectory = "build/WebGL/HIGHFLY-ANYRPG-A1";
            Directory.CreateDirectory(outputDirectory);

            Debug.Log("[HIGHFLY A1] Building untouched AnyRPG Features Demo for WebGL.");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DemoScenes,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception("[HIGHFLY A1] WebGL build failed: " + report.summary.result);
            }

            Debug.Log("[HIGHFLY A1] WebGL build succeeded: " + outputDirectory);
        }
    }
}
