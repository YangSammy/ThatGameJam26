using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MemoryWhitebox.Art;
namespace MemoryWhitebox.EditorTools
{
    // Play-mode integration checks use production pointer handlers and movement, not OS mouse injection.
    public static class AlbumWinterIntegrationValidation
    {
        static PaintingWorld3D W => WBMain.Instance.CurrentWorld as PaintingWorld3D;
        const string Report="output/E_AlbumWinter_Integration_v1/runtime-checks.txt";
        static void Check(bool ok,string text){System.IO.File.AppendAllText(Report,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
        static Transform Find(string name)=>W.Root.GetComponentsInChildren<Transform>(true).Last(x=>x.name==name);
        static AlbumWinterVisual Visual(string part)=>W.Root.GetComponentsInChildren<AlbumWinterVisual>(true).Last(x=>x.name=="Visual_PF_"+part);
        static void Step(WBInput input=null,float dt=.02f){W.Tick(dt,input??new WBInput());}
        static WBInput Right(bool jump=false){var i=new WBInput();i.Keys.Add(KeyCode.D);if(jump)i.Keys.Add(KeyCode.Space);return i;}
        static void Edge(bool open){W.Move(W.View.ViewportPointToRay(new Vector3(.5f,.5f)));Step(null,0);W.Move(W.View.ViewportPointToRay(new Vector3(.5f,open?.01f:.99f)));Step(null,0);W.Move(W.View.ViewportPointToRay(new Vector3(.5f,.5f)));}
        static Ray RouteRay(Vector3 point)
        {
            var cam=Find("Album side camera").GetComponent<Camera>();var uv=cam.WorldToViewportPoint(point);
            var page=W.Nodes["album-page"].transform;var target=page.TransformPoint(new Vector3(uv.x-.5f,uv.y-.5f,0));return new Ray(W.View.transform.position,(target-W.View.transform.position).normalized);
        }
        public static string FirstRoute()
        {
            Check(Application.isPlaying && W!=null && W.Active,"Real House winter chapter active");
            WBMain.Instance.SetHelpVisible(false);WBMain.Instance.enabled=false;
            Physics.SyncTransforms();
            foreach(string id in new[]{"shape-swing","shape-bridge"})
            {
                // Find a visible sample within existing colliders; suitcase occludes the central seat.
                Ray chosen=new Ray();bool found=false;
                foreach(var hot in W.Root.GetComponentsInChildren<WorldHotspot>(true).Where(x=>x.Id==id))
                {
                    var c=hot.GetComponent<Collider>();if(c==null)continue;
                    for(int y=0;y<9&&!found;y++)for(int x=0;x<9&&!found;x++)
                    {var p=c.bounds.center+Vector3.Scale(c.bounds.extents,new Vector3((x-4)/4f,(y-4)/4f,0));var ray=new Ray(W.View.transform.position,p-W.View.transform.position);if(W.Pick(ray)==id){chosen=ray;found=true;}}
                }
                Check(found,"Unchanged observation hotspot reachable: "+id);
                W.Move(chosen);W.Press(id,chosen,0);for(int i=0;i<80;i++)Step();W.Release(id,chosen);
            }
            Check(W.State.Painted[0]&&W.State.Painted[1],"Long-hold observation unlocks swing and bridge");
            Edge(true);Check(W.State.BookOpen,"Bottom screen edge opens actual page");
            for(int i=0;i<50;i++)Step();
            var pivot=Find("Album route - physical geometry");
            Check(Vector2.Distance(pivot.InverseTransformPoint(Find("SwingSeatAnchor").position),W.State.SwingSeat)<.002f,"Swing seat matches existing ride position");
            Check(Mathf.Abs(pivot.InverseTransformPoint(Find("ConnectLeft").position).x-9)<.002f,"Bridge left endpoint stays x9");
            Check(Mathf.Abs(pivot.InverseTransformPoint(Find("ConnectRight").position).x-12)<.002f,"Bridge right endpoint stays x12");
            Check(Visual("Bridge_E04").Capture().bridge==W.State.BridgeGrowth,"Bridge reveal reads chapter progress");
            Check(Find("Visual_AlbumWorldAdapter_SourceScale10").GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"New prop colliders remain disabled");
            Check(!W.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("E02")),"No E02 character imported into gameplay");
            bool rode=false;
            for(int i=0;i<1500&&W.State.Phase==0;i++)
            {rode|=W.State.Riding==0;bool jump=W.State.Riding==0&&W.State.SwingAngle>.45f&&W.State.SwingSpeed>0;Step(Right(jump));}
            Check(rode,"Existing movement mounts original swing");
            System.IO.File.AppendAllText(Report,"INFO Actual first-route traversal reached phase "+W.State.Phase+" actor "+W.State.Actor+"\n");
            return "First-route checks passed, phase="+W.State.Phase;
        }
        public static string RestoredSecondRoute()
        {
            // Explicit stable-state fixture, not a claim of full first-to-last gameplay completion.
            W.State.RestoreCheckpoint("{\"version\":2,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":2}");Edge(true);Step(null,0);
            Check(Visual("CounterweightGate_E05").Capture().load==0,"Restored unloaded mechanism");
            for(int i=0;i<150&&W.State.CrateX<3.8f;i++)Step(Right());
            Check(W.State.PlatePressed&&Visual("CounterweightGate_E05").Capture().load==1,"Actual crate pushing loads E05 via original plate rule");
            var route=Find("Album route - physical geometry");
            for(int i=0;i<6;i++)
            {var seat=Find("RideAnchor_"+i);Check(Vector2.Distance(route.InverseTransformPoint(seat.position),W.State.FlowerSeat(i))<.002f,"Flower carrier "+i+" aligns with original ride path");Check(Vector3.Dot(seat.up,Vector3.up)>.999f,"Carrier "+i+" stays level");}
            var angle=W.State.FlowerAngle;for(int i=0;i<30;i++)Step();Check(W.State.FlowerAngle>angle,"Existing flower simulation drives rotation");
            W.State.RestoreCheckpoint("{\"version\":2,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":4,\"high\":true}");Edge(true);for(int i=0;i<75;i++)Step(Right());
            Check(W.State.AtDoor,"Existing movement reaches door from saved high checkpoint");Physics.SyncTransforms();
            var start=RouteRay(Find("album-key").position);Check(W.Pick(start)=="album-key","Real page ray reaches retained key hotspot");
            W.Press("album-key",start,0);Check(W.HasPointerGesture,"Key drag started through production pointer handler");W.Move(RouteRay(Find("key-recess").position));W.CancelPointerGesture();Step(null,0);
            Check(!W.State.KeyInserted&&!W.HasPointerGesture&&Find("album-key").gameObject.activeInHierarchy,"Cancel does not consume key");
            W.Press("album-key",start,0);var end=RouteRay(Find("key-recess").position);W.Move(end);W.Release("key-recess",end);Step(null,0);
            Check(W.State.KeyInserted&&!Find("album-key").gameObject.activeInHierarchy&&Visual("PendantDoor_E07").Capture().keyVisible,"Original drop rule consumes key and shows one embedded substitute");
            Check(WBMain.Instance.SaveStableProgress(),"Existing save system accepts stable winter progress");
            SessionState.SetString("AlbumWinter.Expected",W.State.CaptureCheckpoint());
            return "Stable-state, crate, six carriers, key cancel/drop and save checks passed";
        }
        public static string CheckRestored()
        {
            WBMain.Instance.SetHelpVisible(false);WBMain.Instance.enabled=false;Step(null,0);
            var expected=new PaintingChapter();expected.RestoreCheckpoint(SessionState.GetString("AlbumWinter.Expected",""));
            Check(W.State.CaptureCheckpoint()==expected.CaptureCheckpoint(),"Exit/re-enter restores identical stable checkpoint");
            Check(Visual("PendantDoor_E07").Capture().keyVisible==W.State.KeyInserted,"Door appearance restores from authoritative saved key state");
            Check(W.Root.GetComponentsInChildren<AlbumWinterVisual>(true).Length==8,"Re-entry does not duplicate prop instances");
            Check(Visual("AlbumSheet_E01").Capture().pageTexture is RenderTexture,"Re-entry retains real game RenderTexture");return "Restoration passed";
        }
    }
}

