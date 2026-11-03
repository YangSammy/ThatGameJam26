using System;
using System.Linq;
using UnityEngine;
namespace MemoryWhitebox.Art
{
    /// <summary>Presentation sampling only. The existing chapter owns time, input and outcomes.</summary>
    [DisallowMultipleComponent]
    public sealed class AlbumWinterVisual : MonoBehaviour
    {
        [Serializable] public struct State
        {
            public float open,turn,settle,bridge,load,swingRadians,flowerRadians,insert,unlock,door,actorProgress,facing;
            public string actorPose; public bool keyVisible; public Texture pageTexture;
        }
        [SerializeField] string asset;
        [SerializeField] AnimationClip rest;
        [SerializeField] AnimationClip[] clips=new AnimationClip[0];
        [SerializeField] Transform swing,wheel,doorKey,actorFacing;
        [SerializeField] Transform[] carriers=new Transform[0];
        [SerializeField] Renderer[] pages=new Renderer[0],revealSurfaces=new Renderer[0];
        [SerializeField] State state;
        MaterialPropertyBlock block;
        static float Finite(float x)=>float.IsNaN(x)||float.IsInfinity(x)?0:x;
        static float Unit(float x)=>Mathf.Clamp01(Finite(x));
        public void Configure(string id,AnimationClip baseline,AnimationClip[] channels)
        {
            asset=id;rest=baseline;clips=channels;var ts=GetComponentsInChildren<Transform>(true);
            swing=ts.FirstOrDefault(t=>t.name=="SwingPivot");wheel=ts.FirstOrDefault(t=>t.name=="WheelPivot");
            carriers=ts.Where(t=>t.name.StartsWith("Carrier_")&&!t.name.EndsWith("Mesh")).ToArray();
            doorKey=ts.FirstOrDefault(t=>t.name=="KeyVisual");actorFacing=id.StartsWith("E02")?ts.First(t=>t.name=="MotionPivot"):null;
            var rs=GetComponentsInChildren<Renderer>(true);pages=rs.Where(r=>r.sharedMaterials.Any(m=>m.name=="M_E_PageContent")).ToArray();revealSurfaces=rs.Where(r=>r.sharedMaterials.Any(m=>m.name=="M_E_BridgeWood")).ToArray();ResetVisual();
        }
        public State Capture()=>state;
        public void Restore(State v){state=v;state.open=Unit(v.open);state.turn=Unit(v.turn);state.settle=Unit(v.settle);state.bridge=Unit(v.bridge);state.load=Unit(v.load);state.swingRadians=Mathf.Clamp(Finite(v.swingRadians),-65*Mathf.Deg2Rad,65*Mathf.Deg2Rad);state.flowerRadians=Finite(v.flowerRadians);state.insert=Unit(v.insert);state.unlock=Unit(v.unlock);state.door=Unit(v.door);state.actorProgress=Unit(v.actorProgress);state.facing=Finite(v.facing);Apply();}
        public void ResetVisual(){state=new State{open=1,bridge=1,actorPose="Idle"};Apply();}
        public void SetOpenProgress(float u){state.open=Unit(u);Apply();}
        public void SetTurnProgress(float u){state.turn=Unit(u);Apply();}
        public void SetSettleProgress(float u){state.settle=Unit(u);Apply();}
        public void SetPageTexture(Texture texture){state.pageTexture=texture;Apply();}
        public void SetBridgeProgress(float u){state.bridge=Unit(u);Apply();}
        public void SetLoadProgress(float u){state.load=Unit(u);Apply();}
        public void SetSwingAngleRadians(float a){state.swingRadians=Mathf.Clamp(Finite(a),-65*Mathf.Deg2Rad,65*Mathf.Deg2Rad);Apply();}
        public void SetFlowerAngleRadians(float a){state.flowerRadians=Finite(a);Apply();}
        public void SetDoorState(float insertion,float unlocked,float opened,bool showKey){state.insert=Unit(insertion);state.unlock=Unit(unlocked);state.door=Unit(opened);state.keyVisible=showKey;Apply();}
        public void SetActorPose(string pose,float progress,float facingDegrees=0){state.actorPose=pose;state.actorProgress=Unit(progress);state.facing=Finite(facingDegrees);Apply();}
        void Sample(string suffix,float u){var c=clips.FirstOrDefault(x=>x!=null&&x.name.EndsWith("_"+suffix,StringComparison.Ordinal));if(c!=null)c.SamplePropAnimation(gameObject,Unit(u)*c.length);}
        void Apply()
        {
            if(rest!=null)rest.SamplePropAnimation(gameObject,0);
            if(asset=="E01_Album"){Sample("BookClose",1-state.open);Sample("PageTurn",state.turn*state.open);Sample("PageSettle",state.settle*state.open);}
            if(asset=="E02_AlbumActor"){Sample(state.actorPose??"Idle",state.actorProgress);if(actorFacing!=null)actorFacing.localRotation=Quaternion.Euler(0,state.facing,0);}
            if(swing!=null)swing.localRotation=Quaternion.Euler(0,0,state.swingRadians*Mathf.Rad2Deg);
            if(wheel!=null){float a=Mathf.Repeat(state.flowerRadians*Mathf.Rad2Deg,360);wheel.localRotation=Quaternion.Euler(0,0,a);foreach(var c in carriers)c.localRotation=Quaternion.Euler(0,0,-a);}
            if(asset=="E05_CounterweightGate")Sample("Load",state.load);
            if(asset=="E07_PendantDoor"){Sample("KeyInsert",state.insert);Sample("Unlock",state.unlock);Sample("DoorOpen",state.door);if(doorKey!=null)doorKey.gameObject.SetActive(state.keyVisible);}
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var r in pages)for(int i=0;i<r.sharedMaterials.Length;i++)if(r.sharedMaterials[i].name=="M_E_PageContent"){
                r.GetPropertyBlock(block,i);block.SetTexture("_MainTex",state.pageTexture!=null?state.pageTexture:Texture2D.whiteTexture);block.SetFloat("_ContentAmount",state.pageTexture!=null?1:0);
                bool left=r.name.Contains("ContentL")||r.name.Contains("TurnBack"),sheet=asset=="E01_AlbumSheet";block.SetVector("_ContentRect",sheet?new Vector4(1,1,0,0):new Vector4(.5f,1,left?0:.5f,0));r.SetPropertyBlock(block,i);
            }
            foreach(var r in revealSurfaces)for(int i=0;i<r.sharedMaterials.Length;i++)if(r.sharedMaterials[i].name=="M_E_BridgeWood"){
                r.GetPropertyBlock(block,i);block.SetFloat("_Reveal",asset=="E04_Bridge"?state.bridge:state.load);block.SetVector("_RevealRange",asset=="E04_Bridge"?new Vector4(-.158f,.158f,0,0):new Vector4(.2f,.42f,0,0));r.SetPropertyBlock(block,i);
            }
        }
        void OnEnable(){Apply();} // Reapply serialized/per-instance state after loading or pooling.
        // No automatic clock, controller, input, persistence or gameplay events.
    }
}
