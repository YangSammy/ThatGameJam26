using System;
using UnityEngine;
using UnityEditor;

namespace MemoryWhitebox.Editor
{
    public static class ParkCinematicBloomChecks
    {
        public static string Run()
        {
            var sourceBloom=UnityEngine.Object.FindObjectOfType<ParkCinematicBloom>().GetComponent<AmplifyBloom.AmplifyBloom>();
            var go=new GameObject("Bloom validation temporary"){hideFlags=HideFlags.HideAndDontSave};
            RenderTexture src=null,dst=null;Texture2D pattern=null;
            var active=RenderTexture.active;
            int checks=0;
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;};
            try
            {
                go.AddComponent<Camera>().enabled=false;
                var bloom=go.AddComponent<AmplifyBloom.AmplifyBloom>();
                var from=new SerializedObject(sourceBloom);var to=new SerializedObject(bloom);
                foreach(var name in new[]{"m_bloomShader","m_finalCompositionShader","m_lensDirtTexture"})
                    to.FindProperty(name).objectReferenceValue=from.FindProperty(name).objectReferenceValue;
                to.ApplyModifiedPropertiesWithoutUndo();
                bloom.HighPrecision=true;bloom.BloomDownsampleCount=4;bloom.OverallThreshold=.72f;
                bloom.ApplyBokehFilter=false;bloom.ApplyLensStardurst=false;
                var focus=go.AddComponent<ParkPhotoFocus>();
                var driver=go.AddComponent<ParkCinematicBloom>();driver.focus=focus;
                src=RenderTexture.GetTemporary(128,64,0,RenderTextureFormat.ARGBHalf);
                dst=RenderTexture.GetTemporary(128,64,0,RenderTextureFormat.ARGB32);
                pattern=new Texture2D(128,64,TextureFormat.RGBAFloat,false,true);
                var pixels=new Color[128*64];
                for(int y=0;y<64;y++)for(int x=0;x<128;x++)pixels[y*128+x]=x>48&&x<80&&y>20&&y<44?new Color(5,3,1,1):new Color(.1f,.1f,.1f,1);
                pattern.SetPixels(pixels);pattern.Apply();Graphics.Blit(pattern,src);
                driver.RenderImage(src,dst);
                check(bloom.TemporalFilteringActive&&!bloom.ApplyLensFlare&&!bloom.ApplyLensDirt,"Walking optics");
                focus.activeFocus=true;driver.RenderImage(src,dst);
                check(bloom.ApplyLensFlare&&bloom.ApplyLensGlare&&bloom.ApplyLensDirt,"Viewfinder optics");
                focus.suppressFocusAid=true;driver.RenderImage(src,dst);var first=Read(dst);
                check(!bloom.TemporalFilteringActive&&!bloom.ApplyLensFlare&&!bloom.ApplyLensGlare&&!bloom.ApplyLensDirt,"Capture suppression");
                focus.suppressFocusAid=false;driver.RenderImage(src,dst);driver.RenderImage(src,dst);
                focus.suppressFocusAid=true;driver.RenderImage(src,dst);var second=Read(dst);
                int max=0;for(int i=0;i<first.Length;i++)max=Math.Max(max,Math.Max(Math.Abs(first[i].r-second[i].r),Math.Max(Math.Abs(first[i].g-second[i].g),Math.Abs(first[i].b-second[i].b))));
                check(max<=1,"Capture must not depend on preview history");
                Graphics.Blit(Texture2D.blackTexture,src);focus.suppressFocusAid=false;focus.raiseOcclusion=1;
                driver.RenderImage(src,dst);var black=Read(dst);
                check(Array.TrueForAll(black,p=>p.r==0&&p.g==0&&p.b==0),"Occlusion must clear bright history");
                focus.raiseOcclusion=0;focus.activeFocus=false;Graphics.Blit(pattern,src);driver.RenderImage(src,dst);
                check(!bloom.ApplyLensFlare&&!bloom.ApplyLensDirt&&bloom.TemporalFilteringActive,"Return to walking");
                Graphics.Blit(Texture2D.blackTexture,src);driver.RenderImage(src,dst);var softened=Read(dst);
                check(Array.Exists(softened,p=>p.r>0||p.g>0||p.b>0),"Temporal filter must smooth highlight disappearance");
                Graphics.Blit(pattern,src);driver.RenderImage(src,dst);
                Graphics.Blit(Texture2D.blackTexture,src);go.transform.position=Vector3.one*10;driver.RenderImage(src,dst);black=Read(dst);
                check(Array.TrueForAll(black,p=>p.r==0&&p.g==0&&p.b==0),"Camera cut must clear history");
                driver.enabled=false;
                check(!bloom.TemporalFilteringActive,"Disable clears history");
                driver.enabled=true;driver.RenderImage(src,dst);black=Read(dst);
                check(Array.TrueForAll(black,p=>p.r==0&&p.g==0&&p.b==0),"Re-enable starts clean");
                var photo=go.AddComponent<ParkPhotoPrototype>();photo.view=go.GetComponent<Camera>();photo.focusEffect=focus;
                photo.view.targetTexture=dst;
                var previousActive=RenderTexture.active;
                try
                {
                    typeof(ParkPhotoPrototype).GetMethod("RenderPhotographicFrame",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(photo,null);
                    check(!bloom.TemporalFilteringActive&&!bloom.ApplyLensFlare&&!focus.suppressFocusAid,"Shared photographic entry suppresses optics and restores scope");
                    check(photo.view.targetTexture==dst && RenderTexture.active==previousActive,"HDR capture restores render targets");
                }
                finally{photo.view.targetTexture=null;}
                return checks+"/"+checks+" checks passed; capture difference="+max+"/255";
            }
            finally
            {
                RenderTexture.active=active;
                if(src!=null)RenderTexture.ReleaseTemporary(src);if(dst!=null)RenderTexture.ReleaseTemporary(dst);
                if(pattern!=null)UnityEngine.Object.DestroyImmediate(pattern);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        static Color32[] Read(RenderTexture rt)
        {
            var old=RenderTexture.active;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();return t.GetPixels32();}
            finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);}
        }
    }
}
