using UnityEngine;
namespace MemoryWhitebox
{
    // Explicit prop poses only. No controller, playback clock, root motion or save state.
    public static class PropAnimationSampling
    {
        public static void SamplePropAnimation(this AnimationClip clip, GameObject root, float time)
        {
            // Editor sampling accepts these clips without an Animator; players do not.
            // Add only to runtime instances, so authoring/previews never alter prefab assets.
            if (Application.isPlaying && !clip.legacy && root.GetComponent<Animator>() == null)
            {
                var animator = root.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.enabled = false;
            }
            clip.SampleAnimation(root, time);
        }
    }
}
