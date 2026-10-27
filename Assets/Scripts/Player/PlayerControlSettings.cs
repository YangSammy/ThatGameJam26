using UnityEngine;

namespace MemoryWhitebox
{
    [CreateAssetMenu(menuName="游戏/统一第一人称控制设置")]
    public sealed class PlayerControlSettings : ScriptableObject
    {
        [Header("移动：书房与公园共用基础手感")]
        [Min(.1f),Tooltip("基础步行速度，米/秒。场景定位物体不受影响。")]
        public float walkSpeed=1.8f;
        [Min(.1f)] public float houseSpeedMultiplier=1;
        [Min(.1f)] public float parkSpeedMultiplier=1.5f;
        [Min(.1f)] public float aimingSpeedMultiplier=.61f;
        [Min(.1f)] public float acceleration=12;
        [Min(.1f)] public float deceleration=24;
        [Header("转头：共用鼠标灵敏度与俯仰范围")]
        [Range(.1f,8)] public float lookSensitivity=2.1f;
        [Range(.1f,1)] public float aimingLookMultiplier=.65f;
        [Range(0,89)] public float lookUpLimit=75;
        [Range(0,89)] public float lookDownLimit=75;
        [Header("公园镜头：出生位置与朝向仍读取场景定位物体")]
        [Min(.1f)] public float eyeHeight=1.65f;
        [Range(20,100)] public float parkWalkingFov=65;
        [Range(20,100)] public float parkAimingFov=48;
        [Min(.01f)] public float aimTransitionSeconds=.25f;
        [Header("关卡镜头：目标姿态仍读取原操作镜头")]
        [Min(.1f)] public float stationTransitionSeconds=.6f;
        [Tooltip("不同操作空间在全黑中切换；关闭则直接平滑移动，需自行确保路径无遮挡。")]
        public bool fadeBetweenStudyStations=true;
        static PlayerControlSettings current;
        public static PlayerControlSettings Current
        {
            get
            {
                if(current==null){current=Resources.Load<PlayerControlSettings>("PlayerControlSettings");
                    if(current==null){current=CreateInstance<PlayerControlSettings>();current.hideFlags=HideFlags.HideAndDontSave;}}
                return current;
            }
        }
        public Vector3 ApproachVelocity(Vector3 velocity,Vector3 wanted,float dt)
        {
            bool slowing=wanted.sqrMagnitude<velocity.sqrMagnitude || Vector3.Dot(velocity,wanted)<0;
            return Vector3.MoveTowards(velocity,wanted,Mathf.Max(.1f,slowing?deceleration:acceleration)*Mathf.Max(0,dt));
        }
        public float ClampPitch(float pitch)=>Mathf.Clamp(pitch,-lookUpLimit,lookDownLimit);
    }
}
