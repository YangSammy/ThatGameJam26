using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Actual controller traversal, separate from static camera captures and manual input.
// Saved stills sample the run; they are not advertised as continuous video review.
public static class BedroomWalkthrough
{
    [Serializable]class Segment{public string furniture;public bool reached;public int waypoints;public float travelled,arrivalError,seconds;}
    [Serializable]class Sample{public float seconds;public Vector3 position;public string station;}
    [Serializable]class Report{public int version=9;public string season,scope="Scripted first-person CharacterController.Move traversal to six furniture stations; sampled stills, no manual keyboard play or continuous-video certification";public bool passed,scripted=true,teleportsUsed=false;public Segment[] segments;public Sample[] samples;public string[] frames;}
    public static IEnumerator Run(Camera view,int season,CharacterController body)
    {
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        string seasonName=new[]{"Spring","Summer","Autumn","Winter"}[season];
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA","walkthrough-v9",seasonName));Directory.CreateDirectory(folder);
        var room=GameObject.Find("Bedroom_"+seasonName+"_Editable");var report=BedroomAccessibility.Check(room,body,body.transform.position);
        var segments=new List<Segment>();var samples=new List<Sample>();var frames=new List<string>();
        view.transform.localPosition=new Vector3(0,1.62f,0);view.transform.localRotation=Quaternion.Euler(3,0,0);view.fieldOfView=66;
        float started=Time.realtimeSinceStartup,nextSample=started,nextFrame=started+2;
        yield return Capture(folder,"00_entry",frames);
        int frame=1;
        foreach(var station in report.stations)
        {
            body.enabled=false;var path=BedroomAccessibility.Route(body.transform.position,station.standing,body.radius,body.height);body.enabled=true;
            var segment=new Segment{furniture=station.furniture,waypoints=path.Length};float began=Time.realtimeSinceStartup;
            bool failed=path.Length==0;
            foreach(var waypoint in path)
            {
                float deadline=Time.realtimeSinceStartup+2.5f;
                while(true)
                {
                    var delta=waypoint-body.transform.position;delta.y=0;if(delta.magnitude<.025f)break;
                    if(Time.realtimeSinceStartup>deadline){failed=true;break;}
                    var prior=body.transform.position;var turn=Quaternion.LookRotation(delta.normalized);
                    body.transform.rotation=Quaternion.RotateTowards(body.transform.rotation,turn,220*Time.unscaledDeltaTime);
                    body.Move(delta.normalized*Mathf.Min(delta.magnitude,1.35f*Time.unscaledDeltaTime)+Vector3.down*.03f);
                    segment.travelled+=Vector3.Distance(prior,body.transform.position);
                    if(Time.realtimeSinceStartup>=nextSample){samples.Add(new Sample{seconds=Time.realtimeSinceStartup-started,position=body.transform.position,station=station.furniture});nextSample=Time.realtimeSinceStartup+.1f;}
                    if(Time.realtimeSinceStartup>=nextFrame){yield return Capture(folder,(frame++).ToString("D2")+"_walking_"+station.furniture,frames);nextFrame=Time.realtimeSinceStartup+2;}
                    yield return null;
                }
                if(failed)break;
            }
            var error=station.standing-body.transform.position;error.y=0;segment.arrivalError=error.magnitude;segment.reached=!failed&&error.magnitude<.08f;segment.seconds=Time.realtimeSinceStartup-began;segments.Add(segment);
            var furniture=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith(station.furniture=="Bed"?"Bed_":station.furniture+"_")).ToArray();
            if(furniture.Length>0){var target=furniture[0].bounds.center;target.y=.95f;view.transform.LookAt(target);}
            yield return Capture(folder,(frame++).ToString("D2")+"_arrived_"+station.furniture,frames);view.transform.localRotation=Quaternion.Euler(3,0,0);
        }
        var result=new Report{season=seasonName,passed=report.passed&&segments.Count==6&&segments.All(s=>s.reached),segments=segments.ToArray(),samples=samples.ToArray(),frames=frames.ToArray()};File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));
        Debug.Log("BEDROOM_WALKTHROUGH_"+(result.passed?"PASS":"FAIL")+" "+seasonName+" stations="+segments.Count+" metres="+segments.Sum(s=>s.travelled));
        if(season<3)SceneManager.LoadScene("Bedroom_"+new[]{"Spring","Summer","Autumn","Winter"}[season+1]);else Application.Quit(result.passed?0:2);
    }
    static IEnumerator Capture(string folder,string name,List<string> frames)
    {
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));frames.Add(name+".png");yield return new WaitForSecondsRealtime(.18f);
    }
}
