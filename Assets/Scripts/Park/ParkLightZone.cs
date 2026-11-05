using UnityEngine;

namespace MemoryWhitebox
{
    // Authored incident-light volume. No collider: it cannot intercept a photo ray
    // or block walking. EV is calibrated to the existing art, not inferred from RGB.
    public sealed class ParkLightZone : MonoBehaviour
    {
        public Vector3 size=new Vector3(10,8,10);
        [Range(0,20)] public float ev100=13;
        [Min(.01f)] public float blendDistance=2;
        public int priority;
        public float Weight(Vector3 worldPosition)
        {
            if(!isActiveAndEnabled)return 0;
            var p=transform.InverseTransformPoint(worldPosition);
            var half=size*.5f;
            float inside=Mathf.Min(half.x-Mathf.Abs(p.x),half.y-Mathf.Abs(p.y),half.z-Mathf.Abs(p.z));
            return Mathf.SmoothStep(0,1,Mathf.Clamp01(inside/Mathf.Max(.01f,blendDistance)));
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=new Color(1,.8f,.2f,.65f);
            Gizmos.DrawWireCube(Vector3.zero,size);
        }
    }
}
