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
                bedroomLoadFailed?InputContextId.Paused:
                safeInspection!=null && safeInspection.Active?InputContextId.Inspection:
                radioDeck!=null && radioDeck.SuccessPopupOpen?InputContextId.Modal:
                transition>0 || loadingSpringRoom?InputContextId.Transition:
                ShowHelp?InputContextId.Paused:
                DirectDarkroomActive?(DarkroomWorld.ParkGalleryOpen?InputContextId.Gallery:InputContextId.Darkroom):
                radioDeck!=null && radioDeck.WorkbenchMode?InputContextId.TapeWorkbench:
                radioDeck!=null && radioDeck.RecordingMode?InputContextId.TapeTransport:
                InStation && CurrentWorld is PhotoWorld3D && ((PhotoWorld3D)CurrentWorld).ParkGalleryOpen?InputContextId.Gallery:
                InStation?NarrativeContext:InputContextId.Exploration;
            var cursor=id==InputContextId.Modal?PlayerControlState.Paused:ControlState;
            InputContext.Select(id,cursor,InStation && CurrentWorld!=null && CurrentWorld.WantsMouseLook,continueGesture);
        }
        public void PreparePauseInput()
        {
            if(safeInspection!=null)safeInspection.PreparePauseInput();
            if(radioDeck!=null)radioDeck.PreparePauseInput();
            if(DirectDarkroomActive)DarkroomWorld.CancelInteraction();
            else if(InStation && CurrentWorld!=null)CurrentWorld.CancelPointerGesture();
            SuspendHouseInput();
        }
        public bool ReturnFromTapeButton()
        {
            if(!HouseInputReady || ChapterIndex!=0 || radioDeck==null || radioDeck.SuccessPopupOpen || !radioDeck.CanReturnFromWorkspace || GamePauseMenu.BlocksGameplay)return false;
            return InputContext.RequestExit(InputContext.Current,null,radioDeck.ExitTapeWorkspace);
        }
        TapeReturnButton contextReturnButton;
        bool ContextReturnVisible=>houseReady && houseFocused && !ShowHelp && !IsTransitioning && !GamePauseMenu.BlocksGameplay &&
            !(safeInspection!=null && safeInspection.BlocksInput) &&
            (InputContext.Current==InputContextId.Gallery || (InStation &&
                (InputContext.Current==InputContextId.AutumnWriting || InputContext.Current==InputContextId.WinterObserve)));
        public bool PointerOverReturnButton=>contextReturnButton!=null && contextReturnButton.ContainsScreenPoint(Input.mousePosition);
        void EnsureContextReturnButton()
        {
            if(contextReturnButton==null)contextReturnButton=TapeReturnButton.Create(transform,()=>ContextReturnVisible,
                ()=>InputContext.Ready && (CurrentWorld==null || !CurrentWorld.HasPointerGesture),ReturnFromContextButton);
        }
        void ReturnFromContextButton()
        {
            if(!ContextReturnVisible)return;
            var id=InputContext.Current;
            InputContext.RequestExit(id,null,()=>{
                if(id==InputContextId.Gallery)
                { (DirectDarkroomActive?DarkroomWorld:(PhotoWorld3D)CurrentWorld).CloseParkGallery();SuspendHouseInput(); }
                else ExitHouseStation();
            });
        }
        bool ExitHouseContext()
        {
            var id=InputContext.Current;
            if(id==InputContextId.Paused)return InputContext.ExitPressed(id,null,()=>SetHelpVisible(false));
            if(id==InputContextId.Gallery)return InputContext.ExitPressed(id,null,()=>{(DirectDarkroomActive?DarkroomWorld:(PhotoWorld3D)CurrentWorld).CloseParkGallery();SuspendHouseInput();});
            if(id==InputContextId.Darkroom)return InputContext.ExitPressed(id,()=>DarkroomWorld.HasActiveGesture,()=>{});
            if(id==InputContextId.Station || id==InputContextId.AutumnWriting || id==InputContextId.WinterObserve || id==InputContextId.WinterBook || id==InputContextId.WardWalk)return InputContext.ExitPressed(id,
                ()=>CurrentWorld!=null && CurrentWorld.HasPointerGesture,
                ()=>{var painting=CurrentWorld as PaintingWorld3D;if(painting!=null && painting.ReturnFromBook()){SuspendHouseInput();return;}ExitHouseStation();});
            if(id==InputContextId.Exploration && DirectTapeHandling)return InputContext.ExitPressed(id,()=>true,()=>{});

            return false;
        }
    }
}
