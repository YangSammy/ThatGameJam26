using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        AsyncOperation summerPreload;
        int bedroomLoadGeneration;
        bool summerPreloadRequested,ownsLoadingPriority;
        ThreadPriority previousLoadingPriority,ownedLoadingPriority;

        void SetBedroomLoadingPriority(ThreadPriority priority)
        {
            if(!ownsLoadingPriority){previousLoadingPriority=Application.backgroundLoadingPriority;ownsLoadingPriority=true;}
            ownedLoadingPriority=priority;Application.backgroundLoadingPriority=priority;
        }
        void RestoreBedroomLoadingPriority()
        {
            if(!ownsLoadingPriority)return;
            if(Application.backgroundLoadingPriority==ownedLoadingPriority)Application.backgroundLoadingPriority=previousLoadingPriority;
            ownsLoadingPriority=false;
        }
        IEnumerator PreloadSummerBedroom()
        {
            if(summerPreloadRequested)yield break;
            summerPreloadRequested=true;
            int generation=bedroomLoadGeneration;
            // Give the first room and input handoff a moment to settle. This delay is
            // outside the loading screen and never delays an actual scene transition.
            yield return new WaitForSecondsRealtime(2);
            if(generation!=bedroomLoadGeneration || loadingSpringRoom || springTapeRoom==null || CurrentDay!=StoryDay.SpringTape)yield break;
            if(!Application.CanStreamedLevelBeLoaded(SummerPhotoRoom.ScenePath) || SceneManager.GetSceneByPath(SummerPhotoRoom.ScenePath).isLoaded)yield break;
            var trace=new BedroomLoadTrace("summer-preload");
            try
            {
                SetBedroomLoadingPriority(ThreadPriority.Low);
                summerPreload=SceneManager.LoadSceneAsync(SummerPhotoRoom.ScenePath,LoadSceneMode.Additive);
                summerPreload.allowSceneActivation=false;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged+=ReleasePreloadBeforePlayExit;
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload+=ReleaseBedroomLoading;
#endif
            }
            catch(Exception ex)
            {
                ReleaseBedroomLoading();
                Debug.LogWarning("[BedroomLoad] Summer preload unavailable; normal load retained. "+ex.Message);
                yield break;
            }
            var pending=summerPreload;
            while(summerPreload==pending && pending.progress<.9f && !pending.isDone)yield return null;
            trace.End(summerPreload==pending?"assets ready; activation deferred":"handoff");
            if(!loadingSpringRoom)RestoreBedroomLoadingPriority();
        }
        IEnumerator DrainBedroomPreload(string destination)
        {
            var pending=summerPreload;if(pending==null)yield break;
            summerPreload=null;
            UnhookBedroomPreload();
            // A scene held at 0.9 blocks Unity's async operation queue. Release it
            // BEFORE requesting any unload or another scene load, including debug routes.
            pending.allowSceneActivation=true;
            using(bedroomTrace?.Measure("preload-remaining-and-activation"))yield return pending;
            if(destination!=SummerPhotoRoom.ScenePath)
            {
                var scene=SceneManager.GetSceneByPath(SummerPhotoRoom.ScenePath);
                if(scene.isLoaded)
                {
                    foreach(var root in scene.GetRootGameObjects())root.SetActive(false);
                    using(bedroomTrace?.Measure("discard-unused-preload"))yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }
        void ReleaseBedroomLoading()
        {
            UnhookBedroomPreload();
            // Never leave an activation barrier behind after cancellation/destruction.
            if(summerPreload!=null){summerPreload.allowSceneActivation=true;summerPreload=null;}
            RestoreBedroomLoadingPriority();
        }
        public IEnumerator PrepareSceneDeparture()
        {
            // Cancel delayed preload starts and drain any existing activation barrier.
            bedroomLoadGeneration++;
            yield return DrainBedroomPreload(null);
            RestoreBedroomLoadingPriority();
        }
        void UnhookBedroomPreload()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged-=ReleasePreloadBeforePlayExit;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload-=ReleaseBedroomLoading;
#endif
        }
#if UNITY_EDITOR
        void ReleasePreloadBeforePlayExit(UnityEditor.PlayModeStateChange state)
        {if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)ReleaseBedroomLoading();}
#endif
    }
}
