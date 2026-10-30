#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        bool PollDebugChapterKeys()
        {
            KeyCode[] row={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6};
            KeyCode[] pad={KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5,KeyCode.Keypad6};
            for(int i=0;i<6;i++)if(Input.GetKeyDown(row[i])||Input.GetKeyDown(pad[i])){DebugEnterChapter(i+1);return true;}
            return false;
        }
        // Development saves only. Narrative shortcuts restart their selected test segment;
        // tape/audio and darkroom material progress are deliberately retained.
        public bool DebugEnterChapter(int key)
        {
            if(!houseReady || key<1 || key>6)return false;
            safeInspection?.Close();radioDeck.DismissRecordingSuccess();
            radioDeck.CloseWorkbench();radioDeck.CloseRecording();if(DirectTapeHandling)radioDeck.PutBack();
            DarkroomWorld?.CancelInteraction();DarkroomWorld?.SetDirectDarkroom(false);
            CurrentWorld?.CancelPointerGesture();
            if(!SaveStableProgress()){cameraRouteNotice="DEV: save failed. Stay here and retry.";cameraRouteNoticeUntil=Time.unscaledTime+6;return false;}
            if(InStation)
            {
                CompleteHouseExit();Room.Camera.transform.SetPositionAndRotation(roomEye,roomRotation);Room.Camera.fieldOfView=roomFov;
            }
            transition=0;returningToRoom=awaitRelease=leaveCompletedChapter=false;ShowHelp=false;springPreview=false;
            Room.StopMotion();Room.EnableLook(true);
            if(!FiveDayProgress.DevelopmentSession){FiveDayProgress.DevelopmentSession=true;story=FiveDayProgress.LoadForSession();}
            int index=key<=4?key-1:key==5?3:4;
            if(index>=2)
            {
                worlds[index].Dispose();CreateHouseChapter(index);BindChapterData();playerHands.Register(index,worlds[index]);
                if(key==5)((PaintingChapter)chapters[3]).RestoreCheckpoint("{\"version\":2,\"phase\":2,\"shapes\":[true,true,true,true,true],\"key\":true,\"high\":true,\"crate\":4}");
            }
            if(!story.PrepareDevelopmentEntry(index,chapters[index].CaptureCheckpoint(),index>=2))return false;
            for(int i=0;i<Done.Length;i++)Done[i]=story.IsComplete(i);
            SetStudyPresentation(-1);radioDeck.SetStationActive(true);
            Current=chapters[index];CurrentWorld=worlds[index];ChapterIndex=index;
            if(key==1)
            {
                if(radioDeck.operationView!=null)radioDeck.operationView.Apply(Room.Camera);else Room.WarpTo(0);
                radioDeck.LoadOriginal();radioDeck.OpenRecording();
            }
            else if(key==2)
            {
                DebugEnterDarkroom();
                if(!DarkroomWorld.DirectDarkroomAvailable)return false;
                Room.Camera.transform.SetPositionAndRotation(DarkroomWorld.Eye,Quaternion.LookRotation(DarkroomWorld.Look-DarkroomWorld.Eye));
                darkroomReachTarget=DarkroomWorld.Nodes["bag"].transform;
                DarkroomWorld.SetDirectDarkroom(true);Room.EnableLook(false);
            }
            else if(key==6)
            {
                springPreview=true;var binding=GetComponent<NarrativeSceneBinding>();
                if(binding!=null && binding.threshold!=null){var t=binding.threshold;Room.Camera.transform.SetPositionAndRotation(t.position+t.forward*1.3f+Vector3.up*PlayerControlSettings.Current.eyeHeight,Quaternion.LookRotation(-t.forward));}
                Room.EnableLook(true);
            }
            else EnterHouseStation(index);
            cameraRouteNotice="DEV: "+new[]{"Tape / A loaded","Darkroom / roll ready","Autumn writing","Winter album","Album epilogue","Final departure"}[key-1];cameraRouteNoticeUntil=Time.unscaledTime+5;
            SuspendHouseInput();SyncInputContext();return true;
        }
    }
}
#endif
