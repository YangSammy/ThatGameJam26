using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class FirstPersonHands
    {
        [Header("Shared skeletal hands")]
        public FirstPersonArmRig skeletalRig;
        bool skeletalPrepared,handFocused=true,handPaused;
        Vector3 photoPalmLocal;
        public Vector3 PhotoPalmPosition=>skeletalRig!=null&&skeletalRig.Valid?skeletalRig.wrists[0].TransformPoint(photoPalmLocal):transform.position;
        void Awake(){if(skeletalRig!=null)PrepareSkeletalRig();HideParkHandMeshes();}
        void PrepareSkeletalRig()
        {
            if(skeletalPrepared)return;
            skeletalPrepared=true;rig=skeletalRig.transform;
            // Keep targets alive. Disable only the obsolete camera display meshes.
            if(authoredRoot!=null && !skeletalRig.transform.IsChildOf(authoredRoot))authoredRoot.gameObject.SetActive(false);
            foreach(var hand in authoredHands)if(hand!=null && !hand.IsChildOf(skeletalRig.transform))Hide(hand);
            foreach(var arm in parkArms)if(arm!=null)Hide(arm);
            if(!skeletalRig.Valid)Debug.LogError("First-person skeletal rig has missing references.",this);
            else
            {
                int count=0;
                foreach(var bone in skeletalRig.wrists[0].GetComponentsInChildren<Transform>())
                    if(bone.name=="f_index.01.L"||bone.name=="f_middle.01.L"||bone.name=="f_ring.01.L"||bone.name=="f_pinky.01.L")
                    {photoPalmLocal+=skeletalRig.wrists[0].InverseTransformPoint(bone.position);count++;}
                if(count>0)photoPalmLocal*=.65f/count;
            }
        }
        void AdvanceSkeletal(float dt,bool immediate=false)
        {
            PrepareSkeletalRig();
            bool blocked=!handFocused || handPaused || Time.timeScale<=0;
            if(park!=null)blocked|=!park.isActiveAndEnabled || park.HandPresentationBlocked;
            else if(house!=null)blocked|=house.ControlState==PlayerControlState.Paused || house.ControlState==PlayerControlState.Unfocused ||
                house.ControlState==PlayerControlState.Transition || house.ControlState==PlayerControlState.Gallery || house.AwaitingHouseRelease ||
                (house.safeInspection!=null && house.safeInspection.BlocksInput);
            if(blocked && park==null){skeletalRig.ResetPose();return;}
            // Park input gates suspend gestures, not the player's body. Keep the same
            // authored rest/holding targets visible, including the first entry frame.
            if(blocked){dt=0;immediate=true;}
            var left=default(FirstPersonArmRig.Pose);var right=left;
            if(park!=null)
            {
                VisibleSet=0;
                bool holding=park.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Exploration;
                var targets=holding && park.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Meter?parkAimTargets:parkRestTargets;
                left=TargetPose(targets,0,holding?1:0);right=TargetPose(targets,1,holding?1:0);
                var heldLeft=park.InstrumentLeftHandTarget;var heldRight=park.InstrumentRightHandTarget;
                if(heldLeft!=null)left=FirstPersonArmRig.Pose.At(heldLeft.position,heldLeft.rotation,1);
                if(heldRight!=null)right=FirstPersonArmRig.Pose.At(heldRight.position,heldRight.rotation,1);
                var instrument=park.HandContactTarget;
                if(instrument!=null)right=FirstPersonArmRig.Pose.At(instrument.position,instrument.rotation,1);
                if(park.CameraRaiseGrip!=null)right=FirstPersonArmRig.Pose.At(park.CameraRaiseGrip.position,park.CameraRaiseGrip.rotation,1);
                if(park.CameraRaiseSupport!=null)left=FirstPersonArmRig.Pose.At(park.CameraRaiseSupport.position,park.CameraRaiseSupport.rotation,1);
                right.push=park.ShutterPulse;right.grip*=1-right.push;
                right.position+=transform.up*(-park.ShutterPulse*.008f);
                if(park.HoldingPhotoHand)
                {
                    var girl=park.companion.visualPresentation;
                    if(girl!=null && girl.palmContact!=null)
                    {
                        var normal=girl.PalmNormal;
                        var fingers=Vector3.ProjectOnPlane(-park.companion.transform.forward,normal).normalized;
                        if(fingers.sqrMagnitude<.01f)fingers=Vector3.ProjectOnPlane(park.companion.transform.right,normal).normalized;
                        var rotation=Quaternion.LookRotation(Vector3.Cross(normal,fingers),fingers);
                        var palm=girl.PalmPosition+normal*.024f;
                        var wrist=palm-rotation*Vector3.Scale(photoPalmLocal,skeletalRig.wrists[0].lossyScale);
                        left=FirstPersonArmRig.Pose.AtWrist(wrist,rotation,.85f);
                        left.bodyReachLimit=.6f;
                    }
                    else left=FirstPersonArmRig.Pose.At(park.PhotoHandPosition,Quaternion.LookRotation(park.companion.transform.forward,Vector3.up),1);
                }
            }
            else if(house!=null)
            {
                int index=house.HouseInputReady?house.ChapterIndex:0;VisibleSet=index;
                Set set;
                if(sets.TryGetValue(index,out set) && house.HouseInputReady && set.world!=null && set.world.PlayerHandsEngaged)
                {left=TargetPose(set.targets,0,1);right=TargetPose(set.targets,1,1);}
                if(index==0 && house.DirectRadioReady && house.radioDeck!=null)
                {
                    var deck=house.radioDeck;var target=deck.HandContactTarget;
                    if(target!=null)right=FirstPersonArmRig.Pose.At(target.position,target.rotation,deck.HandPressing?0:1,deck.HandPressing?1:0);
                }
            }
            skeletalRig.Evaluate(left,right,dt,immediate);
        }
        static FirstPersonArmRig.Pose TargetPose(Transform[] targets,int side,float grip)
        {
            var target=targets!=null && side<targets.Length?targets[side]:null;
            return target!=null?FirstPersonArmRig.Pose.At(target.position,target.rotation,grip):default(FirstPersonArmRig.Pose);
        }
        void OnApplicationFocus(bool focused){handFocused=focused;if(!focused && skeletalRig!=null)skeletalRig.ResetPose();}
        void OnApplicationPause(bool paused){handPaused=paused;if(paused && skeletalRig!=null)skeletalRig.ResetPose();}
        void OnDisable(){if(skeletalRig!=null)skeletalRig.ResetPose();}
    }
}
