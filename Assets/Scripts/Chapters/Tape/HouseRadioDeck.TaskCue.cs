using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        Transform TaskKey(string command)
        {
            if(dualDeck==null || dualDeck.keyCommands==null || dualDeck.keyHits==null)return null;
            for(int i=0;i<dualDeck.keyCommands.Length;i++)if(dualDeck.keyCommands[i]==command)return dualDeck.keyHits[i].transform;
            return null;
        }
        void ShowNextTapeTask()
        {
            if(!focused || Chapter==null || Chapter.Completed || workbenchVisuals==null ||
                house.safeInspection.BlocksInput || radioCameraMoving || preparationCameraMoving)return;
            Transform target=null;Renderer[] surfaces=null;
            if(Selected<0 && Chapter.RecordingTapeIndex>=0 && transport!=null && transport.RecordedSeconds>0)target=workbenchVisuals.finishedHit.transform;
            else if(!Chapter.SourceLoaded)target=movingOriginal?sourceSlot.transform:
                StorageClosed(originalTape)?workbenchVisuals.lidHit.transform:originalTape;
            else if(!Chapter.AuditionComplete){if(transport!=null && transport.SourcePlaying)return;target=TaskKey("A_Play");}
            else if(BenchIndex<0 && Selected<0)
            {
                if(!workbenchVisuals.LidOpen)target=workbenchVisuals.lidHit.transform;
                else {for(int i=0;i<tapes.Length;i++)if(!Chapter.IsArchived(i))TaskOutlineCue.Show(tapes[i].root,view,workbenchVisuals.intact[i].ShellHover);return;}
            }
            else if(BenchIndex>=0 && Selected<0)
            {
                if(!OnPreparationMat(tapes[BenchIndex].root))target=workMat.transform;
                else if(!Chapter.Tape.Candidates[BenchIndex].Prepared)
                {
                    target=Chapter.LooseStripReady?adhesiveStrip:workbenchVisuals.head.parent.Find("A05_PullableTail");
                    if(benchGesture==BenchGesture.MoveStrip)
                    {
                        var d=Chapter.Tape.Candidates[BenchIndex];
                        target=tapes[BenchIndex].root;surfaces=workbenchVisuals.intact[BenchIndex].ShellHover;
                    }
                }
                else {target=slotHit.transform;surfaces=dualDeck!=null?dualDeck.DoorOutline(1):null;}
            }
            else if(Selected>=0)
            {
                if(!Chapter.Tape.Candidates[Selected].Prepared){target=slotHit.transform;surfaces=dualDeck!=null?dualDeck.DoorOutline(1):null;}
                else if(transport!=null && transport.SourceStatus=="END"){target=slotHit.transform;surfaces=dualDeck!=null?dualDeck.DoorOutline(1):null;}
                else target=TaskKey(transport!=null && !transport.SourcePlaying?"A_Play":"B_Record");
            }
            if(target==sourceSlot.transform && dualDeck!=null)surfaces=dualDeck.DoorOutline(0);
            if(target)TaskOutlineCue.Show(target,view,surfaces??target.GetComponentsInChildren<Renderer>(true));
        }
    }
}
