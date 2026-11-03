using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Operates on the saved V17 scenes. It does not regenerate geometry or rebake GI.
public static class BedroomV17Tools
{
    const string Root = "Assets/Scenes/Bedroom/V17/Bedroom_";
    static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };
    static string[] Paths => Seasons.Select(s => Root + s + ".unity").ToArray();

    [MenuItem("Tools/Bedroom V17/Open Spring")]
    public static void OpenSpring()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(Paths[0]);
    }

    [MenuItem("Tools/Bedroom V17/Validate Saved Scenes")]
    public static void ValidateScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in Paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int renderers = 0;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new InvalidOperationException(path + ": missing script on " + t.name);
                    var renderer = t.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderers++;
                        if (renderer.sharedMaterials.Any(m => m == null))
                            throw new InvalidOperationException(path + ": missing material on " + t.name);
                    }
                    var mesh = t.GetComponent<MeshFilter>();
                    if (mesh != null && mesh.sharedMesh == null)
                        throw new InvalidOperationException(path + ": missing mesh on " + t.name);
                }
                if (Camera.main == null || UnityEngine.Object.FindObjectOfType<BedroomWalk>() == null)
                    throw new InvalidOperationException(path + ": missing first-person camera or controller");
                if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightmaps.Any(l => l.lightmapColor == null))
                    throw new InvalidOperationException(path + ": missing baked lightmaps");
                Debug.Log("BEDROOM_V17_VALIDATED " + path + ": renderers=" + renderers
                    + ", lightmaps=" + LightmapSettings.lightmaps.Length + ", colorSpace=" + PlayerSettings.colorSpace);
            }
        }
        finally
        {
            if (previous.Length == 0 || previous.All(s => string.IsNullOrEmpty(s.path)))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }

    [MenuItem("Tools/Bedroom V17/Build Windows Viewer (Current Project Settings)")]
    public static void BuildWindowsViewer()
    {
        if (Application.isBatchMode)
            throw new InvalidOperationException("This build menu requires an interactive output-folder selection; use ValidateScenes for batch validation.");
        if (PlayerSettings.colorSpace != ColorSpace.Linear && !EditorUtility.DisplayDialog("Bedroom V17",
            "This project uses Gamma. The V17 reference viewer uses Linear, so the appearance will differ. Build with the current project settings?", "Build Gamma Viewer", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string directory = EditorUtility.SaveFolderPanel("Build Bedroom V17 viewer", "Builds", "BedroomV17");
        if (string.IsNullOrEmpty(directory)) return;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Paths,
            locationPathName = System.IO.Path.Combine(directory, "SeasonalBedroom.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Bedroom viewer build failed: " + report.summary.result);
    }
}
