using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    // Isolated GPU render checks. No Play, real film, player saves or authored scene mutations.
    public static class ParkFocusRenderingChecks
    {
        static void Check(bool ok,string message,List<string> lines)
        {lines.Add((ok?"PASS ":"FAIL ")+message);if(!ok)throw new Exception(string.Join("\n",lines));}
        static Texture2D Read(RenderTexture rt)
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();return image;}
            finally{RenderTexture.active=previous;}
        }
        static float Difference(Texture2D a,Texture2D b,RectInt region)
        {
            float total=0;
            for(int y=region.yMin;y<region.yMax;y++)for(int x=region.xMin;x<region.xMax;x++)
            {var delta=a.GetPixel(x,y)-b.GetPixel(x,y);total+=Mathf.Abs(delta.r)+Mathf.Abs(delta.g)+Mathf.Abs(delta.b);}
            return total/(region.width*region.height*3);
        }
        static float SharpEdge(Texture2D image)
        {
            float edge=0;
            for(int y=180;y<360;y+=20)for(int x=120;x<350;x++)
                edge=Mathf.Max(edge,Mathf.Abs(image.GetPixel(x,y).r-image.GetPixel(x+1,y).r));
            return edge;
        }
        [MenuItem("Tools/Remember Seasons/Validation/Focus Rendering")]
        public static void RunMenu(){Debug.Log(Run());}
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Run in Edit mode.");
            var lines=new List<string>();var images=new List<Texture2D>();
            var scene=EditorSceneManager.NewPreviewScene();
            Material surface=null,template=null;Texture2D pattern=null;RenderTexture target=null;ParkPhotoFocus focus=null;
            string directory=Path.GetFullPath("output/QA/FocusRendering_20261106");Directory.CreateDirectory(directory);
            try
            {
                var cameraObject=new GameObject("Focus validation camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;
                camera.fieldOfView=40;camera.aspect=16f/9;camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.allowHDR=false;camera.allowMSAA=false;camera.depthTextureMode=DepthTextureMode.Depth;
                target=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
                var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);SceneManager.MoveGameObjectToScene(plane,scene);
                plane.transform.position=new Vector3(0,0,5);plane.transform.localScale=new Vector3(7,4,1);
                pattern=new Texture2D(256,128,TextureFormat.RGB24,false);pattern.filterMode=FilterMode.Point;
                var pixels=new Color[256*128];
                for(int y=0;y<128;y++)for(int x=0;x<256;x++)pixels[y*256+x]=(x%16<8)?new Color(.8f,.8f,.8f):new Color(.08f,.08f,.08f);
                pattern.SetPixels(pixels);pattern.Apply();
                surface=new Material(Shader.Find("Standard"));surface.color=Color.black;surface.mainTexture=pattern;
                surface.EnableKeyword("_EMISSION");surface.SetColor("_EmissionColor",Color.white);surface.SetTexture("_EmissionMap",pattern);
                plane.GetComponent<Renderer>().sharedMaterial=surface;
                template=new Material(Shader.Find("Hidden/ParkPhoto/ManualFocus"));
                // A nearer surface must also be sharp when the subject is in focus.
                var foreground=GameObject.CreatePrimitive(PrimitiveType.Quad);SceneManager.MoveGameObjectToScene(foreground,scene);
                foreground.transform.position=new Vector3(-1.1f,0,3);foreground.transform.localScale=new Vector3(1.2f,1.5f,1);
                foreground.GetComponent<Renderer>().sharedMaterial=surface;
                focus=cameraObject.AddComponent<ParkPhotoFocus>();focus.template=template;focus.activeFocus=true;focus.aperture=2.8f;focus.distance=5;focus.subjectDepth=5;
                cameraObject.AddComponent<ParkFocusCheckRenderDriver>().focus=focus;
                var hostObject=new GameObject("Inactive exposure host");SceneManager.MoveGameObjectToScene(hostObject,scene);hostObject.SetActive(false);
                var host=hostObject.AddComponent<ParkPhotoPrototype>();host.view=camera;host.focusEffect=focus;
                var exposure=typeof(ParkPhotoPrototype).GetMethod("RenderPhotographicFrame",BindingFlags.Instance|BindingFlags.NonPublic);
                Func<string,Texture2D> snapshot=name=>{var t=Read(target);images.Add(t);File.WriteAllBytes(Path.Combine(directory,name+".png"),t.EncodeToPNG());return t;};
                // The bypass reference must also bypass the shader's linear/gamma round trip.
                // Comparing it with a zero-radius shader pass tests color conversion precision, not blur cleanup.
                focus.activeFocus=false;camera.Render();var native=snapshot("00_NativeScene");
                focus.activeFocus=true;focus.showFocusAid=false;camera.Render();var sharp=snapshot("01_InFocus_NoAid");
                Check(Difference(sharp,native,new RectInt(0,0,960,540))<.004f,"Subject in focus restores the whole image, including nearer geometry",lines);
                focus.distance=2;camera.Render();var blurred=snapshot("02_Defocused_NoAid");
                float sharpEdge=SharpEdge(sharp),softEdge=SharpEdge(blurred);
                lines.Add("Adjacent edge contrast: focused="+sharpEdge.ToString("F4")+", defocused="+softEdge.ToString("F4"));
                Check(sharpEdge>.15f && softEdge<sharpEdge*.8f,"Defocus softens actual GPU edges",lines);
                focus.showFocusAid=true;camera.Render();var split=snapshot("03_Defocused_Viewfinder");
                Check(Difference(split,blurred,new RectInt(445,250,70,40))<.001f,"Legacy aid flag cannot restore a sharp split-image patch",lines);
                Check(Difference(split,blurred,new RectInt(100,100,200,200))<.001f,"Aid leaves outside image unchanged",lines);
                exposure.Invoke(host,null);var film=snapshot("04_ActualCapture_NoAid");
                Check(Difference(film,blurred,new RectInt(0,0,960,540))<.001f,"Real exposure render excludes entire aid but preserves blur",lines);
                Check(!focus.suppressFocusAid && focus.showFocusAid,"Exposure restores viewfinder overlay state",lines);
                camera.Render();var resumed=snapshot("05_ViewfinderRestored");
                Check(Difference(split,resumed,new RectInt(0,0,960,540))<.001f,"Viewfinder returns unchanged after shutter",lines);
                focus.distance=5;camera.Render();var aligned=snapshot("06_InFocus_Viewfinder");
                Check(Difference(aligned,sharp,new RectInt(0,0,960,540))<.003f,"Returning to subject distance restores the complete clear image",lines);
                focus.activeFocus=false;camera.Render();var lowered=snapshot("07_Lowered_NoAid");
                Check(Difference(lowered,native,new RectInt(0,0,960,540))<.001f,"Lowered camera restores native scene without blur or aid",lines);
                focus.activeFocus=true;focus.showFocusAid=false;focus.distance=5;focus.detailBackdrop=true;
                camera.Render();var backdrop=snapshot("08_DialBackground");
                Check(SharpEdge(backdrop)<sharpEdge*.8f,"Dial backdrop blurs even objects at the focus distance",lines);
                exposure.Invoke(host,null);var cleanCapture=snapshot("09_CaptureExcludesDialBlur");
                Check(Difference(cleanCapture,sharp,new RectInt(0,0,960,540))<.001f,"Actual photograph excludes dial background blur",lines);
                focus.detailBackdrop=false;camera.Render();var returned=snapshot("10_DialBlurReleased");
                Check(Difference(returned,sharp,new RectInt(0,0,960,540))<.001f,"Leaving dials removes backdrop blur",lines);
                focus.activeFocus=true;focus.distance=2;focus.suppressFocusAid=true;exposure.Invoke(host,null);
                Check(focus.suppressFocusAid,"Nested capture preserves previous suppression",lines);
                lines.Add(ParkCameraPreviewChecks.ResponseRules());lines.Add(ParkCameraPreviewChecks.ExposureGpu());
                string report=string.Join("\n",lines);File.WriteAllText(Path.Combine(directory,"checks.txt"),report);return report;
            }
            finally
            {
                if(focus!=null)typeof(ParkPhotoFocus).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(focus,null);
                EditorSceneManager.ClosePreviewScene(scene);
                foreach(var t in images)UnityEngine.Object.DestroyImmediate(t);
                if(surface!=null)UnityEngine.Object.DestroyImmediate(surface);if(template!=null)UnityEngine.Object.DestroyImmediate(template);
                if(pattern!=null)UnityEngine.Object.DestroyImmediate(pattern);if(target!=null)RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
