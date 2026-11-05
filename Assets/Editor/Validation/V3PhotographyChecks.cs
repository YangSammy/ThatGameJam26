using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Controller and persistence probes; not native pointer acceptance.
    public static class V3PhotographyChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Hidden).Invoke(o,args);
        static object Field(object o,string name)=>o.GetType().GetField(name,Hidden).GetValue(o);
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Hidden).SetValue(o,value);
        static void Check(List<string> lines,bool ok,string message)=>lines.Add((ok?"PASS ":"FAIL ")+message);
        public static string InstrumentPointerContract()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || p==null)throw new InvalidOperationException("Park Play required");
            var lines=new List<string>();var original=p.PhotoMode;
            try
            {
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Body,ParkPhotoPrototype.PhotographyMode.Meter})
                {
                    Call(p,"SetPhotographyMode",mode);Call(p,"SyncParkInput");
                    Check(lines,Cursor.visible && Cursor.lockState==CursorLockMode.None,mode+": precise manipulation uses visible unlocked pointer");
                    Set(p,"virtualPointer",Vector2.zero);Call(p,"TickPhotographyModes",0f);
                    var expected=new Vector2(Mathf.Clamp(Input.mousePosition.x,0,Screen.width),Mathf.Clamp(Input.mousePosition.y,0,Screen.height));
                    Check(lines,Vector2.Distance(p.TooltipPoint,expected)<1,mode+": tooltip and picking use actual mouse position");
                }
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Exploration,ParkPhotoPrototype.PhotographyMode.Viewfinder})
                {
                    Call(p,"SetPhotographyMode",mode);Call(p,"SyncParkInput");
                    Check(lines,!Cursor.visible && Cursor.lockState==CursorLockMode.Locked,mode+": look and edge controls remain locked");
                }
            }
            finally{Call(p,"SetPhotographyMode",original);}
            return string.Join("\n",lines);
        }
        public static string MeadowPointerContract()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || p==null || !(bool)p.GetType().GetProperty("PhotoHandPointerActive",Hidden).GetValue(p,null))
                throw new InvalidOperationException("Prepared meadow offer-hand in Viewfinder required");
            var lines=new List<string>();var roll=(ParkFilmRoll)Field(p,"roll");var task=roll.Task("meadow");
            bool joined=task.handJoined;int frames=p.ExposedFrames;
            try
            {
                Call(p,"SyncParkInput");
                Check(lines,!Cursor.visible && Cursor.lockState==CursorLockMode.Locked,"Offered hand preserves locked mouse-look");
                var rotation=p.view.transform.rotation;
                p.view.transform.LookAt(p.PhotoHandPosition);
                Check(lines,(bool)p.GetType().GetProperty("PointerOnHand",Hidden).GetValue(p,null),"Centre aimed at palm selects hand");
                Set(p,"virtualPointer",Vector2.zero);Call(p,"TickPhotographyModes",0f);
                Check(lines,(int)Field(p,"edgeCandidate")==-1,"Palm hover suppresses conflicting edge gestures");
                p.view.transform.rotation=rotation;
                task.handJoined=true;Call(p,"SyncParkInput");
                Check(lines,Cursor.lockState==CursorLockMode.Locked,"After joining hands the viewfinder regains mouse-look");
                Check(lines,p.ExposedFrames==frames,"Pointer transition alone consumes no film");
            }
            finally{task.handJoined=joined;Call(p,"SyncParkInput");}
            return string.Join("\n",lines);
        }
        public static string BufferedLoadingGestures()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null || p.ExposedFrames!=0)
                throw new InvalidOperationException("Isolated unexposed Park Play required");
            var lines=new List<string>();var capture=p.GetType().GetMethod("CaptureBodyPointer",Hidden);
            Check(lines,capture!=null,"Mechanical loading preserves press/release event rays between Updates");
            if(capture==null)return string.Join("\n",lines);
            var original=ParkFilmStore.Load(p.CurrentFilmId).cameraLoading;bool enabled=p.enabled;
            try
            {
                p.enabled=false;Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);
                Set(p,"inputGate",false);Set(p,"focused",true);p.InputContext.Select(InputContextId.ParkBody,PlayerControlState.Operation);p.InputContext.ContinueGesture();
                var parts=(Dictionary<string,Transform>)Field(p,"instrumentParts");
                Action<int> stage=step=>{Set(p,"roll",ParkFilmStore.SaveCameraLoading(p.CurrentFilmId,ParkCameraLoading.AtStep(step,400)));Set(p,"loadingStep",step);Call(p,"SyncInstruments");Physics.SyncTransforms();};
                Func<string,Vector2> point=id=>{Vector3 v=p.view.WorldToScreenPoint(parts[id].position);return new Vector2(v.x,Screen.height-v.y);};
                Action<bool,Vector2> pointer=(down,position)=>capture.Invoke(p,new object[]{new Event{type=down?EventType.MouseDown:EventType.MouseUp,button=0,mousePosition=position}});
                Action<string,string> drop=(from,to)=>{pointer(true,point(from));pointer(false,point(to));Call(p,"HandleBody");Call(p,"SyncInstruments");};
                stage(1);drop("film-400","chamber");
                Check(lines,(int)Field(p,"loadingStep")==2 && p.FilmIso==400,"Fast cartridge drag retains initial hit and release destination");
                stage(2);drop("leader","spool");
                Check(lines,(int)Field(p,"loadingStep")==4,"Fast leader drag reaches spool without polling the destination as its start");
                stage(1);pointer(true,point("film-100"));pointer(false,Vector2.zero);Call(p,"HandleBody");
                Check(lines,(int)Field(p,"loadingStep")==1 && Field(p,"dragPart")==null,"Invalid drop leaves loading unchanged and releases held cartridge");
                stage(1);pointer(true,point("film-200"));Call(p,"CancelInstrumentDrag");Call(p,"HandleBody");
                Check(lines,(int)Field(p,"loadingStep")==1 && Field(p,"dragPart")==null,"Cancel clears pending press so it cannot restart next frame");
            }
            finally
            {
                Call(p,"CancelInstrumentDrag");Set(p,"roll",ParkFilmStore.SaveCameraLoading(p.CurrentFilmId,original));Set(p,"loadingStep",original.StableStep);
                p.enabled=enabled;Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);
            }
            return string.Join("\n",lines);
        }
        public static string LoadingContract()
        {
            var lines=new List<string>();
            foreach(int iso in new[]{100,200,400,800})foreach(int step in new[]{0,1,2,4,5,6,7})
            {
                var c=JsonUtility.FromJson<ParkCameraLoading>(JsonUtility.ToJson(ParkCameraLoading.AtStep(step,iso)));
                Check(lines,c.StableStep==step && c.iso==iso && c.threaded==(step>=4),"ISO "+iso+" checkpoint "+step+" round-trip");
            }
            Check(lines,!ParkCameraLoading.CanAdvance(1,7) && !ParkCameraLoading.CanAdvance(4,6) && !ParkCameraLoading.CanAdvance(5,5),"Cannot skip loading or double-advance tension");
            Check(lines,new ParkCameraLoading{phase=PhotoChapter.FilmPhase.LoadedOpen,threaded=true}.StableStep==4,"Legacy threaded roll resumes at tension check");
            Check(lines,new ParkCameraLoading{phase=PhotoChapter.FilmPhase.Closed,threaded=true}.StableStep==6,"Legacy closed roll still needs first-frame advance");
            var empty=new ParkFilmRoll{entrance=new BridgePhotoCheckpoint(),cameraLoading=new ParkCameraLoading()};
            Check(lines,!empty.HasPhotographyProgress,"Materialized default checkpoint does not teleport first arrival");
            empty.cameraLoading=ParkCameraLoading.AtStep(1,100);
            Check(lines,empty.HasPhotographyProgress,"Partly opened camera counts as a returning memory");
            return string.Join("\n",lines);
        }
        public static string DarkroomBottleHits()
        {
            var h=WBMain.Instance;
            if(!Application.isPlaying || h==null)throw new InvalidOperationException("House Play required");
            var w=h.DarkroomWorld;var v=UnityEngine.Object.FindObjectOfType<DarkroomAssetVisuals>();var lines=new List<string>();
            // A reachable front-of-worktop view; do not move any authored object.
            var eye=new Vector3(2.95f,1.9f,1.05f);
            Physics.SyncTransforms();
            foreach(var id in new[]{"chemical-0","chemical-1","chemical-2"})
            {
                var mesh=DarkroomAssetVisuals.Node(v.ModelRoot(id),"Body_Mesh").GetComponent<Renderer>();
                var at=mesh.bounds.center+Vector3.up*mesh.bounds.extents.y*.65f;
                var actual=w.Pick(new Ray(eye,at-eye));
                Check(lines,actual==id,id+": visible bottle shoulder selects itself, got "+actual);
            }
            return string.Join("\n",lines);
        }
        public static string CompanionResume()
        {
            var lines=new List<string>();
            var method=typeof(ParkCompanion).GetMethod("ResumeAtPhotoNode");
            Check(lines,method!=null,"Companion can resume at unfinished task without replaying earlier route");
            if(method==null)return string.Join("\n",lines);
            var go=new GameObject("QA companion resume");var point=new GameObject("QA photo task");
            try
            {
                var c=go.AddComponent<ParkCompanion>();var n=point.AddComponent<ParkPhotoWaypoint>();
                n.transform.position=new Vector3(5,0,3);c.route=new[]{n};c.photoHoldNode=n;
                Check(lines,(bool)method.Invoke(c,new object[]{n}),"Resume request accepted before Start");
                typeof(ParkCompanion).GetMethod("Start",Hidden).Invoke(c,null);
                Check(lines,c.AtPhotoNode && Vector3.Distance(c.transform.position,n.transform.position)<.001f,"Start does not reset queued resume");
                c.RestartWalk();
                Check(lines,(bool)method.Invoke(c,new object[]{n}) && c.AtPhotoNode,"Resume also works after Start");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(point);}
            return string.Join("\n",lines);
        }
        public static string HouseLoadingOwnership()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || WBMain.Instance==null)
                throw new InvalidOperationException("Isolated HouseScene Editor Play required.");
            var world=WBMain.Instance.DarkroomWorld;
            var lines=new List<string>();
            foreach(int step in new[]{1,2,4,5,6,7})
            {
                var p=new ParkCameraLoading{mechanicalStep=step,iso=400,threaded=step>=4,
                    phase=step>=7?PhotoChapter.FilmPhase.Ready:step>=6?PhotoChapter.FilmPhase.Closed:step>=2?PhotoChapter.FilmPhase.LoadedOpen:PhotoChapter.FilmPhase.Empty};
                var roll=ParkFilmStore.SaveCameraLoading(null,p);
                typeof(PhotoWorld3D).GetMethod("LoadParkFilm",Hidden).Invoke(world,null);
                bool saved=(bool)typeof(PhotoWorld3D).GetMethod("SaveParkLoading",Hidden).Invoke(world,null);
                var after=ParkFilmStore.Load(roll.id);
                Check(lines,saved && after.cameraLoading.mechanicalStep==step && after.cameraLoading.iso==400,
                    "House save preserves Park mechanical step "+step+" and ISO");
            }
            return string.Join("\n",lines);
        }
        public static string ParkLoadingPointers()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null || p.ExposedFrames!=0)
                throw new InvalidOperationException("Isolated unexposed Park Play required");
            var lines=new List<string>();bool enabled=p.enabled;p.enabled=false;
            var parts=(Dictionary<string,Transform>)Field(p,"instrumentParts");
            Action sync=()=>{var roll=(ParkFilmRoll)Field(p,"roll");var v=(PhotographyCameraVisual)Field(p,"instrumentCamera");int step=(int)Field(p,"loadingStep");
                v.Apply(step>0&&step<6,step>=2,p.FilmIso,step>=7?3:step>=4?2:0,1,p.focusDistance,p.Aperture,p.ShutterSeconds,0,1,true);
                Call(p,"SyncInstruments");Physics.SyncTransforms();};
            Func<string,Ray> ray=id=>new Ray(p.view.transform.position,parts[id].position-p.view.transform.position);
            Action<string> click=id=>{sync();Call(p,"BeginBodyPointer",ray(id));Call(p,"EndBodyPointer",ray(id));sync();};
            Action<string,string> drop=(from,to)=>{sync();Call(p,"BeginBodyPointer",ray(from));Call(p,"MoveBodyPointer",ray(to));Call(p,"EndBodyPointer",ray(to));sync();};
            try
            {
                foreach(int iso in new[]{100,200,400,800})
                {
                    var roll=ParkFilmStore.SaveCameraLoading(p.CurrentFilmId,ParkCameraLoading.AtStep(0,p.FilmIso));
                    roll=ParkFilmStore.SaveCameraLoading(roll.id,ParkCameraLoading.AtStep(0,iso));Set(p,"roll",roll);Set(p,"loadingStep",0);
                    Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);sync();
                    click("eyepiece");Check(lines,!p.CanExpose && p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Body,"ISO "+iso+": empty body cannot enter finder");
                    Call(p,"BeginBodyPointer",ray("rewind"));var r=ray("rewind");Call(p,"MoveBodyPointer",new Ray(r.origin+Vector3.up*.06f,r.direction));sync();
                    Check(lines,(int)Field(p,"loadingStep")==1,"ISO "+iso+": actual rewind hit opens back");
                    Call(p,"BeginBodyPointer",ray("film-"+iso));Call(p,"CancelInstrumentDrag");sync();
                    Check(lines,(int)Field(p,"loadingStep")==1 && Field(p,"dragPart")==null,"ISO "+iso+": cartridge cancellation retains open-back checkpoint");
                    drop("film-"+iso,"chamber");Check(lines,(int)Field(p,"loadingStep")==2,"ISO "+iso+": cartridge ray drop accepted");
                    drop("leader","spool");Check(lines,(int)Field(p,"loadingStep")==4,"ISO "+iso+": leader ray drop engages spool");
                    click("back");Check(lines,(int)Field(p,"loadingStep")==4,"ISO "+iso+": cannot close before tension check");
                    click("advance");Check(lines,(int)Field(p,"loadingStep")==5,"ISO "+iso+": tension lever accepted");
                    click("back");Check(lines,(int)Field(p,"loadingStep")==6,"ISO "+iso+": back closure accepted");
                    click("advance");Check(lines,p.CanExpose && ParkFilmStore.Load(p.CurrentFilmId).cameraLoading.StableStep==7,"ISO "+iso+": first frame advanced and persisted");
                    Check(lines,p.ExposedFrames==0,"ISO "+iso+": loading consumes no frame");
                }
                return string.Join("\n",lines);
            }
            finally{p.enabled=enabled;}
        }
        public static string CaptureCurrentTask()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null || !p.CanExpose)throw new InvalidOperationException("Prepared test roll in Park required");
            var lines=new List<string>();bool enabled=p.enabled;p.enabled=false;
            try
            {
                int before=p.ExposedFrames;var node=(ParkPhotoWaypoint)Field(p,"bridgeNode");
                p.companion.ResumeAtPhotoNode(node);p.companion.Paused=false;
                var cc=p.GetComponent<CharacterController>();cc.enabled=false;
                p.transform.position=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;cc.enabled=true;
                p.view.transform.SetPositionAndRotation(node.referenceView.position,node.referenceView.rotation);
                Set(p,"yaw",node.referenceView.eulerAngles.y);Set(p,"pitch",Mathf.DeltaAngle(0,node.referenceView.eulerAngles.x));
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);p.view.fieldOfView=PlayerControlSettings.Current.parkAimingFov;
                Call(p,"CaptureBridgeReference");var roll=(ParkFilmRoll)Field(p,"roll");var task=roll.Task(roll.NextTask);
                p.focusDistance=Vector3.Dot(task.subjectPoint-p.view.transform.position,p.view.transform.forward);
                // Select an actual equivalent exposure from the existing physical settings.
                float best=float.MaxValue;int ai=0,si=0;
                var apertures=(float[])typeof(ParkPhotoPrototype).GetField("apertures",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                var speeds=(float[])typeof(ParkPhotoPrototype).GetField("speeds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                for(int a=0;a<apertures.Length;a++)for(int s=0;s<speeds.Length;s++)
                {float error=Mathf.Abs(Mathf.Log(apertures[a]*apertures[a]/speeds[s],2)-Mathf.Log(p.FilmIso/100f,2)-task.ev100);if(error<best){best=error;ai=a;si=s;}}
                Set(p,"apertureIndex",ai);Set(p,"speedIndex",si);
                Call(p,"TickBridge",1f);
                for(int i=0;i<300;i++)Call(p.companion,"Update");
                Physics.SyncTransforms();Call(p,"TickBridge",1f);
                Check(lines,(bool)Field(p,"bridgeReady"),node.photoTask+": real framing/focus/exposure/visibility rules ready");
                float focus=p.focusDistance;p.focusDistance=18;Call(p,"TickBridge",0f);Call(p,"Capture");
                Check(lines,p.ExposedFrames==before,node.photoTask+": wrong focus cannot consume film");p.focusDistance=focus;Call(p,"TickBridge",0f);
                if(node.photoTask==ParkPhotoWaypoint.PhotoTask.Meadow)
                {
                    var screen=p.view.WorldToViewportPoint(p.PhotoHandPosition);Set(p,"virtualPointer",new Vector2(screen.x,screen.y));
                    Call(p,"Capture");Check(lines,p.ExposedFrames==before && p.HoldingPhotoHand,"Hand response is separate from shutter and consumes no frame");
                }
                Call(p,"Capture");
                Check(lines,p.ExposedFrames==before+1,node.photoTask+": real Camera.Render exposure saved");
                var saved=ParkFilmStore.Load(p.CurrentFilmId);byte[] image;string reason;
                Check(lines,!ParkFilmStore.TryReadDevelopedFrame(saved.id,before+1,out image,out reason),"Park cannot preview undeveloped frame "+(before+1));
                if(saved.frames.Count>before)Check(lines,saved.frames[before].moment==(before==0?"Entrance candid":before==1?"Bridge look-back":"Meadow joined hands"),"Distinct action recorded for "+node.photoTask);
                return string.Join("\n",lines);
            }
            finally{p.enabled=enabled;}
        }
    }
}
