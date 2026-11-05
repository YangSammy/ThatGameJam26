using MemoryWhitebox.Art;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        AlbumWinterVisual albumActor;
        Transform actorSeatContact,actorUpperArm,actorForearm,actorHand,actorHandContact,doorKeyVisual;
        float actorCycle,actorTransient,actorFacing=-90;
        string actorTransition="Idle";
        const float DoorInsertEnd=.65f,DoorUnlockEnd=1.0f,DoorPerformanceDuration=1.65f;
        float doorPerformanceTime=-1,pendantStallTime;
        public bool DoorPerformanceActive=>doorPerformanceTime>=0 && doorPerformanceTime<DoorPerformanceDuration;

        void BuildAlbumActor()
        {
            actor=new GameObject("album-person").transform;actor.SetParent(route,false);
            var art=PaintingVisualAssets.Load();
            if(art==null || art.actor==null)
                throw new System.InvalidOperationException("PaintingVisualAssets must reference PF_AlbumActor_E02.");
            var visual=VisualInstance(art.actor,actor,Vector3.zero,10);
            albumActor=visual.GetComponent<AlbumWinterVisual>();
            if(albumActor==null)throw new System.InvalidOperationException("Album actor is missing its authored pose sampler.");
            albumActor.SetActorPose("Idle",0,-90);
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="SeatContact")actorSeatContact=t;
                if(t.name=="RUpperArm")actorUpperArm=t;
                if(t.name=="RForearm")actorForearm=t;
                if(t.name=="RHand")actorHand=t;
                if(t.name=="HandContact_R")actorHandContact=t;
            }
        }

        void RestoreWinterPerformance()
        {
            // The accepted drop is already durable. Re-entering restores the final
            // door pose, never a second unlock animation or a second key sound.
            doorPerformanceTime=-1;actorTransient=actorCycle=pendantStallTime=0;
            ResetObservationPointer();
            if(albumActor!=null)albumActor.SetActorPose("Idle",0,actorFacing);
        }

        void BeginDoorPerformance()
        {
            doorPerformanceTime=0;StopWinterMotion();State.CancelInput();
            ResetObservationPointer();WinterShot(47);ApplyDoorPerformance();
        }

        void TickDoorPerformance(float dt)
        {
            float before=doorPerformanceTime;
            doorPerformanceTime=Mathf.Min(DoorPerformanceDuration,doorPerformanceTime+Mathf.Max(0,dt));
            if(before<DoorUnlockEnd && doorPerformanceTime>=DoorUnlockEnd)WinterShot(48);
            ApplyDoorPerformance();
            albumActor.SetActorPose("KeyReach",DoorReachWeight,-90);
            if(doorPerformanceTime>=DoorPerformanceDuration)
            {
                albumActor.SetActorPose("Idle",0,-90);
                State.CancelInput();Host?.InputContext.Handoff();Host?.SyncInputContext();
            }
        }

        float DoorReachWeight=>doorPerformanceTime<DoorInsertEnd?
            Mathf.Clamp01(doorPerformanceTime/DoorInsertEnd):
            1-Mathf.Clamp01((doorPerformanceTime-DoorInsertEnd)/(DoorUnlockEnd-DoorInsertEnd));

        void PlaceAlbumActor(bool second)
        {
            actor.localPosition=new Vector3(State.Actor.x,State.Actor.y,second?-1.2f:-.05f);
            actor.localRotation=Quaternion.identity;
            if(State.Riding>=0 && actorSeatContact!=null)
                actor.localPosition-=route.InverseTransformVector(actorSeatContact.position-actor.position);
            if(!DoorPerformanceActive || doorKeyVisual==null)return;
            float weight=Mathf.SmoothStep(0,1,DoorReachWeight);
            var socket=route.InverseTransformPoint(doorKeyVisual.position);
            actor.localPosition=Vector3.Lerp(actor.localPosition,
                new Vector3(socket.x-.17f,State.Actor.y,socket.z-.18f),weight);
            if(actorHandContact==null || actorHand==null || actorForearm==null || actorUpperArm==null)return;
            // Use the supplied skeletal contact markers to finish the short reach.
            // The gameplay foot point and collision model remain unchanged.
            Vector3 target=doorKeyVisual.TransformPoint(new Vector3(0,.02f,0));
            target=Vector3.Lerp(actorHandContact.position,target,weight);
            Vector3 wristTarget=target-(actorHandContact.position-actorHand.position);
            Vector3 shoulder=actorUpperArm.position;
            float upper=Vector3.Distance(shoulder,actorForearm.position);
            float lower=Vector3.Distance(actorForearm.position,actorHand.position);
            Vector3 direction=(wristTarget-shoulder).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(shoulder,wristTarget),Mathf.Abs(upper-lower)+.0001f,upper+lower-.0001f);
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.down,direction).normalized;
            Vector3 elbow=shoulder+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            actorUpperArm.rotation=Quaternion.FromToRotation(actorForearm.position-shoulder,elbow-shoulder)*actorUpperArm.rotation;
            actorForearm.rotation=Quaternion.FromToRotation(actorHand.position-actorForearm.position,wristTarget-actorForearm.position)*actorForearm.rotation;
        }

        void ApplyDoorPerformance()
        {
            if(!State.KeyInserted){winterDoor.SetDoorState(0,0,0,false);return;}
            float t=DoorPerformanceActive?doorPerformanceTime:DoorPerformanceDuration;
            winterDoor.SetDoorState(Mathf.SmoothStep(0,1,t/DoorInsertEnd),
                Mathf.SmoothStep(0,1,(t-DoorInsertEnd)/(DoorUnlockEnd-DoorInsertEnd)),
                Mathf.SmoothStep(0,1,(t-DoorUnlockEnd)/(DoorPerformanceDuration-DoorUnlockEnd)),true);
        }

        void TickAlbumActor(float dt,WinterAudioFrame before)
        {
            if(albumActor==null || State.Phase<2 && (!State.BookOpen || keyDragging))return;
            float moved=State.Actor.x-before.Actor.x;
            bool walking=Mathf.Abs(moved)>.0005f;
            if(walking)actorFacing=moved>0?-90:90;
            actorCycle=Mathf.Repeat(actorCycle+Mathf.Abs(moved)*1.8f,1);
            if(State.Phase>=2)
            {albumActor.SetActorPose(walking?"Walk":"Idle",walking?actorCycle:0,actorFacing);return;}
            if(before.Riding<0 && State.Riding>=0){actorTransient=.24f;actorTransition="Board";}
            else if(before.Riding>=0 && State.Riding<0){actorTransient=.2f;actorTransition="Dismount";}
            else if(before.Grounded && !State.Grounded){actorTransient=.18f;actorTransition="Takeoff";}
            else if(!before.Grounded && State.Grounded){actorTransient=.15f;actorTransition="Land";}
            if(actorTransient>0)
            {
                actorTransient=Mathf.Max(0,actorTransient-dt);
                albumActor.SetActorPose(actorTransition,1-actorTransient/.24f,actorFacing);return;
            }
            string pose=State.Riding==0?"SitSwing":State.Riding>0?"RideFlower":
                !State.Grounded?"Air":Mathf.Abs(State.CrateX-before.CrateX)>.0005f?"Push":walking?"Walk":"Idle";
            albumActor.SetActorPose(pose,pose=="Walk" || pose=="Push"?actorCycle:.5f,actorFacing);
        }

        void TickPendantHint(float dt)
        {
            if(State.Phase!=1 || State.Painted[4] || State.KeyInserted || State.PendantHintPlayed)return;
            if(!State.AtDoor || State.BookOpen){pendantStallTime=0;return;}
            pendantStallTime+=Mathf.Max(0,dt);
            if(pendantStallTime<7)return;
            State.PendantHintPlayed=true;
            // An in-world chain sound points back into the observed room; focus
            // confirmation remains 46/146 and does not consume this discovery cue.
            StorySfx.Shot(246,layout.observedPendant.position);
            Host?.SaveStableProgress();
        }
    }
}
