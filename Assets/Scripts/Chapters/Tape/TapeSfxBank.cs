using System;
using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Tape SFX Bank")]
    public sealed class TapeSfxBank:ScriptableObject
    {
        [Serializable] public sealed class Cue
        {
            public int id;
            public AudioClip clip;
            [Min(0)] public float start;
            [Min(.02f)] public float duration=1;
            [Range(0,1)] public float volume=.25f;
        }
        public Cue[] cues;
        public Cue Find(int id){if(cues!=null)foreach(var cue in cues)if(cue.id==id)return cue;return null;}
    }
}
