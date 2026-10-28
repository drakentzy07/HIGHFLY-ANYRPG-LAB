using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Editor
{
    // Unlike A6, the FULL build starts in A Lost Soul Character Demo and includes
    // all published world scenes from the 2026 Lost Soul add-on.
    public static class HighflyFullWorldBuild
    {
        private const string Root="Assets/AnyRPG/Addons/a-lost-soul-demo-games/";
        private const string CharacterDemo=Root+"Games/ALostSoulCharacterDemo/Scenes/Game/ALostSoulCharacterDemoGame/ALostSoulCharacterDemoGame.unity";
        private const string StoryDemo=Root+"Games/ALostSoulStoryDemo/Scenes/Game/ALostSoulStoryDemoGame/ALostSoulStoryDemoGame.unity";
        private const string MainMenu=Root+"Content/ALostSoulDemoShared/Scenes/ALostSoulDemoMainMenu/ALostSoulDemoMainMenu.unity";
        public static void BuildWebGL()
        {
            string fantasy="Assets/AnyRPG/Addons/anyrpg-cc0-fantasy-content-pack";
            if (!Directory.Exists(fantasy) || Directory.GetFiles(fantasy,"*.prefab",SearchOption.AllDirectories).Length<10)
                throw new Exception("[HIGHFLY FULL] CC0 Fantasy pack NOT installed — cannot falsely build A6 Features Demo as FULL.");
            if (!Directory.Exists("Assets/UMA/Core"))
                throw new Exception("[HIGHFLY FULL] UMA2 core is missing.");
            if (!File.Exists(CharacterDemo)||!File.Exists(StoryDemo)||!File.Exists(MainMenu))
                throw new Exception("[HIGHFLY FULL] Full-world Character/Story/Menu scenes missing.");
            AnyRpgA6MobileBuild.EnsureTmpEssentialResources();
            var scenes=new List<string>{CharacterDemo,MainMenu,StoryDemo};
            string worlds=Root+"Content/ALostSoulDemoShared/Scenes";
            foreach(var file in Directory.GetFiles(worlds,"*.unity",SearchOption.AllDirectories))
            {
                string normalized=file.Replace('\\','/');
                if (!scenes.Contains(normalized)) scenes.Add(normalized);
            }
            if (scenes.Count<12)
                throw new Exception("[HIGHFLY FULL] World scene count unexpectedly low: "+scenes.Count);
            // GitHub Pages cannot set Content-Encoding: gzip on static Unity payloads.
            // Keep A7 FULL compressed, but let Unity's loader decompress .unityweb.
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.template="PROJECT:HighflyA6Touch";
            PlayerSettings.defaultWebScreenWidth=2316;
            PlayerSettings.defaultWebScreenHeight=1080;
            PlayerSettings.productName="HIGHFLY FULL WORLD - A Lost Soul + FURY";
            const string output="build/WebGL/HIGHFLY-FULL-WORLD";
            Directory.CreateDirectory(output);
            Debug.Log("[HIGHFLY FULL] Native scenes: "+string.Join(" | ",scenes));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=scenes.ToArray(),
                locationPathName=output,
                target=BuildTarget.WebGL,
                options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new Exception("[HIGHFLY FULL] Unity WebGL failed: "+report.summary.result);
            Debug.Log("[HIGHFLY FULL] SUCCESS "+output);
        }
    }
}
