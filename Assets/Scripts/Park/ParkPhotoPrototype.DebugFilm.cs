#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        // Darkroom test entry: use the same durable exposure pipeline as normal shots.
        void DebugFillCurrentRoll()
        {
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
#endif
