using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.V3.Editor
{
    public static class WhiteboxV3Build
    {
        public const string ScenePath="Assets/Scenes/WhiteboxV3.unity";
        [MenuItem("Tools/Whitebox V3/Open Five Day Demo")]
        public static void Open()
        {
            if(!File.Exists(ScenePath))CreateScene();else EditorSceneManager.OpenScene(ScenePath);
        }
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Resources/WhiteboxV3");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var systems=new GameObject("Systems");var player=new GameObject("Player");new GameObject("Environment");new GameObject("Gameplay");new GameObject("Lighting");
            var camera=new GameObject("Camera_Player").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(player.transform,false);
            camera.gameObject.AddComponent<AudioListener>();camera.nearClipPlane=.035f;camera.farClipPlane=140;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.7f,.76f,.75f);
            var controller=new GameObject("Controller_WhiteboxV3");controller.transform.SetParent(systems.transform,false);controller.AddComponent<WBMain>();
            string materialPath="Assets/Resources/WhiteboxV3/WhiteboxMaterial.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,materialPath);}
            const string flatPath="Assets/Resources/WhiteboxV3/FlatColor.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(flatPath)==null)AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Color")),flatPath);
            const string overlayPath="Assets/Resources/WhiteboxV3/WhiteOverlay.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(overlayPath)==null)AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")),overlayPath);
            var blur=Shader.Find("Hidden/MemoryWhitebox/V3/PaintingDefocus");
            if(blur==null)throw new InvalidOperationException("V3 painting defocus shader is missing");
            const string blurPath="Assets/Resources/WhiteboxV3/PaintingDefocus.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(blurPath)==null)AssetDatabase.CreateAsset(new Material(blur),blurPath);
            var resources=new GameObject("Resources_RuntimeGeometry");resources.transform.SetParent(systems.transform,false);
            foreach(PrimitiveType type in new[]{PrimitiveType.Cube,PrimitiveType.Sphere,PrimitiveType.Cylinder,PrimitiveType.Capsule,PrimitiveType.Quad,PrimitiveType.Plane})
            {var g=GameObject.CreatePrimitive(type);g.name="Mesh_"+type;g.transform.SetParent(resources.transform,false);g.GetComponent<Renderer>().sharedMaterial=material;}
            var font=new GameObject("Font_DeviceDigits").AddComponent<TextMesh>();font.transform.SetParent(resources.transform,false);font.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");font.text="100";
            resources.SetActive(false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.49f,.5f,.46f);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            // Build settings for the existing HouseScene/ParkScene remain unchanged. This entry builds its explicit scene.
        }
        [MenuItem("Tools/Whitebox V3/Validate Rules and Scene")]
        public static void Validate()
        {
            if(!File.Exists(ScenePath))CreateScene();
            bool qa=WBMain.IsQA;WBMain.IsQA=true;
            try{WhiteboxV3Validation.Run(null,"QA/WhiteboxV3");}
            finally{WBMain.IsQA=qa;}
            Debug.Log("WHITEBOX_V3_SOURCE_PASS");
        }
        [MenuItem("Tools/Whitebox V3/Build Windows Demo")]
        public static void ValidateAndBuild()
        {
            CreateScene();Validate();Directory.CreateDirectory("Build/WhiteboxV3");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Build/WhiteboxV3/MemoryWhiteboxV3.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Whitebox V3 build failed: "+report.summary.result);
            Debug.Log("WHITEBOX_V3_BUILD_PASS "+report.summary.totalSize+" bytes");
        }
    }
}
