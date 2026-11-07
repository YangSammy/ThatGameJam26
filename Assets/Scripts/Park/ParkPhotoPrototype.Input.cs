using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        public PlayerInputContext InputContext {get;}=new PlayerInputContext(ignoreLocomotionForRelease:true);
        bool applicationPaused;
        void SyncCompanionControl()
        {
            if(companion==null)return;
            // Instruments hold locomotion, not breathing, turning or the offered hand.
            companion.Paused=Paused || !focused || applicationPaused || changingScene || GamePauseMenu.BlocksGameplay;
            companion.TravelPaused=companion.Paused || !GuidedCanTravel;
        }
        void SyncParkInput()
        {
            var id=!focused || applicationPaused?InputContextId.Unfocused:changingScene?InputContextId.Transition:Paused?InputContextId.Paused:
                photoMode==PhotographyMode.Body?InputContextId.ParkBody:photoMode==PhotographyMode.Meter?InputContextId.ParkMeter:
                photoMode==PhotographyMode.Aperture?InputContextId.ParkAperture:photoMode==PhotographyMode.Shutter?InputContextId.ParkShutter:
                photoMode==PhotographyMode.Viewfinder?InputContextId.ParkViewfinder:InputContextId.Exploration;
            var cursor=id==InputContextId.Unfocused?PlayerControlState.Unfocused:changingScene?PlayerControlState.Transition:
                Paused || needsWalkRecovery?PlayerControlState.Paused:photoMode==PhotographyMode.Exploration?PlayerControlState.Exploration:PlayerControlState.Operation;
            // Mechanical drag targets use the visible pointer, without a second relative cursor.
            // Camera modes share the real visible pointer. Metering still aims with mouse-look.
            InputContext.Select(id,cursor,CameraRaising || !PreciseInstrumentPointer);
        }
        public bool SaveForPauseMenu()=>!HasPendingExposure && SavePhotoSettings();
        public void PreparePauseInput(){moveVelocity=Vector3.zero;footsteps.Reset();ResetCameraEdgeGesture();CancelInstrumentDrag();inputGate=true;handoff.Arm();InputContext.Handoff();}
        public void ResumeFromPauseMenu(){moveVelocity=Vector3.zero;footsteps.Reset();ResetCameraEdgeGesture();inputGate=true;handoff.Arm();SyncParkInput();InputContext.Handoff();}
        bool CancelParkGesture()
        {
            if(dragPart==null)return false;
            CancelInstrumentDrag();inputGate=true;handoff.Arm();return true;
        }
        bool ExitParkContext()
        {
            // A completed roll must keep its return-to-darkroom action available.
            if(Paused)return false;
            return InputContext.ExitPressed(InputContext.Current,()=>dragPart!=null,LeaveParkContext);
        }
        void LeaveParkContext()
        {
            if(CameraDetailMode)SetPhotographyMode(PhotographyMode.Viewfinder);
            else if(photoMode!=PhotographyMode.Exploration)SetPhotographyMode(PhotographyMode.Exploration);
        }
    }
}
