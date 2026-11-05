using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkCameraRaiseChecks
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var root=new GameObject("QA camera raise");root.SetActive(false);
            try
            {
                var p=root.AddComponent<ParkPhotoPrototype>();p.enabled=false;
                var camera=new GameObject("QA view").AddComponent<Camera>();camera.transform.SetParent(root.transform);p.view=camera;
                var type=p.GetType();Action<string,object> set=(name,value)=>type.GetField(name,Flags).SetValue(p,value);
                Func<string,object[],object> call=(name,args)=>type.GetMethod(name,Flags).Invoke(p,args);
                call("BuildAssetInstruments",null);
                set("roll",new ParkFilmRoll{cameraLoading=ParkCameraLoading.AtStep(7,400)});set("loadingStep",7);
                call("SetPhotographyMode",new object[]{ParkPhotoPrototype.PhotographyMode.Body});
                var begin=type.GetMethod("BeginCameraRaise",Flags);var tick=type.GetMethod("TickCameraRaise",Flags);
                if(begin==null || tick==null)return "FAIL eyepiece has no animated raise path";
                camera.transform.localRotation=Quaternion.Euler(13,47,0);camera.fieldOfView=65;
                var rotation=camera.transform.rotation;var position=camera.transform.position;
                var body=(Transform)type.GetField("bodyRoot",Flags).GetValue(p);var home=body.localPosition;
                root.SetActive(true);Physics.SyncTransforms();
                var parts=(System.Collections.Generic.Dictionary<string,Transform>)type.GetField("instrumentParts",Flags).GetValue(p);
                call("BeginBodyPointer",new object[]{new Ray(camera.transform.position,parts["eyepiece"].position-camera.transform.position)});
                tick.Invoke(p,new object[]{.25f});
                if(p.Aiming || Vector3.Distance(body.localPosition,home)<.01f)return "FAIL camera must visibly move before entering finder";
                if(p.CameraRaiseGrip==null || p.CameraRaiseSupport==null)return "FAIL both hands need moving camera contact targets";
                if(camera.fieldOfView>=65 || camera.fieldOfView<=PlayerControlSettings.Current.parkAimingFov)return "FAIL FOV must interpolate";
                tick.Invoke(p,new object[]{.3f});
                if(!p.Aiming || p.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Viewfinder)return "FAIL transition must complete in finder";
                if(Quaternion.Angle(rotation,camera.transform.rotation)>.001f || Vector3.Distance(position,camera.transform.position)>.0001f)return "FAIL world view must stay fixed";
                if(Mathf.Abs(camera.fieldOfView-PlayerControlSettings.Current.parkAimingFov)>.001f)return "FAIL final FOV continuity";
                if(Vector3.Distance(body.localPosition,home)>.0001f || body.gameObject.activeSelf)return "FAIL model must hide and reset after arrival";
                if(!(bool)type.GetField("inputGate",Flags).GetValue(p))return "FAIL original click must be release-gated";
                tick.Invoke(p,new object[]{.4f});
                if(p.HandPresentationBlocked)return "FAIL holding original click must never reset settled hands";
                call("SetPhotographyMode",new object[]{ParkPhotoPrototype.PhotographyMode.Body});begin.Invoke(p,null);tick.Invoke(p,new object[]{.2f});
                call("SetPhotographyMode",new object[]{ParkPhotoPrototype.PhotographyMode.Exploration});
                if(Vector3.Distance(body.localPosition,home)>.0001f || (bool)tick.Invoke(p,new object[]{.5f}))return "FAIL cancel must reset animation and never enter finder later";
                set("roll",null);
                return "PASS physical lift, interpolated FOV, view orientation/position, completion, hidden model reset, click release gate, cancellation";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
