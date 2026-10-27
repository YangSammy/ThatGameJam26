using UnityEngine;

namespace MemoryWhitebox
{
    // Scene-side link only. Gameplay scripts are never copied to the inspection display.
    [DisallowMultipleComponent]
    [AddComponentMenu("检视/物品检视入口")]
    public sealed class InspectionEntry : MonoBehaviour
    {
        [HideInInspector] public SafeInspectionSubject subject;
        [HideInInspector] public BoxCollider pickup;
    }
}
