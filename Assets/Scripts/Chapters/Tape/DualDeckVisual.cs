using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation only: sparse clips never write the other deck's transforms.
    public sealed class DualDeckVisual:MonoBehaviour
    {
        public AnimationClip[] clips;
        public Transform tapeA,tapeB;
        public Collider[] keyHits;
        public string[] keyCommands;
        readonly Dictionary<string,AnimationClip> library=new Dictionary<string,AnimationClip>();
        readonly Dictionary<string,Motion> motions=new Dictionary<string,Motion>();
        readonly List<string> finished=new List<string>();
        sealed class Motion {public AnimationClip clip;public float time;public Action complete;}
        readonly bool[] busy=new bool[2],open=new bool[2];
        readonly float[] reelTime=new float[2];
        readonly string[] latched=new string[2];
        public bool Busy(int deck)=>busy[deck];
        public bool Closed(int deck)=>!open[deck]&&!busy[deck];
        public bool IsOpen(int deck)=>open[deck];
        string Prefix(int deck)=>deck==0?"A_":"B_";
        void Ensure(){if(library.Count==0)foreach(var c in clips)if(c!=null)library[c.name]=c;}
        void Pose(string name,float time){Ensure();if(library.TryGetValue(name,out var clip))clip.SampleAnimation(gameObject,Mathf.Clamp(time,0,clip.length));}
        void EndPose(string name){Ensure();if(library.TryGetValue(name,out var c))Pose(name,c.length);}
        void Play(string channel,string name,Action done=null)
        {Ensure();if(!library.TryGetValue(name,out var c))throw new InvalidOperationException("Missing radio clip: "+name);motions[channel]=new Motion{clip=c,complete=done};Pose(name,0);}
        public void Snap(int deck,bool loaded,bool doorOpen=false)
        {
            string p=Prefix(deck);motions.Remove(p+"door");motions.Remove(p+"tape");busy[deck]=false;open[deck]=doorOpen;
            EndPose(p+(doorOpen?"Open":"Close"));EndPose(p+"LoadTape");ShowTape(deck,loaded);
        }
        public void ShowTape(int deck,bool visible){(deck==0?tapeA:tapeB).gameObject.SetActive(visible);}
        public bool Door(int deck,bool opening)
        {
            if(busy[deck]||open[deck]==opening)return false;busy[deck]=true;
            Play(Prefix(deck)+"door",Prefix(deck)+(opening?"Open":"Close"),()=>{open[deck]=opening;busy[deck]=false;});return true;
        }
        public bool Transfer(int deck,bool load,Action complete)
        {
            if(busy[deck])return false;busy[deck]=true;string p=Prefix(deck);
            Action move=()=>{open[deck]=true;ShowTape(deck,true);Play(p+"tape",p+(load?"LoadTape":"UnloadTape"),()=>{
                if(load)Play(p+"door",p+"Close",()=>{open[deck]=false;busy[deck]=false;complete?.Invoke();});
                else {ShowTape(deck,false);busy[deck]=false;complete?.Invoke();}
            });};
            if(open[deck])move();else Play(p+"door",p+"Open",move);return true;
        }
        public void Tick(float dt,bool aPlaying,string bStatus,bool writing)
        {
            finished.Clear();foreach(var pair in motions){var m=pair.Value;m.time=Mathf.Min(m.clip.length,m.time+dt);m.clip.SampleAnimation(gameObject,m.time);if(m.time>=m.clip.length)finished.Add(pair.Key);}
            foreach(var key in finished){var done=motions[key].complete;motions.Remove(key);done?.Invoke();}
            bool bPlaying=bStatus=="PLAYBACK"||bStatus=="RECORDING"||bStatus=="RUNNING BLANK"||bStatus=="REWINDING"||bStatus=="FAST FORWARD";
            for(int i=0;i<2;i++)if(Closed(i)&&(i==0?aPlaying:bPlaying))
            {float speed=i==1&&bStatus=="REWINDING"?-4:i==1&&bStatus=="FAST FORWARD"?4:1;reelTime[i]=Mathf.Repeat(reelTime[i]+dt*speed,2);Pose(Prefix(i)+"PlaybackLoop",reelTime[i]);}
            Latch(0,aPlaying?"Play":null);
            Latch(1,bStatus=="REWINDING"?"Rewind":bStatus=="FAST FORWARD"?"Forward":bPlaying?"Play":null);
            if(recordHeld!=writing){recordHeld=writing;Play("record-key","B_Key_Record_"+(writing?"Latch":"Release"));}
            EndPose(writing?"B_RecordLight_On":"B_RecordLight_Off");
        }
        bool recordHeld;
        void Latch(int deck,string key)
        {
            if(latched[deck]==key)return;string p=Prefix(deck);
            if(latched[deck]!=null)Play(p+latched[deck],p+"Key_"+latched[deck]+"_Release");
            latched[deck]=key;if(key!=null)Play(p+key,p+"Key_"+key+"_Latch");
        }
        public void Press(int deck,string key)=>Play(Prefix(deck)+key,Prefix(deck)+"Key_"+key+"_Press");
        public string Command(Collider hit){if(keyHits==null)return null;for(int i=0;i<keyHits.Length;i++)if(keyHits[i]==hit)return keyCommands[i];return null;}
        public void Settle(bool a,bool b){motions.Clear();Snap(0,a);Snap(1,b);Latch(0,null);Latch(1,null);recordHeld=false;EndPose("B_Key_Record_Release");EndPose("B_RecordLight_Off");}
    }
}
