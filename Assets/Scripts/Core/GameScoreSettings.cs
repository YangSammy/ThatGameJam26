using UnityEngine;

namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Game Score")]
    public sealed class GameScoreSettings : ScriptableObject
    {
        [Tooltip("Day one through day four. Day five has its own arrangement.")]
        public AudioClip[] roomSeasons = new AudioClip[4];
        public AudioClip parkLoop, photoResponse;
        public AudioClip letterLoop, farewell;
        public AudioClip winterPark, winterRoom, ward;
        public AudioClip springWaiting, springDoor, theme;
        [Range(0,1)] public float gain=.18f;
    }
}
