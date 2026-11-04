using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // Explicit development-only probes. Invoke in a fresh Editor Play session, never on production saves.
    public static class TapeP0FlowChecks
    {
        static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        static HouseRadioDeck d;static WBMain h;static List<string> results;
        static object Call(string name,params object[] args)=>d.GetType().GetMethod(name,F).Invoke(d,args);
        static T Get<T>(string name)=>(T)d.GetType().GetField(name,F).GetValue(d);
        static void Set(string name,object value)=>d.GetType().GetField(name,F).SetValue(d,value);
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS "+text);Directory.CreateDirectory("QA/P0-2026-10-31");File.WriteAllLines("QA/P0-2026-10-31/tape-flow.txt",results);}
        static void Ready(){Set("benchWaiting",false);Set("recordingWait",false);d.ReleaseInputGate(true);h.AdoptPointerGesture();Call("TickWorkbenchAssets",.01f);Physics.SyncTransforms();}
        static void Settle(){Call("FocusLoadedRadio");for(int i=0;i<100;i++){d.dualDeck.Tick(.04f,d.transport.SourcePlaying,d.transport.TargetStatus,d.transport.Writing);Call("TickRadioCamera",.04f);Call("TickPreparationCamera",.04f);}Ready();}
        static void Pump(int frames){d.transport.GetType().GetMethod("OnAudioFilterRead",F).Invoke(d.transport,new object[]{new float[frames],1});}
        static Vector2 Point(Vector3 v)=>d.view.WorldToScreenPoint(v);
        static void Pointer(EventType type,Vector2 p,float wheel=0)=>d.WorkbenchPointer(type,0,p,Vector2.zero,wheel);
        static void Patch(int side)
        {
            Ready();Vector2 head=Point(d.adhesiveTab.bounds.center),cut=Point(d.adhesiveCutter.bounds.center),end=cut+(cut-head).normalized*30;
            Pointer(EventType.MouseDown,head);Pointer(EventType.MouseDrag,end);Check(d.Chapter.LooseStripReady,"Cutter produces one strip "+side);
            var hole=Point(d.tapes[2].preparation.pieces[side].transform.position);Pointer(EventType.MouseDrag,hole);Pointer(EventType.MouseUp,hole);Call("TickWorkbenchAssets",.6f);
            Check(side==0?d.Chapter.Tape.Candidates[2].HoleA:d.Chapter.Tape.Candidates[2].HoleB,"Physical patch completes hole "+side);
        }
        public static string Run()
        {
            if(!GameSavePaths.ResetOnEditorPlay)throw new Exception("Enable isolated reset-on-play development saves before running this destructive fixture.");
            h=WBMain.Instance;if(!Application.isPlaying || h==null)throw new Exception("Fresh HouseScene Editor Play required");
            h.enabled=false;h.SendMessage("OnApplicationFocus",true);d=h.radioDeck;d.enabled=false;d.SendMessage("OnApplicationFocus",true);
            results=new List<string>();d.SetStationActive(true);d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);d.view.fieldOfView=d.operationView.fieldOfView;
            d.transport.GetComponent<AudioSource>().Stop();
            d.OpenWorkbench();d.LoadOriginal();d.CloseWorkbench();d.CloseRecording();Call("FocusLoadedRadio");Check(!d.RecordingMode,"Cancelled A load focus cannot reopen workspace");Settle();
            d.EjectDualDeck(0);Settle();d.OpenRecording();Ready();
            Vector2 source=Point(d.originalHit.bounds.center),slot=Point(d.sourceSlot.bounds.center);
            Call("RecordingPointerDown",source);
            Check(Get<bool>("movingOriginal") && d.WorkbenchMode,"Recording native press hands off the original press ray to source drag");
            Pointer(EventType.MouseDrag,slot);Pointer(EventType.MouseUp,slot);Call("FocusLoadedRadio");
            Check(d.Chapter.SourceLoaded && d.DeckBusy(0) && Get<bool>("radioFocused"),"A drag starts close view during loading");Settle();
            Check(d.RecordingPanelVisible,"Close recorder view shows transport controls");
            d.transport.SourceCommand(1);d.FocusRadio(false);Settle();
            Check(!d.RecordingPanelVisible && d.transport.SourcePlaying,"Desk hides blocking controls without stopping A");
            float heard=d.Chapter.AuditionSeconds;Pump(d.transport.Rate);Call("TickAudition");
            Check(d.Chapter.AuditionSeconds>=heard+.99f,"Audition counts audio across a one-second render stall");
            Call("UpdateTransportDisplay");
            Check(d.sourceCounter.text.EndsWith(d.transport.SourceSeconds.ToString("000.0")),"Dual-deck tooltip counter follows actual source cursor");
            d.sourceCounter.text=d.targetCounter.text="stale authored counter";d.InitializeRecording();
            Check(d.sourceCounter.text.EndsWith(d.transport.SourceSeconds.ToString("000.0")) && d.targetCounter.text.EndsWith(d.transport.TargetSeconds.ToString("000.0")),"Restored recording initializes counters before entering a work view");
            d.transport.SourceCommand(1);for(int i=0;i<110;i++){Pump(d.transport.Rate/10);Call("TickAudition");}
            Check(d.Chapter.AuditionComplete,"Audible playback unlocks preparation");d.transport.SourceCommand(0);
            d.OpenWorkbench();Ready();Call("TickWorkbenchAssets",.01f);
            var tape=d.tapes[2];Vector2 shell=Point(tape.root.position),mat=Point(d.workMat.bounds.center);
            Pointer(EventType.MouseDown,shell);Pointer(EventType.MouseDrag,mat);Check(d.preparationDropPreview.enabled,"Mat drop preview");Pointer(EventType.MouseUp,mat);Settle();
            Check(Get<bool>("preparationFocused"),"Drop enters preparation close view");
            var home=tape.root.position;var shellAt=Point(home);var outside=Point(d.workMat.bounds.center+Vector3.right*.35f);
            Pointer(EventType.MouseDown,shellAt);Pointer(EventType.MouseDrag,outside);Check(Get<bool>("preparationCarryReturn"),"Drag away starts automatic return");
            var drag=Get<ObjectDragSession>("benchDrag");Vector2 grip=Point(drag.ScreenCarryGrip);Call("TickPreparationCamera",.15f);
            Check(Vector2.Distance(grip,Point(drag.ScreenCarryGrip))<.1f,"Camera return retains grabbed screen point");
            Check(d.CancelWorkbenchGesture(),"Cancel during camera return");Settle();Check(Vector3.Distance(tape.root.position,home)<.001f && !drag.Active,"Cancel restores stable mat placement");
            d.PlaceTapeOnPreparationMat(false);Settle();
            for(int side=0;side<2;side++){var reel=Point((side==0?tape.leftSpindleHit:tape.rightSpindleHit).bounds.center);Pointer(EventType.MouseDown,reel);for(int i=0;i<12;i++)Pointer(EventType.ScrollWheel,reel,3);Pointer(EventType.MouseUp,reel);}
            Check(d.Chapter.Tape.Candidates[2].Intact,"Actual left and right reel input covers whole tape");
            d.PlaceTapeOnPreparationMat(true);Settle();Patch(0);
            string checkpoint=d.Chapter.CaptureCheckpoint();var restored=new RadioTapeChapter();restored.RestoreCheckpoint(checkpoint);
            Check(restored.Tape.Candidates[2].Intact && restored.Tape.Candidates[2].HoleA && !restored.Tape.Candidates[2].HoleB,"One-hole checkpoint preserves coverage and separate holes");
            var area=OperationArea.Capture();var savedPosition=tape.root.position;var savedRotation=tape.root.rotation;tape.root.position+=Vector3.one;OperationArea.Restore(area);d.RestoreWorkbench();
            Check(Vector3.Distance(savedPosition,tape.root.position)<.001f && Quaternion.Angle(savedRotation,tape.root.rotation)<.01f,"Stable mat pose restores");
            string before=d.Chapter.CaptureCheckpoint();Check(h.safeInspection.Open(h.safeInspection.ForSource(tape.root)),"Safe inspection opens");h.safeInspection.Close();
            Check(before==d.Chapter.CaptureCheckpoint(),"Safe inspection does not modify tape data");Ready();Patch(1);
            Check(d.Chapter.Tape.Candidates[2].Prepared,"Both physical patches prepare B");
            d.PlaceTapeOnPreparationMat(false);Settle();shell=Point(tape.root.position);outside=Point(d.workMat.bounds.center+Vector3.right*.35f);Pointer(EventType.MouseDown,shell);Pointer(EventType.MouseDrag,outside);Settle();
            slot=Point(d.slotHit.bounds.center);Pointer(EventType.MouseDrag,slot);Pointer(EventType.MouseUp,slot);Call("FocusLoadedRadio");
            Check(d.Selected==2 && d.DeckBusy(1) && Get<bool>("radioFocused"),"Continuous mat-to-B drag starts close view during animation");Settle();
            for(int commission=0;commission<3;commission++)
            {
                if(commission>0){d.OpenWorkbench();Ready();Check(d.LoadOriginal(),"Next source loads "+commission);Settle();d.transport.SourceCommand(1);for(int i=0;i<300&&!d.Chapter.AuditionComplete;i++){Pump(d.transport.Rate/10);Call("TickAudition");}Check(d.Chapter.AuditionComplete,"Next audition completes "+commission);d.PlaceOnWorkbench(2);Ready();d.PlaceTapeOnPreparationMat(false);Settle();
                    for(int side=0;side<2;side++){var reel=Point((side==0?tape.leftSpindleHit:tape.rightSpindleHit).bounds.center);Pointer(EventType.MouseDown,reel);for(int i=0;i<12;i++)Pointer(EventType.ScrollWheel,reel,3);Pointer(EventType.MouseUp,reel);}
                    d.PlaceTapeOnPreparationMat(true);Settle();Patch(0);Patch(1);Call("SelectForDrag",2);Check(d.InsertDragged(),"New work tape preparation loads "+commission);Settle();}
                var audio=Get<TapeCommissionAudio>("commissionAudio");d.transport.GetComponent<AudioSource>().Stop();d.transport.RetryCurrent();d.transport.SourceCommand(1);
                // Test fixture supplies deliberate stop/resume decisions. Production controller never reads this map.
                for(int at=0;at<audio.CleanMap.Length;){bool clean=audio.CleanMap[at]>=0;int end=at+1;while(end<audio.CleanMap.Length && (audio.CleanMap[end]>=0)==clean)end++;d.transport.TargetCommand(clean,clean,0,false);Pump(end-at);at=end;}
                d.transport.StopAll();int[] map=d.transport.SnapshotMapping();Check(audio.Reject(map)=="","Real recorded sample mapping passes commission "+commission);
                Check(d.transport.Save(),"PCM save "+commission);d.transport.Initialize(audio.Samples,audio.Rate,commission);d.transport.GetComponent<AudioSource>().Stop();Check(map.SequenceEqual(d.transport.SnapshotMapping())&&!d.transport.Writing&&!d.transport.SourcePlaying,"PCM reload preserves result and stops transports "+commission);
                d.transport.RewindTarget();d.transport.TargetCommand(true,false,0,false);Pump(map.Length+100);Check(d.transport.TargetStatus!="PLAYBACK" && d.transport.RecordedSeconds>0,"Actual result playback reaches end "+commission);
                d.FocusRadio(true);Settle();var deskPosition=Get<Vector3>("radioReturnPosition");
                if(commission==2)
                {
                    h.GetType().GetProperty("InStation").GetSetMethod(true).Invoke(h,new object[]{true});
                    h.GetType().GetField("houseTransitionDuration",F).SetValue(h,.5f);
                    foreach(var field in new[]{"houseFrom","houseTo"})h.GetType().GetField(field,F).SetValue(h,d.view.transform.position);
                    foreach(var field in new[]{"houseFromRotation","houseToRotation"})h.GetType().GetField(field,F).SetValue(h,d.view.transform.rotation);
                    foreach(var field in new[]{"houseFromFov","houseToFov"})h.GetType().GetField(field,F).SetValue(h,d.view.fieldOfView);
                    h.GetType().GetField("transition",F).SetValue(h,.5f);
                }
                d.SubmitCurrentRecording();Check(d.SubmissionSucceeded && d.Chapter.CommissionIndex==commission+1,"Commit advances only current commission "+commission);
                Check(h.InputContext.Current==InputContextId.Modal,"Success modal owns input even with a pending chapter transition "+commission);
                if(commission==2)Check(!(bool)h.GetType().GetField("leaveCompletedChapter",F).GetValue(h),"Final tape completion retains recorder for replay");
                d.transportPanel.GetType().GetMethod("CloseSuccess",F).Invoke(d.transportPanel,null);
                // The synthetic transition has no authored camera interpolation data.
                // Remove it before the live player Update resumes after this fixture.
                if(commission==2)
                {
                    h.GetType().GetField("transition",F).SetValue(h,0f);
                    h.GetType().GetProperty("InStation").GetSetMethod(true).Invoke(h,new object[]{false});
                    h.SyncInputContext();
                }
                if(commission<2)Check(!Get<bool>("radioFocused") && d.WorkbenchMode && Vector3.Distance(d.view.transform.position,deskPosition)<.001f,"Closing success returns to source selection on desk "+commission);
            }
            Check(d.Chapter.Completed,"All three commissions complete");return "TAPE_P0_PASS "+results.Count;
        }
    }
}


