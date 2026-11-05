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
        public bool GuidedPhotoAdjustment {get{guidedDocked=companion!=null && companion.AtPhotoNode && !guidedDeparturePending;return guidedInitialized && guidedDocked;}}
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
        // V3: the player chooses position with WASD, including before loading and
        // while waiting for the companion. Only normal scene collision limits motion.
        Vector3 GuidedDesiredVelocity(Vector3 manual,float dt)=>manual;
        Vector3 ConstrainGuidedMove(Vector3 displacement)=>displacement;
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
