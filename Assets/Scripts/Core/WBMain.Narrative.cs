using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        bool springPreview;
        bool SpringActive=>story!=null && story.Available && (story.Day==StoryDay.SpringDeparture || story.Finished || springPreview);
        bool TickSpring(float dt)
        {
            if(!SpringActive || InStation)return false;
            var ray=Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
            bool handled=((EndingWorld3D)worlds[4]).StepHouse(dt,ray,Input.GetMouseButtonDown(0) && !DirectTapeHandling && !DirectDarkroomActive);
            if(story.Finished){Room.StopMotion();return true;}
            if(DepartureRoom!=null){if(!handled)Room.WalkTick(dt);return true;}
            return handled;
        }
        void CutBackFromWinter()
        {
            if(!InStation || ChapterIndex!=3)return;
            ((PaintingWorld3D)worlds[3]).FreezeFinalFrame();
            CurrentWorld.CancelPointerGesture();CurrentWorld.Deactivate();
            InStation=false;ShowHelp=false;awaitRelease=false;transition=0;returningToRoom=false;leaveCompletedChapter=false;
            SetStudyPresentation(-1);Room.Camera.transform.SetPositionAndRotation(roomEye,roomRotation);Room.Camera.fieldOfView=roomFov;
            radioDeck?.SetStationActive(true);audioSource.Stop();Room.EnableLook(true);SuspendHouseInput();SyncInputContext();
        }
        InputContextId NarrativeContext=>ChapterIndex==2?InputContextId.AutumnWriting:ChapterIndex==3?
            (((PaintingChapter)Current).Phase>=2?InputContextId.WardWalk:((PaintingChapter)Current).BookOpen?InputContextId.WinterBook:InputContextId.WinterObserve):InputContextId.Station;
    }
}
