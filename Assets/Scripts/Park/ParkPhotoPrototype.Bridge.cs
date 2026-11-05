using System;
using UnityEngine;

namespace MemoryWhitebox
{
    [Serializable] public sealed class BridgePhotoCheckpoint
    {
        public bool hasReference,aligned,captured,handJoined;
        public Vector3 position,euler,subjectPoint;
        public float fov=48,ev100=13;
        public float savedFocus,savedAperture,savedShutter;
    }
    public sealed partial class ParkPhotoPrototype
    {
        [Header("Photography task alignment (all three frames)")]
        [Min(.1f)] public float bridgePositionTolerance=1f;
        [Range(1,30)] public float bridgeAngleTolerance=10;
        [Range(.1f,2)] public float bridgeExposureTolerance=1f;
        [Min(.1f)] public float bridgeAlignmentSeconds=.35f;
        ParkPhotoWaypoint bridgeNode;
        Texture2D bridgeReference;
        float alignedFor,referenceRetry;
        bool bridgeReady;
        bool HandOffered=>BridgePending && companion.AtPhotoNode && companion.photoOfferHand &&
            CurrentTaskId=="meadow" && CurrentTask?.aligned==true && !CurrentTask.handJoined;
        bool HandInReach=>Vector3.Distance(view.transform.position,PhotoHandPosition)<=2f;
        bool PhotoHandPointerActive=>Aiming && HandOffered && HandInReach && InView(PhotoHandPosition) && Visible(PhotoHandPosition);
        bool PointerOnHand=>PhotoHandPointerActive && PalmHit(view,PhotoHandPosition);
        float handJoinTransition;
        internal static bool PalmHit(Camera camera,Vector3 contact)
        {
            Vector3 screen=camera.WorldToScreenPoint(contact);
            return screen.z>0 && Vector2.Distance(screen,camera.pixelRect.center)<=PalmRadiusPixels(camera,contact);
        }
        internal static float PalmRadiusPixels(Camera camera,Vector3 contact)
        {
            Vector3 screen=camera.WorldToScreenPoint(contact);
            float shortSide=Mathf.Min(camera.pixelWidth,camera.pixelHeight);
            var edge=camera.WorldToScreenPoint(contact+camera.transform.up*.12f);
            return Mathf.Clamp(Vector2.Distance(screen,edge),shortSide*.015f,shortSide*.05f);
        }
        string CurrentTaskId=>roll!=null && roll.version>=3?roll.NextTask:"bridge";
        BridgePhotoCheckpoint CurrentTask=>roll!=null?roll.Task(CurrentTaskId):null;
        bool BridgePending=>bridgeNode!=null && roll!=null && roll.state==ParkFilmState.Loaded && (CurrentTask==null || !CurrentTask.captured);
        public bool HoldingPhotoHand=>BridgePending && CurrentTaskId=="meadow" && CurrentTask!=null && CurrentTask.handJoined && companion.AtPhotoNode && HandInReach;
        public Vector3 PhotoHandPosition=>bridgeNode!=null && bridgeNode.handContact!=null?bridgeNode.handContact.position:companion.chest.position+companion.transform.forward*.38f-companion.transform.right*.15f-Vector3.up*.18f;
        bool taskRoutingReady;
        void ResumeUnfinishedMemory()
        {
            if(roll==null || roll.state!=ParkFilmState.Loaded || !taskRoutingReady || bridgeNode==null)return;
            // First arrival keeps the original walk. Returning memories resume nearby,
            // without solving composition or consuming another exposure.
            if(!roll.HasPhotographyProgress)return;
            var point=bridgeNode.resumePoint;
            if(point==null){Debug.LogWarning("Photography resume anchor missing: "+bridgeNode.name,this);return;}
            motor.enabled=false;transform.SetPositionAndRotation(point.position,point.rotation);motor.enabled=true;
            yaw=point.eulerAngles.y;pitch=PlayerControlSettings.Current.ClampPitch(Mathf.DeltaAngle(0,point.eulerAngles.x));
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            verticalSpeed=0;moveVelocity=Vector3.zero;alignedFor=0;bridgeReady=false;
            guidedResumedAtPhoto=companion.ResumeAtPhotoNode(bridgeNode);
            handoff.Arm();inputGate=true;
        }
        public string PhotoRouteError {get;private set;}="";
        int FindTaskIndex(ParkPhotoWaypoint.PhotoTask task)
        {
            var route=companion!=null?companion.route:null;if(route==null)return -1;
            for(int i=0;i<route.Length;i++)if(route[i]!=null && route[i].photoTask==task)return i;
            int bridge=-1;for(int i=0;i<route.Length;i++)if(route[i]!=null && route[i].watersideMoment){bridge=i;break;}
            if(bridge<0)return -1;
            if(task==ParkPhotoWaypoint.PhotoTask.Bridge)return bridge;
            if(task==ParkPhotoWaypoint.PhotoTask.Entrance){for(int i=0;i<bridge;i++)if(route[i]!=null)return i;}
            else {for(int i=route.Length-1;i>bridge;i--)if(route[i]!=null)return i;}
            return -1;
        }
        public string CheckPhotographyRoute()
        {
            if(companion==null || companion.chest==null)return "Companion/chest reference is missing";
            int entrance=FindTaskIndex(ParkPhotoWaypoint.PhotoTask.Entrance),bridge=FindTaskIndex(ParkPhotoWaypoint.PhotoTask.Bridge),meadow=FindTaskIndex(ParkPhotoWaypoint.PhotoTask.Meadow);
            if(entrance<0 || bridge<0 || meadow<0)return "Assign entrance, bridge and meadow route points";
            if(!(entrance<bridge && bridge<meadow))return "Photography route must visit entrance, bridge, then meadow";
            foreach(int index in new[]{entrance,bridge,meadow})
            {
                var task=companion.route[index];
                if(task.referenceView==null || task.referencePhoto==null || task.lookAt==null)return "Photography task is missing its authored view, reference photo or look target: "+task.name;
            }
            var node=companion.route[meadow];
            if(node.handContact==null)return "Meadow hand contact is missing";
            if(node.referenceView!=null && node.handContact!=null && Vector3.Distance(node.referenceView.position,node.handContact.position)>2f)
                return "Meadow reference camera is beyond hand contact reach (2 metres)";
            return "";
        }

        void InitializeBridge()
        {
            obstructedFor=clearFor=palmBlockedFor=palmClearFor=0;subjectObstructionCue=palmObstructionCue=false;
            bridgeNode=null;taskRoutingReady=false;alignedFor=referenceRetry=0;
            companion.photoLookBack=false;companion.photoOfferHand=false;
            if(bridgeReference!=null){Destroy(bridgeReference);bridgeReference=null;}
            PhotoRouteError=roll!=null && roll.version>=3?CheckPhotographyRoute():"";
            if(PhotoRouteError!=""){Debug.LogWarning(PhotoRouteError,this);companion.photoHoldNode=null;return;}
            var route=companion.route;
            if(route!=null)
            {
                var wanted=CurrentTaskId=="entrance"?ParkPhotoWaypoint.PhotoTask.Entrance:CurrentTaskId=="meadow"?ParkPhotoWaypoint.PhotoTask.Meadow:ParkPhotoWaypoint.PhotoTask.Bridge;
                foreach(var node in route)if(node!=null && node.photoTask==wanted){bridgeNode=node;break;}
                int bridge=-1;for(int i=0;i<route.Length;i++)if(route[i]!=null && route[i].watersideMoment){bridge=i;break;}
                if(bridgeNode==null && bridge>=0)
                {
                    if(wanted==ParkPhotoWaypoint.PhotoTask.Bridge)bridgeNode=route[bridge];
                    else if(wanted==ParkPhotoWaypoint.PhotoTask.Entrance){for(int i=0;i<bridge;i++)if(route[i]!=null){bridgeNode=route[i];break;}}
                    else {for(int i=route.Length-1;i>bridge;i--)if(route[i]!=null){bridgeNode=route[i];break;}}
                }
            }
            if(bridgeNode==null){Debug.LogWarning("Photography task needs an authored route point: "+CurrentTaskId);companion.photoHoldNode=null;return;}
            taskRoutingReady=true;
            if(CurrentTask!=null)
            {
                if(CurrentTask.savedFocus>0)focusDistance=Mathf.Clamp(CurrentTask.savedFocus,minimumFocus,maximumFocus);
                int aperture=Array.IndexOf(apertures,CurrentTask.savedAperture),speed=Array.IndexOf(speeds,CurrentTask.savedShutter);
                if(aperture>=0)apertureIndex=aperture;if(speed>=0)speedIndex=speed;
                try{if(CurrentTask.hasReference){bridgeReference=new Texture2D(2,2);if(!bridgeReference.LoadImage(ParkFilmStore.ReadTaskReference(roll.id,CurrentTaskId)))throw new InvalidOperationException("Invalid reference image.");}}
                catch(Exception e){filmError="Reference unavailable";Debug.LogWarning(e.Message);}
            }
            companion.photoHoldNode=BridgePending?bridgeNode:null;
        }
        bool settingsSaveFailed;
        bool SavePhotoSettings()
        {
            if(leavingSavedOnly)return true;
            if(HasPendingExposure)return false;
            if(roll==null || filmError!="")return false;
            if(roll.state!=ParkFilmState.Loaded || (CurrentTask!=null && CurrentTask.captured))return true;
            if(CurrentTask!=null && CurrentTask.savedFocus==focusDistance && CurrentTask.savedAperture==Aperture && CurrentTask.savedShutter==ShutterSeconds)return true;
            var next=CurrentTask!=null?JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(CurrentTask)):new BridgePhotoCheckpoint();
            next.savedFocus=focusDistance;next.savedAperture=Aperture;next.savedShutter=ShutterSeconds;
            try{roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,next);settingsSaveFailed=false;return true;}
            catch(Exception e){settingsSaveFailed=true;Notify("Camera settings not saved. Stay here and retry.",6);Debug.LogWarning("Camera settings not saved: "+e.Message);return false;}
        }
        float TaskFocusDepth(BridgePhotoCheckpoint task)
        {
            Vector3 subject=CurrentTaskId=="meadow" && task.handJoined?companion.chest.position:task.subjectPoint;
            return Vector3.Dot(subject-view.transform.position,view.transform.forward);
        }
        bool ReferenceFramed(BridgePhotoCheckpoint task)=>Vector3.Distance(view.transform.position,task.position)<=bridgePositionTolerance &&
            Quaternion.Angle(view.transform.rotation,Quaternion.Euler(task.euler))<=bridgeAngleTolerance;
        bool InView(Vector3 point)
        {
            var p=view.WorldToViewportPoint(point);
            return p.z>0 && p.x>0 && p.x<1 && p.y>0 && p.y<1;
        }
        bool TaskFramed(BridgePhotoCheckpoint task)
        {
            if(CurrentTaskId!="meadow" || !task.handJoined)return ReferenceFramed(task);
            return HoldingPhotoHand && InView(companion.chest.position) && InView(PhotoHandPosition) && Visible(PhotoHandPosition);
        }
        void TickBridge(float dt)
        {
            bridgeReady=false;
            if(guidedDeparturePending)return;
            if(!BridgePending){companion.photoHoldNode=null;companion.photoLookBack=false;companion.photoOfferHand=false;return;}
            companion.photoHoldNode=bridgeNode;
            if(!companion.AtPhotoNode)return;
            if(CurrentTask==null || !CurrentTask.hasReference)
            {
                if(Time.unscaledTime<referenceRetry)return;
                referenceRetry=Time.unscaledTime+3;
                // Let the companion finish the reference orientation before freezing its reference.
                if(bridgeNode.lookAt!=null)
                {
                    Vector3 forward=bridgeNode.lookAt.position-companion.transform.position;forward.y=0;
                    if(forward.sqrMagnitude>.01f && Vector3.Angle(companion.transform.forward,forward)>5)return;
                }
                CaptureBridgeReference();return;
            }
            var reference=CurrentTask;
            UpdateRegionalMetering();
            bool focus=ParkCameraResponse.IsSharp(TaskFocusDepth(reference),focusDistance,Aperture);
            bool exposure=Mathf.Abs(ExposureError)<=bridgeExposureTolerance;
            bool visible=SubjectVisible();TickPhotoVisibility(visible,dt);
            bool match=Aiming && TaskFramed(reference) && focus && exposure && visible;
            if(!reference.aligned)
            {
                alignedFor=match?alignedFor+dt:0;
                if(alignedFor>=bridgeAlignmentSeconds)
                {
                    var next=JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(reference));next.aligned=true;
                    try{roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,next);}
                    catch(Exception e){Debug.LogWarning("Alignment not saved: "+e.Message);alignedFor=0;}
                    if(CurrentTask.aligned && CurrentTaskId!="entrance")StorySfx.Shot(36,companion.chest.position);
                    if(CurrentTaskId=="bridge" && CurrentTask.aligned)GameScore.Instance?.PhotoAnswered();
                }
            }
            companion.photoLookBack=CurrentTask.aligned && CurrentTaskId!="entrance";
            companion.photoOfferHand=CurrentTask.aligned && CurrentTaskId=="meadow";
            companion.photoHandTarget=PhotoHandPosition;
            Vector3 towardPlayer=view.transform.position-companion.transform.position;towardPlayer.y=0;
            // Recheck player settings/position, but never compare the turned pose to the old silhouette.
            bridgeReady=handJoinTransition<=0 && CurrentTask.aligned && match && (CurrentTaskId=="entrance" || Vector3.Angle(companion.transform.forward,towardPlayer)<10);
        }
        void CaptureBridgeReference()
        {
            var reference=new BridgePhotoCheckpoint{hasReference=true,ev100=bridgeNode.referenceEv100,subjectPoint=companion.chest.position,fov=PlayerControlSettings.Current.parkAimingFov,
                savedFocus=focusDistance,savedAperture=Aperture,savedShutter=ShutterSeconds};
            if(bridgeNode.referenceView!=null)
            {reference.position=bridgeNode.referenceView.position;reference.euler=bridgeNode.referenceView.eulerAngles;}
            else
            {
                // Temporary composition beside the existing path. The environment agent can replace it with an authored anchor.
                reference.position=companion.transform.position+(CurrentTaskId=="bridge"?-companion.transform.forward*3.5f+companion.transform.right*1.5f:companion.transform.forward*(CurrentTaskId=="meadow"?1.2f:3f))+Vector3.up*1.6f;
                reference.euler=Quaternion.LookRotation(reference.subjectPoint-reference.position).eulerAngles;
            }
            Vector3 position=view.transform.position;Quaternion rotation=view.transform.rotation;
            float fov=view.fieldOfView;int mask=view.cullingMask;var target=view.targetTexture;var active=RenderTexture.active;
            bool focus=focusEffect.activeFocus;RenderTexture temporary=null;Texture2D texture=null;
            try
            {
                if(bridgeNode.referencePhoto!=null)
                {
                    int height=Mathf.Max(1,Mathf.RoundToInt(960f*bridgeNode.referencePhoto.height/bridgeNode.referencePhoto.width));
                    temporary=RenderTexture.GetTemporary(960,height,0);Graphics.Blit(bridgeNode.referencePhoto,temporary);RenderTexture.active=temporary;
                    texture=new Texture2D(960,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,height),0,0);texture.Apply();
                }
                else
                {
                    temporary=RenderTexture.GetTemporary(960,Mathf.Max(1,Mathf.RoundToInt(960/view.aspect)),24);
                    view.transform.SetPositionAndRotation(reference.position,Quaternion.Euler(reference.euler));view.fieldOfView=reference.fov;
                    view.cullingMask&=~(1<<FirstPersonHands.HandLayer);focusEffect.activeFocus=false;view.targetTexture=temporary;view.Render();RenderTexture.active=temporary;
                    texture=new Texture2D(temporary.width,temporary.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,temporary.width,temporary.height),0,0);texture.Apply();
                }
                roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,reference,texture.EncodeToPNG());
                if(bridgeReference!=null)Destroy(bridgeReference);bridgeReference=texture;texture=null;
            }
            catch(Exception e){Debug.LogWarning("Reference not saved: "+e.Message);}
            finally
            {
                view.transform.SetPositionAndRotation(position,rotation);view.fieldOfView=fov;view.cullingMask=mask;view.targetTexture=target;RenderTexture.active=active;focusEffect.activeFocus=focus;
                if(temporary!=null)RenderTexture.ReleaseTemporary(temporary);if(texture!=null)Destroy(texture);
            }
        }
        bool TryJoinPhotoHand()
        {
            if(!PhotoHandPointerActive || !PointerOnHand)return false;
            var next=JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(CurrentTask));next.handJoined=true;
            try{roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,next);handJoinTransition=.45f;shotCooldown=.5f;GameScore.Instance?.PhotoAnswered();Notify("Hands joined. LMB to take the photograph. No frame used.");return true;}
            catch(Exception e){Notify("Hand response not saved. Click the offered hand to retry. No frame used.");Debug.LogWarning("Hand response not saved: "+e.Message);return true;}
        }
        string PhotographyNextAction()
        {
            if(filmError!="")return filmError;
            if(guidedDeparturePending)return "Photograph saved. Lower the camera to continue walking.";
            if(roll!=null && !roll.ReadyForPark)return OpeningPhotoInstruction();
            if(!string.IsNullOrEmpty(PhotoRouteError))return "Photo route unavailable. Film kept.";
            if(!companion.AtPhotoNode)return "Follow your companion to the next photo spot.";
            var task=CurrentTask;
            if(task==null || !task.hasReference)return "Wait for your companion to settle.";
            if(subjectObstructionCue)return "Your companion is blocked. Shift within the photo area.";
            if(palmObstructionCue)return "The offered hand is blocked. Shift to see the palm.";
            if(HandOffered)
            {
                if(!HandInReach)return "Step closer to the offered hand.";
                return PhotoHandPointerActive?"Aim the centre at the offered palm and click.":"Bring the offered hand into view.";
            }
            if(CurrentTaskId=="meadow" && task.handJoined)
            {
                if(!HandInReach)return "Step closer to your companion.";
                if(!TaskFramed(task))return "Frame your companion and your joined hands.";
            }
            else
            {
                if(Vector3.Distance(view.transform.position,task.position)>bridgePositionTolerance)return "Move to match the reference viewpoint.";
                if(!ReferenceFramed(task))return "Aim to match the reference framing.";
            }
            float depth=TaskFocusDepth(task);
            if(!ParkCameraResponse.IsSharp(depth,focusDistance,Aperture))return "Scroll to focus on your companion.";
            if(Mathf.Abs(ExposureError)>bridgeExposureTolerance)return ExposureError>0?
                "Too dark. Open the aperture or use a slower shutter.":
                "Too bright. Close the aperture or use a faster shutter.";
            if(!bridgeReady)return "Hold the camera steady. Wait for the response.";
            return "LMB: take the photograph.";
        }

        void DrawBridgeReference()
        {
            if(BridgePending && bridgeReference!=null)GUI.DrawTexture(new Rect(24,24,Screen.width*.22f,Screen.height*.22f),bridgeReference,ScaleMode.ScaleToFit);
        }
    }
}
