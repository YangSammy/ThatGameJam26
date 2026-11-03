using UnityEngine;

namespace MemoryWhitebox
{
    // Used only by the standalone preview scene. Production movement stays in ParkCompanion.
    public sealed class ParkGirlLocomotionPreview : MonoBehaviour
    {
        public ParkCompanion companion;
        public Camera reviewCamera;
        public float Elapsed {get;private set;}
        public bool RequestedWalking {get;private set;}
        void Awake(){companion.Paused=true;}
        void Update()
        {
            Elapsed+=Time.deltaTime;
            float phase=Elapsed%12;
            RequestedWalking=(phase>=2&&phase<6)||(phase>=8);
            companion.Paused=!RequestedWalking;
            companion.player.position=companion.transform.position-companion.transform.forward*3;
            if(companion.Finished)companion.RestartWalk();
        }
        void LateUpdate()
        {
            var target=companion.transform.position+Vector3.up*.85f;
            reviewCamera.transform.position=target+new Vector3(2.6f,.4f,3.7f);
            reviewCamera.transform.LookAt(target);
        }
        void OnGUI()
        {
            GUI.Label(new Rect(20,20,500,30),RequestedWalking?"WALK  /  CharacterController movement":"IDLE  /  paused movement");
        }
    }
}
