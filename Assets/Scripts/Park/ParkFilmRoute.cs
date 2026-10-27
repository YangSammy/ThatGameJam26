using UnityEngine;

namespace MemoryWhitebox
{
    // Only transports the entry intent. Film identity and progress always come
    // from disk, so a scene unload or application restart cannot lose the roll.
    public static class ParkFilmRoute
    {
        public const string HouseScenePath="Assets/Scenes/HouseScene.unity";
        public const string ParkScenePath="Assets/Scenes/ParkScene.unity";
        public static bool EnterDarkroomOnLoad;
        static bool hasReturnPose;
        static Vector3 returnPosition;
        static Quaternion returnRotation;
        static float returnFov;
        public static void RememberHouseView(Vector3 position,Quaternion rotation,float fov)
        {
            returnPosition=position;returnRotation=rotation;
            returnFov=fov;hasReturnPose=true;
        }
        public static void RestoreHouseView(Camera view)
        {
            if(!hasReturnPose)return;
            view.transform.SetPositionAndRotation(returnPosition,returnRotation);view.fieldOfView=returnFov;
            hasReturnPose=false;
        }
        public static void ForgetHouseView(){hasReturnPose=false;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){EnterDarkroomOnLoad=false;hasReturnPose=false;}
    }
}
