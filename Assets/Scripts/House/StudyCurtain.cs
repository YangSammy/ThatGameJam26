using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class StudyCurtain : MonoBehaviour
    {
        public Transform front,back;
        public bool IsOpen {get;private set;}
        float blend;
        public void Toggle(){IsOpen=!IsOpen;}
        public void Advance(float dt)
        {
            blend=Mathf.MoveTowards(blend,IsOpen?1:0,dt*1.6f);
            front.localPosition=new Vector3(0,1.35f,Mathf.Lerp(1.5f,.82f,blend));back.localPosition=new Vector3(0,1.35f,Mathf.Lerp(3.4f,4.08f,blend));
            front.localScale=back.localScale=new Vector3(.06f,2.65f,Mathf.Lerp(1.88f,.5f,blend));
        }
        void Update(){Advance(Time.deltaTime);}
        public bool Blocks(Vector3 p)=>blend<.9f && Mathf.Abs(p.x-transform.position.x)<.22f && p.z>.5f && p.z<4.4f;
    }
}
