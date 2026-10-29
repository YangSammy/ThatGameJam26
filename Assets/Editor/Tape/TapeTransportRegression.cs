using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    // In-memory audio callback regression. Never initializes or writes a save file.
    public static class TapeTransportRegression
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(TapeSampleTransport t,string name,object value)=>typeof(TapeSampleTransport).GetField(name,Private).SetValue(t,value);
        static void Pump(TapeSampleTransport t,int count)=>typeof(TapeSampleTransport).GetMethod("OnAudioFilterRead",Private).Invoke(t,new object[]{new float[count],1});
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static string Run()
        {
            var go=new GameObject("Temporary tape regression");go.SetActive(false);
            var t=go.AddComponent<TapeSampleTransport>();
            try
            {
                Set(t,"source",new float[10]);Set(t,"target",new float[30]);Set(t,"mapping",new int[30]);Set(t,"rate",10);Set(t,"configured",true);
                t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,4);
                t.TargetCommand(false,false,0,false);Set(t,"b",1);Set(t,"playA",false);
                t.TargetCommand(true,false,0,false);Pump(t,2);
                t.SourceCommand(1);t.TargetCommand(true,true,0,false);Pump(t,3);
                var mapping=t.SnapshotMapping();Require(mapping.Length==7,"Resume overwrote/truncated previous audio");
                for(int i=0;i<7;i++)Require(mapping[i]==i,"Resume changed existing samples");
                Pump(t,20);Require(t.RecordedSeconds==1 && !t.Writing && !t.SourcePlaying,"Source EOF appended blank audio");
                Set(t,"b",0);t.TargetCommand(true,false,0,false);Pump(t,20);
                Require(t.TargetSeconds==1,"Playback advanced beyond recorded end");
                t.TargetCommand(true,false,0,false);Pump(t,20);Require(t.TargetSeconds==1,"Held PLAY restarted beyond end");
                return "PASS: append after listening; no trailing EOF silence; playback stops at length; held PLAY stays stopped.";
            }
            finally{Set(t,"configured",false);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
