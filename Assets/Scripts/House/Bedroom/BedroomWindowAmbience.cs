using UnityEngine;

namespace MemoryWhitebox
{
    // Scene-owned acoustic anchor; GameAmbience remains the sole owner of seasonal playback.
    public sealed class BedroomWindowAmbience : MonoBehaviour
    {
        [Range(0,3)] public int season;
        [Min(.1f)] public float nearDistance=1.25f;
        [Min(.2f)] public float farDistance=7;
        [Range(0,360)] public float spread=120;

        public void Configure(AudioSource source)
        {
            source.transform.position=transform.position;
            source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Linear;
            source.minDistance=Mathf.Max(.1f,nearDistance);
            source.maxDistance=Mathf.Max(source.minDistance+.1f,farDistance);
            source.spread=spread;
            source.dopplerLevel=0;
        }
    }
}
