using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        FiveDayProgress story;
        float nextCheckpoint;
        bool leaveCompletedChapter;
#if UNITY_EDITOR
        [Header("Development")]
        [Tooltip("仅在 Unity Editor 中解除五日入口顺序限制；不会把前置关卡标记为完成，正式构建仍按顺序解锁。")]
        public bool unlockAllChaptersInEditor=true;
#endif
        public StoryDay CurrentDay=>story!=null?story.Day:StoryDay.SpringTape;
        public bool StoryFinished=>story!=null && story.Finished;
        public event System.Action<StoryDay> DayChanged;
        public bool CanEnterDay(int index)
        {
            if(story==null || !story.Available || index<0 || index>=5)return false;
#if UNITY_EDITOR
            if(unlockAllChaptersInEditor)return true;
#endif
            return index<=(int)story.Day && story.CanEnter(index);
        }
        public string ChapterEntryBlockReason(int index)
        {
            if(index<0 || index>=5)return "Unknown chapter";
            if(!houseReady)return "House initialization incomplete";
            if(story==null || !story.Available)return "Chapter save unavailable: "+(story!=null?story.Error:"not loaded");
            if(!CanEnterDay(index))return "Previous chapter is incomplete";
            if(InStation)return "Exit the current chapter first";
            if(transition>0)return "Camera transition is active";
            if(DirectTapeHandling)return "Release the dragged tape first";
            if(safeInspection!=null && safeInspection.BlocksInput)return "Exit inspection first";
            if(index==0 && radioDeck!=null && radioDeck.RecordingError!="")return radioDeck.RecordingError;
            return "";
        }
#if UNITY_EDITOR
        [ContextMenu("Development/Log current entry blockers")]
        void LogEntryBlockers()
        {
            for(int i=0;i<5;i++){var reason=ChapterEntryBlockReason(i);Debug.Log("Chapter "+i+": "+(reason==""?"Entry state ready":reason),this);}
            if(playerCamera==null)return;
            var ray=playerCamera.ViewportPointToRay(new Vector3(.5f,.5f));
            var hit=StudyEntry.PickVisible(ray);
            Debug.Log("Visible interaction hit: "+(hit!=null?hit.name:"none (range, layer or occlusion)"),this);
            foreach(var block in Physics.RaycastAll(ray,2.5f,~0,QueryTriggerInteraction.Collide))
                Debug.Log("Ray hit: "+block.collider.name+" layer="+block.collider.gameObject.layer+" distance="+block.distance.ToString("0.00"),block.collider);
        }
#endif
        // Existing chapter completions are scaffold adapters, not v3 gameplay certification.
        void LoadStoryProgress()
        {
            #if UNITY_EDITOR
            story=FiveDayProgress.Load(unlockAllChaptersInEditor);
#else
            story=FiveDayProgress.Load();
#endif
            if(!story.Available)return;
            try
            {
                for(int i=0;i<chapters.Length;i++)
                {
                    chapters[i].RestoreCheckpoint(story.Checkpoint(i));
                    if(story.IsComplete(i))chapters[i].RestoreStoryCompletion();
                    Done[i]=story.IsComplete(i);
                }
                radioDeck.AdvanceInspection(1);
                radioDeck.SynchronizeCommission();
                radioDeck.RestoreWorkingTape();
                radioDeck.RestoreWorkbench();
            }
            catch(System.Exception e){story.BlockRestore(e);}
        }
        public void SaveStableProgress()
        {
            if(!houseReady || story==null || !story.Available)return;
            radioDeck.SaveRecordingProgress();
            for(int i=0;i<chapters.Length;i++)
            {
                if(!story.CanEnter(i) || story.IsComplete(i))continue;
                if(chapters[i].Completed)CommitStoryCompletion(i);
                else story.SaveCheckpoint(i,chapters[i].CaptureCheckpoint());
            }
        }
        void CommitStoryCompletion(int index)
        {
            if(story==null || story.IsComplete(index))return;
            if(!story.Complete(index,chapters[index].CaptureCheckpoint()))return;
            Done[index]=true;
            DayChanged?.Invoke(story.Day);
            leaveCompletedChapter=InStation && ChapterIndex==index;
            if(index==0)radioDeck.SuspendInput();
        }
        void TickStoryProgress()
        {
            if(Time.unscaledTime>=nextCheckpoint){nextCheckpoint=Time.unscaledTime+1;SaveStableProgress();}
            if(leaveCompletedChapter && InStation && transition<=0)
            {leaveCompletedChapter=false;ExitHouseStation();}
        }
        void OnApplicationPause(bool paused)
        {
            if(!houseReady)return;
            if(paused)
            {
                safeInspection?.Close();radioDeck?.SuspendInput();
                CurrentWorld?.Release("",pointerRay);
                (CurrentWorld as PhotoWorld3D)?.SaveProgress();SaveStableProgress();
            }
            houseFocused=!paused;SuspendHouseInput();RestoreHouseCursor();
        }
        void OnApplicationQuit(){SaveStableProgress();}
    }
}
