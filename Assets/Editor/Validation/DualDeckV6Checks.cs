using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class DualDeckV6Checks
    {
        public static string PointerRun()
        {
            var h=WBMain.Instance;h.enabled=false;h.SendMessage("OnApplicationFocus",true);var d=h.radioDeck;d.enabled=false;d.SendMessage("OnApplicationFocus",true);
            d.SetStationActive(true);d.ReleaseInputGate(true);d.view.transform.SetPositionAndRotation(d.operationView.transform.position,d.operationView.transform.rotation);
            var f=BindingFlags.NonPublic|BindingFlags.Instance;
            Ray At(Collider c){Physics.SyncTransforms();return new Ray(d.view.transform.position,c.bounds.center-d.view.transform.position);}
            var source=d.GetType().GetMethod("SourcePointer",f);
            source.Invoke(d,new object[]{EventType.MouseDown,0,At(d.originalHit)});
            source.Invoke(d,new object[]{EventType.MouseDrag,0,At(d.sourceSlot)});
            source.Invoke(d,new object[]{EventType.MouseUp,0,At(d.sourceSlot)});
            if(!d.Chapter.SourceLoaded)throw new Exception("Source physical drag did not load A");
            for(int i=0;i<100;i++)d.dualDeck.Tick(.04f,false,"EMPTY",false);
            d.Chapter.AuditionComplete=true;d.RestoreSource();
            d.GetType().GetMethod("SelectForDrag",f).Invoke(d,new object[]{2});
            d.GetType().GetMethod("BeginTapeDrag",f).Invoke(d,new object[]{At(d.tapes[2].hit)});
            var ray=At(d.slotHit);d.GetType().GetField("dragRay",f).SetValue(d,ray);d.PoseDraggedTape(.1f);
            var session=(ObjectDragSession)d.GetType().GetField("tapeDrag",f).GetValue(d);
            bool can=(bool)d.GetType().GetMethod("TapeReceiver",f).Invoke(d,new object[]{d.slotHit,ray});
            if(session.Blocked||!can)throw new Exception("B physical drop blocked: sweep="+session.Blocked+" receiver="+can);
            d.GetType().GetMethod("EndTapeDrag",f).Invoke(d,new object[]{true});
            for(int i=0;i<100;i++)d.dualDeck.Tick(.04f,false,"EMPTY",false);
            if(d.Selected!=2||d.State!=HouseRadioDeck.Phase.Paused)throw new Exception("B did not complete loading");
            return "POINTER_LOAD_A_B_PASS";
        }
        public static string Run()=>TapeToggleStorageChecks.Transport();
    }
}
