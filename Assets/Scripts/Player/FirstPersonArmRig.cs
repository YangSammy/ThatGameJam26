using UnityEngine;

namespace MemoryWhitebox
{
    // Presentation only. FirstPersonHands evaluates this after gameplay and before a synchronous photo.
    public sealed class FirstPersonArmRig : MonoBehaviour
    {
        public Animator animator;
        public Transform model;
        public Transform[] upperArms=new Transform[2],forearms=new Transform[2],wrists=new Transform[2];
        [Header("Model placement (camera local)")]
        public Vector3 modelOffset=new Vector3(0,-1.7432f,.12f),modelEuler;
        [Min(.01f)] public float displayScale=1;
        [Header("Animation")]
        [Min(.01f)] public float blendSeconds=.18f,reachSeconds=.25f;
        [Range(0,1)] public float grabHoldTime=.55f,pushHoldTime=.4f;
        [Min(.1f)] public float idleDuration=1.966667f;
        [Header("Contact (no bone scaling)")]
        public Vector3[] contactOffsets={Vector3.zero,Vector3.zero};
        [Tooltip("旧定位点坐标系到腕骨坐标系的旋转偏移。")]
        public Vector3[] contactEuler={new Vector3(351,107,70),new Vector3(351,253,290)};
        [Range(0,1)] public float contactRotationWeight=.35f;
        [Range(.5f,.99f)] public float maximumExtension=.97f;
        [Tooltip("接触较远目标时允许整个双臂显示模型前倾的最大距离，不改变骨长或场景定位点。")]
        [Range(0,.5f)] public float maximumBodyReach=.45f;
        [Tooltip("前倾时压低肩部，避免完整模型的肩口进入第一人称画面。")]
        [Range(0,.3f)] public float reachLowering=.18f;
        public Vector3[] elbowHints={new Vector3(-.5f,-.35f,.05f),new Vector3(.5f,-.35f,.05f)};
        public struct Pose
        {
            public Vector3 position;
            public Quaternion rotation;
            public float grip,push,contact;
            public static Pose At(Vector3 position,Quaternion rotation,float grip=0,float push=0)
            {return new Pose{position=position,rotation=rotation,grip=grip,push=push,contact=1};}
        }
        readonly float[] grip=new float[2],push=new float[2],contact=new float[2];
        readonly Vector3[] targets=new Vector3[2];
        readonly Quaternion[] rotations={Quaternion.identity,Quaternion.identity};
        static readonly int idle=Animator.StringToHash("Idle.Relax");
        static readonly int[] grabs={Animator.StringToHash("LeftGrab.Grab"),Animator.StringToHash("RightGrab.Grab")};
        static readonly int[] pushes={Animator.StringToHash("LeftPush.Push"),Animator.StringToHash("RightPush.Push")};
        float elapsed;
        Vector3 bodyReach;
        bool initialized;
        public bool Valid=>animator!=null && model!=null && animator.runtimeAnimatorController!=null &&
            upperArms.Length==2 && forearms.Length==2 && wrists.Length==2 &&
            upperArms[0]!=null && upperArms[1]!=null && forearms[0]!=null && forearms[1]!=null && wrists[0]!=null && wrists[1]!=null;

        public void Evaluate(Pose left,Pose right,float dt,bool immediate=false)
        {
            if(!Valid || !gameObject.activeInHierarchy)return;
            model.localPosition=modelOffset;model.localRotation=Quaternion.Euler(modelEuler);model.localScale=Vector3.one*Mathf.Max(.01f,displayScale);
            if(!initialized){animator.Rebind();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=0;initialized=true;}
            dt=Mathf.Max(0,dt);elapsed+=dt;
            animator.Play(idle,0,Mathf.Repeat(elapsed/Mathf.Max(.1f,idleDuration),1));
            Prepare(0,left,dt,immediate);Prepare(1,right,dt,immediate);
            // All animation writes finish before the procedural rotation-only solve.
            animator.Update(0);
            // A bounded whole-body lean lets existing desk/meadow anchors remain useful.
            // The complete skin and skeleton translate together; limb lengths never change.
            Vector3 requested=Vector3.zero;
            for(int side=0;side<2;side++)
            {
                if(contact[side]<=0)continue;
                float length=Vector3.Distance(upperArms[side].position,forearms[side].position)+Vector3.Distance(forearms[side].position,wrists[side].position);
                var delta=transform.TransformPoint(targets[side])+transform.rotation*rotations[side]*contactOffsets[side]-upperArms[side].position;
                var reach=delta.normalized*Mathf.Max(0,delta.magnitude-length*maximumExtension)*contact[side];
                if(reach.sqrMagnitude>requested.sqrMagnitude)requested=reach;
            }
            var lowering=-transform.up*Mathf.Min(reachLowering,requested.magnitude);
            requested=Vector3.zero;
            for(int side=0;side<2;side++)
            {
                if(contact[side]<=0)continue;
                float length=Vector3.Distance(upperArms[side].position,forearms[side].position)+Vector3.Distance(forearms[side].position,wrists[side].position);
                var delta=transform.TransformPoint(targets[side])+transform.rotation*rotations[side]*contactOffsets[side]-upperArms[side].position-lowering;
                var reach=delta.normalized*Mathf.Max(0,delta.magnitude-length*maximumExtension)*contact[side];
                if(reach.sqrMagnitude>requested.sqrMagnitude)requested=reach;
            }
            requested+=lowering;
            requested=Vector3.ClampMagnitude(transform.InverseTransformVector(requested),maximumBodyReach);
            bodyReach=Vector3.Lerp(bodyReach,requested,immediate?1:1-Mathf.Exp(-dt/Mathf.Max(.01f,reachSeconds)*4));
            model.localPosition=modelOffset+bodyReach;
            Solve(0);Solve(1);
        }
        void Prepare(int side,Pose pose,float dt,bool immediate)
        {
            float step=immediate?1:dt/Mathf.Max(.01f,blendSeconds);
            grip[side]=Mathf.MoveTowards(grip[side],Mathf.Clamp01(pose.grip),step);
            push[side]=Mathf.MoveTowards(push[side],Mathf.Clamp01(pose.push),step);
            contact[side]=Mathf.MoveTowards(contact[side],Mathf.Clamp01(pose.contact),step);
            if(pose.contact>0)
            {
                float blend=immediate?1:1-Mathf.Exp(-dt/Mathf.Max(.01f,reachSeconds)*4);
                var local=transform.InverseTransformPoint(pose.position);
                if(contact[side]<=step)targets[side]=transform.InverseTransformPoint(wrists[side].position);
                targets[side]=Vector3.Lerp(targets[side],local,blend);
                rotations[side]=Quaternion.Slerp(rotations[side],Quaternion.Inverse(transform.rotation)*pose.rotation,blend);
            }
            int layer=1+side*2;
            animator.SetLayerWeight(layer,grip[side]);animator.SetLayerWeight(layer+1,push[side]);
            animator.Play(grabs[side],layer,grabHoldTime*grip[side]);
            animator.Play(pushes[side],layer+1,pushHoldTime*push[side]);
        }
        void Solve(int side)
        {
            if(contact[side]<=0)return;
            var a=upperArms[side];var b=forearms[side];var c=wrists[side];
            var desiredRotation=transform.rotation*rotations[side]*Quaternion.Euler(contactEuler[side]);
            var target=transform.TransformPoint(targets[side])+transform.rotation*rotations[side]*contactOffsets[side];
            target=Vector3.Lerp(c.position,target,contact[side]);
            Vector3 local=transform.InverseTransformPoint(target);local.z=Mathf.Max(.16f,local.z);target=transform.TransformPoint(local);
            Quaternion wristRotation=c.rotation;
            float upper=Vector3.Distance(a.position,b.position),lower=Vector3.Distance(b.position,c.position);
            if(upper<.0001f || lower<.0001f)return;
            var delta=target-a.position;
            float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.001f,(upper+lower)*maximumExtension);
            var direction=delta.sqrMagnitude>.000001f?delta.normalized:transform.forward;
            var pole=Vector3.ProjectOnPlane(transform.TransformPoint(elbowHints[side])-a.position,direction).normalized;
            if(pole.sqrMagnitude<.001f)pole=Vector3.ProjectOnPlane(transform.up,direction).normalized;
            float along=(upper*upper-lower*lower+length*length)/(2*length);
            var elbow=a.position+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            target=a.position+direction*length;
            a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
            b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;
            c.rotation=Quaternion.Slerp(wristRotation,desiredRotation,contact[side]*contactRotationWeight);
        }
        public void ResetPose()
        {
            elapsed=0;bodyReach=Vector3.zero;
            for(int i=0;i<2;i++){grip[i]=push[i]=contact[i]=0;targets[i]=Vector3.zero;rotations[i]=Quaternion.identity;}
            if(initialized && Valid && gameObject.activeInHierarchy)Evaluate(default(Pose),default(Pose),0,true);
        }
        void OnDisable(){ResetPose();initialized=false;}
    }
}
