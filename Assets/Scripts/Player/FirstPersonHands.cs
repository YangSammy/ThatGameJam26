using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox
{
    // Chapter transforms remain invisible animation targets. All player hand meshes live under the camera.
    [DefaultExecutionOrder(1000),RequireComponent(typeof(Camera))]
    public sealed partial class FirstPersonHands : MonoBehaviour
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
        public void BindHouse(WBMain host){house=host;ApplyDeskHandVisibility();}
        public void Register(int index,WorldChapter3D world)
        {
            if(sets.TryGetValue(index,out var existing))
            {
                existing.world=world;
                existing.targets=world?.PlayerHandTargets??System.Array.Empty<Transform>();
                return;
            }
            if(skeletalRig!=null)
            {
                PrepareSkeletalRig();
                var skeletalSet=new Set{world=world,targets=world?.PlayerHandTargets??System.Array.Empty<Transform>()};
                for(int side=0;side<skeletalSet.targets.Length;side++)
                {
                    var target=skeletalSet.targets[side];
                    // These authored groups also own the real pen/brush. Hide only hand and sleeve.
                    if(target!=null && side==1 && (world is WritingWorld3D || world is PaintingWorld3D))
                    {for(int child=0;child<Mathf.Min(2,target.childCount);child++)Hide(target.GetChild(child));}
                    else Hide(target);
                }
                foreach(var target in world?.PlayerArmTargets??System.Array.Empty<Transform>())Hide(target);
                sets.Add(index,skeletalSet);return;
            }
            if(rig==null){rig=authoredRoot;if(rig==null){rig=new GameObject("Player first-person hands").transform;rig.SetParent(transform,false);}}
            var set=new Set{world=world,targets=world?.PlayerHandTargets??System.Array.Empty<Transform>()};
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
            foreach(var source in world?.PlayerArmTargets??System.Array.Empty<Transform>())Hide(source);
            sets.Add(index,set);set.root.gameObject.SetActive(false);
            GetComponent<Camera>().cullingMask|=1<<HandLayer;
        }
        void Hide(Transform source)
        {
            if(source==null)return;
            foreach(var r in source.GetComponentsInChildren<Renderer>(true)){r.forceRenderingOff=true;if(!hidden.Contains(r))hidden.Add(r);}
        }
        void HideParkHandMeshes()
        {
            if(park==null)return;
            // Keep poses and contact targets alive; instruments share HandLayer and must remain visible.
            if(skeletalRig!=null)Hide(skeletalRig.transform);
            foreach(var hand in authoredHands)Hide(hand);
            foreach(var arm in parkArms)Hide(arm);
        }
        static Vector3 Rest(int side){return new Vector3(side==0?-.23f:.23f,-.27f,.65f);}
        object clothingContext;
        int clothingPose;

        void LateUpdate()
        {
            ApplyDeskHandVisibility();
            if(GamePauseMenu.BlocksGameplay)return;
            Advance(Time.deltaTime);
            ApplyDeskHandVisibility();
            UpdateClothingSound();
        }

        [SerializeField,HideInInspector] List<Renderer> handVisibilityRenderers=new List<Renderer>();
        [SerializeField,HideInInspector] List<bool> handVisibilityEnabled=new List<bool>();
        void SetHandMeshVisibility(Transform target,bool visible)
        {
            if(!target)return;
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))
            {
                int index=handVisibilityRenderers.IndexOf(r);
                if(index<0){index=handVisibilityRenderers.Count;handVisibilityRenderers.Add(r);handVisibilityEnabled.Add(r.enabled);}
                r.forceRenderingOff=!visible;
                r.enabled=visible && handVisibilityEnabled[index];
            }
        }
        void OnPreCull(){ApplyDeskHandVisibility();}

        internal static bool DeskHandsAllowed(InputContextId context,bool parkScene,bool active,bool blocked)
        {
            return !parkScene && active && !blocked && (context==InputContextId.TapeWorkbench || context==InputContextId.TapeTransport || context==InputContextId.AutumnWriting || context==InputContextId.WinterBook);
        }
        void ApplyDeskHandVisibility()
        {
            bool visible=false;
            if(house!=null)
                visible=DeskHandsAllowed(house.InputContext.Current,park!=null,isActiveAndEnabled && house.isActiveAndEnabled,
                    !handFocused || handPaused || GamePauseMenu.BlocksGameplay || house.AwaitingHouseRelease ||
                    house.ControlState==PlayerControlState.Transition || house.ControlState==PlayerControlState.Paused ||
                    (house.safeInspection!=null && house.safeInspection.BlocksInput));
            // Keep animation targets and the camera's instruments alive; only hide hand meshes.
            bool joinedLeft=park!=null && isActiveAndEnabled && park.isActiveAndEnabled && handFocused && !handPaused &&
                (park.KeepJoinedHandForTransition || (!GamePauseMenu.BlocksGameplay && (!park.HandPresentationBlocked || HandholdAutomaticRelease))) && (skeletalRig!=null?HandholdVisible:park.HoldingPhotoHand) &&
                HandholdViewAllowed;
            if(skeletalRig!=null)
            {
                bool ready=skeletalRig.SetLeftArmOnly(joinedLeft);
                SetHandMeshVisibility(skeletalRig.transform,visible || (joinedLeft && ready));
            }
            // Legacy hands can coexist with the skeletal rig; never leave them outside the policy.
            for(int i=0;i<authoredHands.Length;i++)SetHandMeshVisibility(authoredHands[i],skeletalRig==null && ((visible && park==null) || (joinedLeft && i==0)));
            for(int i=0;i<parkArms.Length;i++)SetHandMeshVisibility(parkArms[i],skeletalRig==null && joinedLeft && i==0);
            foreach(var pair in sets)
            {
                foreach(var t in pair.Value.visuals)SetHandMeshVisibility(t,visible && skeletalRig==null);
                foreach(var t in pair.Value.arms)SetHandMeshVisibility(t,visible && skeletalRig==null);
            }
            if(!visible)VisibleSet=-1;
        }

        void UpdateClothingSound()
        {
            if (!handFocused || handPaused || Time.timeScale <= 0 || (park == null && house == null))
            {
                clothingContext = null;
                return;
            }

            var control = park != null ? park.ControlState : house.ControlState;
            if ((control != PlayerControlState.Exploration && control != PlayerControlState.Operation) ||
                (park != null && !park.isActiveAndEnabled) ||
                (house != null && (house.AwaitingHouseRelease ||
                    (house.DirectDarkroomActive && house.DarkroomWorld.ParkGalleryOpen))))
            {
                clothingContext = null;
                return;
            }

            object context;
            int pose = 0;
            if (park != null)
            {
                context = park;
                if (park.PhotoMode != ParkPhotoPrototype.PhotographyMode.Exploration) pose |= 1;
                if (park.HoldingPhotoHand) pose |= 2;
                if (park.HandContactTarget != null) pose |= 4;
            }
            else if (house.DirectDarkroomActive)
            {
                context = house.DarkroomWorld;
                if (house.DarkroomWorld.PlayerHandsEngaged) pose = 1;
            }
            else if (house.HouseInputReady)
            {
                context = house.CurrentWorld;
                if (house.CurrentWorld != null && house.CurrentWorld.PlayerHandsEngaged) pose = 1;
            }
            else if (house.DirectRadioReady && house.radioDeck != null)
            {
                context = house.radioDeck;
                if (house.radioDeck.HandContactTarget != null) pose = 1;
            }
            else
                context = house;

            if (context == clothingContext && pose != clothingPose)
                StorySfx.Shot(103, transform.position + transform.forward * .35f);
            clothingContext = context;
            clothingPose = pose;
        }
        public void Advance(float dt)
        {
            HideParkHandMeshes();
            if(skeletalRig!=null){AdvanceSkeletal(dt);return;}
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
                var held=i==0?park.InstrumentLeftHandTarget:park.InstrumentRightHandTarget;
                if(held!=null)target=held;
                var raising=i==0?park.CameraRaiseSupport:park.CameraRaiseGrip;
                if(raising!=null)target=raising;
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
        void OnDestroy(){foreach(var r in hidden)if(r!=null)r.forceRenderingOff=true;}
        public void PosePhotoHandForShutter(){HideParkHandMeshes();if(park!=null && (park.HoldingPhotoHand || HandholdVisible)){if(skeletalRig!=null)AdvanceSkeletal(0);else AdvancePark(1);}ApplyDeskHandVisibility();}
    }
}
