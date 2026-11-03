using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;
// Runtime diagnostics only: fixed views, retained baked indirect, isolated direct/specular controls.
public static class BedroomStyleDiagnostics {
 [Serializable]class Shot {public string variant,view;public float sun;public Vector3 eye,target;public float fov;}
 [Serializable]class Report {public string scope="Runtime direct-channel comparison with identical v16 baked indirect; not a new bake per variant";public Shot[] shots;}
 public static IEnumerator Run(Camera view,int season){
  if(season!=1){SceneManager.LoadScene("Bedroom_Summer");yield break;}
  Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
  foreach(var f in UnityEngine.Object.FindObjectsOfType<SeasonalFanSpin>())f.enabled=false;
  var sun=UnityEngine.Object.FindObjectsOfType<Light>().Single(l=>l.name.StartsWith("Window_sun_"));
  var mats=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();var copies=mats.Select(m=>new Material(m)).ToArray();
  var entry=view.transform.position;var entryRotation=view.transform.rotation;float entryFov=view.fieldOfView;
  var desk=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().First(r=>r.name.StartsWith("Desk_solid_walnut_worktop")).bounds.center;
  var poses=new[]{entry,new Vector3(desk.x*.30f,1.40f,desk.z+.95f),new Vector3(.10f,1.60f,-1.35f),new Vector3(-.20f,1.22f,.02f)};
  var targets=new[]{entry+entryRotation*Vector3.forward,desk+Vector3.up*.15f,new Vector3(-.10f,1.90f,-3.6f),new Vector3(1.25f,.65f,-1.0f)};
  var names=new[]{"entry","desk","window","bed"};var fovs=new[]{entryFov,49f,58f,52f};var shots=new List<Shot>();
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA/style-v16/diagnostic"));Directory.CreateDirectory(folder);
  foreach(string variant in new[]{"A_baseline_265","B_direct_off","C_specular_off","D_sun_220","E_sun_190"}){
   for(int i=0;i<mats.Length;i++)mats[i].CopyPropertiesFromMaterial(copies[i]);
   sun.intensity=variant=="B_direct_off"?0:variant=="D_sun_220"?2.2f:variant=="E_sun_190"?1.9f:2.65f;
   if(variant=="C_specular_off")foreach(var m in mats){if(m.HasProperty("_DirectSpecularScale"))m.SetFloat("_DirectSpecularScale",0);if(m.shader.name=="Standard"){m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");}}
   for(int i=0;i<poses.Length;i++){
    view.transform.position=poses[i];view.transform.LookAt(targets[i]);view.fieldOfView=fovs[i];yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
    ScreenCapture.CaptureScreenshot(Path.Combine(folder,variant+"_"+names[i]+".png"));yield return new WaitForSecondsRealtime(.25f);shots.Add(new Shot{variant=variant,view=names[i],sun=sun.intensity,eye=poses[i],target=targets[i],fov=fovs[i]});
   }
  }
  File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(new Report{shots=shots.ToArray()},true));Debug.Log("V16_DIAGNOSTIC_COMPLETE");Application.Quit();
 }
}
