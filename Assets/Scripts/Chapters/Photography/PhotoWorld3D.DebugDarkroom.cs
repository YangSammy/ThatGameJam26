using System;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        public bool EditorCompleteDarkroom(out string message)
        {
            if(!DebugPrepareDarkroom(out message))return false;
            if(ParkPrintsReady){message="Prints already ready. Click a hanging print to view.";return true;}
            CancelInteraction();
            // Advance to the final stable physical operation, then let normal drying,
            // SaveDevelopment and the gallery/season transition publish completion.
            darkroomWorkflow=3;tankSealed=true;needsDrain=false;
            Photo.Phase=PhotoChapter.FilmPhase.Tank;Photo.Chemistry=3;
            Photo.ProcessingTime=0;Photo.BagTurns=1;Photo.Agitated=true;
            RewindTurns=1;RewoundBackOpen=true;clockDuration=0;
            printObjectsState=new DarkroomPrintState{negativeRemoved=true};
            printObjectsState.negative.locationId="print-frame";
            printObjectsState.paper.locationId="print-photo-clip";
            printStage=ParkPrintStage.PaperDrying;printRemaining=.8f;printProcessing=true;printHeld=false;
            lastSavedState="";
            if(!SaveProgress())
            {
                parkFilm=null;lastSavedState="";LoadParkFilm();SyncProps();
                message="Print completion not saved. Previous checkpoint restored; retry after fixing storage.";return false;
            }
            LoadContactTexture();SyncProps();
            message="Prints drying. Click the hanging print when ready.";
            return true;
        }
        public bool DebugPrepareDarkroom(out string message)
        {
            // Save the live timer/placements before touching disk. Repeated use never rewinds a roll.
            CancelInteraction();
            if(!SaveProgress())
            {message="Progress not saved. Test shortcut cancelled; press 2 to retry.";return false;}
            bool wasParkMode=ParkMode;
            try
            {
                bool completedNow;
                DebugPhotographyFilm.FillCurrentRoll(out completedNow);
                if(!wasParkMode)
                {
                    parkLoading=false;loadingFilm=null;lastSavedState="";
                    LoadParkFilm();
                    if(parkFilmUnavailable || !ParkMode)
                    {message="Film saved, but darkroom could not load. Records kept.";return false;}
                    SyncProps();
                }
                message=completedNow?"DEV: test roll ready. Begin developing.":"DEV: film already full. Development progress kept.";
                return true;
            }
            catch(Exception error)
            {
                // A partially filled roll is valid. Refresh loading state so autosave cannot
                // write the previous empty/half-loaded camera state over committed exposures.
                if(!wasParkMode){parkLoading=false;loadingFilm=null;lastSavedState="";LoadParkFilm();SyncProps();}
                message="Test fill failed. Saved frames kept; press 2 to retry.";
                UnityEngine.Debug.LogWarning("House debug film: "+error.Message);
                return false;
            }
        }
    }
}
