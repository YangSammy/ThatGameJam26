using System;
using System.Collections;
using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemoryWhitebox
{
    // Presentation and single-scene navigation only. WBMain still owns bedroom binding/saves.
    public sealed class BrushSceneTransition : MonoBehaviour
    {
        static BrushSceneTransition instance;
        TransitionSettings settings,brushSettings;
        Canvas canvas;
        GameObject visual;
        Image blocker;
        bool busy,covered,externalLoad;
        string destination,redirect,failure;
        public static bool IsBusy=>instance!=null && instance.busy;
        public static bool IsCovered=>instance!=null && instance.covered;
        public static bool ExternalLoadActive=>instance!=null && instance.externalLoad;
        public static string Failure=>instance!=null?instance.failure:null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){instance=null;}

        static BrushSceneTransition Ensure()
        {
            if(instance!=null)return instance;
            var config=Resources.Load<TransitionSettings>("BrushSceneTransition");
            if(config==null || config.transitionIn==null || config.transitionOut==null)
                throw new InvalidOperationException("BrushSceneTransition resource or Brush prefabs are missing.");
            var root=new GameObject("Brush scene transition",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            instance=root.AddComponent<BrushSceneTransition>();instance.settings=config;instance.brushSettings=config;
            instance.canvas=root.GetComponent<Canvas>();instance.canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            instance.canvas.sortingOrder=32760;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=config.refrenceResolution;scaler.matchWidthOrHeight=.5f;
            var shield=new GameObject("Input shield",typeof(RectTransform),typeof(Image));shield.transform.SetParent(root.transform,false);
            var rect=(RectTransform)shield.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            instance.blocker=shield.GetComponent<Image>();instance.blocker.color=Color.clear;instance.blocker.raycastTarget=true;
            instance.canvas.enabled=false;return instance;
        }

        public static IEnumerator Cover()
        {
            var owner=Ensure();
            if(!owner.busy && !owner.covered)owner.settings=owner.brushSettings;
            return owner.CoverRoutine();
        }
        public static IEnumerator Reveal(){return Ensure().RevealRoutine();}

        IEnumerator CoverRoutine()
        {
            if(covered)yield break;
            busy=true;canvas.enabled=true;blocker.color=Color.clear;
            yield return Animate(settings.transitionIn);
            // An opaque guard prevents seams/resolution gaps while objects are being replaced.
            blocker.color=Color.black;covered=true;
            yield return null;
        }

        IEnumerator RevealRoutine()
        {
            if(!covered)yield break;
            failure=null;
            // Create OUT at its fully-covered first frame before removing the opaque guard.
            yield return Animate(settings.transitionOut,true);
            ClearVisual();blocker.color=Color.clear;canvas.enabled=false;covered=false;
            busy=false;
            if(WBMain.Instance!=null){WBMain.Instance.SuspendHouseInput();WBMain.Instance.SyncInputContext();}
            var park=FindObjectOfType<ParkPhotoPrototype>();if(park!=null)park.ResumeFromPauseMenu();
        }

        IEnumerator Animate(GameObject prefab,bool revealing=false)
        {
            ClearVisual();visual=Instantiate(prefab,transform,false);
            visual.transform.localScale=new Vector3(settings.flipX?-1:1,settings.flipY?-1:1,1);
            var animator=visual.GetComponent<Animator>();
            // Unity reports an infinite state length when Animator.speed is zero.
            // Read the duration at normal speed before taking over manual sampling.
            animator.updateMode=AnimatorUpdateMode.UnscaledTime;animator.speed=1;
            animator.Rebind();animator.Update(0);
            var state=animator.GetCurrentAnimatorStateInfo(0);
            animator.speed=0;
            animator.Play(state.fullPathHash,0,0);animator.Update(0);
            if(revealing)blocker.color=Color.clear;
            float duration=Mathf.Max(.01f,state.length/Mathf.Max(.01f,settings.transitionSpeed));
            // Capture time AFTER initialization; a long scene-load frame must not skip OUT.
            double started=Time.realtimeSinceStartupAsDouble;
            float progress=0;
            while(progress<1)
            {
                yield return null;
                progress=Mathf.Clamp01((float)(Time.realtimeSinceStartupAsDouble-started)/duration);
                animator.Play(state.fullPathHash,0,progress);animator.Update(0);
            }
        }

        void ClearVisual(){if(visual!=null){visual.SetActive(false);Destroy(visual);visual=null;}}

        public static void LoadScene(string path)
        {
            if(!Application.CanStreamedLevelBeLoaded(path))throw new ArgumentException("Scene is not in Build Settings: "+path);
            var owner=Ensure();
            if(owner.externalLoad || (owner.busy && string.IsNullOrEmpty(owner.failure)))return;
            var config=owner.brushSettings;
            if(config==null || config.transitionIn==null || config.transitionOut==null)
                throw new InvalidOperationException("Scene transition settings or animation prefabs are missing.");
            owner.settings=config;
            owner.destination=path;owner.failure=null;owner.externalLoad=true;owner.busy=true;
            owner.StartCoroutine(owner.LoadRoutine());
        }

        // A destination can reject entry in Start (e.g. a locked photography chapter).
        public static void RedirectTo(string path)
        {
            if(ExternalLoadActive){instance.redirect=path;return;}
            LoadScene(path);
        }

        IEnumerator LoadRoutine()
        {
            yield return CoverRoutine();
            if(WBMain.Instance!=null)yield return WBMain.Instance.PrepareSceneDeparture();
            while(true)
            {
                AsyncOperation operation=null;
                try{operation=SceneManager.LoadSceneAsync(destination,LoadSceneMode.Single);}
                catch(Exception ex){ShowFailure(ex.Message,destination);}
                if(operation==null){externalLoad=false;yield break;}
                yield return operation;
                // sceneLoaded is before Start. House also loads/binds an additive bedroom.
                yield return null;
                double waiting=Time.realtimeSinceStartupAsDouble;
                while(string.IsNullOrEmpty(redirect) && !DestinationReady())
                {
                    if(!string.IsNullOrEmpty(failure)){externalLoad=false;yield break;}
                    if(Time.realtimeSinceStartupAsDouble-waiting>120)
                    {ShowFailure("The scene did not finish initializing.",destination);externalLoad=false;yield break;}
                    yield return null;
                }
                if(!string.IsNullOrEmpty(redirect))
                {destination=redirect;redirect=null;continue;}
                break;
            }
            Physics.SyncTransforms();yield return null;
            yield return RevealRoutine();externalLoad=false;
        }

        bool DestinationReady()
        {
            // Bedroom binding changes the active scene; readiness belongs to the requested host.
            if(destination==ParkFilmRoute.HouseScenePath)
            {
                var host=WBMain.Instance;
                if(host==null || !host.isActiveAndEnabled){ShowFailure("House initialization failed.",destination);return false;}
                return host.ScenePresentationReady;
            }
            if(destination==ParkFilmRoute.ParkScenePath)
            {
                var park=FindObjectOfType<ParkPhotoPrototype>();
                if(park==null || !park.isActiveAndEnabled){ShowFailure("Park initialization failed.",destination);return false;}
                return park.ScenePresentationReady;
            }
            return true; // Main menu's synchronous Awake/Start finished before the extra frame.
        }

        public static void ShowFailure(string reason,string retryScene)
        {
            var owner=Ensure();owner.failure=reason;owner.destination=retryScene;
            owner.busy=true;owner.covered=true;owner.canvas.enabled=true;owner.blocker.color=Color.black;
            owner.ClearVisual();PlayerInputPolicy.SetCursor(true);
        }

        void OnGUI()
        {
            if(string.IsNullOrEmpty(failure))return;
            GUI.depth=-32768;PlayerInputPolicy.SetCursor(true);
            GUILayout.BeginArea(new Rect(Screen.width/2-220,Screen.height/2-95,440,190),GUI.skin.box);
            GUILayout.Label("Could not finish loading. Your saved progress is kept.");
            GUILayout.Label(failure);
            if(!externalLoad && GUILayout.Button("Retry"))LoadScene(destination);
            if(!externalLoad && Application.CanStreamedLevelBeLoaded(MainMenuSaveAccess.ScenePath) && GUILayout.Button("Main Menu"))LoadScene(MainMenuSaveAccess.ScenePath);
            GUILayout.EndArea();
        }

        void OnDestroy(){if(instance==this)instance=null;}
    }
}
