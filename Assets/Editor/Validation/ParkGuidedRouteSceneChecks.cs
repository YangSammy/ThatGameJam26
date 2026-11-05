using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkGuidedRouteSceneChecks
    {
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static Dictionary<FieldInfo,object> Snapshot(object value)
        {
            var result=new Dictionary<FieldInfo,object>();
            foreach(var field in value.GetType().GetFields(Fields))if(!field.IsInitOnly && !field.IsLiteral)result[field]=field.GetValue(value);
            return result;
        }
        static void Restore(object value,Dictionary<FieldInfo,object> snapshot){foreach(var pair in snapshot)pair.Key.SetValue(value,pair.Value);}
        public static string Run()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || p==null)throw new InvalidOperationException("Existing Park Play required");
            var c=p.companion;var ps=Snapshot(p);var cs=Snapshot(c);var type=p.GetType();
            var path=(List<Vector3>)type.GetField("guidedPath",Fields).GetValue(p);var pathBefore=path.ToArray();
            var poses=new Dictionary<Transform,Tuple<Vector3,Quaternion,Vector3>>();
            foreach(var root in new[]{p.transform,c.transform})foreach(var t in root.GetComponentsInChildren<Transform>(true))poses[t]=Tuple.Create(t.localPosition,t.localRotation,t.localScale);
            var motor=p.GetComponent<CharacterController>();var girlMotor=c.GetComponent<CharacterController>();
            Action<string,object> set=(name,value)=>type.GetField(name,Fields).SetValue(p,value);
            Func<string,object[],object> call=(name,args)=>type.GetMethod(name,Fields).Invoke(p,args);
            var points=new List<ParkPhotoWaypoint>();foreach(var n in c.route)if(n!=null && n.photoTask!=ParkPhotoWaypoint.PhotoTask.None)points.Add(n);
            var lines=new List<string>();
            try
            {
                set("roll",new ParkFilmRoll{cameraLoading=ParkCameraLoading.AtStep(7,400)});set("inputGate",false);set("focused",true);set("applicationPaused",false);set("changingScene",false);set("taskRoutingReady",true);
                type.GetField("<CurrentPauseReason>k__BackingField",Fields).SetValue(p,ParkPhotoPrototype.PauseReason.None);set("photoMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                p.InputContext.Select(InputContextId.Exploration,PlayerControlState.Exploration,true);p.InputContext.ContinueGesture();
                float dt=Mathf.Clamp(Time.deltaTime,.001f,.05f);
                for(int leg=0;leg<points.Count;leg++)
                {
                    var point=points[leg];c.photoHoldNode=point;c.Paused=false;c.TravelPaused=false;c.photoLookBack=false;c.photoOfferHand=false;
                    c.ResumeAtPhotoNode(leg==0?point:points[leg-1]);
                    if(leg==0){motor.enabled=false;p.transform.position=point.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;motor.enabled=true;}
                    set("bridgeNode",point);set("guidedDeparturePending",false);set("guidedDepartureNode",leg==0?null:points[leg-1]);call("InitializeGuidedWalk",null);
                    Vector3 velocity=Vector3.zero;float vertical=0;int frames=0,still=0;
                    var rotation=p.view.transform.rotation;
                    for(;frames<30000;frames++)
                    {
                        var old=p.transform.position;
                        typeof(ParkCompanion).GetMethod("Update",Fields).Invoke(c,null);
                        var wanted=(Vector3)call("GuidedDesiredVelocity",new object[]{Vector3.right*99,dt});
                        velocity=PlayerControlSettings.Current.ApproachVelocity(velocity,wanted,dt);
                        if(motor.isGrounded && vertical<0)vertical=-2;vertical-=18*dt;
                        motor.Move((velocity+Vector3.up*vertical)*dt);
                        Physics.SyncTransforms();
                        if(p.GuidedPhotoAdjustment)break;
                        if(Vector3.Distance(old,p.transform.position)<.00001f && c.AtPhotoNode)still++;else still=0;
                        if(still*dt>3)break;
                    }
                    lines.Add((p.GuidedPhotoAdjustment?"PASS":"FAIL")+" "+point.photoTask+" automatic path/dock frames="+frames+" player="+p.transform.position+" girl="+c.transform.position+" cameraAngleChange="+Quaternion.Angle(rotation,p.view.transform.rotation));
                    if(!p.GuidedPhotoAdjustment)break;
                    call("HoldGuidedDeparture",null);c.Paused=false;
                    var held=c.transform.position;c.PoseEntranceShutter();
                    float poseBefore=(float)typeof(ParkCompanion).GetField("entranceShutterPose",Fields).GetValue(c);
                    typeof(ParkCompanion).GetMethod("Update",Fields).Invoke(c,null);
                    float poseAfter=(float)typeof(ParkCompanion).GetField("entranceShutterPose",Fields).GetValue(c);
                    lines.Add((Vector3.Distance(held,c.transform.position)<.02f && poseAfter<poseBefore?"PASS":"FAIL")+" "+point.photoTask+" pending stop retains position and advances performance");
                }
                return string.Join("\n",lines);
            }
            finally
            {
                motor.enabled=false;girlMotor.enabled=false;
                Restore(p,ps);Restore(c,cs);path.Clear();path.AddRange(pathBefore);
                foreach(var pair in poses){pair.Key.localPosition=pair.Value.Item1;pair.Key.localRotation=pair.Value.Item2;pair.Key.localScale=pair.Value.Item3;}
                motor.enabled=true;girlMotor.enabled=true;Physics.SyncTransforms();p.InputContext.Handoff();
            }
        }
    }
}
