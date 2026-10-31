using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dedicated enabled listener: the screenshot coroutine disables BedroomWalk.Update.
public sealed class CompactWallContactRecorder : MonoBehaviour {
    public bool Recording;public readonly List<string> Names=new List<string>();
    void OnControllerColliderHit(ControllerColliderHit hit){if(Recording&&!Names.Contains(hit.collider.name))Names.Add(hit.collider.name);}
}
public sealed class BedroomWalk : MonoBehaviour
{
    public Camera View;
    public int SeasonIndex;
    CharacterController body;
    float yaw,pitch,vertical;
    bool help;
    static bool restoreSeasonView;
    static Vector3 seasonViewPosition;
    static float seasonViewYaw,seasonViewPitch,seasonViewHeight;
    static readonly string[] Seasons={"Spring","Summer","Autumn","Winter"};
    readonly List<CaptureViewpoint> captureViewpoints=new List<CaptureViewpoint>();
    void Start()
    {
        body=GetComponent<CharacterController>();yaw=transform.eulerAngles.y;pitch=View.transform.localEulerAngles.x;if(pitch>180)pitch-=360;
        if(restoreSeasonView)
        {
            body.enabled=false;transform.position=seasonViewPosition;body.enabled=true;
            yaw=seasonViewYaw;pitch=seasonViewPitch;transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            View.transform.localPosition=new Vector3(0,seasonViewHeight,0);
            restoreSeasonView=false;
        }
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-style-v16")>=0) { enabled=false; StartCoroutine(BedroomStyleDiagnostics.Run(View,SeasonIndex)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-density-v17")>=0) { enabled=false; StartCoroutine(BedroomDensityQA.Run(View,SeasonIndex,body)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-writing-v11")>=0) { enabled=false; StartCoroutine(BedroomWritingQA.Run(View,SeasonIndex,body)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-walkthrough")>=0) { enabled=false; StartCoroutine(BedroomWalkthrough.Run(View,SeasonIndex,body)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-lighting-diagnostic")>=0) { enabled=false; StartCoroutine(BedroomLightingDiagnostics.Run(View,SeasonIndex)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-root-diagnostic")>=0) { enabled=false; StartCoroutine(BedroomRootDiagnostics.Run(View,SeasonIndex)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-foliage-diagnostic")>=0) { enabled=false; StartCoroutine(BedroomFoliageDiagnostics.Run(View,SeasonIndex)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-material-diagnostic")>=0) { enabled=false; StartCoroutine(BedroomMaterialDiagnostics.Run(View)); }
        else if(Array.IndexOf(Environment.GetCommandLineArgs(),"--bedroom-qa")>=0)StartCoroutine(Capture());
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){Cursor.lockState=Cursor.lockState==CursorLockMode.Locked?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Cursor.lockState!=CursorLockMode.Locked;}
        if(Input.GetMouseButtonDown(0)&&Cursor.lockState==CursorLockMode.None){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        if(Input.GetKeyDown(KeyCode.H))help=!help;
        for(int i=0;i<4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))&&i!=SeasonIndex)
        {
            seasonViewPosition=transform.position;seasonViewYaw=yaw;seasonViewPitch=pitch;seasonViewHeight=View.transform.localPosition.y;restoreSeasonView=true;
            SceneManager.LoadScene("Bedroom_"+Seasons[i]);return;
        }
        if(Cursor.lockState!=CursorLockMode.Locked)return;
        yaw+=Input.GetAxisRaw("Mouse X")*1.7f;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.7f,-83,83);
        transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
        bool crouch=Input.GetKey(KeyCode.C);View.transform.localPosition=Vector3.Lerp(View.transform.localPosition,new Vector3(0,crouch?1.02f:1.62f,0),Time.deltaTime*9);
        Vector3 direction=transform.forward*Input.GetAxisRaw("Vertical")+transform.right*Input.GetAxisRaw("Horizontal");
        if(direction.sqrMagnitude>1)direction.Normalize();
        if(body.isGrounded&&vertical<0)vertical=-1;vertical-=9.81f*Time.deltaTime;
        body.Move((direction*(Input.GetKey(KeyCode.LeftShift)?2.3f:1.35f)+Vector3.up*vertical)*Time.deltaTime);
    }
    void OnGUI()
    {
        if(!help)return;
        GUI.Box(new Rect(18,18,470,84),Seasons[SeasonIndex]+" | WASD / Mouse | C crouch | Shift walk faster\n1 Spring   2 Summer   3 Autumn   4 Winter\nEsc release cursor | H close help");
    }
    IEnumerator Capture()
    {
        enabled=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","QA/cloth-v17/native"));Directory.CreateDirectory(folder);
        var drift=FindObjectOfType<AutumnLeafDrift>();Transform leaf=drift!=null&&drift.Leaves.Length>0?drift.Leaves[0]:null;
        Vector3 leafStart=leaf!=null?leaf.position:Vector3.zero;
        var fan=FindObjectOfType<SeasonalFanSpin>();Quaternion fanStart=fan!=null?fan.transform.rotation:Quaternion.identity;
        Vector3 entry=transform.position;
        yield return new WaitForSecondsRealtime(1.0f);
        float leafTravel=leaf!=null?Vector3.Distance(leaf.position,leafStart):0;
        float fanTravel=fan!=null?Quaternion.Angle(fanStart,fan.transform.rotation):0;
        var compactRoom=GameObject.Find("Bedroom_"+Seasons[SeasonIndex]+"_Editable");
        Bounds compactFloor=new Bounds();bool compactGotFloor=false;
        foreach(var renderer in compactRoom.GetComponentsInChildren<MeshRenderer>())if(renderer.name.StartsWith("Floorboard_")){if(!compactGotFloor){compactFloor=renderer.bounds;compactGotFloor=true;}else compactFloor.Encapsulate(renderer.bounds);}
        if(!compactGotFloor)throw new InvalidOperationException("COMPACT_V15_MISSING_FLOOR_BOUNDS");
        var compactAccess=BedroomAccessibility.Check(compactRoom,body,entry);
        var compactWallAttempts=new List<CompactWallAttempt>();
        string compactNorthFirstObstacle=CompactFirstObstacle(Vector3.back);
        CompactWallColliderProof[] compactWallQueries;bool compactWallsQueried=CompactQueryWalls(compactRoom,out compactWallQueries);
        yield return CompactProbeWall(compactFloor,Vector3.forward,"south",compactWallAttempts);
        bool southBlocked=compactAccess.passed&&compactWallAttempts[0].passed;
        body.enabled=false;transform.position=entry;body.enabled=true;Physics.SyncTransforms();
        bool leafPass=SeasonIndex!=2||leafTravel>.02f;
        var meshRenderers=FindObjectsOfType<MeshRenderer>();int mapped=0;
        foreach(var renderer in meshRenderers)if(renderer.lightmapIndex>=0&&renderer.lightmapIndex<LightmapSettings.lightmaps.Length)mapped++;
        int wainscotProbes=0,wainscotMapped=0;
        foreach(var renderer in meshRenderers)if(renderer.name.StartsWith("Wainscot_side")||renderer.name.StartsWith("North_wainscot")||renderer.name.StartsWith("South_wainscot"))
        {bool isMapped=renderer.lightmapIndex>=0&&renderer.lightmapIndex<LightmapSettings.lightmaps.Length;if(isMapped)wainscotMapped++;else if(renderer.lightProbeUsage==UnityEngine.Rendering.LightProbeUsage.BlendProbes&&renderer.probeAnchor!=null)wainscotProbes++;}
        var response=View.GetComponent<BedroomColorResponse>();
        var report=new RuntimeCheck{compactWallAttempts=compactWallAttempts.ToArray(),compactWallQueries=compactWallQueries,southWallMoveVerified=southBlocked,allWallCollidersQueriedAndEnabled=compactWallsQueried,northWallMoveTested=false,sideWallsMoveTested=false,northwardFirstObstacle=compactNorthFirstObstacle,wallVerificationScope="Actual CharacterController.Move contact with reachable south wall/wainscot only. North and side wall Move contact not tested because full-size furniture blocks accessible approaches. All ten new room-wall/window-sill colliders separately checked enabled with direct Collider.Raycast; those queries do not claim standing reachability.",compactFloorMin=compactFloor.min,compactFloorMax=compactFloor.max,wainscotProbeReceivers=wainscotProbes,wainscotMappedReceivers=wainscotMapped,unity=Application.unityVersion,season=Seasons[SeasonIndex],renderers=meshRenderers.Length,wallsBlockWalking=southBlocked&&compactWallsQueried,autumnMotionApplicable=SeasonIndex==2,autumnLeafTravel=leafTravel,passed=southBlocked&&compactWallsQueried&&leafPass,lightmapTextures=LightmapSettings.lightmaps.Length,lightmappedObjects=mapped,lightProbes=LightmapSettings.lightProbes!=null?LightmapSettings.lightProbes.count:0,hdrColorResponse=response!=null&&response.Profile!=null&&response.Profile.shader.isSupported,antiAliasing=QualitySettings.antiAliasing,linearColor=QualitySettings.activeColorSpace==ColorSpace.Linear};
        report.passed=report.passed&&wainscotProbes==5&&wainscotMapped==0&&report.lightmapTextures>0&&report.lightmappedObjects>0&&report.lightProbes>0&&report.hdrColorResponse&&report.linearColor&&report.antiAliasing>=4;
        report.summerFanMotionApplicable=SeasonIndex==1;report.summerFanDegrees=fanTravel;
        if(SeasonIndex==1)report.passed=report.passed&&fan!=null&&fanTravel>5;
        var room=GameObject.Find("Bedroom_"+Seasons[SeasonIndex]+"_Editable");
        UnityEngine.Rendering.SphericalHarmonicsL2 irradiance;LightProbes.GetInterpolatedProbe(new Vector3(0,1.4f,0),null,out irradiance);
        var probeColors=new Color[1];irradiance.Evaluate(new[]{Vector3.up},probeColors);report.centralProbeLuminance=probeColors[0].grayscale;
        LightProbes.GetInterpolatedProbe(new Vector3(-.60f,2.0f,-5.0f),null,out irradiance);irradiance.Evaluate(new[]{Vector3.up},probeColors);report.exteriorTreeProbeLuminance=probeColors[0].grayscale;
        report.lightProbeProxyVolumes=FindObjectsOfType<LightProbeProxyVolume>().Length;report.proxyVolumesSupported=LightProbeProxyVolume.isFeatureSupported;
        report.expectedLightProbeProxyVolumes=SeasonIndex==1?0:1;
        report.beddingIndirectLighting=SeasonIndex==1?"Directional lightmap for the thin cotton coverlet":"Per-pixel Light Probe Proxy Volume for the folded quilt";
        report.passed&=report.centralProbeLuminance>.001f&&report.lightProbeProxyVolumes==report.expectedLightProbeProxyVolumes&&report.proxyVolumesSupported;
        report.accessibility=BedroomAccessibility.Check(room,body,entry);report.passed&=report.accessibility.passed;
        captureViewpoints.Clear();captureViewpoints.Add(new CaptureViewpoint{name="entry",eye=View.transform.position,fieldOfView=View.fieldOfView,reachableStanding=report.accessibility.entryClear});
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Unity_"+Seasons[SeasonIndex]+".png"));
        yield return new WaitForSecondsRealtime(.5f);
        // These are real player renders, at physically plausible eye locations, with the delivered camera.
        body.enabled=false;
        yield return CapturePose(folder,"reverse",new Vector3(-1.25f,1.62f,-1.48f),new Vector3(.25f,1.20f,1.55f),66);
        // v17 garment inspection cameras; controller stays disabled in this QA-only section.
        string cloth=Path.Combine(folder,"cloth",Seasons[SeasonIndex]);Directory.CreateDirectory(cloth);
        float clothBottom=new[]{.91f,1.015f,.89f,.755f}[SeasonIndex];
        float clothFrontHeight=SeasonIndex==3?1.18f:1.30f;
        float clothCuff=SeasonIndex==1?1.295f:SeasonIndex==3?1.005f:1.065f;
        yield return InspectPose(cloth,"front",new Vector3(1.55f,clothFrontHeight,.40f),new Vector3(1.55f,clothFrontHeight,1.665f),48);
        yield return InspectPose(cloth,"side",new Vector3(.72f,1.40f,1.30f),new Vector3(1.55f,1.31f,1.665f),48);
        yield return InspectPose(cloth,"support",new Vector3(1.55f,1.75f,1.13f),new Vector3(1.55f,1.57f,1.665f),36);
        yield return InspectPose(cloth,"cuff",new Vector3(1.13f,clothCuff+.10f,1.16f),new Vector3(1.25f,clothCuff+.07f,1.665f),36);
        yield return InspectPose(cloth,"hem",new Vector3(1.53f,clothBottom+.17f,1.05f),new Vector3(1.55f,clothBottom+.075f,1.665f),42);
        Vector3 desk=Target("Desk_solid_walnut_worktop",new Vector3(-1.8f,.8f,-1.4f));
        yield return CapturePose(folder,"desk",new Vector3(desk.x*.30f,1.40f,desk.z+.95f),desk+Vector3.up*.15f,49);
        Vector3 photos=Target("Photo_original_print_",new Vector3(2.0f,1.5f,-1.35f));
        yield return CapturePose(folder,"bedwall",new Vector3(photos.x*.10f,1.54f,photos.z+1.0f),photos,58);
        Vector3 easel=Target("Easel_canvas",new Vector3(1.65f,1.25f,.7f));
        yield return CapturePose(folder,"easel",new Vector3(easel.x*.12f,1.55f,easel.z+.65f),easel,59);
        yield return CapturePose(folder,"window",new Vector3(.10f,1.60f,-1.35f),new Vector3(-.10f,1.90f,-3.6f),58);
        yield return SampleWindowPerformance(report);
        // A closer lens at the same accessible indoor standing position exposes
        // real bark/branch junctions and leaf edges without moving through glass.
        yield return CapturePose(folder,"tree_detail",new Vector3(-.48f,1.60f,-1.35f),new Vector3(-.60f,1.70f,-5.0f),14.25f);
        yield return CapturePose(folder,"leaf_detail",new Vector3(-.48f,1.60f,-1.35f),new Vector3(-.60f,2.75f,-5.0f),16.0f);
        if(SeasonIndex==1)
        {
            // Dedicated inspection cameras. These do not add to the eight
            // reachable first-person poses and may be outdoors or below eye level.
            string contact=Path.Combine(folder,"cloth-v17/contact");Directory.CreateDirectory(contact);
            yield return InspectPose(contact,"root",new Vector3(-.65f,.42f,-3.65f),new Vector3(-.60f,.13f,-5),33.4f);
            yield return InspectPose(contact,"branch",new Vector3(-1.10f,1.80f,-3.90f),new Vector3(-.60f,1.72f,-5),31.0f);
            yield return InspectPose(contact,"chair",new Vector3(-.35f,.69f,-1.03f),new Vector3(-1.04f,.47f,-1.55f),32.0f);
            yield return InspectPose(contact,"chair_feet",new Vector3(-.35f,.22f,-.95f),new Vector3(-1.04f,.07f,-1.55f),38.2f);
        }
        MeshRenderer roots=null,trunk=null;
        foreach(var renderer in meshRenderers){if(renderer.name.StartsWith("MainTree_SHARED_Roots"))roots=renderer;if(renderer.name.StartsWith("MainTree_SHARED_Trunk"))trunk=renderer;}
        report.rootProbeContinuous=roots!=null&&trunk!=null&&roots.probeAnchor!=null&&trunk.probeAnchor!=null&&Vector3.Distance(roots.probeAnchor.position,trunk.probeAnchor.position)<.00001f;
        if(roots!=null&&roots.probeAnchor!=null){report.rootProbeAnchor=roots.probeAnchor.position;LightProbes.GetInterpolatedProbe(roots.probeAnchor.position,roots,out irradiance);irradiance.Evaluate(new[]{Vector3.up},probeColors);report.rootProbeLuminance=probeColors[0].grayscale;}
        report.passed&=report.rootProbeContinuous&&report.rootProbeLuminance>.001f;
        report.captureViewpoints=captureViewpoints.ToArray();report.captureViewpointsValidated=true;
        foreach(var viewpoint in report.captureViewpoints)report.captureViewpointsValidated&=viewpoint.reachableStanding;
        report.passed&=report.captureViewpointsValidated;
        BedroomGarmentEvidence.Write(folder,body);
        File.WriteAllText(Path.Combine(folder,"Unity_"+Seasons[SeasonIndex]+"_runtime.json"),JsonUtility.ToJson(report,true));
        if(!report.passed)Debug.LogError("BEDROOM_RUNTIME_CHECK_FAIL "+Seasons[SeasonIndex]);
        else Debug.Log("BEDROOM_RUNTIME_PASS "+Seasons[SeasonIndex]+" "+captureViewpoints.Count+" reachable capture positions; window median "+report.windowPerformance.medianFrameMs.ToString("F2")+" ms, p95 "+report.windowPerformance.p95FrameMs.ToString("F2")+" ms");
        if(SeasonIndex<3)SceneManager.LoadScene("Bedroom_"+Seasons[SeasonIndex+1]);else Application.Quit();
    }
    IEnumerator SampleWindowPerformance(RuntimeCheck report)
    {
        // This records the delivered viewer at its normal quality/vsync settings.
        // It is a measured frame-time sample, not an estimate from screenshot timing.
        const int warmup=30,samples=120;
        for(int i=0;i<warmup;i++)yield return new WaitForEndOfFrame();
        var times=new float[samples];double started=Time.realtimeSinceStartupAsDouble;
        for(int i=0;i<samples;i++){yield return new WaitForEndOfFrame();times[i]=Time.unscaledDeltaTime*1000f;}
        double elapsed=Time.realtimeSinceStartupAsDouble-started;
        Array.Sort(times);float sum=0;foreach(float time in times)sum+=time;
        report.windowPerformance=new PerformanceSample{view="window",gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height,warmupFrames=warmup,sampleFrames=samples,vSyncCount=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,antiAliasing=QualitySettings.antiAliasing,elapsedSeconds=(float)elapsed,medianFrameMs=(times[59]+times[60])*.5f,p95FrameMs=times[Mathf.CeilToInt(samples*.95f)-1],meanFrameMs=sum/samples,minFrameMs=times[0],maxFrameMs=times[samples-1],eye=View.transform.position,fieldOfView=View.fieldOfView};
    }
    [Serializable]public class CompactWallAttempt {
        public string wall,error,firstCastCollider;public bool passed,routeReached,controllerEnabledBefore,controllerEnabledAfter,wallsEnabledBefore,wallsEnabledAfter,actualArchitectureContact;
        public Vector3 routeStart,requestedStanding,before,after,direction,requestedMove;public float castDistance,actualOutwardMetres,capsuleRadius,capsuleHeight,skinWidth;
        public int routeMoveCalls,collisionFlags;public string[] actualHitColliders,wallColliderNames;
    }
    static bool CompactNameV15(string actual,string semantic){return actual==semantic||actual.StartsWith(semantic+".",StringComparison.Ordinal);}
    [Serializable]public class CompactWallColliderProof {public string name,actualName,type;public int instanceId;public bool enabled,positiveSize,queryHit,passed;public Vector3 boundsMin,boundsMax,rayOrigin,rayDirection,hitPoint;public float hitDistance;}
    string CompactFirstObstacle(Vector3 direction)
    {
        Vector3 center=transform.position+body.center;float half=body.height*.5f-body.radius;RaycastHit first=new RaycastHit();bool got=false;
        foreach(var hit in Physics.CapsuleCastAll(center-Vector3.up*half,center+Vector3.up*half,body.radius,direction,8f,~0,QueryTriggerInteraction.Ignore))if(hit.collider!=body&&(!got||hit.distance<first.distance)){first=hit;got=true;}
        return got?first.collider.name:"none";
    }
    bool CompactQueryWalls(GameObject room,out CompactWallColliderProof[] output)
    {
        var expected=new[]{"West_wall","East_wall","North_left","North_right","North_below","North_above","South_left","South_right","South_lintel","Window_deep_sill"};
        var rows=new List<CompactWallColliderProof>();bool passed=true;
        foreach(var name in expected)
        {
            Collider collider=null;int matches=0;foreach(var item in room.GetComponentsInChildren<Collider>())if(CompactNameV15(item.name,name)){collider=item;matches++;}
            var proof=new CompactWallColliderProof{name=name};rows.Add(proof);
            if(matches!=1||collider==null){passed=false;continue;}
            proof.actualName=collider.name;proof.type=collider.GetType().Name;proof.instanceId=collider.GetInstanceID();proof.enabled=collider.enabled&&collider.gameObject.activeInHierarchy;
            var bounds=collider.bounds;proof.boundsMin=bounds.min;proof.boundsMax=bounds.max;proof.positiveSize=bounds.size.x>0&&bounds.size.y>0&&bounds.size.z>0;
            int axis=bounds.size.x<=bounds.size.y&&bounds.size.x<=bounds.size.z?0:bounds.size.y<=bounds.size.z?1:2;
            var direction=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;proof.rayDirection=direction;proof.rayOrigin=bounds.center-direction*(bounds.extents[axis]+.25f);
            RaycastHit hit;proof.queryHit=collider.Raycast(new Ray(proof.rayOrigin,direction),out hit,bounds.size[axis]+.50f);
            if(proof.queryHit){proof.hitPoint=hit.point;proof.hitDistance=hit.distance;}
            proof.passed=proof.enabled&&proof.positiveSize&&proof.queryHit;passed&=proof.passed;
        }
        output=rows.ToArray();return passed;
    }
    bool CompactArchitecture(string name,Vector3 direction){return direction.z>.5f?name.StartsWith("South_"):direction.z<-.5f?(name.StartsWith("North_")||CompactNameV15(name,"Window_deep_sill")):(CompactNameV15(name,"West_wall")||CompactNameV15(name,"East_wall"));}
    IEnumerator CompactProbeWall(Bounds floor,Vector3 direction,string label,List<CompactWallAttempt> output)
    {
        var proof=new CompactWallAttempt{wall=label,direction=direction,routeStart=transform.position,capsuleRadius=body.radius,capsuleHeight=body.height,skinWidth=body.skinWidth};
        output.Add(proof);var walls=new List<Collider>();var names=new List<string>();
        foreach(var collider in FindObjectsOfType<Collider>())if(CompactArchitecture(collider.name,direction)){walls.Add(collider);names.Add(collider.name);}
        proof.wallColliderNames=names.ToArray();proof.wallsEnabledBefore=walls.Count>0;foreach(var wall in walls)proof.wallsEnabledBefore&=wall.enabled&&wall.gameObject.activeInHierarchy;
        Vector3[] bestPath=null;float bestDistance=float.MaxValue;Vector3 bestStanding=Vector3.zero;RaycastHit bestHit=new RaycastHit();
        for(int sample=0;sample<=32;sample++)
        {
            float t=sample/32f;Vector3 preferred=Mathf.Abs(direction.z)>.5f?new Vector3(Mathf.Lerp(floor.min.x+.20f,floor.max.x-.20f,t),floor.max.y+.035f,direction.z>0?floor.max.z-body.radius-body.skinWidth-.10f:floor.min.z+body.radius+body.skinWidth+.10f):new Vector3(direction.x>0?floor.max.x-body.radius-body.skinWidth-.10f:floor.min.x+body.radius+body.skinWidth+.10f,floor.max.y+.035f,Mathf.Lerp(floor.min.z+.20f,floor.max.z-.20f,t));
            Vector3 standing;if(!BedroomAccessibility.TryNearestReachable(preferred,out standing))continue;
            var center=standing+body.center;float half=body.height*.5f-body.radius;
            RaycastHit first=new RaycastHit();bool got=false;
            foreach(var hit in Physics.CapsuleCastAll(center-Vector3.up*half,center+Vector3.up*half,body.radius,direction,8f,~0,QueryTriggerInteraction.Ignore))if(hit.collider!=body&&(!got||hit.distance<first.distance)){first=hit;got=true;}
            if(!got||!CompactArchitecture(first.collider.name,direction)||first.distance>1.10f||!first.collider.enabled)continue;
            bool wasEnabled=body.enabled;Vector3[] path;body.enabled=false;
            try{path=BedroomAccessibility.Route(transform.position,standing,body.radius,body.height);}finally{body.enabled=wasEnabled;}
            if(path.Length==0)continue;float cost=path.Length+first.distance;
            if(cost<bestDistance){bestDistance=cost;bestPath=path;bestStanding=standing;bestHit=first;}
        }
        if(bestPath==null){proof.error="No reachable standing point whose first outward capsule obstacle is an enabled actual requested room wall interface";yield break;}
        proof.requestedStanding=bestStanding;proof.firstCastCollider=bestHit.collider.name;proof.castDistance=bestHit.distance;
        bool reached=true;
        foreach(var waypoint in bestPath)
        {
            float deadline=Time.realtimeSinceStartup+3f;
            while(true)
            {
                var delta=waypoint-transform.position;delta.y=0;if(delta.magnitude<.025f)break;
                if(Time.realtimeSinceStartup>deadline){reached=false;break;}
                body.Move(delta.normalized*Mathf.Min(delta.magnitude,1.35f*Time.unscaledDeltaTime)+Vector3.down*.03f);proof.routeMoveCalls++;yield return null;
            }
            if(!reached)break;
        }
        var error=transform.position-bestStanding;error.y=0;proof.routeReached=reached&&error.magnitude<.24f;
        proof.before=transform.position;proof.controllerEnabledBefore=body.enabled;proof.requestedMove=direction*8;
        var recorder=GetComponent<CompactWallContactRecorder>();if(recorder==null)recorder=gameObject.AddComponent<CompactWallContactRecorder>();recorder.enabled=true;recorder.Names.Clear();recorder.Recording=true;CollisionFlags flags;
        try{flags=body.Move(proof.requestedMove);}finally{recorder.Recording=false;}
        Physics.SyncTransforms();proof.after=transform.position;proof.controllerEnabledAfter=body.enabled;proof.actualHitColliders=recorder.Names.ToArray();proof.collisionFlags=(int)flags;
        proof.actualOutwardMetres=Vector3.Dot(proof.after-proof.before,direction);
        foreach(var name in recorder.Names)proof.actualArchitectureContact|=CompactArchitecture(name,direction);
        proof.wallsEnabledAfter=walls.Count>0;foreach(var wall in walls)proof.wallsEnabledAfter&=wall.enabled&&wall.gameObject.activeInHierarchy;
        float margin=body.radius-2*body.skinWidth-.02f;
        bool inside=proof.after.x>=floor.min.x+margin&&proof.after.x<=floor.max.x-margin&&proof.after.z>=floor.min.z+margin&&proof.after.z<=floor.max.z-margin;
        proof.passed=proof.routeReached&&proof.controllerEnabledBefore&&proof.controllerEnabledAfter&&proof.wallsEnabledBefore&&proof.wallsEnabledAfter&&proof.actualArchitectureContact&&(flags&CollisionFlags.Sides)!=0&&proof.actualOutwardMetres<2f&&inside;
        if(!proof.passed)proof.error="Actual Move did not establish enabled architecture-side contact and containment";
    }
    IEnumerator CapturePose(string folder,string suffix,Vector3 eye,Vector3 target,float fov)
    {
        Vector3 standing;bool reachable=BedroomAccessibility.TryNearestReachable(eye,out standing);
        if(reachable){eye.x=standing.x;eye.z=standing.z;}
        captureViewpoints.Add(new CaptureViewpoint{name=suffix,eye=eye,fieldOfView=fov,reachableStanding=reachable});
        View.transform.position=eye;View.transform.LookAt(target);View.fieldOfView=fov;
        yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Unity_"+Seasons[SeasonIndex]+"_"+suffix+".png"));
        yield return new WaitForSecondsRealtime(.15f);
    }
    IEnumerator InspectPose(string folder,string subject,Vector3 eye,Vector3 target,float fov)
    {
        View.transform.position=eye;View.transform.LookAt(target);View.fieldOfView=fov;
        yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"native_"+Seasons[SeasonIndex].ToLowerInvariant()+"_"+subject+".png"));
        yield return new WaitForSecondsRealtime(.25f);
    }
    Vector3 Target(string prefix,Vector3 fallback)
    {
        Vector3 sum=Vector3.zero;int count=0;
        foreach(var r in FindObjectsOfType<MeshRenderer>())if(r.name.StartsWith(prefix)){sum+=r.bounds.center;count++;}
        return count>0?sum/count:fallback;
    }
    [Serializable]class CaptureViewpoint{public string name;public Vector3 eye;public float fieldOfView;public bool reachableStanding;}
    [Serializable]class PerformanceSample{public string view,gpu,graphicsApi;public int width,height,warmupFrames,sampleFrames,vSyncCount,targetFrameRate,antiAliasing;public float elapsedSeconds,medianFrameMs,p95FrameMs,meanFrameMs,minFrameMs,maxFrameMs,fieldOfView;public Vector3 eye;}
    [Serializable]class RuntimeCheck{public CompactWallAttempt[] compactWallAttempts;public CompactWallColliderProof[] compactWallQueries;public bool southWallMoveVerified,allWallCollidersQueriedAndEnabled,northWallMoveTested,sideWallsMoveTested;public string northwardFirstObstacle,wallVerificationScope;public Vector3 compactFloorMin,compactFloorMax;public string unity,season,beddingIndirectLighting;public int wainscotProbeReceivers,wainscotMappedReceivers,renderers,lightmapTextures,lightmappedObjects,lightProbes,antiAliasing,lightProbeProxyVolumes,expectedLightProbeProxyVolumes;public bool wallsBlockWalking,autumnMotionApplicable,summerFanMotionApplicable,passed,hdrColorResponse,linearColor,captureViewpointsValidated,proxyVolumesSupported,rootProbeContinuous;public float autumnLeafTravel,summerFanDegrees,centralProbeLuminance,exteriorTreeProbeLuminance,rootProbeLuminance;public Vector3 rootProbeAnchor;public BedroomAccessibility.Report accessibility;public CaptureViewpoint[] captureViewpoints;public PerformanceSample windowPerformance;}
}

