using System;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkExposureEncoderChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception("PARK_EXPOSURE_ENCODER: "+message);}
        // Waiting belongs only to this bounded editor fixture. Production polls once per Update.
        static void Take(ParkExposureEncoder encoder,out ParkPhotoRecord record,out byte[] png,out Exception error)
        {
            ParkPhotoRecord taken=null;byte[] bytes=null;Exception failure=null;
            if(!SpinWait.SpinUntil(()=>encoder.TryTake(out taken,out bytes,out failure),5000))throw new Exception("PNG worker timeout");
            record=taken;png=bytes;error=failure;
        }
        public static string Run()
        {
            var record=new ParkPhotoRecord{id="encoder-fixture"};byte[] rgb={255,0,0,0,255,0,0,0,255,31,63,127,250,200,150,5,10,15};
            ParkPhotoRecord saved;byte[] png;Exception error;
            using(var gate=new ManualResetEventSlim(false))
            {
                var held=new ParkExposureEncoder((raw,w,h)=>{gate.Wait(5000);return new byte[]{1};});
                try
                {
                    held.Start(record,rgb,3,2);
                    Check(held.HasPending && !held.TryTake(out saved,out png,out error),"not-ready encoding remains pending without blocking");
                    bool rejected=false;try{held.Start(record,rgb,3,2);}catch(InvalidOperationException){rejected=true;}
                    Check(rejected,"concurrent starts rejected");
                    gate.Set();Take(held,out saved,out png,out error);
                    Check(error==null && saved==record && !held.HasPending,"same record transferred once");
                    Check(!held.TryTake(out saved,out png,out error),"completed job cannot transfer twice");
                }
                finally{gate.Set();held.Cancel();}
            }
            int calls=0;
            var retry=new ParkExposureEncoder((raw,w,h)=>{
                Check(ReferenceEquals(raw,rgb) && w==3 && h==2,"retry retains original RGB and dimensions");
                if(Interlocked.Increment(ref calls)==1)throw new Exception("injected encode failure");
                return new byte[]{2};
            });
            retry.Start(record,rgb,3,2);Take(retry,out saved,out png,out error);
            Check(error!=null && retry.HasPending && retry.HasFailed,"failure retains raw record for retry");
            Check(retry.Retry(),"explicit retry starts failed job");Take(retry,out saved,out png,out error);
            Check(error==null && saved==record && calls==2 && !retry.HasPending,"retry transfers original record exactly once");
            var native=new ParkExposureEncoder();native.Start(record,rgb,3,2);Take(native,out saved,out png,out error);
            Check(error==null && png!=null,"native RGB24 worker encoding succeeds");
            Texture2D decoded=null;
            try
            {
                decoded=new Texture2D(2,2,TextureFormat.RGB24,false);
                Check(decoded.LoadImage(png) && decoded.width==3 && decoded.height==2,"PNG dimensions preserved");
                var pixels=decoded.GetPixels32();
                for(int i=0;i<pixels.Length;i++)Check(pixels[i].r==rgb[i*3] && pixels[i].g==rgb[i*3+1] && pixels[i].b==rgb[i*3+2] && pixels[i].a==255,"RGB24 pixels and orientation preserved "+i);
            }
            finally{if(decoded!=null)UnityEngine.Object.DestroyImmediate(decoded);}
            using(var gate=new ManualResetEventSlim(false))
            {
                var cancelled=new ParkExposureEncoder((raw,w,h)=>{gate.Wait(5000);return new byte[]{1};});
                cancelled.Start(record,rgb,3,2);cancelled.Cancel();gate.Set();
                Check(!cancelled.HasPending && !cancelled.TryTake(out saved,out png,out error),"cancelled job cannot deliver later");
            }
            return "Park exposure encoder checks passed: nonblocking polling, concurrent rejection, failure/retry, exactly-once transfer, native PNG dimensions/pixels/orientation, cancellation. No save files touched.";
        }
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Invoke(ParkPhotoPrototype prototype,string name,params object[] args)
        {return typeof(ParkPhotoPrototype).GetMethod(name,Private).Invoke(prototype,args);}
        static void Set(ParkPhotoPrototype prototype,string name,object value)
        {typeof(ParkPhotoPrototype).GetField(name,Private).SetValue(prototype,value);}
        static bool Pending(ParkPhotoPrototype prototype)
        {return (bool)typeof(ParkPhotoPrototype).GetProperty("HasPendingExposure",Private).GetValue(prototype);}
        public static string RunProductionIntegration()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA","park-async-integration-"+Guid.NewGuid().ToString("N")));
            GameObject isolated=null;ParkPhotoPrototype prototype=null;Texture2D reference=null,decoded=null;
            try
            {
                using(ParkFilmStore.BeginValidationScope(directory))
                {
                    var roll=ParkFilmStore.SaveCameraLoading(null,ParkCameraLoading.AtStep(7,400));
                    reference=new Texture2D(3,2,TextureFormat.RGB24,false);
                    byte[] raw={255,0,0,0,255,0,0,0,255,31,63,127,250,200,150,5,10,15};
                    reference.LoadRawTextureData(raw);reference.Apply();
                    roll=ParkFilmStore.SaveTask(roll.id,"entrance",new BridgePhotoCheckpoint{hasReference=true,aligned=true,fov=60,ev100=10,savedFocus=3,savedAperture=8,savedShutter=.004f},reference.EncodeToPNG());
                    // Never activate: no Start/Update/OnDisable, cursor/input context or live companion effects.
                    isolated=new GameObject("Isolated async exposure acceptance"){hideFlags=HideFlags.HideAndDontSave};
                    isolated.SetActive(false);prototype=isolated.AddComponent<ParkPhotoPrototype>();prototype.enabled=false;
                    Set(prototype,"roll",roll);
                    var record=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),taskId="entrance",iso=400,accepted=true,capturedUtc=DateTime.UtcNow.ToString("o")};
                    int notifications=0;prototype.PhotoTaken+=saved=>{Check(ReferenceEquals(saved,record),"production completion retains original record");notifications++;};
                    string manifest=Path.Combine(directory,roll.id,"roll.json");byte[] before=File.ReadAllBytes(manifest);
                    Invoke(prototype,"BeginExposureEncoding",record,reference.GetRawTextureData<byte>().ToArray(),3,2);
                    Check(Pending(prototype),"production encoding immediately owns pending exposure");
                    Check(!(bool)Invoke(prototype,"SavePhotoSettings"),"pending production exposure blocks settings writes");
                    Check(ParkFilmStore.MatchingExposureBytes(before,File.ReadAllBytes(manifest)),"pending settings guard preserves committed manifest");
                    var timeout=Stopwatch.StartNew();
                    while(Pending(prototype) && timeout.ElapsedMilliseconds<5000)
                    {Invoke(prototype,"TickExposureEncoding");Thread.Sleep(1);}
                    Check(!Pending(prototype),"production main-thread polling commits encoded exposure");
                    var committed=ParkFilmStore.Load(roll.id);
                    Check(committed.frames.Count==1 && committed.frames[0].id==record.id && committed.entrance.captured,"real store receives exactly one production exposure");
                    Check(notifications==1 && (bool)typeof(ParkPhotoPrototype).GetField("guidedDeparturePending",Private).GetValue(prototype),"production success handler fires once and holds guided departure");
                    Invoke(prototype,"TickExposureEncoding");
                    Check(!(bool)Invoke(prototype,"RetryPendingExposure") && ParkFilmStore.Load(roll.id).frames.Count==1 && notifications==1,"completed production retry cannot charge or notify twice");
                    decoded=new Texture2D(2,2,TextureFormat.RGB24,false);
                    Check(decoded.LoadImage(File.ReadAllBytes(Path.Combine(directory,roll.id,committed.frames[0].negativeFile))) && decoded.width==3 && decoded.height==2,"production store persists decodable worker PNG dimensions");
                    var pixels=decoded.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)Check(pixels[i].r==raw[i*3] && pixels[i].g==raw[i*3+1] && pixels[i].b==raw[i*3+2],"production stored PNG preserves exact pixels "+i);
                    Invoke(prototype,"BeginExposureEncoding",new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),taskId="bridge"},raw,3,2);
                    Set(prototype,"leavingSavedOnly",true);
                    for(int i=0;i<5;i++){Invoke(prototype,"TickExposureEncoding");Thread.Sleep(1);}
                    Check(ParkFilmStore.Load(roll.id).frames.Count==1 && notifications==1,"explicit saved-only exit suppresses later encoding writes");
                    Invoke(prototype,"CancelExposureEncoding");
                }
                return "Production async integration passed: hidden inactive prototype -> BeginExposureEncoding -> polling -> real store -> success handler once; settings guarded, duplicate retry blocked, PNG dimensions/pixels preserved, saved-only exit prevents writes. Fixture RGB pixels; live camera/companion/input/scene state untouched.";
            }
            finally
            {
                if(prototype!=null){Set(prototype,"leavingSavedOnly",true);Invoke(prototype,"CancelExposureEncoding");}
                if(isolated!=null)UnityEngine.Object.DestroyImmediate(isolated);
                if(reference!=null)UnityEngine.Object.DestroyImmediate(reference);
                if(decoded!=null)UnityEngine.Object.DestroyImmediate(decoded);
                string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA"))+Path.DirectorySeparatorChar;
                if(directory.StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }    }
}
