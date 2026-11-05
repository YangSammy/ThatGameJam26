using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Play-mode controller/raycast fixtures. These do not certify OS mouse/keyboard input.
    public static class TapeToggleStorageChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static HouseRadioDeck Deck
        {
            get
            {
                if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay)throw new InvalidOperationException("Isolated Editor Play session required.");
                return WBMain.Instance.radioDeck;
            }
        }
        static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Hidden).SetValue(target,value);
        static void Check(List<string> log,bool ok,string label){log.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(string.Join("\n",log));}
        static void Audio(TapeSampleTransport t,int count)=>Call(t,"OnAudioFilterRead",new float[count],1);
        static void Animate(HouseRadioDeck d)
        {
            for(int i=0;i<100;i++)
            {
                d.dualDeck.Tick(.04f,d.transport.SourcePlaying,d.transport.TargetStatus,d.transport.Writing,d.TargetKeyDown(0),null,d.transport.SourceStatus);
                d.workbenchVisuals.Tick(.04f);Call(d,"TickRadioCamera",.04f);Call(d,"TickStorageDelivery",.04f);
            }
            Physics.SyncTransforms();
        }
        static string Write(string name,List<string> lines)
        {Directory.CreateDirectory("output/QA/TapeToggleStorage_20261105");var result=string.Join("\n",lines);File.WriteAllText("output/QA/TapeToggleStorage_20261105/"+name+".txt",result);return result;}
        public static string Storage()
        {
            var d=Deck;var h=WBMain.Instance;var log=new List<string>();
            h.enabled=false;d.enabled=false;d.transport.GetComponent<AudioSource>().Stop();
            h.SendMessage("OnApplicationFocus",true);d.SendMessage("OnApplicationFocus",true);
            Call(h,"EnterHouseStation",0);h.AdvanceHouseTransition(2);h.ReleaseHouseEntryInput();
            d.SetStationActive(true);d.ReleaseInputGate(true);Set(d,"benchWaiting",false);
            Check(log,d.originals.All(o=>d.workbenchVisuals.InStorage(o.tape)) && d.tapes.All(t=>d.workbenchVisuals.InStorage(t.root)),"All authored cassette identities start inside storage");
            var lid=d.workbenchVisuals.lidHit;
            var ray=new Ray(d.view.transform.position,lid.bounds.center-d.view.transform.position);
            Check(log,d.TryBeginWorkbenchInteraction(ray),"Real lid ray enters storage interaction despite decorative lid inset");
            Animate(d);
            Check(log,d.workbenchVisuals.LidOpen && !d.workbenchVisuals.LidBusy,"Approach completes before lid animation finishes");
            Screenshot("storage-open",d.view);
            var source=d.originalHit;ray=new Ray(d.view.transform.position,source.bounds.center-d.view.transform.position);
            var picked=d.PickWorkbenchTarget(ray);
            Check(log,picked==source,"Open box camera can hit Original 01 (picked "+(picked?picked.name:"none")+")");
            Set(d,"benchWaiting",false);
            Check(log,d.TryBeginOriginalInteraction(ray),"Click existing original starts desk delivery");
            Animate(d);
            Check(log,!d.workbenchVisuals.InStorage(d.originalTape),"Original arrives at desk without collision rejection");
            Vector3 p;Quaternion q;
            Check(log,OperationArea.TryGetPose(d.originalTape,out p,out q),"Delivery commits through existing area placement save");
            var expected=d.originalTape.position;d.RestoreSource();
            Check(log,Vector3.Distance(expected,d.originalTape.position)<.001f,"Source restore preserves delivered desk position");
            var candidate=d.tapes[2].root;var from=candidate.position;
            d.Chapter.AuditionComplete=true;Set(d,"benchWaiting",false);
            Call(d,"TryDeliverStoredTape",d.tapes[2].hit);Call(d,"TickStorageDelivery",.2f);d.CancelWorkbenchGesture();
            Check(log,Vector3.Distance(candidate.position,from)<.001f,"Cancel mid-delivery returns same work tape to its slot");
            Set(d,"benchWaiting",false);Call(d,"TryDeliverStoredTape",d.tapes[2].hit);Animate(d);
            Check(log,!d.workbenchVisuals.InStorage(candidate),"Work tape reaches its own right-hand desk landing");
            Screenshot("tapes-on-desk",d.view);
            return Write("storage",log);
        }
        public static string Transport()
        {
            var d=Deck;var h=WBMain.Instance;var t=d.transport;var log=new List<string>();
            h.enabled=false;d.enabled=false;t.GetComponent<AudioSource>().Stop();t.GetComponent<AudioSource>().enabled=false;
            d.SendMessage("OnApplicationFocus",true);d.SetStationActive(true);d.ReleaseInputGate(true);
            t.RetryCurrent();
            if(!d.Chapter.SourceLoaded){Check(log,d.LoadOriginal(),"Load existing source A");Animate(d);}
            d.Chapter.AuditionComplete=true;
            var c=d.Chapter.Tape.Candidates[2];c.LeftUsed=c.RightUsed=c.StartReached=c.EndReached=c.HoleA=c.HoleB=true;c.Scanned=TapeInspectionState.Samples;for(int i=0;i<c.Seen.Length;i++)c.Seen[i]=true;
            if(d.Selected<0){Check(log,(bool)d.GetType().GetMethod("SelectForDrag",Hidden).Invoke(d,new object[]{2}),"Select prepared B fixture");Check(log,d.InsertDragged(),"Load existing work B");Animate(d);}
            d.OpenRecording();Set(d,"recordingWait",false);h.AdoptPointerGesture();int step=t.Rate/10;
            d.PressTransportKey(KeyCode.F);Audio(t,step);
            Check(log,t.SourcePlaying && !t.Writing,"F starts only A");
            d.PressTransportKey(KeyCode.S);Audio(t,step);int first=t.SnapshotMapping().Length;
            d.ReleaseTransportKey(0);Audio(t,step);
            Check(log,t.Writing && t.SnapshotMapping().Length==first+step,"S latches B recording; releasing pointer does not stop it");
            d.PressTransportKey(KeyCode.S);first=t.SnapshotMapping().Length;float a=t.SourceSeconds;Audio(t,step);
            Check(log,!t.Writing && t.SourcePlaying && t.SourceSeconds>a && t.SnapshotMapping().Length==first,"Second S pauses only B, with no gap written while A continues");
            d.PressTransportKey(KeyCode.S);Audio(t,step);Check(log,t.SnapshotMapping().Length==first+step,"S resumes at saved append position");
            d.PressTransportKey(KeyCode.Quote);Audio(t,step);
            Check(log,!t.SourcePlaying && t.Writing && t.SnapshotMapping().Last()==-1,"Quote stops only A; B independently records missing source as silence");
            d.PressTransportKey(KeyCode.S);a=t.SourceSeconds;d.PressTransportKey(KeyCode.L);Audio(t,step);
            Check(log,t.SourceSeconds>a && t.TargetSeconds<=t.RecordedSeconds,"L winds loaded tapes without crossing B's recorded end");
            a=t.SourceSeconds;d.PressTransportKey(KeyCode.L);Audio(t,step);Check(log,t.SourceSeconds==a,"Second L stops A winding");
            d.PressTransportKey(KeyCode.J);Audio(t,step);Check(log,t.SourceSeconds<a,"J winds A backwards");
            d.PressTransportKey(KeyCode.J);d.PressDualControl("A_Play");d.PressDualControl("B_Record");Audio(t,step);
            Check(log,t.SourcePlaying && t.Writing,"Physical A PLAY and B REC use same toggle semantics");
            d.PressDualControl("B_Record");Check(log,t.SourcePlaying && !t.Writing,"Second physical REC pauses B without stopping A");
            d.PressTransportKey(KeyCode.Quote);t.RewindTarget();d.PressDualControl("B_Play");Audio(t,step);
            Check(log,t.TargetStatus=="PLAYBACK" && !t.SourcePlaying,"B physical PLAY listens to real recorded buffer independently");
            var prefix=t.SnapshotMapping();d.PressDualControl("B_Stop");d.PressTransportKey(KeyCode.F);d.PressTransportKey(KeyCode.S);Audio(t,step);
            Check(log,t.SnapshotMapping().Take(prefix.Length).SequenceEqual(prefix),"Resume after listening never overwrites earlier samples");
            d.PressDualControl("B_Door");Check(log,!t.Writing && t.SourcePlaying,"Click B slot ejects B and preserves A playback");
            Check(log,!d.EjectDualDeck(1),"Repeated eject during transfer is rejected");Animate(d);
            Check(log,d.Selected<0 && !d.workbenchVisuals.InStorage(d.tapes[2].root),"B eject returns same identity to desk");
            d.PressTransportKey(KeyCode.S);Check(log,!t.Writing,"Empty B cannot record");
            Check(log,!d.transportPanel.panel.activeInHierarchy,"Transport Canvas cannot reappear");
            d.CloseRecording();Check(log,!t.SourceRunning && !t.TargetRunning,"Leaving operation stops both transports at stable saved positions");
            return Write("transport",log);
        }
        public static string RestoreAndSubmit()
        {
            var d=Deck;var t=d.transport;var log=new List<string>();
            t.GetComponent<AudioSource>().Stop();t.GetComponent<AudioSource>().enabled=false;
            d.SetStationActive(true);d.ReleaseInputGate(true);
            if(d.Selected<0){Call(d,"SelectForDrag",2);Check(log,d.InsertDragged(),"Reload prepared work tape");Animate(d);}
            d.OpenRecording();Set(d,"recordingWait",false);WBMain.Instance.AdoptPointerGesture();
            t.RetryCurrent();d.PressTransportKey(KeyCode.F);
            var audio=(TapeCommissionAudio)typeof(HouseRadioDeck).GetField("commissionAudio",Hidden).GetValue(d);
            // Test-only acceptance intervals schedule explicit presses. Runtime controls never read CleanMap.
            for(int start=0;start<audio.CleanMap.Length;)
            {
                bool clean=audio.CleanMap[start]>=0;int end=start+1;
                while(end<audio.CleanMap.Length && (audio.CleanMap[end]>=0)==clean)end++;
                if(t.Writing!=clean)d.PressTransportKey(KeyCode.S);
                Audio(t,end-start);start=end;
            }
            if(t.Writing)d.PressTransportKey(KeyCode.S);
            var expected=t.SnapshotMapping();Check(log,expected.Length>0 && expected.All(i=>i>=0),"Manual toggle sequence produces joined real source samples without pause silence");
            Check(log,audio.Reject(expected)=="","Existing acceptance rules accept the real joined recording");
            float a=t.SourceSeconds,b=t.TargetSeconds;string checkpoint=d.Chapter.CaptureCheckpoint();
            Check(log,t.Save(),"PCM artifact saved using existing format");
            d.CloseRecording();t.GetComponent<AudioSource>().enabled=true;t.Initialize(audio.Samples,t.Rate,d.CurrentCopy);t.GetComponent<AudioSource>().Stop();t.GetComponent<AudioSource>().enabled=false;
            Check(log,t.SnapshotMapping().SequenceEqual(expected) && t.SourceSeconds==a && t.TargetSeconds==b,"Disk reload restores actual samples and both deck cursors");
            Check(log,!t.SourceRunning && !t.TargetRunning,"Restored transports stay stopped, no unexpected latched recording");
            var restored=new RadioTapeChapter();restored.RestoreCheckpoint(checkpoint);
            Check(log,restored.SourceLoaded && restored.WorkingTapeIndex==2 && restored.Tape.Candidates[2].Prepared,"Existing chapter checkpoint preserves A/B identity and sealed holes");
            d.OpenRecording();Set(d,"recordingWait",false);int commission=d.Chapter.CommissionIndex;
            t.GetComponent<AudioSource>().enabled=true;
            Call(d,"StorageClick",d.workbenchVisuals.finishedHit);
            Check(log,d.Chapter.CommissionIndex==commission,"Click alone cannot submit");
            TapeStorageSubmissionChecks.EjectAndDrop(d);
            Check(log,d.SubmissionSucceeded && d.Chapter.CommissionIndex==commission+1 && d.SuccessPopupOpen,"Finished-slot drop uses original transactional save and success popup");
            return Write("restore-submit",log);
        }
        public static void Screenshot(string name,Camera camera)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("output/QA/TapeToggleStorage_20261105");File.WriteAllBytes("output/QA/TapeToggleStorage_20261105/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
