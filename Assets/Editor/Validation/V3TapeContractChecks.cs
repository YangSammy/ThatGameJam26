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
                Check(lines,map.Length==first+step*3 && map.Skip(first+step).All(i=>i==-1),"Low-level motor without write preserves explicit blank transport semantics (not mapped to live keys)");
                t.TargetCommand(false,false,0,false);t.SourceCommand(0);t.RewindTarget();t.TargetCommand(true,false,0,false);Pump(t,map.Length+step);
                Check(lines,t.SnapshotMapping().SequenceEqual(map) && t.TargetStatus=="PAUSED","Normal replay ends at recorded length without creating blanks");
                t.SourceCommand(2);t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,step);
                Check(lines,t.SnapshotMapping().Take(map.Length).SequenceEqual(map),"Recording after rewind appends without overwriting the existing result");
                t.RetryCurrent();t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,step);t.SourceCommand(0);Pump(t,step);
                Check(lines,t.Writing && t.SnapshotMapping().Skip(step).All(i=>i==-1),"Stopping A does not stop B; its missing input becomes blank audio");
                t.RetryCurrent();t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,Mathf.RoundToInt(t.SourceDuration*t.Rate)+step);
                Check(lines,!t.SourcePlaying && t.Writing,"A source end does not silently release B recording");
                t.RetryCurrent();t.TargetCommand(true,true,0,false);Pump(t,Mathf.RoundToInt(t.TargetCapacitySeconds*t.Rate)+step);
                Check(lines,!t.TargetRunning && !t.Writing && !t.SourcePlaying,"Full B stops its own motor even when A is paused");
                return string.Join("\n",lines);
            }
            finally {t.StopAll();t.RetryCurrent();}
        }

        public static string Controller()=>TapeToggleStorageChecks.Transport();
    }
}
