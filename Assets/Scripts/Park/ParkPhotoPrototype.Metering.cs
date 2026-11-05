using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        [Header("Regional incident metering")]
        public ParkRegionalMetering regionalMetering;
        public float MeterEv100 {get;private set;}=13;
        void UpdateRegionalMetering()
        {
            if(regionalMetering==null){MeterEv100=sceneEv100;return;}
            MeterEv100=regionalMetering.Sample(view!=null?view.transform.position:transform.position);
            // Measure where the subject is lit, not the reference's saved EV.
            var subject=companion!=null && companion.chest!=null?companion.chest.position:transform.position;
            sceneEv100=regionalMetering.Sample(subject);
        }
        string OpeningPhotoInstruction()
        {
            if(roll==null)return "Film unavailable. Leave and retry; saved progress is kept.";
            if(roll.ReadyForPark)return "WASD: walk. E: inspect camera; click eyepiece to compose.";
            return "Your companion is waiting. E: open camera and load film.";
        }
    }
}
