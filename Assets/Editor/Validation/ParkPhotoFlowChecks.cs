using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkPhotoFlowChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object o,string field,object value){o.GetType().GetField(field,Hidden).SetValue(o,value);}
        static object Call(object o,string method,params object[] args){return o.GetType().GetMethod(method,Hidden).Invoke(o,args);}
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static object Get(object o,string field){return o.GetType().GetField(field,Hidden).GetValue(o);}
        static void ClearReference(ParkPhotoPrototype p)
        {var image=(Texture2D)Get(p,"bridgeReference");if(image!=null)UnityEngine.Object.DestroyImmediate(image);Set(p,"bridgeReference",null);}
        public static string ThreeTaskRestore()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA","park-flow-"+Guid.NewGuid().ToString("N")));
            var root=new GameObject("QA three task restore");root.SetActive(false);
            var lines=new System.Collections.Generic.List<string>();Texture2D image=null;ParkPhotoPrototype p=null;
            var cursor=Cursor.lockState;bool cursorVisible=Cursor.visible;
            try
            {
                using(ParkFilmStore.BeginValidationScope(directory))
                {
                    Func<string,Transform> child=name=>{var t=new GameObject(name).transform;t.SetParent(root.transform);return t;};
                    p=child("Player").gameObject.AddComponent<ParkPhotoPrototype>();p.GetComponent<CharacterController>().enabled=false;Set(p,"motor",p.GetComponent<CharacterController>());
                    p.view=child("Camera").gameObject.AddComponent<Camera>();p.view.enabled=false;p.view.transform.SetParent(p.transform);p.view.pixelRect=new Rect(0,0,960,540);p.view.fieldOfView=48;
                    var girl=child("Girl").gameObject.AddComponent<ParkCompanion>();girl.GetComponent<CharacterController>().enabled=false;girl.player=p.transform;p.companion=girl;Set(girl,"started",true);
                    girl.chest=child("Chest");girl.chest.SetParent(girl.transform,false);girl.chest.localPosition=Vector3.up*1.2f;
                    girl.head=girl.hips=girl.feet=girl.chest;
                    image=new Texture2D(2,2,TextureFormat.RGB24,false);image.SetPixels(new[]{Color.red,Color.green,Color.blue,Color.white});image.Apply();var png=image.EncodeToPNG();
                    var nodes=new ParkPhotoWaypoint[3];
                    for(int i=0;i<3;i++)
                    {
                        var n=child("Photo "+i).gameObject.AddComponent<ParkPhotoWaypoint>();nodes[i]=n;n.photoTask=(ParkPhotoWaypoint.PhotoTask)(i+1);n.transform.position=new Vector3(1000+i*10,0,1000);n.pauseSeconds=i==1?12:0;
                        n.lookAt=child("Look "+i);n.lookAt.position=n.transform.position+Vector3.forward*3;
                        n.referenceView=child("View "+i);n.referenceView.position=n.transform.position+Vector3.up*1.2f+Vector3.back*(i==2?1.5f:3);n.referenceView.rotation=Quaternion.identity;
                        n.resumePoint=child("Resume "+i);n.resumePoint.position=n.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;n.referencePhoto=image;
                        n.handContact=child("Palm "+i);n.handContact.position=n.transform.position+Vector3.up*1.2f+Vector3.back*.5f;
                    }
                    girl.route=nodes;var roll=ParkFilmStore.LoadOrCreate();
                    foreach(int step in new[]{1,2,4,5,6,7})roll=ParkFilmStore.SaveCameraLoading(roll.id,ParkCameraLoading.AtStep(step,400));
                    for(int i=0;i<3;i++)
                    {
                        string task=i==0?"entrance":i==1?"bridge":"meadow";var n=nodes[i];
                        var state=new BridgePhotoCheckpoint{hasReference=true,aligned=false,position=n.referenceView.position,euler=Vector3.zero,subjectPoint=n.transform.position+Vector3.up*1.2f,fov=48,savedFocus=i==2?1.5f:3,savedAperture=8,savedShutter=1f/125};
                        roll=ParkFilmStore.SaveTask(roll.id,task,state,png);Set(p,"roll",ParkFilmStore.Load(roll.id));ClearReference(p);Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");
                        Check(girl.AtPhotoNode && (ParkPhotoWaypoint)Get(p,"bridgeNode")==n,"Reload selects wrong task "+task);
                        p.view.transform.SetPositionAndRotation(n.referenceView.position,n.referenceView.rotation);p.sceneEv100=11;
                        var cameraPosition=p.view.transform.position;var cameraRotation=p.view.transform.rotation;
                        Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Aperture);Call(p,"StepInstrumentControl",1);
                        Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Shutter);Call(p,"StepInstrumentControl",-1);
                        Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                        Check(p.Aiming && p.Aperture==11 && p.ShutterSeconds==1f/60 && p.view.transform.position==cameraPosition && p.view.transform.rotation==cameraRotation,"Dial return changed optical mode/pose "+task);
                        Check(p.SaveForPauseMenu(),"Edited settings cannot be saved "+task);
                        Set(p,"apertureIndex",0);Set(p,"speedIndex",0);p.focusDistance=18;
                        Set(p,"roll",ParkFilmStore.Load(roll.id));ClearReference(p);Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");
                        p.view.transform.SetPositionAndRotation(n.referenceView.position,n.referenceView.rotation);
                        Check(p.Aperture==11 && p.ShutterSeconds==1f/60 && p.focusDistance==state.savedFocus && !ParkFilmStore.Load(roll.id).Task(task).aligned,"Edited settings do not survive re-entry "+task);
                        girl.Paused=false;
                        lines.Add("PASS "+task+": dial controls -> finder -> save/reload retains parameters, camera pose and unfinished response");
                        Call(p,"TickBridge",.1f);
                        Check(!ParkFilmStore.Load(roll.id).Task(task).aligned && !girl.photoLookBack && !girl.photoOfferHand,"Response skips alignment dwell "+task);
                        Call(p,"TickBridge",.3f);Check(ParkFilmStore.Load(roll.id).Task(task).aligned,"Valid framing does not trigger response "+task);
                        Set(p,"roll",ParkFilmStore.Load(roll.id));ClearReference(p);Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");p.view.transform.SetPositionAndRotation(n.referenceView.position,n.referenceView.rotation);
                        Call(p,"TickBridge",.1f);for(int j=0;j<40;j++)Call(girl,"TickTravel",.1f);Call(p,"TickBridge",.1f);
                        Check(i==0?!girl.photoLookBack:girl.photoLookBack,"Response not restored "+task);
                        if(i==2)
                        {
                            Check(girl.photoOfferHand,"Meadow never offers hand");
                            // A real cursor can hit an offered palm near the side of the frame.
                            // Route through Capture, not just the hand helper, to catch accidental
                            // film exposure or the shutter's edge guard swallowing a valid hand click.
                            var originalContact=n.handContact.position;
                            var pointer=new Vector2(.02f,.5f);n.handContact.position=p.view.ViewportToWorldPoint(new Vector3(pointer.x,pointer.y,1));
                            Set(p,"virtualPointer",pointer);
                            bool onHand=(bool)p.GetType().GetProperty("PointerOnHand",Hidden).GetValue(p);
                            var clickGate=typeof(ParkPhotoPrototype).GetMethod("FinderClickAllowed",BindingFlags.Static|BindingFlags.NonPublic);
                            Check(onHand && (bool)clickGate.Invoke(null,new object[]{pointer,onHand}),"Edge palm click was blocked by shutter routing");
                            Call(p,"Capture");Call(p,"Capture"); // A second call during joining must remain non-exposing.
                            Check(ParkFilmStore.Load(roll.id).frames.Count==2 && ParkFilmStore.Load(roll.id).meadow.handJoined,"Joining consumes film or is not saved");
                            n.handContact.position=originalContact;Set(p,"virtualPointer",new Vector2(.5f,.5f));
                            lines.Add("PASS edge palm: capture routing joins without exposure; joining blocks duplicate shutter; saved hand retained");
                            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Aperture);Call(p,"StepInstrumentControl",-1);
                            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Shutter);Call(p,"StepInstrumentControl",1);
                            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                            Check(p.HoldingPhotoHand && p.SaveForPauseMenu() && ParkFilmStore.Load(roll.id).frames.Count==2,"Retuning after hand join drops hand or consumes film");
                            Set(p,"roll",ParkFilmStore.Load(roll.id));ClearReference(p);Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");p.view.transform.SetPositionAndRotation(n.referenceView.position,n.referenceView.rotation);
                            Call(p,"TickBridge",.1f);for(int j=0;j<40;j++)Call(girl,"TickTravel",.1f);Call(p,"TickPhotoFeedback",.6f);Call(p,"TickBridge",.1f);
                            Check(p.HoldingPhotoHand,"Joined hand lost after reload");
                        }
                        Check((bool)Get(p,"bridgeReady"),"Restored response blocks shutter "+task);
                        var record=(ParkPhotoRecord)Call(p,"Assess");Check(record.accepted && record.taskId==task,"Wrong task/acceptance "+task);
                        if(i==1)
                        {
                            int attempts=0;
                            var pending=new ParkExposurePersistence(ParkFilmStore.Load,(r,f,bytes)=>
                            {if(++attempts==1)throw new IOException("QA simulated one-shot save failure");return ParkFilmStore.SaveExposure(r,f,bytes);});
                            pending.Queue(record,png);Set(p,"exposurePersistence",pending);
                            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                            Set(p,"inputGate",false);p.InputContext.ContinueGesture();Call(p,"SyncCompanionControl");
                            Check(girl.TravelPaused && !p.SaveForPauseMenu(),"Lowering while saving departs or permits unsafe exit");
                            Check(!(bool)Call(p,"RetryPendingExposure") && ParkFilmStore.Load(roll.id).frames.Count==1 && pending.HasPending,"Failed save consumes a frame or loses its retry");
                            Check((bool)Call(p,"RetryPendingExposure"),"Retry cannot commit bridge frame");
                            roll=(ParkFilmRoll)Get(p,"roll");ClearReference(p);
                            Check(!(bool)Call(p,"RetryPendingExposure") && ParkFilmStore.Load(roll.id).frames.Count==2,"Repeated retry duplicates an exposure");
                            lines.Add("PASS bridge: lower while saving stays put; failed save retains frame; retry commits exactly once");
                        }
                        else
                        {roll=ParkFilmStore.SaveExposure((ParkFilmRoll)Get(p,"roll"),record,png);Set(p,"roll",roll);ClearReference(p);Call(p,"ExposureCommitted",record);}
                        if(i<2)
                        {
                            if(i==0)
                            {
                                Call(p,"TickLoweredDeparture");Check(p.GuidedDeparturePending,"Raised camera departed early");
                                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Aperture);Call(p,"TickLoweredDeparture");
                                Check(p.GuidedDeparturePending,"Opening a dial departed before camera lowering");
                                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);Call(p,"TickLoweredDeparture");
                                Check(p.GuidedDeparturePending,"Returning from dial departed before camera lowering");
                                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                            }
                            Call(p,"TickLoweredDeparture");
                            Check(!p.GuidedDeparturePending && (ParkPhotoWaypoint)Get(p,"bridgeNode")==nodes[i+1],"Lowering fails to select next task");
                            girl.TravelPaused=false;Call(girl,"TickTravel",.1f);Call(girl,"TickTravel",.1f);Check(girl.TravelRequestedLastStep,"Completed photo retains dwell time");
                            // Re-enter from committed film, not an injected next-task checkpoint.
                            Set(p,"roll",ParkFilmStore.Load(roll.id));ClearReference(p);Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");
                            Check(girl.AtPhotoNode && (ParkPhotoWaypoint)Get(p,"bridgeNode")==nodes[i+1] && ParkFilmStore.Load(roll.id).frames.Count==i+1,"Re-entry repeats a completed exposure or selects wrong next stop");
                        }
                        else Check(p.CanReturnToDarkroom && p.CurrentPauseReason==ParkPhotoPrototype.PauseReason.FilmComplete,"Third photo fails to enter film return state");
                        lines.Add("PASS "+task+": restored response, shutter eligibility, committed frame, "+(i<2?"lower-camera departure":"joined-hand restore and full-roll return"));
                    }
                }
                return string.Join("\n",lines);
            }
            finally
            {
                if(p!=null)ClearReference(p);UnityEngine.Object.DestroyImmediate(root);if(image!=null)UnityEngine.Object.DestroyImmediate(image);Cursor.lockState=cursor;Cursor.visible=cursorVisible;
            }
        }
        public static string Arrival()
        {
            var root=new GameObject("QA arrival");root.SetActive(false);
            try
            {
                var girl=new GameObject("Girl").AddComponent<ParkCompanion>();girl.transform.SetParent(root.transform);
                var node=new GameObject("Photo").AddComponent<ParkPhotoWaypoint>();node.transform.SetParent(root.transform);node.transform.position=Vector3.forward*.05f;
                var player=new GameObject("Player").transform;player.SetParent(root.transform);player.position=Vector3.back*2;
                girl.player=player;girl.route=new[]{node};girl.photoHoldNode=node;girl.TravelPaused=true;
                Call(girl,"TickTravel",.1f);
                Check(girl.AtPhotoNode && !girl.TravelRequestedLastStep,"Raising camera inside arrival tolerance prevents photo arrival");
                return "PASS arrival while instrument holds locomotion";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static string PausedSave()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA","park-pause-save-"+Guid.NewGuid().ToString("N")));
            var root=new GameObject("QA pause save");root.SetActive(false);bool previousPause=GamePauseMenu.Active;
            ParkExposureEncoder encoder=null;
            try
            {
                using(ParkFilmStore.BeginValidationScope(directory))
                {
                    var roll=ParkFilmStore.LoadOrCreate();
                    foreach(int step in new[]{1,2,4,5,6,7})roll=ParkFilmStore.SaveCameraLoading(roll.id,ParkCameraLoading.AtStep(step,400));
                    roll=ParkFilmStore.SaveTask(roll.id,"entrance",new BridgePhotoCheckpoint{hasReference=true,aligned=true},new byte[]{1,2,3});
                    var p=root.AddComponent<ParkPhotoPrototype>();
                    var girl=new GameObject("Girl").AddComponent<ParkCompanion>();girl.transform.SetParent(root.transform);p.companion=girl;
                    Set(p,"roll",roll);
                    encoder=new ParkExposureEncoder((rgb,w,h)=>new byte[]{1,2,3});
                    Set(p,"exposureEncoder",encoder);
                    var frame=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),taskId="entrance",accepted=true};
                    encoder.Start(frame,new byte[]{1,2,3},1,1);
                    var task=(System.Threading.Tasks.Task)typeof(ParkExposureEncoder).GetField("task",Hidden).GetValue(encoder);
                    Check(task.Wait(2000),"Fixture encoder did not finish");
                    Check(typeof(ParkPhotoPrototype).GetMethod("LeaveParkMemory")==null && typeof(ParkPhotoPrototype).GetMethod("LeaveWithSavedProgress")==null,"Early park exit API remains available");
                    Check(!p.SaveForPauseMenu(),"Unified pause must not discard a pending exposure");
                    GamePauseMenu.Active=true;
                    Call(p,"Update");
                    Check(ParkFilmStore.Load(roll.id).frames.Count==1,"Completed encoding cannot commit while pause menu is open");
                    Call(p,"Update");
                    Check(ParkFilmStore.Load(roll.id).frames.Count==1 && p.GuidedDeparturePending,"Pause polling duplicates a frame or departs before camera lowering");
                    return "PASS completed encoding commits once while paused; departure remains held";
                }
            }
            finally
            {
                GamePauseMenu.Active=previousPause;if(encoder!=null)encoder.Cancel();
                UnityEngine.Object.DestroyImmediate(root);
                // Keep fixture output for inspection; never touch the user save root.
            }
        }
    }
}
