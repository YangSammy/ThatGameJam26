using UnityEngine;
namespace MemoryWhitebox
{
    // The click volume belongs to the actual moving lid, never a world-space anchor.
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class TapeStorageHitBinding : MonoBehaviour
    {
        public MeshFilter lidMesh;
        public BoxCollider clickBox;
        void OnEnable(){Sync();}
        void LateUpdate(){Sync();}
        public void Sync()
        {
            if(!lidMesh || !lidMesh.sharedMesh || !clickBox)return;
            var bounds=lidMesh.sharedMesh.bounds;
            var local=new Bounds();
            for(int i=0;i<8;i++)
            {
                var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                p=clickBox.transform.InverseTransformPoint(lidMesh.transform.TransformPoint(p));
                if(i==0)local=new Bounds(p,Vector3.zero);else local.Encapsulate(p);
            }
            var scale=clickBox.transform.lossyScale;
            var size=Vector3.Max(local.size,new Vector3(.004f/Mathf.Max(.0001f,Mathf.Abs(scale.x)),.004f/Mathf.Max(.0001f,Mathf.Abs(scale.y)),.004f/Mathf.Max(.0001f,Mathf.Abs(scale.z))));
            if(clickBox.center!=local.center)clickBox.center=local.center;
            if(clickBox.size!=size)clickBox.size=size;
        }
    }
}
