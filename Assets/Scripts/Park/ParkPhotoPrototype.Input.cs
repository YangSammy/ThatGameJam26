using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        public PlayerInputContext InputContext {get;}=new PlayerInputContext(ignoreLocomotionForRelease:true);
        bool applicationPaused;
        void SyncParkInput()
        {
            var id=!focused || applicationPaused?InputContextId.Unfocused:changingScene?InputContextId.Transition:Paused?InputContextId.Paused:
                photoMode==PhotographyMode.Body?InputContextId.ParkBody:photoMode==PhotographyMode.Meter?InputContextId.ParkMeter:
                photoMode==PhotographyMode.Aperture?InputContextId.ParkAperture:photoMode==PhotographyMode.Shutter?InputContextId.ParkShutter:
                photoMode==PhotographyMode.Viewfinder?InputContextId.ParkViewfinder:InputContextId.Exploration;
            var cursor=id==InputContextId.Unfocused?PlayerControlState.Unfocused:changingScene?PlayerControlState.Transition:
                Paused || needsWalkRecovery?PlayerControlState.Paused:photoMode==PhotographyMode.Exploration?PlayerControlState.Exploration:PlayerControlState.Operation;
            // Mechanical drag targets use the visible pointer, without a second relative cursor.
            // Exploration/viewfinder retain mouse-look and the existing edge gestures.
            InputContext.Select(id,cursor,CameraRaising || !PreciseInstrumentPointer);
        }
        public bool SaveForPauseMenu()=>!HasPendingExposure && SavePhotoSettings();
        public void ResumeFromPauseMenu(){inputGate=true;handoff.Arm();SyncParkInput();InputContext.Handoff();}
        bool CancelParkGesture()
        {
            if(dragPart==null)return false;
            CancelInstrumentDrag();inputGate=true;handoff.Arm();return true;
        }
        bool ExitParkContext()
        {
            return InputContext.ExitPressed(InputContext.Current,Paused?null:(System.Func<bool>)CancelParkGesture,()=>
            {
                if(Paused)Resume();
                else if(photoMode!=PhotographyMode.Exploration)SetPhotographyMode(PhotographyMode.Exploration);
                else GamePauseMenu.Open();
            });
        }
    }
}
