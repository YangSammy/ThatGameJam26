using System;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // In-memory only: never opens a scene, loads assets, or reads/writes player saves.
    public static class ParkInstrumentPresentationChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool pass,string reason){if(!pass)throw new InvalidOperationException(reason);}
        public static string Run()
        {
            var go=new GameObject("Isolated instrument policy check");go.SetActive(false);
            var p=go.AddComponent<ParkPhotoPrototype>();
            try
            {
                var type=typeof(ParkPhotoPrototype);
                var rotation=type.GetMethod("InstrumentPresentationRotation",BindingFlags.Static|BindingFlags.NonPublic);
                Require(rotation!=null,"Missing rear-facing presentation policy");
                var aperture=(Quaternion)rotation.Invoke(null,new object[]{ParkPhotoPrototype.PhotographyMode.Aperture});
                Require(Vector3.Dot(aperture*Vector3.forward,Vector3.forward)>.9f,"Aperture turned the camera front toward the player");
                Require(Quaternion.Angle(Quaternion.identity,aperture)>=15 && Quaternion.Angle(Quaternion.identity,aperture)<=25,"Aperture should expose the side ring with a small tilt");
                var shutter=(Quaternion)rotation.Invoke(null,new object[]{ParkPhotoPrototype.PhotographyMode.Shutter});
                Require((shutter*Vector3.forward).y<-.1f,"Shutter pose must expose the top dial by tilting down");
                var scroll=type.GetMethod("StepInstrumentControl",Hidden);
                var mode=type.GetField("photoMode",Hidden);var ai=type.GetField("apertureIndex",Hidden);
                mode.SetValue(p,ParkPhotoPrototype.PhotographyMode.Aperture);ai.SetValue(p,0);
                Require(!(bool)scroll.Invoke(p,new object[]{-1}),"Minimum stop falsely reports a click");
                Require((bool)scroll.Invoke(p,new object[]{1}) && (int)ai.GetValue(p)==1,"Aperture did not advance exactly one stop");
                ai.SetValue(p,5);Require(!(bool)scroll.Invoke(p,new object[]{1}),"Maximum stop falsely reports a click");
                var meter=type.GetField("meterIsoIndex",Hidden);meter.SetValue(p,3);
                var sync=type.GetMethod("SyncMeterAfterLoadingStep",Hidden);
                sync.Invoke(p,new object[]{1,200});Require((int)meter.GetValue(p)==3,"Opening the back reset manual meter ISO");
                sync.Invoke(p,new object[]{2,400});Require((int)meter.GetValue(p)==2,"Inserted film did not initialize meter ISO");
                meter.SetValue(p,0);sync.Invoke(p,new object[]{4,400});Require((int)meter.GetValue(p)==0,"Advancing film reset manual meter ISO");
                Require((int)ai.GetValue(p)==5,"Meter changed camera aperture");
                return "PASS rear-facing aperture / down-tilted shutter / per-stop clamp / insertion-only meter ISO (no saves)";
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
