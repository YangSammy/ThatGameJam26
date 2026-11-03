using UnityEditor;
using UnityEngine;
namespace MemoryWhitebox.Art.Editor
{
 [CustomEditor(typeof(AlbumWinterVisual))]
 public sealed class AlbumWinterVisualEditor : UnityEditor.Editor
 {
  public override void OnInspectorGUI(){
   var v=(AlbumWinterVisual)target;var s=v.Capture();EditorGUILayout.HelpBox("Manual presentation sampling. No gameplay, automatic clock or save writes.",MessageType.Info);EditorGUI.BeginChangeCheck();
   string n=v.name;
   if(n.Contains("Album_E01")||n.Contains("AlbumSheet")){s.open=EditorGUILayout.Slider("Open",s.open,0,1);s.turn=EditorGUILayout.Slider("Turn",s.turn,0,1);s.settle=EditorGUILayout.Slider("Settle",s.settle,0,1);s.pageTexture=(Texture)EditorGUILayout.ObjectField("Page texture",s.pageTexture,typeof(Texture),false);}
   if(n.Contains("Actor")){s.actorPose=EditorGUILayout.TextField("Action suffix",s.actorPose);s.actorProgress=EditorGUILayout.Slider("Action progress",s.actorProgress,0,1);s.facing=EditorGUILayout.Slider("Facing",s.facing,-180,180);}
   if(n.Contains("Swing"))s.swingRadians=EditorGUILayout.Slider("Swing radians",s.swingRadians,-1.13446f,1.13446f);
   if(n.Contains("Bridge"))s.bridge=EditorGUILayout.Slider("Bridge appearance",s.bridge,0,1);
   if(n.Contains("Counterweight"))s.load=EditorGUILayout.Slider("Load",s.load,0,1);
   if(n.Contains("DaisyWheel"))s.flowerRadians=EditorGUILayout.FloatField("Wheel radians (unbounded)",s.flowerRadians);
   if(n.Contains("Door")){s.insert=EditorGUILayout.Slider("Insert",s.insert,0,1);s.unlock=EditorGUILayout.Slider("Unlock",s.unlock,0,1);s.door=EditorGUILayout.Slider("Open",s.door,0,1);s.keyVisible=EditorGUILayout.Toggle("Nested key visible",s.keyVisible);}
   if(EditorGUI.EndChangeCheck()){Undo.RecordObject(v,"Sample album prop");v.Restore(s);EditorUtility.SetDirty(v);SceneView.RepaintAll();}
   if(GUILayout.Button("Restore default visual")){Undo.RecordObject(v,"Reset album prop");v.ResetVisual();EditorUtility.SetDirty(v);SceneView.RepaintAll();}
  }
 }
}
