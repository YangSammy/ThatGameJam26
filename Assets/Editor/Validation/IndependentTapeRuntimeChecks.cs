using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Controller/physics fixtures in the backed-up, reset-on-Play test slot.
    // These exercise real sample transport and commits, not OS input timing.
    public static class IndependentTapeRuntimeChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
        static void Check(List<string> log,bool ok,string label)
        {log.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(string.Join("\n",log));}
        static HouseRadioDeck Deck()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || WBMain.Instance?.radioDeck==null)
                throw new InvalidOperationException("Fresh isolated Spring Editor Play required.");
            var d=WBMain.Instance.radioDeck;WBMain.Instance.enabled=false;d.enabled=false;
            d.transport.GetComponent<AudioSource>().Stop();d.transport.GetComponent<AudioSource>().enabled=false;
            d.SendMessage("OnApplicationFocus",true);d.SetStationActive(true);d.ReleaseInputGate(true);return d;
        }
        static void Animate(HouseRadioDeck d)
        {
            for(int i=0;i<100;i++)
            {
                d.dualDeck.Tick(.04f,d.transport.SourcePlaying,d.transport.TargetStatus,d.transport.Writing,d.TargetKeyDown(0),null,d.transport.SourceStatus);
                d.workbenchVisuals.Tick(.04f);Call(d,"TickRadioCamera",.04f);Call(d,"TickStorageDelivery",.04f);
            }
            Physics.SyncTransforms();
        }
        static void Pump(TapeSampleTransport t,int count)=>Call(t,"OnAudioFilterRead",new float[count],1);
        static string Write(string name,List<string> log)
        {
            const string path="output/QA/IndependentWorkTapes_20261105";
            Directory.CreateDirectory(path);string result=string.Join("\n",log);File.WriteAllText(path+"/"+name+".txt",result);return result;
        }
        public static string StorageGeometry()
        {
            var d=Deck();var log=new List<string>();
            Check(log,d.tapes.Length==5 && d.Chapter.Tape.Candidates.Length==5,"Five separate physical/model work tapes");
            if(!d.workbenchVisuals.LidOpen)d.workbenchVisuals.ToggleLid();Animate(d);
            d.view.transform.SetPositionAndRotation(d.storageCloseView.transform.position,d.storageCloseView.transform.rotation);
            d.view.fieldOfView=d.storageCloseView.fieldOfView;Physics.SyncTransforms();
            for(int i=0;i<5;i++)
            {
                var ray=new Ray(d.view.transform.position,d.tapes[i].hit.bounds.center-d.view.transform.position);
                var hit=d.PickWorkbenchTarget(ray);
                int candidate=(int)Call(d,"CandidateForHit",hit);
                Check(log,candidate==i,"Open storage camera picks work tape "+(i+1)+" ("+(hit?hit.name:"none")+")");
            }
            TapeToggleStorageChecks.Screenshot("independent-five-in-box",d.view);
            return Write("storage-geometry",log);
        }
        public static string RemainingCopies()
        {
            var d=Deck();var log=new List<string>();var chapter=d.Chapter;
            Check(log,chapter.CommissionIndex==1 && chapter.IsArchived(2),"First real accepted recording archives tape 03");
            var firstFile=Path.Combine(GameSavePaths.Root,"tape-one-pcm-v1.bin");var firstBytes=File.ReadAllBytes(firstFile);
            for(int commission=1;commission<3;commission++)
            {
                if(d.SuccessPopupOpen)Call(d.transportPanel,"CloseSuccess");
                d.ReleaseInputGate(true);Set(d,"benchWaiting",false);
                Check(log,!(bool)Call(d,"SelectForDrag",2),"Archived first cassette cannot be loaded again");
                int index=commission+2;var root=d.tapes[index].root;var data=chapter.Tape.Candidates[index];
                Check(log,!data.Intact && !data.HoleA && !data.HoleB,"Next cassette "+(index+1)+" starts unchecked and unsealed");
                Check(log,d.LoadOriginal(),"Next commission loads its own original");Animate(d);chapter.AuditionComplete=true;
                Check(log,(bool)Call(d,"SelectForDrag",index) && d.InsertDragged(),"Fresh cassette can enter B for preparation guard test");Animate(d);
                d.OpenRecording();Set(d,"recordingWait",false);d.SetUITransportCommand(0);
                Check(log,!d.transport.Writing && chapter.RecordingTapeIndex<0,"Unchecked fresh cassette cannot record");
                Check(log,d.EjectDualDeck(1),"Eject unchecked cassette");Animate(d);
                var state=chapter.Tape;state.Selected=index;state.SourceVerified=false;state.BeginSpindle(0);
                for(int step=0;step<12;step++)state.Turn(3);
                state.BeginSpindle(1);for(int step=0;step<12;step++)state.Turn(3);state.EndSpindle();
                Check(log,data.Intact && !data.Prepared,"Both real spindle scans complete; holes still required");
                data.HoleA=true;
                Check(log,!chapter.BindRecordingTape(index),"One sealed hole cannot prepare a recording");data.HoleB=true;
                Check(log,(bool)Call(d,"SelectForDrag",index) && d.InsertDragged(),"Prepared fresh cassette loads B");Animate(d);
                d.OpenRecording();Set(d,"recordingWait",false);d.SetUITransportCommand(0);Pump(d.transport,100);d.SetUITransportCommand(0);
                d.SubmitCurrentRecording();
                Check(log,!d.SubmissionSucceeded && chapter.CommissionIndex==commission && !chapter.IsArchived(index),"Rejected silent recording does not consume or archive the tape");
                Check(log,d.EjectDualDeck(1),"Eject partially recorded cassette");Animate(d);
                if(commission==1)Check(log,!(bool)Call(d,"SelectForDrag",4),"Partial recording cannot transfer onto another cassette");
                Check(log,(bool)Call(d,"SelectForDrag",index) && d.InsertDragged(),"Same recorded cassette can be reinserted");Animate(d);
                d.OpenRecording();Set(d,"recordingWait",false);d.RetryCurrentCommission();Set(d,"recordingWait",false);
                Check(log,data.Prepared && d.Selected==index && chapter.RecordingTapeIndex<0 && d.transport.RecordedSeconds==0,"Retry preserves selected tape and both sealed holes");
                d.PressTransportKey(KeyCode.F);
                var audio=(TapeCommissionAudio)typeof(HouseRadioDeck).GetField("commissionAudio",Hidden).GetValue(d);
                for(int start=0;start<audio.CleanMap.Length;)
                {
                    bool clean=audio.CleanMap[start]>=0;int end=start+1;
                    while(end<audio.CleanMap.Length && (audio.CleanMap[end]>=0)==clean)end++;
                    if(d.transport.Writing!=clean)d.PressTransportKey(KeyCode.S);
                    Pump(d.transport,end-start);start=end;
                }
                if(d.transport.Writing)d.PressTransportKey(KeyCode.S);
                Check(log,audio.Reject(d.transport.SnapshotMapping())=="","Real sample copy "+(commission+1)+" passes existing noise/content acceptance");
                TapeStorageSubmissionChecks.EjectAndDrop(d);
                Check(log,d.SubmissionSucceeded && chapter.FinishedCommission(index)==commission && d.tapes[index].root==root,"Drop archives the SAME cassette instance for copy "+(commission+1));
                Check(log,d.Selected==-1 && chapter.WorkingTapeIndex==-1 && Vector3.Distance(root.position,d.finishedTapeAnchors[commission].position)<.001f,"B empties and accepted cassette occupies its finished slot");
                Check(log,firstBytes.SequenceEqual(File.ReadAllBytes(firstFile)),"Earlier completed PCM remains byte-identical");
                d.transport.GetComponent<AudioSource>().Stop();d.transport.GetComponent<AudioSource>().enabled=false;
            }
            Check(log,chapter.Completed && Enumerable.Range(0,5).Count(chapter.IsArchived)==3,"Three different tapes complete all three commissions");
            string checkpoint=chapter.CaptureCheckpoint();var restored=new RadioTapeChapter();restored.RestoreCheckpoint(checkpoint);
            Check(log,restored.Completed && Enumerable.Range(2,3).All(restored.IsArchived),"Completed checkpoint roundtrip retains three separate finished identities");
            d.ResetDeck();d.RestoreSource();d.RestoreWorkbench();
            Check(log,Enumerable.Range(2,3).All(i=>Vector3.Distance(d.tapes[i].root.position,d.finishedTapeAnchors[i-2].position)<.001f),"Reset and restore do not return finished tapes to storage");
            Check(log,d.completedCopies.All(copy=>!copy.activeSelf),"Decorative duplicates remain hidden");
            d.workbenchVisuals.Tick(1);d.AdvanceInspection(1);
            d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);d.view.fieldOfView=d.operationView.fieldOfView;
            TapeToggleStorageChecks.Screenshot("independent-three-finished",d.view);
            return Write("remaining-copies",log);
        }
    }
}
