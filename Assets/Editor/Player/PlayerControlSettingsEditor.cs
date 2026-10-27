using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    [CustomEditor(typeof(PlayerControlSettings))]
    public sealed class PlayerControlSettingsEditor : UnityEditor.Editor
    {
        [MenuItem("Tools/玩家控制/统一 3C 设置")]
        static void Open()
        {
            var settings=AssetDatabase.LoadAssetAtPath<PlayerControlSettings>("Assets/Resources/PlayerControlSettings.asset");
            if(settings==null){Debug.LogWarning("请等待统一控制设置资源导入完成。");return;}
            Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);
        }
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("书房与公园共用这份设置。出生点和关卡操作镜头仍使用场景中的定位物体。旧控制器上的移动/转头参数已隐藏，不再作为这两个场景的运行参数。",MessageType.Info);
            serializedObject.Update();
            Field("walkSpeed","基础步行速度（米/秒）");
            Field("houseSpeedMultiplier","书房速度倍率");Field("parkSpeedMultiplier","公园速度倍率");
            Field("aimingSpeedMultiplier","公园取景速度倍率（相对基础速度）");
            Field("acceleration","起步加速度");Field("deceleration","停止与反向减速度");
            Field("lookSensitivity","共用转头灵敏度");Field("aimingLookMultiplier","取景转头倍率");
            Field("lookUpLimit","最大仰视角");Field("lookDownLimit","最大俯视角");
            Field("eyeHeight","公园眼睛高度（相对玩家根节点）");
            Field("parkWalkingFov","公园步行视野");Field("parkAimingFov","公园取景视野");
            Field("aimTransitionSeconds","举机视野与速度过渡（秒）");
            Field("stationTransitionSeconds","进入和退出关卡过渡（秒）");
            Field("fadeBetweenStudyStations","书房不同操作空间使用淡出淡入");
            serializedObject.ApplyModifiedProperties();
        }
        void Field(string name,string label){EditorGUILayout.PropertyField(serializedObject.FindProperty(name),new GUIContent(label));}
    }
}
