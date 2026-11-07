using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public DualDeckVisual dualDeck;
        bool transferA,transferB;
        bool internalDeckHandoff;
        public string DeckControlTooltip(string command)
        {
            if(string.IsNullOrEmpty(command) || Chapter==null || transport==null)return null;
            int side=command[0]=='A'?0:1;
            string key=command.Substring(2),prefix=side==0?"A · ":"B · ";
            if(side==0 && (key=="Rewind" || key=="Forward"))return transport.Writing?"Pause B recording with S before winding":key=="Rewind"?"J · Rewind loaded A / B tapes · Click again to stop":"L · Fast-forward loaded A / B tapes · Click again to stop";
            if(DeckBusy(side))return prefix+"Loading / unloading — please wait";
            if(key=="Door")return prefix+"Click slot to eject · Other deck stays independent";
            if(key=="Stop" || key=="Pause")return side==0?"A · ['] Stop original · B stays independent":transport.Writing?"B · Stop recording · A keeps playing":"B · Stop copy · A keeps playing";
            if(key=="Eject")return prefix+"Eject tape · Other deck keeps playing";

            if(side==0 && !Chapter.SourceLoaded)return "A · Load the original tape first";
            if(side==1 && Selected<0)return "B · Load a work tape first";
            if(dualDeck!=null && !dualDeck.Closed(side))return prefix+"Close the door first";
            if(side==0)return key=="Rewind"?"A · J · Toggle rewind":key=="Forward"?"A · L · Toggle fast-forward":"A · F · Toggle playback";
            if(key=="Record")
            {
                if(Chapter.Completed)return "B · All copies saved · Playback available";
                if(!Chapter.SourceLoaded)return "B · Load the original into A first";
                if(!Chapter.AuditionComplete)return "B · Listen to A first";
                if(!Chapter.Tape.Candidates[Selected].Prepared)return "B · Eject and cover both upper-edge holes";
                if(dualDeck!=null && !dualDeck.Closed(0))return "B · Close A before recording";

                if(transport.Writing)return "B · S · Pause recording · A keeps playing";
                return "B · S · Start / resume recording · Press again to pause";
            }
            if(key=="Play")return "B · Toggle playback of your copy · Stop A to listen";
            if(key=="Rewind")return "B · Toggle rewind · Click again to stop";
            if(key=="Forward")return "B · Toggle fast-forward · Click again to stop";
            return null;
        }
        public bool DeckBusy(int deck)=>dualDeck!=null&&dualDeck.Busy(deck);
        Transform TapeExtractionContainer(Transform tape)
        {
            if(workbenchVisuals!=null && workbenchVisuals.LidOpen && workbenchVisuals.InStorage(tape))return workbenchVisuals.storage.transform;
            if(dualDeck==null || adhesiveTab==null)return null;
            var tool=adhesiveTab.transform.parent;
            foreach(var r in tape.GetComponentsInChildren<Renderer>())
                foreach(var c in tool.GetComponentsInChildren<Collider>())
                    if(c.enabled&&r.bounds.Intersects(c.bounds))return tool;
            return null;
        }
        void MoveRadioTape(ObjectDragSession drag,Ray ray,Transform receiver)
        {
            drag.MoveScreenCarry(ray);
            KeepDraggedTapeAboveStorage(drag.Item);
            ShowTapeDropPreview(drag.Blocked?null:receiver);
        }
        void VisibleCassette(Transform item,bool visible)
        {
            if(item==null)return;
            // A returned tape is placed in the same frame. Retired whitebox surfaces must
            // remain excluded now, not wait for the next workbench visual tick to hide them.
            foreach(var r in item.GetComponentsInChildren<Renderer>(true))
                r.enabled=visible && (workbenchVisuals==null || System.Array.IndexOf(workbenchVisuals.legacyRenderers,r)<0);
            foreach(var c in item.GetComponentsInChildren<Collider>(true))
                c.enabled=visible && (workbenchVisuals==null || System.Array.IndexOf(workbenchVisuals.legacyColliders,c)<0);
        }
        void StopTargetOnly(){ClearKeyFeedback();UITransportCommand=-1;wasWriting=false;transport?.TargetCommand(false,false,0,house!=null&&house.Muted);transport?.Save();}
        void HandoffToWorkbench()
        {internalDeckHandoff=true;try{OpenWorkbench();}finally{internalDeckHandoff=false;}}
        void FinishWorkbenchTransfer(){internalDeckHandoff=dualDeck!=null;try{CloseWorkbench();}finally{internalDeckHandoff=false;}}
        bool InsertDualTarget()
        {
            if(dualDeck.Busy(1))return false;
            OperationArea.ClearPlacement(tapes[Selected].root);Chapter.WorkingTapeIndex=Selected;
            if(Chapter.WorkbenchTapeIndex==Selected)Chapter.WorkbenchTapeIndex=-1;
            CancelTapeDrag();ReleaseSpindle();StopTargetOnly();State=Phase.Loading;transferB=true;
            VisibleCassette(tapes[Selected].root,false);
            dualDeck.Transfer(1,true,()=>{transferB=false;State=Phase.Paused;Chapter.VerifyPlayback();if(WorkbenchMode && !RecordingMode)OpenRecording();house.SaveStableProgress();});QueueLoadedRadioFocus();return true;
        }
        public bool EjectDualDeck(int deck)
        {
            if(dualDeck==null||dualDeck.Busy(deck))return false;
            if(deck==0)
            {
                transport.SourceCommand(0);
                if(!Chapter.SourceLoaded)return dualDeck.Door(0,!dualDeck.IsOpen(0));
                transferA=true;
                return dualDeck.Transfer(0,false,()=>{transferA=false;Chapter.SourceLoaded=false;RestoreSource();ReturnTapeToDesk(originalTape,CurrentCopy);dualDeck.Snap(0,false,true);house.SaveStableProgress();});
            }
            StopTargetOnly();
            if(Selected<0)return dualDeck.Door(1,!dualDeck.IsOpen(1));
            transferB=true;
            int removed=Selected;
            return dualDeck.Transfer(1,false,()=>
            {
                transferB=false;ResetDeck();
                var tape=tapes[removed];
                tape.root.gameObject.SetActive(true);VisibleCassette(tape.root,true);
                Chapter.WorkbenchTapeIndex=removed;
                // Keep the recording view: its tick does not advance preparation camera motion.
                if(!PlaceTapeOnPreparationMat(false,false))
                {
                    Chapter.WorkbenchTapeIndex=-1;
                    ReturnTapeToDesk(tape.root,3+removed);
                    // A blocked/missing legacy landing falls back to the box. Make
                    // that fallback accessible instead of hiding the recorded tape.
                    if(workbenchVisuals!=null && workbenchVisuals.InStorage(tape.root) && !workbenchVisuals.LidOpen)
                        ToggleStorageLidWithSound();
                }
                dualDeck.Snap(1,false,true);house.SaveStableProgress();
            });
        }
        public bool ToggleDualDoor(int deck)
        {
            if(dualDeck==null||dualDeck.Busy(deck))return false;
            if(deck==0)transport.SourceCommand(0);else StopTargetOnly();
            return dualDeck.Door(deck,!dualDeck.IsOpen(deck));
        }
        public bool PressDualControl(string command)
        {
            if(dualDeck==null||string.IsNullOrEmpty(command))return false;
            int deck=command[0]=='A'?0:1;string key=command.Substring(2);
            if(deck==0 && (key=="Rewind" || key=="Forward")){SourceControl(key=="Rewind"?2:4);return true;}
            if(dualDeck.Busy(deck))return true;
            if(key=="Door"){EjectDualDeck(deck);return true;}
            if(key=="Eject"){dualDeck.Press(deck,key);EjectDualDeck(deck);return true;}
            if(key=="Stop"||key=="Pause")dualDeck.Press(deck,key);
            if(!dualDeck.Closed(deck))return true;
            if(!RecordingMode){OpenRecording();recordingWait=false;}
            if(deck==0)
            {
                if(!Chapter.SourceLoaded)return true;
                if(key=="Play")SourceControl(1);
                else if(key=="Rewind")SourceControl(2);
                else if(key=="Stop"||key=="Pause")SourceControl(0);
                else if(key=="Forward")SourceControl(4);
            }
            else
            {
                // Use the existing UI command validation (audition, protection and capacity).
                if(key=="Stop"||key=="Pause")StopTargetOnly();
                else SetUITransportCommand(key=="Record"?0:key=="Play"?1:key=="Rewind"?2:key=="Forward"?3:4);
            }
            return true;
        }
        void TickDualDeck()
        {
            if(dualDeck==null||Chapter==null||transport==null)return;
            bool paused=!focused || (house!=null&&(house.ShowHelp||house.safeInspection!=null&&house.safeInspection.BlocksInput));
            if(!paused)
            {
                string pressed=TargetKeyDown(2)?"Rewind":TargetKeyDown(3)?"Forward":TargetKeyDown(1)?"Play":null;
                dualDeck.Tick(Time.unscaledDeltaTime,Chapter.SourceLoaded&&transport.SourcePlaying,Selected>=0?transport.TargetStatus:"EMPTY",transport.Writing,TargetKeyDown(0),pressed,transport.SourceStatus);
            }
            if(Chapter.SourceLoaded)VisibleCassette(originalTape,false);
            if(Selected>=0&&(State==Phase.Loading||State==Phase.Paused))VisibleCassette(tapes[Selected].root,false);
        }
        void SettleDualDeck()
        {
            if(dualDeck==null||Chapter==null)return;
            transferA=transferB=false;
            if(State==Phase.Loading)State=Phase.Paused;
            dualDeck.Settle(Chapter.SourceLoaded,Chapter.WorkingTapeIndex>=0);
        }
    }
}
