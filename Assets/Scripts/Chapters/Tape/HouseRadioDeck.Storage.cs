using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Tooltip("Desk landing poses: three originals followed by five work tapes. Storage homes remain in the box.")]
        public Transform[] storageDeskLandings=new Transform[3+RadioTapeChapter.WorkTapeCount];
        Transform storageDelivery;
        Vector3 deliveryStart,deliveryEnd;
        Quaternion deliveryStartRotation,deliveryEndRotation;
        float deliveryTime;
        int deliveryCandidate=-1;

        bool OverStorageFootprint(Transform item)
        {
            if(item==null || workbenchVisuals==null || workbenchVisuals.storageFloor==null)return false;
            var box=workbenchVisuals.storageFloor.bounds;
            Bounds tape=new Bounds(item.position,Vector3.zero);
            foreach(var r in item.GetComponentsInChildren<Renderer>())
                if(r.enabled && !(r is LineRenderer))tape.Encapsulate(r.bounds);
            return tape.max.x>=box.min.x-.01f && tape.min.x<=box.max.x+.01f &&
                tape.max.z>=box.min.z-.01f && tape.min.z<=box.max.z+.01f;
        }
        void KeepDraggedTapeAboveStorage(Transform item)
        {
            if(!OverStorageFootprint(item))return;
            var box=workbenchVisuals.storageFloor.bounds;
            // The floor has no solid navigation collider. Keep the whole cassette
            // above the compartment walls; storage acceptance remains explicit.
            float bottom=item.position.y;
            foreach(var r in item.GetComponentsInChildren<Renderer>())
                if(r.enabled && !(r is LineRenderer))bottom=Mathf.Min(bottom,r.bounds.min.y);
            float top=box.max.y+.12f;
            if(bottom<top)item.position+=Vector3.up*(top-bottom);
        }

        bool TryDeliverStoredTape(Collider hit)
        {
            Transform item=null;int landing=-1;
            for(int i=0;originals!=null && i<originals.Length;i++)
                if(hit==originals[i].hit){item=originals[i].tape;landing=i;break;}
            int candidate=CandidateForHit(hit);
            if(candidate>=0){item=tapes[candidate].root;landing=3+candidate;}
            if(item==null || !workbenchVisuals.InStorage(item))return false;
            if(StorageClosed(item)){ShowTapePointerNotice("Open the storage box first");return true;}
            if(candidate>=0)
            {
                var reason=WorkTapeLockReason(candidate);
                if(reason!=null){ShowTapePointerNotice(reason);return true;}
                if(workAnchor==null || !PreparationAvailable(item))
                {ShowTapePointerNotice("Clear the preparation mat first");return true;}
            }
            else if(Chapter.Completed || landing!=CurrentCopy || Chapter.SourceLoaded)
            {ShowTapePointerNotice(SourceHint(hit));return true;}
            if(candidate<0 && (landing>=storageDeskLandings.Length || storageDeskLandings[landing]==null))
            {ShowTapePointerNotice("Desk landing is not assigned");return true;}
            if(candidate>=0 || (!WorkbenchMode && !RecordingMode))OpenWorkbench();
            storageDelivery=item;deliveryStart=item.position;deliveryStartRotation=item.rotation;
            deliveryCandidate=candidate;
            var pose=candidate>=0?workAnchor:storageDeskLandings[landing];deliveryEnd=pose.position;deliveryEndRotation=pose.rotation;deliveryTime=0;
            TapeSound(5,item.position);
            return true;
        }
        void TickStorageDelivery(float dt)
        {
            if(storageDelivery==null)return;
            if(!StationActive || !focused || house==null || house.safeInspection.BlocksInput)
            {CancelStorageDelivery();return;}
            deliveryTime+=dt;
            float t=Mathf.Clamp01(deliveryTime/.55f),ease=Mathf.SmoothStep(0,1,t);
            storageDelivery.SetPositionAndRotation(Vector3.Lerp(deliveryStart,deliveryEnd,ease)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.12f),Quaternion.Slerp(deliveryStartRotation,deliveryEndRotation,ease));
            if(t<1)return;
            if(deliveryCandidate>=0)
            {
                int previous=Chapter.WorkbenchTapeIndex;
                Chapter.WorkbenchTapeIndex=deliveryCandidate;
                // Remove the box close-up before the preparation camera captures its return pose.
                RestoreRadioCamera();
                if(!PlaceTapeOnPreparationMat(false))
                {Chapter.WorkbenchTapeIndex=previous;CancelStorageDelivery();return;}
                storageDelivery=null;deliveryCandidate=-1;
                house.SaveStableProgress();benchWaiting=true;benchGate.Arm();return;
            }
            var item=storageDelivery;
            var area=OperationArea.For(item);
            if(area==null || !area.Place(item))
            {CancelStorageDelivery();ShowTapePointerNotice("Clear this tape's desk landing first");return;}
            storageDelivery=null;
            house.SaveStableProgress();benchWaiting=true;benchGate.Arm();
            if(radioFocused && activeCloseView==storageCloseView)FocusCloseView(false,storageCloseView);
        }
        void CancelStorageDelivery()
        {
            if(storageDelivery==null)return;
            storageDelivery.SetPositionAndRotation(deliveryStart,deliveryStartRotation);storageDelivery=null;deliveryCandidate=-1;
        }
        bool TryDeskLanding(Transform item,OperationArea area,Vector3 position,Quaternion rotation)
        {
            item.SetPositionAndRotation(position,rotation);
            Vector3 constrained;bool edge;
            if(!area.Constrain(item,position,0,out constrained,out edge))return false;
            item.position=constrained;
            // The box floor intentionally has no solid collider. Test its footprint
            // after area clamping, before Place can save a pose underneath it.
            if(OverStorageFootprint(item))return false;
            return area.Place(item);
        }
        void ReturnTapeToDesk(Transform item,int landing)
        {
            var position=item.position;var rotation=item.rotation;
            var area=OperationArea.For(item);
            if(area!=null)
            {
                Physics.SyncTransforms();
                var pose=landing>=0 && landing<storageDeskLandings.Length?storageDeskLandings[landing]:null;
                var preferred=pose!=null?pose.position:workAnchor!=null?workAnchor.position:area.Surface;
                var facing=pose!=null?pose.rotation:workAnchor!=null?workAnchor.rotation:rotation;
                if(TryDeskLanding(item,area,preferred,facing))return;
                // Use the closest clear point on this operation area. Never move
                // another cassette or accept the non-solid storage footprint.
                var points=new System.Collections.Generic.List<Vector3>();
                int columns=Mathf.Max(1,Mathf.CeilToInt(area.size.x/.06f));
                int rows=Mathf.Max(1,Mathf.CeilToInt(area.size.y/.06f));
                for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++)
                    points.Add(area.transform.TransformPoint(new Vector3(
                        area.center.x-area.size.x*.5f+area.size.x*x/columns,
                        area.surfaceHeight,area.center.y-area.size.y*.5f+area.size.y*z/rows)));
                points.Sort((a,b)=>(a-preferred).sqrMagnitude.CompareTo((b-preferred).sqrMagnitude));
                foreach(var point in points)if(TryDeskLanding(item,area,point,facing))return;
            }
            item.SetPositionAndRotation(position,rotation);
            ShowTapePointerNotice("No clear desk landing. Tape returned to its box slot.");
            if(workbenchVisuals!=null && workbenchVisuals.InStorage(item) && !workbenchVisuals.LidOpen)
                ToggleStorageLidWithSound();
        }
    }
}
