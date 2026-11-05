using UnityEngine;

namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Supplemental Score Layers")]
    public sealed class GameScoreLayers : ScriptableObject
    {
        [System.Serializable] public sealed class Layer
        {
            public string role;
            public AudioClip clip;
            [Range(0,1)] public float gain=.2f;
            [Range(0,1)] public float enterAt;
            public bool environment;
        }
        [System.Serializable] public sealed class Arrangement
        {
            public AudioClip sourceMix;
            public Layer[] layers;
        }
        [Tooltip("New supplemental composition, not separated original instruments.")]
        public Arrangement[] arrangements;
        [Range(0,1)] public float environmentGain=.12f;
        public Arrangement Find(AudioClip source)
        {
            if(source==null || arrangements==null)return null;
            foreach(var item in arrangements)if(item!=null && item.sourceMix==source)return item;
            return null;
        }
    }
}
