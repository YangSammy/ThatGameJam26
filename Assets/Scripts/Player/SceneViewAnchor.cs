using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class SceneViewAnchor : MonoBehaviour
    {
        [Range(20,100)] public float fieldOfView=70;
        public void Apply(Camera camera){camera.transform.SetPositionAndRotation(transform.position,transform.rotation);camera.fieldOfView=fieldOfView;}
        void OnDrawGizmosSelected()
        {
            var previous=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=Color.cyan;
            Gizmos.DrawFrustum(Vector3.zero,fieldOfView,.8f,.05f,16f/9f);Gizmos.matrix=previous;
        }
    }
}
