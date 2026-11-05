using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        readonly List<Vector3> guidedPath=new List<Vector3>();
        int guidedPathIndex;
        bool guidedInitialized,guidedDocked,guidedDeparturePending;
        bool guidedResumedAtPhoto;
        ParkPhotoWaypoint guidedDepartureNode;
        public bool GuidedDeparturePending=>guidedDeparturePending;
        public bool GuidedPhotoAdjustment=>guidedDocked && companion!=null && companion.AtPhotoNode && !guidedDeparturePending;
        bool GuidedCanTravel=>focused && !applicationPaused && !Paused && !changingScene && !inputGate &&
            InputContext.Ready && photoMode==PhotographyMode.Exploration && roll!=null && roll.ReadyForPark &&
            taskRoutingReady && filmError=="" && !HasPendingExposure && !guidedDeparturePending && !GuidedRecoveryBusy;
        Vector3 GuidedPhotoCentre=>bridgeNode.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;

        void InitializeGuidedWalk()
        {
            guidedInitialized=true;guidedDocked=false;guidedPathIndex=0;guidedPath.Clear();
            walkStall.Reset();safetySampled=false;
            if(bridgeNode==null || bridgeNode.referenceView==null || companion==null)return;
            var route=companion.route;
            int end=System.Array.IndexOf(route,bridgeNode),start=0;
            if(guidedDepartureNode!=null)start=Mathf.Max(0,System.Array.IndexOf(route,guidedDepartureNode));
            else if(companion.AtPhotoNode || guidedResumedAtPhoto)start=end; // includes a queued companion Start
            for(int i=start;i<end;i++)if(route[i]!=null)guidedPath.Add(route[i].transform.position);
            guidedPath.Add(GuidedPhotoCentre);
            guidedDepartureNode=null;
            guidedResumedAtPhoto=false;
        }
        void HoldGuidedDeparture()
        {
            guidedDeparturePending=true;guidedDepartureNode=bridgeNode;
            if(companion!=null)companion.TravelPaused=true;
            moveVelocity=Vector3.zero;
        }
        void TickLoweredDeparture()
        {if(photoMode==PhotographyMode.Exploration && !HasPendingExposure)ReleaseGuidedDeparture();}
        void ReleaseGuidedDeparture()
        {
            if(!guidedDeparturePending || Paused || !focused || applicationPaused || changingScene)return;
            guidedDeparturePending=false;InitializeBridge();InitializeGuidedWalk();
        }
        static Vector3 ClampGuidedPhotoPosition(Vector3 position,Vector3 centre,Quaternion rotation,bool bridge)
        {
            var local=Quaternion.Inverse(rotation)*(new Vector3(position.x,centre.y,position.z)-centre);
            float width=bridge?.55f:1.1f,depth=bridge?1:1.1f;
            var normalized=new Vector2(local.x/width,local.z/depth);
            if(normalized.sqrMagnitude>1)normalized.Normalize();
            var result=centre+rotation*new Vector3(normalized.x*width,0,normalized.y*depth);
            result.y=position.y;return result;
        }
        Vector3 GuidedBound(Vector3 position)=>ClampGuidedPhotoPosition(position,GuidedPhotoCentre,
            Quaternion.Euler(0,bridgeNode.referenceView.eulerAngles.y,0),bridgeNode.photoTask==ParkPhotoWaypoint.PhotoTask.Bridge);
        Vector3 GuidedDesiredVelocity(Vector3 manual,float dt)
        {
            if(!guidedInitialized || guidedPath.Count==0)InitializeGuidedWalk();
            if(bridgeNode==null || bridgeNode.referenceView==null || roll==null || !roll.ReadyForPark || guidedDeparturePending || HasPendingExposure)return Vector3.zero;
            if(GuidedPhotoAdjustment)
            {
                manual=SoftenPhotoBoundary(transform.position,manual,GuidedPhotoCentre,
                    Quaternion.Euler(0,bridgeNode.referenceView.eulerAngles.y,0),bridgeNode.photoTask==ParkPhotoWaypoint.PhotoTask.Bridge);
                var wanted=GuidedBound(transform.position+manual*dt)-transform.position;wanted.y=0;
                return wanted/Mathf.Max(.001f,dt);
            }
            if(!GuidedCanTravel)return Vector3.zero;
            while(guidedPathIndex<guidedPath.Count)
            {
                var delta=guidedPath[guidedPathIndex]-transform.position;delta.y=0;
                if(delta.magnitude<=.10f)
                {
                    if(guidedPathIndex==guidedPath.Count-1)
                    {if(companion.AtPhotoNode && !guidedDocked){guidedDocked=true;Notify("Photo spot. WASD: adjust position. E: camera.",4);}return Vector3.zero;}
                    guidedPathIndex++;continue;
                }
                float gap=Vector3.Distance(new Vector3(transform.position.x,0,transform.position.z),new Vector3(companion.transform.position.x,0,companion.transform.position.z));
                if(!companion.AtPhotoNode && gap<=2.5f)return Vector3.zero;
                float speed=companion.AtPhotoNode?1.1f:Mathf.Clamp((gap-2.5f)*.8f,0,companion.walkSpeed+.25f);
                return delta.normalized*Mathf.Min(speed,delta.magnitude/Mathf.Max(.001f,dt));
            }
            return Vector3.zero;
        }
        Vector3 ConstrainGuidedMove(Vector3 displacement)
        {
            if(!GuidedPhotoAdjustment)return displacement;
            var bounded=GuidedBound(transform.position+displacement)-transform.position;
            bounded.y=displacement.y;return bounded;
        }
        void RecoverGuidedWalk()
        {
            if(bridgeNode==null || bridgeNode.referenceView==null)return;
            var safe=bridgeNode.resumePoint;
            motor.enabled=false;transform.position=safe!=null?safe.position:GuidedPhotoCentre;motor.enabled=true;
            verticalSpeed=0;moveVelocity=Vector3.zero;
            guidedResumedAtPhoto=companion.ResumeAtPhotoNode(bridgeNode);
            InitializeGuidedWalk();
            Notify("Back at the photo spot. Film kept.");
        }
    }
}
