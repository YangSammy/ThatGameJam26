using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class ParkPhotoWaypoint : MonoBehaviour
    {
        public enum PhotoTask { None, Entrance, Bridge, Meadow }
        public PhotoTask photoTask;
        [Tooltip("Optional shared contact point for the companion and player's hand.")]
        public Transform handContact;
        [Tooltip("人物抵达后自然停留的时间；最后一点会一直等候。")][Min(0)] public float pauseSeconds;
        [Tooltip("停留时望向的场景定位物体。") ] public Transform lookAt;
        [Tooltip("在这里停留时可以拍到望水瞬间。") ] public bool watersideMoment;
        [Header("Photography reference (optional)")]
        public Transform referenceView;
        public Texture2D referencePhoto;
        [Range(0,20)] public float referenceEv100=13;
        void OnDrawGizmos(){Gizmos.color=watersideMoment?Color.cyan:Color.yellow;Gizmos.DrawWireSphere(transform.position,.22f);if(lookAt!=null)Gizmos.DrawLine(transform.position+Vector3.up,lookAt.position);}
    }
}
