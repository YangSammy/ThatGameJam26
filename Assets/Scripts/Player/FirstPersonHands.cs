using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox
{
    // Chapter transforms remain invisible animation targets. All player hand meshes live under the camera.
    [DefaultExecutionOrder(1000),RequireComponent(typeof(Camera))]
    public sealed class FirstPersonHands : MonoBehaviour
    {
        public const int HandLayer=27;
        public Transform authoredRoot;
        public Transform[] authoredHands=new Transform[2];
        public Material sleeveMaterial;
        [Header("公园独立控制（书房保持为空）")]
        public ParkPhotoPrototype park;
        [Tooltip("镜头下的静置与举机手位，可直接移动场景定位物体调整。")]
        public Transform[] parkRestTargets=new Transform[2],parkAimTargets=new Transform[2];
        public Transform[] parkArms=new Transform[2];
        sealed class Set
        {
            public WorldChapter3D world;
            public Transform root;
            public Transform[] targets,visuals=new Transform[2],arms=new Transform[2];
        }
        readonly Dictionary<int,Set> sets=new Dictionary<int,Set>();
        readonly List<Renderer> hidden=new List<Renderer>();
        WBMain house;
        Transform rig;
        public int VisibleSet {get;private set;}=-1;
        public Transform RigRoot => rig;
        public void BindHouse(WBMain host){house=host;}
        public void Register(int index,WorldChapter3D world)
        {
            if(sets.ContainsKey(index))return;
            if(rig==null){rig=authoredRoot;if(rig==null){rig=new GameObject("Player first-person hands").transform;rig.SetParent(transform,false);}}
            var set=new Set{world=world,targets=world.PlayerHandTargets};
            bool authored=index==0 && authoredHands.Length==2 && authoredHands[0]!=null && authoredHands[1]!=null;
            set.root=authored?authoredHands[0].parent:new GameObject("Hands - "+HouseLayout.Names[index]).transform;set.root.SetParent(rig,false);
            for(int i=0;i<2 && (authored || i<set.targets.Length);i++)
            {
                var source=i<set.targets.Length?set.targets[i]:null;if(source==null && !authored)continue;
                var visual=authored?authoredHands[i]:Instantiate(source.gameObject,set.root).transform;visual.name=i==0?"Player left hand":"Player right hand";
                if(!authored)visual.localScale=source.lossyScale;
                visual.localPosition=Rest(i);visual.localRotation=Quaternion.identity;visual.gameObject.SetActive(true);
                foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=HandLayer;
                foreach(var col in visual.GetComponentsInChildren<Collider>(true))Destroy(col);
                foreach(var r in visual.GetComponentsInChildren<Renderer>(true)){r.forceRenderingOff=false;r.shadowCastingMode=ShadowCastingMode.Off;}
                // Replace the short detached sleeve with an arm reaching from the player's viewpoint.
                Material sleeve=null;
                if(visual.childCount>1){var r=visual.GetChild(1).GetComponent<Renderer>();if(r!=null)sleeve=r.sharedMaterial;visual.GetChild(1).gameObject.SetActive(false);}
                if(sleeve!=null && sleeveMaterial==null)sleeveMaterial=sleeve;
                if(sleeve==null)sleeve=sleeveMaterial;
                var arm=GameObject.CreatePrimitive(PrimitiveType.Capsule);arm.name=i==0?"Player left forearm":"Player right forearm";
                arm.transform.SetParent(set.root,false);arm.layer=HandLayer;Destroy(arm.GetComponent<Collider>());
                var renderer=arm.GetComponent<Renderer>();renderer.sharedMaterial=sleeve;renderer.shadowCastingMode=ShadowCastingMode.Off;
                set.visuals[i]=visual;set.arms[i]=arm.transform;
                Hide(source);
            }
            foreach(var source in world.PlayerArmTargets)Hide(source);
            sets.Add(index,set);set.root.gameObject.SetActive(false);
            GetComponent<Camera>().cullingMask|=1<<HandLayer;
        }
        void Hide(Transform source)
        {
            if(source==null)return;
            foreach(var r in source.GetComponentsInChildren<Renderer>(true)){r.forceRenderingOff=true;hidden.Add(r);}
        }
        static Vector3 Rest(int side){return new Vector3(side==0?-.23f:.23f,-.27f,.65f);}
        void LateUpdate(){Advance(Time.unscaledDeltaTime);}
        public void Advance(float dt)
        {
            if(park!=null){AdvancePark(dt);return;}
            if(rig==null || house==null)return;
            int index=house!=null && house.HouseInputReady?house.ChapterIndex:0;
            bool operating=house.HouseInputReady || house.DirectRadioReady;
            Set set;if(!sets.TryGetValue(index,out set))return;
            foreach(var pair in sets)pair.Value.root.gameObject.SetActive(pair.Key==index);
            VisibleSet=index;
            if(house.ShowHelp)return;
            bool engaged=operating && set.world!=null && set.world.PlayerHandsEngaged;
            float blend=1-Mathf.Exp(-Mathf.Max(0,dt)*18);
            for(int i=0;i<2;i++)
            {
                var visual=set.visuals[i];if(visual==null)continue;
                var source=i<set.targets.Length?set.targets[i]:null;
                Vector3 target=engaged && source!=null?transform.InverseTransformPoint(source.position):Rest(i);
                // Never place the visible hand behind the near plane.
                target.z=Mathf.Max(.16f,target.z);
                Quaternion rotation=engaged && source!=null?Quaternion.Inverse(transform.rotation)*source.rotation:Quaternion.Euler(15,0,0);
                visual.localPosition=Vector3.Lerp(visual.localPosition,target,blend);
                visual.localRotation=Quaternion.Slerp(visual.localRotation,rotation,blend);
                Vector3 shoulder=new Vector3(i==0?-.27f:.27f,-.48f,-.12f);
                Vector3 wrist=visual.localPosition+visual.localRotation*new Vector3(0,0,-.07f);
                var arm=set.arms[i];arm.localPosition=(shoulder+wrist)*.5f;
                arm.localRotation=Quaternion.FromToRotation(Vector3.up,wrist-shoulder);
                arm.localScale=new Vector3(.085f,Vector3.Distance(shoulder,wrist)*.5f,.085f);
            }
        }
        void AdvancePark(float dt)
        {
            if(authoredRoot==null || authoredHands==null || authoredHands.Length<2)return;
            rig=authoredRoot;VisibleSet=0;
            authoredRoot.gameObject.SetActive(true);
            if(park.Paused)return;
            float blend=1-Mathf.Exp(-Mathf.Max(0,dt)*14);
            float step=Time.time*7;
            float sway=Mathf.Clamp01(park.MovementSpeed/2)*.008f*(park.Aiming ? .25f : 1);
            for(int i=0;i<2;i++)
            {
                var visual=authoredHands[i];if(visual==null)continue;
                var targets=park.Aiming?parkAimTargets:parkRestTargets;
                var target=targets!=null && targets.Length>i?targets[i]:null;
                Vector3 position=target!=null?transform.InverseTransformPoint(target.position):Rest(i);
                var rotation=target!=null?Quaternion.Inverse(transform.rotation)*target.rotation:Quaternion.Euler(15,0,0);
                if(i==0 && park.HoldingPhotoHand)
                {
                    position=transform.InverseTransformPoint(park.PhotoHandPosition);
                    rotation=Quaternion.Inverse(transform.rotation)*Quaternion.LookRotation(park.companion.transform.forward,Vector3.up);
                }
                position+=new Vector3(Mathf.Sin(step*.5f)*sway,Mathf.Sin(step)*sway,park.ShutterPulse*.008f);
                position.z=Mathf.Max(.16f,position.z);
                // Poses are camera-local even when the hand set has an authored parent transform.
                visual.position=Vector3.Lerp(visual.position,transform.TransformPoint(position),blend);
                visual.rotation=Quaternion.Slerp(visual.rotation,transform.rotation*rotation,blend);
                var arm=parkArms!=null && parkArms.Length>i?parkArms[i]:null;if(arm==null)continue;
                Vector3 shoulder=transform.TransformPoint(new Vector3(i==0?-.27f:.27f,-.48f,-.12f));
                Vector3 wrist=visual.position+visual.rotation*new Vector3(0,0,-.07f);
                arm.position=(shoulder+wrist)*.5f;arm.rotation=Quaternion.FromToRotation(Vector3.up,wrist-shoulder);
                arm.localScale=new Vector3(.085f,Vector3.Distance(shoulder,wrist)*.5f,.085f);
            }
        }
        void OnDestroy(){foreach(var r in hidden)if(r!=null)r.forceRenderingOff=false;}
        public void PosePhotoHandForShutter(){if(park!=null && park.HoldingPhotoHand)AdvancePark(1);}
    }
}
