using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public enum InputContextId
    {
        Exploration, Station, TapeWorkbench, TapeTransport, Inspection, Gallery,
        Modal, Paused, Transition, Unfocused, ParkViewfinder, ParkBody, ParkMeter, ParkAperture, ParkShutter
    }
    // One session per player/scene. Controllers own gestures; this owns permission,
    // handoff and consumption, so an exit cannot reach two controllers in one frame.
    public sealed class PlayerInputContext
    {
        readonly PlayerInputReleaseGate release=new PlayerInputReleaseGate();
        bool initialized;
        int consumedFrame=-1;
        public InputContextId Current {get;private set;}
        public bool Ready=>initialized && Current!=InputContextId.Unfocused && Current!=InputContextId.Transition && consumedFrame!=Time.frameCount && release.Ready;
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
        public bool ExitPressed(InputContextId owner,Func<bool> cancelGesture,Action leave,bool inspectionKey=false)
        {
            if(!Allows(owner) || !(Input.GetKeyDown(KeyCode.Escape) || (inspectionKey && Input.GetKeyDown(KeyCode.E))))return false;
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
