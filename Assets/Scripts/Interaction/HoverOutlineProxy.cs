using UnityEngine;

namespace MemoryWhitebox
{
    // An outline-only copy of geometry already present in an observation background.
    // Disabled renderer, no collider, and no independent gameplay identity.
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class HoverOutlineProxy : MonoBehaviour
    {
        void Awake(){GetComponent<MeshRenderer>().enabled=false;}
    }
}
