using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        readonly ParkWalkStallDetector walkStall=new ParkWalkStallDetector();
        Vector3 safetyPlayer;
        bool safetySampled,needsWalkRecovery,recoverCurrentPhoto;
        float walkRetryHold,recoveryFade=-1,healthyTravel;
        int walkRetries;
        bool GuidedRecoveryBusy=>needsWalkRecovery || walkRetryHold>0 || recoveryFade>=0;
        void TickGuidedSafety(float dt,Vector3 requested)
        {
            if(!safetySampled){safetyPlayer=transform.position;safetySampled=true;return;}
            var playerDelta=transform.position-safetyPlayer;playerDelta.y=0;
            safetyPlayer=transform.position;
            if(!GuidedCanTravel || GuidedPhotoAdjustment){walkStall.Reset();return;}
            healthyTravel+=playerDelta.magnitude;
            if(healthyTravel>=3){walkRetries=0;healthyTravel=0;}
            // Read intent and progress from the same companion step, independent of
            // Update ordering. Scenic/photo stops and yielding never request travel.
            bool girlExpected=companion.TravelRequestedLastStep && !companion.Paused && !companion.TravelPaused;
            if(!walkStall.Tick(false,girlExpected,playerDelta.magnitude,companion.TravelDistanceLastStep,dt))return;
            walkStall.Reset();moveVelocity=Vector3.zero;walkRetries++;
            if(walkRetries==1){walkRetryHold=.5f;Notify("Path blocked. Taking a moment to continue.");}
            else if(walkRetries==2){needsWalkRecovery=true;InputContext.Handoff();}
            else{needsWalkRecovery=true;InputContext.Handoff();}
        }
        bool TickGuidedRecovery(float dt)
        {
            if(needsWalkRecovery)return true;
            if(walkRetryHold>0){walkRetryHold=Mathf.Max(0,walkRetryHold-dt);return true;}
            if(recoveryFade<0)return false;
            float previous=recoveryFade;recoveryFade+=dt;
            if(previous<.18f && recoveryFade>=.18f)
            {
                if(recoverCurrentPhoto)RecoverGuidedWalk();
                else if(guidedPath.Count>0)
                {
                    guidedPathIndex=Mathf.Max(0,guidedPathIndex-1);
                    motor.enabled=false;transform.position=guidedPath[guidedPathIndex];motor.enabled=true;
                    verticalSpeed=0;moveVelocity=Vector3.zero;
                }
                walkStall.Reset();safetySampled=false;
                recoveryFade=.18f; // one fully black frame covers relocation before fading in
            }
            if(recoveryFade>=.4f)recoveryFade=-1;
            return true;
        }
        void DrawGuidedRecovery(GUIStyle text,GUIStyle button)
        {
            if(needsWalkRecovery && !Paused)
            {
                var rect=new Rect(Screen.width*.5f-180,Screen.height*.5f-70,360,135);
                GUI.Box(rect,"");InteractionUI.Label(new Rect(rect.x+18,rect.y+12,324,50),"The path is blocked. Continue from the photo spot; film progress is kept.",text);
                if(InputContext.Ready && InteractionUI.Button(new Rect(rect.x+30,rect.y+78,300,40),"Return to current photo spot",button))
                {needsWalkRecovery=false;recoverCurrentPhoto=true;recoveryFade=0;InputContext.Handoff();}
            }
            if(recoveryFade>=0 && !Paused)
            {
                float alpha=recoveryFade<.18f?Mathf.Clamp01(recoveryFade/.18f):1-Mathf.Clamp01((recoveryFade-.18f)/.22f);
                var old=GUI.color;GUI.color=new Color(0,0,0,alpha);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            }
        }
        static Vector3 SoftenPhotoBoundary(Vector3 position,Vector3 velocity,Vector3 centre,Quaternion rotation,bool bridge)
        {
            var local=Quaternion.Inverse(rotation)*(position-centre);
            var move=Quaternion.Inverse(rotation)*velocity;
            float width=bridge?.55f:1.1f,depth=bridge?1:1.1f;
            float radius=Mathf.Sqrt(local.x*local.x/(width*width)+local.z*local.z/(depth*depth));
            var normal=new Vector3(local.x/(width*width),0,local.z/(depth*depth)).normalized;
            float outward=Vector3.Dot(move,normal);
            if(outward>0)
            {
                float remaining=(1-radius)*Mathf.Min(width,depth);
                float scale=Mathf.SmoothStep(0,1,Mathf.Clamp01(remaining/.2f));
                move-=normal*outward*(1-scale);
            }
            return rotation*move;
        }
    }
}
