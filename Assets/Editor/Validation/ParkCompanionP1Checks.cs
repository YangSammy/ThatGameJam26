using System;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkCompanionP1Checks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var root=new GameObject("QA companion P1");root.SetActive(false);
            try
            {
                Func<string,Transform> child=name=>{var t=new GameObject(name).transform;t.SetParent(root.transform);return t;};
                var girl=child("Girl").gameObject.AddComponent<ParkCompanion>();
                girl.player=child("Player");girl.player.position=Vector3.back*3;
                var node=child("Current photo").gameObject.AddComponent<ParkPhotoWaypoint>();node.photoTask=ParkPhotoWaypoint.PhotoTask.Bridge;
                node.referenceView=child("Authored view");node.referenceView.position=Vector3.back*3;
                node.lookAt=child("Initial look");node.lookAt.position=Vector3.forward*3;
                var start=child("Entrance").gameObject.AddComponent<ParkPhotoWaypoint>();start.photoTask=ParkPhotoWaypoint.PhotoTask.Entrance;start.transform.position=Vector3.left*10;
                girl.route=new[]{start,node};girl.photoHoldNode=node;
                var type=typeof(ParkCompanion);
                type.GetField("index",Hidden).SetValue(girl,1);type.GetField("atNode",Hidden).SetValue(girl,true);
                Action<float> tick=dt=>type.GetMethod("TickTravel",Hidden).Invoke(girl,new object[]{dt});
                Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);};
                girl.photoLookBack=true;
                for(int i=0;i<40;i++)tick(.1f);
                var settledProperty=type.GetProperty("PhotoResponseSettled");
                check(settledProperty!=null && (bool)settledProperty.GetValue(girl),"Turn completion does not unlock the shutter response gate");
                Quaternion settled=girl.transform.rotation;
                girl.player.position=Vector3.right*3;
                for(int i=0;i<40;i++)tick(.1f);
                check(Quaternion.Angle(settled,girl.transform.rotation)<.1f,"Response body keeps chasing the player");
                girl.photoOfferHand=true;girl.player.position=Vector3.forward*20;
                for(int i=0;i<40;i++)tick(.1f);
                check(Quaternion.Angle(settled,girl.transform.rotation)<.1f,"Distant player overrides stable hand-off pose");
                check((bool)settledProperty.GetValue(girl),"Player adjustment invalidates completed response turn");
                var recovery=type.GetMethod("RecoverFromFall",Hidden);
                check(recovery!=null,"Missing current-photo fall recovery");
                type.GetField("started",Hidden).SetValue(girl,true);
                // Disable physics: this check verifies recovery state, not scene support.
                girl.GetComponent<CharacterController>().enabled=false;
                var expected=node.transform.position;
                girl.transform.position=Vector3.down*5;
                recovery.Invoke(girl,null);
                check(Vector3.Distance(girl.transform.position,expected)<.001f && girl.AtPhotoNode,"Fall restarted route instead of current photo");
                check(girl.photoLookBack && girl.photoOfferHand,"Fall erased response/hand state");
                check((float)type.GetField("verticalSpeed",Hidden).GetValue(girl)==0,"Fall retained downward velocity");
                for(int i=0;i<40;i++)tick(.1f);
                check((bool)settledProperty.GetValue(girl) && Quaternion.Angle(settled,girl.transform.rotation)<.1f,"Recovered response heading differs from authored composition");
                girl.transform.position=Vector3.down*5;tick(.1f);
                check(Vector3.Distance(girl.transform.position,expected)<.001f && girl.AtPhotoNode,"Actual fall branch bypasses current-photo recovery");
                var host=child("Host").gameObject.AddComponent<ParkPhotoPrototype>();host.companion=girl;
                var hostType=typeof(ParkPhotoPrototype);var sync=hostType.GetMethod("SyncCompanionControl",Hidden);
                check(sync!=null,"Missing independent travel/animation policy");
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Body,ParkPhotoPrototype.PhotographyMode.Meter,ParkPhotoPrototype.PhotographyMode.Viewfinder})
                {
                    hostType.GetField("photoMode",Hidden).SetValue(host,mode);sync.Invoke(host,null);
                    check(!girl.Paused && girl.TravelPaused,"Instrument freezes animation: "+mode);
                    float phase=(float)type.GetField("phase",Hidden).GetValue(girl);tick(.1f);
                    check((float)type.GetField("phase",Hidden).GetValue(girl)>phase,"Instrument freezes companion idle clock: "+mode);
                }
                hostType.GetField("focused",Hidden).SetValue(host,false);sync.Invoke(host,null);
                check(girl.Paused && girl.TravelPaused,"Focus loss no longer suspends companion");
                float frozenPhase=(float)type.GetField("phase",Hidden).GetValue(girl);var frozenPose=girl.transform.rotation;tick(.1f);
                check((float)type.GetField("phase",Hidden).GetValue(girl)==frozenPhase && Quaternion.Angle(frozenPose,girl.transform.rotation)<.001f,"Suspension changes animation/turn pose");
                hostType.GetField("focused",Hidden).SetValue(host,true);sync.Invoke(host,null);
                check(!girl.Paused && girl.TravelPaused,"Focus restore does not resume instrument idle");
                return "PASS: stable response/offer heading; distant-player override; current-photo fall recovery and retained state; camera/meter idle; focus suspension/restoration";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
