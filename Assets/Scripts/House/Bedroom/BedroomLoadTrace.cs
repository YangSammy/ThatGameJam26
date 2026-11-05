using System;
using System.Diagnostics;
using UnityEngine;

namespace MemoryWhitebox
{
    // One entry per stage, never per frame. Available in both Editor.log and Player.log.
    public sealed class BedroomLoadTrace
    {
        static int sequence;
        readonly string label;
        readonly Stopwatch total=Stopwatch.StartNew(),lap=Stopwatch.StartNew();
        public BedroomLoadTrace(string name)
        {label=(++sequence)+" "+name;UnityEngine.Debug.Log("[BedroomLoad] "+label+" begin");}
        public void Mark(string stage)
        {Write(stage,lap.Elapsed.TotalMilliseconds);lap.Restart();}
        public void MarkFrameWait(string stage,int previousFrame)
        {
            Mark(stage+" [frames="+(Time.frameCount-previousFrame)+", focused="+Application.isFocused+", dt="+Time.unscaledDeltaTime.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"]");
        }
        public void End(string result="ready")=>Write("TOTAL / "+result,total.Elapsed.TotalMilliseconds);
        public IDisposable Measure(string stage)=>new Sample(this,stage);
        void Write(string stage,double ms)=>UnityEngine.Debug.Log("[BedroomLoad] "+label+" | "+stage+" | "+ms.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" ms");
        sealed class Sample:IDisposable
        {
            readonly BedroomLoadTrace owner;readonly string stage;readonly Stopwatch watch=Stopwatch.StartNew();
            public Sample(BedroomLoadTrace owner,string stage){this.owner=owner;this.stage=stage;}
            public void Dispose()=>owner.Write(stage,watch.Elapsed.TotalMilliseconds);
        }
    }
}
