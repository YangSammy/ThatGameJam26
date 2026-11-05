using System;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class AutumnRestorationAuthoring
    {
        static GameObject Load(string path)
        {
            var value=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(value==null)throw new InvalidOperationException("Missing autumn source: "+path);
            return value;
        }

        public static string BindAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Bind in Edit Mode.");
            var assets=WritingVisualAssets.Load();
            if(assets==null)throw new InvalidOperationException("WritingVisualAssets missing.");
            const string props="Assets/Flooded_Grounds/Prefabs/Props/";
            var actor=Load("Assets/Art/Props/AlbumWinterE/Prefabs/PF_AlbumActor_E02.prefab");
            var vase=Load(props+"Prop_Vase_A.prefab");var table=Load(props+"Prop_SmallTable_A.prefab");
            var chair=Load(props+"Prop_Chair_A.prefab");var cabinet=Load(props+"Prop_Cabinet_A.prefab");
            var sofa=Load(props+"Prop_Sofa_A.prefab");
            var fade=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Chapters/Writing/AutumnMemoryFade.shader");
            if(fade==null)throw new InvalidOperationException("Autumn memory fade shader missing.");
            Undo.RecordObject(assets,"Bind autumn memory art");
            assets.memoryActor=actor;assets.memoryVase=vase;assets.memoryTable=table;
            assets.memoryChair=chair;assets.memoryCabinet=cabinet;assets.memorySofa=sofa;assets.memoryFadeShader=fade;
            EditorUtility.SetDirty(assets);AssetDatabase.SaveAssetIfDirty(assets);
            return "Bound existing E02 skinned actor, five Flooded Grounds furniture/vase sources and memory-only fade shader; no scenes modified. E02 remains a stylized wooden/paper actor, not new realistic male art.";
        }
    }
}
