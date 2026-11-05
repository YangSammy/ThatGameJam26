using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkGuidedWalkChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var type=typeof(ParkPhotoPrototype);
            var clamp=type.GetMethod("ClampGuidedPhotoPosition",BindingFlags.Static|BindingFlags.NonPublic);
            if(clamp==null)return "FAIL guided photo area and follow controller are missing";
            var root=new GameObject("QA guided walk");root.SetActive(false);
            try
            {
                var p=root.AddComponent<ParkPhotoPrototype>();p.enabled=false;
                var girl=new GameObject("QA girl");girl.transform.SetParent(root.transform);p.companion=girl.AddComponent<ParkCompanion>();
                var cam=new GameObject("QA camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);p.view=cam;
                var point=new GameObject("QA photo").AddComponent<ParkPhotoWaypoint>();point.transform.SetParent(root.transform);
                var reference=new GameObject("QA reference").transform;reference.SetParent(root.transform);reference.position=Vector3.up*1.65f;point.referenceView=reference;
                p.companion.route=new[]{point};
                type.GetField("bridgeNode",Hidden).SetValue(p,point);
                Func<string,object[],object> call=(name,args)=>type.GetMethod(name,Hidden).Invoke(p,args);
                var lines=new System.Collections.Generic.List<string>();
                foreach(bool bridge in new[]{false,true})
                {
                    var value=(Vector3)clamp.Invoke(null,new object[]{new Vector3(8,2,8),Vector3.zero,Quaternion.identity,bridge});
                    float x=bridge?.55f:1.1f,z=bridge?1:1.1f;
                    bool contained=value.x*value.x/(x*x)+value.z*value.z/(z*z)<=1.001f && value.y==2;
                    lines.Add((contained?"PASS":"FAIL")+" local photo ellipse preserves height and limits movement bridge="+bridge);
                }
                type.GetField("roll",Hidden).SetValue(p,new ParkFilmRoll{cameraLoading=ParkCameraLoading.AtStep(7,400)});
                call("InitializeGuidedWalk",null);
                var direction=(Vector3)call("GuidedDesiredVelocity",new object[]{Vector3.right*99,1f/60});
                lines.Add((direction.sqrMagnitude<.001f?"PASS":"FAIL")+" automatic arrival ignores unrelated manual input");
                var corner=new GameObject("QA corner").AddComponent<ParkPhotoWaypoint>();corner.transform.SetParent(root.transform);corner.transform.position=Vector3.right*5;
                point.photoTask=ParkPhotoWaypoint.PhotoTask.Bridge;point.transform.position=new Vector3(5,0,5);reference.position=new Vector3(4,1.65f,4);
                p.companion.route=new[]{corner,point};p.companion.transform.position=new Vector3(5,0,5);
                type.GetField("inputGate",Hidden).SetValue(p,false);type.GetField("taskRoutingReady",Hidden).SetValue(p,true);
                p.InputContext.Select(InputContextId.Exploration,PlayerControlState.Exploration,true);p.InputContext.ContinueGesture();
                call("InitializeGuidedWalk",null);
                direction=(Vector3)call("GuidedDesiredVelocity",new object[]{Vector3.left*99,1f/60});
                lines.Add((direction.x>0 && Mathf.Abs(direction.z)<.001f?"PASS":"FAIL")+" follows route corner instead of chasing girl or WASD");
                type.GetField("photoMode",Hidden).SetValue(p,ParkPhotoPrototype.PhotographyMode.Meter);
                direction=(Vector3)call("GuidedDesiredVelocity",new object[]{Vector3.right*99,1f/60});
                lines.Add((direction.sqrMagnitude<.001f?"PASS":"FAIL")+" metering stops automatic travel");
                type.GetField("photoMode",Hidden).SetValue(p,ParkPhotoPrototype.PhotographyMode.Exploration);
                call("HoldGuidedDeparture",null);
                lines.Add(((bool)type.GetProperty("GuidedDeparturePending").GetValue(p,null)?"PASS":"FAIL")+" successful photo waits for camera lowering");
                lines.Add((!p.companion.Paused && p.companion.TravelPaused?"PASS":"FAIL")+" waiting holds travel without freezing presentation");
                direction=(Vector3)call("GuidedDesiredVelocity",new object[]{Vector3.one*99,1f/60});
                lines.Add((direction.sqrMagnitude<.001f?"PASS":"FAIL")+" departure gate blocks all movement");
                type.GetField("roll",Hidden).SetValue(p,new ParkFilmRoll{version=2,cameraLoading=ParkCameraLoading.AtStep(7,400)});
                call("ReleaseGuidedDeparture",null);
                lines.Add((!(bool)type.GetProperty("GuidedDeparturePending").GetValue(p,null)?"PASS":"FAIL")+" lower camera releases next leg without writing film");
                call("HoldGuidedDeparture",null);
                type.GetField("photoMode",Hidden).SetValue(p,ParkPhotoPrototype.PhotographyMode.Viewfinder);
                call("TickLoweredDeparture",null);
                lines.Add((p.GuidedDeparturePending?"PASS":"FAIL")+" async completion still waits while camera raised");
                type.GetField("photoMode",Hidden).SetValue(p,ParkPhotoPrototype.PhotographyMode.Exploration);
                call("TickLoweredDeparture",null);
                lines.Add((!p.GuidedDeparturePending?"PASS":"FAIL")+" camera lowered during encoding departs after commit without second toggle");
                var restoredPath=(System.Collections.Generic.List<Vector3>)type.GetField("guidedPath",Hidden).GetValue(p);
                int countBefore=restoredPath.Count;p.OnBeforeSerialize();restoredPath.Clear();p.OnAfterDeserialize();
                lines.Add((countBefore>0 && restoredPath.Count==countBefore?"PASS":"FAIL")+" script reload preserves remaining guided path");
                var resumed=type.GetField("guidedResumedAtPhoto",Hidden);
                if(resumed!=null)resumed.SetValue(p,true);
                call("InitializeGuidedWalk",null);
                var path=(System.Collections.Generic.List<Vector3>)type.GetField("guidedPath",Hidden).GetValue(p);
                lines.Add((path.Count==1?"PASS":"FAIL")+" deferred companion Start cannot replay earlier route on resume");
                type.GetField("needsWalkRecovery",Hidden).SetValue(p,true);call("SyncParkInput",null);
                lines.Add((Cursor.visible && Cursor.lockState==CursorLockMode.None?"PASS":"FAIL")+" blocked-route recovery has an accessible free pointer");
                type.GetField("needsWalkRecovery",Hidden).SetValue(p,false);
                // No disk access: a missing route task blocks release initialization safely.
                type.GetField("roll",Hidden).SetValue(p,null);
                return string.Join("\n",lines);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
