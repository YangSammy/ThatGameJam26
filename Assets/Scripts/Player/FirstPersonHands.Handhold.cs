using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class FirstPersonHands
    {
        // Transient presentation only: the film checkpoint remains the authority.
        enum HandholdPhase { Hidden, Reach, Close, Hold, Open, Withdraw }
        HandholdPhase handholdPhase;
        float handholdTime, handholdGrip, releaseGrip;
        Vector3 handholdStart, handholdWrist, releaseWrist;
        Quaternion handholdStartRotation, handholdRotation, releaseRotation;
        Transform[,] handholdFingers;
        Transform[] handholdThumb;
        const float ReachSeconds=.28f, CloseSeconds=.16f, OpenSeconds=.14f, WithdrawSeconds=.28f;
        bool HandholdVisible=>handholdPhase!=HandholdPhase.Hidden;
        bool HandholdAutomaticRelease=>HandholdVisible && park!=null && !park.HoldingPhotoHand && park.PhotoHandReleaseAllowed;
        public float PhotoHandGrip=>handholdGrip;
        bool HandholdViewAllowed=>park!=null && (park.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder ||
            park.PhotoMode==ParkPhotoPrototype.PhotographyMode.Exploration);

        void ResetHandhold()
        {handholdPhase=HandholdPhase.Hidden;handholdTime=handholdGrip=0;}

        Vector3 HandholdRest=>transform.TransformPoint(new Vector3(-.25f,-.57f,.34f));
        static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));

        void ApplyHandholdFingers()
        {
            if(!HandholdVisible || skeletalRig==null || !skeletalRig.Valid)return;
            PrepareHandClearance();
            var wrist=skeletalRig.wrists[0];
            if(handholdFingers==null)
            {
                handholdFingers=new Transform[4,4];handholdThumb=new Transform[4];
                var names=new[]{"f_index","f_middle","f_ring","f_pinky"};
                foreach(var bone in wrist.GetComponentsInChildren<Transform>())
                {
                    for(int finger=0;finger<4;finger++)for(int joint=0;joint<4;joint++)
                        if(bone.name==names[finger]+(joint==3?".03.L_end":".0"+(joint+1)+".L"))handholdFingers[finger,joint]=bone;
                    for(int joint=0;joint<4;joint++)if(bone.name=="thumb"+(joint==3?".03.L_end":".0"+(joint+1)+".L"))handholdThumb[joint]=bone;
                }
            }
            // The generic grab clip is for objects. Straighten the open fingers, then
            // curl them around the opposite palm rather than closing into a fist.
            for(int finger=0;finger<4;finger++)for(int joint=0;joint<3;joint++)
            {
                var bone=handholdFingers[finger,joint];var tip=handholdFingers[finger,joint+1];
                if(bone==null || tip==null)continue;
                float angle=(joint==0?12:joint==1?35:55)*handholdGrip*Mathf.Deg2Rad;
                var direction=wrist.up*Mathf.Cos(angle)-wrist.right*Mathf.Sin(angle)+wrist.forward*((1.5f-finger)*.065f*(1-handholdGrip));
                AimFingerWithClearance(bone,tip,wrist.up,direction);
            }
            // Distribute opposition over the thumb chain; a free CCD tip target can
            // fold the two distal joints back onto each other and create a ring.
            for(int joint=0;joint<3;joint++)
            {
                var bone=handholdThumb[joint];var tip=handholdThumb[joint+1];
                if(bone==null || tip==null)continue;
                var open=(wrist.up*.65f+wrist.forward*.75f).normalized;
                var closed=(wrist.up*.65f-wrist.forward*(.45f+joint*.12f)-wrist.right*.15f).normalized;
                AimFingerWithClearance(bone,tip,open,Vector3.Slerp(open,closed,handholdGrip));
            }
        }
        bool AnimateHandhold(float dt, bool blocked, out FirstPersonArmRig.Pose pose)
        {
            pose=default(FirstPersonArmRig.Pose);
            if(park==null || !park.isActiveAndEnabled || !HandholdViewAllowed)
            {ResetHandhold();return false;}
            bool joined=park.HoldingPhotoHand;
            if(!joined && !HandholdVisible)return false;
            if(park.companion==null){ResetHandhold();return false;}
            // Focus/input gates freeze a current gesture. They never finish it off-screen.
            if(HandholdAutomaticRelease)blocked=false;
            if(blocked || GamePauseMenu.BlocksGameplay)dt=0;
            dt=Mathf.Max(0,dt);
            Vector3 target=park.PhotoHandPosition;
            Quaternion rotation=Quaternion.LookRotation(park.companion.transform.forward,Vector3.up);
            var girl=park.companion.visualPresentation;
            if(girl!=null && girl.palmContact!=null)
            {
                var normal=girl.PalmNormal.normalized;
                // Both rigs use local Y along the fingers. Left palm faces local -X.
                // Approach from the lower left while keeping opposed palm normals.
                var entry=transform.TransformPoint(new Vector3(-.38f,-.42f,.12f));
                var fingers=Vector3.ProjectOnPlane(girl.PalmPosition-entry,normal).normalized;
                fingers=Vector3.Slerp(fingers,Vector3.ProjectOnPlane(-girl.rightHand.up,normal).normalized,.75f).normalized;
                if(fingers.sqrMagnitude<.01f)fingers=Vector3.ProjectOnPlane(-park.companion.transform.forward,normal).normalized;
                rotation=Quaternion.LookRotation(Vector3.Cross(normal,fingers),fingers);
                // Cover the open supporting palm from above, with room for the thumb.
                // These are bone anchors, with skin thickness between their centres.
                target=girl.PalmPosition+normal*.026f-fingers*.015f+rotation*Vector3.forward*.025f-rotation*Vector3.Scale(photoPalmLocal,skeletalRig.wrists[0].lossyScale);
            }
            if(joined && handholdPhase==HandholdPhase.Hidden && !blocked)
            {
                handholdStart=HandholdRest;
                handholdStartRotation=rotation*Quaternion.Euler(0,0,-12);
                handholdWrist=handholdStart;handholdRotation=handholdStartRotation;
                handholdGrip=0;handholdTime=0;handholdPhase=HandholdPhase.Reach;
            }
            if(!joined && HandholdVisible && handholdPhase!=HandholdPhase.Open && handholdPhase!=HandholdPhase.Withdraw && !blocked)
            {
                releaseWrist=handholdWrist;releaseRotation=handholdRotation;
                releaseGrip=handholdGrip;handholdTime=0;handholdPhase=HandholdPhase.Open;
            }
            // A new join during withdrawal continues from the current hand, without a pop.
            if(joined && (handholdPhase==HandholdPhase.Open || handholdPhase==HandholdPhase.Withdraw) && !blocked)
            {
                handholdStart=handholdWrist;handholdStartRotation=handholdRotation;
                handholdTime=0;handholdGrip=0;handholdPhase=HandholdPhase.Reach;
            }
            if(!HandholdVisible)return false;
            handholdTime+=dt;
            if(handholdPhase==HandholdPhase.Reach)
            {
                float t=Ease(handholdTime/ReachSeconds);
                var approachNormal=girl!=null?girl.PalmNormal:transform.up;
                handholdWrist=Vector3.Lerp(handholdStart,target,t)+approachNormal*(Mathf.Sin(t*Mathf.PI)*.12f);
                handholdRotation=Quaternion.Slerp(handholdStartRotation,rotation,t);handholdGrip=0;
                if(handholdTime>=ReachSeconds){handholdTime-=ReachSeconds;handholdPhase=HandholdPhase.Close;}
            }
            if(handholdPhase==HandholdPhase.Close || handholdPhase==HandholdPhase.Hold)
            {
                handholdWrist=target;handholdRotation=rotation;
                handholdGrip=handholdPhase==HandholdPhase.Hold?1:Ease(handholdTime/CloseSeconds);
                if(handholdTime>=CloseSeconds)handholdPhase=HandholdPhase.Hold;
            }
            if(handholdPhase==HandholdPhase.Open)
            {
                handholdWrist=releaseWrist;handholdRotation=releaseRotation;
                handholdGrip=releaseGrip*(1-Ease(handholdTime/OpenSeconds));
                if(handholdTime>=OpenSeconds){handholdTime-=OpenSeconds;handholdPhase=HandholdPhase.Withdraw;}
            }
            if(handholdPhase==HandholdPhase.Withdraw)
            {
                float t=Ease(handholdTime/WithdrawSeconds);
                handholdWrist=Vector3.Lerp(releaseWrist,HandholdRest,t);
                handholdRotation=Quaternion.Slerp(releaseRotation,releaseRotation*Quaternion.Euler(0,0,-12),t);
                handholdGrip=0;
                if(handholdTime>=WithdrawSeconds){ResetHandhold();return false;}
            }
            pose=FirstPersonArmRig.Pose.AtWrist(handholdWrist,handholdRotation,handholdGrip*.68f);
            pose.bodyReachLimit=.6f;
            pose.alignForearmToPalm=girl!=null && girl.palmContact!=null;
            // Fallback without a girl rig retains the existing elbow solve.
            pose.overrideElbowDirection=true;
            pose.elbowDirection=new Vector3(.08f,-1,-.12f);
            return true;
        }
    }
}
