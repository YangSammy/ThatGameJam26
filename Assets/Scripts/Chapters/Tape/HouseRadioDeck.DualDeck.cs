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
            if(DeckBusy(side))return prefix+"Loading / unloading — please wait";
            if(key=="Door")return prefix+(dualDeck!=null && dualDeck.IsOpen(side)?"Close door":"Open door · Stops this deck only");
            if(key=="Stop" || key=="Pause")return side==0?"A · Stop original · B stays independent":transport.Writing?"B · Stop recording · A keeps playing":"B · Stop copy · A keeps playing";
            if(key=="Eject")return prefix+"Eject tape · Other deck keeps playing";
            if(side==0 && key=="Forward")return "A · Fast-forward unavailable";
            if(side==0 && !Chapter.SourceLoaded)return "A · Load the original tape first";
            if(side==1 && Selected<0)return "B · Load a work tape first";
            if(dualDeck!=null && !dualDeck.Closed(side))return prefix+"Close the door first";
            if(side==0)return key=="Rewind"?"A · Return original to start":"A · Play original · Listen for noise";
            if(key=="Record")
            {
                if(Chapter.Completed)return "B · All copies saved · Playback available";
                if(!Chapter.SourceLoaded)return "B · Load the original into A first";
                if(!Chapter.AuditionComplete)return "B · Listen to A first";
                if(!Chapter.Tape.Candidates[Selected].Intact)return "B · Eject and check both reels first";
                if(!Chapter.Tape.Candidates[Selected].Prepared)return "B · Eject and cover both upper-edge holes";
                if(dualDeck!=null && !dualDeck.Closed(0))return "B · Close A before recording";
                if(transport.SourceStatus=="END" && transport.RecordedSeconds>0)return "B · Original ended · Listen to your copy";
                if(transport.Writing)return "B · Recording · STOP B before noise";
                return "B · Hold S + F to record · Start A separately";
            }
            if(key=="Play")return "B · Hold F to play · Hold S too to record";
            if(key=="Rewind")return "B · Hold J to rewind · Release to stop";
            if(key=="Forward")return "B · Hold L to advance · Release to stop";
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
            ShowTapeDropPreview(drag.Blocked?null:receiver);
        }
        void VisibleCassette(Transform item,bool visible)
        {if(item==null)return;foreach(var r in item.GetComponentsInChildren<Renderer>(true))r.enabled=visible;foreach(var c in item.GetComponentsInChildren<Collider>(true))c.enabled=visible;}
        void StopTargetOnly(){ClearKeyFeedback();UITransportCommand=-1;wasWriting=false;stopLatch=true;transport?.TargetCommand(false,false,0,house!=null&&house.Muted);transport?.Save();}
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
                return dualDeck.Transfer(0,false,()=>{transferA=false;Chapter.SourceLoaded=false;RestoreSource();dualDeck.Snap(0,false,true);house.SaveStableProgress();});
            }
            StopTargetOnly();
            if(Selected<0)return dualDeck.Door(1,!dualDeck.IsOpen(1));
            transferB=true;
            return dualDeck.Transfer(1,false,()=>{transferB=false;ResetDeck();dualDeck.Snap(1,false,true);house.SaveStableProgress();});
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
            if(dualDeck.Busy(deck))return true;
            if(key=="Door"){ToggleDualDoor(deck);return true;}
            if(key=="Eject"){dualDeck.Press(deck,key);EjectDualDeck(deck);return true;}
            if(key=="Stop"||key=="Pause")dualDeck.Press(deck,key);
            if(!dualDeck.Closed(deck))return true;
            if(!RecordingMode)OpenRecording();
            if(deck==0)
            {
                if(!Chapter.SourceLoaded)return true;
                if(key=="Play")transport.SourceCommand(1);
                else if(key=="Rewind"){transport.SourceCommand(2);dualDeck.Press(0,key);}
                else if(key=="Stop"||key=="Pause")transport.SourceCommand(0);
                else if(key=="Forward")Notice="Source fast-forward is not available.";
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
                dualDeck.Tick(Time.unscaledDeltaTime,Chapter.SourceLoaded&&transport.SourcePlaying,Selected>=0?transport.TargetStatus:"EMPTY",transport.Writing,TargetKeyDown(0),pressed);
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
