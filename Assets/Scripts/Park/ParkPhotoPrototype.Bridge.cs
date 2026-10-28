using System;
using UnityEngine;

namespace MemoryWhitebox
{
    [Serializable] public sealed class BridgePhotoCheckpoint
    {
        public bool hasReference,aligned,captured,handJoined;
        public Vector3 position,euler,subjectPoint;
        public float fov=48,ev100=13;
    }
    public sealed partial class ParkPhotoPrototype
    {
        [Header("Photography task alignment (all three frames)")]
        [Min(.1f)] public float bridgePositionTolerance=.6f;
        [Range(1,30)] public float bridgeAngleTolerance=6;
        [Range(.1f,2)] public float bridgeExposureTolerance=.7f;
        [Min(.1f)] public float bridgeAlignmentSeconds=.6f;
        ParkPhotoWaypoint bridgeNode;
        Texture2D bridgeReference;
        float alignedFor,referenceRetry;
        bool bridgeReady;
        string CurrentTaskId=>roll!=null && roll.version>=3?roll.NextTask:"bridge";
        BridgePhotoCheckpoint CurrentTask=>roll!=null?roll.Task(CurrentTaskId):null;
        bool BridgePending=>bridgeNode!=null && roll!=null && roll.state==ParkFilmState.Loaded && (CurrentTask==null || !CurrentTask.captured);
        public bool HoldingPhotoHand=>BridgePending && CurrentTaskId=="meadow" && CurrentTask!=null && CurrentTask.handJoined && companion.AtPhotoNode && Vector3.Distance(view.transform.position,PhotoHandPosition)<=2f;
        public Vector3 PhotoHandPosition=>bridgeNode!=null && bridgeNode.handContact!=null?bridgeNode.handContact.position:companion.chest.position+companion.transform.forward*.38f-companion.transform.right*.15f-Vector3.up*.18f;
        bool taskRoutingReady;

        void InitializeBridge()
        {
            bridgeNode=null;taskRoutingReady=false;alignedFor=referenceRetry=0;
            companion.photoLookBack=false;companion.photoOfferHand=false;
            if(bridgeReference!=null){Destroy(bridgeReference);bridgeReference=null;}
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
            if(CurrentTask!=null && CurrentTask.hasReference)
            {
                try{bridgeReference=new Texture2D(2,2);if(!bridgeReference.LoadImage(ParkFilmStore.ReadTaskReference(roll.id,CurrentTaskId)))throw new InvalidOperationException("Invalid reference image.");}
                catch(Exception e){filmError="Reference unavailable";Debug.LogWarning(e.Message);}
            }
            companion.photoHoldNode=BridgePending?bridgeNode:null;
        }
        void TickBridge(float dt)
        {
            bridgeReady=false;
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
            sceneEv100=reference.ev100;
            bool position=Vector3.Distance(view.transform.position,reference.position)<=bridgePositionTolerance;
            bool direction=Quaternion.Angle(view.transform.rotation,Quaternion.Euler(reference.euler))<=bridgeAngleTolerance;
            float depth=Vector3.Dot(reference.subjectPoint-view.transform.position,view.transform.forward);
            bool focus=depth>0 && Mathf.Abs(depth-focusDistance)<=Mathf.Max(.2f,depth*.065f);
            bool exposure=Mathf.Abs(ExposureError)<=bridgeExposureTolerance;
            bool match=Aiming && position && direction && focus && exposure;
            if(!reference.aligned)
            {
                alignedFor=match?alignedFor+dt:0;
                if(alignedFor>=bridgeAlignmentSeconds)
                {
                    var next=JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(reference));next.aligned=true;
                    try{roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,next);}
                    catch(Exception e){Debug.LogWarning("Alignment not saved: "+e.Message);alignedFor=0;}
                }
            }
            companion.photoLookBack=CurrentTask.aligned && CurrentTaskId!="entrance";
            companion.photoOfferHand=CurrentTask.aligned && CurrentTaskId=="meadow";
            companion.photoHandTarget=PhotoHandPosition;
            Vector3 towardPlayer=view.transform.position-companion.transform.position;towardPlayer.y=0;
            // Recheck player settings/position, but never compare the turned pose to the old silhouette.
            bridgeReady=CurrentTask.aligned && match && (CurrentTaskId=="entrance" || Vector3.Angle(companion.transform.forward,towardPlayer)<10) && Visible(companion.chest.position);
        }
        void CaptureBridgeReference()
        {
            var reference=new BridgePhotoCheckpoint{hasReference=true,ev100=bridgeNode.referenceEv100,subjectPoint=companion.chest.position,fov=PlayerControlSettings.Current.parkAimingFov};
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
                    temporary=RenderTexture.GetTemporary(960,600,0);Graphics.Blit(bridgeNode.referencePhoto,temporary);RenderTexture.active=temporary;
                    texture=new Texture2D(960,600,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,600),0,0);texture.Apply();
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
            if(!BridgePending || !companion.AtPhotoNode || CurrentTaskId!="meadow" || CurrentTask==null || !CurrentTask.aligned || CurrentTask.handJoined)return false;
            Vector3 screen=view.WorldToViewportPoint(PhotoHandPosition);
            if(screen.z<=0 || Vector2.Distance(new Vector2(screen.x,screen.y),virtualPointer)>.1f || Vector3.Distance(view.transform.position,PhotoHandPosition)>2f)return false;
            if(!Visible(PhotoHandPosition))return false;
            var next=JsonUtility.FromJson<BridgePhotoCheckpoint>(JsonUtility.ToJson(CurrentTask));next.handJoined=true;
            try{roll=ParkFilmStore.SaveTask(roll.id,CurrentTaskId,next);shotCooldown=.5f;return true;}
            catch(Exception e){Debug.LogWarning("Hand response not saved: "+e.Message);return true;}
        }
        void DrawBridgeReference()
        {
            if(BridgePending && bridgeReference!=null)GUI.DrawTexture(new Rect(24,24,Screen.width*.22f,Screen.height*.22f),bridgeReference,ScaleMode.ScaleToFit);
        }
    }
}
