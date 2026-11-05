using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        float doorViewBlend;
        bool doorViewNear;
        bool PagePendant=>paper!=null && key!=null && key.parent==paper;

        void BindPagePendant()
        {
            key.SetParent(paper,false);
            key.localPosition=layout.pagePendantAnchor!=null?layout.pagePendantAnchor.localPosition:new Vector3(-.49f,-.16f,-.055f);
            key.localRotation=layout.pagePendantAnchor!=null?layout.pagePendantAnchor.localRotation:Quaternion.identity;
            key.localScale=Vector3.one*.2f;
            keyHome=key.localPosition;
            Layer(key,PaperLayer);
            foreach(var surface in key.GetComponentsInChildren<Renderer>(true))
                foreach(var source in layout.observedPendant.GetComponentsInChildren<Renderer>(true))
                    if(surface.name==source.name){surface.sharedMaterials=source.sharedMaterials;break;}
            var hit=key.GetComponent<BoxCollider>();hit.center=new Vector3(0,.20f,0);hit.size=new Vector3(.60f,.65f,.20f);
        }

        void RestoreDoorView()
        {
            doorViewNear=State.Phase==1 && State.AtDoor;
            doorViewBlend=doorViewNear?1:0;
            ApplyDoorView();
        }

        void TickDoorView(float dt)
        {
            if(layout==null || State.Phase>=2 || keyDragging || DoorPerformanceActive)return;
            if(State.Phase!=1){doorViewNear=false;doorViewBlend=0;ApplyDoorView();return;}
            // Lowering the book retains its framing; it does not replay the approach.
            if(!State.BookOpen)return;
            float doorX=route.InverseTransformPoint(hole.position).x;
            if(State.AtDoor)doorViewNear=true;
            else if(State.Actor.x<doorX-1.5f || State.Actor.y<3)doorViewNear=false;
            doorViewBlend=Mathf.MoveTowards(doorViewBlend,doorViewNear?1:0,Mathf.Max(0,dt)/Mathf.Max(.1f,layout.doorViewSeconds));
            ApplyDoorView();
        }

        void ApplyDoorView()
        {
            if(layout==null || albumCamera==null || State.Phase>=2)return;
            float u=Mathf.SmoothStep(0,1,doorViewBlend);
            var target=route.InverseTransformPoint(hole.position);
            target+=new Vector3(layout.doorViewOffset.x,layout.doorViewOffset.y,0);target.z=-16;
            albumCamera.transform.position=route.TransformPoint(Vector3.Lerp(new Vector3(8,2,-16),target,u));
            albumCamera.orthographicSize=Mathf.Lerp(5.2f,layout.doorViewSize,u);
        }

        bool PendantDropMatches(Ray ray)
        {
            if(!State.AtDoor || !State.BookOpen || !State.Painted[4] || State.KeyInserted)return false;
            Ray bookRay;if(!PageRay(ray,out bookRay))return false;
            float d;var plane=new Plane(albumCamera.transform.forward,hole.position);
            if(!plane.Raycast(bookRay,out d))return false;
            if(!PagePendant)return Vector3.Distance(bookRay.GetPoint(d),hole.position)<.55f;
            var at=albumCamera.WorldToViewportPoint(bookRay.GetPoint(d));
            var target=albumCamera.WorldToViewportPoint(hole.position);
            var delta=new Vector2((at.x-target.x)*albumCamera.aspect,at.y-target.y);
            return target.z>0 && delta.magnitude<=layout.pendantDropRadius;
        }

        bool PendantRay(Ray ray,out Ray dragRay)
        {
            dragRay=ray;
            return PagePendant || PageRay(ray,out dragRay);
        }
    }
}
