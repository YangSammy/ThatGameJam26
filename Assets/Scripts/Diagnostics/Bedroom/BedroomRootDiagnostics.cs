using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Temporary runtime changes only. Each sample resets every touched property.
public static class BedroomRootDiagnostics
{
    [Serializable] class Sample { public string label; public Vector3 anchor; public Color[] sh; }
    [Serializable] class Report { public bool passed,restored; public string purpose="Fixed camera root isolation; same baked scene, materials and transforms"; public string receiveGI; public Bounds rootsBounds; public Sample[] samples; }
    public static IEnumerator Run(Camera view,int season)
    {
        if(season!=1){SceneManager.LoadScene("Bedroom_Summer");yield break;}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA","root-diagnostic-v7"));Directory.CreateDirectory(folder);
        var all=UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
        var roots=all.Single(r=>r.name.StartsWith("MainTree_SHARED_Roots"));
        var tree=all.Where(r=>r.name.StartsWith("MainTree_")).ToArray();
        var anchor=roots.probeAnchor;Vector3 position=anchor.position;
        var usage=roots.lightProbeUsage;bool receive=roots.receiveShadows;
        var modes=tree.Select(r=>r.shadowCastingMode).ToArray();
        view.transform.position=new Vector3(-.65f,.42f,-3.65f);view.transform.LookAt(new Vector3(-.60f,.13f,-5));view.fieldOfView=33.4f;
        var labels=new[]{"A_baseline","B_above_ground_probe","C_tree_cast_off","D_root_receive_off","E_probe_off","F_above_probe_receive_off","G_roots_hidden","H_trunk_probe"};
        var samples=new List<Sample>();
        for(int i=0;i<labels.Length;i++){
            anchor.position=position;roots.lightProbeUsage=usage;roots.receiveShadows=receive;roots.enabled=true;
            for(int j=0;j<tree.Length;j++)tree[j].shadowCastingMode=modes[j];
            if(i==1||i==5)anchor.position=new Vector3(roots.bounds.center.x,.22f,roots.bounds.center.z);
            if(i==2)foreach(var r in tree)r.shadowCastingMode=ShadowCastingMode.Off;
            if(i==3||i==5)roots.receiveShadows=false;
            if(i==4)roots.lightProbeUsage=LightProbeUsage.Off;
            if(i==6)roots.enabled=false; if(i==7)anchor.position=all.Single(r=>r.name.StartsWith("MainTree_SHARED_Trunk")).probeAnchor.position;
            SphericalHarmonicsL2 sh;LightProbes.GetInterpolatedProbe(anchor.position,roots,out sh);
            var colors=new Color[6];sh.Evaluate(new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back},colors);
            samples.Add(new Sample{label=labels[i],anchor=anchor.position,sh=colors});
            yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,labels[i]+".png"));yield return new WaitForSecondsRealtime(.35f);
        }
        anchor.position=position;roots.lightProbeUsage=usage;roots.receiveShadows=receive;roots.enabled=true;
        for(int j=0;j<tree.Length;j++)tree[j].shadowCastingMode=modes[j];
        var report=new Report{passed=labels.All(n=>File.Exists(Path.Combine(folder,n+".png"))),restored=true,receiveGI="lightmapIndex="+roots.lightmapIndex+"; probe="+roots.lightProbeUsage,rootsBounds=roots.bounds,samples=samples.ToArray()};
        File.WriteAllText(Path.Combine(folder,"isolation.json"),JsonUtility.ToJson(report,true));
        Debug.Log("BEDROOM_ROOT_DIAGNOSTIC_"+(report.passed?"PASS":"FAIL"));Application.Quit(report.passed?0:2);
    }
}
