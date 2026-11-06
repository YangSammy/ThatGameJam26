using UnityEngine;

namespace MemoryWhitebox
{
    // Presentation only. Route motion, photography anchors and acceptance remain on ParkCompanion.
    public sealed class ParkGirlPresentation : MonoBehaviour
    {
        public ParkCompanion companion;
        public Transform headBone, rightUpperArm, rightLowerArm, rightHand;
        public Transform upperChest,visualRoot,leftFoot,rightFoot;
        public Transform[] hairBones=new Transform[0];
        [Header("Natural face-obscuring pose study (no material override)")]
        public bool useNaturalPrivacyPose;
        public Transform neckBone;
        // Retained for serialized prefab compatibility; no longer drives head pitch.
        [HideInInspector] public float privacyLookDown;
        [Range(-55,55)] public float privacyLookAway=-42;
        [Min(.1f)] public float privacyPoseSeconds=.6f;
        public float PrivacyPitch {get;private set;}
        public float PrivacyYaw {get;private set;}
        float privacyPitchVelocity,privacyYawVelocity;
        Quaternion neckBase,neckApplied;
        [Header("Meadow performance")]
        public Transform palmContact;
        [Min(.1f)] public float offerContactSeconds=2.1f;
        public float OfferWeight {get;private set;}
        public float OfferElapsed {get;private set;}
        public Vector3 PalmPosition=>palmContact!=null?palmContact.position:rightHand.position;
        // This rig's palm faces local -X. Do not flip the reported normal based on
        // world height: that would mistake a palm-down hand for a palm-up one.
        public Vector3 PalmNormal=>-rightHand.right;
        Animator performanceAnimator;
        bool hasOfferParameter,wasOffering;
        static readonly int offerParameter=Animator.StringToHash("OfferHand");
        Transform[] gripBones=new Transform[0];
        Quaternion[] gripBase,gripApplied;
        [Range(0,1)] public float shoulderFollow=.24f;
        [Min(.01f)] public float shoulderDelay=.26f;
        [Min(.01f)] public float hairDelay=.19f;
        public float ShoulderYaw {get;private set;}
        public float HairLag {get;private set;}
        Quaternion proxyHome, headBase, upperBase, lowerBase;
        Quaternion headApplied, upperApplied, lowerApplied;
        Quaternion chestBase,chestApplied;
        Quaternion handBase,handApplied;
        Quaternion[] hairHomes;
        float shoulderVelocity,hairVelocity,lastHeadYaw;
        int dynamicsFrame=-1;
        bool applied;
        void Awake()
        {
            if(companion!=null && companion.photoHeadPivot!=null)
                proxyHome=companion.photoHeadPivot.localRotation;
            hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;
            performanceAnimator=GetComponentInChildren<Animator>();
            if(neckBone==null && headBone!=null)neckBone=headBone.parent;
            if(performanceAnimator!=null)foreach(var p in performanceAnimator.parameters)if(p.nameHash==offerParameter && p.type==AnimatorControllerParameterType.Bool)hasOfferParameter=true;
            var fingers=new System.Collections.Generic.List<Transform>();
            if(rightHand!=null)foreach(var bone in rightHand.GetComponentsInChildren<Transform>())
                if(!bone.name.Contains("Thumb") && (bone.name.EndsWith("Proximal")||bone.name.EndsWith("Intermediate")||bone.name.EndsWith("Distal")))fingers.Add(bone);
            gripBones=fingers.ToArray();gripBase=new Quaternion[gripBones.Length];gripApplied=new Quaternion[gripBones.Length];
        }
        void Update()
        {
            if(companion==null)return;
            bool offering=companion.photoOfferHand;
            if(hasOfferParameter && offering!=wasOffering)performanceAnimator.SetBool(offerParameter,offering);
            if(performanceAnimator!=null)performanceAnimator.speed=companion.AnimationPaused?0:1;
            if(companion.AnimationPaused)return;
            float dt=Time.deltaTime;
            if(offering){if(!wasOffering)OfferElapsed=0;OfferElapsed+=dt;}
            else OfferElapsed=0;
            float target=offering?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,offerContactSeconds,OfferElapsed)):0;
            OfferWeight=offering?target:Mathf.MoveTowards(OfferWeight,0,dt/.3f);
            float pitch=0,yaw=0;
            if(useNaturalPrivacyPose)
            {
                // Keep the authored side/back reference pose neutral. Applying the
                // response's side turn here exposed the face at the meadow anchor.
                // Preserve the authored animation's head height. Do not add a bow
                // when turning, offering the hand or completing the grip.
                yaw=companion.photoLookBack?privacyLookAway+(offering?6:0):0;
                if(companion.CurrentMoment==ParkCompanion.Moment.Walking){pitch*=.7f;yaw*=.5f;}
            }
            if(dt>0)
            {
                PrivacyPitch=Mathf.SmoothDampAngle(PrivacyPitch,pitch,ref privacyPitchVelocity,privacyPoseSeconds,55,dt);
                PrivacyYaw=Mathf.SmoothDampAngle(PrivacyYaw,yaw,ref privacyYawVelocity,privacyPoseSeconds,65,dt);
            }
            wasOffering=offering;
        }
        void LateUpdate(){ApplyPhotoPose();}
        public void ApplyPhotoPose()
        {
            if(headBone==null)return;
            if(hairHomes==null){hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;}
            if(companion==null)return;
            if(companion.AnimationPaused)return;
            // Shutter capture can request this a second time before LateUpdate. Don't compound offsets.
            if(applied && Quaternion.Angle(headBone.localRotation,headApplied)<.001f)headBone.localRotation=headBase;
            if(applied && Quaternion.Angle(rightUpperArm.localRotation,upperApplied)<.001f)rightUpperArm.localRotation=upperBase;
            if(applied && Quaternion.Angle(rightLowerArm.localRotation,lowerApplied)<.001f)rightLowerArm.localRotation=lowerBase;
            if(applied && Quaternion.Angle(rightHand.localRotation,handApplied)<.001f)rightHand.localRotation=handBase;
            for(int i=0;i<gripBones.Length;i++)if(applied && Quaternion.Angle(gripBones[i].localRotation,gripApplied[i])<.001f)gripBones[i].localRotation=gripBase[i];
            if(upperChest!=null && applied && Quaternion.Angle(upperChest.localRotation,chestApplied)<.001f)upperChest.localRotation=chestBase;
            if(neckBone!=null && applied && Quaternion.Angle(neckBone.localRotation,neckApplied)<.001f)neckBone.localRotation=neckBase;
            headBase=headBone.localRotation;upperBase=rightUpperArm.localRotation;lowerBase=rightLowerArm.localRotation;handBase=rightHand.localRotation;
            if(upperChest!=null)chestBase=upperChest.localRotation;
            if(neckBone!=null)neckBase=neckBone.localRotation;
            if(companion.photoHeadPivot!=null)
            {
                var delta=companion.photoHeadPivot.localRotation*Quaternion.Inverse(proxyHome);
                var basis=companion.transform.rotation;
                var targetHead=basis*delta*Quaternion.Inverse(basis)*headBone.rotation;
                float yaw=Mathf.DeltaAngle(0,delta.eulerAngles.y);
                if(dynamicsFrame!=Time.frameCount)
                {
                    float dt=Mathf.Min(Time.deltaTime,.1f);
                    ShoulderYaw=Mathf.SmoothDampAngle(ShoulderYaw,yaw*shoulderFollow,ref shoulderVelocity,shoulderDelay,90,dt);
                    float movement=Mathf.Clamp(Mathf.DeltaAngle(lastHeadYaw,yaw)/Mathf.Max(.001f,dt)*-.035f,-3.5f,3.5f);
                    HairLag=Mathf.SmoothDamp(HairLag,movement,ref hairVelocity,hairDelay,35,dt);
                    lastHeadYaw=yaw;dynamicsFrame=Time.frameCount;
                }
                if(upperChest!=null)upperChest.rotation=Quaternion.AngleAxis(ShoulderYaw,companion.transform.up)*upperChest.rotation;
                headBone.rotation=targetHead;
            }
            if(Mathf.Abs(PrivacyPitch)+Mathf.Abs(PrivacyYaw)>.001f)
            {
                // An authored side, not camera tracking. Distribute the turn so the neck
                // carries part of the movement; keep root and all gameplay anchors fixed.
                var basis=companion.transform.rotation;
                var desired=basis*Quaternion.Euler(PrivacyPitch,PrivacyYaw,0)*Quaternion.Inverse(basis)*headBone.rotation;
                if(upperChest!=null)upperChest.rotation=basis*Quaternion.Euler(PrivacyPitch*.12f,PrivacyYaw*.12f,0)*Quaternion.Inverse(basis)*upperChest.rotation;
                if(neckBone!=null)neckBone.rotation=basis*Quaternion.Euler(PrivacyPitch*.28f,PrivacyYaw*.28f,0)*Quaternion.Inverse(basis)*neckBone.rotation;
                headBone.rotation=desired;
            }
            if(OfferWeight>0)
            {
                // Align the palm, not the wrist. Preserve the clip's palm-up rotation through IK.
                var rotation=rightHand.rotation;
                rotation=Quaternion.Slerp(rotation,Quaternion.FromToRotation(PalmNormal,Vector3.up)*rotation,OfferWeight);
                var offset=rotation*Quaternion.Inverse(rightHand.rotation)*(PalmPosition-rightHand.position);
                var target=Vector3.Lerp(rightHand.position,companion.photoHandTarget-offset,OfferWeight);
                for(int i=0;i<16;i++)
                {
                    Aim(rightLowerArm,target);
                    Aim(rightUpperArm,target);
                }
                rightHand.rotation=rotation;
            }
            for(int i=0;i<gripBones.Length;i++)
            {
                gripBase[i]=gripBones[i].localRotation;
                // She offers a relaxed open palm underneath the player's hand.
                // Replace the clip's curled fingers during the offer, rather than
                // adding another closing rotation when the player takes her hand.
                var bone=gripBones[i];
                if(bone.childCount>0 && OfferWeight>0)
                {
                    float curl=bone.name.EndsWith("Proximal")?4:bone.name.EndsWith("Intermediate")?10:16;
                    float spread=bone.name.Contains("Index")?-.08f:bone.name.Contains("Pinky")?.10f:0;
                    var direction=rightHand.up*Mathf.Cos(curl*Mathf.Deg2Rad)+PalmNormal*Mathf.Sin(curl*Mathf.Deg2Rad)+rightHand.forward*spread;
                    var targetRotation=Quaternion.FromToRotation(bone.GetChild(0).position-bone.position,direction)*bone.rotation;
                    bone.rotation=Quaternion.Slerp(bone.rotation,targetRotation,OfferWeight);
                }
                gripApplied[i]=gripBones[i].localRotation;
            }
            headApplied=headBone.localRotation;upperApplied=rightUpperArm.localRotation;lowerApplied=rightLowerArm.localRotation;applied=true;
            handApplied=rightHand.localRotation;
            if(upperChest!=null)chestApplied=upperChest.localRotation;
            if(neckBone!=null)neckApplied=neckBone.localRotation;
            for(int i=0;i<hairBones.Length;i++)hairBones[i].localRotation=hairHomes[i]*Quaternion.Euler(0,HairLag*(i==0?1:.75f),HairLag*.24f);
        }
        void Aim(Transform joint,Vector3 target)
        {
            var from=rightHand.position-joint.position;var to=target-joint.position;
            if(from.sqrMagnitude>.000001f && to.sqrMagnitude>.000001f)
                joint.rotation=Quaternion.FromToRotation(from,to)*joint.rotation;
        }
    }
}
