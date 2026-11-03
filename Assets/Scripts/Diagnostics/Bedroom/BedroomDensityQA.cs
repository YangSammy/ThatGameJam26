using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Continuous physics traversal in the delivered art viewer. No chapter logic.
public static class BedroomDensityQA
{
    [Serializable] public class Controller { public float radius,height,skinWidth,stepOffset; }
    [Serializable] public class Segment { public string target; public string[] via; public bool reached,controllerEnabled; public float arrivalError,travelled,seconds; public int waypoints,moveCalls; public Vector3 standing; }
    [Serializable] public class Sample { public float seconds; public string target; public Vector3 position; }
    [Serializable] public class Obstacle { public string name; public bool enabled; public Vector3 center,size; }
    [Serializable] public class RayEvidence { public string name,type;public int instanceId;public float distance;public bool observerSelf;public Vector3 point; }
    [Serializable] public class Result {
        public int version=17; public string season,error,scope="Actual CharacterController.Move continuous traversal and sampled stills; no teleport, OS input injection or full manual walkthrough.";
        public bool passed,scripted=true,teleportsUsed=false,continuousFromDoor,returnedToDoor,newCollidersEnabled,southReachable,tableCollision,northFacing,sharedProps,sixFurnitureReachable,paperSightlineClear;
        public int newColliderCount,expectedNewColliderCount; public Controller controller; public Obstacle[] newColliders,furnitureColliders;
        public string rawPaperRayCollider,scenePaperRayCollider;public float rawPaperRayDistance,scenePaperRayDistance;public bool rawPaperRayHitWasPlayer;public string[] paperRayHitInventory;public Vector3 paperRayOrigin,paperRayTarget;
        public int observerBodyInstanceId,firstNonSelfInstanceId;public string firstNonSelfColliderType;public bool selfIgnoredForPaperRay,bodyEnabledDuringPaperRay;public RayEvidence rawPaperRay;public RayEvidence[] paperRayHits;
        public Vector3 entry,returnPosition,tableCenter,windowCenter,usePosition,forward; public Segment[] segments; public Sample[] samples; public string[] frames;
    }
    public static IEnumerator Run(Camera view,int season,CharacterController body)
    {
        string name=new[]{"Spring","Summer","Autumn","Winter"}[season];
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA","density-v17",name));Directory.CreateDirectory(folder);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        var room=GameObject.Find("Bedroom_"+name+"_Editable");
        var result=new Result{season=name,entry=body.transform.position,controller=new Controller{radius=body.radius,height=body.height,skinWidth=body.skinWidth,stepOffset=body.stepOffset}};
        var frames=new List<string>();var segments=new List<Segment>();var samples=new List<Sample>();
        var access=BedroomAccessibility.Check(room,body,body.transform.position);result.sixFurnitureReachable=access.passed;
        var meshes=room.GetComponentsInChildren<MeshRenderer>();
        var tables=meshes.Where(r=>r.name.StartsWith("TeaTable_top")).ToArray();
        var window=room.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Window_latch"));
        var center=tables[0].bounds.center;var north=window.position-center;north.y=0;north.Normalize();var east=Vector3.Cross(Vector3.up,north);
        result.tableCenter=center;result.windowCenter=window.position;
        result.sharedProps=tables.Length==1&&meshes.Count(r=>r.name.StartsWith("Writing_Paper_A4_"))==3&&meshes.Any(r=>r.name.StartsWith("Writing_Pen_body"));
        RaycastHit hit;result.tableCollision=Physics.Raycast(center+Vector3.up,Vector3.down,out hit,2f)&&hit.collider.name.StartsWith("TeaTable_");
        var denseRoots=room.GetComponentsInChildren<Transform>().Where(t=>t.GetComponent<MeshRenderer>()==null&&t.name.StartsWith("DENSE_")).ToArray();
        var denseColliders=UnityEngine.Object.FindObjectsOfType<BoxCollider>().Where(c=>c.name.StartsWith("DENSE_")&&c.name.EndsWith("_Walking_collision")).ToArray();
        result.expectedNewColliderCount=denseRoots.Length;result.newColliderCount=denseColliders.Length;
        result.newColliders=denseColliders.Select(c=>new Obstacle{name=c.name,enabled=c.enabled&&c.gameObject.activeInHierarchy,center=c.bounds.center,size=c.bounds.size}).ToArray();
        result.furnitureColliders=UnityEngine.Object.FindObjectsOfType<BoxCollider>().Where(c=>c.name.EndsWith("Walking_collision")&&!c.name.StartsWith("DENSE_")).Select(c=>new Obstacle{name=c.name,enabled=c.enabled&&c.gameObject.activeInHierarchy,center=c.bounds.center,size=c.bounds.size}).ToArray();
        result.newCollidersEnabled=denseRoots.Length>0&&denseRoots.Length==denseColliders.Length&&result.newColliders.All(c=>c.enabled);
        view.transform.localPosition=new Vector3(0,1.62f,0);view.transform.localRotation=Quaternion.Euler(4.5f,0,0);view.fieldOfView=66;
        yield return Capture(folder,"00_entry",frames);
        var targets=new List<KeyValuePair<string,Vector3>>();
        targets.Add(new KeyValuePair<string,Vector3>("Writing",center-north*.70f));
        targets.Add(new KeyValuePair<string,Vector3>("East_working_aisle",center+east*.70f+north*.12f));
        var bedParts=meshes.Where(r=>r.name.StartsWith("Bed_")).ToArray();var deskParts=meshes.Where(r=>r.name.StartsWith("Desk_")).ToArray();
        Bounds bedBounds=bedParts[0].bounds;foreach(var part in bedParts)bedBounds.Encapsulate(part.bounds);
        Bounds deskBounds=deskParts[0].bounds;foreach(var part in deskParts)deskBounds.Encapsulate(part.bounds);
        var openArea=(bedBounds.center+deskBounds.center)*.5f;
        float bedHalfDepth=Mathf.Abs(north.x)*bedBounds.extents.x+Mathf.Abs(north.z)*bedBounds.extents.z;
        float bedHalfWidth=Mathf.Abs(east.x)*bedBounds.extents.x+Mathf.Abs(east.z)*bedBounds.extents.z;
        var bedFoot=bedBounds.center+east*(bedHalfWidth+.36f)-north*(bedHalfDepth+.20f);
        targets.Add(new KeyValuePair<string,Vector3>("Bed_desk_open_area",openArea));
        targets.Add(new KeyValuePair<string,Vector3>("Bed_foot_detail",bedFoot));
        foreach(var station in access.stations)targets.Add(new KeyValuePair<string,Vector3>(station.furniture,station.standing));
        targets.Add(new KeyValuePair<string,Vector3>("Door_return",result.entry));
        float started=Time.realtimeSinceStartup,nextSample=started;int index=0;samples.Add(new Sample{seconds=0,target="Door_entry",position=body.transform.position});
        foreach(var target in targets)
        {
            Vector3 requested=target.Value;requested.y=access.floorHeight+.035f;Vector3 standing;
            bool reachable=BedroomAccessibility.TryNearestReachable(requested,out standing)&&(standing-requested).magnitude<.24f;
            var seg=new Segment{target=target.Key,standing=standing,controllerEnabled=true};float began=Time.realtimeSinceStartup;
            // Only the player's own capsule is disabled for query planning, then enabled
            // before the first movement. Every scene obstacle stays active throughout.
            body.enabled=false;Vector3[] path;
            if(reachable&&target.Key=="Door_return")
            {
                var spur=access.stations.First(s=>s.furniture=="Coatrack").standing;
                var a=BedroomAccessibility.Route(body.transform.position,spur,body.radius,body.height);var b=BedroomAccessibility.Route(spur,standing,body.radius,body.height);
                path=a.Length>0&&b.Length>0?a.Concat(b).ToArray():new Vector3[0];seg.via=new[]{"Coatrack_spur_return"};
            }
            else{path=reachable?BedroomAccessibility.Route(body.transform.position,standing,body.radius,body.height):new Vector3[0];seg.via=new string[0];}
            body.enabled=true;
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
                    seg.moveCalls++;
                    seg.controllerEnabled&=body.enabled;result.newCollidersEnabled&=denseColliders.All(c=>c.enabled&&c.gameObject.activeInHierarchy);
                    seg.travelled+=Vector3.Distance(old,body.transform.position);
                    if(Time.realtimeSinceStartup>=nextSample){samples.Add(new Sample{seconds=Time.realtimeSinceStartup-started,target=target.Key,position=body.transform.position});nextSample=Time.realtimeSinceStartup+.1f;}
                    yield return null;
                }
                if(failed)break;
            }
            var error=standing-body.transform.position;error.y=0;seg.arrivalError=error.magnitude;seg.reached=reachable&&!failed&&error.magnitude<.08f;seg.seconds=Time.realtimeSinceStartup-began;segments.Add(seg);
            samples.Add(new Sample{seconds=Time.realtimeSinceStartup-started,target=target.Key,position=body.transform.position});
            if(target.Key=="Writing")
            {
                result.southReachable=seg.reached;result.usePosition=body.transform.position;body.transform.rotation=Quaternion.LookRotation(north);result.forward=body.transform.forward;result.northFacing=Vector3.Dot(result.forward,north)>.999f;
                var paper=meshes.First(r=>r.name.StartsWith("Writing_Paper_A4_"));var aim=paper.bounds.center;var eye=body.transform.position+Vector3.up*1.62f;var ray=aim-eye;
                result.paperRayOrigin=eye;result.paperRayTarget=aim;result.observerBodyInstanceId=body.GetInstanceID();result.bodyEnabledDuringPaperRay=body.enabled;bool rawHit=Physics.Raycast(eye,ray.normalized,out hit,ray.magnitude-.015f);result.rawPaperRayCollider=rawHit?hit.collider.name:"none";result.rawPaperRayDistance=rawHit?hit.distance:0;result.rawPaperRayHitWasPlayer=rawHit&&hit.collider==body;result.rawPaperRay=rawHit?RayProof(hit,body):null;
                var rayHits=Physics.RaycastAll(eye,ray.normalized,ray.magnitude-.015f,~0,QueryTriggerInteraction.UseGlobal).OrderBy(h=>h.distance).ToArray();
                result.paperRayHitInventory=rayHits.Select(h=>h.collider.name+" | "+h.collider.GetType().FullName+" | "+h.collider.GetInstanceID()+" | "+h.distance.ToString("F6")).ToArray();result.paperRayHits=rayHits.Select(h=>RayProof(h,body)).ToArray();result.selfIgnoredForPaperRay=rayHits.Any(h=>h.collider==body)||result.rawPaperRayHitWasPlayer;
                // Visibility rays ignore only the observer's own capsule. Scene
                // furniture, including every new storage collider, stays active.
                var sceneHits=rayHits.Where(h=>h.collider!=body).ToArray();
                if(rawHit&&hit.collider!=body&&!sceneHits.Any(h=>h.collider==hit.collider&&Mathf.Abs(h.distance-hit.distance)<.000001f))sceneHits=sceneHits.Concat(new[]{hit}).OrderBy(h=>h.distance).ToArray();
                result.scenePaperRayCollider=sceneHits.Length>0?sceneHits[0].collider.name:"none";result.scenePaperRayDistance=sceneHits.Length>0?sceneHits[0].distance:0;
                result.firstNonSelfInstanceId=sceneHits.Length>0?sceneHits[0].collider.GetInstanceID():0;result.firstNonSelfColliderType=sceneHits.Length>0?sceneHits[0].collider.GetType().FullName:"none";
                result.paperSightlineClear=sceneHits.Length==0||sceneHits[0].collider.name.StartsWith("TeaTable_");
                view.transform.localPosition=new Vector3(0,1.02f,0);view.transform.localRotation=Quaternion.Euler(14,0,0);view.fieldOfView=75;yield return Capture(folder,"01_north_window",frames);
                view.transform.localRotation=Quaternion.Euler(55,0,0);view.fieldOfView=52;yield return Capture(folder,"02_paper_pen",frames);
            }
            view.transform.localPosition=new Vector3(0,1.62f,0);view.transform.localRotation=Quaternion.Euler(5,0,0);view.fieldOfView=66;
            if(target.Key=="Bed_foot_detail"||target.Key=="Bed")
            {
                var quilt=meshes.First(r=>r.name.StartsWith("Bed_"+name+(season==1?"_thin_cotton_coverlet":"_quilt")));
                view.transform.localPosition=new Vector3(0,1.02f,0);view.fieldOfView=target.Key=="Bed_foot_detail"?66:56;view.transform.LookAt(quilt.bounds.center);
                yield return Capture(folder,target.Key=="Bed"?"detail_bed_side":"detail_bed_foot",frames);
            }
            string detailGroup=target.Key=="Desk"?"DENSE_LIFE13_CameraBag":target.Key=="East_working_aisle"?"DENSE_LIFE13_OpenTapeArchive":target.Key=="Coatrack"?"DENSE_LIFE13_OldSuitcase":target.Key=="Bed_desk_open_area"?"DENSE_LIFE13_ReadingAtWindow":null;
            if(detailGroup!=null)
            {
                var group=room.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.StartsWith(detailGroup)&&t.GetComponent<MeshRenderer>()==null);
                if(group!=null){var parts=group.GetComponentsInChildren<MeshRenderer>();if(parts.Length>0){Bounds bounds=parts[0].bounds;foreach(var part in parts)bounds.Encapsulate(part.bounds);view.transform.localPosition=new Vector3(0,target.Key=="Desk"?.82f:1.18f,0);view.fieldOfView=44;view.transform.LookAt(bounds.center);yield return Capture(folder,"detail_"+detailGroup,frames);}}
            }
            if(target.Key=="Desk")
            {
                var repair=meshes.FirstOrDefault(r=>r.name.StartsWith("LIFE13_repair_felt_work_mat"));
                if(repair!=null){view.transform.localPosition=new Vector3(0,1.35f,0);view.fieldOfView=44;view.transform.LookAt(repair.bounds.center);yield return Capture(folder,"detail_repair_desk",frames);}
            }
            view.transform.localPosition=new Vector3(0,1.62f,0);view.transform.localRotation=Quaternion.Euler(5,0,0);view.fieldOfView=66;
            if((target.Key=="East_working_aisle"||target.Key=="Bed_desk_open_area")){body.transform.rotation=Quaternion.LookRotation(north);yield return Capture(folder,(3+index).ToString("D2")+"_"+target.Key,frames);}
            else if(target.Key!="Writing")
            {
                var prefix=target.Key=="Door_return"?"Door_leaf":target.Key+"_";var furniture=meshes.FirstOrDefault(r=>r.name.StartsWith(prefix));
                if(furniture!=null)view.transform.LookAt(furniture.bounds.center);
                yield return Capture(folder,(3+index).ToString("D2")+"_arrived_"+target.Key,frames);
            }
            if(target.Key=="Coatrack")BedroomGarmentEvidence.Write(folder,body);
            index++;
        }
        result.returnPosition=body.transform.position;result.continuousFromDoor=segments.Count==11&&segments.All(s=>s.reached&&s.controllerEnabled);
        var returned=body.transform.position-result.entry;returned.y=0;result.returnedToDoor=returned.magnitude<.24f&&segments.Last().reached;
        result.passed=result.continuousFromDoor&&result.returnedToDoor&&result.newCollidersEnabled&&result.sixFurnitureReachable&&result.sharedProps&&result.southReachable&&result.tableCollision&&result.northFacing&&result.paperSightlineClear;
        if(!result.passed)result.error="One or more actual route, collider or writing checks failed; inspect segments and accessibility.";
        result.segments=segments.ToArray();result.samples=samples.ToArray();result.frames=frames.ToArray();File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));
        Debug.Log("BEDROOM_DENSITY_V17_"+(result.passed?"PASS":"FAIL")+" "+name+" segments="+segments.Count+" colliders="+result.newColliderCount);
        if(season<3)SceneManager.LoadScene("Bedroom_"+new[]{"Spring","Summer","Autumn","Winter"}[season+1]);else Application.Quit(result.passed?0:2);
    }
    static IEnumerator Capture(string folder,string name,List<string> frames)
    {
        yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));frames.Add(name+".png");yield return new WaitForSecondsRealtime(.25f);
    }
    static RayEvidence RayProof(RaycastHit hit,CharacterController body){return new RayEvidence{name=hit.collider.name,type=hit.collider.GetType().FullName,instanceId=hit.collider.GetInstanceID(),distance=hit.distance,point=hit.point,observerSelf=hit.collider==body};}
}

