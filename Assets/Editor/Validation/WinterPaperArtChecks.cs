using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MemoryWhitebox.Art;

namespace MemoryWhitebox.Editor
{
    public static class WinterPaperArtChecks
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(PaintingWorld3D w,string key,object v)=>typeof(PaintingWorld3D).GetField(key,Flags).SetValue(w,v);
        static T Get<T>(PaintingWorld3D w,string key)=>(T)typeof(PaintingWorld3D).GetField(key,Flags).GetValue(w);
        static void Call(PaintingWorld3D w,string name)=>typeof(PaintingWorld3D).GetMethod(name,Flags).Invoke(w,null);
        static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static AlbumWinterVisual Prop(GameObject prefab,Transform parent,Vector3 p)
        {
            var go=UnityEngine.Object.Instantiate(prefab,parent,false);go.transform.localPosition=p;go.transform.localScale=Vector3.one*10;
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
            return go.GetComponent<AlbumWinterVisual>();
        }
        public static string Run()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit-mode preview only");
            const string folder="output/QA/WinterPaperArt_20261107";Directory.CreateDirectory(folder);
            var scene=EditorSceneManager.NewPreviewScene();PaintingWorld3D w=null;RenderTexture rt=null;Texture2D shot=null;
            var previous=RenderTexture.active;
            try
            {
                var state=new PaintingChapter();w=new PaintingWorld3D(null,state);
                SceneManager.MoveGameObjectToScene(w.Root,scene);w.Root.transform.position=Vector3.zero;
                var route=Group(w.Root.transform,"Route");Set(w,"route",route);Call(w,"BuildRoute");
                var a=PaintingVisualAssets.Load();var layout=Group(w.Root.transform,"Layout").gameObject.AddComponent<WinterAlbumWorkspace>();layout.routeOrigin=route;
                layout.swing=Prop(a.swing,route,new Vector3(4.5f,0,0));Set(w,"winterSwing",layout.swing);
                layout.bridge=Prop(a.bridge,route,new Vector3(10.5f,.7f,0));Set(w,"winterBridge",layout.bridge);
                layout.counterweight=Prop(a.counterweight,route,new Vector3(4,0,0));Set(w,"winterLoad",layout.counterweight);
                layout.wheel=Prop(a.flower,route,new Vector3(8,0,0));Set(w,"winterFlower",layout.wheel);
                layout.door=Prop(a.door,route,new Vector3(14.7f,3.2f,0));Set(w,"winterDoor",layout.door);
                layout.keySocket=Group(route,"Key socket");layout.keySocket.localPosition=new Vector3(14.7f,4.2f,0);
                Set(w,"layout",layout);
                var crate=UnityEngine.Object.Instantiate(a.suitcase,route,false).transform;Set(w,"winterCrate",crate);
                foreach(var t in route.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
                var cam=Group(w.Root.transform,"Camera").gameObject.AddComponent<Camera>();cam.enabled=false;cam.scene=scene;
                cam.orthographic=true;cam.orthographicSize=5.2f;cam.aspect=1.8f;cam.transform.position=new Vector3(8,2,-16);cam.cullingMask=1<<27;
                rt=new RenderTexture(1280,720,24);rt.Create();cam.targetTexture=rt;shot=new Texture2D(1280,720,TextureFormat.RGBA32,false);
                Action<string> capture=name=>{
                    layout.door.gameObject.SetActive(state.Phase==1);
                    layout.door.SetDoorState(0,0,0,false);
                    string before=state.CaptureCheckpoint();Call(w,"RefreshPaperArt");
                    if(before!=state.CaptureCheckpoint())throw new Exception("Art mutated chapter state");
                    cam.Render();RenderTexture.active=rt;shot.ReadPixels(new Rect(0,0,1280,720),0,0);shot.Apply();File.WriteAllBytes(folder+"/"+name+".png",shot.EncodeToPNG());
                };
                state.RestoreCheckpoint("{\"version\":5,\"phase\":0,\"shapes\":[true,true,false,false,false]}");
                state.BridgeGrowth=1;state.Actor=new Vector2(.7f,0);capture("01-outdoor");
                state.Riding=0;state.Actor=state.SwingSeat;capture("02-swing-contact");
                var traveler=Get<SpriteRenderer>(w,"travelerArt");
                var seat=Get<Transform>(w,"swingSeatArt");
                var foot=route.InverseTransformPoint(traveler.transform.position);
                // Sitting legs hang .18 below the model contact point; the painted
                // plank center lies .06 below its top contact surface.
                if(Vector2.Distance(new Vector2(foot.x,foot.y+.18f),new Vector2(seat.localPosition.x,seat.localPosition.y+.06f))>.001f)
                    throw new Exception("Mapped seated actor does not track swing seat");
                foreach(string layer in new[]{"Background","SnowPlatforms","RiverRocks","Foreground","Bridge","SwingFrame"})
                {
                    var texture=Resources.Load<Texture2D>("WinterPaperArt/Layers/"+layer);
                    if(texture==null)throw new Exception("Missing layer "+layer);
                    if(layer!="Background")
                    {
                        bool clear=false,solid=false;
                        foreach(var pixel in texture.GetPixels32()){clear|=pixel.a==0;solid|=pixel.a>200;if(clear&&solid)break;}
                        if(!clear||!solid)throw new Exception("Layer has no usable transparent silhouette: "+layer);
                    }
                }
                state.Riding=-1;state.Actor=new Vector2(10.5f,.7f);state.BridgeGrowth=.5f;capture("07-half-bridge");
                if(Mathf.Abs(Get<Material>(w,"outdoorBridgeMaterial").GetFloat("_Reveal")-.5f)>.001f)
                    throw new Exception("Bridge reveal does not track growth");
                state.BridgeGrowth=1;capture("08-bridge-contact");
                state.RestoreCheckpoint("{\"version\":5,\"phase\":1,\"shapes\":[true,true,true,true,true],\"crate\":4}");
                state.Actor=new Vector2(3.4f,0);state.FlowerAngle=.4f;capture("03-indoor");
                var frames=Get<Sprite[]>(w,"travelerFrames");if(frames.Length!=24)throw new Exception("Frame count");
                foreach(var f in frames)if(Mathf.Abs(f.bounds.min.y)>.001f)throw new Exception("Unaligned sprite feet");
                state.Grounded=false;state.Velocity=new Vector2(1,3);state.Actor=new Vector2(5,1);capture("04-jump");
                var sr=Get<SpriteRenderer>(w,"travelerArt");if(sr.sprite!=frames[14])throw new Exception("Wrong right jump frame");
                state.Actor=new Vector2(4.9f,1);capture("05-jump-left");
                if(sr.sprite!=frames[20])throw new Exception("Wrong left jump frame");
                state.Grounded=true;state.Riding=1;state.Actor=new Vector2(5.1f,1);Call(w,"RefreshPaperArt");
                if(sr.sprite!=frames[6])throw new Exception("Flower ride changed facing or walked");
                state.Riding=-1;state.Actor=new Vector2(.7f,0);Call(w,"RefreshPaperArt");
                if(sr.sprite!=frames[6])throw new Exception("Respawn teleport changed facing or walked");
                state.RestoreCheckpoint("{\"version\":5,\"phase\":0,\"shapes\":[false,false,false,false,false]}");capture("06-before-discovery");
                if(Get<Transform>(w,"swingArt").gameObject.activeSelf||Get<Transform>(w,"bridgeArt").gameObject.activeSelf)throw new Exception("Unacquired props visible");
                Call(w,"RestorePaperArt");
                if(!Get<AlbumWinterVisual>(w,"albumActor").GetComponentInChildren<SkinnedMeshRenderer>(true).enabled)throw new Exception("Original actor not restored");
                string report="PASS: eight isolated renders; six painted layers with usable alpha silhouettes; mapped swing sitting contact; bridge reveal tracks growth; presentation preserves checkpoints; 24 foot-aligned sprites; right/left jump selection; stable flower riding and respawn poses; unacquired mechanism visibility; renderer restoration. No live playthrough or player build.";
                File.WriteAllText(folder+"/checks.txt",report);return report;
            }
            finally
            {
                RenderTexture.active=previous;if(w!=null)w.Dispose();
                if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(shot!=null)UnityEngine.Object.DestroyImmediate(shot);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
