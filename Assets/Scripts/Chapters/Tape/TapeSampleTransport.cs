using System;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    // Runs in the audio callback: sample cursors do not depend on render frame rate.
    [RequireComponent(typeof(AudioSource))]
    public sealed class TapeSampleTransport : MonoBehaviour
    {
        readonly object sync=new object();
        float[] source,target;
        int[] mapping;
        int a,b,length,rate;
        bool playA,playB,record,muted,dirty;
        int seek;
        AudioClip clockClip;
        public int Rate=>rate;
        public float SourceSeconds {get{lock(sync)return a/(float)Math.Max(1,rate);}}
        public float TargetSeconds {get{lock(sync)return b/(float)Math.Max(1,rate);}}
        public bool SourcePlaying {get{lock(sync)return playA;}}
        public string SaveError {get;private set;}="";
        string path=>Path.Combine(GameSavePaths.Root,"tape-one-pcm-v1.bin");
        public void Initialize(float[] samples,int sampleRate)
        {
            rate=sampleRate;source=samples;target=new float[rate*120];mapping=new int[target.Length];
            for(int i=0;i<mapping.Length;i++)mapping[i]=-1;
            try
            {
                if(File.Exists(path))
                {
                    using(var reader=new BinaryReader(File.OpenRead(path)))
                    {
                        if(reader.ReadInt32()!=1 || reader.ReadInt32()!=rate || reader.ReadInt32()!=source.Length)throw new InvalidDataException("Tape recording format/source changed.");
                        length=reader.ReadInt32();if(length<0 || length>target.Length)throw new InvalidDataException("Invalid recording length.");
                        for(int i=0;i<length;i++){target[i]=reader.ReadSingle();mapping[i]=reader.ReadInt32();if(float.IsNaN(target[i]) || float.IsInfinity(target[i]) || mapping[i]<-1 || mapping[i]>=source.Length)throw new InvalidDataException("Invalid recording sample.");}
                    }
                }
            }
            catch(Exception e){SaveError=e.Message;Debug.LogError("Recording preserved but unavailable: "+e.Message);enabled=false;return;}
            clockClip=AudioClip.Create("Tape sample clock",rate,1,rate,false);
            var audio=GetComponent<AudioSource>();audio.clip=clockClip;audio.loop=true;audio.spatialBlend=0;audio.Play();
        }
        public void SourceCommand(int command)
        {lock(sync){if(command==0)playA=false;else if(command==1)playA=a<source.Length;else {playA=false;a=0;}}}
        public void TargetCommand(bool play,bool write,int winding,bool mute)
        {lock(sync){playB=play;record=write;seek=winding;muted=mute;}}
        public void StopAll(){lock(sync){playA=playB=record=false;seek=0;}Save();}
        public void ClearTarget(){lock(sync){playB=record=false;seek=0;b=length=0;dirty=true;}Save();}
        public void RewindTarget(){lock(sync){playB=record=false;seek=0;b=0;}}
        void OnAudioFilterRead(float[] output,int channels)
        {
            lock(sync)
            {
                if(source==null || SaveError!=""){Array.Clear(output,0,output.Length);return;}
                for(int i=0;i<output.Length;i+=channels)
                {
                    float input=a<source.Length?source[a]:0;
                    float heard=playA?input:0;
                    if(seek!=0)b=Math.Max(0,Math.Min(length,b+seek*4));
                    else if(playB && b<target.Length)
                    {
                        if(record)
                        {
                            target[b]=playA?input:0;mapping[b]=playA && a<source.Length?a:-1;
                            b++;length=Math.Max(length,b);dirty=true;
                        }
                        else if(b<length){if(!playA)heard=target[b];b++;}
                    }
                    if(playA){a++;if(a>=source.Length){a=source.Length;playA=false;}}
                    for(int c=0;c<channels;c++)output[i+c]=muted?0:heard*.65f;
                }
            }
        }
        // Submission only. This result never feeds transport or edits samples.
        public int[] SnapshotMapping(){lock(sync){var result=new int[length];Array.Copy(mapping,result,length);return result;}}
        public bool Save()
        {
            lock(sync)
            {
                if(SaveError!="")return false;
                if(!dirty)return true;
                try
                {
                    Directory.CreateDirectory(GameSavePaths.Root);
                    using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write))
                    {using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write(1);writer.Write(rate);writer.Write(source.Length);writer.Write(length);for(int i=0;i<length;i++){writer.Write(target[i]);writer.Write(mapping[i]);}}stream.Flush(true);}
                    if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
                    dirty=false;return true;
                }
                catch(Exception e){Debug.LogWarning("Recording not saved; retained in memory: "+e.Message);return false;}
            }
        }
        void OnDisable(){StopAll();}
        void OnDestroy(){StopAll();if(clockClip!=null)Destroy(clockClip);}
    }
}
