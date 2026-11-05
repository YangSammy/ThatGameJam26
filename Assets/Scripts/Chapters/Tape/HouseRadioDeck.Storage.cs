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
            }
            else if(Chapter.Completed || landing!=CurrentCopy || Chapter.SourceLoaded)
            {ShowTapePointerNotice(SourceHint(hit));return true;}
            if(landing>=storageDeskLandings.Length || storageDeskLandings[landing]==null)
            {ShowTapePointerNotice("Desk landing is not assigned");return true;}
            if(!WorkbenchMode && !RecordingMode)OpenWorkbench();
            storageDelivery=item;deliveryStart=item.position;deliveryStartRotation=item.rotation;
            var pose=storageDeskLandings[landing];deliveryEnd=pose.position;deliveryEndRotation=pose.rotation;deliveryTime=0;
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
            storageDelivery.SetPositionAndRotation(deliveryStart,deliveryStartRotation);storageDelivery=null;
        }
        void ReturnTapeToDesk(Transform item,int landing)
        {
            if(landing<0 || landing>=storageDeskLandings.Length || storageDeskLandings[landing]==null)return;
            var pose=storageDeskLandings[landing];var position=item.position;var rotation=item.rotation;
            item.SetPositionAndRotation(pose.position,pose.rotation);
            var area=OperationArea.For(item);
            if(area==null || !area.Place(item))
            {item.SetPositionAndRotation(position,rotation);ShowTapePointerNotice("Desk landing blocked. Tape returned to its box slot.");}
        }
    }
}
