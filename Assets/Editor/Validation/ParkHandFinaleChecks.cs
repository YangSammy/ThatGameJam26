using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class ParkHandFinaleChecks
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit mode.");
            var scene=EditorSceneManager.NewPreviewScene();var cursor=Cursor.lockState;bool visible=Cursor.visible;
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>lines.Add((ok?"PASS ":"FAIL ")+label);
            var saveScope=ParkFilmStore.BeginValidationScope("output/QA/HandFinale20261107/Isolated-"+Guid.NewGuid().ToString("N"));
            try
            {
                var root=new GameObject("Hand finale isolated check");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);
                var p=root.AddComponent<ParkPhotoPrototype>();
                var camera=new GameObject("View");camera.transform.SetParent(root.transform,false);p.view=camera.AddComponent<Camera>();p.view.enabled=false;p.view.scene=scene;
                p.focusEffect=camera.AddComponent<ParkPhotoFocus>();
                var girl=new GameObject("Companion");girl.transform.SetParent(root.transform,false);p.companion=girl.AddComponent<ParkCompanion>();
                girl.transform.position=Vector3.forward*3;
                p.companion.head=p.companion.hips=p.companion.feet=p.companion.chest=girl.transform;
                var node=new GameObject("Checkpoint");node.transform.SetParent(root.transform,false);Set(p,"bridgeNode",node.AddComponent<ParkPhotoWaypoint>());
                var film=new ParkFilmRoll{id="isolated",state=ParkFilmState.Loaded,meadow=new BridgePhotoCheckpoint{hasReference=true,aligned=true,handJoined=true}};
                film.frames.Add(new ParkPhotoRecord());film.frames.Add(new ParkPhotoRecord());Set(p,"roll",film);
                Set(p,"<ScenePresentationReady>k__BackingField",true);
                check(!p.CompleteStageShortcut(2) && p.ExposedFrames==2,"Joined blocks stage shortcut without filling film");
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                Set(p,"handJoinTransition",.3f);
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Exploration,ParkPhotoPrototype.PhotographyMode.Body,ParkPhotoPrototype.PhotographyMode.Meter})
                {Call(p,"SetPhotographyMode",mode);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Joined blocks "+mode);Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);}
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Aperture);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Aperture,"Aperture remains available");
                Call(p,"LeaveParkContext");check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Dial back returns to finder");
                Call(p,"LeaveParkContext");check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Finder back cannot exit");
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Shutter);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Shutter,"Shutter remains available");
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                check((float)typeof(ParkPhotoPrototype).GetField("handJoinTransition",Flags).GetValue(p)==.3f,"Dial navigation preserves response timer");
                Set(p,"virtualPointer",new Vector2(.5f,.5f));float focus=p.focusDistance;
                Call(p,"StepFinderWheel",1f);check(p.focusDistance==focus,"Focus waits for hand response to settle");
                Set(p,"handJoinTransition",0f);Call(p,"StepFinderWheel",1f);
                check(p.focusDistance>focus,"Settled joined finder permits manual focus");
                focus=p.focusDistance;
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Aperture,ParkPhotoPrototype.PhotographyMode.Shutter})
                {
                    Call(p,"SetPhotographyMode",mode);float aperture=p.Aperture,shutter=p.ShutterSeconds;
                    Call(p,"StepFinderWheel",1f);Call(p,"StepInstrumentControl",1);
                    check(p.focusDistance==focus && (mode==ParkPhotoPrototype.PhotographyMode.Aperture?p.Aperture!=aperture && p.ShutterSeconds==shutter:p.ShutterSeconds!=shutter && p.Aperture==aperture),"Joined dial changes its own setting only: "+mode);
                }
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                var apertures=(float[])typeof(ParkPhotoPrototype).GetField("apertures",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                var speeds=(float[])typeof(ParkPhotoPrototype).GetField("speeds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                bool all=true;int count=0;
                for(int a=0;a<apertures.Length;a++)for(int s=0;s<speeds.Length;s++)
                {Set(p,"apertureIndex",a);Set(p,"speedIndex",s);var record=(ParkPhotoRecord)Call(p,"Assess");all&=record.accepted;count++;}
                check(all,"All "+count+" aperture/shutter combinations accepted regardless of framing/focus");
                var hands=camera.AddComponent<FirstPersonHands>();hands.park=p;
                var arm=new GameObject("Arm reference");arm.transform.SetParent(camera.transform,false);arm.transform.localPosition=new Vector3(-.2f,-.3f,.1f);
                hands.skeletalRig=arm.AddComponent<FirstPersonArmRig>();
                var phase=typeof(FirstPersonHands).GetField("handholdPhase",Flags);phase.SetValue(hands,Enum.Parse(phase.FieldType,"Hold"));
                var response=camera.AddComponent<ParkHandResponseView>();response.owner=p;Set(response,"hands",hands);Set(p,"handResponseView",response);
                var cameraPosition=camera.transform.position;var armPosition=arm.transform.position;
                for(int i=0;i<10;i++)
                {
                    Call(response,"OnPreCull");check(Vector3.Distance(camera.transform.position,cameraPosition+Vector3.forward*.1f)<.00001f && Vector3.Distance(arm.transform.position,armPosition)<.00001f,"Render moves optical viewpoint only "+i);
                    Call(response,"OnPostRender");
                }
                check(Vector3.Distance(camera.transform.position,cameraPosition)<.00001f && Vector3.Distance(arm.transform.position,armPosition)<.00001f,"Repeated render restores camera and arm exactly");
                p.focusDistance=7;
                var lateUpdate=typeof(ParkPhotoPrototype).GetMethod("LateUpdate",Flags);
                if(lateUpdate!=null)lateUpdate.Invoke(p,null);
                Call(p,"SyncCameraPreview");
                check(p.focusDistance==7 && p.focusEffect.distance==7,"Completed hand response preserves player focus without automatic correction");
                check(Mathf.Abs(p.focusEffect.subjectDepth-2.9f)<.00001f,"Preview still measures depth from advanced optical viewpoint");
                film.meadow.handJoined=false;Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Exploration,"Before response exploration remains available");
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);saveScope.Dispose();Cursor.lockState=cursor;Cursor.visible=visible;}
            return string.Join("\n",lines);
        }
    }
}
