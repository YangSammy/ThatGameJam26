using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        public PlayerInputContext InputContext {get;}=new PlayerInputContext();
        public void AdoptPointerGesture()
        {houseReturnGate=false;houseHandoff.AdoptGesture();SyncInputContext(true);InputContext.ContinueGesture();}
        public void SyncInputContext(bool continueGesture=false)
        {
            var id=!houseFocused?InputContextId.Unfocused:
                safeInspection!=null && safeInspection.Active?InputContextId.Inspection:
                transition>0?InputContextId.Transition:
                radioDeck!=null && radioDeck.SuccessPopupOpen?InputContextId.Modal:
                ShowHelp?InputContextId.Paused:
                DirectDarkroomActive?(DarkroomWorld.ParkGalleryOpen?InputContextId.Gallery:InputContextId.Darkroom):
                radioDeck!=null && radioDeck.WorkbenchMode?InputContextId.TapeWorkbench:
                radioDeck!=null && radioDeck.RecordingMode?InputContextId.TapeTransport:
                InStation && CurrentWorld is PhotoWorld3D && ((PhotoWorld3D)CurrentWorld).ParkGalleryOpen?InputContextId.Gallery:
                InStation?InputContextId.Station:InputContextId.Exploration;
            var cursor=id==InputContextId.Modal?PlayerControlState.Paused:ControlState;
            InputContext.Select(id,cursor,InStation && CurrentWorld!=null && CurrentWorld.WantsMouseLook,continueGesture);
        }
        bool ExitHouseContext()
        {
            var id=InputContext.Current;
            if(id==InputContextId.Paused)return InputContext.ExitPressed(id,null,()=>SetHelpVisible(false));
            if(id==InputContextId.Gallery)return InputContext.ExitPressed(id,null,()=>{(DirectDarkroomActive?DarkroomWorld:(PhotoWorld3D)CurrentWorld).CloseParkGallery();SuspendHouseInput();});
            if(id==InputContextId.Darkroom)return InputContext.ExitPressed(id,()=>
            {
                if(!DarkroomWorld.HasActiveGesture)return false;
                DarkroomWorld.CancelInteraction();DarkroomWorld.SaveProgress();darkroomWasLeft=false;SuspendHouseInput();return true;
            },()=>SetHelpVisible(true));
            if(id==InputContextId.Station)return InputContext.ExitPressed(id,()=>
            {
                var photo=CurrentWorld as PhotoWorld3D;
                if(photo==null || !photo.HasActiveGesture)return false;
                photo.CancelInteraction();SuspendHouseInput();return true;
            },ExitHouseStation);
            if(id==InputContextId.Exploration && DirectTapeHandling)return InputContext.ExitPressed(id,null,()=>{radioDeck.PutBack();SyncTapePointer();SuspendHouseInput();});
            return false;
        }
    }
}
