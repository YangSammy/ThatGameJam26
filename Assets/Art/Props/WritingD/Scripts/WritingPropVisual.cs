using System;
using UnityEngine;

namespace MemoryWhitebox.Art
{
    /// <summary>Deterministic prop presentation. Caller owns input, time, placement and persistence.</summary>
    [DisallowMultipleComponent]
    public sealed class WritingPropVisual : MonoBehaviour
    {
        public enum Channel { Crumple, ChainPose, Sway, PickupSettle, Handle, Wheel, Flap, Timer, Dial }
        [Serializable] public struct Binding { public Channel channel; public AnimationClip clip; }
        [Serializable] public struct State
        {
            public float crumple, chainPose, sway, pickupSettle, handle, wheel, flap, timer, dial;
            public bool usePickupSettle;
            public Texture inkTexture;
            public float inkAmount;
        }
        [SerializeField] AnimationClip rest;
        [SerializeField] Binding[] bindings = new Binding[0];
        [SerializeField] Renderer inkSurface;
        MaterialPropertyBlock inkBlock;
        State state;

        // Configuration happens once at authoring time; references are serialized in each prefab.
        public void Configure(AnimationClip restClip, Binding[] channels, Renderer contentSurface=null)
        { rest=restClip; bindings=channels ?? new Binding[0]; inkSurface=contentSurface; ResetVisual(); }

        static float Unit(float x) => float.IsNaN(x)||float.IsInfinity(x) ? 0 : Mathf.Clamp01(x);
        public State Capture() => state;
        public void Restore(State value)
        {
            state=new State { crumple=Unit(value.crumple),chainPose=Unit(value.chainPose),sway=Unit(value.sway),
                pickupSettle=Unit(value.pickupSettle),handle=Unit(value.handle),wheel=Unit(value.wheel),
                flap=Unit(value.flap),timer=Unit(value.timer),dial=Unit(value.dial),usePickupSettle=value.usePickupSettle,
                inkTexture=value.inkTexture,inkAmount=Unit(value.inkAmount) };
            Apply();
        }
        public void ResetVisual() { state=default(State); Apply(); }
        public void SetCrumpleProgress(float value) { state.crumple=Unit(value); Apply(); }
        public void SetChainPoseProgress(float value) { state.chainPose=Unit(value); Apply(); }
        public void SetSwayProgress(float value) { state.sway=Unit(value);state.usePickupSettle=false; Apply(); }
        public void SetPickupSettleProgress(float value) { state.pickupSettle=Unit(value);state.usePickupSettle=true; Apply(); }
        public void SetHandleProgress(float value) { state.handle=Unit(value); Apply(); }
        public void SetWheelProgress(float value) { state.wheel=Unit(value); Apply(); }
        public void SetFlapProgress(float value) { state.flap=Unit(value); Apply(); }
        public void SetTimerProgress(float value) { state.timer=Unit(value); Apply(); }
        public void SetDialProgress(float value) { state.dial=Unit(value); Apply(); }
        public void SetInkTexture(Texture texture,float visibility=1) {state.inkTexture=texture;state.inkAmount=Unit(visibility);Apply();}

        void Apply()
        {
            if(rest!=null)rest.SamplePropAnimation(gameObject,0);
            foreach(var binding in bindings)
            {
                if(binding.clip==null)continue;
                float u;
                switch(binding.channel)
                {
                    case Channel.Crumple:u=state.crumple;break;
                    case Channel.ChainPose:u=state.chainPose;break;
                    case Channel.Sway:if(state.usePickupSettle)continue;u=state.sway;break;
                    case Channel.PickupSettle:if(!state.usePickupSettle)continue;u=state.pickupSettle;break;
                    case Channel.Handle:u=state.handle;break;
                    case Channel.Wheel:u=state.wheel;break;
                    case Channel.Flap:u=state.flap;break;
                    case Channel.Timer:u=state.timer;break;
                    case Channel.Dial:u=state.dial;break;
                    default:continue;
                }
                binding.clip.SamplePropAnimation(gameObject,u*binding.clip.length);
            }
            if(inkSurface!=null)
            {
                if(inkBlock==null)inkBlock=new MaterialPropertyBlock();
                inkSurface.GetPropertyBlock(inkBlock,0);
                inkBlock.SetTexture("_MainTex",state.inkTexture!=null?state.inkTexture:Texture2D.whiteTexture);
                inkBlock.SetFloat("_InkAmount",state.inkTexture!=null?state.inkAmount:0);
                inkSurface.SetPropertyBlock(inkBlock,0);
            }
        }
        // Intentionally no Awake/OnEnable reset: pooled or temporarily hidden props keep their sampled pose.
        // Intentionally no Update, input, coroutines, controller, gameplay callbacks or file writes.
    }
}
