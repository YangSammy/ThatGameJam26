using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        public PlayerInputContext InputContext {get;}=new PlayerInputContext();
        bool applicationPaused;
        void SyncParkInput()
        {
            var id=!focused || applicationPaused?InputContextId.Unfocused:changingScene?InputContextId.Transition:Paused?InputContextId.Paused:
                photoMode==PhotographyMode.Body?InputContextId.ParkBody:photoMode==PhotographyMode.Meter?InputContextId.ParkMeter:
                photoMode==PhotographyMode.Aperture?InputContextId.ParkAperture:photoMode==PhotographyMode.Shutter?InputContextId.ParkShutter:
                photoMode==PhotographyMode.Viewfinder?InputContextId.ParkViewfinder:InputContextId.Exploration;
            var cursor=id==InputContextId.Unfocused?PlayerControlState.Unfocused:changingScene?PlayerControlState.Transition:
                Paused?PlayerControlState.Paused:photoMode==PhotographyMode.Exploration?PlayerControlState.Exploration:PlayerControlState.Operation;
            // Instrument modes use the existing virtual pointer while the OS cursor stays locked.
            InputContext.Select(id,cursor,true);
        }
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
                else PauseControls();
            });
        }
    }
}
