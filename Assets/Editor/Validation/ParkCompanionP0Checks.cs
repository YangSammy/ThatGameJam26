using System;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Isolated logic checks: no scene loading, player input or save writes.
    public static class ParkCompanionP0Checks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var root=new GameObject("QA companion P0");root.SetActive(false);
            try
            {
                var girl=root.AddComponent<ParkCompanion>();
                var player=new GameObject("Player");player.transform.SetParent(root.transform);player.transform.position=Vector3.back*3;girl.player=player.transform;
                var node=new GameObject("Photo stop").AddComponent<ParkPhotoWaypoint>();node.transform.SetParent(root.transform);node.pauseSeconds=12;
                var next=new GameObject("Next").AddComponent<ParkPhotoWaypoint>();next.transform.SetParent(root.transform);next.transform.position=Vector3.forward*5;
                girl.route=new[]{node,next};
                var type=typeof(ParkCompanion);
                var step=type.GetMethod("TickTravel",Hidden);
                var request=type.GetProperty("TravelRequestedLastStep");
                var release=type.GetMethod("ReleasePhotoStop");
                if(step==null || request==null || release==null)throw new Exception("Missing commanded-travel sampling / completed-photo departure contract");
                Action<float> tick=dt=>step.Invoke(girl,new object[]{dt});
                Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
                var detector=new ParkWalkStallDetector();
                girl.photoHoldNode=node;
                for(int i=0;i<150;i++)
                {
                    tick(.1f);
                    check(!detector.Tick(false,(bool)request.GetValue(girl),0,0,.1f),"Photo waiting falsely reports blockage");
                }
                check(girl.AtPhotoNode,"Photo hold advanced before completion");
                check(!(bool)release.Invoke(girl,new object[]{next}),"Wrong node released current photo hold");
                check((bool)release.Invoke(girl,new object[]{node}),"Completed photo did not release");
                check(!(bool)release.Invoke(girl,new object[]{node}),"Repeated release advanced twice");
                girl.photoHoldNode=next;
                tick(.1f);tick(.1f);
                check((bool)request.GetValue(girl),"Completed photo still waits its authored 12 seconds");
                detector.Reset();bool blocked=false;
                for(int i=0;i<22;i++){tick(.1f);blocked|=detector.Tick(false,(bool)request.GetValue(girl),0,0,.1f);}
                check(blocked,"Actual commanded movement with no progress must report blockage");
                girl.TravelPaused=true;tick(.1f);check(!(bool)request.GetValue(girl),"Instrument stop retains movement request");
                girl.TravelPaused=false;girl.Paused=true;tick(.1f);check(!(bool)request.GetValue(girl),"Pause retains movement request");
                girl.Paused=false;player.transform.position=Vector3.back*20;tick(.1f);check(!(bool)request.GetValue(girl),"Waiting for distant player counts as moving");
                player.transform.position=Vector3.forward*.4f;tick(.1f);check(!(bool)request.GetValue(girl),"Yielding to nearby player counts as moving");
                player.transform.position=Vector3.back*3;
                type.GetField("index",Hidden).SetValue(girl,0);type.GetField("atNode",Hidden).SetValue(girl,true);type.GetField("stopRemaining",Hidden).SetValue(girl,4f);
                detector.Reset();
                for(int i=0;i<39;i++){tick(.1f);check(!(bool)request.GetValue(girl),"Ordinary scenic stop was skipped");check(!detector.Tick(false,(bool)request.GetValue(girl),0,0,.1f),"Scenic stop falsely reports blockage");}
                tick(.1f);tick(.1f);tick(.1f);check((bool)request.GetValue(girl),"Scenic stop never resumes");
                return "PASS: photo hold; completed departure; wrong/repeated release; real blockage; instrument/pause/distance/personal-space waits; ordinary 4-second stop";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
