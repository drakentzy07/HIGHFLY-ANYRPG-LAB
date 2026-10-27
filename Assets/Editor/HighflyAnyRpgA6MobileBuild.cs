using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Editor
{
    // A6 builds Features Demo with mobile WebGL template and HIGHFLY native Unity touch HUD; A1/A2 remain unchanged.
    public static class AnyRpgA6MobileBuild
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
                    throw new FileNotFoundException("A6 demo scene missing", scene);

            EnsureTmpEssentialResources();

            const string mobileTemplate = "Assets/WebGLTemplates/HighflyA6Touch/index.html";
            if (!File.Exists(mobileTemplate))
                throw new FileNotFoundException("A6 mobile WebGL template missing", mobileTemplate);

            PlayerSettings.WebGL.template = "PROJECT:HighflyA6Touch";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.productName = "HIGHFLY - AnyRPG A6 Native Touch Lab";

            const string output = "build/WebGL/HIGHFLY-ANYRPG-A6";
            Directory.CreateDirectory(output);
            Debug.Log("[HIGHFLY A6] Building AnyRPG Features Demo + TMP shader resources + A6 native mobile HUD + mobile WebGL template.");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = DemoScenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[HIGHFLY A6] Build failed: " + report.summary.result);

            Debug.Log("[HIGHFLY A6] SUCCESS: " + output);
        }

        private static void EnsureTmpEssentialResources()
        {
            // Many pre-existing AnyRPG font materials point to TMP's standard SDF shader GUID.
            // AssetDatabase.ImportPackage(unitypackage, false) is asynchronous in batchmode
            // and can leave the asset database unchanged; unpack the Unity tarball directly.
            const string sdfGuid = "68e6db2ebdc24f95958faec2be5558d6";
            // Library is restored by CI caching, but Assets/TextMesh Pro is gitignored.
            // An imported GUID can survive in AssetDatabase after its source file is gone.
            // Check BOTH the real shader file and its .meta; do not trust the GUID alone.
            string cachedPath = AssetDatabase.GUIDToAssetPath(sdfGuid);
            bool needsRestore = string.IsNullOrEmpty(cachedPath)
                || !File.Exists(cachedPath) || !File.Exists(cachedPath + ".meta");
            if (needsRestore)
            {
                Debug.LogWarning("[HIGHFLY A6] TMP shader file missing despite cached GUID; restoring package resources.");
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
                    throw new FileNotFoundException("[HIGHFLY A6] TMP Essential Resources unitypackage not found.");

                string temp = Path.Combine(Path.GetTempPath(), "highfly-tmp-essential-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temp);
                try
                {
                    // .unitypackage = gzip tar: each guid directory contains pathname, asset, asset.meta.
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/usr/bin/tar",
                        Arguments = "-xzf \"" + pkg + "\" -C \"" + temp + "\"",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var process = System.Diagnostics.Process.Start(psi))
                    {
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new Exception("[HIGHFLY A6] tar TMP essentials extraction failed: " + error);
                    }
                    int count = 0;
                    foreach (string pathname in Directory.GetFiles(temp, "pathname", SearchOption.AllDirectories))
                    {
                        string path = File.ReadAllText(pathname).Trim().Replace('\\', '/');
                        // Never write outside project-owned TMP assets.
                        if (!path.StartsWith("Assets/TextMesh Pro/", StringComparison.Ordinal)) continue;
                        string payload = Path.Combine(Path.GetDirectoryName(pathname), "asset");
                        string metadata = Path.Combine(Path.GetDirectoryName(pathname), "asset.meta");
                        if (!File.Exists(payload)) continue;
                        string output = Path.GetFullPath(path);
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                        if (!File.Exists(output)) File.Copy(payload, output);
                        if (File.Exists(metadata) && !File.Exists(output + ".meta"))
                            File.Copy(metadata, output + ".meta");
                        count++;
                    }
                    Debug.Log("[HIGHFLY A6] Extracted TMP essential resources entries: " + count);
                    if (count < 1)
                        throw new Exception("[HIGHFLY A6] Unity TMP essentials tar did not contain Assets/TextMesh Pro entries.");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                finally
                {
                    if (Directory.Exists(temp)) Directory.Delete(temp, true);
                }
            }

            string shaderPath = AssetDatabase.GUIDToAssetPath(sdfGuid);
            if (string.IsNullOrEmpty(shaderPath))
                throw new Exception("[HIGHFLY A6] TMP SDF shader GUID missing even after extracting unitypackage.");
            // GitHub Actions headless Unity 6 can return null for a shader after
            // GUID resolution even when the .shader and .meta have been imported.
            // Reimport synchronously before checking; let BuildPipeline itself verify
            // platform compilation instead of treating a transient Editor null as fatal.
            if (!File.Exists(shaderPath) || !File.Exists(shaderPath + ".meta"))
                throw new FileNotFoundException("[HIGHFLY A6] TMP SDF shader source or meta file is missing: " + shaderPath);
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                Debug.LogWarning("[HIGHFLY A6] TMP SDF source/meta present but headless LoadAssetAtPath returned null; continuing to WebGL shader compilation: " + shaderPath);
            }
            else
            {
                Debug.Log("[HIGHFLY A6] TMP SDF shader imported: " + shader.name + " (" + shaderPath + ")");
            }
        }
    }
}
