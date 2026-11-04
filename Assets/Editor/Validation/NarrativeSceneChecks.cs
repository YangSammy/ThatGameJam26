using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox.Editor
{
    // Run in a disposable project/save slot. Scene transitions deliberately exercise lifecycle cleanup.
    public static class NarrativeSceneChecks
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly List<string> checks=new List<string>();
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add("PASS "+label);File.WriteAllLines("QA/narrative-scene-checks.txt",checks);}
        static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
        static void Focus(WBMain h){h.SendMessage("OnApplicationFocus",true);h.ReleaseHouseReturnInput(true);h.InputContext.ContinueGesture();}
        static void Enter(WBMain h,int index)
        {Focus(h);h.Select(index);Check(h.InStation,"Enter chapter "+index);for(int i=0;i<4;i++)h.AdvanceHouseTransition(1);h.ReleaseHouseEntryInput();Focus(h);h.SyncInputContext();h.InputContext.ContinueGesture();}
        static void Leave(WBMain h){h.LeaveStation();for(int i=0;i<4;i++)h.AdvanceHouseTransition(1);Focus(h);Check(!h.InStation,"Return to room");}
        static void Capture(Camera camera,string name,Camera overlay=null)
        {
            var rt=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();if(overlay!=null){var previous=overlay.targetTexture;overlay.targetTexture=rt;overlay.Render();overlay.targetTexture=previous;}RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("QA/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
        }
        static Ray PageRay(PaintingWorld3D world,Vector3 target)
        {
            var camera=Field<Camera>(world,"albumCamera");var page=Field<Collider>(world,"pageCollider");var uv=camera.WorldToViewportPoint(target);
            Vector3 point=page.transform.TransformPoint(new Vector3(uv.x-.5f,uv.y-.5f,0));return new Ray(world.View.transform.position,point-world.View.transform.position);
        }
        public static IEnumerator Flow()
        {
            Directory.CreateDirectory("QA");checks.Clear();
            for(int i=0;i<15;i++)yield return null;
            var h=UnityEngine.Object.FindObjectOfType<WBMain>();Check(h!=null&&h.HouseReady,"House initializes with authored references");h.enabled=false;Focus(h);
            var start=h.playerCamera.transform.position;var rotation=h.playerCamera.transform.rotation;
            try{ContinuousDarkroomChecks.Run(h);File.WriteAllText("QA/continuous-darkroom-result.txt","PASS");}
            catch(Exception error){File.WriteAllText("QA/continuous-darkroom-result.txt","FAIL\n"+error);Debug.LogWarning("Separate darkroom suite failed: "+error.Message);}
            finally{h.DarkroomWorld.CancelInteraction();h.DarkroomWorld.SetDirectDarkroom(false);h.SyncInputContext();h.InputContext.ContinueGesture();}
            var binding=h.GetComponent<NarrativeSceneBinding>();Check(binding!=null&&binding.entranceDoor!=null&&binding.threshold!=null,"Narrative scene anchors resolve");
            Enter(h,2);var writing=(WritingWorld3D)h.CurrentWorld;
            Ray pen(float x)=>new Ray(writing.World(new Vector3(x,2,.3f)),Vector3.down);
            writing.Press("paper",pen(-.4f),0);writing.Move(pen(-.3f));writing.Release("paper",pen(-.3f));
            Check(writing.State.WritingProgress>0&&writing.InkStrokeCount>0,"World paper input produces physical ink");
            Capture(h.playerCamera,"autumn-writing",writing.MemoryCamera);float ink=writing.State.WritingProgress;
            Leave(h);Check(Vector3.Distance(start,h.playerCamera.transform.position)<.001f&&Quaternion.Angle(rotation,h.playerCamera.transform.rotation)<.01f,"Writing restores original room view");
            Enter(h,2);Check(writing.InkStrokeCount>0&&writing.State.WritingProgress==ink,"Writing restored ink and progress");Leave(h);
            Enter(h,3);var album=(PaintingWorld3D)h.CurrentWorld;var p=album.State;
            Capture(h.playerCamera,"winter-observe");
            p.RestoreCheckpoint("{\"version\":2,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":4,\"high\":true}");p.SetBook(true);p.Actor=new Vector2(14.3f,3.2f);album.Tick(.02f,new WBInput());Physics.SyncTransforms();
            var key=Field<Transform>(album,"key");var hole=Field<Transform>(album,"hole");var ray=PageRay(album,key.GetChild(0).position);
            Check(album.Pick(ray)=="album-key","Physical page ray reaches pendant");
            album.Press("album-key",ray,0);album.Move(PageRay(album,hole.position));album.CancelPointerGesture();Check(!p.KeyInserted,"Cancelled pendant drag does not unlock");
            ray=PageRay(album,key.GetChild(0).position);album.Press("album-key",ray,0);album.Move(PageRay(album,hole.position));album.Release("key-recess",PageRay(album,hole.position));Check(p.KeyInserted,"Page drag inserts matching pendant");
            album.Defocus.SetFocusHeld(true,1);Field<Camera>(album,"albumCamera").Render();Capture(h.playerCamera,"winter-album");
            p.RestoreCheckpoint("{\"version\":2,\"phase\":3,\"shapes\":[true,true,true,true,true],\"key\":true,\"crate\":4,\"ward\":2}");album.Tick(.02f,new WBInput());
            var paper=Field<Transform>(album,"paper");var winterImage=Field<RenderTexture>(album,"albumTexture");Field<Camera>(album,"albumCamera").Render();Capture(h.playerCamera,"winter-ward");
            var walk=new WBInput();walk.Keys.Add(KeyCode.D);for(int i=0;i<300&&h.InStation;i++)album.Tick(.02f,walk);
            Check(p.Completed&&!h.InStation,"Nose cutoff commits and returns to house immediately");
            Check(paper!=null&&!paper.gameObject.activeInHierarchy&&album.GetFinalPageImage()==winterImage,"Spring receives the frozen winter image");
            Check(Vector3.Distance(start,h.playerCamera.transform.position)<.001f,"Ward cutoff restores original room view");
            Check(h.SaveStableProgress(),"Narrative snapshots commit to unified save");
            h.Select(4);var ending=(EndingWorld3D)Field<WorldChapter3D[]>(h,"worlds")[4];var eye=binding.threshold.position+Vector3.up*1.65f+binding.threshold.forward*.6f;
            h.playerCamera.transform.position=eye;var doorRay=new Ray(eye,binding.doorHandle.position-eye);ending.StepHouse(.02f,doorRay,true);ending.StepHouse(1,doorRay,false);
            Check(ending.Ending.DoorOpen&&!ending.Ending.Completed,"Authored entrance opens without finishing");
            h.playerCamera.transform.position=eye-binding.threshold.forward*.8f;ending.StepHouse(.02f,doorRay,false);Check(ending.Ending.Completed,"Crossing authored threshold finishes departure");
            // Set up earlier days for a later-scene startup check; this is not a tape/photo gameplay pass.
            var story=Field<FiveDayProgress>(h,"story");for(int i=0;i<5;i++)if(!story.IsComplete(i))Check(story.Complete(i,h.FindChapter(ChapterIds.At(i)).CaptureCheckpoint()),"Fixture day available "+i);
            var load=SceneManager.LoadSceneAsync(ParkFilmRoute.ParkScenePath);while(!load.isDone)yield return null;for(int i=0;i<15;i++)yield return null;
            var park=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();Check(park!=null&&park.enabled&&park.view!=null&&park.companion!=null,"Park starts with player and companion (scene="+SceneManager.GetActiveScene().name+", player="+(park!=null)+", enabled="+(park!=null&&park.enabled)+", camera="+(park!=null&&park.view!=null)+", companion="+(park!=null&&park.companion!=null)+")");park.enabled=false;Capture(park.view,"park-startup");
            load=SceneManager.LoadSceneAsync(ParkFilmRoute.HouseScenePath);while(!load.isDone)yield return null;for(int i=0;i<15;i++)yield return null;
            h=UnityEngine.Object.FindObjectOfType<WBMain>();Check(h!=null&&h.HouseReady,"House reload restores unified progress");h.enabled=false;
            for(int i=0;i<600 && h.DepartureRoom==null;i++)yield return null;
            var room=h.DepartureRoom;album=(PaintingWorld3D)Field<WorldChapter3D[]>(h,"worlds")[3];
            Check(room!=null && room.finalPage.transform.parent==room.paperAnchor && room.finalPage.gameObject.activeInHierarchy &&
                room.finalPage.Capture().pageTexture==album.GetFinalPageImage(),"Completed image restored on the authored spring page");
            Debug.Log("NARRATIVE_SCENE_PASS "+checks.Count);
        }
    }
}
