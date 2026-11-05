using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class WardSoftRevealAuthoring
    {
        const string Folder="Assets/Art/Environments/WardPreview";
        const string ScenePath=Folder+"/WardSoftRevealPreview.unity";
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../output/WardRevealPreview"));

        [MenuItem("Memory Whitebox/Winter/Ward Soft Reveal/Open Preview")]
        public static void OpenPreview()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before opening the preview.");
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);
        }

        // For isolated authoring/batch validation. Never overwrites the shared gameplay scenes.
        public static void AuthorAndValidate()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Run validation outside Play Mode.");
            Directory.CreateDirectory(Output);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/WardConcept.png");
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WardConcept.png");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Folder+"/WardSoftReveal.shader");
            Require(shader!=null && shader.isSupported,"Shader missing or unsupported");
            foreach(var message in ShaderUtil.GetShaderMessages(shader))
                Require(message.severity!=UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,message.message);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/WardSoftReveal.mat");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Folder+"/WardSoftReveal.mat");}
            material.mainTexture=texture;
            material.SetFloat("_ImageAspect",(float)texture.width/texture.height);
            material.SetFloat("_ViewportAspect",(float)texture.width/texture.height);
            material.SetFloat("_Progress",0);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            if(!File.Exists(ScenePath))
            {
            var active=SceneManager.GetActiveScene();
            bool emptyBatch=Application.isBatchMode && SceneManager.sceneCount==1 && string.IsNullOrEmpty(active.path) && !active.isDirty;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,emptyBatch?NewSceneMode.Single:NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera=new GameObject("Ward soft reveal preview").AddComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=material.GetColor("_WhiteColor");
                camera.cullingMask=0;
                camera.allowHDR=false;
                camera.allowMSAA=false;
                var preview=camera.gameObject.AddComponent<WardSoftRevealPreview>();
                var so=new SerializedObject(preview);
                so.FindProperty("revealMaterial").objectReferenceValue=material;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally
            {
                if(!emptyBatch)EditorSceneManager.CloseScene(scene,true);
                if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
            }
            }
            ValidateAndRender(material);
            Debug.Log("WARD_SOFT_REVEAL_PASS " + Output);
        }

        static void Require(bool ok,string reason)
        {if(!ok)throw new InvalidOperationException(reason);}

        static Texture2D Render(Material mat,float progress,int width=1280,int height=720)
        {
            var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            try
            {
                mat.SetFloat("_Progress",progress);
                mat.SetFloat("_ViewportAspect",(float)width/height);
                Graphics.Blit(mat.mainTexture,rt,mat);
                RenderTexture.active=rt;
                var image=new Texture2D(width,height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,width,height),0,0);
                image.Apply();
                return image;
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        static float Delta(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        static Color Pixel(Texture2D image,float u,float v)=>image.GetPixel((int)(u*(image.width-1)),(int)(v*(image.height-1)));

        static void ValidateAndRender(Material source)
        {
            var mat=new Material(source);
            var images=new System.Collections.Generic.List<Texture2D>();
            var report=new System.Text.StringBuilder();
            try
            {
                float[] stages={0,.2f,.4f,.6f,.8f,1};
                for(int i=0;i<stages.Length;i++)
                {
                    var image=Render(mat,stages[i]);images.Add(image);
                    File.WriteAllBytes(Path.Combine(Output,"stage-"+i+".png"),image.EncodeToPNG());
                }
                var white=Pixel(images[0],.5f,.9f);
                Require(white.r>.9f && white.r>white.b,"Start is not warm white");
                for(int y=190;y<720;y+=13)
                    for(int x=0;x<1280;x+=13)
                        Require(Delta(images[0].GetPixel(x,y),white)<.015f,"Hospital leaks at progress zero");
                report.AppendLine("PASS warm white start; no hospital pixels above foreground");
                Require(Delta(Pixel(images[0],.2f,.1f),white)>.1f,"Foreground is hidden");
                for(int i=1;i<images.Count;i++)
                    Require(Delta(Pixel(images[i],.2f,.1f),Pixel(images[0],.2f,.1f))<.015f,"Paper changes during reveal");
                report.AppendLine("PASS foreground preserved");
                foreach(var image in images)
                    for(float v=.695f;v<=.72f;v+=.004f)
                        for(float u=.477f;u<=.516f;u+=.004f)
                            Require(Delta(Pixel(image,u,v),white)<.015f,"Eye region leaked");
                report.AppendLine("PASS both eye regions remain opaque white across six stages");
                Require(Delta(Pixel(images[3],.7f,.32f),white)>.05f,"Lower room fails to reveal");
                Require(Delta(Pixel(images[3],.7f,.86f),white)<.015f,"Upper room reveals before lower room");
                report.AppendLine("PASS lower room visible before upper room");
                for(int i=1;i<images.Count;i++)
                    for(int y=170;y<720;y+=11)
                        for(int x=0;x<1280;x+=11)
                            Require(Delta(images[i].GetPixel(x,y),white)+.025f>=Delta(images[i-1].GetPixel(x,y),white),"Revealed pixel disappears");
                report.AppendLine("PASS cumulative reveal is monotonic");
                var again=Render(mat,.6f);images.Add(again);
                var original=images[3].GetPixels32();var repeat=again.GetPixels32();
                for(int i=0;i<original.Length;i++)Require(original[i].Equals(repeat[i]),"Fixed noise changed between identical renders");
                report.AppendLine("PASS repeated progress produces identical GPU pixels");
                var wide=Render(mat,1,1600,720);images.Add(wide);
                var narrow=Render(mat,1,960,720);images.Add(narrow);
                File.WriteAllBytes(Path.Combine(Output,"wide-20x9.png"),wide.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(Output,"standard-4x3.png"),narrow.EncodeToPNG());
                Require(Delta(Pixel(wide,.01f,.5f),white)<.015f,"Wide viewport lacks pillarbox");
                Require(Delta(Pixel(narrow,.5f,.98f),white)<.015f,"Narrow viewport lacks letterbox");
                report.AppendLine("PASS aspect fit at 16:9, 20:9 and 4:3");

                string frames=Path.Combine(Output,"frames");Directory.CreateDirectory(frames);
                for(int frame=0;frame<450;frame++)
                {
                    float time=frame/30f;
                    var image=Render(mat,Mathf.Clamp01((time-1)/12));
                    for(float v=.695f;v<=.72f;v+=.004f)
                        for(float u=.477f;u<=.516f;u+=.004f)
                            Require(Delta(Pixel(image,u,v),white)<.015f,"Eye leaked during animation at frame "+frame);
                    File.WriteAllBytes(Path.Combine(frames,"frame-"+frame.ToString("D4")+".png"),image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                report.AppendLine("PASS eye protection across all 450 animation frames");
                report.AppendLine("450 actual Unity GPU frames; 30fps; 1s hold + 12s reveal + 2s hold");
                report.AppendLine("GPU: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType);
                File.WriteAllText(Path.Combine(Output,"validation.txt"),report.ToString());
            }
            finally
            {
                foreach(var image in images)UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }
    }
}
