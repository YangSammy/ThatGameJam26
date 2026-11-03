using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dedicated v11 runtime traversal and images; this art viewer has no chapter logic.
public static class BedroomWritingQA
{
    [Serializable] public class Segment { public string target; public bool reached; public float arrivalError,travelled; public int waypoints; }
    [Serializable] public class Result { public int version=11; public string season; public bool passed,scripted=true,teleportsUsed=false,southReachable,tableCollision,northFacing,sharedProps,sixFurnitureReachable; public Vector3 tableCenter,windowCenter,usePosition,forward; public Segment[] segments; public string[] frames; }
    public static IEnumerator Run(Camera view,int season,CharacterController body)
    {
        string name=new[]{"Spring","Summer","Autumn","Winter"}[season];
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA","writing-v11",name));Directory.CreateDirectory(folder);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        var room=GameObject.Find("Bedroom_"+name+"_Editable");
        var access=BedroomAccessibility.Check(room,body,body.transform.position);
        var meshes=room.GetComponentsInChildren<MeshRenderer>();var tables=meshes.Where(r=>r.name.StartsWith("TeaTable_top")).ToArray();
        var window=room.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Window_latch"));
        var center=tables[0].bounds.center;var north=window.position-center;north.y=0;north.Normalize();var east=Vector3.Cross(Vector3.up,north);
        var result=new Result{season=name,tableCenter=center,windowCenter=window.position,sixFurnitureReachable=access.passed,sharedProps=tables.Length==1&&meshes.Count(r=>r.name.StartsWith("Writing_Paper_A4_"))==3&&meshes.Any(r=>r.name.StartsWith("Writing_Pen_body"))};
        RaycastHit hit;result.tableCollision=Physics.Raycast(center+Vector3.up,Vector3.down,out hit,2f)&&hit.collider.name.StartsWith("TeaTable_");
        var frames=new List<string>();var segments=new List<Segment>();
        var preferred=new[]{center-north*.70f,center+east*.68f,center+north*.65f,center-east*.68f,center-north*.70f};
        var labels=new[]{"south","east","north","west","south_return"};
        for(int t=0;t<preferred.Length;t++)
        {
            preferred[t].y=access.floorHeight+.035f;Vector3 standing;
            bool reachable=BedroomAccessibility.TryNearestReachable(preferred[t],out standing)&&(standing-preferred[t]).magnitude<.24f;
            var seg=new Segment{target=labels[t]};
            body.enabled=false;var path=reachable?BedroomAccessibility.Route(body.transform.position,standing,body.radius,body.height):new Vector3[0];body.enabled=true;
            seg.waypoints=path.Length;bool failed=path.Length==0;
            foreach(var waypoint in path)
            {
                float deadline=Time.realtimeSinceStartup+3f;
                while(true)
                {
                    var delta=waypoint-body.transform.position;delta.y=0;if(delta.magnitude<.025f)break;
                    if(Time.realtimeSinceStartup>deadline){failed=true;break;}
                    var old=body.transform.position;body.transform.rotation=Quaternion.LookRotation(delta);
                    body.Move(delta.normalized*Mathf.Min(delta.magnitude,1.35f*Time.unscaledDeltaTime)+Vector3.down*.03f);
                    seg.travelled+=Vector3.Distance(old,body.transform.position);yield return null;
                }
                if(failed)break;
            }
            var error=standing-body.transform.position;error.y=0;seg.arrivalError=error.magnitude;seg.reached=reachable&&!failed&&error.magnitude<.08f;segments.Add(seg);
            if(t==0)result.southReachable=seg.reached;
        }
        body.transform.rotation=Quaternion.LookRotation(north);result.usePosition=body.transform.position;result.forward=body.transform.forward;
        result.northFacing=Vector3.Dot(result.forward,north)>.999f;
        view.transform.localPosition=new Vector3(0,1.02f,0);view.transform.localRotation=Quaternion.Euler(14,0,0);view.fieldOfView=75;
        yield return Capture(folder,"north_window",frames);
        view.transform.localRotation=Quaternion.Euler(55,0,0);view.fieldOfView=52;
        yield return Capture(folder,"paper_pen",frames);
        result.passed=result.sixFurnitureReachable&&result.sharedProps&&result.southReachable&&result.tableCollision&&result.northFacing&&segments.All(s=>s.reached);
        result.segments=segments.ToArray();result.frames=frames.ToArray();File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));
        Debug.Log("BEDROOM_WRITING_V11_"+(result.passed?"PASS":"FAIL")+" "+name+" south="+result.southReachable+" collision="+result.tableCollision+" facing="+result.northFacing+" sides="+segments.All(s=>s.reached));
        if(season<3)SceneManager.LoadScene("Bedroom_"+new[]{"Spring","Summer","Autumn","Winter"}[season+1]);else Application.Quit(result.passed?0:2);
    }
    static IEnumerator Capture(string folder,string name,List<string> frames)
    {
        yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));frames.Add(name+".png");yield return new WaitForSecondsRealtime(.3f);
    }
}
