using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        readonly LineRenderer[] taskHoles=new LineRenderer[2];
        Material taskHoleInk;
        void ShowTaskHole(int side,Transform hole)
        {
            if(!taskHoleInk)taskHoleInk=new Material(Shader.Find("Unlit/Color")){color=new Color(1,.65f,.12f,1)};
            if(!taskHoles[side])
            {
                var go=new GameObject("Protection hole task outline");go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=4;
                line.startWidth=line.endWidth=.0012f;line.numCornerVertices=3;line.sharedMaterial=taskHoleInk;taskHoles[side]=line;
            }
            var c=hole.position+hole.up*.001f;var x=hole.right*.0035f;var z=hole.forward*.0025f;
            var rim=taskHoles[side];rim.SetPositions(new[]{c-x-z,c-x+z,c+x+z,c+x-z});rim.enabled=true;
        }
        void HideTaskHoles(){foreach(var line in taskHoles)if(line)line.enabled=false;}
        Transform TaskKey(string command)
        {
            if(dualDeck==null || dualDeck.keyCommands==null || dualDeck.keyHits==null)return null;
            for(int i=0;i<dualDeck.keyCommands.Length && i<dualDeck.keyHits.Length;i++)
                if(dualDeck.keyCommands[i]==command && dualDeck.keyHits[i]!=null)return dualDeck.keyHits[i].transform;
            return null;
        }
        void ShowNextTapeTask()
        {
            HideTaskHoles();
            if(!focused || Chapter==null || Chapter.CommissionIndex>0 || Chapter.Completed || workbenchVisuals==null ||
                house.safeInspection.BlocksInput || radioCameraMoving || preparationCameraMoving ||
                storageDelivery!=null || State==Phase.Loading || DeckBusy(0) || DeckBusy(1))return;
            Transform target=null;Renderer[] surfaces=null;
            // Preparation belongs to the cassette on the mat, even if A has stopped meanwhile.
            if(BenchIndex>=0 && Selected<0 && !Chapter.IsArchived(BenchIndex))
            {
                var tape=tapes[BenchIndex];var data=Chapter.Tape.Candidates[BenchIndex];
                bool recorded=Chapter.RecordingTapeIndex==BenchIndex && transport!=null && transport.RecordedSeconds>0;
                if(recorded)
                {
                    if(!workbenchVisuals.LidOpen)target=workbenchVisuals.lidHit.transform;
                    else if(benchGesture==BenchGesture.MoveTape)target=workbenchVisuals.finishedHit.transform;
                    else {target=tape.root;surfaces=workbenchVisuals.intact[BenchIndex].ShellHover;}
                }
                else if(!data.Prepared && !OnPreparationMat(tape.root))
                {
                    target=benchGesture==BenchGesture.MoveTape?workMat.transform:tape.root;
                    if(target==tape.root)surfaces=workbenchVisuals.intact[BenchIndex].ShellHover;
                }
                else if(!data.Prepared)
                {
                    if(benchGesture==BenchGesture.MoveStrip)
                    {
                        if(PatchLockReason(BenchIndex)!=null)return;
                        // Both legal remaining sockets are choices, never the whole shell or a sealed socket.
                        for(int i=0;i<2;i++)if(!(i==0?data.HoleA:data.HoleB))
                        {
                            var piece=tape.preparation.pieces[i];
                            if(piece!=null)ShowTaskHole(i,piece.transform);
                        }
                        return;
                    }
                    if(benchGesture!=BenchGesture.None)return;
                    target=Chapter.LooseStripReady?adhesiveStrip:workbenchVisuals.dispenser.transform;
                }
                else if(benchGesture==BenchGesture.MoveTape){target=slotHit.transform;surfaces=dualDeck!=null?dualDeck.DoorOutline(1):null;}
                else {target=tape.root;surfaces=workbenchVisuals.intact[BenchIndex].ShellHover;}
            }
            else if(!Chapter.SourceLoaded)
            {
                target=movingOriginal?sourceSlot.transform:StorageClosed(originalTape)?workbenchVisuals.lidHit.transform:originalTape;
                if(target==sourceSlot.transform && dualDeck!=null)surfaces=dualDeck.DoorOutline(0);
            }
            else if(!Chapter.AuditionComplete)
            {if(transport!=null && transport.SourcePlaying)return;target=TaskKey("A_Play");}
            else if(Selected<0)
            {
                if(!workbenchVisuals.LidOpen)target=workbenchVisuals.lidHit.transform;
                else
                {
                    // Offer one available work tape, not all five competing choices.
                    for(int i=0;i<tapes.Length;i++)if(WorkTapeLockReason(i)==null)
                    {target=tapes[i].root;surfaces=workbenchVisuals.intact[i].ShellHover;break;}
                }
            }
            else
            {
                if(!Chapter.Tape.Candidates[Selected].Prepared ||
                    (transport!=null && transport.SourceStatus=="END" && !transport.Writing))
                {target=slotHit.transform;surfaces=dualDeck!=null?dualDeck.DoorOutline(1):null;}
                else if(transport!=null && (transport.Writing || transport.TargetStatus=="PLAYBACK"))return;
                else
                {
                    var play=TaskKey("A_Play");
                    var record=TaskKey("B_Record");
                    if(play)TaskOutlineCue.Show(play,view,play.GetComponentsInChildren<Renderer>(true));
                    if(record)TaskOutlineCue.Show(record,view,record.GetComponentsInChildren<Renderer>(true));
                    return;
                }
            }
            if(target)TaskOutlineCue.Show(target,view,surfaces??target.GetComponentsInChildren<Renderer>(true));
        }
    }
}
