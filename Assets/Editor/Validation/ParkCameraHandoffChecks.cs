using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class ParkCameraHandoffChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object p,string field,object value)=>p.GetType().GetField(field,Hidden).SetValue(p,value);
        static object Get(object p,string field)=>p.GetType().GetField(field,Hidden).GetValue(p);
        static object Call(object p,string method,params object[] args)=>p.GetType().GetMethod(method,Hidden).Invoke(p,args);
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode only.");
            var scene=EditorSceneManager.NewPreviewScene();var cursor=Cursor.lockState;bool visible=Cursor.visible;
            var lines=new List<string>();Action<bool,string> check=(ok,label)=>lines.Add((ok?"PASS ":"FAIL ")+label);
            try
            {
                var root=new GameObject("Isolated camera handoff checks");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);
                var p=root.AddComponent<ParkPhotoPrototype>();p.GetComponent<CharacterController>().enabled=false;
                var cameraObject=new GameObject("Camera");cameraObject.transform.SetParent(root.transform,false);
                p.view=cameraObject.AddComponent<Camera>();p.view.enabled=false;
                Action<ParkPhotoPrototype.PhotographyMode> mode=m=>Call(p,"SetPhotographyMode",m);
                Action<float,float,float,bool> pointer=(x,y,dt,busy)=>{Set(p,"virtualPointer",new Vector2(x,y));Call(p,"TickCameraPointerEdge",dt,busy);};
                mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);
                pointer(.5f,.5f,.3f,true);pointer(.01f,.5f,1,false);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Held-button centre crossing cannot arm edge selection");
                foreach(var outside in new[]{new Vector2(-.1f,.5f),new Vector2(1.1f,.5f),new Vector2(.01f,-.1f),new Vector2(.99f,1.1f)})
                {
                    mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);pointer(.5f,.5f,.3f,false);pointer(outside.x,outside.y,1,false);
                    check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Outside pointer cannot open dial "+outside);
                }
                mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);pointer(.5f,.5f,.3f,false);pointer(.01f,.5f,.1f,false);
                pointer(-.1f,.5f,.1f,false);pointer(.01f,.5f,1,false);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Leaving viewport cancels dwell until a fresh centre visit");
                pointer(.5f,.5f,.3f,false);pointer(.01f,.5f,.3f,false);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Aperture,"Fresh centre and left dwell still opens aperture");
                mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);pointer(.5f,.5f,.3f,false);pointer(.01f,.5f,.2f,false);
                pointer(.99f,.5f,.1f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Opposite edges do not share dwell time");
                pointer(.99f,.5f,.2f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Shutter,"New edge completes its own dwell");
                mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);pointer(.5f,.5f,.3f,false);pointer(.01f,.5f,1,true);
                pointer(.01f,.5f,.1f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Busy return/click gesture does not accumulate edge dwell");
                Set(p,"moveVelocity",new Vector3(1,0,1));p.PreparePauseInput();p.ResumeFromPauseMenu();
                check(((Vector3)Get(p,"moveVelocity")).sqrMagnitude==0,"Pause and resume discard residual movement");
                mode(ParkPhotoPrototype.PhotographyMode.Viewfinder);pointer(.5f,.5f,.3f,false);pointer(.01f,.5f,.2f,false);
                p.PreparePauseInput();p.ResumeFromPauseMenu();pointer(.01f,.5f,1,false);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Pause discards partial edge dwell");
                root.SetActive(true);mode(ParkPhotoPrototype.PhotographyMode.Shutter);
                float aperture=p.Aperture,shutter=p.ShutterSeconds,focus=p.focusDistance;
                Call(p,"LeaveParkContext");
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Right-click back action exits just the dial");
                check(!p.InputContext.Ready,"Back action arms release gate before another exit");
                check(p.Aperture==aperture && p.ShutterSeconds==shutter && p.focusDistance==focus,"Back action preserves optical settings");
                foreach(var detail in new[]{ParkPhotoPrototype.PhotographyMode.Aperture,ParkPhotoPrototype.PhotographyMode.Shutter})
                {
                    mode(detail);pointer(.5f,.5f,.1f,false);
                    check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Centre returns immediately during entry cooldown: "+detail);
                    mode(detail);
                    pointer(.5f,.5f,.3f,true);
                    check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Held click does not delay centre return: "+detail);
                    pointer(.5f,.5f,.3f,false);
                    check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder && !p.InputContext.Ready,"Centre return hands off input before shutter/focus: "+detail);
                    check(p.Aperture==aperture && p.ShutterSeconds==shutter && p.focusDistance==focus,"Centre return retains optical settings: "+detail);
                    mode(detail);p.PreparePauseInput();p.ResumeFromPauseMenu();
                    check(p.PhotoMode==detail && !p.InputContext.Ready,"Pause preparation/resume retains detail with fresh release gate: "+detail);
                }
                var gate=typeof(ParkPhotoPrototype).GetMethod("FinderClickAllowed",BindingFlags.Static|BindingFlags.NonPublic);
                check(gate!=null,"Finder click eligibility is available for regression checks");
                if(gate!=null)
                {
                    Func<Vector2,bool,bool> click=(pos,palm)=>(bool)gate.Invoke(null,new object[]{pos,palm});
                    check(click(new Vector2(.02f,.5f),true) && click(new Vector2(.98f,.5f),true),"Visible palms at either edge remain clickable");
                    check(!click(new Vector2(.02f,.5f),false) && !click(new Vector2(.98f,.5f),false),"Empty edge clicks cannot expose film");
                    check(!click(new Vector2(-.01f,.5f),true) && !click(new Vector2(.5f,1.1f),false),"Outside clicks never reach palm or shutter");
                    check(click(new Vector2(.5f,.5f),false),"Normal finder shutter click remains available");
                }
                string report=string.Join("\n",lines);Directory.CreateDirectory("output/QA/CameraHandoff_20261106");File.WriteAllText("output/QA/CameraHandoff_20261106/checks.txt",report);return report;
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);Cursor.lockState=cursor;Cursor.visible=visible;}
        }
    }
}
