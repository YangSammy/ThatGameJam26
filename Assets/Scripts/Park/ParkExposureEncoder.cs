using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MemoryWhitebox
{
    // Only managed RGB pixels cross this worker boundary. Unity documents EncodeArrayToPNG as thread safe.
    public sealed class ParkExposureEncoder
    {
        sealed class Result { public byte[] png;public Exception error; }
        readonly Func<byte[],int,int,byte[]> encode;
        ParkPhotoRecord pending;
        byte[] raw;
        int width,height;
        Task<Result> task;
        public bool HasPending=>pending!=null;
        public bool IsRunning=>task!=null;
        public bool HasFailed=>HasPending && task==null;
        public ParkExposureEncoder(Func<byte[],int,int,byte[]> encode=null)
        {this.encode=encode??EncodeRgb;}
        static byte[] EncodeRgb(byte[] pixels,int width,int height)
        {return ImageConversion.EncodeArrayToPNG(pixels,GraphicsFormat.R8G8B8_UNorm,(uint)width,(uint)height,(uint)(width*3));}
        public void Start(ParkPhotoRecord record,byte[] rgb,int width,int height)
        {
            if(HasPending)throw new InvalidOperationException("An exposure is already encoding.");
            if(record==null || width<=0 || height<=0 || rgb==null || rgb.LongLength!=(long)width*height*3)throw new InvalidDataException("Invalid RGB24 exposure pixels.");
            pending=record;raw=rgb;this.width=width;this.height=height;Launch();
        }
        void Launch()
        {
            // Capture values so Cancel can release owner state without changing a running job.
            byte[] pixels=raw;int w=width,h=height;var encoder=encode;
            task=Task.Run(()=>{
                try
                {
                    byte[] png=encoder(pixels,w,h);
                    if(png==null || png.Length==0)throw new InvalidDataException("PNG encoding returned no data.");
                    return new Result{png=png};
                }
                catch(Exception error){return new Result{error=error};}
            });
        }
        public bool Retry(){if(!HasFailed)return false;Launch();return true;}
        public bool TryTake(out ParkPhotoRecord record,out byte[] png,out Exception error)
        {
            record=null;png=null;error=null;
            if(task==null || !task.IsCompleted)return false;
            // IsCompleted guarantees GetResult cannot wait. Worker faults are returned as data.
            var completed=task.GetAwaiter().GetResult();task=null;
            if(completed.error!=null){error=completed.error;return true;}
            record=pending;png=completed.png;pending=null;raw=null;return true;
        }
        public void Cancel(){pending=null;raw=null;task=null;}
    }
}
