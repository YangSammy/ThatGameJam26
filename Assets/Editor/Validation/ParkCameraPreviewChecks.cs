using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkCameraPreviewChecks
    {
        static void Check(List<string> lines,bool ok,string message)=>lines.Add((ok?"PASS ":"FAIL ")+message);
        public static string ResponseRules()
        {
            var lines=new List<string>();
            float normal=ParkCameraResponse.ExposureError(8,1f/125,100,13);
            float equivalent=ParkCameraResponse.ExposureError(5.6f,1f/250,100,13);
            Check(lines,Mathf.Abs(normal-equivalent)<.05f,"Equivalent aperture/shutter pairs agree within rounded f-stop precision");
            Check(lines,Mathf.Abs(ParkCameraResponse.ExposureError(8,1f/250,200,13)-normal)<.001f,"Doubling ISO compensates halved exposure time");
            Check(lines,ParkCameraResponse.ExposureGain(1)==.5f && ParkCameraResponse.ExposureGain(-1)==2,"Exposure sign and one-stop gain");
            foreach(float distance in new[]{1.2f,3f,8f,18f})
            {
                float wide=ParkCameraResponse.ClearRange(distance,2.8f),mid=ParkCameraResponse.ClearRange(distance,8),small=ParkCameraResponse.ClearRange(distance,16);
                Check(lines,wide<mid && mid<small,"Aperture orders clear range at "+distance+" m");
                Check(lines,Mathf.Abs(mid-Mathf.Max(.2f,distance*.065f))<.0001f,"f/8 preserves existing tolerance at "+distance+" m");
                Check(lines,ParkCameraResponse.IsSharp(distance+mid*.99f,distance,8) && !ParkCameraResponse.IsSharp(distance+mid*1.01f,distance,8),"Acceptance follows rendered clear band at "+distance+" m");
                Check(lines,ParkCameraResponse.FocusBlur(distance+mid*.99f,distance,8)==0 && ParkCameraResponse.FocusBlur(distance+mid*1.01f,distance,8)>0,"Whole-image clarity matches acceptance at "+distance+" m");
                float previous=0;
                foreach(float error in new[]{0f,.1f,.3f,1f,3f})
                {
                    float blur=ParkCameraResponse.FocusBlur(distance+mid+error,distance,8);
                    Check(lines,blur>=previous && blur>=0 && blur<=1,"Defocus grows continuously with excess distance at "+distance+" m");previous=blur;
                }
            }
            float near=ParkCameraResponse.ScrollFocus(1.5f,1,.25f,1.2f,18)-1.5f;
            float far=ParkCameraResponse.ScrollFocus(10,1,.25f,1.2f,18)-10;
            Check(lines,near<.12f && far>.5f,"Near focus fine adjustment and distant fast travel");
            float outward=ParkCameraResponse.ScrollFocus(4,3,.25f,1.2f,18);
            Check(lines,Mathf.Abs(ParkCameraResponse.ScrollFocus(outward,-3,.25f,1.2f,18)-4)<.0001f,"Opposite wheel travel is reversible away from end stops");
            Check(lines,ParkCameraResponse.ScrollFocus(1.2f,-100,.25f,1.2f,18)==1.2f && ParkCameraResponse.ScrollFocus(18,100,.25f,1.2f,18)==18,"Focus remains within existing lens limits");
            return string.Join("\n",lines);
        }
        public static string ExposureGpu()
        {
            var lines=new List<string>();
            var shader=Shader.Find("Hidden/ParkPhoto/ManualFocus");
            if(shader==null || !shader.isSupported)return "FAIL Camera shader unavailable";
            var material=new Material(shader);
            var input=new Texture2D(16,16,TextureFormat.RGBA32,false);
            var pixels=new Color[256];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.35f,.35f,.35f,1);
            input.SetPixels(pixels);input.Apply();
            var target=RenderTexture.GetTemporary(16,16,0);var previous=RenderTexture.active;
            var output=new Texture2D(16,16,TextureFormat.RGB24,false);
            try
            {
                var values=new float[3];float[] gains={.5f,1,2};
                material.SetFloat("_FocusDistance",4);material.SetFloat("_SharpRange",.26f);material.SetFloat("_BlurTransition",.6f);
                material.SetFloat("_MaxBlurPixels",0);
                for(int i=0;i<3;i++)
                {
                    material.SetFloat("_ExposureGain",gains[i]);Graphics.Blit(input,target,material,2);RenderTexture.active=target;
                    output.ReadPixels(new Rect(0,0,16,16),0,0);output.Apply();values[i]=output.GetPixel(8,8).r;
                }
                Check(lines,values[0]<values[1]-.04f,"Underexposure darkens actual GPU pixels");
                Check(lines,values[2]>values[1]+.04f,"Overexposure brightens actual GPU pixels");
                Check(lines,Mathf.Abs(values[1]-.35f)<.015f,"Neutral exposure preserves source brightness");
                lines.Add("GPU brightness: "+string.Join(", ",Array.ConvertAll(values,x=>x.ToString("F4"))));
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(input);UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(material);}
            return string.Join("\n",lines);
        }
    }
}
