using UnityEngine;

namespace MemoryWhitebox
{
    // A presentation joint. It has no reference to a gameplay model or save store.
    public sealed class SafeInspectionPart : MonoBehaviour
    {
        public enum Motion { Hinge, Dial, Button }
        public string label="部件";
        public Motion motion;
        public Vector3 axis=Vector3.up, pivotOffset;
        public float range=100, travel=.012f;
        public AudioClip contact;
        public float Value {get;private set;}
        public bool Held {get;private set;}
        Vector3 origin;
        Quaternion rotation;
        float target;
        Renderer[] highlightRenderers;
        MaterialPropertyBlock[] originalBlocks;
        bool highlighted;
        static readonly int ColorId=Shader.PropertyToID("_Color");
        public bool Highlighted => highlighted;
        public void SetHighlighted(bool value)
        {
            if(highlighted==value)return;
            highlighted=value;
            if(highlightRenderers==null){highlightRenderers=GetComponentsInChildren<Renderer>(true);originalBlocks=new MaterialPropertyBlock[highlightRenderers.Length];}
            for(int i=0;i<highlightRenderers.Length;i++)
            {
                var r=highlightRenderers[i];if(r==null)continue;
                if(!value){r.SetPropertyBlock(originalBlocks[i]);continue;}
                if(originalBlocks[i]==null)originalBlocks[i]=new MaterialPropertyBlock();
                r.GetPropertyBlock(originalBlocks[i]);
                var tint=new MaterialPropertyBlock();r.GetPropertyBlock(tint);
                if(r.sharedMaterial==null || !r.sharedMaterial.HasProperty(ColorId))continue;
                Color color=tint.HasColor(ColorId)?tint.GetColor(ColorId):r.sharedMaterial.GetColor(ColorId);
                var warm=Color.Lerp(color,new Color(1,.91f,.71f,color.a),.16f);
                tint.SetColor(ColorId,warm);r.SetPropertyBlock(tint);
            }
        }
        public void ResetToBaseline(){SetHighlighted(false);Held=false;target=0;}

        public void Capture()
        {
            SetHighlighted(false);origin=transform.localPosition;rotation=transform.localRotation;
            Value=target=0;Held=false;
        }
        public void Press(){Held=true;if(motion==Motion.Button)target=1;}
        public bool Drag(float pixels)
        {
            if(!Held || motion==Motion.Button)return false;
            float old=target;target=Mathf.Clamp01(target+pixels/160f);
            return (target==0 || target==1) && old!=target;
        }
        public void Release(){Held=false;if(motion==Motion.Button)target=0;}
        public void Advance(float dt)
        {
            Value=Mathf.MoveTowards(Value,target,Mathf.Max(0,dt)*5);
            if(motion==Motion.Button){transform.localPosition=origin+axis.normalized*(travel*Value);return;}
            Quaternion delta=Quaternion.AngleAxis(range*Value,axis.normalized);
            transform.localRotation=delta*rotation;
            transform.localPosition=origin+pivotOffset-delta*pivotOffset;
        }
        public void Restore(){SetHighlighted(false);Held=false;Value=target=0;transform.localPosition=origin;transform.localRotation=rotation;}
    }
}
