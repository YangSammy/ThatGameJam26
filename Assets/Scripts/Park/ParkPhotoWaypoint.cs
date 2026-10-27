using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class ParkPhotoWaypoint : MonoBehaviour
    {
        [Tooltip("人物抵达后自然停留的时间；最后一点会一直等候。")][Min(0)] public float pauseSeconds;
        [Tooltip("停留时望向的场景定位物体。") ] public Transform lookAt;
        [Tooltip("在这里停留时可以拍到望水瞬间。") ] public bool watersideMoment;
        void OnDrawGizmos(){Gizmos.color=watersideMoment?Color.cyan:Color.yellow;Gizmos.DrawWireSphere(transform.position,.22f);if(lookAt!=null)Gizmos.DrawLine(transform.position+Vector3.up,lookAt.position);}
    }
}
