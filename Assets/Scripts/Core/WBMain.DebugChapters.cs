using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        bool PollDebugChapterKeys()
        {
            int pending=StageCompletionShortcuts.PendingHouseStage;
            if(pending!=0 && EditorCompleteStage(pending))
            {
                StageCompletionShortcuts.PendingHouseStage=0;
                return true;
            }
            int completion=StageCompletionShortcuts.Pressed();
            if(completion!=0){EditorCompleteStage(completion);return true;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            KeyCode[] row={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.Alpha6};
            KeyCode[] pad={KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5,KeyCode.Keypad6};
            for(int i=0;i<6;i++)if(Input.GetKeyDown(row[i])||Input.GetKeyDown(pad[i]))
            {
                DebugEnterChapter(i+1);
                return true;
            }
#endif
            return false;
        }
        // Development saves only. Narrative shortcuts restart their selected test segment;
        // tape/audio and darkroom material progress are deliberately retained.
        public bool DebugEnterChapter(int key)
        {
            if(!houseReady || loadingSpringRoom || key<1 || key>6)return false;
            safeInspection?.Close();radioDeck?.DismissRecordingSuccess();
            radioDeck?.CloseWorkbench();radioDeck?.CloseRecording();if(DirectTapeHandling)radioDeck.PutBack();
            DarkroomWorld?.CancelInteraction();DarkroomWorld?.SetDirectDarkroom(false);
            CurrentWorld?.CancelPointerGesture();
            if(!SaveStableProgress()){cameraRouteNotice="DEV: save failed. Stay here and retry.";cameraRouteNoticeUntil=Time.unscaledTime+6;return false;}
            if(InStation)
            {
                CompleteHouseExit();Room.Camera.transform.SetPositionAndRotation(roomEye,roomRotation);Room.Camera.fieldOfView=roomFov;
            }
            transition=0;returningToRoom=awaitRelease=leaveCompletedChapter=false;ShowHelp=false;springPreview=false;
            int narrativeIndex=key==3?2:key==4 || key==5?3:-1;
            if(key==6 && DepartureRoom==null)
            {StartCoroutine(OpenDepartureBedroom(false,Vector3.zero,Quaternion.identity,key));return true;}
            if(narrativeIndex>=0 && (narrativeBedroom==null || narrativeBedroom.chapterEntry.chapter!=narrativeIndex))
            {
                StartCoroutine(OpenNarrativeBedroom((StoryDay)narrativeIndex,key));return true;
            }
            if(key==1 && springTapeRoom==null)
            {StartCoroutine(OpenSeasonBedroom(false,false,1));return true;}
            if(key==2 && summerPhotoRoom==null && !loadingSpringRoom)
            {
                StartCoroutine(OpenSeasonBedroom(true,false,2));return true;
            }
            Room.StopMotion();Room.EnableLook(true);
            if(!editorCompletionRunning && !FiveDayProgress.DevelopmentSession){FiveDayProgress.DevelopmentSession=true;story=FiveDayProgress.LoadForSession();}
            int index=key<=4?key-1:key==5?3:4;
            if(index>=2)
            {
                worlds[index].Dispose();CreateHouseChapter(index);BindChapterData();playerHands.Register(index,worlds[index]);
                if(key==5)((PaintingChapter)chapters[3]).RestoreCheckpoint("{\"version\":2,\"phase\":2,\"shapes\":[true,true,true,true,true],\"key\":true,\"high\":true,\"crate\":4}");
            }
            if(!(editorCompletionRunning?story.PrepareShortcutEntry(index,chapters[index].CaptureCheckpoint(),index>=2):
                story.PrepareDevelopmentEntry(index,chapters[index].CaptureCheckpoint(),index>=2)))return false;
            for(int i=0;i<Done.Length;i++)Done[i]=story.IsComplete(i);
            SetStudyPresentation(-1);radioDeck?.SetStationActive(true);
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
                if(summerPhotoRoom!=null)Room.Camera.transform.SetPositionAndRotation(summerPhotoRoom.darkroomView.position,summerPhotoRoom.darkroomView.rotation);
                else Room.Camera.transform.SetPositionAndRotation(DarkroomWorld.Eye,Quaternion.LookRotation(DarkroomWorld.Look-DarkroomWorld.Eye));
                // Adopt the checkpoint heading before free-pointer movement resumes.
                // Otherwise StepFreePointer restores the heading from before the warp.
                Room.EnableLook(true);
                darkroomReachTarget=DarkroomWorld.Nodes["bag"].transform;
                DarkroomWorld.SetDirectDarkroom(true);Room.EnableLook(false);
            }
            else if(key==6)
            {
                springPreview=true;var binding=GetComponent<NarrativeSceneBinding>();
                var t=DepartureRoom!=null?DepartureRoom.threshold:binding?.threshold;
                if(t!=null){Room.Camera.transform.SetPositionAndRotation(t.position+t.forward*1.3f+Vector3.up*PlayerControlSettings.Current.eyeHeight,Quaternion.LookRotation(-t.forward));}
                Room.EnableLook(true);
            }
            else EnterHouseStation(index);
            cameraRouteNotice="DEV: "+new[]{"Tape / A loaded","Darkroom / roll ready","Autumn writing","Winter album","Album epilogue","Final departure"}[key-1];cameraRouteNoticeUntil=Time.unscaledTime+5;
            SuspendHouseInput();SyncInputContext();return true;
        }
    }
}
