using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    public static class WinterPendantPresentationChecks
    {
        const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
        static T Get<T>(PaintingWorld3D w,string name)=>(T)typeof(PaintingWorld3D).GetField(name,Flags).GetValue(w);
        static object Call(PaintingWorld3D w,string name,params object[] args)=>typeof(PaintingWorld3D).GetMethod(name,Flags).Invoke(w,args);
        static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);}
        static Ray KeyRay(PaintingWorld3D w)
        {
            var key=Get<Transform>(w,"key");return new Ray(w.View.transform.position,key.TransformPoint(new Vector3(0,.2f,0))-w.View.transform.position);
        }
        static Ray HoleRay(PaintingWorld3D w)
        {
            var camera=Get<Camera>(w,"albumCamera");var uv=camera.WorldToViewportPoint(Get<Transform>(w,"hole").position);
            var page=Get<Collider>(w,"pageCollider").transform;var position=page.TransformPoint(new Vector3(uv.x-.5f,uv.y-.5f,0));
            return new Ray(w.View.transform.position,position-w.View.transform.position);
        }
        public static string Stage(bool found,bool book)
        {
            var host=WBMain.Instance;var w=host?.CurrentWorld as PaintingWorld3D;
            Check(Application.isPlaying && w!=null && w.Active,"Enter the actual winter chapter first.");
            host.SetHelpVisible(false);host.enabled=false;w.CancelPointerGesture();
            w.State.RestoreCheckpoint("{\"version\":5,\"phase\":1,\"shapes\":[true,true,true,true,"+(found?"true":"false")+"],\"crate\":4,\"high\":true}");
            var hole=Get<Transform>(w,"hole");var route=Get<Transform>(w,"route");
            w.State.Actor=new Vector2(route.InverseTransformPoint(hole.position).x-.55f,3.2f);w.State.SetBook(book);
            Call(w,"RestoreWinterPerformance");Call(w,"SetObservationAct",1);Call(w,"RestoreDoorView");Call(w,"RefreshAlbum");w.Defocus.SetFocusHeld(true,2);Physics.SyncTransforms();
            return "Staged actual winter scene; no completion injected.";
        }
        public static string Run()
        {
            Stage(false,true);var w=(PaintingWorld3D)WBMain.Instance.CurrentWorld;
            var notes=new List<string>();var camera=Get<Camera>(w,"albumCamera");var key=Get<Transform>(w,"key");var layout=Get<WinterAlbumWorkspace>(w,"layout");
            Call(w,"TickPendantHint",20f);Check(!w.State.PendantHintPlayed,"Hint consumed behind the raised album.");
            w.State.SetBook(false);Call(w,"TickPendantHint",6.9f);Check(!w.State.PendantHintPlayed,"Hint fired too early.");Call(w,"TickPendantHint",.2f);
            Check(w.State.PendantHintPlayed,"Closed-book hint missing.");notes.Add("PASS cue waits for lowered album and seven seconds; existing saved hint flag used.");
            // Real collider hit from the permitted fixed eye; search the exposed region, not through a wall.
            Call(w,"RefreshAlbum");var bounds=layout.pendantFocus.bounds;int visible=0;
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {var p=bounds.center+new Vector3((x/6f-.5f)*bounds.size.x,(y/6f-.5f)*bounds.size.y,-bounds.extents.z);if(w.Pick(new Ray(w.View.transform.position,p-w.View.transform.position))=="shape-pendant")visible++;}
            Check(visible>=5,"Pendant exposed region too small or occluded.");notes.Add("PASS exposed pendant hit samples="+visible+"/49 from the fixed room eye.");
            Stage(true,true);Check(!layout.observedPendant.gameObject.activeSelf && key.gameObject.activeSelf,"Duplicate discovered pendant.");
            var near=w.State.Actor;w.State.Actor=new Vector2(10.8f,3.2f);Call(w,"RestoreDoorView");Check(Mathf.Abs(camera.orthographicSize-5.2f)<.001f,"Wide view missing.");
            var keyBefore=key.position;var scale=key.lossyScale;w.State.Actor=near;Call(w,"TickDoorView",.375f);
            Check(camera.orthographicSize>layout.doorViewSize && camera.orthographicSize<5.2f,"No intermediate approach framing.");Call(w,"TickDoorView",.375f);
            Check(Mathf.Abs(camera.orthographicSize-layout.doorViewSize)<.001f,"Close framing did not settle.");
            Check(key.position==keyBefore && key.lossyScale==scale,"Page key zoomed with the route.");
            w.State.SetBook(false);var held=camera.transform.position;Call(w,"TickDoorView",2f);Check(camera.transform.position==held,"Lowering book resets framing.");w.State.SetBook(true);
            Physics.SyncTransforms();var pick=KeyRay(w);Check(w.Pick(pick)=="album-key","Physical page key cannot be picked.");w.Press("album-key",pick,0);
            Check(w.HasPointerGesture,"Key drag did not start.");w.State.Actor=new Vector2(10.8f,3.2f);Call(w,"TickDoorView",2f);Check(camera.transform.position==held,"Camera moved during drag.");w.State.Actor=near;
            w.Move(new Ray(w.View.transform.position,w.View.transform.right));w.CancelPointerGesture();Check(!w.HasPointerGesture && key.localPosition==Get<Vector3>(w,"keyHome"),"Cancel lost key/home.");
            w.Press("album-key",KeyRay(w),0);w.Release("",new Ray(w.View.transform.position,w.View.transform.right));Check(!w.State.KeyInserted && key.gameObject.activeSelf,"Wrong drop consumed key.");
            notes.Add("PASS approach interpolation, fixed page key, closed-book retention, drag camera lock, cancel and wrong drop.");
            var drop=HoleRay(w);Check((bool)Call(w,"PendantDropMatches",drop),"Page-to-route drop mapping failed.");
            w.Press("album-key",KeyRay(w),0);w.Move(drop);
            object[] args={ (Vector2)w.View.WorldToScreenPoint(drop.GetPoint(2)), null };
            var target=typeof(PaintingWorld3D).GetMethod("HoverVisual",Flags).Invoke(w,args) as Transform;
            Check(target==layout.recessOutline && (Camera)args[1]==camera,"Valid drop does not outline the actual recess.");
            w.Release("",drop);Check(w.State.KeyInserted && !key.gameObject.activeSelf && w.DoorPerformanceActive,"Accepted drop did not commit once.");
            string saved=w.State.CaptureCheckpoint();w.Deactivate();w.State.RestoreCheckpoint(saved);w.Activate();
            Check(w.State.KeyInserted && !key.gameObject.activeSelf && !w.DoorPerformanceActive,"Re-entry duplicated key or replayed insertion.");
            Stage(true,true);string foundSave=w.State.CaptureCheckpoint();w.Deactivate();w.State.RestoreCheckpoint(foundSave);w.Activate();
            Check(key.gameObject.activeSelf && !layout.observedPendant.gameObject.activeSelf && !w.State.KeyInserted,"Found-but-uninserted restore lost ownership.");
            notes.Add("PASS production page-space drop/outline, insertion, inserted and discovered checkpoint re-entry.");
            Stage(true,true);return string.Join("\n",notes);
        }
    }
}
