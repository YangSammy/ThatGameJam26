using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Runtime-only controls. Never saves scenes or changes the baked maps.
public static class BedroomLightingDiagnostics
{
    [Serializable] class Receiver { public string name,probeUsage;public int lightmapIndex;public Bounds bounds;public Vector3 anchor;public Color[] sh; }
    [Serializable] class Sample { public string label;public Color sunColor;public float sunIntensity,bounceIntensity,transmission,smoothness;public Receiver[] receivers; }
    [Serializable] class Report {public bool diagnosticOnly=true,restored,passed;public string season,purpose="Same baked v7 scene, fixed cameras, one runtime channel per control; encoded pixels are not physical illuminance";public Sample[] samples;}
    public static IEnumerator Run(Camera view,int season)
    {
        if(season==0){SceneManager.LoadScene("Bedroom_Summer");yield break;}
        if(season==3){Application.Quit();yield break;}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        foreach(var drift in UnityEngine.Object.FindObjectsOfType<AutumnLeafDrift>())drift.enabled=false;
        foreach(var fan in UnityEngine.Object.FindObjectsOfType<SeasonalFanSpin>())fan.enabled=false;
        string seasonName=season==1?"Summer":"Autumn";
        bool refined=Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-lighting-refined")>=0;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA",refined?"lighting-diagnostic-refined-v8":"lighting-diagnostic-v8",seasonName));Directory.CreateDirectory(folder);
        var all=UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
        var selected=all.Where(r=>!r.name.StartsWith("MainTree_")&&Mathf.Abs(r.bounds.center.x)<2.5f&&Mathf.Abs(r.bounds.center.z)<3.1f&&((r.name.StartsWith("Bed")&&r.bounds.center.y<.50f)||(r.name.StartsWith("Desk_")&&r.bounds.center.y<.80f)||r.name.StartsWith("North_wainscot")||(season==1&&r.sharedMaterials.Any(m=>m!=null&&m.shader.name=="QuietRoom/Thin linen transmission")))).ToArray();
        var sun=UnityEngine.Object.FindObjectsOfType<Light>().Single(l=>l.name.StartsWith("Window_sun_"));
        var bounce=UnityEngine.Object.FindObjectsOfType<Light>().Single(l=>l.name=="Window_near_field_diffuse_bounce");
        var linen=all.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.shader.name=="QuietRoom/Thin linen transmission").Distinct().ToArray();
        var sunColor=sun.color;float sunIntensity=sun.intensity,bounceIntensity=bounce.intensity;
        var usage=selected.Select(r=>r.lightProbeUsage).ToArray();var anchors=selected.Select(r=>r.probeAnchor).ToArray();var maps=selected.Select(r=>r.lightmapIndex).ToArray();
        var mats=linen.Select(m=>new Material(m)).ToArray();var temps=new List<GameObject>();var samples=new List<Sample>();
        string[] labels=season==1?(refined?new[]{"A_baseline","B_linen_probes_off","C_linen_gray18","D_linen_reflectance_060","E_linen_backface_cull","F_linen_response"}:new[]{"A_baseline","B_sun_off","C_window_bounce_off","D_transmission_off","E_direct_specular_off","F_linen_response"}):new[]{"A_baseline","B_sun_neutral","C_window_bounce_off","D_low_receivers_probe_off","E_low_receivers_near_face_probe","F_low_receivers_gray18"};
        for(int i=0;i<labels.Length;i++)
        {
            sun.color=sunColor;sun.intensity=sunIntensity;bounce.intensity=bounceIntensity;
            for(int j=0;j<linen.Length;j++)linen[j].CopyPropertiesFromMaterial(mats[j]);
            for(int j=0;j<selected.Length;j++){selected[j].lightProbeUsage=usage[j];selected[j].probeAnchor=anchors[j];selected[j].lightmapIndex=maps[j];}
            if(season==1)
            {
                if(!refined){if(i==1)sun.intensity=0;if(i==2)bounce.intensity=0;}
                if(refined&&i==1)foreach(var r in selected.Where(r=>r.sharedMaterials.Any(m=>linen.Contains(m))))r.lightProbeUsage=LightProbeUsage.Off;
                foreach(var m in linen){if(!refined&&i==3)m.SetFloat("_Transmission",0);if(!refined&&i==4)m.SetFloat("_DirectSpecularScale",0);if(refined&&i==2){m.mainTexture=null;m.color=new Color(.18f,.18f,.18f,.78f).gamma;}if(refined&&i==3)m.color=new Color(.6f,.6f,.6f,.78f).gamma;if(refined&&i==4)m.SetFloat("_Cull",2);if(i==5)m.SetFloat("_IndirectDiffuseScale",.35f);}
            }
            else
            {
                if(i==1)sun.color=new Color(1,.94f,.84f);
                if(i==2)bounce.intensity=0;
                if(i==3)foreach(var r in selected)if(r.lightmapIndex<0)r.lightProbeUsage=LightProbeUsage.Off;
                if(i==4)foreach(var r in selected)
                {
                    var go=new GameObject("Temporary_near_face_probe");temps.Add(go);
                    var c=r.bounds.center;go.transform.position=new Vector3(Mathf.Clamp(c.x,-1.65f,1.65f),Mathf.Clamp(c.y,.35f,1.2f),c.z-.20f);
                    r.lightmapIndex=-1;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.probeAnchor=go.transform;
                }
            }
            var originalSelected=selected.Select(r=>r.sharedMaterials).ToArray();var gray=new Material(Shader.Find("Standard"));gray.color=new Color(.18f,.18f,.18f).gamma;gray.SetFloat("_Glossiness",0);
            if(season==2&&i==5)foreach(var r in selected)r.sharedMaterials=r.sharedMaterials.Select(m=>gray).ToArray();
            var receivers=new List<Receiver>();
            foreach(var r in selected){var p=r.probeAnchor!=null?r.probeAnchor.position:r.bounds.center;SphericalHarmonicsL2 sh;LightProbes.GetInterpolatedProbe(p,r,out sh);var colors=new Color[6];sh.Evaluate(new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back},colors);receivers.Add(new Receiver{name=r.name,bounds=r.bounds,lightmapIndex=r.lightmapIndex,probeUsage=r.lightProbeUsage.ToString(),anchor=p,sh=colors});}
            samples.Add(new Sample{label=labels[i],sunColor=sun.color,sunIntensity=sun.intensity,bounceIntensity=bounce.intensity,transmission=linen.Length>0?linen[0].GetFloat("_Transmission"):0,smoothness=linen.Length>0?linen[0].GetFloat("_GlossMapScale"):0,receivers=receivers.ToArray()});
            // Entry and close window preserve the v7 regular framing. Low view is diagnostic-only.
            yield return Capture(view,folder,labels[i]+"_entry",new Vector3(.17f,1.67f,2.35f),new Vector3(.17f,1.45f,-2.8f),66);
            if(season==1)yield return Capture(view,folder,labels[i]+"_window",new Vector3(0,1.65f,-1.22f),new Vector3(.15f,1.94f,-3.75f),45);
            else yield return Capture(view,folder,labels[i]+"_low",new Vector3(.12f,.48f,.1f),new Vector3(0,.37f,-1.75f),72);
            for(int j=0;j<selected.Length;j++)selected[j].sharedMaterials=originalSelected[j];UnityEngine.Object.Destroy(gray);
        }
        sun.color=sunColor;sun.intensity=sunIntensity;bounce.intensity=bounceIntensity;
        for(int j=0;j<linen.Length;j++){linen[j].CopyPropertiesFromMaterial(mats[j]);UnityEngine.Object.Destroy(mats[j]);}
        for(int j=0;j<selected.Length;j++){selected[j].lightProbeUsage=usage[j];selected[j].probeAnchor=anchors[j];selected[j].lightmapIndex=maps[j];}
        foreach(var go in temps)UnityEngine.Object.Destroy(go);
        var report=new Report{season=seasonName,restored=true,passed=Directory.GetFiles(folder,"*.png").Length==12,samples=samples.ToArray()};File.WriteAllText(Path.Combine(folder,"isolation.json"),JsonUtility.ToJson(report,true));
        Debug.Log("BEDROOM_LIGHTING_DIAGNOSTIC_"+(report.passed?"PASS":"FAIL")+" "+seasonName);
        if(season==1)SceneManager.LoadScene("Bedroom_Autumn");else Application.Quit(report.passed?0:2);
    }
    static IEnumerator Capture(Camera view,string folder,string label,Vector3 eye,Vector3 target,float fov)
    {
        view.transform.position=eye;view.transform.LookAt(target);view.fieldOfView=fov;
        yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,label+".png"));yield return new WaitForSecondsRealtime(.25f);
    }
}
