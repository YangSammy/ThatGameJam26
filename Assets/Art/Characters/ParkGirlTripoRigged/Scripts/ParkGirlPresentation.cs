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
        // Legacy privacy settings retained only for existing prefab/editor compatibility.
        [HideInInspector] public bool useNaturalPrivacyPose;
        public Transform neckBone;
        // Retained for serialized prefab compatibility; no longer drives head pitch.
        [HideInInspector] public float privacyLookDown;
        [HideInInspector] public float privacyLookAway=-42;
        [HideInInspector] public float privacyPoseSeconds=.6f;
        public float PrivacyPitch {get;private set;}
        public float PrivacyYaw {get;private set;}
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
        Vector3 headFaceAxis,neckFaceAxis;
        bool faceAxesReady;
        float offerHeadWeight;
        Transform[] poseBones;
        Vector3[] posePositions,poseScales;
        Quaternion[] poseRotations;
        bool hasFinalPose;
        FirstPersonHands playerHands;
        float contactGrip;
        void Awake()
        {
            if(companion!=null && companion.photoHeadPivot!=null)
                proxyHome=companion.photoHeadPivot.localRotation;
            hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;
            performanceAnimator=GetComponentInChildren<Animator>();
            poseBones=performanceAnimator!=null?performanceAnimator.GetComponentsInChildren<Transform>(true):new Transform[0];
            posePositions=new Vector3[poseBones.Length];poseScales=new Vector3[poseBones.Length];poseRotations=new Quaternion[poseBones.Length];
            if(neckBone==null && headBone!=null)neckBone=headBone.parent;
            CacheFaceAxes();
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
            if(playerHands==null && companion.player!=null)playerHands=companion.player.GetComponentInChildren<FirstPersonHands>();
            float gripTarget=offering && playerHands!=null?playerHands.PhotoHandGrip:0;
            contactGrip=Mathf.MoveTowards(contactGrip,gripTarget,dt/.2f);
            offerHeadWeight=Mathf.MoveTowards(offerHeadWeight,offering?1:0,dt/.35f);
            if(offering){if(!wasOffering)OfferElapsed=0;OfferElapsed+=dt;}
            else OfferElapsed=0;
            float target=offering?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,offerContactSeconds,OfferElapsed)):0;
            OfferWeight=offering?target:Mathf.MoveTowards(OfferWeight,0,dt/.3f);
            // ParkCompanion's smoothed photoHeadPivot is the sole look target.
            PrivacyPitch=PrivacyYaw=0;
            wasOffering=offering;
        }
        void LateUpdate(){ApplyPhotoPose();}
        public void ApplyPhotoPose()
        {
            if(headBone==null)return;
            if(hairHomes==null){hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;}
            if(companion==null)return;
            if(companion.AnimationPaused){RestoreFinalPose();return;}
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
            ApplyOfferHeadPitch(offerHeadWeight);
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
                // Keep the offering hand open, then softly cup the player's hand
                // after contact instead of closing into a fist before arrival.
                var bone=gripBones[i];
                if(bone.childCount>0 && OfferWeight>0)
                {
                    float openCurl=bone.name.EndsWith("Proximal")?4:bone.name.EndsWith("Intermediate")?10:16;
                    float heldCurl=bone.name.EndsWith("Proximal")?12:bone.name.EndsWith("Intermediate")?26:38;
                    float curl=Mathf.Lerp(openCurl,heldCurl,contactGrip);
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
            CacheFinalPose();
        }
        void CacheFinalPose()
        {
            if(poseBones==null)return;
            for(int i=0;i<poseBones.Length;i++)
            {posePositions[i]=poseBones[i].localPosition;poseRotations[i]=poseBones[i].localRotation;poseScales[i]=poseBones[i].localScale;}
            hasFinalPose=true;
        }
        void RestoreFinalPose()
        {
            // An Animator with speed=0 still evaluates its clip. Reapply the final
            // pose after it so pause freezes the visible result, including IK.
            if(!hasFinalPose)return;
            for(int i=0;i<poseBones.Length;i++)
            {poseBones[i].localPosition=posePositions[i];poseBones[i].localRotation=poseRotations[i];poseBones[i].localScale=poseScales[i];}
        }
        void CacheFaceAxes()
        {
            if(faceAxesReady || companion==null || headBone==null)return;
            headFaceAxis=BindFaceAxis(headBone);
            if(neckBone!=null)neckFaceAxis=BindFaceAxis(neckBone);
            faceAxesReady=true;
        }
        Vector3 BindFaceAxis(Transform bone)
        {
            // Read the neutral rig, not the current animation frame (also safe after reload).
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.sharedMesh==null)continue;
                int index=System.Array.IndexOf(skin.bones,bone);
                var bind=skin.sharedMesh.bindposes;
                if(index<0 || index>=bind.Length)continue;
                return bind[index].MultiplyVector(skin.transform.InverseTransformDirection(companion.transform.forward)).normalized;
            }
            return bone.InverseTransformDirection(companion.transform.forward).normalized;
        }
        void ApplyOfferHeadPitch(float weight)
        {
            if(weight<=0)return;
            CacheFaceAxes();
            if(!faceAxesReady)return;
            // Neck is an ancestor of the head, but not of either arm. Preserve the
            // final animated head orientation before correcting the neck so the
            // two corrections blend independently, without changing head yaw.
            var originalHead=headBone.rotation;
            LevelFace(neckBone,neckFaceAxis,weight);
            headBone.rotation=originalHead;
            LevelFace(headBone,headFaceAxis,weight);
        }
        void LevelFace(Transform bone,Vector3 faceAxis,float weight)
        {
            if(bone==null)return;
            var facing=bone.rotation*faceAxis;
            var horizontal=Vector3.ProjectOnPlane(facing,companion.transform.up);
            if(horizontal.sqrMagnitude<.000001f)return;
            // This is pitch around the face's horizontal right axis only: no
            // camera look-at, yaw replacement, chest, arm or contact adjustment.
            var correction=Quaternion.FromToRotation(facing,horizontal.normalized);
            bone.rotation=Quaternion.Slerp(Quaternion.identity,correction,weight)*bone.rotation;
        }
        void Aim(Transform joint,Vector3 target)
        {
            var from=rightHand.position-joint.position;var to=target-joint.position;
            if(from.sqrMagnitude>.000001f && to.sqrMagnitude>.000001f)
                joint.rotation=Quaternion.FromToRotation(from,to)*joint.rotation;
        }
    }
}
