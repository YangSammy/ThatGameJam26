using UnityEngine;
namespace MemoryWhitebox
{
    // Render-only approach: gameplay/AI and the hand solver keep their original
    // camera reference. Only the optical viewpoint takes a small step forward.
    [RequireComponent(typeof(Camera))]
    public sealed class ParkHandResponseView : MonoBehaviour
    {
        public ParkPhotoPrototype owner;
        FirstPersonHands hands;
        bool applied;
        Vector3 cameraLocalPosition,rigLocalPosition;
        Quaternion cameraLocalRotation,rigLocalRotation;
        Transform armRoot;
        void Awake(){hands=GetComponent<FirstPersonHands>();}
        public float ReachProgress=>hands!=null?hands.PhotoHandReachProgress:0;
        Vector3 Offset
        {
            get
            {
                if(owner==null || !owner.HandResponseLocked)return Vector3.zero;
                float t=ReachProgress,ease=Mathf.SmoothStep(0,1,t);
                return transform.forward*(.10f*ease)+transform.up*(.003f*Mathf.Sin(t*Mathf.PI*2)*Mathf.Sin(t*Mathf.PI));
            }
        }
        public Vector3 RenderPosition=>applied?transform.position:transform.position+Offset;
        void OnPreCull()
        {
            if(applied || owner==null || !owner.HandResponseLocked)return;
            cameraLocalPosition=transform.localPosition;cameraLocalRotation=transform.localRotation;
            armRoot=hands!=null && hands.skeletalRig!=null?hands.skeletalRig.transform:null;
            Vector3 armPosition=Vector3.zero;Quaternion armRotation=Quaternion.identity;
            if(armRoot!=null){rigLocalPosition=armRoot.localPosition;rigLocalRotation=armRoot.localRotation;armPosition=armRoot.position;armRotation=armRoot.rotation;}
            float t=ReachProgress;var offset=Offset;
            transform.position+=offset;
            transform.rotation*=Quaternion.Euler(0,0,.2f*Mathf.Sin(t*Mathf.PI*2)*Mathf.Sin(t*Mathf.PI));
            if(armRoot!=null)armRoot.SetPositionAndRotation(armPosition,armRotation);
            applied=true;
        }
        void OnPostRender(){Restore();}
        void OnDisable(){Restore();}
        void Restore()
        {
            if(!applied)return;
            transform.localPosition=cameraLocalPosition;transform.localRotation=cameraLocalRotation;
            if(armRoot!=null){armRoot.localPosition=rigLocalPosition;armRoot.localRotation=rigLocalRotation;}
            applied=false;
        }
    }
}
