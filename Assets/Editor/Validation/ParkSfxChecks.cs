using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkSfxChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static object Call(object target,string name,params object[] values)=>target.GetType().GetMethod(name,Hidden).Invoke(target,values);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Hidden).SetValue(target,value);
        public static string Run()
        {
            var go=new GameObject("Isolated Park SFX checks");go.SetActive(false);var p=go.AddComponent<ParkPhotoPrototype>();
            try
            {
                var cue=typeof(ParkPhotoPrototype).GetMethod("LoadingSfxCue",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(var row in new[]{new[]{1,23},new[]{2,22},new[]{4,24},new[]{5,25},new[]{6,123},new[]{7,25},new[]{0,-1}})
                    Require((int)cue.Invoke(null,new object[]{row[0]})==row[1],"Incorrect mechanical transition cue at "+row[0]);
                Set(p,"photoMode",ParkPhotoPrototype.PhotographyMode.Meter);
                Require(!(bool)Call(p,"MeterAdviceBecameReady",.1f),"Meter advice played before settling");
                Require((bool)Call(p,"MeterAdviceBecameReady",.25f),"Settled meter did not acquire advice");
                Require(!(bool)Call(p,"MeterAdviceBecameReady",1f),"Meter advice repeats each frame");
                Set(p,"meterIsoIndex",2);
                Require(!(bool)Call(p,"MeterAdviceBecameReady",.1f),"ISO change bypassed settle");
                Require((bool)Call(p,"MeterAdviceBecameReady",.25f),"ISO change failed to acquire new advice");
                Set(p,"photoMode",ParkPhotoPrototype.PhotographyMode.Exploration);Call(p,"MeterAdviceBecameReady",1f);
                Set(p,"photoMode",ParkPhotoPrototype.PhotographyMode.Meter);
                Require(!(bool)Call(p,"MeterAdviceBecameReady",.1f),"Re-raising meter bypassed settle");
                p.focusDistance=p.maximumFocus;
                Require(!(bool)Call(p,"StepFocusControl",1f),"Clamped focus reports an audible detent");
                Require((bool)Call(p,"StepFocusControl",-1f),"Actual focus movement did not report a detent");
                Set(p,"opticalShutterDeadline",10.55f);
                Require((bool)Call(p,"ArmAutomaticShutterTail",10.1f),"Automatic final panel truncated an active shutter tail");
                Require((bool)Call(p,"AutomaticShutterTailAlive",10.54f),"Automatic panel tail ended before cue deadline");
                Require(!(bool)Call(p,"AutomaticShutterTailAlive",10.55f),"Automatic panel extended shutter past cue deadline");
                Set(p,"opticalShutterDeadline",20.55f);Call(p,"ArmAutomaticShutterTail",20.1f);
                Call(p,"StopPhotographySfx");
                Require(!(bool)Call(p,"AutomaticShutterTailAlive",20.2f),"Manual interruption retained an automatic shutter tail");
                Require(!(bool)Call(p,"ArmAutomaticShutterTail",20.2f),"A later panel replayed an interrupted shutter");
                Set(p,"opticalShutterDeadline",30.55f);
                Require(!(bool)Call(p,"AutomaticShutterTailAlive",30.2f),"An ordinary pause implicitly preserved a shutter tail");
                Require(!(bool)Call(p,"ArmAutomaticShutterTail",31f),"Panel revived an already-finished shutter");
                Require(go.GetComponents<TapeSfxPlayer>().Length==0,"In-memory checks created a playback owner");
                return "PASS Park loading cue map, meter settle/ISO rearm/no repeats, focus clamp, automatic shutter deadline/manual interruption (no audio, assets, scenes or saves)";
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
