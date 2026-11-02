using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Game Ambience")]
    public sealed class GameAmbienceSettings:ScriptableObject
    {
        public AudioClip globalLoop;
        [Tooltip("Spring, Summer, Autumn, Winter. Day five reuses Spring.")]
        public AudioClip[] seasons=new AudioClip[4];
        [Range(0,1)] public float globalVolume=.16f,seasonVolume=.22f;
        [Min(.1f)] public float fadeSeconds=2;
    }
}
