using UnityEngine;
namespace MemoryWhitebox
{
    // References only: existing gameplay and save models remain authoritative.
    public sealed class PhotographyVisualAssets : ScriptableObject
    {
        public GameObject cameraBody,lens,meter;
        public GameObject[] films;
        public AnimationClip leaderReady,pullLeader,seatLeader,advanced,advance,rewind,retracted;
        public Material focusEffect;
        public static PhotographyVisualAssets Load()=>Resources.Load<PhotographyVisualAssets>("PhotographyVisualAssets");
        public GameObject Film(int iso)=>films[iso==200?1:iso==400?2:iso==800?3:0];
    }
}
