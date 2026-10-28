using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    // Uses production interaction handlers, real scene rays, animation and rendered exposures.
    // Editor-only scheduler survives the House -> Park -> House scene transition.
    public static class HouseGameplayChecks
    {
        public static string Result="Not run";
        static IEnumerator run;
        static readonly List<string> checks=new List<string>();
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        static void Focus(WBMain h){h.SendMessage("OnApplicationFocus",true);h.radioDeck.SendMessage("OnApplicationFocus",true);h.safeInspection.SetFocus(true);h.ReleaseHouseReturnInput(true);}
        static void Settle(WBMain h){for(int i=0;i<4;i++)h.AdvanceHouseTransition(1);}
        static void Enter(WBMain h,int index)
        {
            h.ReleaseHouseReturnInput(true);h.Select(index);Check(h.InStation,"Entered chapter "+index);
            Check(!h.HouseInputReady,"Transition blocks input "+index);Settle(h);
            h.ReleaseHouseEntryInput();h.ReleaseHouseReturnInput(true);h.radioDeck.ReleaseInputGate(true);
            Check(h.HouseInputReady,"Release enables chapter "+index);
        }
        static void Exit(WBMain h){h.LeaveStation();Settle(h);h.ReleaseHouseReturnInput(true);Check(!h.InStation,"Returned to exploration");}
        static Ray At(Camera c,Vector3 p){Physics.SyncTransforms();return new Ray(c.transform.position,p-c.transform.position);}
        static void Frame(HouseRadioDeck d,Ray r,bool down,float wheel=0){d.TickStation(.02f,r,down,false,wheel,false,!down);}
        static void Drag(PhotoWorld3D p,string item,string destination)
        {p.Press(item,p.QARayTo(item),0);p.Move(p.QARayTo(destination));p.Release(destination,p.QARayTo(destination));}
        static void Press(PhotoWorld3D p,string id){p.Press(id,p.QARayTo(id),0);p.Release(id,p.QARayTo(id));}
        static void Invoke(object target,string method){target.GetType().GetMethod(method,Private).Invoke(target,null);}
        public static string Start()
        {
            if(!Application.isPlaying)throw new Exception("Play HouseScene first");
            if(!GameSavePaths.ResetOnEditorPlay)throw new Exception("Use the isolated Editor test-save setting before validation.");
            if(run!=null)throw new Exception("Validation already running");
            checks.Clear();Result="Running";run=Flow();EditorApplication.update+=Step;return Result;
        }
        static void Step()
        {
            try{if(!Application.isPlaying)throw new Exception("Play stopped during validation");if(!run.MoveNext())Finish(null);}
            catch(Exception e){Finish(e.GetBaseException().ToString());}
        }
        static void Finish(string error)
        {
            EditorApplication.update-=Step;(run as IDisposable)?.Dispose();run=null;
            var h=UnityEngine.Object.FindObjectOfType<WBMain>();if(h!=null)h.enabled=true;
            var park=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();if(park!=null)park.enabled=true;
            Result=error==null?"PASS: "+checks.Count+" checks":"FAIL after "+checks.Count+" checks: "+error;
            Directory.CreateDirectory("QA");File.WriteAllText("QA/house-gameplay-retirement.txt",Result+"\n"+string.Join("\n",checks));
            if(error==null)Debug.Log(Result);else Debug.LogError(Result);
        }
        static IEnumerator Flow()
        {
            var h=UnityEngine.Object.FindObjectOfType<WBMain>();
            Check(h!=null && h.HouseReady,"House starts without legacy station");Focus(h);h.enabled=false;
            Check(typeof(WBMain).GetField("houseTape")==null,"No legacy station field");
            Check(h.playerCamera==h.radioDeck.view && h.playerHands==h.playerCamera.GetComponent<FirstPersonHands>(),"Explicit current player references");
            Check(!SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="Legacy_TapeStation"),"No legacy device object");
            Check(Vector3.Distance(h.playerCamera.transform.position,h.playerSpawn.transform.position)<.001f,"Authored spawn applied");
            var initial=h.playerCamera.transform.position;var initialRot=h.playerCamera.transform.rotation;var initialFov=h.playerCamera.fieldOfView;
            h.Select(0);Check(!h.InStation && !h.IsTransitioning,"Radio has no fixed-view entry");
            Check(Vector3.Distance(h.playerCamera.transform.position,initial)<.001f,"Radio entry preserves exploration pose");
            for(int i=1;i<5;i++)
            {
                var eye=initial+new Vector3(.03f*i,0,-.02f*i);var rot=initialRot*Quaternion.Euler(0,i*2,0);float fov=initialFov+i;
                h.playerCamera.transform.SetPositionAndRotation(eye,rot);h.playerCamera.fieldOfView=fov;
                Enter(h,i);h.playerHands.Advance(1);
                Check(!System.Text.RegularExpressions.Regex.IsMatch(h.CurrentWorld.Help,"[\u4e00-\u9fff]"),"English operation help "+i);
                Check(h.playerHands.VisibleSet==i,"Player-owned hands available "+i);
                var model=h.Current;var world=h.CurrentWorld;
                if(i==0)Check(Vector3.Distance(h.playerCamera.transform.position,h.radioDeck.operationView.transform.position)<.001f,"Authored radio operation anchor applied");
                Exit(h);
                Check(Vector3.Distance(h.playerCamera.transform.position,eye)<.001f && Quaternion.Angle(h.playerCamera.transform.rotation,rot)<.01f && Mathf.Abs(h.playerCamera.fieldOfView-fov)<.001f,"Return pose and FOV restored "+i);
                Check(h.playerCamera.rect==new Rect(0,0,1,1),"Viewport restored "+i);
                Enter(h,i);Check(ReferenceEquals(model,h.Current)&&ReferenceEquals(world,h.CurrentWorld),"Chapter state reused "+i);Exit(h);
                h.Select(i);h.AdvanceHouseTransition(.1f);Exit(h);
                Check(Vector3.Distance(h.playerCamera.transform.position,eye)<.001f,"Interrupted entry returns to approach "+i);
            }
            var d=h.radioDeck;d.ResetDeck();d.ReleaseInputGate(true);
            // A reachable player-selected approach, offset from the old operation anchor.
            h.playerCamera.transform.SetPositionAndRotation(d.operationView.transform.position+new Vector3(.08f,0,0),d.operationView.transform.rotation);
            var tapeEye=h.playerCamera.transform.position;var tapeRotation=h.playerCamera.transform.rotation;
            var ray=At(d.view,d.tapes[0].hit.bounds.center);var start=d.tapes[0].root.position;
            Frame(d,ray,true);d.PoseDraggedTape(3);Physics.SyncTransforms();
            Check(d.Selected==0 && d.State==HouseRadioDeck.Phase.Dragging && d.DraggingTape,"Scene ray starts direct drag");
            Check(Vector3.Distance(start,d.tapes[0].root.position)<.001f,"Drag starts at table, not eye");
            Check(!h.InStation && h.DirectTapeHandling,"Drag has no station entry");
            Invoke(h,"SyncTapePointer");h.Room.StepInput(Vector2.one,new Vector2(20,20),.1f);
            Check(Vector3.Distance(h.playerCamera.transform.position,tapeEye)<.001f && Quaternion.Angle(h.playerCamera.transform.rotation,tapeRotation)<.01f,"Drag retains current camera");
            Frame(d,ray,true,3);Check(d.Chapter.Tape.InspectPosition==0,"World drag cannot scan reels");
            Frame(d,ray,false);Check(d.State==HouseRadioDeck.Phase.Returning,"Off-slot release returns tape");
            d.AdvanceReturn(3);Invoke(h,"SyncTapePointer");
            Check(d.State==HouseRadioDeck.Phase.Empty && !h.DirectTapeHandling,"No persistent hand state");
            for(int candidate=0;candidate<3;candidate+=2)
            {
                var subject=h.safeInspection.ForSource(d.tapes[candidate].root);var inspect=h.safeInspection;
                Check(inspect.Open(subject),"Tape inspection opens "+candidate);inspect.Advance(1);Physics.SyncTransforms();
                var spindle=subject.poses.First(pair=>pair.source==d.tapes[candidate].rightReel).display;
                var point=(Vector2)inspect.displayCamera.WorldToScreenPoint(spindle.position);
                inspect.PointerEvent(EventType.MouseDown,0,point,Vector2.zero);inspect.Advance(.2f);
                Check(inspect.HeldPart!=null && inspect.HeldPart.transform==spindle,"Inspection ray selects real reel "+candidate);
                var data=d.Chapter.Tape.Candidates[candidate];
                for(int j=0;j<30 && !data.Broken && data.Scanned<TapeInspectionState.Samples;j++)
                    inspect.PointerEvent(EventType.ScrollWheel,0,point,Vector2.zero,3);
                inspect.PointerEvent(EventType.MouseUp,0,point,Vector2.zero);
                Check(candidate==0?data.Broken:data.Scanned==TapeInspectionState.Samples,"Inspection scan result "+candidate);
                int scanned=data.Scanned;inspect.ResetView();inspect.Advance(1);
                Check(data.Scanned==scanned && (candidate!=0 || data.Broken),"Reset view preserves tape evidence");
                inspect.Close();int frame=Time.frameCount;while(Time.frameCount<=frame+2)yield return null;inspect.ReleaseInputGate(true);
                Check(Vector3.Distance(d.tapes[candidate].root.position,d.tapes[candidate].homeParent.TransformPoint(d.tapes[candidate].homePosition))<.001f,"Inspection leaves tape on table");
            }
            Check(d.tapes[0].damageMarker.activeSelf,"Discovered break remains visible outside inspection");
            ray=At(d.view,d.tapes[0].hit.bounds.center);Frame(d,ray,true);
            var brokenSlot=At(d.view,d.slotHit.bounds.center);Frame(d,brokenSlot,false);
            Check(d.State==HouseRadioDeck.Phase.Returning,"Broken tape rejected and returned");d.AdvanceReturn(3);
            foreach(var subject in h.safeInspection.subjects)
            {
                var positions=subject.poses.Select(p=>p.source.localPosition).ToArray();var rotations=subject.poses.Select(p=>p.source.localRotation).ToArray();
                Check(h.safeInspection.Open(subject),"Inspection opens "+subject.title);h.safeInspection.Advance(1);h.safeInspection.Rotate(new Vector2(80,40));h.safeInspection.Flip();
                foreach(var part in subject.parts){part.Press();part.Drag(40);part.Release();}
                h.safeInspection.Close();
                Check(subject.poses.Select((p,i)=>Vector3.Distance(p.source.localPosition,positions[i])<.0001f && Quaternion.Angle(p.source.localRotation,rotations[i])<.01f).All(ok=>ok),"Inspection preserves source "+subject.title);
                int closeFrame=Time.frameCount;while(Time.frameCount<=closeFrame+2)yield return null;
                h.safeInspection.ReleaseInputGate(true);
            }
            ray=At(d.view,d.tapes[2].hit.bounds.center);Frame(d,ray,true);
            Check(d.DraggingTape,"Intact tape starts direct drag");
            float depth=d.view.transform.InverseTransformPoint(d.tapes[2].root.position).z;
            var slot=At(d.view,d.slotHit.bounds.center);Frame(d,slot,true);d.PoseDraggedTape(3);Physics.SyncTransforms();
            Check(Mathf.Abs(d.view.transform.InverseTransformPoint(d.tapes[2].root.position).z-depth)<.002f,"Slot hover stays on original drag plane");
            Frame(d,slot,false);Check(d.State==HouseRadioDeck.Phase.Loading,"Release over slot starts load animation");
            Check(!d.InsertDragged()&&!d.speaker.isPlaying,"No duplicate loading or premature sound");
            Check(!h.DirectTapeHandling && !h.InStation,"Loading returns player to exploration");
            Invoke(h,"SyncTapePointer");
            yield return null;Enter(h,1);float progress=d.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float until=Time.realtimeSinceStartup+.2f;while(Time.realtimeSinceStartup<until)yield return null;
            Check(d.State==HouseRadioDeck.Phase.Loading && Mathf.Abs(d.animator.GetCurrentAnimatorStateInfo(0).normalizedTime-progress)<.01f,"Exit freezes load progress");
            Exit(h);d.ReleaseInputGate(true);until=Time.realtimeSinceStartup+10;
            while(d.State==HouseRadioDeck.Phase.Loading && Time.realtimeSinceStartup<until)yield return null;
            Check(d.State==HouseRadioDeck.Phase.Playing && d.speaker.isPlaying && d.speaker.clip==d.tapes[2].audio,"Re-entry finishes physical loading and starts correct audio");
            Check(d.Chapter.Completed,"Verified tape playback completes chapter");
            Check(h.Done[0] && !h.Done[1],"Radio completes its own chapter outside station");
            var reel=d.tapes[2].leftReel.localRotation;until=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<until)yield return null;
            Check(Quaternion.Angle(reel,d.tapes[2].leftReel.localRotation)>.5f,"Loaded cassette reel animation runs");
            Enter(h,1);Check(d.State==HouseRadioDeck.Phase.Paused&&!d.speaker.isPlaying,"Other chapter pauses loaded tape");
            Exit(h);d.ReleaseInputGate(true);Check(d.Selected==2&&d.Chapter.Completed,"Tape completion retained");ray=At(d.view,d.radioHit.bounds.center);d.TickStation(.02f,ray,false,true,0,false,false);Check(d.State==HouseRadioDeck.Phase.Empty,"Radio RMB ejects tape without hand state");d.ReleaseInputGate(true);
            Enter(h,1);var photo=(PhotoWorld3D)h.CurrentWorld;
            Check(!h.TryStartParkPhotography(),"Unloaded camera cannot enter park");
            Drag(photo,"film-200","chamber");Drag(photo,"leader","spool");Press(photo,"back");Press(photo,"advance");photo.SaveProgress();
            Check(photo.Photo.Phase==PhotoChapter.FilmPhase.Ready && ParkFilmStore.LoadActive().ReadyForPark,"Mechanical camera loading saved");
            var returnEye=h.playerCamera.transform.position; // Restore the actual exploration pose after exiting.
            Exit(h);returnEye=h.playerCamera.transform.position;var returnRot=h.playerCamera.transform.rotation;var returnFov=h.playerCamera.fieldOfView;Enter(h,1);
            Check(h.TryStartParkPhotography(),"Loaded camera enters park");
            ParkPhotoPrototype park=null;until=Time.realtimeSinceStartup+15;
            while((park=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>())==null && Time.realtimeSinceStartup<until)yield return null;
            yield return null;Check(park!=null&&park.CanExpose,"Park receives prepared roll");park.enabled=false;park.companion.Paused=true;
            var hands=park.view.GetComponent<FirstPersonHands>();Check(hands!=null&&hands.authoredHands.All(t=>t!=null),"Park retains player hands");
            park.view.transform.LookAt(park.companion.chest.position);park.photoWidth=640;
            for(int i=0;i<ParkFilmRoll.FrameCapacity;i++)
            {
                park.focusDistance=i==0?park.maximumFocus:park.view.WorldToViewportPoint(park.companion.chest.position).z;
                Invoke(park,"Capture");Check(park.ExposedFrames==i+1,"Rendered exposure saved "+(i+1));yield return null;
            }
            Check(!park.Records[0].sharp&&park.Records[1].sharp,"Manual focus changes photo sharpness assessment");
            Check(park.CanReturnToDarkroom&&!park.CanExpose,"Eight frames seal roll");
            Invoke(park,"Capture");Check(park.ExposedFrames==ParkFilmRoll.FrameCapacity,"Exposure beyond capacity rejected");
            typeof(ParkPhotoPrototype).GetField("returnUiGate",Private).SetValue(park,false);park.ReturnToDarkroom();
            h=null;until=Time.realtimeSinceStartup+15;while((h=UnityEngine.Object.FindObjectOfType<WBMain>())==null && Time.realtimeSinceStartup<until)yield return null;
            yield return null;Check(h!=null&&h.HouseReady,"House reload initializes without legacy device");Focus(h);h.enabled=false;Settle(h);h.ReleaseHouseEntryInput();h.ReleaseHouseReturnInput(true);
            Check(h.InStation&&h.ChapterIndex==1,"Return opens darkroom");photo=(PhotoWorld3D)h.CurrentWorld;
            Check(photo.Photo.Phase==PhotoChapter.FilmPhase.DarkBag,"Undeveloped roll enters dark bag");
            photo.Press("developed-photo",photo.QARayTo("developed-photo"),0);Check(!photo.ParkGalleryOpen,"Undeveloped photos cannot be viewed");
            photo.Press("bag",photo.QARayTo("bag"),0);photo.Scroll("bag",4,photo.QARayTo("bag"));photo.Release("bag",photo.QARayTo("bag"));photo.SaveProgress();
            float turns=photo.Photo.BagTurns;Exit(h);
            Check(Vector3.Distance(h.playerCamera.transform.position,returnEye)<.001f && Quaternion.Angle(h.playerCamera.transform.rotation,returnRot)<.01f && Mathf.Abs(h.playerCamera.fieldOfView-returnFov)<.001f,"Park roundtrip restores original house approach");
            Enter(h,1);Check(photo.Photo.BagTurns==turns&&ParkFilmStore.LoadActive().darkroom.bagTurns==turns,"Darkroom progress survives exit and is saved");
            photo.Press("bag",photo.QARayTo("bag"),0);photo.Scroll("bag",10,photo.QARayTo("bag"));photo.Release("bag",photo.QARayTo("bag"));
            Check(photo.Photo.Phase==PhotoChapter.FilmPhase.Tank,"Hand-winding loads development tank");
            for(int chemical=0;chemical<3;chemical++)
            {
                Drag(photo,"chemical-"+chemical,"tank");Check(photo.Photo.Phase==PhotoChapter.FilmPhase.Processing,"Chemical stage starts "+chemical);
                var point=photo.Nodes["tank"].transform.position;var from=new Ray(point+Vector3.up*2,Vector3.down);var to=new Ray(point+Vector3.up*2+Vector3.right*.2f,Vector3.down);
                photo.Press("tank",from,0);photo.Move(to);photo.Release("tank",to);Check(photo.Photo.Agitated,"Hand agitation accepted "+chemical);
                for(int frame=0;frame<100;frame++)photo.Tick(.05f,new WBInput());
                Check(photo.Photo.Phase==PhotoChapter.FilmPhase.Tank&&photo.Photo.Chemistry==chemical+1,"Chemical stage completes "+chemical);
            }
            Press(photo,"water");for(int frame=0;frame<50;frame++)photo.Tick(.05f,new WBInput());photo.SaveProgress();
            Check(ParkFilmStore.LoadActive().state==ParkFilmState.Developed,"Final rinse unlocks developed roll");
            photo.Press("developed-photo",photo.QARayTo("developed-photo"),0);Check(photo.ParkGalleryOpen,"Developed gallery opens");
            for(int page=1;page<=ParkFilmRoll.FrameCapacity;page++)
            {
                byte[] bytes;string message;
                Check(ParkFilmStore.TryReadDevelopedFrame(ParkFilmStore.LoadActive().id,page,out bytes,out message)&&bytes.Length>0,"Developed image readable "+page);
                if(page<ParkFilmRoll.FrameCapacity)typeof(PhotoWorld3D).GetMethod("TurnParkFrame",Private).Invoke(photo,new object[]{1});
            }
            Check(ParkFilmStore.LoadActive().darkroom.selectedFrame==ParkFilmRoll.FrameCapacity,"Gallery page persisted");
            Check(photo.CloseParkGallery(),"Gallery closes independently");Exit(h);h.enabled=true;
        }
    }
}
