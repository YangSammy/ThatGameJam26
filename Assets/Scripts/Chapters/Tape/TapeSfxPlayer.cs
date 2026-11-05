using UnityEngine;
namespace MemoryWhitebox
{
    // Separate voices: never replace the sample transport AudioSource or recorded PCM.
    public sealed class TapeSfxPlayer:MonoBehaviour
    {
        public TapeSfxBank bank;
        sealed class Voice {public AudioSource source;public TapeSfxBank.Cue cue;public float remaining;public bool wanted;}
        readonly Voice[] loops=new Voice[4];
        readonly Voice[] shots=new Voice[8];
        int next;
        bool muted;
        Voice Bind(string name)
        {
            var child=transform.Find(name);
            if(child==null){child=new GameObject(name).transform;child.SetParent(transform,false);}
            var a=child.GetComponent<AudioSource>();if(a==null)a=child.gameObject.AddComponent<AudioSource>();
            a.Stop();a.playOnAwake=false;a.spatialBlend=.65f;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=.5f;a.maxDistance=7;
            return new Voice{source=a};
        }
        void EnsureVoices()
        {
            if(loops[0]!=null && shots[0]!=null)return;
            // Script reload clears the managed voice cache but leaves its scene AudioSources.
            for(int i=0;i<loops.Length;i++)loops[i]=Bind("SFX Loop "+i);
            for(int i=0;i<shots.Length;i++)shots[i]=Bind("SFX Contact "+i);
        }
        void Awake(){EnsureVoices();}
        void StartVoice(Voice v,TapeSfxBank.Cue c,Vector3 position,float pan,float pitch)
        {v.cue=c;v.source.Stop();v.source.transform.position=position;v.source.clip=c.clip;v.source.panStereo=pan;v.source.pitch=pitch;v.source.volume=c.volume;v.source.Play();v.source.time=Mathf.Clamp(c.start,0,c.clip.length-.01f);v.remaining=c.duration/Mathf.Max(.1f,pitch);}
        public void Shot(int id,Vector3 at,float pan=0,float pitch=1)
        {if(muted || bank==null || !isActiveAndEnabled)return;var c=bank.Find(id);if(c==null||c.clip==null)return;EnsureVoices();StartVoice(shots[next++%shots.Length],c,at,pan,pitch);}
        public void Loop(int channel,int id,bool active,Vector3 at,float pan=0,float pitch=1)
        {
            if(!isActiveAndEnabled)return;EnsureVoices();
            var v=loops[channel];var c=bank==null?null:bank.Find(id);v.wanted=active&&!muted&&c!=null&&c.clip!=null;
            if(!v.wanted)return;
            if(v.cue!=c || !v.source.isPlaying){StartVoice(v,c,at,pan,pitch);v.source.volume=0;}
            v.source.transform.position=at;v.source.pitch=pitch;
        }
        public void SetMuted(bool value){muted=value;if(value)StopAll();}
        public void StopLoop(int channel,bool immediate=false){var v=loops[channel];if(v==null)return;v.wanted=false;if(immediate)v.source.Stop();}
        public void StopAll(){foreach(var v in loops){if(v==null)continue;v.wanted=false;v.source.Stop();}foreach(var v in shots)if(v!=null)v.source.Stop();}
        void Update()
        {
            EnsureVoices();
            foreach(var v in shots)if(v.source.isPlaying){v.remaining-=Time.unscaledDeltaTime;v.source.volume=v.cue.volume*Mathf.Clamp01(v.remaining/.02f);if(v.remaining<=0)v.source.Stop();}
            foreach(var v in loops)
            {
                if(v.cue==null)continue;
                v.source.volume=Mathf.MoveTowards(v.source.volume,v.wanted?v.cue.volume:0,Time.unscaledDeltaTime*v.cue.volume/.1f);
                if(!v.wanted){if(v.source.volume<=.0001f)v.source.Stop();continue;}
                if(v.source.time>=Mathf.Min(v.cue.clip.length-.01f,v.cue.start+v.cue.duration))v.source.time=v.cue.start;
            }
        }
        void OnDisable(){StopAll();}
    }
}
