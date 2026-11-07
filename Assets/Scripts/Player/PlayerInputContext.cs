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
        int consumedFrame=-1;
        public InputContextId Current {get;private set;}
        public bool Ready=>initialized && !BrushSceneTransition.IsBusy && Current!=InputContextId.Unfocused && Current!=InputContextId.Transition && consumedFrame!=Time.frameCount && release.Ready;
        public bool Allows(InputContextId context)=>Current==context && Ready;
        public void Select(InputContextId context,PlayerControlState cursorState,bool mouseLook=false,bool continueGesture=false)
        {
            if(!initialized || Current!=context)
            {
                initialized=true;Current=context;
                if(continueGesture)release.AdoptGesture();else release.Arm();
            }
            PlayerInputPolicy.ApplyCursor(cursorState,mouseLook);
        }
        public void Handoff(){release.Arm();PlayerInputPolicy.SuppressLook();}
        // Only call from the same press that deliberately began a scene drag.
        public void ContinueGesture(){release.AdoptGesture();}
        public bool ExitPressed(InputContextId owner,Func<bool> hasGesture,Action leave,bool inspectionKey=false)
        {
            // Navigation is explicit: UI buttons use RequestExit; inspection also uses E.
            // Mouse buttons are reserved for manipulating objects and looking around.
            if(!inspectionKey || !Input.GetKeyDown(KeyCode.E) || GamePauseMenu.BlocksGameplay || !Allows(owner))return false;
            return RequestExit(owner,null,leave);
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
