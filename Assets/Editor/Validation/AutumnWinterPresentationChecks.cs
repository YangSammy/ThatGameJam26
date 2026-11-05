using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class AutumnWinterPresentationChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static WBInput Keys(params KeyCode[] codes){var input=new WBInput();foreach(var key in codes)input.Keys.Add(key);return input;}
        static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Private).GetValue(obj);
        static void RestoreVisual(WritingWorld3D w)=>typeof(WritingWorld3D).GetMethod("UpdateWritingVisuals",Private).Invoke(w,new object[]{0f,true});
        static void Write(WritingChapter s){s.PointerDown(new Vector2(700,200),0);s.PointerMove(new Vector2(725,200));}

        // Normal chapter input/state path in the active world. Placement is a test fixture,
        // not a claim that the full timed chapter was completed through native keys.
        public static string RunAutumn()
        {
            var w=WBMain.Instance?.CurrentWorld as WritingWorld3D;
            Require(Application.isPlaying && w!=null,"Enter the actual autumn chapter before testing.");
            var s=w.State;string saved=s.CaptureCheckpoint();
            var result=new List<string>();
            try
            {
                s.RestoreCheckpoint("{\"version\":2,\"draft\":2,\"phase\":\"collect\",\"remaining\":60,\"player\":{\"x\":175,\"y\":275},\"rows\":[0]}");
                w.Tick(0,Keys());RestoreVisual(w);
                var palm=Field<Transform>(w,"reachPalm");var home=palm.position;
                for(int i=0;i<4;i++)w.Tick(.05f,Keys(KeyCode.E));
                Require(s.Collected==0 && Vector3.Distance(home,palm.position)>.12f,"Stopped-pen E must reach visibly without collecting.");
                result.Add("PASS E prepares reach without collecting");
                w.CancelPointerGesture();Require(Field<float>(w,"reachWeight")==0 && s.Collected==0,"Cancel must clear preparation only.");
                result.Add("PASS cancel releases preparation");
                var held=Field<Transform[]>(w,"inventoryProps");
                foreach(int item in new[]{0,2,1,3})
                {
                    s.Player=s.ItemPositions[item];Write(s);w.Tick(.02f,Keys(KeyCode.E));s.CancelInput();
                    Require(s.HasItem(item),"Writing + E failed to collect "+item);
                    for(int i=0;i<8;i++)w.Tick(.1f,Keys());
                }
                Require(s.Collected==15 && !held[0].gameObject.activeSelf && held.Skip(1).All(t=>t.gameObject.activeSelf),"Carry visibility incorrect.");
                Require(held[2].parent!=held[1].parent,"Luggage must follow the ground, not the player's inventory row.");
                result.Add("PASS four items collect in arbitrary order; pendant tucked, other items distinct");
                var previous=held[2].position;w.Tick(.1f,Keys(KeyCode.W));
                Require(Vector3.Distance(previous,held[2].position)>.01f,"Luggage did not follow movement.");
                result.Add("PASS luggage follows movement");
                string carry=s.CaptureCheckpoint();w.CancelPointerGesture();s.RestoreCheckpoint(carry);w.Tick(0,Keys());RestoreVisual(w);
                Require(s.Collected==15 && !held[0].gameObject.activeSelf && held[2].gameObject.activeSelf,"Restore lost carried items.");
                result.Add("PASS existing checkpoint restores carried presentation without replay");
                var girl=Field<AutumnFarewellVisual>(w,"farewellVisual");
                Require(girl.GetComponentsInChildren<SkinnedMeshRenderer>().Length>0 && girl.hair.Length>0,"Formal skin/hair missing.");
                var hand=girl.GetComponentsInChildren<Transform>().Single(t=>t.name=="Left_Hand");
                var first=hand.position;
                s.Phase="farewell";for(int i=0;i<8;i++)w.Tick(.1f,Keys());
                Require(Vector3.Distance(first,hand.position)>.12f,"Source farewell wave was not sampled.");
                result.Add("PASS source rig wave and weighted hair are active");
                return string.Join("\n",result);
            }
            finally{s.RestoreCheckpoint(saved);w.CancelPointerGesture();w.Tick(0,Keys());RestoreVisual(w);}
        }

        public static string InspectWinter()
        {
            var w=WBMain.Instance?.CurrentWorld as PaintingWorld3D;
            Require(Application.isPlaying && w!=null,"Enter the actual winter chapter before testing.");
            var layout=Field<WinterAlbumWorkspace>(w,"layout");
            Require(layout.realtimeObservation,"Realtime observation binding missing.");
            Require(!layout.parkObservation.Find("Scene_Observation").gameObject.activeSelf && !layout.roomObservation.Find("Scene_Observation").gameObject.activeSelf,"Baked panorama is still visible.");
            Require(layout.parkObservation.Find("Realtime_Observation").GetComponentsInChildren<MeshRenderer>(true).Length>0,"Park geometry missing.");
            Require(layout.roomObservation.Find("Realtime_Observation").GetComponentsInChildren<MeshRenderer>(true).Length>0,"Room geometry missing.");
            int hits=0;
            foreach(var mesh in layout.observedBridge.GetComponentsInChildren<MeshCollider>())
            {
                var ray=new Ray(w.View.transform.position,mesh.bounds.center-w.View.transform.position);
                if(w.Pick(ray)=="shape-bridge")hits++;
            }
            Require(hits>0 && !layout.bridgeFocus.enabled,"Bridge needs real mesh hits with proxy disabled.");
            Require(layout.observationCarrier.GetComponentsInChildren<MonoBehaviour>(true).All(c=>!(c is ParkCompanion) && !(c is ParkPhotoPrototype) && !(c is WBMain)),"Geometry copy leaked gameplay scripts.");
            return "PASS live park/room geometry; no active panorama; real bridge mesh ray hits="+hits+"; no duplicate gameplay controllers.";
        }
    }
}
