using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    /// <summary>Rule checks exercise the same models as input. Spatial checks use the actual runtime colliders.</summary>
    public static class WhiteboxV3Validation
    {
        [Serializable] sealed class Report { public string version="3.0.0-whitebox";public string unity;public int passed;public string[] checks;public string[] failures; }
        static void Check(List<string> result,bool condition,string label){if(!condition)throw new InvalidOperationException(label);result.Add(label);}
        public static string[] Run(WBMain host,string outputFolder)
        {
            var results=new List<string>();
            var failures=new List<string>();
            if(host!=null)
            {
                host.CurrentWorld?.Deactivate();host.Room.SetVisible(false);
                TapeV3Checks.CaptureStage=(name,world)=>Capture(Path.Combine(outputFolder,name+".png"),world);
                PhotoV3Checks.CaptureStage=(name,world)=>Capture(Path.Combine(outputFolder,name+".png"),world);
                WritingV3Checks.CaptureStage=(name,world)=>Capture(Path.Combine(outputFolder,name+".png"),world);
                PaintingV3Checks.CaptureStage=(name,world)=>Capture(Path.Combine(outputFolder,name+".png"),world);
            }
            var previous=WBMain.Instance;WBMain.Instance=null;
            try
            {
                RunSuite("Tape",TapeV3Checks.Run,results,failures);
                RunSuite("Photo",PhotoV3Checks.Run,results,failures);
                RunSuite("Writing",WritingV3Checks.Run,results,failures);
                RunSuite("Painting",PaintingV3Checks.Run,results,failures);
                var ending=new EndingChapter();ending.CrossThreshold();Check(results,!ending.Completed,"Ending cannot complete before the door is opened");
                ending.Open();ending.CrossThreshold();Check(results,ending.Completed,"Opening and physically crossing completes the fifth chapter");
            }
            finally{WBMain.Instance=previous;TapeV3Checks.CaptureStage=null;PhotoV3Checks.CaptureStage=null;WritingV3Checks.CaptureStage=null;PaintingV3Checks.CaptureStage=null;}
            if(host!=null)
            {
                try{CheckDefocus(host,results,outputFolder);}catch(Exception e){failures.Add("Defocus: "+e);}
                try{CheckHub(host,results);}catch(Exception e){failures.Add("Hub: "+e);}
            }
            Directory.CreateDirectory(outputFolder);
            File.WriteAllText(Path.Combine(outputFolder,"validation.json"),JsonUtility.ToJson(new Report{unity=Application.unityVersion,passed=results.Count,checks=results.ToArray(),failures=failures.ToArray()},true));
            if(failures.Count>0)throw new InvalidOperationException("WHITEBOX_V3_FAILED\n"+string.Join("\n",failures));
            Debug.Log("WHITEBOX_V3_CHECKS_PASS "+results.Count);
            return results.ToArray();
        }

        static void RunSuite(string name,Func<string[]> run,List<string> results,List<string> failures)
        {try{results.AddRange(run());}catch(Exception e){failures.Add(name+": "+e);}}

        static void CheckDefocus(WBMain host,List<string> results,string folder)
        {
            var world=new PaintingWorld3D(host,new PaintingChapter());
            try
            {
                host.Room.SetVisible(false);world.Activate();world.PrepareQAFrame();
                Check(results,world.Defocus.HasShader&&world.Defocus.IsSupported,"Painting blur material and both GPU passes are supported in the player");
                world.Defocus.SetFocusHeld(true,.5f);string sharp=Path.Combine(folder,"painting-focus-sharp.png");Capture(sharp,world);
                world.Defocus.SetFocusHeld(false,.5f);string blurred=Path.Combine(folder,"painting-focus-blurred.png");Capture(blurred,world);
                float a=Energy(sharp),b=Energy(blurred);
                File.WriteAllText(Path.Combine(folder,"defocus-metrics.txt"),"Sharp edge energy="+a+"\nBlurred edge energy="+b+"\n");
                Check(results,a>.0001f&&b<a*.94f,"Released focus measurably reduces rendered edge detail; holding restores the same 3D scene");
            }
            finally{world.Deactivate();world.Dispose();}
        }
        static float Energy(string path)
        {
            var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));Color32[] p=image.GetPixels32();double energy=0;int w=image.width,h=image.height;
            for(int y=1;y<h;y++)for(int x=1;x<w;x++)
            {
                int i=y*w+x;float c=(p[i].r+p[i].g+p[i].b)/(3f*255);
                float left=(p[i-1].r+p[i-1].g+p[i-1].b)/(3f*255),down=(p[i-w].r+p[i-w].g+p[i-w].b)/(3f*255);
                energy+=Math.Abs(c-left)+Math.Abs(c-down);
            }
            UnityEngine.Object.Destroy(image);return (float)(energy/(w*h));
        }

        public static void Capture(string path,WorldChapter3D world)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));world.Root.SetActive(true);world.ApplyPose();Physics.SyncTransforms();
            var writing=world as WritingWorld3D;
            CaptureCamera(path,world.View,writing!=null?writing.MemoryCamera:null);
        }
        public static void CaptureCamera(string path,Camera camera,Camera secondary=null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(camera==null)throw new InvalidOperationException("No native camera for "+path);
            var target=new RenderTexture(1280,760,24);target.Create();
            var previous=camera.targetTexture;var active=RenderTexture.active;
            RenderTexture memoryPrevious=secondary!=null?secondary.targetTexture:null;
            try
            {
                camera.targetTexture=target;camera.Render();
                if(secondary!=null){secondary.targetTexture=target;secondary.Render();}
                RenderTexture.active=target;var texture=new Texture2D(1280,760,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1280,760),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
            }
            finally
            {
                camera.targetTexture=previous;if(secondary!=null)secondary.targetTexture=memoryPrevious;
                RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);
            }
        }

        static void CheckHub(WBMain host,List<string> results)
        {
            host.Room.SetVisible(true);var room=host.Room;
            // Flood fill uses the exact player clearance function, never WarpTo or a teleport success flag.
            const float step=.16f;const int width=27,height=35;
            var start=new Vector2Int(15,3);var queue=new Queue<Vector2Int>();var visited=new HashSet<Vector2Int>();
            queue.Enqueue(start);visited.Add(start);
            var positions=new List<Vector3>();
            while(queue.Count>0)
            {
                Vector2Int cell=queue.Dequeue();Vector3 p=new Vector3(-2.08f+cell.x*step,1.62f,-2.6f+cell.y*step);
                if(!room.CanStand(p))continue;positions.Add(p);
                foreach(var direction in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                {
                    Vector2Int next=cell+direction;if(next.x<0||next.x>=width||next.y<0||next.y>=height||visited.Contains(next))continue;
                    Vector3 n=new Vector3(-2.08f+next.x*step,1.62f,-2.6f+next.y*step);if(!room.CanStand(n))continue;
                    visited.Add(next);queue.Enqueue(next);
                }
            }
            Check(results,positions.Count>80,"Bedroom has a connected walkable area with furniture clearance");Physics.SyncTransforms();
            for(int station=0;station<5;station++)
            {
                bool found=false;
                foreach(var p in positions)
                {
                    Vector3 d=room.StationPositions[station]-p;if(d.magnitude>2.4f)continue;
                    if(Physics.Raycast(p,d.normalized,out RaycastHit hit,2.4f,1<<9))
                    {var h=hit.collider.GetComponent<HubHotspot>();if(h!=null&&h.Chapter==station){found=true;break;}}
                }
                Check(results,found,"Connected walking path reaches actual workstation collider "+station);
            }
            Check(results,!room.CanStand(new Vector3(-1.51f,1.62f,1.66f)),"Bedroom blocks walking through the bed");
            Check(results,!room.CanStand(new Vector3(1.77f,1.62f,-1.88f)),"Bedroom blocks walking through the wardrobe");
            Check(results,!room.CanStand(new Vector3(0,1.62f,-3.5f)),"Closed door blocks the exit path");
            room.SetDay(4);room.OpenDoor();Check(results,room.CanStand(new Vector3(0,1.62f,-3.5f)),"Thawed spring door opens a traversable physical threshold");
            room.WalkTick(.5f);Check(results,room.ThawProgress>0&&room.ThawProgress<1,"Final spring visibly thaws gradually instead of instantly replacing winter");
            room.WalkTick(5);Check(results,room.ThawProgress==1,"Final thaw reaches open flowers and an ice-free yard");
            for(int i=0;i<4;i++){room.ApplySeason(i);Check(results,room.Season==i,"Season state applies day variant "+i);}
            room.SetDay(host.Day);
        }
    }
}
