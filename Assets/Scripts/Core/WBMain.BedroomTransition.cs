using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        BedroomTransitionOverlay bedroomCover;
        BedroomLoadTrace bedroomTrace;
        bool bedroomLoadFailed;
        public bool ScenePresentationReady=>houseReady && !loadingSpringRoom && !bedroomLoadFailed;
        bool HasBedroom=>springTapeRoom!=null || summerPhotoRoom!=null || narrativeBedroom!=null || DepartureRoom!=null;

        void InitializeBedroomCover()
        {
            if(gameObject.scene.path!=ParkFilmRoute.HouseScenePath)return;
            bedroomCover=BedroomTransitionOverlay.Create(transform,UIFont);
            bedroomCover.ShowLoading("Preparing room");
            SetBedroomLoadingPriority(ThreadPriority.High);
            loadingSpringRoom=true;
        }
        // Keep logical bindings available, but never restore the old room's presentation.
        public void HideLegacyHousePresentation()
        {
            if(houseShell!=null)foreach(Transform child in houseShell.transform)
                if(child!=StudyEntries)child.gameObject.SetActive(false);
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root.name=="Lighting")root.SetActive(false);
        }
        IEnumerator ChangeBedroom(string destination,Func<IEnumerator> load,int debugKey)
        {
            loadingSpringRoom=true;bedroomLoadFailed=false;
            bedroomLoadGeneration++;
            bedroomTrace=new BedroomLoadTrace(System.IO.Path.GetFileNameWithoutExtension(destination));
            SetBedroomLoadingPriority(ThreadPriority.High);
            if(bedroomCover==null)bedroomCover=BedroomTransitionOverlay.Create(transform,UIFont);
            PreparePauseInput();
            if(radioDeck!=null)radioDeck.PrepareSceneInput();
            bedroomCover.ClearError();Room.StopMotion();SuspendHouseInput();SyncInputContext();
            bedroomCover.ShowLoading("Saving progress");
            // Save before unbinding; startup already restored the committed checkpoint.
            if(HasBedroom && !SaveStableProgress())
            {FailBedroomTransition("Could not save before changing bedroom: "+LastSaveError);yield break;}
            bedroomTrace.Mark("save-before-handoff");
            bedroomCover.ShowLoading("Loading bedroom");
            yield return BrushSceneTransition.Cover();
            bedroomCover.SetAlpha(0);
            bedroomTrace.Mark("fade-out");
            // Render a completely covered frame before any scene/object changes.
            int coverFrame=Time.frameCount;
            yield return null;
            // Finish the writing view only after EasyTransition has covered the departing girl.
            if(InStation && ChapterIndex==2 && Current.Completed)CompleteHouseExit();
            bedroomTrace.MarkFrameWait("covered-frame-wait",coverFrame);
            // Release the held load before UnloadSceneAsync; otherwise the async
            // queue can wait forever behind allowSceneActivation=false.
            yield return DrainBedroomPreload(destination);
            bedroomTrace.Mark("preload-handoff");
            var leaving=new List<Scene>();
            if(springTapeRoom!=null)leaving.Add(springTapeRoom.gameObject.scene);
            if(summerPhotoRoom!=null)leaving.Add(summerPhotoRoom.gameObject.scene);
            if(narrativeBedroom!=null)leaving.Add(narrativeBedroom.gameObject.scene);
            if(DepartureRoom!=null)leaving.Add(DepartureRoom.gameObject.scene);
            string error=null;
            try
            {
                LeaveSpringTapeRoom();LeaveSummerPhotoRoom();LeaveNarrativeBedroom();
                if(DepartureRoom!=null){DepartureRoom.Leave(this);DepartureRoom=null;}
                HideLegacyHousePresentation();
                if(DarkroomWorld!=null)DarkroomWorld.Root.SetActive(false);
            }
            catch(Exception ex){error=ex.ToString();}
            if(error!=null){FailBedroomTransition(error);yield break;}
            bedroomTrace.Mark("detach-old-room");
            foreach(var scene in leaving)while(scene.isLoaded)yield return null;
            bedroomTrace.Mark("unload-old-scene");
            var loader=load();
            // The binding iterators yield only Unity async scene operations. Catch their
            // binding failures too, so partial setup never reveals the host room.
            while(true)
            {
                bool more=false;object wait=null;
                try{more=loader.MoveNext();if(more)wait=loader.Current;}
                catch(Exception ex){error=ex.ToString();}
                if(error!=null){FailBedroomTransition(error);yield break;}
                if(!more)break;
                yield return wait;
            }
            bedroomTrace.Mark("load-and-bind-total");
            Physics.SyncTransforms();Room.StopMotion();
            bedroomTrace.Mark("physics-sync-and-stop");
            int readyFrame=Time.frameCount;
            yield return null;
            bedroomTrace.MarkFrameWait("ready-frame-wait",readyFrame);
            RestoreBedroomLoadingPriority();
            loadingSpringRoom=false;SuspendHouseInput();SyncInputContext();
            if(debugKey!=0)DebugEnterChapter(debugKey);
            // Debug entry can position a camera/build objects too; finish it under the cover.
            yield return null;
            if(!BrushSceneTransition.ExternalLoadActive)yield return BrushSceneTransition.Reveal();
            bedroomTrace.Mark("brush-reveal-or-host-handoff");bedroomTrace.End();
            if(!loadingSpringRoom && springTapeRoom!=null && CurrentDay==StoryDay.SpringTape)StartCoroutine(PreloadSummerBedroom());
        }
        void FailBedroomTransition(string reason)
        {
            bedroomLoadFailed=true;loadingSpringRoom=true;
            bedroomTrace?.End("failed");ReleaseBedroomLoading();
            Debug.LogError("Bedroom transition failed: "+reason,this);
            if(bedroomCover==null)bedroomCover=BedroomTransitionOverlay.Create(transform,UIFont);
            bedroomCover.SetAlpha(0);
            BrushSceneTransition.ShowFailure(reason,gameObject.scene.path);
            SyncInputContext();
            PlayerInputPolicy.SetCursor(true);
        }
    }
}
