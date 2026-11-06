using UnityEngine;

namespace MemoryWhitebox
{
    // Stable authoring parent; gameplay continues to own the existing child targets.
    [DisallowMultipleComponent]
    public sealed class DarkroomLayoutItem : MonoBehaviour
    {
        [Tooltip("Move and rotate this parent in Edit Mode. Children contain the model, hits and placement anchors.")]
        public string itemId;
        public Transform primaryTarget;
        [Tooltip("Camera-only layout preview. The mechanical camera is bound here at runtime.")]
        public GameObject scenePreview;
        void Awake(){if(scenePreview)scenePreview.SetActive(false);}
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(1,.65f,.12f,.8f);
            foreach(var box in GetComponentsInChildren<BoxCollider>(true))
            {
                Gizmos.matrix=box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center,box.size);
            }
            Gizmos.matrix=Matrix4x4.identity;
        }
    }
}
