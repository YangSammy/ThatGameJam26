using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using MemoryWhitebox.Art;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    // DIAGNOSTIC ONLY: baked snapshots are not visual acceptance evidence.
    // The first capture exhibited an apparent duplicate-scale artifact. Explicit
    // BakeMesh(useScale:true) and the raw '*-live-skin.png' comparison have NOT
    // been executed since that finding; wait for natural Edit Mode to compare.
    // Marker/bone checks remain valid independently of this capture path.
    public static class WinterRestorationOffscreen
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(PaintingWorld3D w,string name,object value)=>typeof(PaintingWorld3D).GetField(name,Private).SetValue(w,value);
        static T Get<T>(PaintingWorld3D w,string name)=>(T)typeof(PaintingWorld3D).GetField(name,Private).GetValue(w);
        static object Call(PaintingWorld3D w,string name,params object[] args)=>typeof(PaintingWorld3D).GetMethod(name,Private).Invoke(w,args);
        static void Layer(Transform root){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;}
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static AlbumWinterVisual Prop(GameObject prefab,Transform parent,Vector3 at)
        {
            var t=UnityEngine.Object.Instantiate(prefab,parent,false).transform;t.localPosition=at;t.localScale=Vector3.one*10;
            foreach(var collider in t.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            return t.GetComponent<AlbumWinterVisual>();
        }
        static void Save(RenderTexture rt,string path)
        {
            var before=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{RenderTexture.active=before;UnityEngine.Object.DestroyImmediate(texture);}
        }
        static void Render(Camera camera,Transform root)
        {
            // Fresh skin snapshots avoid stale same-frame Editor GPU skinning.
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
            var copies=new List<GameObject>();var meshes=new List<Mesh>();
            try
            {
                foreach(var sk in skins)
                {
                    var mesh=new Mesh();sk.BakeMesh(mesh,true);meshes.Add(mesh);
                    var go=new GameObject("Offscreen skin");go.transform.SetParent(sk.transform,false);go.layer=27;
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=sk.sharedMaterials;
                    copies.Add(go);sk.enabled=false;
                }
                camera.Render();
            }
            finally
            {foreach(var go in copies)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var sk in skins)sk.enabled=true;}
        }
        public static string Capture()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Offscreen capture is Edit Mode only.");
            const string folder="output/QA/AutumnWinterRestore_20261105/winter-offscreen";
            var previousTarget=RenderTexture.active;
            Directory.CreateDirectory(folder);var preview=EditorSceneManager.NewPreviewScene();
            PaintingWorld3D w=null;var resources=new List<UnityEngine.Object>();
            try
            {
                var s=new PaintingChapter();w=new PaintingWorld3D(null,s);SceneManager.MoveGameObjectToScene(w.Root,preview);w.Root.transform.position=Vector3.zero;
                var route=Child(w.Root.transform,"Offscreen route");Set(w,"route",route);Call(w,"BuildRoute");
                var a=PaintingVisualAssets.Load();var actor=Get<Transform>(w,"actor");var av=Get<AlbumWinterVisual>(w,"albumActor");
                var swing=Prop(a.swing,route,new Vector3(4.5f,0,0));
                var door=Prop(a.door,route,new Vector3(14.7f,3.2f,0));Set(w,"winterDoor",door);
                var key=door.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="KeyVisual");Set(w,"doorKeyVisual",key);
                var wheel=Prop(a.flower,route,new Vector3(8,0,0));wheel.SetFlowerAngleRadians(0);
                var load=Prop(a.counterweight,route,new Vector3(4,0,0));load.SetLoadProgress(1);
                var crate=UnityEngine.Object.Instantiate(a.suitcase,route,false);crate.transform.localPosition=new Vector3(4,0,0);crate.transform.localScale=Vector3.one*1.4f;
                var camera=Child(w.Root.transform,"Offscreen camera").gameObject.AddComponent<Camera>();camera.enabled=false;camera.scene=preview;
                camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.cullingMask=1<<27;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.89f,.87f,.8f);camera.allowHDR=false;
                var light=Child(w.Root.transform,"Offscreen key light").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.cullingMask=1<<27;light.transform.rotation=Quaternion.Euler(35,-25,0);
                var fill=Child(w.Root.transform,"Offscreen fill light").gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.6f;fill.cullingMask=1<<27;fill.transform.rotation=Quaternion.Euler(25,145,0);
                Layer(route);
                var rt=new RenderTexture(960,540,24);rt.Create();resources.Add(rt);camera.targetTexture=rt;
                Action<string,Vector3,float> shot=(name,target,size)=>{camera.aspect=(float)rt.width/rt.height;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(target-Vector3.forward*16,Quaternion.identity);Render(camera,route);Save(rt,folder+"/"+name+".png");};
                s.RestoreCheckpoint("{\"version\":5,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":4,\"high\":true}");s.Actor=new Vector2(14.4f,3.2f);s.SetBook(true);
                swing.gameObject.SetActive(false);w.Nodes["ground-mid"].SetActive(false);w.Nodes["ground-end"].SetActive(false);
                w.Nodes["ground-a"].transform.localPosition=new Vector3(3,-.15f,0);w.Nodes["ground-a"].transform.localScale=new Vector3(6,.3f,1);
                Get<Transform>(w,"key").gameObject.SetActive(false);Call(w,"PlaceAlbumActor",true);
                shot("01-second-route",new Vector3(8,2,0),5.2f);
                camera.Render();Save(rt,folder+"/01-second-route-live-skin.png");
                s.KeyInserted=true;Set(w,"doorPerformanceTime",.65f);Call(w,"ApplyDoorPerformance");av.SetActorPose("KeyReach",1,-90);Call(w,"PlaceAlbumActor",true);
                var hand=Get<Transform>(w,"actorHandContact");var target=key.TransformPoint(new Vector3(0,.02f,0));
                float handGap=Vector3.Distance(hand.position,target);
                shot("02-key-contact",new Vector3(14.65f,4.15f,0),1.25f);
                camera.Render();Save(rt,folder+"/02-key-contact-live-skin.png");
                // Seat sample uses the production root placement, not a hand-authored offset.
                foreach(Transform t in route)t.gameObject.SetActive(t==actor || t==swing.transform);
                s.RestoreCheckpoint("{\"version\":5,\"phase\":0,\"shapes\":[true,true,false,false,false]}");s.Riding=0;s.SwingAngle=.45f;s.Actor=s.SwingSeat;
                Set(w,"doorPerformanceTime",-1f);swing.SetSwingAngleRadians(s.SwingAngle);av.SetActorPose("SitSwing",.5f,-90);Call(w,"PlaceAlbumActor",false);
                var seat=Get<Transform>(w,"actorSeatContact");var targetSeat=swing.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="SwingSeatAnchor");
                float seatGap=Vector2.Distance(seat.position,targetSeat.position);
                shot("03-swing-seat",targetSeat.position+Vector3.up*.3f,1.2f);
                foreach(Transform t in route)t.gameObject.SetActive(t==actor);
                Call(w,"BindWard");var line=Get<Transform>(w,"straightRoute");line.gameObject.SetActive(true);line.localScale=new Vector3(24,.025f,.8f);
                actor.localPosition=new Vector3(6,.02f,0);actor.localScale=Vector3.one*.25f;av.SetActorPose("Walk",.25f,-90);
                var ink=new RenderTexture(1024,128,24);ink.Create();resources.Add(ink);camera.targetTexture=ink;camera.aspect=8;
                camera.orthographicSize=s.WardWalkDistance/14.4f;camera.backgroundColor=Color.clear;camera.transform.SetPositionAndRotation(new Vector3(s.WardWalkDistance*.5f,.4f,-16),Quaternion.identity);
                Render(camera,route);Save(ink,folder+"/04-ward-live-ink.png");
                var material=new Material(Resources.Load<Material>("WardRuntimeReveal"));resources.Add(material);WardImageReveal.ConfigureOrderedMaterial(material);
                material.SetFloat("_Progress",1);material.SetFloat("_SceneBlend",1);material.SetFloat("_UsePageInk",1);material.SetFloat("_ViewportAspect",960f/540);
                material.SetTexture("_PageTex",ink);material.SetTexture("_SceneTex",Texture2D.blackTexture);
                Graphics.Blit(material.mainTexture,rt,material);Save(rt,folder+"/05-ward-final-paper.png");
                actor.localScale=Vector3.one;actor.localPosition=new Vector3(8,.02f,0);av.SetActorPose("Idle",0,-90);camera.targetTexture=rt;camera.backgroundColor=new Color(.89f,.87f,.8f);
                shot("06-final-keepsake",new Vector3(8,2,0),5.2f);
                string report="DIAGNOSTIC ONLY: baked screenshots do not establish visual acceptance. Raw skin comparison must be inspected after an Edit Mode run. Production E02/root placement and authored mechanism coordinates; controlled preview lighting. Seat XY gap="+seatGap.ToString("F4")+"; key hand gap="+handGap.ToString("F4")+". Ward image uses actual WardRuntimeReveal material and production ink camera scale/aspect. Six offscreen PNGs: "+Path.GetFullPath(folder);
                File.WriteAllText(folder+"/capture.txt",report);return report;
            }
            finally
            {
                RenderTexture.active=previousTarget;
                if(w!=null)w.Dispose();foreach(var resource in resources){if(resource is RenderTexture texture)texture.Release();UnityEngine.Object.DestroyImmediate(resource);}
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
