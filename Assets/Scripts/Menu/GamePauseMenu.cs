using UnityEngine;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GamePauseMenu:MonoBehaviour
    {
        public static volatile bool Active;
        static GamePauseMenu instance;
        static int resumedFrame=-1,openedFrame=-1;
        public static bool BlocksGameplay=>Active || BrushSceneTransition.IsBusy || resumedFrame==Time.frameCount;
        Animator[] animators;float[] speeds;
        bool loading;
        float previousScale;bool previousAudioPause;string message="";float volume;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Active=false;instance=null;resumedFrame=-1;openedFrame=-1;}
        public static bool PollEscape()
        {
            // Escape always belongs to pause, including photographic controls.
            // Active/resumedFrame prevent another controller consuming the same press.
            if(!BlocksGameplay && Input.GetKeyDown(KeyCode.Escape))
            {
                Open();
            }
            return BlocksGameplay;
        }
        public static void Open()
        {
            if(Active || BrushSceneTransition.IsBusy)return;
            if(instance==null){var go=new GameObject("Pause Menu");DontDestroyOnLoad(go);instance=go.AddComponent<GamePauseMenu>();}
            if(WBMain.Instance!=null)WBMain.Instance.PreparePauseInput();
            var park=FindObjectOfType<ParkPhotoPrototype>();if(park!=null)park.PreparePauseInput();
            instance.previousScale=Time.timeScale;instance.previousAudioPause=AudioListener.pause;
            instance.volume=AudioListener.volume;instance.message="";Active=true;openedFrame=Time.frameCount;
            instance.animators=FindObjectsOfType<Animator>();instance.speeds=new float[instance.animators.Length];
            for(int i=0;i<instance.animators.Length;i++){instance.speeds[i]=instance.animators[i].speed;if(instance.animators[i].GetComponentInParent<Canvas>()==null)instance.animators[i].speed=0;}
            Time.timeScale=0;AudioListener.pause=true;PlayerInputPolicy.SetCursor(true);
        }
        public void Resume()
        {
            resumedFrame=Time.frameCount;Active=false;
            for(int i=0;i<animators.Length;i++)if(animators[i]!=null)animators[i].speed=speeds[i];
            var park=FindObjectOfType<ParkPhotoPrototype>();if(park!=null)park.ResumeFromPauseMenu();
            Time.timeScale=previousScale;AudioListener.pause=previousAudioPause;
            if(WBMain.Instance!=null){WBMain.Instance.SuspendHouseInput();WBMain.Instance.SyncInputContext();}
        }
        void Update(){if(Active && openedFrame!=Time.frameCount && !loading && Input.GetKeyDown(KeyCode.Escape))Resume();}
        bool Save()
        {
            if(WBMain.Instance==null){var park=FindObjectOfType<ParkPhotoPrototype>();bool saved=park!=null && park.SaveForPauseMenu();message=saved?"Game saved.":"Save unavailable. Wait for the photo to finish and retry.";return saved;}
            bool ok=WBMain.Instance.SaveStableProgress();message=ok?"Game saved.":"Save failed: "+WBMain.Instance.LastSaveError;return ok;
        }
        bool CanReturn(){if(Application.CanStreamedLevelBeLoaded(MainMenuSaveAccess.ScenePath))return true;message="Main menu scene is unavailable.";return false;}
        void OnGUI()
        {
            if(!Active)return;
            PlayerInputPolicy.SetCursor(true);
            GUILayout.BeginArea(new Rect(Screen.width/2-170,Screen.height/2-190,340,380),GUI.skin.box);
            GUILayout.Label("Paused");
            if(loading){GUILayout.Label("Loading main menu...");GUILayout.EndArea();return;}
            if(GUILayout.Button("Resume"))Resume();
            GUILayout.Label("Volume  "+Mathf.RoundToInt(volume*100)+"%");
            float next=GUILayout.HorizontalSlider(volume,0,1);
            if(next!=volume){volume=next;var prefs=(GamePreferences.Current??GamePreferences.Defaults()).Copy();prefs.volume=volume;GamePreferences.Save(prefs);}
            if(GUILayout.Button("Save Game"))Save();
            if(GUILayout.Button("Main Menu") && CanReturn() && Save())
            {loading=true;try{BrushSceneTransition.LoadScene(MainMenuSaveAccess.ScenePath);Resume();loading=false;}catch(System.Exception e){loading=false;message="Could not load main menu: "+e.Message;}}
            if(GUILayout.Button("Quit Game") && Save()){
                Resume();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            }
            GUILayout.Label(message);GUILayout.EndArea();
        }
    }
}
