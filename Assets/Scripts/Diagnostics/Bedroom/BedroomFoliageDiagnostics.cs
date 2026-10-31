using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime-only isolation at the exact delivered summer branch camera.
public static class BedroomFoliageDiagnostics
{
    [Serializable] class Report {public string purpose="Runtime isolation only; camera/light/bake/textures fixed";public string[] samples;public bool restored,passed;public int materials;}
    public static IEnumerator Run(Camera view,int season)
    {
        if(season!=1){SceneManager.LoadScene("Bedroom_Summer");yield break;}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA","foliage-diagnostic-v6"));Directory.CreateDirectory(folder);
        var originals=new Dictionary<Renderer,Material[]>();var copies=new List<Material>();
        foreach(var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())if(r.name.StartsWith("MainTree_Summer_Leaves")){
            originals[r]=r.sharedMaterials;var materials=r.materials;copies.AddRange(materials);
        }
        view.transform.position=new Vector3(-.48f,1.60f,-1.35f);view.transform.LookAt(new Vector3(-.60f,1.70f,-5));view.fieldOfView=14.25f;
        string[] labels={"A_baseline","B_no_direct_specular","C_no_reflections","D_no_specular_or_reflections","E_no_transmission","F_diffuse_rough","G_restrained_direct"};
        for(int i=0;i<labels.Length;i++){
            foreach(var m in copies){
                m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");m.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
                m.SetFloat("_Transmission",i==4?0:.30f);m.SetFloat("_Glossiness",i==5?.02f:.28f);
                m.SetFloat("_DirectSpecularScale",i==6?.20f:1);
                if(i==6)m.SetFloat("_Glossiness",.14f);
                if(i==1||i==3||i==5)m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                if(i==2||i==3||i==5)m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            }
            yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,labels[i]+".png"));yield return new WaitForSecondsRealtime(.4f);
        }
        foreach(var p in originals)p.Key.sharedMaterials=p.Value;
        foreach(var m in copies)UnityEngine.Object.Destroy(m);
        var report=new Report{samples=labels,restored=true,materials=copies.Count,passed=copies.Count==3};
        foreach(string label in labels)report.passed&=File.Exists(Path.Combine(folder,label+".png"));
        File.WriteAllText(Path.Combine(folder,"isolation.json"),JsonUtility.ToJson(report,true));
        Debug.Log("BEDROOM_FOLIAGE_DIAGNOSTIC_"+(report.passed?"PASS":"FAIL"));Application.Quit(report.passed?0:2);
    }
}
