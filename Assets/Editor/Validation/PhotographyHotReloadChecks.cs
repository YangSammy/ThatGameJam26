using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Run in an isolated Park Play session. Prepare, request script compilation,
    // then Check after Unity's actual domain reload; no simulated cache clearing.
    public static class PhotographyHotReloadChecks
    {
        const string Key="PhotographyHotReloadChecks.Before";
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        [Serializable] sealed class Snapshot
        {
            public string film,mode;public int step,iso,parts,root,camera,meter;
            public float aperture,shutter,focus;public Vector3 position;
        }
        static ParkPhotoPrototype Player()
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay || p==null)throw new InvalidOperationException("Isolated Park Play required");
            return p;
        }
        public static string Prepare(string mode,int step,bool drag)
        {
            var p=Player();p.Resume();
            var roll=ParkFilmStore.Load(p.CurrentFilmId);
            roll=ParkFilmStore.SaveCameraLoading(roll.id,ParkCameraLoading.AtStep(step,400));
            Set(p,"roll",roll);Set(p,"loadingStep",step);
            Set(p,"apertureIndex",4);Set(p,"speedIndex",7);Set(p,"meterIsoIndex",2);p.focusDistance=3.7f;
            Call(p,"SetPhotographyMode",Enum.Parse(typeof(ParkPhotoPrototype.PhotographyMode),mode));
            Call(p,"SyncInstruments");
            if(drag)
            {
                var parts=(Dictionary<string,Transform>)Get(p,"instrumentParts");
                var target=parts[step==1?"film-400":"leader"];
                Physics.SyncTransforms();Call(p,"BeginBodyPointer",new Ray(p.view.transform.position,target.position-p.view.transform.position));
                if(Get(p,"dragPart")==null)throw new InvalidOperationException("Fixture missed drag target");
            }
            var s=new Snapshot{film=p.CurrentFilmId,mode=mode,step=step,iso=p.FilmIso,aperture=p.Aperture,shutter=p.ShutterSeconds,focus=p.focusDistance,
                position=p.transform.position,parts=((Dictionary<string,Transform>)Get(p,"instrumentParts")).Count,
                root=((Transform)Get(p,"instruments")).GetInstanceID(),camera=((PhotographyCameraVisual)Get(p,"instrumentCamera")).GetInstanceID(),meter=((PhotographyMeterDisplay)Get(p,"instrumentMeter")).GetInstanceID()};
            SessionState.SetString(Key,JsonUtility.ToJson(s));return JsonUtility.ToJson(s);
        }
        public static string Check()
        {
            var p=Player();var s=JsonUtility.FromJson<Snapshot>(SessionState.GetString(Key,""));var lines=new List<string>();
            Action<bool,string> check=(ok,text)=>lines.Add((ok?"PASS ":"FAIL ")+text);
            var parts=(Dictionary<string,Transform>)Get(p,"instrumentParts");var homes=(Dictionary<string,Vector3>)Get(p,"partHomes");
            check(parts.Count==s.parts && homes.Count==s.parts,"All instrument hit targets and drag homes survive ("+parts.Count+"/"+s.parts+")");
            check(((Transform)Get(p,"instruments")).GetInstanceID()==s.root && ((PhotographyCameraVisual)Get(p,"instrumentCamera")).GetInstanceID()==s.camera && ((PhotographyMeterDisplay)Get(p,"instrumentMeter")).GetInstanceID()==s.meter,"Original model instances retained");
            check(p.CurrentFilmId==s.film && (int)Get(p,"loadingStep")==s.step && p.FilmIso==s.iso,"Film identity, ISO and stable loading step retained");
            check(p.Aperture==s.aperture && p.ShutterSeconds==s.shutter && p.focusDistance==s.focus,"Aperture, shutter and focus retained");
            check(p.PhotoMode.ToString()==s.mode,"Operation mode retained");
            check(Vector3.Distance(p.transform.position,s.position)<.03f,"Player location retained");
            check(Get(p,"dragPart")==null,"Interrupted gesture cancelled");
            try{Call(p,"SyncInstruments");check(true,"Camera and meter synchronise without exception");}
            catch(Exception ex){check(false,"Synchronisation: "+(ex.InnerException??ex).Message);}
            var roll=ParkFilmStore.Load(s.film);check(roll.cameraLoading.StableStep==s.step && roll.cameraLoading.iso==s.iso,"Stable loading data reloads from disk");
            return string.Join("\n",lines);
        }
        public static string ExerciseBody()
        {
            var p=Player();Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Body);Call(p,"SyncInstruments");
            var parts=(Dictionary<string,Transform>)Get(p,"instrumentParts");
            Action<string> click=id=>{Physics.SyncTransforms();Call(p,"BeginBodyPointer",new Ray(p.view.transform.position,parts[id].position-p.view.transform.position));Call(p,"SyncInstruments");};
            if((int)Get(p,"loadingStep")==2)
            {
                click("leader");if((string)Get(p,"dragPart")!="leader")throw new InvalidOperationException("Cannot grab leader");
                var ray=new Ray(p.view.transform.position,parts["spool"].position-p.view.transform.position);
                Call(p,"MoveBodyPointer",ray);Physics.SyncTransforms();Call(p,"EndBodyPointer",ray);Call(p,"SyncInstruments");
                if((int)Get(p,"loadingStep")!=4)throw new InvalidOperationException("Leader not seated");
                click("advance");click("back");click("advance");
            }
            if((int)Get(p,"loadingStep")!=7)throw new InvalidOperationException("Film not ready");
            click("eyepiece");if(!p.Aiming)throw new InvalidOperationException("Cannot enter viewfinder");
            return "PASS actual collider rays: leader to spool, advance, close, advance, eyepiece";
        }
    }
}
