using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation only. No input, inventory ownership or checkpoint data lives here.
    public sealed class WorkbenchAssetView:MonoBehaviour
    {
        public AnimationClip[] clips;
        public Transform leftReel,rightReel;
        public SkinnedMeshRenderer leftWinding,rightWinding;
        public Transform[] patches=new Transform[2];
        public SkinnedMeshRenderer[] patchMeshes=new SkinnedMeshRenderer[2];
        readonly Dictionary<string,AnimationClip> library=new Dictionary<string,AnimationClip>();
        public void Sample(string name,float normalized)
        {
            if(library.Count==0)foreach(var c in clips)if(c!=null)library[c.name]=c;
            if(library.TryGetValue(name,out var clip))clip.SampleAnimation(gameObject,Mathf.Clamp01(normalized)*clip.length);
        }
        public static float EmptyWeight(float fraction)
        {return (0.020f-Mathf.Sqrt(.0085f*.0085f+Mathf.Clamp01(fraction)*(.020f*.020f-.0085f*.0085f)))/(.020f-.0085f)*100;}
        public void Winding(float p)
        {
            if(leftWinding!=null)leftWinding.SetBlendShapeWeight(0,EmptyWeight(1-p));
            if(rightWinding!=null)rightWinding.SetBlendShapeWeight(0,EmptyWeight(p));
        }
        public void Cover(int side,float value)
        {
            if(patches[side]==null)return;
            patches[side].localScale=Vector3.one*(value<=0?.001f:1);
            if(patchMeshes[side]!=null)patchMeshes[side].SetBlendShapeWeight(0,value<=0?0:(1-Mathf.Clamp01(value))*200);
        }
        public void Reel(int side,float degrees)
        {var t=side==0?leftReel:rightReel;if(t!=null)t.localRotation=Quaternion.AngleAxis(degrees,Vector3.up);}
    }
}
