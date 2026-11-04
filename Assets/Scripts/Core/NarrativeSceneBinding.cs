using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class NarrativeSceneBinding:MonoBehaviour
    {
        public Transform entranceDoor,doorHandle,threshold,windowAnchor;
        [Min(.2f)] public float doorwayHalfWidth=.52f;
        public float openAngle=-100;
    }
}
