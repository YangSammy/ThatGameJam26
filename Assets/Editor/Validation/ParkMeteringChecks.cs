using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkMeteringChecks
    {
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Hidden).Invoke(target,args);
        static object Field(object target,string name)=>target.GetType().GetField(name,Hidden).GetValue(target);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Hidden).SetValue(target,value);

        // This fixture is intentionally isolated from normal player saves. It drives real
        // readiness, hand response, Camera.Render and SaveExposure, not just exposure math.
        public static string CaptureEquivalentRoll(int iso,int apertureIndex)
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null)
                throw new InvalidOperationException("Isolated Editor Park Play required");
            float[] apertures=(float[])typeof(ParkPhotoPrototype).GetField("apertures",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            float[] speeds=(float[])typeof(ParkPhotoPrototype).GetField("speeds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            var lines=new List<string>();
            string active=System.IO.Path.Combine(ParkFilmStore.Root,"active-roll.json");
            if(System.IO.File.Exists(active))System.IO.File.Move(active,active+".qa-"+Guid.NewGuid().ToString("N"));
            var roll=ParkFilmStore.LoadOrCreate();roll=ParkFilmStore.SaveCameraLoading(roll.id,ParkCameraLoading.AtStep(7,iso));
            Set(p,"roll",roll);Set(p,"loadingStep",7);Set(p,"filmError","");Call(p,"InitializeBridge");
            bool enabled=p.enabled;p.enabled=false;
            try
            {
                for(int frame=0;frame<3;frame++)
                {
                    p.Resume();var node=(ParkPhotoWaypoint)Field(p,"bridgeNode");p.companion.ResumeAtPhotoNode(node);p.companion.Paused=false;
                    var cc=p.GetComponent<CharacterController>();cc.enabled=false;
                    p.transform.position=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;cc.enabled=true;
                    p.view.transform.SetPositionAndRotation(node.referenceView.position,node.referenceView.rotation);
                    Set(p,"yaw",node.referenceView.eulerAngles.y);Set(p,"pitch",Mathf.DeltaAngle(0,node.referenceView.eulerAngles.x));
                    Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);p.view.fieldOfView=PlayerControlSettings.Current.parkAimingFov;
                    Call(p,"CaptureBridgeReference");roll=(ParkFilmRoll)Field(p,"roll");var task=roll.Task(roll.NextTask);
                    float seconds=ParkCameraResponse.SuggestedShutter(apertures[apertureIndex],iso,task.ev100);
                    int si=Array.IndexOf(speeds,seconds);Set(p,"apertureIndex",apertureIndex);Set(p,"speedIndex",si);
                    p.focusDistance=Vector3.Dot(task.subjectPoint-p.view.transform.position,p.view.transform.forward);
                    // The editor can exceed 300 fps: wait a simulated full turn, not a
                    // fixed frame count that intermittently leaves the response unfinished.
                    Call(p,"TickBridge",1f);
                    int responseFrames=Mathf.CeilToInt(3f/Mathf.Max(.0001f,Mathf.Min(Time.deltaTime,.1f)));
                    for(int i=0;i<responseFrames;i++)Call(p.companion,"Update");
                    Physics.SyncTransforms();Call(p,"TickBridge",1f);
                    if(!(bool)Field(p,"bridgeReady"))throw new InvalidOperationException(node.photoTask+" not ready: "+Call(p,"PhotographyNextAction"));
                    if(node.photoTask==ParkPhotoWaypoint.PhotoTask.Meadow)
                    {
                        Set(p,"apertureIndex",apertures.Length-1);Set(p,"speedIndex",speeds.Length-1);
                        float correctFocus=p.focusDistance;
                        p.focusDistance=p.maximumFocus;
                        Call(p,"TickBridge",0f);
                        Set(p,"virtualPointer",Vector2.zero);Call(p,"Capture");
                        if(p.HoldingPhotoHand || p.ExposedFrames!=frame)throw new InvalidOperationException("Missed hand click was accepted");
                        var screen=p.view.WorldToViewportPoint(p.PhotoHandPosition);
                        Set(p,"virtualPointer",new Vector2(screen.x,screen.y));Call(p,"Capture");
                        roll=ParkFilmStore.Load(p.CurrentFilmId);
                        if(!p.HoldingPhotoHand || !roll.meadow.handJoined || roll.frames.Count!=frame)
                            throw new InvalidOperationException("Offered hand rejected under incorrect camera settings");
                        p.focusDistance=correctFocus;Call(p,"TickBridge",0f);Call(p,"Capture");
                        if(!p.HoldingPhotoHand || p.ExposedFrames!=frame)throw new InvalidOperationException("Incorrect exposure changed hand or film");
                        Set(p,"apertureIndex",apertureIndex);Set(p,"speedIndex",si);
                        p.focusDistance=p.maximumFocus;Call(p,"TickBridge",0f);Call(p,"Capture");
                        if(!p.HoldingPhotoHand || p.ExposedFrames!=frame)throw new InvalidOperationException("Incorrect focus changed hand or film");
                        p.focusDistance=correctFocus;
                        lines.Add("PASS Meadow: missed hand ignored; incorrect exposure/focus allowed hand only; each blocked shutter kept hand and film");
                    }
                    // Wrong exposure remains blocked even after alignment/hand acceptance.
                    Set(p,"apertureIndex",0);Set(p,"speedIndex",0);Call(p,"TickBridge",0f);Call(p,"Capture");
                    if(p.ExposedFrames!=frame)throw new InvalidOperationException("Bad exposure consumed film");
                    Set(p,"apertureIndex",apertureIndex);Set(p,"speedIndex",si);Call(p,"TickBridge",0f);Call(p,"Capture");
                    roll=ParkFilmStore.Load(p.CurrentFilmId);
                    if(p.ExposedFrames!=frame+1 || roll.frames.Count!=frame+1)throw new InvalidOperationException("Equivalent exposure not saved");
                    var record=roll.frames[frame];
                    if(node.photoTask==ParkPhotoWaypoint.PhotoTask.Meadow && !record.joinedHands)
                        throw new InvalidOperationException("Saved meadow photograph lost the joined hands");
                    if(!record.framed || !record.visible || record.feedback!="Captured: "+record.moment+".")
                        throw new InvalidOperationException(node.photoTask+" passed but its saved assessment disagrees: "+record.feedback);
                    if(record.iso!=iso || record.aperture!=apertures[apertureIndex] || !record.accepted || !record.sharp || Mathf.Abs(record.exposureError)>.7f)
                        throw new InvalidOperationException("Saved exposure settings disagree");
                    byte[] bytes;string reason;
                    if(ParkFilmStore.TryReadDevelopedFrame(roll.id,frame+1,out bytes,out reason))throw new InvalidOperationException("Undeveloped photo exposed");
                    if(apertureIndex==3)
                    {
                        string dir="output/QA/2026-11-04-Park-Metering";System.IO.Directory.CreateDirectory(dir);
                        bytes=System.IO.File.ReadAllBytes(System.IO.Path.Combine(ParkFilmStore.Root,roll.id,record.negativeFile));
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,node.photoTask+"-ISO"+iso+".png"),bytes);
                    }
                    lines.Add("PASS "+node.photoTask+" ISO"+iso+" f/"+record.aperture+" 1/"+Mathf.RoundToInt(1/seconds)+" EV="+task.ev100+" error="+record.exposureError.ToString("F4")+" | saved; invalid exposure blocked; undeveloped hidden");
                }
                lines.Add("ROLL "+roll.id+" "+roll.state);
            }
            finally{p.enabled=enabled;p.Resume();}
            return string.Join("\n",lines);
        }
        public static string MeterScale()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || p==null)throw new InvalidOperationException("Park Play required");
            var meter=(PhotographyMeterDisplay)p.GetType().GetField("instrumentMeter",Hidden).GetValue(p);
            var lines=new List<string>();int[] isos={100,200,400,800},denominators={125,250,500,1000};
            try
            {
                for(int i=0;i<isos.Length;i++)
                {
                    meter.Apply(isos[i],13,true,1);
                    string text=(string)typeof(PhotographyMeterDisplay).GetField("lastText",Hidden).GetValue(meter);
                    bool ok=text.Contains("F8 1/"+denominators[i]+"\n");
                    lines.Add((ok?"PASS ":"FAIL ")+"ISO "+isos[i]+" advice uses available shutter: "+text.Replace('\n',' '));
                }
            }
            finally{p.GetType().GetMethod("SyncInstruments",Hidden).Invoke(p,null);}
            return string.Join("\n",lines);
        }
    }
}
