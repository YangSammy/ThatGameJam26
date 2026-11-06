using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class ParkCameraEdgeChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object p,string name,object value)=>p.GetType().GetField(name,Private).SetValue(p,value);
        static object Get(object p,string name)=>p.GetType().GetField(name,Private).GetValue(p);
        static object Call(object p,string name,params object[] args)=>p.GetType().GetMethod(name,Private).Invoke(p,args);
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new Exception("Use Edit mode.");
            var lines=new System.Collections.Generic.List<string>();
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);lines.Add("PASS "+label);};
            var scene=EditorSceneManager.NewPreviewScene();var cursor=Cursor.lockState;bool visible=Cursor.visible;
            ParkPhotoPrototype p=null;
            string path=Path.GetFullPath("output/QA/CameraEdge_20261106");Directory.CreateDirectory(path);
            try
            {
                var root=new GameObject("Camera edge isolated check");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);
                p=root.AddComponent<ParkPhotoPrototype>();p.GetComponent<CharacterController>().enabled=false;
                var cameraObject=new GameObject("Camera");cameraObject.transform.SetParent(root.transform,false);
                p.view=cameraObject.AddComponent<Camera>();p.view.enabled=false;p.view.scene=scene;p.view.pixelRect=new Rect(0,0,960,540);
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                Action<float,float,float,bool> edge=(x,y,dt,busy)=>{Set(p,"virtualPointer",new Vector2(x,y));Call(p,"TickCameraPointerEdge",dt,busy);};
                edge(.01f,.5f,1,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Entry pointer at edge cannot auto-open a dial");
                edge(.5f,.5f,.3f,false);edge(.01f,.5f,.1f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Brief edge crossing does not open dial");
                edge(.01f,.5f,.2f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Aperture,"Left dwell opens aperture");
                float focus=p.focusDistance,shutter=p.ShutterSeconds,aperture=p.Aperture;
                Call(p,"StepInstrumentControl",1);Call(p,"StepFinderWheel",1f);
                check(p.Aperture!=aperture && p.focusDistance==focus && p.ShutterSeconds==shutter,"Aperture wheel has one owner");
                edge(.5f,.5f,.3f,true);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder && !p.InputContext.Ready,"Held mouse returns to finder with shutter release gate armed");
                edge(.5f,.5f,.3f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Returning to centre exits dial");
                edge(.5f,.5f,.3f,false);edge(.99f,.5f,.3f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Shutter,"Right dwell opens shutter");
                aperture=p.Aperture;Call(p,"StepInstrumentControl",1);Call(p,"StepFinderWheel",1f);
                check(p.ShutterSeconds!=shutter && p.Aperture==aperture && p.focusDistance==focus,"Shutter wheel has one owner");
                root.SetActive(true);
                Call(p,"LeaveParkContext");
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Back action returns just one level to finder");
                check(!GamePauseMenu.Active,"Back action does not open pause");
                edge(.99f,.5f,1,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Back action cannot immediately retrigger an edge");
                edge(.5f,.5f,.3f,false);Call(p,"StepFinderWheel",1f);
                check(p.focusDistance!=focus && p.Aperture==aperture,"Finder wheel changes focus only");
                edge(.5f,.99f,.4f,false);check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Top edge does not lower camera");
                check(Cursor.visible && Cursor.lockState==CursorLockMode.None,"Finder uses real visible cursor");
                p.PreparePauseInput();p.ResumeFromPauseMenu();edge(.01f,.5f,1,false);
                check(p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder,"Pause recovery disarms stale edge dwell");
                var palm=new Vector3(.8f,0,3);var screen=p.view.WorldToScreenPoint(palm);
                var hit=typeof(ParkPhotoPrototype).GetMethod("PalmHit",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(Camera),typeof(Vector3),typeof(Vector2)},null);
                check((bool)hit.Invoke(null,new object[]{p.view,palm,(Vector2)screen}) && !(bool)hit.Invoke(null,new object[]{p.view,palm,new Vector2(20,20)}),"Palm uses actual pointer instead of screen centre");
                Call(p,"InitializePhotographyModes");
                Set(p,"loadingStep",7); // Real dial use happens with film already loaded.
                var startRotation=p.view.transform.rotation;var startPosition=p.view.transform.position;
                foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Aperture,ParkPhotoPrototype.PhotographyMode.Shutter})
                {
                    Set(p,"dialDetailFrame",-1);Call(p,"SetPhotographyMode",mode);
                    var detail=(Camera)Get(p,"dialDetailCamera");detail.scene=scene;Set(p,"dialDetailFrame",-1);Call(p,"RenderCameraDetail");
                    var visual=(PhotographyCameraVisual)Get(p,"instrumentCamera");
                    check(visual.cartridges[0].activeSelf,"Loaded film is present during framing check: "+mode);
                    check(Vector3.Dot(detail.transform.forward,-visual.body.up)>.9999f,"Detail looks straight down on camera: "+mode);
                    bool fits=true;
                    var projectedMin=Vector2.one;var projectedMax=Vector2.zero;
                    foreach(var renderer in visual.body.GetComponentsInChildren<Renderer>(true))
                    {
                        if(!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)continue;
                        if(renderer.transform.IsChildOf(visual.insert))continue;
                        if(mode==ParkPhotoPrototype.PhotographyMode.Shutter && !renderer.transform.IsChildOf(visual.dial))continue;
                        var bounds=renderer.localBounds;
                        for(int i=0;i<8;i++)
                        {
                            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            var point=detail.WorldToViewportPoint(renderer.transform.TransformPoint(corner));
                            projectedMin=Vector2.Min(projectedMin,point);projectedMax=Vector2.Max(projectedMax,point);
                            fits&=point.x>.01f && point.x<.99f && point.y>.01f && point.y<.99f && point.z>detail.nearClipPlane && point.z<detail.farClipPlane;
                        }
                    }
                    check(fits,"Relevant geometry fits inside detail frame: "+mode);
                    var screenRect=(Rect)Call(p,"CameraDetailRect");
                    float occupiedWidth=(projectedMax.x-projectedMin.x)*screenRect.width/Screen.width;
                    float occupiedHeight=(projectedMax.y-projectedMin.y)*screenRect.height/Screen.height;
                    check(Mathf.Max(occupiedWidth,occupiedHeight)>(mode==ParkPhotoPrototype.PhotographyMode.Shutter?.5f:.75f),"Relevant geometry is large on screen: "+mode);
                    if(mode==ParkPhotoPrototype.PhotographyMode.Shutter)
                    {
                        var centre=detail.WorldToViewportPoint(visual.dial.position);
                        var screenCentre=new Vector2(screenRect.x+centre.x*screenRect.width,screenRect.y+(1-centre.y)*screenRect.height);
                        check(Vector2.Distance(screenCentre,new Vector2(Screen.width,Screen.height)*.5f)<2,"Shutter dial is centred on screen");
                    }
                    lines.Add("Measured model screen bounds: "+occupiedWidth.ToString("P1")+" width, "+occupiedHeight.ToString("P1")+" height ("+Screen.width+"x"+Screen.height+")");
                    var rt=(RenderTexture)Get(p,"dialDetailImage");var previous=RenderTexture.active;Texture2D image=null;
                    try
                    {
                        RenderTexture.active=rt;image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(path,mode+".png"),image.EncodeToPNG());
                    }
                    finally{RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
                    check(p.view.transform.position==startPosition && p.view.transform.rotation==startRotation,"Detail preview preserves gameplay camera: "+mode);
                }
                Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                check(((Transform)Get(p,"bodyRoot")).localPosition==p.cameraOperationOffset,"Return restores operation-model position");
                check(!((Camera)Get(p,"dialDetailCamera")).gameObject.activeSelf,"Leaving detail disables preview camera and light");
                string report=string.Join("\n",lines);File.WriteAllText(Path.Combine(path,"checks.txt"),report);return report;
            }
            finally
            {
                if(p!=null)Call(p,"DisposeCameraDetail");EditorSceneManager.ClosePreviewScene(scene);
                Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
