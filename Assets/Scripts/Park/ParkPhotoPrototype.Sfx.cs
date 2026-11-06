using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        // PhotographySfxBank is a TapeSfxBank asset; playback never owns camera/save state.
        TapeSfxBank photographySfxBank;
        TapeSfxPlayer photographySfx;
        bool photographySfxLoaded;
        float leaderSfxLease;
        bool automaticFilmPanelPause,automaticShutterTail;
        float opticalShutterDeadline;
        AudioSource opticalShutterVoice;
        bool CanPlayPhotographySfx=>Application.isPlaying && isActiveAndEnabled && focused && !applicationPaused && !Paused && !changingScene;
        Vector3 PhotographySfxPosition=>view!=null?view.transform.position+view.transform.forward*.3f:transform.position;
        bool EnsurePhotographySfx(int cue)
        {
            if(!CanPlayPhotographySfx)return false;
            if(!photographySfxLoaded)
            {photographySfxLoaded=true;photographySfxBank=Resources.Load<TapeSfxBank>("PhotographySfxBank");}
            if(photographySfxBank==null || photographySfxBank.Find(cue)?.clip==null)return false;
            if(photographySfx==null)
            {
                foreach(var player in GetComponents<TapeSfxPlayer>())if(player.bank==photographySfxBank){photographySfx=player;break;}
                if(photographySfx==null)photographySfx=gameObject.AddComponent<TapeSfxPlayer>();
                photographySfx.bank=photographySfxBank;
            }
            photographySfx.SetMuted(false);return true;
        }
        bool PlayPhotographyCue(int cue)
        {
            if(!EnsurePhotographySfx(cue))return false;
            photographySfx.Shot(cue,PhotographySfxPosition);return true;
        }
        static int LoadingSfxCue(int step)
        {
            switch(step){case 1:return 23;case 6:return 123;case 2:return 22;case 4:return 24;case 5:case 7:return 25;default:return -1;}
        }
        void PlayLoadingSfx(int step)
        {int cue=LoadingSfxCue(step);if(cue==123){if(!PlayPhotographyCue(123))PlayPhotographyCue(23);}else if(cue>=0)PlayPhotographyCue(cue);}
        void PhotographyModeSfx(PhotographyMode before,PhotographyMode after)
        {
            if(before==after)return;
            StopPhotographyLoops();
            if(before==PhotographyMode.Exploration && after!=PhotographyMode.Exploration)PlayPhotographyCue(21);
        }
        void LeaderMovementSfx(Vector3 before,Vector3 after)
        {
            if(dragPart!="leader" || (after-before).sqrMagnitude<.000001f)return;
            if(!EnsurePhotographySfx(24))return;
            leaderSfxLease=.08f;photographySfx.Loop(0,24,true,PhotographySfxPosition);
        }
        void StopPhotographyLoops()
        {
            leaderSfxLease=0;
            if(photographySfx!=null)photographySfx.StopLoop(0,true);
        }
        void StopPhotographySfx()
        {
            automaticShutterTail=false;opticalShutterDeadline=0;
            if(opticalShutterVoice!=null)opticalShutterVoice.Stop();opticalShutterVoice=null;
            StopPhotographyLoops();
            if(photographySfx!=null){photographySfx.StopAll();photographySfx.SetMuted(true);}
        }
        bool ArmAutomaticShutterTail(float now)
        {
            automaticShutterTail=opticalShutterDeadline>now;
            return automaticShutterTail;
        }
        bool AutomaticShutterTailAlive(float now)=>automaticShutterTail && now<opticalShutterDeadline;
        void PausePhotographySfx()
        {
            if(!automaticFilmPanelPause || !ArmAutomaticShutterTail(Time.unscaledTime))
            {StopPhotographySfx();return;}
            StopPhotographyLoops();
            // The automatic final panel may retain only the optical voice already started.
            // Keep the player unmuted for that voice; Paused still prohibits every new cue.
            if(photographySfx!=null)
                foreach(var source in photographySfx.GetComponentsInChildren<AudioSource>())
                    if(source!=opticalShutterVoice)source.Stop();
        }
        void TickPhotographySfx(float dt)
        {
            if(!CanPlayPhotographySfx)
            {
                if(Application.isPlaying && isActiveAndEnabled && focused && !applicationPaused && !changingScene && Paused && AutomaticShutterTailAlive(Time.unscaledTime))
                {StopPhotographyLoops();return;}
                StopPhotographySfx();return;
            }
            if(photographySfx!=null)photographySfx.SetMuted(false);
            leaderSfxLease=Mathf.Max(0,leaderSfxLease-Mathf.Max(0,dt));
            if(leaderSfxLease<=0 || dragPart!="leader" || photoMode!=PhotographyMode.Body)StopPhotographyLoops();
            // Do not begin advice while the input/presentation handoff is still settling.
            // Target metering acknowledges an explicit measurement, not elapsed panel time.
        }
        bool StepFocusControl(float scroll)
        {
            float before=focusDistance;
            focusDistance=ParkCameraResponse.ScrollFocus(focusDistance,scroll,focusStep,minimumFocus,maximumFocus);
            if(Mathf.Approximately(before,focusDistance) || Mathf.Abs(scroll)<=.01f)return false;
            PlayInstrumentClick();return true;
        }
        void PlayOpticalShutterSfx()
        {
            if(!CanPlayPhotographySfx)return;
            opticalShutterDeadline=0;automaticShutterTail=false;opticalShutterVoice=null;
            if(PlayPhotographyCue(27))
            {
                var cue=photographySfxBank.Find(27);
                opticalShutterDeadline=Time.unscaledTime+cue.duration;
                foreach(var source in photographySfx.GetComponentsInChildren<AudioSource>())
                    if(source.isPlaying && source.clip==cue.clip)opticalShutterVoice=source;
            }
            else if(sound!=null && shutter!=null)
            {
                sound.PlayOneShot(shutter,.6f);opticalShutterVoice=sound;
                opticalShutterDeadline=Time.unscaledTime+shutter.length/Mathf.Max(.1f,Mathf.Abs(sound.pitch));
            }
        }
    }
}
