using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // Isolated Editor Play fixture: real pointer handlers, sample buffers and persistence.
    // Prepared tapes/accepted audio are fixtures, not a claim of a native-input full playthrough.
    public static class TapeStorageSubmissionChecks
    {
        const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Hidden).Invoke(o,args);
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Hidden).SetValue(o,value);
        static void Check(List<string> log,bool ok,string message)
        {log.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines("output/QA/TapeStorageSubmission_20261105/checks.txt",log);if(!ok)throw new Exception(message);}
        static void Animate(HouseRadioDeck d)
        {
            for(int i=0;i<120;i++)
            {
                d.dualDeck.Tick(.04f,d.transport.SourcePlaying,d.transport.TargetStatus,d.transport.Writing,d.TargetKeyDown(0),null,d.transport.SourceStatus);
                d.workbenchVisuals.Tick(.04f);Call(d,"TickRadioCamera",.04f);Call(d,"TickStorageDelivery",.04f);
            }
            Physics.SyncTransforms();
        }
        static void Pump(TapeSampleTransport t,int count)=>Call(t,"OnAudioFilterRead",new float[count],1);
        static float[] Pcm(TapeSampleTransport t)=>((float[])typeof(TapeSampleTransport).GetField("target",Hidden).GetValue(t)).Take(t.SnapshotMapping().Length).ToArray();
        static void DisableClock(HouseRadioDeck d){d.transport.GetComponent<AudioSource>().Stop();d.transport.GetComponent<AudioSource>().enabled=false;}
        static void Controls(HouseRadioDeck d){d.OpenRecording();Set(d,"recordingWait",false);Set(d,"benchWaiting",false);}
        static void LoadB(HouseRadioDeck d,int index)
        {
            if(!(bool)Call(d,"SelectForDrag",index) || !d.InsertDragged())throw new Exception("Cannot reinsert B");
            Animate(d);Controls(d);
        }
        static void Drop(HouseRadioDeck d,int index,bool cancel=false)
        {
            d.PlaceOnWorkbench(index);Animate(d);Set(d,"benchWaiting",false);
            // Match TryBeginWorkbenchInteraction's ownership handoff. The fixture
            // performs several gestures in one Editor frame instead of polling Update.
            d.house.AdoptPointerGesture();
            d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);d.view.fieldOfView=d.operationView.fieldOfView;
            Physics.SyncTransforms();
            var b=d.workbenchVisuals.candidateGrips[index].bounds;Vector2 from=Vector2.zero;bool found=false;
            foreach(float x in new[]{-.8f,-.4f,0,.4f,.8f})foreach(float z in new[]{-.8f,-.4f,0,.4f,.8f})
            {
                var p=b.center+new Vector3(b.extents.x*x,0,b.extents.z*z);
                var hit=d.PickWorkbenchTarget(new Ray(d.view.transform.position,p-d.view.transform.position));
                if(hit!=d.tapes[index].hit && hit!=d.workbenchVisuals.candidateGrips[index])continue;
                from=d.view.WorldToScreenPoint(p);found=true;break;
            }
            if(!found)throw new Exception("No visible shell grab point");
            var to=(Vector2)d.view.WorldToScreenPoint(d.workbenchVisuals.finishedHit.bounds.center);
            d.WorkbenchPointer(EventType.MouseDown,0,from,Vector2.zero,0);
            if(d.GetType().GetField("benchGesture",Hidden).GetValue(d).ToString()!="Pending")throw new Exception("Shell press did not start a grab");
            d.WorkbenchPointer(EventType.MouseDrag,0,to,to-from,0);
            if(cancel)d.CancelWorkbenchGesture();else d.WorkbenchPointer(EventType.MouseUp,0,to,Vector2.zero,0);
        }
        public static void EjectAndDrop(HouseRadioDeck d)
        {
            int index=d.Selected>=0?d.Selected:d.Chapter.RecordingTapeIndex;
            if(d.Selected>=0){if(!d.EjectDualDeck(1))throw new Exception("Cannot eject B for return");Animate(d);}
            if(!d.workbenchVisuals.LidOpen)d.workbenchVisuals.ToggleLid();Animate(d);
            Drop(d,index);
        }
        public static string Run()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay)throw new Exception("Isolated Editor Play required");
            Directory.CreateDirectory("output/QA/TapeStorageSubmission_20261105");
            var d=WBMain.Instance.radioDeck;var log=new List<string>();
            if(d.Chapter.CommissionIndex!=0)throw new Exception("Fresh tape chapter required");
            WBMain.Instance.enabled=false;d.enabled=false;DisableClock(d);d.SendMessage("OnApplicationFocus",true);d.SetStationActive(true);d.ReleaseInputGate(true);
            if(!d.workbenchVisuals.LidOpen)d.workbenchVisuals.ToggleLid();Animate(d);
            Check(log,!UnityEngine.Object.FindObjectsOfType<Transform>().Any(t=>t.name=="Finished Tape Rack"),"External rack removed");
            Check(log,d.finishedTapeAnchors.All(a=>a.IsChildOf(d.workbenchVisuals.storage.transform)),"All finished anchors belong to storage box");
            var committed=new List<byte[]>();
            for(int n=0;n<3;n++)
            {
                if(d.SuccessPopupOpen)Call(d.transportPanel,"CloseSuccess");
                int index=n+2;var chapter=d.Chapter;var data=chapter.Tape.Candidates[index];
                data.LeftUsed=data.RightUsed=data.StartReached=data.EndReached=data.HoleA=data.HoleB=true;
                for(int i=0;i<data.Seen.Length;i++)data.Seen[i]=true;data.Scanned=data.Seen.Length;
                chapter.AuditionComplete=true;
                if(!chapter.SourceLoaded)d.LoadOriginal();Animate(d);DisableClock(d);LoadB(d,index);
                if(n==0)
                {
                    d.SetUITransportCommand(0);Pump(d.transport,100);d.SetUITransportCommand(0);
                    var bad=d.transport.SnapshotMapping();var badPcm=Pcm(d.transport);
                    Check(log,d.EjectDualDeck(1),"Eject invalid recording");Animate(d);
                    Drop(d,index);
                    Check(log,!d.SubmissionSucceeded && d.SubmissionMessage.StartsWith("Not accepted:") && chapter.CommissionIndex==0 && bad.SequenceEqual(d.transport.SnapshotMapping()) && badPcm.SequenceEqual(Pcm(d.transport)),"Rejected drop preserves exact recording samples/mapping and commission");
                    Check(log,!chapter.IsArchived(index) && !d.workbenchVisuals.InStorage(d.tapes[index].root),"Rejected tape stays on desk, not in finished compartment");
                    var saved=chapter.CaptureCheckpoint();d.transport.Initialize(((TapeCommissionAudio)typeof(HouseRadioDeck).GetField("commissionAudio",Hidden).GetValue(d)).Samples,AudioSettings.outputSampleRate,0);DisableClock(d);
                    chapter.RestoreCheckpoint(saved);d.RestoreSource();d.RestoreWorkbench();
                    Check(log,chapter.RecordingTapeIndex==index && bad.SequenceEqual(d.transport.SnapshotMapping()) && badPcm.SequenceEqual(Pcm(d.transport)),"Reload preserves ejected tape identity and rejected recording");
                    LoadB(d,index);
                }
                if(chapter.RecordingTapeIndex<0)Check(log,chapter.BindRecordingTape(index),"Bind current recorded identity");
                var audio=(TapeCommissionAudio)typeof(HouseRadioDeck).GetField("commissionAudio",Hidden).GetValue(d);
                Check(log,d.transport.EditorPrepareAcceptedCopy(audio),"Prepare accepted real PCM fixture "+(n+1));
                var before=d.transport.SnapshotMapping();
                if(n==0)
                {
                    Set(d.transport,"a",d.transport.Rate*2);Set(d.transport,"b",d.transport.Rate*2);
                    d.PressTransportKey(KeyCode.J);Pump(d.transport,100);
                    Check(log,d.transport.SourceSeconds<2 && d.transport.TargetSeconds<2,"J rewinds both loaded tapes");
                    d.PressTransportKey(KeyCode.J);
                    Check(log,!d.transport.SourceRunning && !d.transport.TargetRunning,"Second J stops both winding motors");
                    float a=d.transport.SourceSeconds,bpos=d.transport.TargetSeconds;
                    d.PressDualControl("A_Forward");Pump(d.transport,100);
                    Check(log,d.transport.SourceSeconds>a && d.transport.TargetSeconds>bpos,"Physical fast-forward button moves both cursors");
                    d.PressDualControl("A_Forward");
                    Check(log,before.SequenceEqual(d.transport.SnapshotMapping()),"Shared winding never writes or edits recorded content");
                    Set(d.transport,"a",0);Set(d.transport,"b",before.Length-4);
                    d.PressTransportKey(KeyCode.L);Pump(d.transport,100);
                    Check(log,!d.transport.TargetRunning && d.transport.SourceRunning && Mathf.Abs(d.transport.TargetSeconds-d.transport.RecordedSeconds)<.001f,"B stops at recorded end without stopping A winding");
                    d.PressTransportKey(KeyCode.L);
                    Check(log,d.EjectDualDeck(0),"Eject A for B-only winding");Animate(d);Controls(d);
                    float emptyA=d.transport.SourceSeconds;float loadedB=d.transport.TargetSeconds;
                    d.PressTransportKey(KeyCode.J);Pump(d.transport,100);
                    Check(log,d.transport.SourceSeconds==emptyA && d.transport.TargetSeconds<loadedB,"J still rewinds B when A is empty");d.PressTransportKey(KeyCode.J);
                    d.LoadOriginal();Animate(d);Controls(d);
                    d.SetUITransportCommand(0);d.PressTransportKey(KeyCode.J);
                    Check(log,d.transport.Writing && d.transport.SourceStatus!="REWINDING","Winding is blocked while B records");d.SetUITransportCommand(0);
                }
                Check(log,d.EjectDualDeck(1),"Eject accepted tape "+(n+1));Animate(d);
                if(n==0)
                {
                    var p=d.tapes[index].root.position;Drop(d,index,true);
                    Check(log,chapter.CommissionIndex==0 && Vector3.Distance(p,d.tapes[index].root.position)<.001f,"Cancel return restores desk pose without submitting");
                    Call(d,"StorageClick",d.workbenchVisuals.finishedHit);
                    Check(log,chapter.CommissionIndex==0,"Clicking empty finished compartment does not submit");
                    LoadB(d,index);Check(log,d.transport.SnapshotMapping().SequenceEqual(before),"Reinsert after cancel preserves full audio");d.EjectDualDeck(1);Animate(d);
                }
                Drop(d,index);
                Check(log,d.SubmissionSucceeded && chapter.CommissionIndex==n+1 && chapter.FinishedCommission(index)==n,"Drop commits correct tape exactly once "+(n+1));
                Check(log,Vector3.Distance(d.tapes[index].root.position,d.finishedTapeAnchors[n].position)<.001f,"Actual work tape moves to box finished pose");
                DisableClock(d);
                var file=Path.Combine(GameSavePaths.Root,n==0?"tape-one-pcm-v1.bin":"tape-commission-"+n+"-pcm.bin");committed.Add(File.ReadAllBytes(file));
                for(int i=0;i<committed.Count;i++)Check(log,committed[i].SequenceEqual(File.ReadAllBytes(Path.Combine(GameSavePaths.Root,i==0?"tape-one-pcm-v1.bin":"tape-commission-"+i+"-pcm.bin"))),"Earlier PCM unchanged: "+i);
            }
            var snapshot=d.Chapter.CaptureCheckpoint();d.Chapter.RestoreCheckpoint(snapshot);d.ResetDeck();d.RestoreFinishedTapes();
            Check(log,d.Chapter.Completed && Enumerable.Range(2,3).All(i=>Vector3.Distance(d.tapes[i].root.position,d.finishedTapeAnchors[i-2].position)<.001f),"Completed checkpoint restores all three real tapes in box");
            return string.Join("\n",log);
        }
    }
}
