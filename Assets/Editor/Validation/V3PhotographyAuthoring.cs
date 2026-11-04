using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class V3PhotographyAuthoring
    {
        public static string ResumeAnchors()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/ParkScene.unity");bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Additive);
            else if(scene.isDirty)throw new InvalidOperationException("ParkScene has unsaved edits; preserve them first.");
            var report=new StringBuilder();
            try
            {
                Physics.SyncTransforms();
                foreach(var root in scene.GetRootGameObjects())foreach(var node in root.GetComponentsInChildren<ParkPhotoWaypoint>(true))
                {
                    if(node.photoTask==ParkPhotoWaypoint.PhotoTask.None || node.resumePoint!=null || node.referenceView==null)continue;
                    var t=new GameObject("Resume_"+node.photoTask).transform;t.SetParent(node.referenceView.parent,false);
                    Vector3 back=Vector3.ProjectOnPlane(node.referenceView.forward,Vector3.up).normalized;
                    Vector3 pos=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight-back*.9f;
                    pos.y=node.transform.position.y;
                    t.SetPositionAndRotation(pos,node.referenceView.rotation);node.resumePoint=t;EditorUtility.SetDirty(node);
                    report.AppendLine(node.name+" -> "+t.name+" "+pos.ToString("F3"));
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            return report.ToString();
        }

        static Material Material(string name,Color color)
        {
            const string folder="Assets/Art/Interaction/DarkroomCues";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Art/Interaction","DarkroomCues");
            string path=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.15f);AssetDatabase.CreateAsset(m,path);}
            return m;
        }
        static Transform Shape(Transform parent,string name,Vector3 at,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=at;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        public static string DarkroomCues()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
            var v=UnityEngine.Object.FindObjectOfType<DarkroomAssetVisuals>();
            if(v==null || v.gameObject.scene.path!="Assets/Scenes/HouseScene.unity" || v.gameObject.scene.isDirty)
                throw new InvalidOperationException("Saved HouseScene required");
            var ink=Material("Cue_Ink",new Color(.07f,.08f,.07f));
            var colors=new[]{new Color(.83f,.65f,.30f),new Color(.49f,.68f,.77f),new Color(.69f,.54f,.72f),new Color(.49f,.72f,.61f)};
            var ids=new[]{"chemical-0","chemical-1","chemical-2","print-developer","print-stop","print-fixer","print-wash"};
            var report=new StringBuilder();
            foreach(string id in ids)
            {
                var root=v.ModelRoot(id);if(root==null || root.Find("ProcessCue")!=null)continue;
                int index=id.StartsWith("chemical-")?int.Parse(id.Substring(9)):Array.IndexOf(ids,id)-3;
                bool bottle=id.StartsWith("chemical-");var badge=new GameObject("ProcessCue").transform;badge.SetParent(root,false);
                // A physical sticker on the bottle's aisle side, or the tray rim. No hit surface.
                badge.localPosition=bottle?new Vector3(-.0425f,.125f,0):new Vector3(-.164f,.059f,0);
                badge.localRotation=bottle?Quaternion.Euler(0,0,-90):Quaternion.identity;
                Shape(badge,"Backing",Vector3.zero,new Vector3(.05f,.001f,.045f),Material("Cue_"+index,colors[index]));
                if(index<3)for(int i=0;i<=index;i++)Shape(badge,"Mark_"+i,new Vector3((i-index*.5f)*.012f,.0012f,0),new Vector3(.007f,.001f,.015f),ink);
                else for(int i=0;i<3;i++)Shape(badge,"Water_"+i,new Vector3(0,.0012f,(i-1)*.010f),new Vector3(.031f,.001f,.003f),ink);
                report.AppendLine(id+": matching physical "+(index<3?(index+1)+"-mark":"water")+" cue; no collider");
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(v.gameObject.scene);EditorSceneManager.SaveScene(v.gameObject.scene);
            return report.ToString();
        }
    }
}
