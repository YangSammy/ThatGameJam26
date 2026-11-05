using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Deterministic contract probes, not native input certification.
    public static class V3TapeContractChecks
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(List<string> lines,bool ok,string message)=>lines.Add((ok?"PASS ":"FAIL ")+message);

        public static string Acceptance()
        {
            var lines=new List<string>();
            for(int n=1;n<=3;n++)
            {
                var definition=AssetDatabase.LoadAssetAtPath<TapeCommissionDefinition>("Assets/Data/TapeCommissions/Commission_0"+n+".asset");
                var audio=TapeCommissionCompiler.Build(definition,1000);
                var all=Enumerable.Range(0,audio.Samples.Length).ToArray();
                Check(lines,audio.Reject(all)!="","Commission "+n+": recording unchanged source must fail");
                var clean=all.Where(i=>audio.CleanMap[i]>=0).ToArray();
                Check(lines,audio.Reject(clean)=="","Commission "+n+": deliberate clean cuts pass");
                int segment=0;
                for(int start=0;start<audio.CleanMap.Length;start++)
                {
                    if(audio.CleanMap[start]>=0)continue;
                    int end=start+1;while(end<audio.CleanMap.Length && audio.CleanMap[end]<0)end++;
                    int first=start,last=end;
                    var missed=all.Where(i=>audio.CleanMap[i]>=0 || i>=first && i<last).ToArray();
                    Check(lines,audio.Reject(missed)!="","Commission "+n+": retaining whole noise segment "+(++segment)+" must fail");
                    start=end-1;
                }
                // Forgiving cuts still include small noise tails and a short blank.
                var forgiving=clean.Concat(all.Where(i=>audio.CleanMap[i]<0).Take(200)).OrderBy(i=>i).Concat(Enumerable.Repeat(-1,200)).ToArray();
                Check(lines,audio.Reject(forgiving)=="","Commission "+n+": 0.2 s boundary noise and 0.2 s blank remain allowed");
                Check(lines,audio.Reject(clean.Take(clean.Length/2).ToArray())!="","Commission "+n+": missing clean content fails");
            }
            return string.Join("\n",lines);
        }

        static void Pump(TapeSampleTransport transport,int samples)
        {typeof(TapeSampleTransport).GetMethod("OnAudioFilterRead",Hidden).Invoke(transport,new object[]{new float[samples],1});}

        public static string Transport()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay)
                throw new InvalidOperationException("Fresh isolated HouseScene Editor Play required. This fixture changes test-session recordings.");
            var h=WBMain.Instance;var d=h.radioDeck;var t=d.transport;
            h.enabled=false;d.enabled=false;t.GetComponent<AudioSource>().Stop();
            var lines=new List<string>();int step=t.Rate/10;
            try
            {
                t.RetryCurrent();t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,step);
                int first=t.SnapshotMapping().Length;t.TargetCommand(false,false,0,false);Pump(t,step);
                Check(lines,t.SourcePlaying && t.SnapshotMapping().Length==first,"Stopping B leaves A playing and adds no target samples");
                t.TargetCommand(true,true,0,false);Pump(t,step);
                Check(lines,t.SnapshotMapping().Length==first+step,"Resuming B joins recorded content without a silence gap");
                t.TargetCommand(true,false,0,false);Pump(t,step);t.TargetCommand(true,false,0,false);Pump(t,step);
                var map=t.SnapshotMapping();
                Check(lines,map.Length==first+step*3 && map.Skip(first+step).All(i=>i==-1),"Releasing S while F stays held advances B and leaves real blank samples");
                t.TargetCommand(false,false,0,false);t.SourceCommand(0);t.RewindTarget();t.TargetCommand(true,false,0,false);Pump(t,map.Length+step);
                Check(lines,t.SnapshotMapping().SequenceEqual(map) && t.TargetStatus=="PAUSED","Normal replay ends at recorded length without creating blanks");
                t.SourceCommand(2);t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,step);
                Check(lines,t.SnapshotMapping().Take(map.Length).SequenceEqual(map),"Recording after rewind appends without overwriting the existing result");
                t.RetryCurrent();t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,step);t.SourceCommand(0);Pump(t,step);
                Check(lines,t.Writing && t.SnapshotMapping().Skip(step).All(i=>i==-1),"Stopping A does not stop B; its missing input becomes blank audio");
                t.RetryCurrent();t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,Mathf.RoundToInt(t.SourceDuration*t.Rate)+step);
                Check(lines,!t.SourcePlaying && !t.Writing,"Source end stops recording at the exact final source sample");
                t.RetryCurrent();t.TargetCommand(true,true,0,false);Pump(t,Mathf.RoundToInt(t.TargetCapacitySeconds*t.Rate)+step);
                Check(lines,!t.TargetRunning && !t.Writing && !t.SourcePlaying,"Full B stops its own motor even when A is paused");
                return string.Join("\n",lines);
            }
            finally {t.StopAll();t.RetryCurrent();}
        }

        public static string Controller()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay)
                throw new InvalidOperationException("Fresh isolated HouseScene Editor Play required.");
            var h=WBMain.Instance;var d=h.radioDeck;var t=d.transport;
            h.enabled=false;d.enabled=false;t.GetComponent<AudioSource>().Stop();
            d.SetStationActive(true);
            typeof(HouseRadioDeck).GetProperty("Selected").GetSetMethod(true).Invoke(d,new object[]{2});
            typeof(HouseRadioDeck).GetProperty("State").GetSetMethod(true).Invoke(d,new object[]{HouseRadioDeck.Phase.Paused});
            d.Chapter.SourceLoaded=true;d.Chapter.AuditionComplete=true;d.Chapter.WorkingTapeIndex=2;
            var candidate=d.Chapter.Tape.Candidates[2];candidate.LeftUsed=candidate.RightUsed=candidate.StartReached=candidate.EndReached=candidate.HoleA=candidate.HoleB=true;
            candidate.Scanned=TapeInspectionState.Samples;
            if(d.dualDeck!=null){d.dualDeck.Snap(0,true);d.dualDeck.Snap(1,true);}
            d.OpenRecording();typeof(HouseRadioDeck).GetField("recordingWait",Hidden).SetValue(d,false);h.AdoptPointerGesture();
            var lines=new List<string>();
            try
            {
                t.RetryCurrent();t.SourceCommand(1);Pump(t,t.Rate/10);t.SourceCommand(0);
                float position=t.SourceSeconds;
                d.ApplyRecordingKeys(false,false,false,false,false);
                d.ApplyRecordingKeys(true,true,false,false,false);
                Check(lines,!t.SourcePlaying && Mathf.Abs(t.SourceSeconds-position)<.00001f,"F+S cannot start or rewind a stopped A");
                d.ApplyRecordingKeys(false,false,false,false,false);t.SourceCommand(0);
                Check(lines,d.RecordingInputReady,"Fixture reaches the real recording input context");
                d.transportPanel.Command(0);
                Check(lines,d.UITransportCommand==-1,"Button click/release cannot latch a recording command");
                d.SetUITransportCommand(0);
                Check(lines,!t.SourcePlaying && Mathf.Abs(t.SourceSeconds-position)<.00001f,"Physical/UI Record cannot start or rewind A");
                var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
                var key=d.transportPanel.buttons[0].GetComponent<TapeTransportKey>();
                Check(lines,key!=null && key.panel==d.transportPanel && key.command==0,"Authored UI Record has its held-input binding");
                if(key!=null){key.OnPointerUp(pointer);Check(lines,d.UITransportCommand==-1,"Pointer release clears the held key");d.SetUITransportCommand(0);key.OnPointerExit(pointer);Check(lines,d.UITransportCommand==-1,"Leaving a held UI key cancels it");}
                d.ApplyRecordingKeys(false,true,false,false,false);Pump(t,t.Rate/10);
                var rec=d.dualDeck.transform.Find("Deck_B_ROOT/Deck_B_Key_Record");
                var lamp=d.dualDeck.transform.Find("Deck_B_ROOT/Deck_B_RecordLamp");
                var up=rec.localPosition;var unlit=lamp.localScale;
                // Sample the actual model clips across two ticks (start, then settle).
                for(int i=0;i<2;i++)d.dualDeck.Tick(.5f,t.SourcePlaying,t.TargetStatus,t.Writing,d.TargetKeyDown(0));
                Check(lines,d.TargetKeyDown(0) && (rec.localPosition-up).sqrMagnitude>1e-12f &&
                    (lamp.localScale-unlit).sqrMagnitude<1e-12f && !t.Writing && t.TargetSeconds==0,
                    "S alone depresses REC while the motor stays stopped and the lamp stays off");
                d.ApplyRecordingKeys(false,false,false,false,false);
                for(int i=0;i<2;i++)d.dualDeck.Tick(.5f,t.SourcePlaying,t.TargetStatus,t.Writing,d.TargetKeyDown(0));
                Check(lines,!d.TargetKeyDown(0) && (rec.localPosition-up).sqrMagnitude<1e-12f,"Releasing S returns the model key");
                t.SourceCommand(1);d.ApplyRecordingKeys(true,true,false,false,false);Pump(t,t.Rate/10);
                d.dualDeck.Tick(.5f,t.SourcePlaying,t.TargetStatus,t.Writing,d.TargetKeyDown(0),"Play");
                Check(lines,t.Writing && d.TargetKeyDown(0) && d.TargetKeyDown(1) &&
                    (lamp.localScale-unlit).sqrMagnitude>1e-12f,"F+S lights the lamp when B actually writes");
                d.ApplyRecordingKeys(true,true,false,false,true);int count=t.SnapshotMapping().Length;Pump(t,t.Rate/10);
                Check(lines,t.SourcePlaying && !t.Writing && t.SnapshotMapping().Length==count,"Stop has highest B priority without stopping A");
                d.ApplyRecordingKeys(true,true,false,false,false);
                Check(lines,!t.Writing,"Stop cannot rearm while F remains held");
                d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,true,false,false,false);
                Check(lines,t.Writing,"Release and press F+S deliberately resumes B");
                d.PressDualControl("B_Stop");d.ApplyRecordingKeys(true,true,false,false,false);
                Check(lines,!t.Writing && t.SourcePlaying,"Physical Stop cannot resume under an unreleased F+S chord");
                d.ApplyRecordingKeys(true,true,false,false,true);d.SetUITransportCommand(0);d.ApplyRecordingKeys(true,true,false,false,false);
                Check(lines,!t.Writing,"Pointer Record cannot synthesize release to bypass Stop");
                d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,true,false,false,false);
                d.OpenPreparation();
                Check(lines,t.SourcePlaying && !t.Writing && !d.RecordingMode,"Entering preparation releases B's gesture while preserving A");
                d.ApplyRecordingKeys(false,false,false,false,false);d.ApplyRecordingKeys(true,true,false,false,false);
                typeof(HouseRadioDeck).GetMethod("TickHeldTargetDuringHandoff",Hidden).Invoke(d,null);
                Check(lines,!t.Writing && t.SourcePlaying,"A source-drag handoff still observes released keyboard keys outside recording mode");
                return string.Join("\n",lines);
            }
            finally {t.StopAll();d.CloseRecording();}
        }
    }
}
