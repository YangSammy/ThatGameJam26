using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        public bool CompleteStageShortcut(int key)
        {
            if(key<1 || key>5 || !ScenePresentationReady || changingScene || BrushSceneTransition.IsBusy || BrushSceneTransition.ExternalLoadActive)return false;
            if(HasPendingExposure){Notify("Wait for the photograph to finish saving, then retry.",6);return false;}
            if(key==2){DebugFillCurrentRoll();return true;}
            if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))return false;
            CancelInstrumentDrag();CancelCameraRaise();
            if(!SavePhotoSettings() || !TryLoadCommittedForExit())return false;
            StageCompletionShortcuts.PendingHouseStage=key;
            changingScene=true;ParkFilmRoute.EnterDarkroomOnLoad=false;
            SyncParkInput();InputContext.Handoff();
            try{BrushSceneTransition.LoadScene(ParkFilmRoute.HouseScenePath);return true;}
            catch(Exception error)
            {
                changingScene=false;StageCompletionShortcuts.PendingHouseStage=0;
                Notify("Stage shortcut unavailable. Saved progress kept.",6);Debug.LogWarning(error.Message);return false;
            }
        }
        // Darkroom test entry: use the same durable exposure pipeline as normal shots.
        void DebugFillCurrentRoll()
        {
            if(HasPendingExposure){Notify("Wait for the current photograph to finish saving, then press 2.",6);return;}
            if(!string.IsNullOrEmpty(filmError)){Notify("Test film unavailable. Existing records kept.",6);return;}
            try
            {
                bool completedNow;
                roll=DebugPhotographyFilm.FillCurrentRoll(out completedNow);
                if(!completedNow)
                {OpenFilmReturnPanel();Notify("Film already complete. Development progress kept.",5);return;}
                OpenFilmReturnPanel();
                Notify("DEV: roll filled with test negatives. Return to darkroom.",7);
                FilmFilled?.Invoke(roll.id);
            }
            catch(Exception error)
            {
                // Completed frames remain committed; pressing 2 again resumes missing slots.
                Notify("Test fill failed. Saved frames kept; press 2 to retry.",7);
                Debug.LogWarning("Debug film fill: "+error.Message,this);
            }
        }
    }
}
