using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        readonly HoverOutline[] emptyShapeInk={new HoverOutline(),new HoverOutline()};
        readonly GameObject[] emptyShapes=new GameObject[2];
        readonly Renderer[][] emptyShapeSurfaces=new Renderer[2][];
        void ShowEmptyShapes()
        {
            for(int i=0;i<2;i++)
            {
                if(State.Phase!=0 || State.Painted[i]){emptyShapeInk[i].Clear();continue;}
                var source=i==0?winterSwing.transform:winterBridge.transform;
                if(emptyShapes[i]==null)
                {
                    var group=new GameObject(i==0?"Missing swing outline":"Missing bridge outline");
                    group.transform.SetParent(source.parent,false);emptyShapes[i]=group;
                    foreach(var mesh in source.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if(!mesh.sharedMesh || mesh.name.Contains("Hit") || mesh.name.Contains("Proxy"))continue;
                        var part=new GameObject(mesh.name,typeof(HoverOutlineProxy));part.layer=mesh.gameObject.layer;
                        part.transform.SetParent(group.transform,false);part.transform.SetPositionAndRotation(mesh.transform.position,mesh.transform.rotation);
                        var scale=group.transform.lossyScale;var world=mesh.transform.lossyScale;
                        part.transform.localScale=new Vector3(world.x/scale.x,world.y/scale.y,world.z/scale.z);
                        part.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                    }
                    emptyShapeSurfaces[i]=group.GetComponentsInChildren<Renderer>(true);
                }
                emptyShapeInk[i].Show(albumCamera,emptyShapeSurfaces[i],dashed:true);
            }
        }
        void DisposeEmptyShapes(){for(int i=0;i<2;i++){emptyShapeInk[i].Dispose();if(emptyShapes[i])ReleaseObject(emptyShapes[i]);}}
    }
}
