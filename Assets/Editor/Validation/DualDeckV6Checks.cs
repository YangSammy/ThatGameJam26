using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class DualDeckV6Checks
    {
        public static string PointerRun()
        {
            var h=WBMain.Instance;h.enabled=false;h.SendMessage("OnApplicationFocus",true);var d=h.radioDeck;d.enabled=false;d.SendMessage("OnApplicationFocus",true);
            d.SetStationActive(true);d.ReleaseInputGate(true);d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);
            var f=BindingFlags.NonPublic|BindingFlags.Instance;
            Ray At(Collider c){Physics.SyncTransforms();return new Ray(d.view.transform.position,c.bounds.center-d.view.transform.position);}
            var source=d.GetType().GetMethod("SourcePointer",f);
            source.Invoke(d,new object[]{EventType.MouseDown,0,At(d.originalHit)});
            source.Invoke(d,new object[]{EventType.MouseDrag,0,At(d.sourceSlot)});
            source.Invoke(d,new object[]{EventType.MouseUp,0,At(d.sourceSlot)});
            if(!d.Chapter.SourceLoaded)throw new Exception("Source physical drag did not load A");
            for(int i=0;i<100;i++)d.dualDeck.Tick(.04f,false,"EMPTY",false);
            d.Chapter.AuditionComplete=true;d.RestoreSource();
            d.GetType().GetMethod("SelectForDrag",f).Invoke(d,new object[]{2});
            d.GetType().GetMethod("BeginTapeDrag",f).Invoke(d,new object[]{At(d.tapes[2].hit)});
            var ray=At(d.slotHit);d.GetType().GetField("dragRay",f).SetValue(d,ray);d.PoseDraggedTape(.1f);
            var session=(ObjectDragSession)d.GetType().GetField("tapeDrag",f).GetValue(d);
            bool can=(bool)d.GetType().GetMethod("TapeReceiver",f).Invoke(d,new object[]{d.slotHit,ray});
            if(session.Blocked||!can)throw new Exception("B physical drop blocked: sweep="+session.Blocked+" receiver="+can);
            d.GetType().GetMethod("EndTapeDrag",f).Invoke(d,new object[]{true});
            for(int i=0;i<100;i++)d.dualDeck.Tick(.04f,false,"EMPTY",false);
            if(d.Selected!=2||d.State!=HouseRadioDeck.Phase.Paused)throw new Exception("B did not complete loading");
            return "POINTER_LOAD_A_B_PASS";
        }
        public static string Run()
        {
            if(!Application.isPlaying)throw new Exception("Play HouseScene first");
            var h=WBMain.Instance;h.enabled=false;h.SendMessage("OnApplicationFocus",true);
            var d=h.radioDeck;d.enabled=false;d.SendMessage("OnApplicationFocus",true);
            var f=BindingFlags.Instance|BindingFlags.NonPublic;var checks=new List<string>();
            void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add("PASS "+name);File.WriteAllLines("QA/DualDeckV6/runtime.txt",checks);}
            void Animate(){for(int i=0;i<100;i++)d.dualDeck.Tick(.04f,d.transport.SourcePlaying,d.transport.TargetStatus,d.transport.Writing);}
            // Keep a usable playback window: a 0.1s copy can naturally finish while
            // a slower editor saves the scene checkpoint during the independence test.
            void Audio(int frames=4800){d.transport.GetType().GetMethod("OnAudioFilterRead",f).Invoke(d.transport,new object[]{new float[frames],1});}
            void LoadB(){Check((bool)d.GetType().GetMethod("SelectForDrag",f).Invoke(d,new object[]{2}),"Select existing B identity");Check(d.InsertDragged(),"Begin B load");Check(!d.InsertDragged(),"Reject duplicate B load");Animate();}
            d.transport.GetComponent<AudioSource>().Stop();d.SetStationActive(true);d.ReleaseInputGate(true);
            d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);Physics.SyncTransforms();
            foreach(var hit in d.dualDeck.keyHits)Check(StudyEntry.PickVisible(new Ray(d.view.transform.position,hit.bounds.center-d.view.transform.position))==hit,"Authored camera hits "+d.dualDeck.Command(hit));
            Check(d.LoadOriginal(),"Load existing A identity");Check(!d.LoadOriginal(),"Reject duplicate A load");Animate();
            Check(d.dualDeck.Closed(0)&&d.Chapter.SourceLoaded,"A load closes its door");
            d.Chapter.AuditionComplete=true;var c=d.Chapter.Tape.Candidates[2];c.LeftUsed=c.RightUsed=c.StartReached=c.EndReached=c.HoleA=c.HoleB=true;c.Scanned=400;for(int i=0;i<400;i++)c.Seen[i]=true;
            d.transport.SourceCommand(2);d.transport.SourceCommand(1);LoadB();
            Check(d.transport.SourcePlaying,"Loading B does not stop A");
            d.GetType().GetField("recordingWait",f).SetValue(d,false);d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,true,false,false,false);Audio(96000);
            Check(d.transport.Writing&&d.transport.RecordedSeconds>0,"B writes existing real sample buffer");
            d.dualDeck.Tick(.05f,true,d.transport.TargetStatus,true);
            var lamp=d.dualDeck.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Deck_B_RecordLamp");var lit=lamp.localScale;
            Check(!d.originalTape.GetComponentsInChildren<Renderer>().Any(r=>r.enabled)&&!d.tapes[2].root.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Only visual substitutes visible while loaded");
            d.PressDualControl("B_Stop");Check(d.transport.SourcePlaying&&!d.transport.Writing,"B stop preserves A");
            d.dualDeck.Tick(.05f,true,d.transport.TargetStatus,false);Check(Vector3.Distance(lit,lamp.localScale)>.001f,"Record light follows actual writing state");
            var reel=d.dualDeck.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Deck_B_Tape_Reel_L_PIVOT");var angle=reel.localRotation;d.dualDeck.Tick(.5f,true,d.transport.TargetStatus,false);Check(Quaternion.Angle(angle,reel.localRotation)<.001f,"Stopped B reel holds its angle");
            float before=d.transport.SourceSeconds;Check(d.EjectDualDeck(1),"B eject begins");Check(!d.EjectDualDeck(1),"Repeated eject rejected");Animate();Audio();
            Check(d.transport.SourcePlaying&&d.transport.SourceSeconds>before,"A audio cursor continues through B unload");
            Check(d.Selected==-1&&d.Chapter.WorkingTapeIndex==-1&&d.dualDeck.IsOpen(1),"B returns identity to table and stays open");
            Check(d.tapes[2].root.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Original B renderer restored");
            d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,true,false,false,false);Check(!d.transport.Writing,"Empty B cannot record");
            LoadB();d.GetType().GetField("recordingWait",f).SetValue(d,false);d.transport.RewindTarget();d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,false,false,false,false);
            Check(d.EjectDualDeck(0),"A eject begins");Check(d.transport.TargetStatus=="PLAYBACK","A eject does not stop B playback");Animate();
            Check(!d.Chapter.SourceLoaded&&d.Selected==2,"A eject preserves B identity");
            Check(d.dualDeck.IsOpen(0),"A eject leaves door open");
            d.PressDualControl("A_Play");Check(!d.transport.SourcePlaying,"Empty A cannot play");
            d.GetType().GetMethod("HandoffToWorkbench",f).Invoke(d,null);Check(d.transport.TargetStatus=="PLAYBACK","A loading input handoff preserves B playback");
            Check(d.LoadOriginal(),"Reload A");Animate();Check(d.transport.TargetStatus=="PLAYBACK","Reloading A preserves B playback");
            d.EjectDualDeck(0);Animate();
            Check(d.ToggleDualDoor(1),"B door opens independently");Animate();d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,false,false,false,false);
            Check(d.transport.TargetStatus!="PLAYBACK","Open B cannot run");
            string save=d.Chapter.CaptureCheckpoint();var restored=new RadioTapeChapter();restored.RestoreCheckpoint(save);Check(restored.WorkingTapeIndex==2&&!restored.SourceLoaded,"Existing save schema retains cassette ownership");
            d.PrepareForHide();Check(!d.transport.SourcePlaying&&!d.transport.Writing,"Exit stops transport");
            return "DUAL_DECK_PASS "+checks.Count;
        }
    }
}
