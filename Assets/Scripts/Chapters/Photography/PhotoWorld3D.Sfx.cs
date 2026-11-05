using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        TapeSfxPlayer photographySfx;
        bool photographyBankLoaded;
        TapeSfxBank photographyBank;
        readonly float[] photographyMotionUntil=new float[3];
        readonly int[] photographyMotionCue={28,29,130};
        readonly Vector3[] photographyMotionPoint=new Vector3[3];
        public event Action<int> MechanicalSoundRequested;
        public int MechanicalSoundRequestCount{get;private set;}
        bool PhotographySilent=>Host==null || Host.Muted || Host.ShowHelp || !Host.enabled ||
            (Host.safeInspection!=null && Host.safeInspection.BlocksInput) ||
            (!Host.InputContext.Allows(InputContextId.Station) && !Host.InputContext.Allows(InputContextId.Darkroom));
        Vector3 PhotographySoundPoint(string id)
        {GameObject item;return id!=null && Nodes.TryGetValue(id,out item)?item.transform.position:Root.transform.position;}
        bool EnsurePhotographySound()
        {
            if(Host==null)return false;
            if(!photographyBankLoaded){photographyBankLoaded=true;photographyBank=Resources.Load<TapeSfxBank>("PhotographySfxBank");}
            if(photographyBank==null)return false;
            if(photographySfx==null)
            {
                var child=new GameObject("Photography mechanical audio");child.transform.SetParent(Root.transform,false);
                photographySfx=child.AddComponent<TapeSfxPlayer>();photographySfx.bank=photographyBank;
                child.AddComponent<PhotographySfxLifecycle>().owner=this;
            }
            return true;
        }
        void NotifyPhotographySound(int cue)
        {MechanicalSoundRequestCount++;MechanicalSoundRequested?.Invoke(cue);}
        bool PhotographySound(int cue,string id)
        {
            NotifyPhotographySound(cue);
            // A null-host world is an isolated controller fixture, with no audible output.
            if(PhotographySilent)return true;
            if(!EnsurePhotographySound())return false;
            var authored=photographyBank.Find(cue);if(authored==null || authored.clip==null)return false;
            photographySfx.SetMuted(false);photographySfx.Shot(cue,PhotographySoundPoint(id));return true;
        }
        void PhotographyMotion(int channel,string id)
        {
            NotifyPhotographySound(photographyMotionCue[channel]);
            if(PhotographySilent){StopPhotographyMotion();return;}
            if(!EnsurePhotographySound())return;
            photographyMotionUntil[channel]=Time.unscaledTime+.14f;
            photographyMotionPoint[channel]=PhotographySoundPoint(id);TickPhotographySound();
        }
        void StopPhotographyMotion()
        {
            for(int i=0;i<photographyMotionUntil.Length;i++)
            {photographyMotionUntil[i]=0;if(photographySfx!=null)photographySfx.StopLoop(i,true);}
        }
        internal void StopPhotographySound()
        {StopPhotographyMotion();if(photographySfx!=null)photographySfx.StopAll();}
        internal void TickPhotographySound()
        {
            if(photographySfx==null)return;
            bool silent=PhotographySilent || (!Active && !DirectDarkroom);
            photographySfx.SetMuted(silent);
            if(silent){StopPhotographyMotion();return;}
            for(int i=0;i<photographyMotionUntil.Length;i++)
            {
                // Let the final physical scroll pulse finish across its immediate phase transition.
                bool held=i==0?(Rewinding || Photo.Phase==PhotoChapter.FilmPhase.Rewound):i==1?(bagHeld || Photo.Phase==PhotoChapter.FilmPhase.Tank):tankHeld;
                photographySfx.Loop(i,photographyMotionCue[i],held && Time.unscaledTime<photographyMotionUntil[i],photographyMotionPoint[i]);
            }
        }
    }
    // Runs independently from station Tick, which stops while help, pause or focus gates are active.
    public sealed class PhotographySfxLifecycle:MonoBehaviour
    {
        internal PhotoWorld3D owner;
        bool focused=true,paused;
        void LateUpdate(){if(owner==null)return;if(!focused || paused)owner.StopPhotographySound();else owner.TickPhotographySound();}
        void OnApplicationFocus(bool value){focused=value;if(!value)owner?.StopPhotographySound();}
        void OnApplicationPause(bool value){paused=value;if(value)owner?.StopPhotographySound();}
        void OnDisable(){owner?.StopPhotographySound();}
    }
}
