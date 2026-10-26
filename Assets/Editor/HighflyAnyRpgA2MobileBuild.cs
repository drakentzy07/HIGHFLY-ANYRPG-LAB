using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Editor
{
    // A2 is an additive mobile WebGL build; the verified original A1 build remains unchanged.
    public static class AnyRpgA2MobileBuild
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
                if (!File.Exists(scene))
                    throw new FileNotFoundException("A2 demo scene missing", scene);

            EnsureTmpEssentialResources();

            const string mobileTemplate = "Assets/WebGLTemplates/HighflyMobile/index.html";
            if (!File.Exists(mobileTemplate))
                throw new FileNotFoundException("A2 mobile WebGL template missing", mobileTemplate);

            PlayerSettings.WebGL.template = "PROJECT:HighflyMobile";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.productName = "HIGHFLY - AnyRPG A2 Mobile Lab";

            const string output = "build/WebGL/HIGHFLY-ANYRPG-A2";
            Directory.CreateDirectory(output);
            Debug.Log("[HIGHFLY A2] Building AnyRPG Features Demo + TMP shader resources + mobile WebGL wrapper.");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DemoScenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[HIGHFLY A2] Build failed: " + report.summary.result);

            Debug.Log("[HIGHFLY A2] SUCCESS: " + output);
        }

        private static void EnsureTmpEssentialResources()
        {
            // Existing AnyRPG font materials reference the historical TMP SDF shader GUID.
            // Without importing Essential Resources, WebGL renders their quads magenta.
            const string sdfGuid = "68e6db2ebdc24f95958faec2be5558d6";
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(sdfGuid)))
            {
                string cache = Path.GetFullPath("Library/PackageCache");
                string pkg = null;
                if (Directory.Exists(cache))
                {
                    foreach (string dir in Directory.GetDirectories(cache, "com.unity.ugui*"))
                    {
                        string candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                        if (File.Exists(candidate)) { pkg = candidate; break; }
                    }
                }

                if (string.IsNullOrEmpty(pkg))
                    throw new FileNotFoundException("[HIGHFLY A2] TMP Essential Resources package not found inside Unity UGUI cache.");

                Debug.Log("[HIGHFLY A2] Importing TMP Essential Resources from: " + pkg);
                AssetDatabase.ImportPackage(pkg, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            string shaderPath = AssetDatabase.GUIDToAssetPath(sdfGuid);
            if (string.IsNullOrEmpty(shaderPath))
                throw new Exception("[HIGHFLY A2] TMP SDF shader GUID is still missing after essentials import.");

            Shader sdf = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (sdf == null)
                throw new Exception("[HIGHFLY A2] TMP SDF shader file not importable: " + shaderPath);
            Debug.Log("[HIGHFLY A2] TMP shader resolved: " + sdf.name + " at " + shaderPath);
        }
    }
}
