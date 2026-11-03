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
        [Range(0,1)] public float shoulderFollow=.24f;
        [Min(.01f)] public float shoulderDelay=.26f;
        [Min(.01f)] public float hairDelay=.19f;
        public float ShoulderYaw {get;private set;}
        public float HairLag {get;private set;}
        Quaternion proxyHome, headBase, upperBase, lowerBase;
        Quaternion headApplied, upperApplied, lowerApplied;
        Quaternion chestBase,chestApplied;
        Quaternion[] hairHomes;
        float shoulderVelocity,hairVelocity,lastHeadYaw;
        int dynamicsFrame=-1;
        bool applied;
        void Awake()
        {
            if(companion!=null && companion.photoHeadPivot!=null)
                proxyHome=companion.photoHeadPivot.localRotation;
            hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;
        }
        void LateUpdate(){ApplyPhotoPose();}
        public void ApplyPhotoPose()
        {
            if(headBone==null)return;
            if(hairHomes==null){hairHomes=new Quaternion[hairBones.Length];for(int i=0;i<hairBones.Length;i++)hairHomes[i]=hairBones[i].localRotation;}
            if(companion==null)return;
            // Shutter capture can request this a second time before LateUpdate. Don't compound offsets.
            if(applied && Quaternion.Angle(headBone.localRotation,headApplied)<.001f)headBone.localRotation=headBase;
            if(applied && Quaternion.Angle(rightUpperArm.localRotation,upperApplied)<.001f)rightUpperArm.localRotation=upperBase;
            if(applied && Quaternion.Angle(rightLowerArm.localRotation,lowerApplied)<.001f)rightLowerArm.localRotation=lowerBase;
            if(upperChest!=null && applied && Quaternion.Angle(upperChest.localRotation,chestApplied)<.001f)upperChest.localRotation=chestBase;
            headBase=headBone.localRotation;upperBase=rightUpperArm.localRotation;lowerBase=rightLowerArm.localRotation;
            if(upperChest!=null)chestBase=upperChest.localRotation;
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
            if(companion.photoOfferHand)
            {
                // Preserve the authored contact point; solve the new arm without scaling its bones.
                for(int i=0;i<8;i++)
                {
                    Aim(rightLowerArm,companion.photoHandTarget);
                    Aim(rightUpperArm,companion.photoHandTarget);
                }
            }
            headApplied=headBone.localRotation;upperApplied=rightUpperArm.localRotation;lowerApplied=rightLowerArm.localRotation;applied=true;
            if(upperChest!=null)chestApplied=upperChest.localRotation;
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
