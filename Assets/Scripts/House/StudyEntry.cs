using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class StudyEntry : MonoBehaviour
    {
        public int chapter;
        static readonly RaycastHit[] blockers=new RaycastHit[64];
        public static Collider PickVisible(Ray ray)
        {
            RaycastHit hit;
            if(!Physics.Raycast(ray,out hit,2.5f,1<<25,QueryTriggerInteraction.Collide))return null;
            var inspection=hit.collider.GetComponentInParent<InspectionEntry>();
            var ownRoot=inspection!=null?inspection.transform:hit.collider.transform;
            // Interaction proxies do not make opaque walls or furniture transparent.
            int mask=~((1<<25)|(1<<26)|(1<<27)|(1<<29));
            int count=Physics.RaycastNonAlloc(ray,blockers,Mathf.Max(0,hit.distance-.01f),mask,QueryTriggerInteraction.Ignore);
            if(count==blockers.Length)return null;
            for(int i=0;i<count;i++)
            {
                // Low invisible navigation footprints (e.g. a coat rack's base) are not opaque geometry.
                var body=blockers[i].collider;
                if(body.name.StartsWith("Walk blocker") && body.GetComponent<Renderer>()==null && body.bounds.max.y<1.65f)continue;
                if(!blockers[i].transform.IsChildOf(ownRoot))return null;
            }
            return hit.collider;
        }
        public static StudyEntry Pick(Camera camera)
        {
            var hit=PickVisible(camera.ViewportPointToRay(new Vector3(.5f,.5f)));
            return hit!=null?hit.GetComponentInParent<StudyEntry>():null;
        }
    }
}
