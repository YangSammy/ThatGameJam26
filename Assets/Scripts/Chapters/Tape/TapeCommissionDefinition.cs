using System;
using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Tape Commission")]
    public sealed class TapeCommissionDefinition:ScriptableObject
    {
        public string title;
        public AudioClip sourceAudio;
        [Tooltip("使用的干净原声长度；0 使用整条音频。不循环、不覆盖原声。")]
        [Min(0)] public float cleanDuration;
        public int noiseSeed=861;
        [Serializable] public struct NoiseInsertion
        {
            [Tooltip("插入点：干净原声时间轴上的秒数。")]
            public float atCleanSecond;
            [Min(.01f)] public float duration;
            [Range(0,1)] public float amplitude;
        }
        public NoiseInsertion[] noise=new NoiseInsertion[0];
        public TapeAcceptanceRules acceptance;
    }
}
