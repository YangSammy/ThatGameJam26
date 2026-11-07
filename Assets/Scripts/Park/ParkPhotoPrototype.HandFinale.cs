using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        bool finishingHandPhoto,handPhotoReturnPending;
        ParkHandResponseView handResponseView;
        public bool KeepJoinedHandForTransition=>finishingHandPhoto;
        Vector3 OpticalViewPosition=>handResponseView!=null?handResponseView.RenderPosition:view.transform.position;
        void InitializeHandResponseView()
        {
            handResponseView=view.GetComponent<ParkHandResponseView>();
            if(handResponseView==null)handResponseView=view.gameObject.AddComponent<ParkHandResponseView>();
            handResponseView.owner=this;
        }
        void QueueHandPhotoReturn()
        {
            // Called only after persistence acknowledges the final frame. Keep the
            // existing offer/contact alive until the transition covers the scene.
            finishingHandPhoto=true;handPhotoReturnPending=true;
            moveVelocity=Vector3.zero;verticalSpeed=0;footsteps.Reset();
        }
        void TickHandPhotoReturn()
        {
            if(!handPhotoReturnPending)return;
            handPhotoReturnPending=false;ReturnToDarkroom();
        }
    }
}
