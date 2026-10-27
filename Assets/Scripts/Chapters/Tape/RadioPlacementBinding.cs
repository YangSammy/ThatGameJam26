using UnityEngine;
namespace MemoryWhitebox
{
    // Keep the existing entry collider aligned when the artist moves/scales the model.
    [ExecuteAlways]
    public sealed class RadioPlacementBinding : MonoBehaviour
    {
        public Transform model;
        public BoxCollider body;
        Matrix4x4 lastMatrix;
        void OnEnable(){Sync();}
        void LateUpdate(){if(model!=null && model.localToWorldMatrix!=lastMatrix)Sync();}
        public void Sync()
        {
            if(model==null || body==null)return;
            bool first=true;Bounds bounds=new Bounds();
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                if(!r.enabled)continue;var b=r.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    var p=body.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            if(!first){body.center=bounds.center;body.size=bounds.size;}
            lastMatrix=model.localToWorldMatrix;
        }
    }
}
