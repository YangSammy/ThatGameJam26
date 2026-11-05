using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        Transform reachUpper,reachLower,reachPalm;
        AutumnFarewellVisual farewellVisual;
        bool pickupIntent;
        float reachWeight,farewellSeconds,suitcaseDistance;
        readonly Vector3[] pickupStart=new Vector3[4];
        Vector3 suitcaseLast;
        bool suitcasePlaced;
        Transform suitcaseGrip;

        void BuildAutumnPerformance()
        {
            if(visualAssets.farewell==null)throw new System.InvalidOperationException("Autumn farewell visual is not bound.");
            HideMesh(npc);
            farewellVisual=Object.Instantiate(visualAssets.farewell,npc,false);
            farewellVisual.transform.localScale=Vector3.one*.8f;
            farewellVisual.transform.localPosition=Vector3.zero;
            farewellVisual.transform.localRotation=Quaternion.identity;
            SetLayer(farewellVisual.transform,MemoryLayer);
            farewellVisual.Present(0,0,false);
            var rim=new GameObject("Farewell doorway rim").transform;rim.SetParent(memory,false);
            rim.localPosition=new Vector3(3.3f,1.35f,-.55f);
            var light=rim.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=2.5f;
            light.intensity=2.2f;light.color=new Color(1,.88f,.7f);light.cullingMask=1<<MemoryLayer;
            foreach(var t in inventoryProps[2].GetComponentsInChildren<Transform>(true))
                if(t.name=="CarryGripAnchor"){suitcaseGrip=t;break;}
            foreach(Transform child in player)
                if(!child.name.StartsWith("held-"))HideMesh(child);
            var actor=CreateVisual(visualAssets.memoryActor,player);
            // E02 FacingAnchor/toes point along -Z; memory travel uses +Z.
            FitVisual(actor,new Vector3(.55f,1.06f,.42f),new Vector3(0,.53f,0),new Vector3(0,180,0));
            memoryActor=actor.GetComponent<MemoryWhitebox.Art.AlbumWinterVisual>();
            // The 180-degree source adapter puts the authored L arm on player-local +X,
            // beside the luggage handle; use that arm instead of reaching across the chest.
            foreach(var t in actor.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="LUpperArm")reachUpper=t;
                if(t.name=="LForearm")reachLower=t;
                if(t.name=="LHand")reachPalm=t;
            }
            if(memoryActor==null || reachUpper==null || reachLower==null || reachPalm==null)
                throw new System.InvalidOperationException("Autumn actor requires the authored E02 mesh and arm bones.");
            BuildMemoryRestoration();
        }

        static void AimBone(Transform bone,Transform child,Vector3 target)
        {
            var from=child.position-bone.position;var to=target-bone.position;
            if(from.sqrMagnitude>.000001f && to.sqrMagnitude>.000001f)
                bone.rotation=Quaternion.FromToRotation(from,to)*bone.rotation;
        }

        void UpdateCarryPresentation(float dt,bool restoring)
        {
            if(reachPalm==null)return;
            int target=-1;
            for(int i=0;i<4;i++)if(State.InPickupRange(i)){target=i;break;}
            bool preparing=pickupIntent && target>=0 && (State.Phase=="tutorial"||State.Phase=="collect");
            reachWeight=restoring?0:Mathf.MoveTowards(reachWeight,preparing?1:0,dt*6);
            var shoulder=reachUpper.position;
            var hand=reachPalm.position;
            bool contact=reachWeight>.001f;
            if(target>=0)
            {
                var destination=collectionProps[target].position;
                var offset=Vector3.ClampMagnitude(destination-shoulder,.51f);
                hand=Vector3.Lerp(hand,shoulder+offset,reachWeight);
            }
            for(int i=0;i<4;i++)
            {
                var held=inventoryProps[i];
                if(!State.HasItem(i)){held.gameObject.SetActive(false);continue;}
                float u=pickupProgress[i],ease=Mathf.SmoothStep(0,1,u);
                if(u==0)pickupStart[i]=collectionProps[i].position;
                Vector3 destination;
                Quaternion rotation=player.rotation;
                switch(i)
                {
                    case 0:
                        // Lift to the collar, then tuck under it. HasItem remains authoritative.
                        destination=player.TransformPoint(new Vector3(0,Mathf.Lerp(.91f,.72f,Mathf.InverseLerp(.65f,1,u)),.12f));
                        rotation*=Quaternion.Euler(90,0,0);break;
                    case 1:
                        destination=player.TransformPoint(new Vector3(-.25f,.58f,-.08f));break;
                    case 2:
                        destination=player.TransformPoint(new Vector3(.38f,.19f,-.35f));
                        rotation*=Quaternion.Euler(-15,0,0);break;
                    default:
                        destination=player.TransformPoint(new Vector3(-.15f,.60f,.14f));
                        rotation*=Quaternion.Euler(90,0,-15);break;
                }
                held.gameObject.SetActive(i!=0 || u<.98f);
                if(i==2 && u>=1)
                {
                    // Ground-following presentation, bounded near the hand even after sharp turns.
                    var previous=held.position;
                    held.position=restoring||!suitcasePlaced?destination:Vector3.Lerp(previous,destination,1-Mathf.Exp(-dt*12));
                    held.position=destination+Vector3.ClampMagnitude(held.position-destination,.12f);
                    float travel=suitcasePlaced && !restoring?Vector3.Distance(suitcaseLast,held.position):0;
                    suitcaseDistance+=travel;
                    if(travel>.002f && dt>0)StorySfx.Motion(234,MemorySoundPosition);
                    else StorySfx.Stop(234);
                    suitcaseLast=held.position;suitcasePlaced=true;
                    carriedVisuals[i].SetWheelProgress(Mathf.Repeat(suitcaseDistance/.18f,1));
                }
                else held.position=Vector3.Lerp(pickupStart[i],destination,restoring?1:ease)+Vector3.up*(restoring?0:Mathf.Sin(Mathf.PI*u)*.17f);
                held.rotation=rotation;
                if(u<.85f){hand=Vector3.Lerp(hand,held.position,Mathf.Sin(Mathf.PI*Mathf.Clamp01(u/.85f)));contact=true;}
            }
            if(!State.HasItem(2)){suitcasePlaced=false;suitcaseDistance=0;StorySfx.Stop(234);}
            if(State.HasItem(2) && pickupProgress[2]>=1 && reachWeight<.01f)
            {hand=suitcaseGrip!=null?suitcaseGrip.position:inventoryProps[2].TransformPoint(new Vector3(0,.19f,.08f));contact=true;}
            if(!contact)return;
            float upper=Vector3.Distance(shoulder,reachLower.position),lower=Vector3.Distance(reachLower.position,reachPalm.position);
            var direction=hand-shoulder;
            float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(upper-lower)+.001f,(upper+lower)*.995f);
            var delta=(direction.sqrMagnitude>.000001f?direction.normalized:-player.up)*distance;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            var bend=Vector3.ProjectOnPlane(-player.forward,delta.normalized).normalized;
            var elbow=shoulder+delta.normalized*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            AimBone(reachUpper,reachLower,elbow);AimBone(reachLower,reachPalm,shoulder+delta);
        }

        void CancelAutumnPresentation()
        {
            pickupIntent=false;reachWeight=0;
            StorySfx.Stop(234);missingGestureTime=0;
            if(memoryActor!=null)memoryActor.SetActorPose("Idle",0);
            for(int i=0;i<4;i++)pickupProgress[i]=State.HasItem(i)?1:0;
            UpdateCarryPresentation(0,true);
        }
    }
}
