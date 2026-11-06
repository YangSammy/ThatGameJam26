using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public enum InputContextId
    {
        Exploration, Station, TapeWorkbench, TapeTransport, Inspection, Gallery,
        Modal, Paused, Transition, Unfocused, ParkViewfinder, ParkBody, ParkMeter, ParkAperture, ParkShutter, Darkroom, AutumnWriting, WinterObserve, WinterBook, WardWalk
    }
    // One session per player/scene. Controllers own gestures; this owns permission,
    // handoff and consumption, so an exit cannot reach two controllers in one frame.
    public sealed class PlayerInputContext
    {
        readonly PlayerInputReleaseGate release;
        public PlayerInputContext(bool ignoreLocomotionForRelease=false){release=new PlayerInputReleaseGate(ignoreLocomotionForRelease);}
        bool initialized;
        int consumedFrame=-1,backSampleFrame=-1,backClickFrame=-1;
        bool backHeld,backChord; Vector2 backStart;
        void SampleBackClick()
        {
            if(backSampleFrame==Time.frameCount)return;backSampleFrame=Time.frameCount;
            if(Input.GetMouseButtonDown(1)){backHeld=true;backChord=Current==InputContextId.Inspection && Input.GetMouseButton(0);backStart=Input.mousePosition;}
            if(backHeld && ((Current==InputContextId.Inspection && Input.GetMouseButton(0)) || Vector2.Distance(backStart,Input.mousePosition)>8))backChord=true;
            if(Input.GetMouseButtonUp(1)){if(backHeld && !backChord)backClickFrame=Time.frameCount;backHeld=false;}
        }
        public InputContextId Current {get;private set;}
        public bool Ready=>initialized && !BrushSceneTransition.IsBusy && Current!=InputContextId.Unfocused && Current!=InputContextId.Transition && consumedFrame!=Time.frameCount && release.Ready;
        public bool Allows(InputContextId context)=>Current==context && Ready;
        public void Select(InputContextId context,PlayerControlState cursorState,bool mouseLook=false,bool continueGesture=false)
        {
            if(!initialized || Current!=context)
            {
                backHeld=backChord=false;backClickFrame=-1;
                initialized=true;Current=context;
                if(continueGesture)release.AdoptGesture();else release.Arm();
            }
            PlayerInputPolicy.ApplyCursor(cursorState,mouseLook);
        }
        public void Handoff(){backHeld=backChord=false;backClickFrame=-1;release.Arm();PlayerInputPolicy.SuppressLook();}
        // Only call from the same press that deliberately began a scene drag.
        public void ContinueGesture(){release.AdoptGesture();}
        public bool ExitPressed(InputContextId owner,Func<bool> cancelGesture,Action leave,bool inspectionKey=false)
        {
            SampleBackClick();
            if(GamePauseMenu.BlocksGameplay || !Allows(owner) || !(backClickFrame==Time.frameCount || (inspectionKey && Input.GetKeyDown(KeyCode.E))))return false;
            return RequestExit(owner,inspectionKey && Input.GetKeyDown(KeyCode.E)?null:cancelGesture,leave);
        }
        public bool RequestExit(InputContextId owner,Func<bool> cancelGesture,Action leave)
        {
            if(!Allows(owner))return false;
            consumedFrame=Time.frameCount;Handoff();
            if(cancelGesture==null || !cancelGesture())leave?.Invoke();
            return true;
        }
    }
}
