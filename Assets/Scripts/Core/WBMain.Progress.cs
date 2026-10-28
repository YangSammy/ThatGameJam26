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
            return story.CanEnter(index);
        }
        // Existing chapter completions are scaffold adapters, not v3 gameplay certification.
        void LoadStoryProgress()
        {
            story=FiveDayProgress.Load();
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
            }
            catch(System.Exception e){story.BlockRestore(e);}
        }
        public void SaveStableProgress()
        {
            if(!houseReady || story==null || !story.Available)return;
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
