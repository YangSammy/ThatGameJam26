using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    [CustomEditor(typeof(SafeInspection))]
    public sealed class InspectionSettingsEditor : UnityEditor.Editor
    {
        bool showObjects;
        void Field(string name,string label,string description)
        {EditorGUILayout.PropertyField(serializedObject.FindProperty(name),new GUIContent(label,description));}
        void Section(string title)
        {EditorGUILayout.Space();EditorGUILayout.LabelField(title,EditorStyles.boldLabel);}
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("统一检视配置",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("全场景检视共用此配置。物品只单独调整初始朝向和观察中心。关卡操作镜头仍由 Scene View Anchors 下的定位物体控制。鼠标悬停在参数名称上可查看说明。",MessageType.Info);
            Section("拿到眼前：距离与构图");
            Field("inspectionFieldOfView","统一检视视野（度）","所有安全检视共用的镜头视野，不再继承进入前的视野；退出时恢复原视野，不改动关卡操作镜头定位物体。");
            Field("defaultDistance","默认检视距离（米）","进入安全检视及按重置后使用的距离，受最近和最远距离限制。");
            Field("minimumDistance","最近检视距离（米）","滚轮能靠近的最小距离。");
            Field("maximumDistance","最远检视距离（米）","滚轮能拉远的最大距离，不应小于最近距离。");
            Field("zoomStep","滚轮缩放步长","每格滚轮改变的距离；越大，拉近拉远越快。");
            Field("displaySize","统一展示尺寸（米）","每次进入检视时自动测量可见模型，并将最长边缩放到此尺寸。只改变展示副本，不改变真实物品尺寸。");
            Field("displayOffset","画面中心偏移（米）","镜头局部坐标：X 向右，Y 向上；所有可拿起的检视物品共用。");
            Section("检视动作");
            Field("rotationSensitivity","整体旋转灵敏度","左右键同时按住拖动时，每单位鼠标移动对应的旋转角度。");
            Field("approachDuration","进入检视时长（秒）","物品移到眼前或镜头靠近固定设备所需的时间。");
            Field("resetDuration","重置视角时长（秒）","重置角度和距离的过渡时间；不改变真实关卡状态。");
            Field("openPartClearance","打开部件避让距离（米）","盖子或书页打开时让展示物后移，减少穿过镜头的情况。");
            Section("实际手持：磁带关卡共用");
            EditorGUILayout.HelpBox("用于真实磁带拿取、检查与拖放；与 E 安全检视的展示副本分开。三盒磁带共用，无需逐盒设置。",MessageType.None);
            Field("heldTapePosition","手持位置（米）","相对玩家镜头：X 向右，Y 向上，Z 向前。拖动时 Z 决定鼠标前方的持物距离。");
            Field("heldTapeEulerAngles","手持角度（度）","相对镜头的欧拉角，调整真实磁带在手中的朝向。");
            Field("heldTapeMoveSpeed","手持移动速度（米/秒）","拿起和拖动时磁带追随目标位置的速度。");
            Field("heldTapeRotateSpeed","手持转动速度（度/秒）","磁带转向手持角度或卡槽朝向的速度。");
            serializedObject.ApplyModifiedProperties();
            Section("单件物品：朝向与中心");
            showObjects=EditorGUILayout.Foldout(showObjects,"选择要微调的物品",true);
            if(showObjects)
            {
                var viewer=(SafeInspection)target;
                if(viewer.subjects!=null)foreach(var subject in viewer.subjects)
                    if(subject!=null && GUILayout.Button(subject.title))Selection.activeGameObject=subject.gameObject;
            }
        }

        [MenuItem("Tools/检视/选择统一检视配置")]
        static void SelectSettings()
        {
            var viewer=Object.FindObjectOfType<SafeInspection>(true);
            if(viewer!=null){Selection.activeGameObject=viewer.gameObject;EditorGUIUtility.PingObject(viewer);}
        }
    }

    [CustomEditor(typeof(SafeInspectionSubject)),CanEditMultipleObjects]
    public sealed class InspectionSubjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("主要观察面由统一规则对准玩家，中心与尺寸自动计算。初始朝向为自动结果上的额外微调，通常保持零；无需因桌面摆放或拿取姿态变化重新调整。",MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayEuler"),new GUIContent("初始朝向微调（度）","在自动主要观察面基础上额外旋转。零值使用统一规则；不改变场景内物品或关卡操作镜头。"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("centerOffset"),new GUIContent("观察中心偏移（米）","相对自动模型中心的偏移，零值即自动居中；同时影响居中点和旋转中心。采用展示根节点局部轴向，不移动真实物品。"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Migrate serialized artist settings once, before the unified inspector can edit them.
    [InitializeOnLoad]
    public static class InspectionSettingsMigration
    {
        static InspectionSettingsMigration()
        {
            EditorApplication.delayCall+=MigrateOpenScenes;
            EditorSceneManager.sceneOpened+=(scene,mode)=>MigrateOpenScenes();
        }
        public static void MigrateOpenScenes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            foreach(var deck in Object.FindObjectsOfType<HouseRadioDeck>(true))
            {
                if(EditorUtility.IsPersistent(deck) || !deck.gameObject.scene.IsValid())continue;
                if(!deck.MigrateInspectionSettings())continue;
                EditorUtility.SetDirty(deck);EditorUtility.SetDirty(deck.inspectionSettings);
                EditorSceneManager.MarkSceneDirty(deck.gameObject.scene);
            }
        }
    }
}
