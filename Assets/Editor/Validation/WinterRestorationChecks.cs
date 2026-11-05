using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MemoryWhitebox.Art;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Temporary owned objects only: no scene save, Play Mode, or native mouse input.
    public static class WinterRestorationChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool yes,string why){if(!yes)throw new InvalidOperationException(why);}
        static T Get<T>(PaintingWorld3D w,string name)=>(T)typeof(PaintingWorld3D).GetField(name,Private).GetValue(w);
        static void Set(PaintingWorld3D w,string name,object value)=>typeof(PaintingWorld3D).GetField(name,Private).SetValue(w,value);
        static object Call(PaintingWorld3D w,string name,params object[] args)=>typeof(PaintingWorld3D).GetMethod(name,Private).Invoke(w,args);
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}

        public static string RunEditMode()
        {
            Require(!Application.isPlaying,"RunEditMode must run outside Play Mode.");
            var notes=new List<string>();var cues=new List<int>();
            Action<int,bool> heard=(id,motion)=>{if(!motion)cues.Add(id);};
            GameObject cameraObject=null;PaintingWorld3D w=null;
            StorySfx.CueRequested+=heard;
            try
            {
                if(Camera.main==null){cameraObject=new GameObject("Winter check camera",typeof(Camera));cameraObject.tag="MainCamera";}
                var s=new PaintingChapter();w=new PaintingWorld3D(null,s);
                var route=Child(w.Root.transform,"Test route");Set(w,"route",route);Call(w,"BuildRoute");
                var art=PaintingVisualAssets.Load();
                Require(art.actor!=null && art.actor.name=="PF_AlbumActor_E02","Actor asset is unbound.");
                var actor=Get<Transform>(w,"actor");var visual=actor.GetComponentInChildren<AlbumWinterVisual>();
                Require(actor.GetComponentsInChildren<SkinnedMeshRenderer>().Length>0,"Production actor is not skinned art.");
                Require(actor.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Actor art introduced collider blockers.");
                foreach(var pose in new[]{"Walk","Board","Push","SitSwing","RideFlower","Takeoff","Air","Land","Dismount","KeyReach"})
                {visual.SetActorPose(pose,.5f,90);Require(visual.Capture().actorPose==pose,"Pose not sampled: "+pose);}
                Require(Vector3.Dot(actor.up,Vector3.up)>.999f,"Actor root must remain upright.");
                Require(Get<Transform>(w,"actorSeatContact")!=null && Get<Transform>(w,"actorHandContact")!=null,"Source seat/hand contacts missing.");
                notes.Add("PASS actual E02 skinned prefab and ten authored poses; upright root; no collider blockers");

                Call(w,"MoveObservationPointer",new Vector2(.5f,.5f));
                Call(w,"MoveObservationPointer",new Vector2(.6f,.6f));
                Require(Get<float>(w,"yaw")>0 && Get<float>(w,"pitch")<0,"Direct mouse movement needs a button.");
                float yaw=Get<float>(w,"yaw");w.Press("",new Ray(),0);
                Call(w,"MoveObservationPointer",new Vector2(.7f,.5f));
                Require(Get<float>(w,"yaw")==yaw,"Focus aiming drifts the camera.");
                w.CancelPointerGesture();Call(w,"MoveObservationPointer",new Vector2(.98f,.5f));
                Require(Get<float>(w,"yaw")==yaw,"Resume cursor warp injected head movement.");
                Set(w,"pointer",new Vector2(.98f,.5f));Call(w,"TickObservationPointer",10f);
                Require(Get<float>(w,"yaw")==65,"Horizontal free cursor margin cannot reach bounded look limit.");
                w.CancelPointerGesture();Require(!w.HasPointerGesture && !Get<bool>(w,"edgeArmed"),"Cancel retained a gesture/edge trigger.");
                notes.Add("PASS no-button look, focus aiming freeze, warp suppression, +/-65 yaw clamp, cancellation");

                s.RestoreCheckpoint("{\"version\":5,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":4,\"high\":true}");
                s.Actor=new Vector2(14.5f,3.2f);s.SetBook(true);
                var door=UnityEngine.Object.Instantiate(art.door,route,false).GetComponent<AlbumWinterVisual>();Set(w,"winterDoor",door);
                var embedded=door.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="KeyVisual");
                var pendantMeshes=new HashSet<Mesh>(art.pendant.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).Where(m=>m!=null));
                var doorKeyMeshes=new HashSet<Mesh>(embedded.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).Where(m=>m!=null));
                Require(pendantMeshes.Count>0 && pendantMeshes.SetEquals(doorKeyMeshes),"Door key and draggable pendant do not share actual mesh sources.");
                var key=Get<Transform>(w,"key");var hole=Get<Transform>(w,"hole");hole.localPosition=new Vector3(14.7f,4,-.48f);
                Set(w,"keyDragging",true);key.localPosition+=Vector3.right;
                cues.Clear();w.Release("",new Ray(Vector3.zero,Vector3.up));
                Require(!s.KeyInserted && key.localPosition==Get<Vector3>(w,"keyHome") && !cues.Contains(47) && !cues.Contains(48),"Wrong drop consumed key or played success sound.");
                // Route camera and page collider exercise actual production PageRay/drop handlers.
                var page=GameObject.CreatePrimitive(PrimitiveType.Quad);page.transform.SetParent(w.Root.transform,false);
                var cam=Child(w.Root.transform,"Test album camera").gameObject.AddComponent<Camera>();
                cam.enabled=false;cam.orthographic=true;cam.transform.SetPositionAndRotation(hole.position-Vector3.forward*10,Quaternion.identity);
                Set(w,"pageCollider",page.GetComponent<Collider>());Set(w,"albumCamera",cam);
                var ray=new Ray(page.transform.position-Vector3.forward*2,Vector3.forward);Physics.SyncTransforms();
                w.Press("album-key",ray,0);Require(Get<bool>(w,"keyDragging"),"Production key pickup failed.");
                w.Release("key-recess",ray);
                Require(s.KeyInserted && w.DoorPerformanceActive && !key.gameObject.activeSelf,"Accepted drop must commit one key and start its presentation.");
                Require(cues.Count(id=>id==47)==1 && !cues.Contains(48),"Insertion/open sound phases collapsed.");
                Set(w,"paper",Child(w.Root.transform,"Test paper"));Set(w,"observation",Child(w.Root.transform,"Test observation"));Set(w,"built",true);
                var input=new WBInput();input.Keys.Add(KeyCode.D);var start=s.Actor;
                w.Tick(.3f,input);Call(w,"ApplyDoorPerformance");var sample=door.Capture();
                Require(s.Actor==start && sample.insert>0 && sample.insert<1 && sample.unlock==0 && sample.door==0,"Insertion did not freeze movement or stage the door.");
                float paused=Get<float>(w,"doorPerformanceTime");w.CancelPointerGesture();w.Tick(0,input);
                Require(Get<float>(w,"doorPerformanceTime")==paused && s.Actor==start,"Pause advanced or cancelled accepted key performance.");
                w.Tick(.5f,input);Call(w,"ApplyDoorPerformance");sample=door.Capture();
                Require(sample.insert==1 && sample.unlock>0 && sample.unlock<1 && sample.door==0,"Unlock progression incorrect.");
                w.Tick(.4f,input);Call(w,"ApplyDoorPerformance");sample=door.Capture();
                Require(sample.door>0 && sample.door<1 && cues.Count(id=>id==48)==1,"Door opening was not progressive/sounded once.");
                w.Tick(.5f,input);Call(w,"ApplyDoorPerformance");
                Require(!w.DoorPerformanceActive && s.Actor==start && door.Capture().door==1,"Route resumed before presentation finished.");
                int count=cues.Count;Call(w,"RestoreWinterPerformance");Call(w,"ApplyDoorPerformance");
                Require(door.Capture().door==1 && !w.DoorPerformanceActive && cues.Count==count,"Restored inserted key replayed performance/audio.");
                notes.Add("PASS actual page-ray wrong/correct drops; same source pendant; unique key; phased insertion/unlock/open; frozen route/pause; restore no replay");

                s.RestoreCheckpoint("{\"version\":5,\"phase\":1,\"shapes\":[true,true,true,true,false],\"crate\":4,\"high\":true}");s.Actor=new Vector2(14.5f,3.2f);
                var layout=Child(w.Root.transform,"Test hint layout").gameObject.AddComponent<WinterAlbumWorkspace>();layout.observedPendant=Child(layout.transform,"Pendant");Set(w,"layout",layout);
                cues.Clear();Call(w,"TickPendantHint",6.9f);Require(!s.PendantHintPlayed,"Hint fired before stall delay.");
                Call(w,"TickPendantHint",.2f);Call(w,"TickPendantHint",20f);
                Require(s.PendantHintPlayed && cues.Count(id=>id==246)==1 && !cues.Contains(46) && !cues.Contains(146),"Pre-discovery hint is duplicated or confused with focus feedback.");
                string save=s.CaptureCheckpoint();s.RestoreCheckpoint(save);s.Actor=new Vector2(14.5f,3.2f);Call(w,"RestoreWinterPerformance");Call(w,"TickPendantHint",20f);
                Require(cues.Count(id=>id==246)==1,"Checkpoint replayed once-only hint.");
                notes.Add("PASS stalled-door discovery cue 246 once after 7s, distinct from 46/146, persistent across checkpoint");
                return string.Join("\n",notes);
            }
            finally
            {
                StorySfx.CueRequested-=heard;
                if(w!=null)w.Dispose();
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
