using System;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public bool EditorCompleteRecording(out string message)
        {
            message="Recording already complete.";
            if(Chapter.Completed)return true;
            StopRecordingInput();
            while(!Chapter.Completed)
            {
                SynchronizeCommission();
                if(!recordingReady || transport==null || RecordingError!="")
                {message="Recording unavailable: "+RecordingError;return false;}
                int tape=Chapter.RecordingTapeIndex;
                if(tape<0 || Chapter.IsArchived(tape) || Chapter.Tape.Candidates[tape].Breaks.Length!=0)
                    tape=Array.FindIndex(Chapter.Tape.Candidates,c=>c.Breaks.Length==0 && !Chapter.IsArchived(Array.IndexOf(Chapter.Tape.Candidates,c)));
                if(tape<0){message="No intact work tape available.";return false;}
                var candidate=Chapter.Tape.Candidates[tape];
                candidate.Broken=false;candidate.LeftUsed=candidate.RightUsed=candidate.StartReached=candidate.EndReached=candidate.HoleA=candidate.HoleB=true;
                for(int i=0;i<candidate.Seen.Length;i++)candidate.Seen[i]=true;
                candidate.Scanned=candidate.Seen.Length;
                Chapter.SourceLoaded=Chapter.AuditionComplete=true;Chapter.ProtectionDiscovered=true;
                Chapter.WorkingTapeIndex=tape;Chapter.ClearRecordingTapeBinding();
                if(!Chapter.BindRecordingTape(tape)){message="Recording tape binding failed.";return false;}
                Selected=tape;State=Phase.Paused;
                if(!transport.EditorPrepareAcceptedCopy(commissionAudio)){message="Could not save editor test recording.";return false;}
                // Only suppress the intermediate popups. The last successful submission
                // uses the normal commit, box compartment and dismiss/continue presentation.
                if(transportPanel.successPopup!=null)transportPanel.successPopup.SetActive(false);
                int previous=Chapter.CommissionIndex;
                ResetDeck();
                SubmitRecordingFromStorage(tape);
                if(Chapter.CommissionIndex==previous){message=SubmissionMessage;return false;}
            }
            message=SubmissionMessage;return true;
        }
    }
}
