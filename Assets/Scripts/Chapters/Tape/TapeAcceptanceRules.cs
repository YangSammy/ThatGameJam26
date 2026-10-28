using System;
using UnityEngine;
namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="Memory/Tape Acceptance Rules")]
    public sealed class TapeAcceptanceRules:ScriptableObject
    {
        [Range(0,1)] public float minimumCleanCoverage=.95f;
        [Min(0)] public float allowedNoiseSeconds=.25f;
        [Min(0)] public float allowedSilenceSeconds=.25f;
        [Serializable] public struct RequiredSound
        {
            public string label;
            [Tooltip("必须保留的干净原声区间，单位秒。")]
            public float start,end;
            [Range(0,1)] public float minimumCoverage;
        }
        public RequiredSound[] requiredSounds=new RequiredSound[0];
    }
}
