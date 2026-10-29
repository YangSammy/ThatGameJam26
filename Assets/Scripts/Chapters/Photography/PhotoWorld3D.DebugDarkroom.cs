#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
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
#endif
