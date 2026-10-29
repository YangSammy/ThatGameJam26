using UnityEngine;

namespace MemoryWhitebox
{
    // Standalone art navigation: no chapter, interaction, save or gameplay input dependencies.
    [RequireComponent(typeof(Camera))]
    public sealed class ParkArtPreviewCamera : MonoBehaviour
    {
        [Min(.1f)] public float moveSpeed=5;
        [Min(1)] public float fastMultiplier=3;
        [Range(.1f,5)] public float lookSensitivity=1.6f;
        float yaw,pitch;
        bool looking,focused=true,waitForRelease;

        void OnEnable()
        {
            yaw=transform.eulerAngles.y;pitch=Mathf.DeltaAngle(0,transform.eulerAngles.x);
            waitForRelease=true;
        }
        void Update()
        {
            if(!focused)return;
            if(Input.GetKeyDown(KeyCode.Escape)){ReleaseMouse();waitForRelease=true;return;}
            if(waitForRelease){if(!Input.GetMouseButton(1))waitForRelease=false;return;}
            bool held=Input.GetMouseButton(1);
            if(!held){ReleaseMouse();return;}
            if(!looking){looking=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;return;}
            yaw+=Input.GetAxisRaw("Mouse X")*lookSensitivity;
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*lookSensitivity,-89,89);
            transform.rotation=Quaternion.Euler(pitch,yaw,0);
            float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            float z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            float y=(Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0);
            var direction=Vector3.ClampMagnitude(transform.right*x+transform.forward*z+Vector3.up*y,1);
            float speed=moveSpeed*(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?fastMultiplier:1);
            transform.position+=direction*speed*Time.unscaledDeltaTime;
        }
        void ReleaseMouse()
        {
            if(!looking)return;
            looking=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void OnApplicationFocus(bool value){focused=value;if(!value){ReleaseMouse();waitForRelease=true;}}
        void OnApplicationPause(bool value){if(value){ReleaseMouse();waitForRelease=true;}}
        void OnDisable(){ReleaseMouse();}
    }
}
