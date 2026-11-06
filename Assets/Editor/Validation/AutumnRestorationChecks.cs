using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    public static class AutumnRestorationChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Private).GetValue(obj);
        static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
        static WBInput Empty()=>new WBInput();
        static void Write(WritingChapter state){state.PointerDown(new Vector2(700,200),0);state.PointerMove(new Vector2(725,200));}
        static void Restart(WritingChapter state){state.PointerDown(new Vector2(1195,590),0);state.PointerUp(new Vector2(1185,635));}

        public static string RunModel()
        {
            var s=new WritingChapter();s.Tick(90,Empty());
            Require(s.Phase=="tutorial" && s.Remaining==60 && !s.TimedOut,"Tutorial must not time out.");
            s.Phase="crumple";Restart(s);
            s.Tick(90,Empty());Require(!s.Started && s.Remaining==60,"Formal clock started without ink.");
            Write(s);s.CancelInput();s.Tick(1,Empty());
            Require(s.Started && s.Remaining==59,"Stopping ink must not stop formal clock.");
            s.Player=new Vector2(300,565);s.Collected=3;s.Tick(.01f,Empty());
            Require(!s.MissingItemHintUsed,"One-item gesture fired with two objects missing.");
            s.Collected=7;s.Tick(.01f,Empty());
            Require(!s.MissingItemHintUsed && s.Phase=="collect","Retired doorway cue must stay off.");
            var resumed=new WritingChapter();resumed.RestoreCheckpoint(s.CaptureCheckpoint());
            Require(!resumed.MissingItemHintUsed,"Retired cue restored from checkpoint.");
            resumed.Tick(100,Empty());Require(resumed.TimedOut && resumed.Phase=="crumple","Timeout must be distinguishable from tutorial stall.");
            var failed=new WritingChapter();failed.RestoreCheckpoint(resumed.CaptureCheckpoint());
            Require(failed.TimedOut,"Timeout lost on restore.");Restart(failed);
            Require(!failed.MissingItemHintUsed && !failed.TimedOut && failed.Collected==0,"Retry must reset cue/fade/items.");
            failed.RestoreCheckpoint("{\"version\":2,\"draft\":2,\"phase\":\"collect\",\"collected\":15,\"started\":true,\"remaining\":0.01,\"player\":{\"x\":300,\"y\":565},\"rows\":[0]}");
            failed.Tick(.01f,Empty());Require(failed.Phase=="arrival" && !failed.TimedOut,"Arrival tie changed.");
            failed.Tick(120,Empty());Require(failed.Phase=="arrival","Arrival must stop timeout.");
            failed.RestoreCheckpoint("{\"version\":1,\"draft\":2,\"crumple\":true}");
            Require(failed.Phase=="crumple","Legacy v1 load failed.");
            return "PASS tutorial untimed; first-ink start; stopped ink keeps time; doorway cue retired; timeout persists; retry resets; v1/v2 migration; exact deadline arrival and frozen arrival clock.";
        }

        public static string RunPresentation()
        {
            var assets=WritingVisualAssets.Load();
            Require(assets!=null && assets.memoryActor!=null && assets.memoryVase!=null && assets.memoryFadeShader!=null,"Run AutumnRestorationAuthoring.BindAssets first.");
            var state=new WritingChapter{Draft=2,Phase="collect",Started=true};
            WritingWorld3D world=null;
            try
            {
                world=new WritingWorld3D(null,state);
                var vase=Field<Transform>(world,"memoryVase");
                var items=Field<Transform[]>(world,"collectionProps");
                state.Collected=2;Call(world,"UpdateAutumnActors",.1f);
                Require(vase.gameObject.activeInHierarchy && !items[1].gameObject.activeSelf,"Flower pickup hid vase.");
                Require(!world.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("Pickup halo")),"Answer rings remain.");
                var player=Field<Transform>(world,"player");
                Require(player.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled),"Authored player skin missing.");
                state.Player=new Vector2(300,565);state.Collected=7;state.Tick(.01f,Empty());
                Call(world,"UpdateAutumnActors",.1f);
                Require(!state.MissingItemHintUsed,"Retired girl cue fired.");
                Call(world,"RestoreMemoryPresentation");Call(world,"UpdateAutumnActors",.1f);
                state.Remaining=.01f;state.Tick(.02f,Empty());
                for(int i=0;i<15;i++)Call(world,"UpdateAutumnActors",.1f);
                Require(Field<float>(world,"memoryFade")>=.99f,"Timeout memory did not fade fully.");
                Restart(state);Call(world,"UpdateAutumnActors",.1f);
                Require(Field<float>(world,"memoryFade")==0 && vase.gameObject.activeInHierarchy && items[1].gameObject.activeSelf,"Retry did not restore room/flower/vase.");
                return "PASS independent vase; no answer rings; authored skinned player; no missing-item cue and restore; memory timeout fade and retry.";
            }
            finally{if(world!=null)world.Dispose();}
        }

        public static string RunFraming(AutumnWritingDesk desk=null)
        {
            if(desk==null)desk=Object.FindObjectsOfType<AutumnWritingDesk>(true).FirstOrDefault();
            Require(desk!=null,"Load an autumn desk for its actual paper/timer bounds; no scene is opened by this check.");
            WritingWorld3D world=null;GameObject cameraObject=null;
            try
            {
                world=new WritingWorld3D(null,new WritingChapter());
                // Cache only; avoid binding hotspots, creating ink or changing the borrowed desk.
                typeof(WritingWorld3D).GetField("desk",Private).SetValue(world,desk);
                Call(world,"CacheLetterFraming");
                cameraObject=new GameObject("Autumn framing validation camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=48;
                float smallestPaper=1;
                foreach(float fullAspect in new[]{16f/9,20f/9,4f/3})foreach(float bin in new[]{0f,1f})
                {
                    camera.aspect=fullAspect*.5f;world.CalculateLetterFrame(camera.aspect,bin,out var eye,out var look);
                    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(look-eye,desk.paper.transform.forward));
                    var points=Field<System.Collections.Generic.List<Vector3>>(world,"letterFramePoints");
                    foreach(var p in points)
                    {
                        var screen=camera.WorldToViewportPoint(desk.paper.transform.TransformPoint(p));
                        Require(screen.z>0 && screen.x>.04f && screen.x<.96f && screen.y>.04f && screen.y<.96f,"Paper/timer cropped at aspect "+fullAspect+" bin="+bin);
                    }
                    if(bin==1)foreach(var p in Field<System.Collections.Generic.List<Vector3>>(world,"binFramePoints"))
                    {
                        var screen=camera.WorldToViewportPoint(desk.paper.transform.TransformPoint(p));
                        Require(screen.z>0 && screen.x>.04f && screen.x<.96f && screen.y>.04f && screen.y<.96f,"Bin cropped at aspect "+fullAspect);
                    }
                    if(bin==0)
                    {
                        var t=desk.paper.transform;var c=desk.paper.center;var h=desk.paper.size.x*.5f;
                        float width=Mathf.Abs(camera.WorldToViewportPoint(t.TransformPoint(c+Vector3.right*h)).x-camera.WorldToViewportPoint(t.TransformPoint(c-Vector3.right*h)).x);
                        smallestPaper=Mathf.Min(smallestPaper,width);
                        Require(width>.30f,"Timer framing made paper too small: "+width);
                    }
                }
                return "PASS actual desk paper+timer inside 4% margin at 16:9/20:9/4:3 in writing/bin shots; minimum paper viewport width="+smallestPaper.ToString("F3");
            }
            finally{if(world!=null)world.Dispose();if(cameraObject!=null)Object.DestroyImmediate(cameraObject);}
        }

        public static string CaptureMemory(string directory)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Offscreen memory capture is Edit Mode only.");
            System.IO.Directory.CreateDirectory(directory);
            WritingWorld3D world=null;RenderTexture target=null;Texture2D image=null;
            var previous=RenderTexture.active;
            try
            {
                var state=new WritingChapter{Draft=2,Phase="collect",Started=true};
                world=new WritingWorld3D(null,state);
                var camera=world.MemoryCamera;camera.rect=new Rect(0,0,1,1);camera.aspect=8f/9;
                target=new RenderTexture(800,900,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
                image=new Texture2D(800,900,TextureFormat.RGB24,false);
                Save("autumn-memory-room.png");
                state.Collected=2;state.Player=state.ItemPositions[1]+new Vector2(0,45);
                Call(world,"UpdateAutumnActors",.1f);Field<float[]>(world,"pickupProgress")[1]=1;
                Call(world,"UpdateCarryPresentation",0f,true);Save("autumn-empty-vase.png");
                state.Collected=7;state.Player=new Vector2(300,565);state.Tick(.01f,Empty());
                for(int i=0;i<8;i++)Call(world,"UpdateAutumnActors",.1f);
                var progress=Field<float[]>(world,"pickupProgress");for(int i=0;i<4;i++)progress[i]=state.HasItem(i)?1:0;
                Call(world,"UpdateCarryPresentation",0f,true);Save("autumn-doorway-cue.png");
                state.Remaining=.01f;state.Tick(.02f,Empty());
                for(int i=0;i<12;i++)Call(world,"UpdateAutumnActors",.1f);
                Save("autumn-timeout-memory.png");
                return "Rendered four isolated memory-camera PNGs at 800x900 using current-pose temporary skin meshes to "+directory+". No Play or native input; framing/contact still require visual review.";

                void Save(string filename)
                {
                    RenderCurrentSkinPose(camera,world.Root.transform);
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,800,900),0,0);image.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,filename),image.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active=previous;
                if(world!=null){world.MemoryCamera.targetTexture=null;world.Dispose();}
                if(target!=null){target.Release();Object.DestroyImmediate(target);}
                if(image!=null)Object.DestroyImmediate(image);
            }
        }

        static void RenderCurrentSkinPose(Camera camera,Transform root)
        {
            // Multiple poses are sampled in one Editor frame. Temporary CPU snapshots avoid
            // reusing that frame's GPU skin cache. The true/child-identity convention was
            // compared with the live renderer in the winter offscreen capture at source scale.
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
            var copies=new System.Collections.Generic.List<GameObject>();
            var meshes=new System.Collections.Generic.List<Mesh>();
            try
            {
                foreach(var skin in skins)
                {
                    var mesh=new Mesh();meshes.Add(mesh);skin.BakeMesh(mesh,true);
                    var copy=new GameObject("Temporary autumn skin snapshot");copies.Add(copy);
                    copy.transform.SetParent(skin.transform,false);copy.layer=skin.gameObject.layer;
                    copy.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=copy.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;
                    renderer.shadowCastingMode=skin.shadowCastingMode;renderer.receiveShadows=skin.receiveShadows;
                    renderer.lightProbeUsage=skin.lightProbeUsage;renderer.reflectionProbeUsage=skin.reflectionProbeUsage;
                    var properties=new MaterialPropertyBlock();skin.GetPropertyBlock(properties);renderer.SetPropertyBlock(properties);
                    skin.enabled=false;
                }
                camera.Render();
            }
            finally
            {
                foreach(var copy in copies)Object.DestroyImmediate(copy);
                foreach(var mesh in meshes)Object.DestroyImmediate(mesh);
                foreach(var skin in skins)if(skin!=null)skin.enabled=true;
            }
        }
    }
}
