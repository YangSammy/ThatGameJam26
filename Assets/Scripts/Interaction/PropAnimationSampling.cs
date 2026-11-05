using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    // Transform/blend-shape poses for props. Runtime copies leave imported clips intact.
    public static class PropAnimationSampling
    {
        public static void SamplePropAnimation(this AnimationClip clip, GameObject root, float time)
        {
            if(Application.isPlaying && !clip.legacy)
            {
                var samples=root.GetComponent<PropAnimationSamples>();
                if(!samples)samples=root.AddComponent<PropAnimationSamples>();
                clip=samples.For(clip);
            }
            clip.SampleAnimation(root,time);
        }
    }

    internal sealed class PropAnimationSamples : MonoBehaviour
    {
        readonly Dictionary<AnimationClip,AnimationClip> copies=new Dictionary<AnimationClip,AnimationClip>();
        public AnimationClip For(AnimationClip source)
        {
            if(!copies.TryGetValue(source,out var copy))
            {
                copy=Instantiate(source);
                copy.name=source.name+" (prop pose)";
                copy.legacy=true;
                copies.Add(source,copy);
            }
            return copy;
        }
        void OnDestroy(){foreach(var clip in copies.Values)if(clip)Destroy(clip);}
    }
}
