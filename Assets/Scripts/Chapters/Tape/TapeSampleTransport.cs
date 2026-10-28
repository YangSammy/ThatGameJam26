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
        int commission;
        string sourceHash;
        bool configured;
        public int Rate=>rate;
        public float SourceDuration {get{lock(sync)return source==null?0:source.Length/(float)Math.Max(1,rate);}}
        public float SourceSeconds {get{lock(sync)return a/(float)Math.Max(1,rate);}}
        public float TargetSeconds {get{lock(sync)return b/(float)Math.Max(1,rate);}}
        public bool SourcePlaying {get{lock(sync)return playA;}}
        public string SourceStatus {get{lock(sync)return playA?"PLAYING":source!=null && a>=source.Length?"END":"STOPPED";}}
        public bool Writing {get{lock(sync)return configured && playB && record && seek==0 && b<target.Length;}}
        public string TargetStatus {get{lock(sync)return seek<0?"REWINDING":seek>0?"FAST FORWARD":playB && b>=target.Length?"END":playB && record?"RECORDING":playB && b<length?"PLAYBACK":playB?"RUNNING BLANK":length==0?"EMPTY":"PAUSED";}}
        public float RecordedSeconds {get{lock(sync)return length/(float)Math.Max(1,rate);}}
        public string SaveError {get;private set;}="";
        string path=>Path.Combine(GameSavePaths.Root,commission==0?"tape-one-pcm-v1.bin":"tape-commission-"+commission+"-pcm.bin");
        public void Initialize(float[] samples,int sampleRate,int commissionIndex=0)
        {
            if(samples==null || samples.Length==0 || sampleRate<=0 || commissionIndex<0 || commissionIndex>=RadioTapeChapter.CommissionCount)throw new ArgumentException("Invalid commission audio.");
            if(configured && SaveError=="" && !Save())throw new IOException("Previous commission could not be saved.");
            var voice=GetComponent<AudioSource>();voice.Stop();
            lock(sync)
            {
            playA=playB=record=false;seek=0;a=b=length=0;dirty=false;
            commission=commissionIndex;SaveError="";
            var bytes=new byte[samples.Length*sizeof(float)];Buffer.BlockCopy(samples,0,bytes,0,bytes.Length);
            using(var hash=System.Security.Cryptography.SHA256.Create())sourceHash=Convert.ToBase64String(hash.ComputeHash(bytes));
            rate=sampleRate;source=samples;target=new float[rate*120];mapping=new int[target.Length];
            for(int i=0;i<mapping.Length;i++)mapping[i]=-1;
            try
            {
                if(File.Exists(path))
                {
                    using(var reader=new BinaryReader(File.OpenRead(path)))
                    {
                        int version=reader.ReadInt32();
                        if((version<1 || version>3) || reader.ReadInt32()!=rate || reader.ReadInt32()!=source.Length)throw new InvalidDataException("Tape recording format/source changed.");
                        if(version>=2 && reader.ReadString()!=sourceHash)throw new InvalidDataException("Commission source audio changed; recording preserved.");
                        length=reader.ReadInt32();if(length<0 || length>target.Length)throw new InvalidDataException("Invalid recording length.");
                        for(int i=0;i<length;i++){target[i]=reader.ReadSingle();mapping[i]=reader.ReadInt32();if(float.IsNaN(target[i]) || float.IsInfinity(target[i]) || mapping[i]<-1 || mapping[i]>=source.Length)throw new InvalidDataException("Invalid recording sample.");}
                        if(version>=2){a=reader.ReadInt32();b=reader.ReadInt32();if(a<0 || a>source.Length || b<0 || b>(version>=3?target.Length:length))throw new InvalidDataException("Invalid tape cursor.");}
                    }
                }
            }
            catch(Exception e){SaveError=e.Message;Debug.LogError("Recording preserved but unavailable: "+e.Message);enabled=false;return;}
            if(clockClip!=null)Destroy(clockClip);
            configured=true;enabled=true;
            clockClip=AudioClip.Create("Tape sample clock",rate,1,rate,false);
            var audio=GetComponent<AudioSource>();audio.clip=clockClip;audio.loop=true;audio.spatialBlend=0;audio.Play();
            }
        }
        public void SourceCommand(int command)
        {lock(sync){if(!configured || SaveError!="")return;if(command==0)playA=false;else if(command==1)playA=a<source.Length;else {playA=false;a=0;}dirty=true;}if(command!=1)Save();}
        public void TargetCommand(bool play,bool write,int winding,bool mute)
        {lock(sync){playB=configured && play;record=playB && write;seek=!playB && !write?Math.Sign(winding):0;muted=mute;}}
        public void StopAll(){lock(sync){playA=playB=record=false;seek=0;if(configured)dirty=true;}Save();}
        public void ClearTarget(){lock(sync){playB=record=false;seek=0;b=length=0;if(target!=null)Array.Clear(target,0,target.Length);if(mapping!=null)for(int i=0;i<mapping.Length;i++)mapping[i]=-1;dirty=true;}Save();}
        public bool RetryCurrent()
        {
            lock(sync)
            {
                if(!configured || SaveError!="")return false;
                playA=playB=record=false;seek=0;a=b=length=0;
                Array.Clear(target,0,target.Length);for(int i=0;i<mapping.Length;i++)mapping[i]=-1;
                dirty=true;
            }
            return Save();
        }
        public void RewindTarget(){lock(sync){playB=record=false;seek=0;b=0;dirty=true;}Save();}
        void OnAudioFilterRead(float[] output,int channels)
        {
            lock(sync)
            {
                if(source==null || SaveError!=""){Array.Clear(output,0,output.Length);return;}
                for(int i=0;i<output.Length;i+=channels)
                {
                    float input=a<source.Length?source[a]:0;
                    float heard=playA?input:0;
                    if(seek!=0){b=seek<0?Math.Max(0,b-4):b<length?Math.Min(length,b+4):b;dirty=true;}
                    else if(playB && b<target.Length)
                    {
                        if(record)
                        {
                            target[b]=playA?input:0;mapping[b]=playA && a<source.Length?a:-1;
                            b++;length=Math.Max(length,b);dirty=true;
                        }
                        else {if(!playA)heard=b<length?target[b]:0;b++;dirty=true;}
                    }
                    if(playA){a++;dirty=true;if(a>=source.Length){a=source.Length;playA=false;}}
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
                if(!configured)return true;
                if(!dirty)return true;
                try
                {
                    Directory.CreateDirectory(GameSavePaths.Root);
                    using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write))
                    {using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write(3);writer.Write(rate);writer.Write(source.Length);writer.Write(sourceHash);writer.Write(length);for(int i=0;i<length;i++){writer.Write(target[i]);writer.Write(mapping[i]);}writer.Write(a);writer.Write(b);}stream.Flush(true);}
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
