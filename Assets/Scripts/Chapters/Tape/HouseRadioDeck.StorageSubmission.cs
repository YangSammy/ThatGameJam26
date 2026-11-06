using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        string FinishedTapeGate(int index,bool requireOpen=true)
        {
            if(Chapter==null || transport==null || !recordingReady)return "Recording unavailable";
            if(Chapter.Completed)return "All copies are already saved";
            if(requireOpen && (workbenchVisuals==null || !workbenchVisuals.LidOpen || workbenchVisuals.LidBusy))return "Open the storage box first";
            if(DeckBusy(1) || Selected>=0 || Chapter.WorkingTapeIndex>=0)return "Eject B before returning the recorded tape";
            if(index<0 || index>=tapes.Length || Chapter.IsArchived(index))return "Choose the current recorded work tape";
            if(index!=Chapter.RecordingTapeIndex || transport.RecordedSeconds<=0)return "This tape has no current recording";
            if(!Chapter.AuditionComplete)return "Listen to the original first";
            if(!Chapter.Tape.Candidates[index].Prepared)return "Seal both holes first";
            return null;
        }
        bool OverFinishedCompartment(Ray ray,Transform item)
        {
            var w=workbenchVisuals;
            return w!=null && w.finishedHit!=null && item!=null &&
                DragReceiverProbe.Evaluate(ray,w.finishedHit.transform,item,.012f,3f,
                    c=>c.transform.IsChildOf(w.storage.transform) || IsArchivedTapeCollider(c),w.finishedHit)==DragReceiverProbe.Result.Ready;
        }
        bool IsArchivedTapeCollider(Collider c)
        {
            for(int i=0;i<tapes.Length;i++)if(Chapter.IsArchived(i) && c.transform.IsChildOf(tapes[i].root))return true;
            return false;
        }
        bool FinishedDropReady(int index,Ray ray)=>FinishedTapeGate(index)==null && OverFinishedCompartment(ray,tapes[index].root);
        bool FinishStorageDrop(int index,Ray ray,ObjectDragSession drag)
        {
            if(index<0 || !OverFinishedCompartment(ray,tapes[index].root))return false;
            string reason=FinishedTapeGate(index);
            if(reason==null && drag.Blocked)reason="Path blocked. Lift the tape clear of the box";
            // Revert the gesture before committing. Rejection keeps the existing PCM,
            // identity, cursor and last desk pose; only a successful commit archives it.
            drag.End(true);ShowTapeDropPreview(null);
            if(reason!=null){ShowTapePointerNotice(reason);return true;}
            SubmitRecordingFromStorage(index);
            if(!SubmissionSucceeded)
            {
                ShowTapePointerNotice(SubmissionMessage+" · Recording kept. Reload B to review or retry.");
                tapePointerNoticeUntil=Time.unscaledTime+8;
                house.SaveStableProgress();
            }
            return true;
        }
    }
}
