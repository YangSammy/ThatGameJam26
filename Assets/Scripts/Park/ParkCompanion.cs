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
        public ParkGirlPresentation visualPresentation;
        public AudioSource footsteps;
        public AudioClip footstep;
        public Moment CurrentMoment {get;private set;}
        public bool Paused {get;set;}
        public bool AnimationPaused=>Paused || GamePauseMenu.BlocksGameplay;
        public bool TravelPaused {get;set;}
        [System.NonSerialized] public ParkPhotoWaypoint photoHoldNode;
        [System.NonSerialized] public bool photoLookBack;
        [System.NonSerialized] public bool photoOfferHand;
        [System.NonSerialized] public Vector3 photoHandTarget;
        [Header("Photographic performance bindings")]
        public Transform photoHeadPivot,photoHairPivot,photoRightHand;
        [Min(.1f)] public float photoTurnSpeed=72;
        Quaternion headHome,photoHeadHome,photoHairHome;
        Vector3 rightArmScale;
        float hairResponse;
        float entranceShutterPose;
        ParkPhotoWaypoint responseNode;
        Quaternion responseHeading;
        bool hasResponseHeading;
        public bool PhotoResponseSettled=>AtPhotoNode && photoLookBack && hasResponseHeading &&
            responseNode==photoHoldNode && Quaternion.Angle(transform.rotation,responseHeading)<10;
        public void PoseEntranceShutter()
        {
            entranceShutterPose=.6f;
            if(photoHeadPivot!=null)photoHeadPivot.localRotation=photoHeadHome*Quaternion.Euler(0,12,-7);
            else if(head!=null)head.localRotation=headHome*Quaternion.Euler(0,12,-7);
            if(photoHairPivot!=null)photoHairPivot.localRotation=photoHairHome*Quaternion.Euler(0,-4,3);
            if(visualPresentation!=null)visualPresentation.ApplyPhotoPose();
        }
        public void PosePhotoResponseForShutter()
        {
            // Commit the visible hand pose before Camera.Render, independent of Update ordering.
            Animate(false,0);
            if(visualPresentation!=null)visualPresentation.ApplyPhotoPose();
        }
        public bool AtPhotoNode=>atNode && route!=null && index<route.Length && route[index]==photoHoldNode;
        public bool Waiting {get;private set;}
        // Paired samples from the same movement step; waiting is not a blocked move.
        public bool TravelRequestedLastStep {get;private set;}
        public float TravelDistanceLastStep {get;private set;}
        public bool Finished => route!=null && index>=route.Length;
        public string Line {get;private set;}
        int index;float stopRemaining,respondRemaining,callCooldown,phase,lineTime,stepTime;
        bool atNode,hasSpeed;
        bool started;
        ParkPhotoWaypoint pendingResume;
        Vector3 visualHome;
        void Start()
        {
            motor=GetComponent<CharacterController>();
            if(head!=null)headHome=head.localRotation;
            if(photoHeadPivot!=null)photoHeadHome=photoHeadPivot.localRotation;
            if(photoHairPivot!=null)photoHairHome=photoHairPivot.localRotation;
            if(rightArm!=null)rightArmScale=rightArm.localScale;
            if(visual!=null)visualHome=visual.localPosition;
            if(animator!=null)foreach(var p in animator.parameters)if(p.name=="Speed" && p.type==AnimatorControllerParameterType.Float)hasSpeed=true;
            started=true;
            var resume=pendingResume;pendingResume=null;
            RestartWalk();
            if(resume!=null)ResumeAtPhotoNode(resume);
        }
        public bool ResumeAtPhotoNode(ParkPhotoWaypoint node)
        {
            int target=route!=null?System.Array.IndexOf(route,node):-1;
            if(node==null || target<0)return false;
            if(!started){pendingResume=node;return true;}
            if(motor==null)motor=GetComponent<CharacterController>();
            bool wasEnabled=motor!=null && motor.enabled;
            if(wasEnabled)motor.enabled=false;
            transform.position=node.transform.position;
            if(node.lookAt!=null)
            {var forward=node.lookAt.position-transform.position;forward.y=0;if(forward.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(forward);}
            if(wasEnabled)motor.enabled=true;
            index=target;atNode=true;stopRemaining=node.pauseSeconds;verticalSpeed=0;
            hasResponseHeading=false;responseNode=null;
            TravelRequestedLastStep=false;TravelDistanceLastStep=0;
            respondRemaining=callCooldown=0;Waiting=false;CurrentMoment=Moment.None;Line="";
            return true;
        }
        public void RestartWalk()
        {
            if(motor==null)motor=GetComponent<CharacterController>();
            if(spawn!=null){if(motor!=null)motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);if(motor!=null)motor.enabled=true;}
            verticalSpeed=0;
            hasResponseHeading=false;responseNode=null;
            index=0;stopRemaining=respondRemaining=callCooldown=phase=0;atNode=false;Waiting=false;CurrentMoment=Moment.None;
            TravelRequestedLastStep=false;TravelDistanceLastStep=0;
            Say("Let's walk.",4);
        }
        public bool ReleasePhotoStop(ParkPhotoWaypoint completedNode)
        {
            // Only the committed photo's current stop may skip its scenic dwell.
            // Keep ordinary route pauses and do not advance the index twice on retry.
            if(completedNode==null || !AtPhotoNode || photoHoldNode!=completedNode)return false;
            stopRemaining=0;
            photoHoldNode=null;
            return true;
        }
        public bool Call()
        {
            if(player==null || Paused || callCooldown>0 || Vector3.Distance(player.position,transform.position)>callDistance)return false;
            respondRemaining=5;callCooldown=6;Say("Hey, I'm here.",3);return true;
        }
        void Say(string text,float seconds){Line=text;lineTime=seconds;}
        void Face(Vector3 point,float dt)
        {var direction=point-transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),160*dt);}
        void UpdateResponseHeading()
        {
            if(!AtPhotoNode || !photoLookBack){hasResponseHeading=false;responseNode=null;return;}
            if(hasResponseHeading && responseNode==photoHoldNode)return;
            // Stable authored composition also restores the same pose after reload.
            // Only the head follows subsequent player movement; the palm anchor stays fixed.
            var target=photoHoldNode.referenceView!=null?photoHoldNode.referenceView.position:player.position;
            var forward=target-transform.position;forward.y=0;
            responseHeading=forward.sqrMagnitude>.001f?Quaternion.LookRotation(forward)*Quaternion.Euler(0,-12,0):transform.rotation;
            responseNode=photoHoldNode;hasResponseHeading=true;
        }
        void RecoverFromFall()
        {
            // photoHoldNode is the current unfinished task, including while travelling.
            var target=photoHoldNode;
            if(target==null || route==null || System.Array.IndexOf(route,target)<0)
            {
                target=null;
                if(route!=null)for(int i=Mathf.Min(index,route.Length-1);i>=0;i--)
                    if(route[i]!=null && route[i].photoTask!=ParkPhotoWaypoint.PhotoTask.None){target=route[i];break;}
            }
            if(target!=null && ResumeAtPhotoNode(target))return;
            // Standalone companion previews without photography keep their old fallback.
            RestartWalk();
        }
        void Update(){TickTravel(Mathf.Min(Time.deltaTime,.1f));}
        void TickTravel(float dt)
        {
            TravelRequestedLastStep=false;TravelDistanceLastStep=0;
            if(AnimationPaused)return;
            if(player==null || route==null || route.Length==0){Animate(false,dt);return;}
            if(transform.position.y< -3){RecoverFromFall();Animate(false,dt);return;}
            UpdateResponseHeading();
            callCooldown=Mathf.Max(0,callCooldown-dt);lineTime-=dt;if(lineTime<=0)Line="";
            Vector3 requestedMove=Vector3.zero;
            Waiting=false;bool walking=false;CurrentMoment=Moment.None;
            if(respondRemaining>0 && !hasResponseHeading)
            {respondRemaining-=dt;Face(player.position,dt);CurrentMoment=Moment.Response;}
            else if(Vector3.Distance(player.position,transform.position)>waitDistance && !hasResponseHeading)
            {Waiting=true;Face(player.position,dt);}
            else if(!Finished)
            {
                var node=route[index];if(node==null){index++;return;}
                if(!atNode)
                {
                    var delta=node.transform.position-transform.position;delta.y=0;
                    var toPlayer=player.position-transform.position;toPlayer.y=0;
                    if(delta.magnitude<waypointTolerance){atNode=true;stopRemaining=node.pauseSeconds;}
                    else if(!TravelPaused && toPlayer.magnitude<personalSpace && Vector3.Dot(delta.normalized,toPlayer.normalized)>.25f)
                    {Waiting=true;Face(player.position,dt);}
                    else if(!TravelPaused)
                    {Face(node.transform.position,dt);requestedMove=Vector3.ClampMagnitude(delta,walkSpeed*dt);}
                }
                if(atNode)
                {
                    bool photoHold=node==photoHoldNode;
                    if(photoHold && photoLookBack)
                    {
                        UpdateResponseHeading();
                        transform.rotation=Quaternion.RotateTowards(transform.rotation,responseHeading,photoTurnSpeed*dt);
                        CurrentMoment=Moment.Response;
                    }
                    else
                    {
                        if(node.lookAt!=null)Face(node.lookAt.position,dt);
                        if(node.watersideMoment)CurrentMoment=Moment.Waterside;
                    }
                    if(!photoHold){stopRemaining-=dt;if(stopRemaining<=0){atNode=false;index++;}}
                }
            }
            else Face(player.position,dt);
            TravelRequestedLastStep=requestedMove.sqrMagnitude>0;
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
                TravelDistanceLastStep=travelled.magnitude;
                walking=travelled.sqrMagnitude>.000001f;
                if(walking)CurrentMoment=Moment.Walking;
                else if(requestedMove.sqrMagnitude>0)Waiting=true;
                if(transform.position.y< -3){RecoverFromFall();walking=false;}
            }
            Animate(walking,dt);
            if(walking){stepTime-=dt;if(stepTime<=0){stepTime=.48f;if(footsteps!=null && footstep!=null)footsteps.PlayOneShot(footstep,.3f);}}else stepTime=0;
        }
        void Animate(bool walking,float dt)
        {
            entranceShutterPose=Mathf.Max(0,entranceShutterPose-dt);
            var pose=photoHeadPivot!=null?photoHeadPivot:head;var rest=photoHeadPivot!=null?photoHeadHome:headHome;
            float yaw=0;
            if(photoLookBack && player!=null){var local=transform.InverseTransformDirection(player.position-transform.position);float limit=photoOfferHand?15:25;yaw=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-limit,limit);}
            if(pose!=null)
            {
                var target=rest*(entranceShutterPose>0?Quaternion.Euler(0,12,-7):Quaternion.Euler(0,yaw,0));
                pose.localRotation=entranceShutterPose>0?target:Quaternion.Slerp(pose.localRotation,target,1-Mathf.Exp(-dt*9));
            }
            hairResponse=Mathf.Lerp(hairResponse,entranceShutterPose>0?-4:-yaw*.18f,1-Mathf.Exp(-dt*3));
            if(photoHairPivot!=null)photoHairPivot.localRotation=Quaternion.Slerp(photoHairPivot.localRotation,photoHairHome*Quaternion.Euler(0,hairResponse,entranceShutterPose>0?3:0),1-Mathf.Exp(-dt*4));
            if(rightArm!=null)rightArm.localScale=rightArmScale;
            phase+=dt*(walking?7:2);float swing=walking?Mathf.Sin(phase)*25:0;
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,-7);
            if(rightArm!=null)rightArm.localRotation=respondRemaining>0?Quaternion.Euler(-25,0,115+Mathf.Sin(phase*4)*12):Quaternion.Euler(swing*.7f,0,7);
            if(photoOfferHand && rightArm!=null)
            {
                var direction=photoHandTarget-rightArm.position;
                if(direction.sqrMagnitude>.001f)
                {
                    rightArm.rotation=Quaternion.FromToRotation(Vector3.down,direction.normalized);
                    if(photoRightHand!=null)
                    {
                        float reach=Mathf.Max(.1f,Mathf.Abs(photoRightHand.localPosition.y));
                        rightArm.localScale=new Vector3(rightArmScale.x,Mathf.Clamp(direction.magnitude/reach,.8f,1.25f)*rightArmScale.y,rightArmScale.z);
                    }
                }
            }
            if(visual!=null)visual.localPosition=visualHome+Vector3.up*(walking?Mathf.Abs(Mathf.Sin(phase))*.025f:0);
            if(hasSpeed && animator!=null)animator.SetFloat("Speed",walking?walkSpeed:0,.18f,dt);
        }
    }
}
