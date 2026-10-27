using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MemoryWhitebox.Editor
{
    public static class GameBuild
    {
        public static readonly string[] Scenes={ParkFilmRoute.HouseScenePath,ParkFilmRoute.ParkScenePath};
        [MenuItem("Tools/Build/Build Windows Player")]
        public static void ValidateAndBuild()
        {
            foreach(var path in Scenes)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)throw new InvalidOperationException("Missing scene: "+path);
            Directory.CreateDirectory("Build");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Scenes,locationPathName="Build/MemoryGame.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Game build failed: "+report.summary.result);
            UnityEngine.Debug.Log("GAME_BUILD_PASS "+report.summary.totalSize+" bytes");
        }
    }
}
