using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        readonly List<Vector3> letterFramePoints=new List<Vector3>();
        readonly List<Vector3> binFramePoints=new List<Vector3>();

        void CacheLetterFraming()
        {
            letterFramePoints.Clear();binFramePoints.Clear();
            var paper=desk.paper;var t=paper.transform;
            for(int i=0;i<8;i++)
                letterFramePoints.Add(paper.center+Vector3.Scale(paper.size*.5f,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            foreach(var r in desk.timer.GetComponentsInChildren<Renderer>(true))
            {
                var b=r.bounds;
                for(int i=0;i<8;i++)letterFramePoints.Add(t.InverseTransformPoint(b.center+Vector3.Scale(b.extents,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            }
            foreach(var hit in desk.basketHits)
            {
                for(int i=0;i<8;i++)binFramePoints.Add(t.InverseTransformPoint(hit.transform.TransformPoint(hit.center+Vector3.Scale(hit.size*.5f,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))));
            }
        }

        void FramingExtent(float aspect,bool includeBin,out Vector3 look,out float distance)
        {
            var t=desk.paper.transform;
            var b=new Bounds(letterFramePoints[0],Vector3.zero);
            foreach(var p in letterFramePoints)b.Encapsulate(p);
            if(includeBin)foreach(var p in binFramePoints)b.Encapsulate(p);
            look=t.TransformPoint(new Vector3(b.center.x,desk.paper.center.y,b.center.z));
            float required=.1f,tangent=Mathf.Tan(48*Mathf.Deg2Rad*.5f);
            // Include height: the raised timer sits closer to the lens than the sheet.
            foreach(var p in letterFramePoints)required=Mathf.Max(required,FrameDistance(t.TransformPoint(p)-look,t,aspect,tangent));
            if(includeBin)foreach(var p in binFramePoints)required=Mathf.Max(required,FrameDistance(t.TransformPoint(p)-look,t,aspect,tangent));
            distance=required;
        }

        static float FrameDistance(Vector3 point,Transform paper,float aspect,float tangent)
        {
            const float safeFrame=.86f;
            float x=Mathf.Abs(Vector3.Dot(point,paper.right)),z=Mathf.Abs(Vector3.Dot(point,paper.forward));
            return Mathf.Max(x/(tangent*Mathf.Max(.15f,aspect)*safeFrame),z/(tangent*safeFrame))+Vector3.Dot(point,paper.up);
        }

        public void CalculateLetterFrame(float viewportAspect,float binAmount,out Vector3 eye,out Vector3 look)
        {
            FramingExtent(viewportAspect,false,out var close,out var closeDistance);
            FramingExtent(viewportAspect,true,out var wide,out var wideDistance);
            look=Vector3.Lerp(close,wide,Mathf.Clamp01(binAmount));
            eye=look+desk.paper.transform.up*Mathf.Lerp(closeDistance,wideDistance,Mathf.Clamp01(binAmount));
        }
    }
}
