using UnityEditor;
using UnityEngine;
namespace MemoryWhitebox.Art.Editor
{
    [CustomEditor(typeof(WritingPropVisual))]
    public sealed class WritingPropVisualEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Preview only. Sliders sample prop state; no game input, clock or saves. Use on the standalone preview scene or a prefab instance.",MessageType.Info);
            var v=(WritingPropVisual)target;var s=v.Capture();EditorGUI.BeginChangeCheck();
            s.crumple=EditorGUILayout.Slider("Crumple",s.crumple,0,1);
            s.chainPose=EditorGUILayout.Slider("Chain: table / hanging",s.chainPose,0,1);
            s.usePickupSettle=EditorGUILayout.Toggle("Use pickup settle",s.usePickupSettle);
            if(s.usePickupSettle)s.pickupSettle=EditorGUILayout.Slider("Pickup settle",s.pickupSettle,0,1);
            else s.sway=EditorGUILayout.Slider("Sway",s.sway,0,1);
            s.handle=EditorGUILayout.Slider("Handle",s.handle,0,1);s.wheel=EditorGUILayout.Slider("Wheel",s.wheel,0,1);
            s.flap=EditorGUILayout.Slider("Flap",s.flap,0,1);s.timer=EditorGUILayout.Slider("Elapsed fraction",s.timer,0,1);s.dial=EditorGUILayout.Slider("Dial",s.dial,0,1);
            if(EditorGUI.EndChangeCheck()){v.Restore(s);SceneView.RepaintAll();}
            if(GUILayout.Button("Reset visual pose")){v.ResetVisual();SceneView.RepaintAll();}
        }
    }
}
