using System;
using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Tape Commission")]
    public sealed class TapeCommissionDefinition:ScriptableObject
    {
        public string title;
        public AudioClip sourceAudio;
        [Tooltip("Source WAV already contains the configured noise intervals. Do not insert them again.")]
        public bool sourceContainsNoise;
        [Tooltip("Old definition retained only to resume recordings whose PCM hash matches it.")]
        public TapeCommissionDefinition legacyDefinition;
        public AudioClip noiseAudio;
        [Min(0)] public float noiseAudioStart;
        [Serializable] public sealed class SourceLayer
        {
            public string label;
            public AudioClip clip;
            [Min(0)] public float at, trimStart;
            [Min(.01f)] public float duration=1;
            [Range(0,1)] public float gain=1;
        }
        public SourceLayer[] sourceLayers=new SourceLayer[0];
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
