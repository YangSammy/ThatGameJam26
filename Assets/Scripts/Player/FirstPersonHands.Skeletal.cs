using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class FirstPersonHands
    {
        [Header("Shared skeletal hands")]
        public FirstPersonArmRig skeletalRig;
        bool skeletalPrepared,handFocused=true,handPaused;
        void Awake(){if(skeletalRig!=null)PrepareSkeletalRig();}
        void PrepareSkeletalRig()
        {
            if(skeletalPrepared)return;
            skeletalPrepared=true;rig=skeletalRig.transform;
            // Keep targets alive. Disable only the obsolete camera display meshes.
            if(authoredRoot!=null && !skeletalRig.transform.IsChildOf(authoredRoot))authoredRoot.gameObject.SetActive(false);
            foreach(var hand in authoredHands)if(hand!=null && !hand.IsChildOf(skeletalRig.transform))Hide(hand);
            foreach(var arm in parkArms)if(arm!=null)Hide(arm);
            if(!skeletalRig.Valid)Debug.LogError("First-person skeletal rig has missing references.",this);
        }
        void AdvanceSkeletal(float dt,bool immediate=false)
        {
            PrepareSkeletalRig();
            bool blocked=!handFocused || handPaused || Time.timeScale<=0;
            if(park!=null)blocked|=!park.isActiveAndEnabled || park.HandPresentationBlocked;
            else if(house!=null)blocked|=house.ControlState==PlayerControlState.Paused || house.ControlState==PlayerControlState.Unfocused ||
                house.ControlState==PlayerControlState.Transition || house.ControlState==PlayerControlState.Gallery || house.AwaitingHouseRelease ||
                (house.safeInspection!=null && house.safeInspection.BlocksInput);
            if(blocked){skeletalRig.ResetPose();return;}
            var left=default(FirstPersonArmRig.Pose);var right=left;
            if(park!=null)
            {
                VisibleSet=0;
                bool holding=park.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Exploration;
                var targets=holding?parkAimTargets:parkRestTargets;
                left=TargetPose(targets,0,holding?1:0);right=TargetPose(targets,1,holding?1:0);
                var instrument=park.HandContactTarget;
                if(instrument!=null)right=FirstPersonArmRig.Pose.At(instrument.position,instrument.rotation,1);
                right.push=park.ShutterPulse;right.grip*=1-right.push;
                right.position+=transform.up*(-park.ShutterPulse*.008f);
                if(park.HoldingPhotoHand)left=FirstPersonArmRig.Pose.At(park.PhotoHandPosition,Quaternion.LookRotation(park.companion.transform.forward,Vector3.up),1);
            }
            else if(house!=null)
            {
                int index=house.HouseInputReady?house.ChapterIndex:0;VisibleSet=index;
                Set set;
                if(sets.TryGetValue(index,out set) && house.HouseInputReady && set.world.PlayerHandsEngaged)
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
