using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class BrushTransitionChecks
    {
        public static string StartRuntime()
        {
            if(!Application.isPlaying || !GameSavePaths.ResetOnEditorPlay)throw new Exception("Use an isolated reset-on-play session.");
            var go=new GameObject("Brush transition verification");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BrushTransitionCheckRunner>().StartChecks();
            return "Runtime checks started; results: QA/brush-transition-checks.txt";
        }
        public static string AssetsAndRoutes()
        {
            var type=typeof(WBMain).Assembly.GetType("MemoryWhitebox.BrushSceneTransition");
            if(type==null)throw new Exception("Missing persistent Brush transition controller.");
            var settings=Resources.Load<EasyTransition.TransitionSettings>("BrushSceneTransition");
            if(settings==null || settings.transitionIn==null || settings.transitionOut==null)
                throw new Exception("Brush resource must include both imported animation prefabs in player builds.");
            foreach(var prefab in new[]{settings.transitionIn,settings.transitionOut})
            {
                var anim=prefab.GetComponent<Animator>();
                if(anim==null || anim.runtimeAnimatorController==null || anim.runtimeAnimatorController.animationClips.Any(c=>c.isLooping))
                    throw new Exception("Brush animation must be present and non-looping: "+prefab.name);
            }
            var bypasses=System.IO.Directory.GetFiles("Assets/Scripts","*.cs",System.IO.SearchOption.AllDirectories)
                .Where(p=>!p.EndsWith("BrushSceneTransition.cs") && !p.EndsWith("WBMain.SpringBedroom.cs") && !p.EndsWith("WBMain.BedroomPreload.cs"))
                .Where(p=>System.IO.File.ReadAllText(p).Contains("SceneManager.LoadScene")).ToArray();
            if(bypasses.Length>0)throw new Exception("Scene navigation bypasses Brush: "+string.Join(", ",bypasses));
            return "PASS: Brush assets included, non-looping animations, all scene navigation centralized.";
        }
    }

    public sealed class BrushTransitionCheckRunner:MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        string step;
        void Check(bool condition,string label)
        {
            if(!condition)throw new Exception(label);
            results.Add("PASS "+label);File.WriteAllLines("QA/brush-transition-checks.txt",results);
        }
        public void StartChecks(){Directory.CreateDirectory("QA");StartCoroutine(Guard(Flow()));}
        IEnumerator Guard(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                object next=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}
                catch(Exception ex){failure=ex;}
                if(failure!=null){results.Add("FAIL "+step+": "+failure);File.WriteAllLines("QA/brush-transition-checks.txt",results);Time.timeScale=1;yield break;}
                if(!more){stack.Pop();continue;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}
                yield return next;
            }
            results.Add("COMPLETE");File.WriteAllLines("QA/brush-transition-checks.txt",results);Destroy(gameObject);
        }
        IEnumerator WaitTransition(string label,string expectedScene)
        {
            step=label;double start=Time.realtimeSinceStartupAsDouble;bool sawCover=false;
            while(BrushSceneTransition.IsBusy)
            {
                if(!string.IsNullOrEmpty(BrushSceneTransition.Failure))throw new Exception(BrushSceneTransition.Failure);
                sawCover|=BrushSceneTransition.IsCovered;
                if(Time.realtimeSinceStartupAsDouble-start>180)throw new Exception("Transition timeout");
                yield return null;
            }
            Check(sawCover,label+" used opaque Brush hold");
            Check(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(expectedScene).isLoaded,label+" destination loaded");
            if(expectedScene==ParkFilmRoute.HouseScenePath)Check(WBMain.Instance!=null && WBMain.Instance.ScenePresentationReady,label+" host/objects ready before reveal completes");
            if(expectedScene==ParkFilmRoute.ParkScenePath)Check(FindObjectOfType<ParkPhotoPrototype>().ScenePresentationReady,label+" park ready");
        }
        IEnumerator Flow()
        {
            step="assets";Check(BrushTransitionChecks.AssetsAndRoutes().StartsWith("PASS"),"asset and route audit");
            step="paused cover";Time.timeScale=0;
            yield return BrushSceneTransition.Cover();Check(BrushSceneTransition.IsCovered && BrushSceneTransition.IsBusy,"Brush covers at timeScale zero");
            yield return new WaitForSecondsRealtime(1.5f);
            Check(BrushSceneTransition.IsCovered && BrushSceneTransition.IsBusy,"coverage persists beyond original plugin auto-destroy time");
            ScreenCapture.CaptureScreenshot("QA/brush-covered.png");
            yield return BrushSceneTransition.Reveal();Check(!BrushSceneTransition.IsBusy,"Brush reveals at timeScale zero");Time.timeScale=1;
            step="House";BrushSceneTransition.LoadScene(ParkFilmRoute.HouseScenePath);
            // A second click must not replace the in-flight destination.
            BrushSceneTransition.LoadScene(MainMenuSaveAccess.ScenePath);
            yield return WaitTransition("menu to house / duplicate request",ParkFilmRoute.HouseScenePath);
            yield return new WaitForSecondsRealtime(3);
            foreach(int key in new[]{2,3,4,6,1})
            {
                var host=WBMain.Instance;host.SendMessage("OnApplicationFocus",true);
                for(int i=0;i<3;i++)host.AdvanceHouseTransition(1);
                host.ReleaseHouseReturnInput(true);host.InputContext.ContinueGesture();
                Check(host.DebugEnterChapter(key),"debug entry "+key+" accepted");
                yield return WaitTransition("bedroom "+key,ParkFilmRoute.HouseScenePath);
                Check(host.ScenePresentationReady,"bedroom "+key+" objects ready");
            }
            // Spring starts a deferred summer preload. Leaving must drain its activation barrier.
            yield return new WaitForSecondsRealtime(3);
            BrushSceneTransition.LoadScene(MainMenuSaveAccess.ScenePath);
            yield return WaitTransition("leave with background preload",MainMenuSaveAccess.ScenePath);
            FiveDayProgress.DevelopmentSession=true;
            BrushSceneTransition.LoadScene(ParkFilmRoute.ParkScenePath);
            yield return WaitTransition("park entry",ParkFilmRoute.ParkScenePath);
            BrushSceneTransition.LoadScene(ParkFilmRoute.HouseScenePath);
            yield return WaitTransition("park return",ParkFilmRoute.HouseScenePath);
            FiveDayProgress.DevelopmentSession=false;
            BrushSceneTransition.LoadScene(ParkFilmRoute.ParkScenePath);
            yield return WaitTransition("locked park redirects under cover",ParkFilmRoute.HouseScenePath);
            BrushSceneTransition.ShowFailure("Verification: simulated initialization failure",MainMenuSaveAccess.ScenePath);
            Check(BrushSceneTransition.IsCovered && BrushSceneTransition.IsBusy,"failure keeps scene hidden");
            BrushSceneTransition.LoadScene(MainMenuSaveAccess.ScenePath);
            yield return WaitTransition("retry after initialization failure",MainMenuSaveAccess.ScenePath);
        }
    }
}
