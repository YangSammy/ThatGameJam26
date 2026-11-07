using System;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkSolarBloomChecks
    {
        public static string Run()
        {
            var root=new GameObject("Solar render checks"){hideFlags=HideFlags.HideAndDontSave};
            root.transform.position=new Vector3(10000,10000,10000);
            var original=UnityEngine.Object.FindObjectOfType<ParkSunSky>();
            Material sky=null,black=null;RenderTexture rt=null;Texture2D pixels=null;
            var oldActive=RenderTexture.active;int checks=0;
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;};
            try
            {
                var c=root.AddComponent<Camera>();c.enabled=false;c.allowHDR=true;c.cullingMask=1<<31;c.clearFlags=CameraClearFlags.Skybox;c.fieldOfView=60;c.aspect=2;
                sky=new Material(original.sky);sky.SetVector("_SunDirection",new Vector4(0,0,1,0));sky.SetColor("_SunRadiance",new Color(60,48,33,1));
                root.AddComponent<Skybox>().material=sky;
                rt=RenderTexture.GetTemporary(512,256,24,RenderTextureFormat.ARGBHalf);c.targetTexture=rt;
                pixels=new Texture2D(512,256,TextureFormat.RGBAFloat,false,true);
                Action read=()=>{RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,512,256),0,0);pixels.Apply();};
                c.Render();read();float open=pixels.GetPixel(256,128).maxColorComponent;
                check(open>5,"Solar disc must retain HDR radiance");
                check(pixels.GetPixel(30,220).maxColorComponent<=1.15f,"Ordinary sky below bloom threshold");
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(root.transform,false);blocker.transform.localPosition=Vector3.forward*5;blocker.transform.localScale=Vector3.one*2;blocker.layer=31;
                black=new Material(Shader.Find("Unlit/Color"));black.color=Color.black;blocker.GetComponent<Renderer>().sharedMaterial=black;
                c.Render();read();check(pixels.GetPixel(256,128).r<.01f,"Opaque geometry must occlude sun");
                blocker.SetActive(false);c.Render();read();check(pixels.GetPixel(256,128).r>5,"Sun returns when obstruction removed");
                var sunGo=new GameObject("Check sun");sunGo.transform.SetParent(root.transform,false);sunGo.transform.rotation=Quaternion.Euler(0,180,0);
                var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.cullingMask=0;
                var b=root.AddComponent<AmplifyBloom.AmplifyBloom>();
                var so=new SerializedObject(b);
                so.FindProperty("m_bloomShader").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Shader>("Assets/AmplifyBloom/Shaders/Bloom.shader");
                so.FindProperty("m_finalCompositionShader").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Shader>("Assets/AmplifyBloom/Shaders/BloomFinal.shader");so.ApplyModifiedPropertiesWithoutUndo();
                var d=root.AddComponent<ParkCinematicBloom>();d.sun=sun;
                check(d.SunViewportWeight()>.99f,"Centered sun gets full optics");
                root.transform.rotation=Quaternion.Euler(0,180,0);sun.transform.rotation=Quaternion.Euler(0,180,0);check(d.SunViewportWeight()==0,"Sun behind camera suppresses optics");
                root.transform.rotation=Quaternion.Euler(0,90,0);sun.transform.rotation=Quaternion.Euler(0,180,0);check(d.SunViewportWeight()==0,"Offscreen sun suppresses optics");
                root.transform.rotation=Quaternion.identity;sun.enabled=false;check(d.SunViewportWeight()==0,"Disabled sun suppresses optics");
                var sync=sunGo.AddComponent<ParkSunSky>();sync.sun=sun;sync.sky=sky;sync.Sync();check(sky.GetColor("_SunRadiance").r==0,"Disabled sun removes sky disc");
                sun.enabled=true;sync.Sync();check(Vector3.Dot((Vector3)sky.GetVector("_SunDirection"),-sun.transform.forward)>.999f,"Sky disc follows light direction");
                sync.sun=null;sync.Sync();check(sky.GetColor("_SunRadiance").r==0,"Missing sun removes sky disc");
                return checks+"/"+checks+" solar checks passed; peak HDR="+open;
            }
            finally
            {
                if(root!=null){var c=root.GetComponent<Camera>();if(c!=null)c.targetTexture=null;UnityEngine.Object.DestroyImmediate(root);}
                RenderTexture.active=oldActive;if(rt!=null)RenderTexture.ReleaseTemporary(rt);
                if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);if(sky!=null)UnityEngine.Object.DestroyImmediate(sky);if(black!=null)UnityEngine.Object.DestroyImmediate(black);
            }
        }
    }
}
