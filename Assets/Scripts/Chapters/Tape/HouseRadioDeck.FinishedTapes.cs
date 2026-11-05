using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Tooltip("The three authored finished-copy poses. The actual recorded work cassette occupies its commission slot.")]
        public Transform[] finishedTapeAnchors=new Transform[RadioTapeChapter.CommissionCount];

        string RecordingOwnerLock(int index)
        {
            if(transport==null || transport.RecordedSeconds<=0)return null;
            if(Chapter.RecordingTapeIndex<0)return "Recording tape identity is unavailable. Press R twice at the recorder to erase and retry.";
            if(Chapter.RecordingTapeIndex==index)return null;
            return "This recording belongs to another tape. Reinsert it, or press R twice at the recorder to erase and retry.";
        }

        // Chapter state owns finished poses. Loose-area snapshots must never put
        // a committed cassette back on the mat or among the unused tapes.
        public void RestoreFinishedTapes()
        {
            if(Chapter==null || tapes==null)return;
            if(completedCopies!=null)foreach(var copy in completedCopies)if(copy!=null)copy.SetActive(false);
            for(int i=0;i<tapes.Length;i++)
            {
                int slot=Chapter.FinishedCommission(i);
                if(slot<0)continue;
                var tape=tapes[i];
                OperationArea.ClearPlacement(tape.root);
                if(finishedTapeAnchors==null || slot>=finishedTapeAnchors.Length || finishedTapeAnchors[slot]==null)
                {Debug.LogError("Missing finished tape anchor for copy "+(slot+1),this);continue;}
                tape.root.SetParent(tape.homeParent,false);
                tape.root.localScale=tape.homeScale;
                tape.model.localPosition=tape.modelPosition;tape.model.localRotation=tape.modelRotation;tape.model.localScale=tape.modelScale;
                tape.root.SetPositionAndRotation(finishedTapeAnchors[slot].position,finishedTapeAnchors[slot].rotation);
                tape.root.gameObject.SetActive(true);VisibleCassette(tape.root,true);
                tape.hit.enabled=true;
                if(workbenchVisuals!=null)workbenchVisuals.Inspect(i,Chapter.Tape.Candidates[i]);
            }
        }
    }
}
