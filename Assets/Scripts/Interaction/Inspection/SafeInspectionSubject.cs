using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class SafeInspectionSubject : MonoBehaviour
    {
        [Serializable] public sealed class PosePair { public Transform source,display; }
        [Serializable] public sealed class RenderPair { public Renderer source,display; [NonSerialized] public MaterialPropertyBlock properties; }
        public string title;
        public Transform source,displayRoot,content;
        public bool fixedDevice;
        // New assets: front = local -Z, up = local +Y. Legacy assets retain authored corrections.
        public Vector3 displayEuler=Vector3.zero;
        [HideInInspector] public Transform mainFaceFrame;
        [HideInInspector] public Vector3 mainFaceNormal=Vector3.back;
        [HideInInspector] public Vector3 mainFaceUp=Vector3.up;
        [HideInInspector] public Vector3 mainFaceTilt;
        public Quaternion InitialRotation()
        {
            var correction=Quaternion.Euler(displayEuler);
            if(mainFaceFrame==null || source==null)return correction;
            var toRoot=Quaternion.Inverse(source.rotation)*mainFaceFrame.rotation;
            var front=toRoot*mainFaceNormal;
            var up=toRoot*mainFaceUp;
            if(front.sqrMagnitude<.0001f || Vector3.Cross(front,up).sqrMagnitude<.0001f)return correction;
            // Evaluate against the actual model frame, independent of placement and loaded/held poses.
            return correction*Quaternion.Euler(mainFaceTilt)*Quaternion.Inverse(Quaternion.LookRotation(-front,up));
        }
        public Vector3 centerOffset;
        // Cached framing geometry, refreshed from visible source meshes on every inspection.
        public Vector3 localCenter;
        public float diameter=1;
        public PosePair[] poses;
        public RenderPair[] renderers;
        public SafeInspectionPart[] parts;
        public Collider[] surfaces;
        public StudyEntry entry;
        bool[] sourceHidden;

        public void RefreshFraming()
        {
            if(source==null || renderers==null)return;
            bool found=false;var bounds=new Bounds();
            var inverseRotation=Quaternion.Inverse(source.rotation);
            foreach(var pair in renderers)
            {
                var renderer=pair.source;
                if(renderer==null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                var filter=renderer.GetComponent<MeshFilter>();
                bool mesh=filter!=null && filter.sharedMesh!=null;
                var box=mesh?filter.sharedMesh.bounds:renderer.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    if(mesh)p=renderer.transform.TransformPoint(p);
                    // Display root has unit scale; source scaling is already copied onto its content.
                    p=inverseRotation*(p-source.position);
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
            }
            if(!found)return;
            localCenter=bounds.center+centerOffset;
            diameter=Mathf.Max(.01f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
        }

        public void Begin()
        {
            // Local transforms are copied only between equivalent parents. Display root placement is owned by the viewer.
            foreach(var p in poses)
            {
                p.display.localPosition=p.source==source?Vector3.zero:p.source.localPosition;
                p.display.localRotation=p.source==source?Quaternion.identity:p.source.localRotation;
                p.display.localScale=p.source==source?source.lossyScale:p.source.localScale;
                if(p.source!=source)p.display.gameObject.SetActive(p.source.gameObject.activeSelf);
            }
            sourceHidden=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++)
            {
                var p=renderers[i];sourceHidden[i]=p.source.forceRenderingOff;
                p.display.sharedMaterials=p.source.sharedMaterials;p.display.enabled=p.source.enabled;
                var surface=p.display.GetComponent<Collider>();if(surface!=null)surface.enabled=p.source.enabled;
                if(p.properties==null)p.properties=new MaterialPropertyBlock();
                p.source.GetPropertyBlock(p.properties);p.display.SetPropertyBlock(p.properties);
                p.source.forceRenderingOff=true;
            }
            foreach(var part in parts)part.Capture();
            displayRoot.gameObject.SetActive(true);
        }
        public void End()
        {
            foreach(var part in parts)if(part!=null)part.Restore();
            if(sourceHidden!=null)for(int i=0;i<renderers.Length;i++)if(renderers[i].source!=null)renderers[i].source.forceRenderingOff=sourceHidden[i];
            sourceHidden=null;if(displayRoot!=null)displayRoot.gameObject.SetActive(false);
        }
        public SafeInspectionPart Pick(Ray ray)
        {
            float nearest=float.PositiveInfinity;SafeInspectionPart result=null;
            foreach(var surface in surfaces)
            {
                RaycastHit hit;
                if(surface!=null && surface.enabled && surface.gameObject.activeInHierarchy && surface.Raycast(ray,out hit,20) && hit.distance<nearest)
                {nearest=hit.distance;result=surface.GetComponentInParent<SafeInspectionPart>();}
            }
            return result;
        }
    }
}
