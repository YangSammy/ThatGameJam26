using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Isolated Editor test slot only. These are scripted integration checks, not
    // evidence that a human completed the native mouse/keyboard interaction.
    public static class ParkFreeWalkChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static ParkPhotoPrototype Player()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null)
                throw new InvalidOperationException("Isolated Park Play required");
            return p;
        }
        static object Get(object o,string field)=>o.GetType().GetField(field,Hidden).GetValue(o);
        static void Set(object o,string field,object value)=>o.GetType().GetField(field,Hidden).SetValue(o,value);
        static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Hidden).Invoke(o,args);
        static void Check(List<string> lines,bool ok,string label)
        {if(!ok)throw new InvalidOperationException("FAIL "+label);lines.Add("PASS "+label);}
        public static string EntryAndPause()
        {
            var p=Player();var lines=new List<string>();
            Check(lines,!p.Paused,"new roll enters without manual pause");
            Check(lines,p.regionalMetering!=null && p.CheckPhotographyRoute()=="","authored meter and three-task route bound");
            Check(lines,((string)Call(p,"OpeningPhotoInstruction")).Contains("load film"),"unloaded opening explains film preparation");
            Check(lines,p.companion.TravelPaused,"companion waits while film is unloaded");
            var manual=new Vector3(1.2f,0,-.8f);
            Check(lines,(Vector3)Call(p,"GuidedDesiredVelocity",manual,.02f)==manual,"unloaded player has free WASD motion");
            Check(lines,(Vector3)Call(p,"ConstrainGuidedMove",manual)==manual,"no photo ellipse clamps displacement");
            var start=p.transform.position;var motor=p.GetComponent<CharacterController>();
            motor.Move(p.transform.right*.15f);
            Check(lines,Vector3.Distance(start,p.transform.position)>.05f,"authored spawn permits real CharacterController displacement");
            motor.enabled=false;p.transform.position=start;motor.enabled=true;
            var gate=new PlayerInputReleaseGate(true);
            var requires=typeof(PlayerInputReleaseGate).GetMethod("RequiresRelease",Hidden|BindingFlags.Public);
            Check(lines,!(bool)requires.Invoke(gate,new object[]{KeyCode.W}) && (bool)requires.Invoke(gate,new object[]{KeyCode.Mouse0}),"movement keys cannot deadlock Park handoff; mouse must release");
            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);
            Call(p,"OnApplicationFocus",false);Check(lines,!p.Paused,"focus loss is not manual pause");
            Call(p,"OnApplicationFocus",true);Check(lines,!p.Paused && p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Body,"focus return retains instrument without pause menu");
            Call(p,"PauseControls");Call(p,"OnApplicationFocus",false);Call(p,"OnApplicationFocus",true);
            Check(lines,p.Paused && p.CurrentPauseReason==ParkPhotoPrototype.PauseReason.Manual,"manual pause survives focus round trip");
            p.Resume();Call(p,"OnApplicationPause",true);Call(p,"OnApplicationPause",false);
            Check(lines,!p.Paused,"OS suspension returns without creating manual pause");
            var hands=p.view.GetComponent<FirstPersonHands>();hands.Advance(1);
            Check(lines,hands.skeletalRig!=null && hands.skeletalRig.Valid,"original hand skeleton remains bound");
            return string.Join("\n",lines);
        }
        public static string MeterZones()
        {
            var p=Player();var meter=p.regionalMetering;var lines=new List<string>();
            foreach(var zone in meter.zones)
            {
                float ev=meter.Sample(zone.transform.position);
                Check(lines,Mathf.Abs(ev-zone.ev100)<.02f,zone.name+" centre EV="+ev);
                Check(lines,zone.GetComponentsInChildren<Collider>().Length==0,zone.name+" never blocks input/physics");
                foreach(int iso in new[]{100,200,400,800})
                foreach(float aperture in new[]{4f,8f})
                {
                    float shutter=ParkCameraResponse.SuggestedShutter(aperture,iso,ev);
                    // Mechanical speed bounds can clip wide-open ISO800 in sunshine;
                    // at f/8 all authored zones must stay within acceptance tolerance.
                    float error=Mathf.Abs(ParkCameraResponse.ExposureError(aperture,shutter,iso,ev));
                    if(aperture==8)Check(lines,error<=p.bridgeExposureTolerance,"ISO"+iso+" f/8 advice accepted in "+zone.name);
                }
                var edge=zone.transform.TransformPoint(new Vector3(zone.size.x*.5f,0,0));
                Check(lines,Mathf.Abs(meter.Sample(edge+zone.transform.right*.01f)-meter.Sample(edge-zone.transform.right*.01f))<.05f,"continuous boundary "+zone.name);
            }
            Check(lines,Mathf.Abs(meter.Sample(new Vector3(-10.51f,3,-6))-meter.Sample(new Vector3(-10.49f,3,-6)))<.05f,"higher priority overlap blends over existing shade");
            var sun=meter.sunlight;float original=sun.intensity;
            try{float before=meter.Sample(Vector3.zero);sun.intensity=original*.5f;Check(lines,Mathf.Abs(meter.Sample(Vector3.zero)-before+1)<.02f,"half actual sun intensity lowers EV one stop");}
            finally{sun.intensity=original;}
            return string.Join("\n",lines);
        }
        public static string PrepareCurrentPhoto()
        {
            var p=Player();p.Resume();Call(p,"OnApplicationFocus",true);
            // Exercise saved reentry through the normal controller restore functions.
            Set(p,"roll",ParkFilmStore.Load(p.CurrentFilmId));Call(p,"InitializeBridge");Call(p,"ResumeUnfinishedMemory");
            var node=(ParkPhotoWaypoint)Get(p,"bridgeNode");
            p.companion.ResumeAtPhotoNode(node);p.companion.Paused=false;
            var motor=p.GetComponent<CharacterController>();motor.enabled=false;
            p.transform.position=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;motor.enabled=true;
            p.view.transform.SetPositionAndRotation(node.referenceView.position,node.referenceView.rotation);
            Set(p,"yaw",node.referenceView.eulerAngles.y);Set(p,"pitch",Mathf.DeltaAngle(0,node.referenceView.eulerAngles.x));
            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);p.view.fieldOfView=48;
            Call(p,"CaptureBridgeReference");Call(p,"UpdateRegionalMetering");
            var roll=(ParkFilmRoll)Get(p,"roll");var task=roll.Task(roll.NextTask);
            Set(p,"apertureIndex",3);
            var speeds=(float[])typeof(ParkPhotoPrototype).GetField("speeds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            Set(p,"speedIndex",Array.IndexOf(speeds,ParkCameraResponse.SuggestedShutter(8,p.FilmIso,p.sceneEv100)));
            p.focusDistance=Vector3.Dot(task.subjectPoint-p.view.transform.position,p.view.transform.forward);
            Call(p,"TickBridge",1f);Physics.SyncTransforms();
            return "Prepared "+roll.NextTask+" | meter="+p.MeterEv100+" subject="+p.sceneEv100+" frames="+p.ExposedFrames;
        }
        public static string CaptureCurrent()
        {
            var p=Player();Call(p,"TickBridge",1f);
            var roll=(ParkFilmRoll)Get(p,"roll");
            if(roll.NextTask=="meadow" && !p.HoldingPhotoHand)
            {
                int frames=p.ExposedFrames;var rotation=p.view.transform.rotation;
                p.view.transform.LookAt(p.PhotoHandPosition);Call(p,"Capture");p.view.transform.rotation=rotation;
                if(!p.HoldingPhotoHand || p.ExposedFrames!=frames)throw new InvalidOperationException("Hand join failed or consumed film");
                return "Joined hands, no frame consumed. Wait for transition then CaptureCurrent again.";
            }
            if(!(bool)Get(p,"bridgeReady"))throw new InvalidOperationException((string)Call(p,"PhotographyNextAction"));
            Call(p,"Capture");return "Exposure queued for "+roll.NextTask;
        }
        public static string CheckCommitted(int expected)
        {
            var p=Player();Call(p,"TickExposureEncoding");var roll=ParkFilmStore.Load(p.CurrentFilmId);
            if(p.ExposedFrames!=expected || roll.frames.Count!=expected)throw new InvalidOperationException("Expected "+expected+" committed frames; got "+roll.frames.Count);
            var frame=roll.frames[expected-1];
            if(!frame.accepted || !frame.sharp || Mathf.Abs(frame.exposureError)>p.bridgeExposureTolerance)throw new InvalidOperationException("Invalid recorded photo");
            byte[] bytes;string reason;
            if(ParkFilmStore.TryReadDevelopedFrame(roll.id,expected,out bytes,out reason))throw new InvalidOperationException("Undeveloped image leaked");
            if(expected<3 && !p.GuidedDeparturePending)throw new InvalidOperationException("Must wait for camera lowering");
            if(expected==3 && p.CurrentPauseReason!=ParkPhotoPrototype.PauseReason.FilmComplete)throw new InvalidOperationException("Completed roll return not distinguished");
            return "PASS "+frame.taskId+" saved and hidden until development; EV="+frame.sceneEv100+" error="+frame.exposureError+" next="+roll.NextTask;
        }
        public static string ShowHands(string mode)
        {
            var p=Player();p.Resume();Call(p,"OnApplicationFocus",true);
            Call(p,"SetPhotographyMode",(ParkPhotoPrototype.PhotographyMode)Enum.Parse(typeof(ParkPhotoPrototype.PhotographyMode),mode));
            p.view.GetComponent<FirstPersonHands>().Advance(1);
            return "Showing existing rig in "+mode;
        }
    }
}
