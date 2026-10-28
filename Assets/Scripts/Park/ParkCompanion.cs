using UnityEngine;
namespace MemoryWhitebox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ParkCompanion : MonoBehaviour
    {
        public enum Moment { None, Walking, Waterside, Response }
        [Header("路线与玩家")]
        public Transform player,spawn;
        public ParkPhotoWaypoint[] route;
        [Min(.1f)] public float walkSpeed=1.25f;
        [Min(2)] public float waitDistance=9;
        [Min(1)] public float callDistance=11;
        [Header("白模通行与人物避让")]
        [Min(.1f),Tooltip("玩家在行进方向内靠近时停步，不直接挤过玩家。")]
        public float personalSpace=.8f;
        [Min(.01f),Tooltip("按水平距离抵达路线点，高度交由地面碰撞决定。")]
        public float waypointTolerance=.12f;
        [Min(.1f)] public float gravity=18;
        CharacterController motor;
        float verticalSpeed;
        [Header("照片判定位置（替换人物后移动到相应部位）")]
        public Transform head,chest,hips,feet;
        [Header("临时模型动作；正式模型可清空这些引用")]
        public Transform visual,leftArm,rightArm,leftLeg,rightLeg;
        [Tooltip("可选正式动画控制器；只在存在 Speed 浮点参数时写入。") ] public Animator animator;
        public AudioSource footsteps;
        public AudioClip footstep;
        public Moment CurrentMoment {get;private set;}
        public bool Paused {get;set;}
        [System.NonSerialized] public ParkPhotoWaypoint photoHoldNode;
        [System.NonSerialized] public bool photoLookBack;
        [System.NonSerialized] public bool photoOfferHand;
        [System.NonSerialized] public Vector3 photoHandTarget;
        Quaternion headHome;
        float entranceShutterPose;
        public void PoseEntranceShutter()
        {entranceShutterPose=.6f;if(head!=null)head.localRotation=headHome*Quaternion.Euler(0,8,-5);}
        public bool AtPhotoNode=>atNode && route!=null && index<route.Length && route[index]==photoHoldNode;
        public bool Waiting {get;private set;}
        public bool Finished => route!=null && index>=route.Length;
        public string Line {get;private set;}
        int index;float stopRemaining,respondRemaining,callCooldown,phase,lineTime,stepTime;
        bool atNode,hasSpeed;
        Vector3 visualHome;
        void Start()
        {
            motor=GetComponent<CharacterController>();
            if(head!=null)headHome=head.localRotation;
            if(visual!=null)visualHome=visual.localPosition;
            if(animator!=null)foreach(var p in animator.parameters)if(p.name=="Speed" && p.type==AnimatorControllerParameterType.Float)hasSpeed=true;
            RestartWalk();
        }
        public void RestartWalk()
        {
            if(motor==null)motor=GetComponent<CharacterController>();
            if(spawn!=null){if(motor!=null)motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);if(motor!=null)motor.enabled=true;}
            verticalSpeed=0;
            index=0;stopRemaining=respondRemaining=callCooldown=phase=0;atNode=false;Waiting=false;CurrentMoment=Moment.None;
            Say("Let's walk.",4);
        }
        public bool Call()
        {
            if(player==null || Paused || callCooldown>0 || Vector3.Distance(player.position,transform.position)>callDistance)return false;
            respondRemaining=5;callCooldown=6;Say("Hey, I'm here.",3);return true;
        }
        void Say(string text,float seconds){Line=text;lineTime=seconds;}
        void Face(Vector3 point,float dt)
        {var direction=point-transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),160*dt);}
        void Update()
        {
            if(Paused || player==null || route==null || route.Length==0){Animate(false,0);return;}
            float dt=Mathf.Min(Time.deltaTime,.1f);callCooldown=Mathf.Max(0,callCooldown-dt);lineTime-=dt;if(lineTime<=0)Line="";
            Vector3 requestedMove=Vector3.zero;
            Waiting=false;bool walking=false;CurrentMoment=Moment.None;
            if(respondRemaining>0)
            {respondRemaining-=dt;Face(player.position,dt);CurrentMoment=Moment.Response;}
            else if(Vector3.Distance(player.position,transform.position)>waitDistance)
            {Waiting=true;Face(player.position,dt);}
            else if(!Finished)
            {
                var node=route[index];if(node==null){index++;return;}
                if(!atNode)
                {
                    var delta=node.transform.position-transform.position;delta.y=0;
                    var toPlayer=player.position-transform.position;toPlayer.y=0;
                    if(delta.magnitude<waypointTolerance){atNode=true;stopRemaining=node.pauseSeconds;}
                    else if(toPlayer.magnitude<personalSpace && Vector3.Dot(delta.normalized,toPlayer.normalized)>.25f)
                    {Waiting=true;Face(player.position,dt);}
                    else
                    {Face(node.transform.position,dt);requestedMove=Vector3.ClampMagnitude(delta,walkSpeed*dt);}
                }
                if(atNode)
                {
                    bool photoHold=node==photoHoldNode;
                    if(photoHold && photoLookBack){Face(player.position,dt);CurrentMoment=Moment.Response;}
                    else
                    {
                        if(node.lookAt!=null)Face(node.lookAt.position,dt);
                        if(node.watersideMoment)CurrentMoment=Moment.Waterside;
                    }
                    if(!photoHold){stopRemaining-=dt;if(stopRemaining<=0){atNode=false;index++;}}
                }
            }
            else Face(player.position,dt);
            if(motor!=null && motor.enabled)
            {
                var before=transform.position;
                float remaining=dt;
                while(remaining>0)
                {
                    float step=Mathf.Min(.02f,remaining);remaining-=step;
                    if(motor.isGrounded && verticalSpeed<0)verticalSpeed=-2;
                    verticalSpeed-=gravity*step;
                    motor.Move(requestedMove*(step/Mathf.Max(.0001f,dt))+Vector3.up*verticalSpeed*step);
                }
                var travelled=transform.position-before;travelled.y=0;
                walking=travelled.sqrMagnitude>.000001f;
                if(walking)CurrentMoment=Moment.Walking;
                else if(requestedMove.sqrMagnitude>0)Waiting=true;
                if(transform.position.y< -3)RestartWalk();
            }
            Animate(walking,dt);
            if(walking){stepTime-=dt;if(stepTime<=0){stepTime=.48f;if(footsteps!=null && footstep!=null)footsteps.PlayOneShot(footstep,.3f);}}else stepTime=0;
        }
        void Animate(bool walking,float dt)
        {
            entranceShutterPose=Mathf.Max(0,entranceShutterPose-dt);
            if(head!=null && entranceShutterPose>0)head.localRotation=headHome*Quaternion.Euler(0,8,-5);
            else if(head!=null)head.localRotation=Quaternion.Slerp(head.localRotation,headHome,1-Mathf.Exp(-dt*6));
            phase+=dt*(walking?7:2);float swing=walking?Mathf.Sin(phase)*25:0;
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,-7);
            if(rightArm!=null)rightArm.localRotation=respondRemaining>0?Quaternion.Euler(-25,0,115+Mathf.Sin(phase*4)*12):Quaternion.Euler(swing*.7f,0,7);
            if(photoOfferHand && rightArm!=null)
            {var direction=photoHandTarget-rightArm.position;if(direction.sqrMagnitude>.001f)rightArm.rotation=Quaternion.FromToRotation(Vector3.down,direction.normalized);}
            if(visual!=null)visual.localPosition=visualHome+Vector3.up*(walking?Mathf.Abs(Mathf.Sin(phase))*.025f:0);
            if(hasSpeed && animator!=null)animator.SetFloat("Speed",walking?walkSpeed:0);
        }
    }
}
