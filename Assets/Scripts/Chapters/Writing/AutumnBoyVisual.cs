using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class AutumnBoyVisual : MonoBehaviour
    {
        public AnimationClip idle, walk;
        float clock;
        public void Present(bool moving,float dt)
        {
            clock+=Mathf.Max(0,dt);
            var clip=moving?walk:idle;
            if(clip!=null)clip.SampleAnimation(gameObject,Mathf.Repeat(clock,clip.length));
        }
    }
}
