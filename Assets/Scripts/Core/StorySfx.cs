using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    [RequireComponent(typeof(TapeSfxPlayer))]
    public sealed class StorySfx : MonoBehaviour
    {
        const float MotionHoldSeconds=.16f;
        static StorySfx current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static event System.Action<int,bool> CueRequested;
#endif
        readonly int[] motionIds=new int[4];
        readonly float[] motionExpires=new float[4];
        TapeSfxPlayer player;
        bool paused;

        static bool Silent => !Application.isPlaying || !Application.isFocused ||
            (WBMain.Instance!=null && (WBMain.Instance.Muted || WBMain.Instance.ShowHelp));

        static StorySfx Active()
        {
            if(Silent)return null;
            if(current==null)
            {
                current=FindObjectOfType<StorySfx>();
                if(current==null)current=new GameObject("Story interaction audio").AddComponent<StorySfx>();
            }
            if(current.paused)return null;
            current.BindPlayer();
            current.player.SetMuted(false);
            return current;
        }

        void BindPlayer()
        {
            if(player!=null)return;
            player=GetComponent<TapeSfxPlayer>();
            player.bank=Resources.Load<TapeSfxBank>("StorySfxBank");
        }

        public static void Shot(int id,Vector3 position)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CueRequested?.Invoke(id,false);
#endif
            var audio=Active();
            if(audio!=null)audio.player.Shot(id,position);
        }

        public static void Motion(int id,Vector3 position)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CueRequested?.Invoke(id,true);
#endif
            var audio=Active();
            if(audio!=null)audio.PlayMotion(id,position);
        }

        public static void Stop(int id)
        {
            if(current==null || current.player==null)return;
            for(int i=0;i<current.motionIds.Length;i++)
                if(current.motionIds[i]==id)current.ReleaseMotion(i);
        }

        void PlayMotion(int id,Vector3 position)
        {
            int free=-1;
            for(int i=0;i<motionIds.Length;i++)
            {
                if(motionIds[i]==id){free=i;break;}
                if(free<0 && motionIds[i]==0)free=i;
            }
            if(free<0)return;
            motionIds[free]=id;
            motionExpires[free]=Time.unscaledTime+MotionHoldSeconds;
            player.Loop(free,id,true,position);
        }

        void ReleaseMotion(int channel)
        {
            motionIds[channel]=0;
            player.StopLoop(channel,true);
        }

        void LateUpdate()
        {
            if(player==null)return;
            if(Silent || paused){StopAll();return;}
            for(int i=0;i<motionIds.Length;i++)
                if(motionIds[i]!=0 && Time.unscaledTime>=motionExpires[i])ReleaseMotion(i);
        }

        void StopAll()
        {
            if(player!=null)player.SetMuted(true);
            System.Array.Clear(motionIds,0,motionIds.Length);
        }

        void OnEnable(){SceneManager.activeSceneChanged+=OnSceneChanged;}
        void OnDisable(){SceneManager.activeSceneChanged-=OnSceneChanged;StopAll();}
        void OnSceneChanged(Scene previous,Scene next){StopAll();}
        void OnApplicationFocus(bool focused){if(!focused)StopAll();}
        void OnApplicationPause(bool value){paused=value;if(value)StopAll();}
    }
}
