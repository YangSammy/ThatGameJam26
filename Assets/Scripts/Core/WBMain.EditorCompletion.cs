#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        bool editorCompletionRunning;
        public bool EditorCompletionRunning=>editorCompletionRunning;

        public bool EditorCompleteStage(int key)
        {
            if(key<1 || key>5 || !houseReady || loadingSpringRoom || editorCompletionRunning)return false;
            StartCoroutine(CompleteEditorStage(key));return true;
        }

        IEnumerator CompleteEditorStage(int key)
        {
            editorCompletionRunning=true;
            try
            {
                int index=key==1?0:key<=3?1:key==4?2:3;
                if(chapters[index].Completed || (story!=null && story.IsComplete(index)))
                {EditorCompletionNotice("Already complete. Shift + number opens a test entry.");yield break;}
                safeInspection?.Close();CurrentWorld?.CancelPointerGesture();
                radioDeck?.SuspendInput();DarkroomWorld?.CancelInteraction();
                if(!SaveStableProgress()){EditorCompletionNotice("Save failed: "+LastSaveError);yield break;}
                bool ready=key==1?springTapeRoom!=null && radioDeck!=null:
                    key<=3?summerPhotoRoom!=null && DarkroomWorld!=null:
                    InStation && ChapterIndex==index && (key!=5 || ((PaintingChapter)Current).Phase>=2);
                if(!ready)
                {
                    int entry=key==1?1:key<=3?2:key==4?3:5;
                    if(!DebugEnterChapter(entry)){EditorCompletionNotice("Test entry unavailable.");yield break;}
                    float deadline=Time.realtimeSinceStartup+45;
                    yield return null;
                    while(loadingSpringRoom || transition>0 || ChapterIndex!=index || (key>=4 && !InStation))
                    {
                        if(Time.realtimeSinceStartup>deadline){EditorCompletionNotice("Entry timed out. Progress kept; retry after loading.");yield break;}
                        yield return null;
                    }
                }
                try
                {
                    if(key==1)
                    {
                        radioDeck.OpenRecording();
                        if(!radioDeck.EditorCompleteRecording(out var message))EditorCompletionNotice(message);
                    }
                    else if(key<=3)
                    {
                        string message;
                        bool ok=key==2?DarkroomWorld.DebugPrepareDarkroom(out message):DarkroomWorld.EditorCompleteDarkroom(out message);
                        EditorCompletionNotice(message);
                        if(ok)
                        {
                            PrepareContinuousDarkroom();
                            if(Curtain!=null && !Curtain.IsOpen){Curtain.Toggle();Curtain.Advance(1);}
                            Room.Camera.transform.SetPositionAndRotation(summerPhotoRoom.darkroomView.position,summerPhotoRoom.darkroomView.rotation);
                            Room.EnableLook(true);darkroomReachTarget=DarkroomWorld.Nodes["bag"].transform;
                            DarkroomWorld.SetDirectDarkroom(true);Room.EnableLook(false);
                        }
                    }
                    else if(key==4)((WritingWorld3D)worlds[2]).EditorBeginFarewell();
                    else ((PaintingWorld3D)worlds[3]).EditorCompleteWard();
                    SuspendHouseInput();SyncInputContext();
                }
                catch(Exception error){EditorCompletionNotice("Completion failed: "+error.Message);Debug.LogException(error,this);}
            }
            finally{editorCompletionRunning=false;}
        }

        void EditorCompletionNotice(string message)
        {cameraRouteNotice="EDITOR: "+message;cameraRouteNoticeUntil=Time.unscaledTime+7;Debug.Log(cameraRouteNotice,this);}
    }
}
#endif
