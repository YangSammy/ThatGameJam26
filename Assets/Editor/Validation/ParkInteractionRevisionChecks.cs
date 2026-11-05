using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Temporary geometry only; never opens a scene or writes a player's roll.
    public static class ParkInteractionRevisionChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Call(object p,string name,params object[] args)=>p.GetType().GetMethod(name,Hidden).Invoke(p,args);
        static void Set(object p,string name,object value)=>p.GetType().GetField(name,Hidden).SetValue(p,value);
        public static string Geometry()
        {
            var root=new GameObject("QA park loading geometry");root.SetActive(false);
            try
            {
                var p=root.AddComponent<ParkPhotoPrototype>();p.enabled=false;
                var camera=new GameObject("QA camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);p.view=camera;
                Call(p,"BuildAssetInstruments");Set(p,"loadingStep",1);Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);root.SetActive(true);
                var parts=(Dictionary<string,Transform>)typeof(ParkPhotoPrototype).GetField("instrumentParts",Hidden).GetValue(p);
                Physics.SyncTransforms();
                var target=parts["chamber"].position+camera.transform.up*.04f;
                var ray=new Ray(camera.transform.position,(target-camera.transform.position).normalized);
                string pick=(string)Call(p,"PickInstrument",ray);
                var lines=new List<string>{(pick=="chamber"?"PASS":"FAIL")+" visible cartridge-height slot accepts off-centre release (picked "+pick+")"};
                var slot=parts["chamber"].GetComponent<BoxCollider>();var fixedSize=slot.size;
                slot.size=new Vector3(.055f,.055f,.04f);Physics.SyncTransforms();
                bool oldMiss=!slot.Raycast(ray,out RaycastHit oldHit,2);slot.size=fixedSize;Physics.SyncTransforms();
                lines.Add((oldMiss?"PASS":"FAIL")+" legacy slot demonstrably misses the same visible-height release");
                Set(p,"loadingStep",2);Call(p,"SyncInstruments");
                Set(p,"dragPart","leader");
                var handRay=new Ray(camera.transform.position,(parts["spool"].position-camera.transform.position).normalized);
                Call(p,"MoveBodyPointer",handRay);var before=parts["leader"].position;
                Call(p,"SyncInstruments");
                lines.Add((Vector3.Distance(before,parts["leader"].position)<.001f?"PASS":"FAIL")+" held leader pointer survives presentation sync");
                Call(p,"CancelInstrumentDrag");
                Set(p,"roll",new ParkFilmRoll{cameraLoading=ParkCameraLoading.AtStep(0,100)});
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);Call(p,"ToggleCamera");
                lines.Add((p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Body?"PASS":"FAIL")+" E opens loading for an unprepared roll");
                Call(p,"ToggleCamera");
                lines.Add((p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Exploration?"PASS":"FAIL")+" E closes camera");
                Set(p,"roll",new ParkFilmRoll{cameraLoading=ParkCameraLoading.AtStep(7,400)});Set(p,"loadingStep",7);
                Call(p,"ToggleCamera");
                lines.Add((p.Aiming?"PASS":"FAIL")+" E enters viewfinder for loaded roll");
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                p.virtualCursorSpeed=0;Set(p,"virtualPointer",new Vector2(.96f,.04f));Set(p,"edgeArmed",true);Set(p,"cooldown",0f);
                Call(p,"TickPhotographyModes",.3f);
                lines.Add((p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Exploration?"PASS":"FAIL")+" bottom-right dwell never raises camera");
                Set(p,"virtualPointer",new Vector2(.04f,.04f));Set(p,"edgeArmed",true);Set(p,"cooldown",0f);
                Call(p,"TickPhotographyModes",.3f);
                lines.Add((p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Meter?"PASS":"FAIL")+" bottom-left meter entry remains available");
                var palm=typeof(ParkPhotoPrototype).GetMethod("PalmHit",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(var size in new[]{new Vector2(1920,1080),new Vector2(2560,1080),new Vector2(1280,1024)})
                {
                    camera.pixelRect=new Rect(0,0,size.x,size.y);
                    bool centre=(bool)palm.Invoke(null,new object[]{camera,new Vector3(0,0,1.5f)});
                    bool miss=(bool)palm.Invoke(null,new object[]{camera,new Vector3(.5f,0,1.5f)});
                    lines.Add((centre && !miss?"PASS":"FAIL")+" palm world-space radius centre/miss "+size);
                }
                var checkpoint=new BridgePhotoCheckpoint{hasReference=true,position=Vector3.zero,euler=Vector3.zero,subjectPoint=Vector3.forward*4};
                var subject=GameObject.CreatePrimitive(PrimitiveType.Capsule);subject.name="QA silhouette subject";subject.transform.SetParent(root.transform);subject.transform.localPosition=new Vector3(0,-.3f,4);
                p.companion=subject.AddComponent<ParkCompanion>();
                Set(p,"roll",new ParkFilmRoll{entrance=checkpoint});camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                float near=(float)Call(p,"CompositionCloseness");camera.transform.position=Vector3.right*2;
                float far=(float)Call(p,"CompositionCloseness");
                lines.Add((near>far && near>.99f?"PASS":"FAIL")+" composition feedback grows as reference position converges");
                camera.transform.position=Vector3.zero;
                var reference=new Texture2D(32,32);
                var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=x<16?Color.black:Color.white;
                reference.SetPixels(pixels);reference.Apply();Set(p,"bridgeReference",reference);Call(p,"InitializePhotoFeedback");
                var previous=RenderTexture.active;Call(p,"PrepareReferenceOutline");
                lines.Add((RenderTexture.active==previous?"PASS":"FAIL")+" outline generation restores active render target");
                var outline=(RenderTexture)p.GetType().GetField("referenceOutline",Hidden).GetValue(p);
                var read=new Texture2D(32,32,TextureFormat.RGBA32,false);
                try
                {
                    RenderTexture.active=outline;read.ReadPixels(new Rect(0,0,32,32),0,0);read.Apply();
                    float min=1,max=0;foreach(var pixel in read.GetPixels()){min=Mathf.Min(min,pixel.a);max=Mathf.Max(max,pixel.a);}
                    lines.Add((min<.01f && max>.1f?"PASS":"FAIL")+" reference overlay contains transparent background and visible edges");
                }
                finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(read);}
                lines.Add((ParkCameraResponse.IsSharp(4,4,8) && !ParkCameraResponse.IsSharp(4,18,8)?"PASS":"FAIL")+" focus feedback shares rendered clear range");
                lines.Add((Mathf.Abs(ParkCameraResponse.ExposureError(5.6f,1f/1000,400,13))<1 && ParkCameraResponse.ExposureError(16,1f/1000,100,13)>1?"PASS":"FAIL")+" exposure distinguishes accepted equivalent setting from underexposure");
                Set(p,"roll",null); // temporary fixture must never write settings on disable
                return string.Join("\n",lines);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static string PrepareFeedback()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null)throw new InvalidOperationException("Isolated Park Play required");
            var roll=ParkFilmStore.SaveCameraLoading(p.CurrentFilmId,ParkCameraLoading.AtStep(7,400));
            Set(p,"roll",roll);Set(p,"loadingStep",7);Call(p,"InitializeBridge");
            var node=(ParkPhotoWaypoint)p.GetType().GetField("bridgeNode",Hidden).GetValue(p);
            p.companion.ResumeAtPhotoNode(node);
            var cc=p.GetComponent<CharacterController>();cc.enabled=false;
            p.transform.position=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;cc.enabled=true;
            p.view.transform.SetPositionAndRotation(node.referenceView.position,node.referenceView.rotation);
            Set(p,"yaw",node.referenceView.eulerAngles.y);Set(p,"pitch",Mathf.DeltaAngle(0,node.referenceView.eulerAngles.x));
            Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);Call(p,"CaptureBridgeReference");
            Set(p,"speedIndex",8);Set(p,"apertureIndex",2);p.focusDistance=18;
            return "Prepared reference viewpoint with deliberately incorrect focus; no exposure consumed";
        }
    }
}
