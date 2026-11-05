using UnityEngine;

namespace MemoryWhitebox
{
    // A presentation sample of the existing girl rig. The chapter owns time and departure.
    // No Update, gameplay callbacks, input or persistent state.
    public sealed class AutumnFarewellVisual : MonoBehaviour
    {
        public Animator rig;
        public AnimationClip idle, wave;
        public Transform hips, head, chest;
        public Transform[] hair = new Transform[0];
        public Transform[] groundedBones = new Transform[0];
        Vector3 hipsHome;
        Quaternion[] hairHome;
        Vector3[] groundedPosition;
        Quaternion[] groundedRotation;
        bool initialized;
        Transform pointingArm,pointingElbow;

        void Initialize()
        {
            if(initialized)return;
            rig.enabled=false;rig.applyRootMotion=false;
            idle.SamplePropAnimation(rig.gameObject,0);
            hipsHome=hips.localPosition;
            hairHome=new Quaternion[hair.Length];
            groundedPosition=new Vector3[groundedBones.Length];groundedRotation=new Quaternion[groundedBones.Length];
            for(int i=0;i<hair.Length;i++)hairHome[i]=hair[i].localRotation;
            foreach(var bone in rig.GetComponentsInChildren<Transform>(true))
            {
                if(bone.name=="Right_UpperArm")pointingArm=bone;
                if(bone.name=="Right_LowerArm")pointingElbow=bone;
            }
            initialized=true;
        }

        public void PointToward(Vector3 target,float weight,float elapsed)
        {
            if(pointingArm==null || pointingElbow==null)return;
            // Orient the shoulder line first; the arm reinforces the same direction.
            // Hair tips follow slightly later, keeping the face unobscured.
            var localDirection=transform.InverseTransformDirection(target-chest.position);
            float yaw=Mathf.Clamp(Mathf.Atan2(localDirection.x,localDirection.z)*Mathf.Rad2Deg,-22,22);
            chest.rotation=Quaternion.AngleAxis(yaw*Mathf.Clamp01(weight),transform.up)*chest.rotation;
            float delayed=Mathf.Sin(Mathf.PI*Mathf.Clamp01((elapsed-.14f)/1.56f));
            for(int i=0;i<hair.Length;i++)
                hair[i].localRotation*=Quaternion.Euler(0,-yaw*delayed*.2f,-yaw*delayed*.04f);
            var direction=target-pointingArm.position;
            var current=pointingElbow.position-pointingArm.position;
            if(direction.sqrMagnitude<.0001f || current.sqrMagnitude<.0001f)return;
            var aim=Quaternion.FromToRotation(current,direction)*pointingArm.rotation;
            pointingArm.rotation=Quaternion.Slerp(pointingArm.rotation,aim,Mathf.Clamp01(weight)*.85f);
            head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(direction,transform.up),Mathf.Clamp01(weight)*.18f);
        }

        public void Present(float clock,float farewellSeconds,bool departing)
        {
            Initialize();
            // The source take has two seconds of lead-in. Sample its raise/wave/lower
            // section inside the chapter's existing two-second farewell, without root motion.
            idle.SamplePropAnimation(rig.gameObject,Mathf.Repeat(clock,idle.length));
            if(departing)
            {
                // Source wave includes a large leg lift. Preserve the existing grounded
                // idle lower body while sampling its authored shoulder/arm/head gesture.
                for(int i=0;i<groundedBones.Length;i++)
                {groundedPosition[i]=groundedBones[i].localPosition;groundedRotation[i]=groundedBones[i].localRotation;}
                wave.SamplePropAnimation(rig.gameObject,Mathf.Lerp(2.35f,4.65f,Mathf.Clamp01(farewellSeconds/2)));
                for(int i=0;i<groundedBones.Length;i++)
                {groundedBones[i].localPosition=groundedPosition[i];groundedBones[i].localRotation=groundedRotation[i];}
            }
            hips.localPosition=hipsHome;
            float turn=departing?Mathf.Sin(Mathf.PI*Mathf.Clamp01(farewellSeconds/2)):0;
            chest.rotation=Quaternion.AngleAxis(8*turn,transform.up)*chest.rotation;
            head.rotation=Quaternion.AngleAxis(16*turn,transform.up)*head.rotation;
            // Existing weighted hair tips lag behind the head, rather than moving a hair sphere.
            float lag=departing?Mathf.Sin(Mathf.PI*Mathf.Clamp01((farewellSeconds-.16f)/2))*-5:Mathf.Sin(clock*1.5f)*.7f;
            for(int i=0;i<hair.Length;i++)hair[i].localRotation=hairHome[i]*Quaternion.Euler(0,lag,lag*.22f);
        }
    }
}
