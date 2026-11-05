using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MemoryWhitebox.Editor
{
    // Explicit visual-only revision. No scene save, runtime input, or chapter mutation.
    public static class DualDeckV7Build
    {
        const string Folder="Assets/Art/Props/Radio/DualDeckV6";
        const string Out="output/boombox-reference/dual_deck_v7";
        public static readonly string[] Primary={"Deck_B_Key_Record","Deck_A_Key_Play","Deck_A_Key_Rewind","Deck_A_Key_Forward","Deck_A_Key_Stop"};
        static string PathOf(Transform t,Transform root)=>AnimationUtility.CalculateTransformPath(t,root);
        public static string Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
            string prefabPath=Folder+"/Radio_DualDeck.prefab",fbxPath=Folder+"/Import/Radio_DualDeck.fbx";
            File.Copy(Out+"/models/Radio_DualDeck.fbx",fbxPath,true);
            AssetDatabase.ImportAsset(fbxPath,ImportAssetOptions.ForceUpdate);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var target=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var oldNodes=target.GetComponentsInChildren<Transform>(true).ToDictionary(t=>PathOf(t,target.transform));
                var keyHomes=oldNodes.Values.Where(t=>Primary.Contains(t.name)).ToDictionary(t=>t.name,t=>t.localPosition);
                var materialMap=AssetDatabase.FindAssets("t:Material",new[]{Folder+"/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(m=>m.name);
                foreach(var s in source.GetComponentsInChildren<Transform>(true))
                {
                    string path=PathOf(s,source.transform);if(path.Length==0)continue;
                    Transform t;
                    if(!oldNodes.TryGetValue(path,out t))
                    {
                        string parent=PathOf(s.parent,source.transform);var go=new GameObject(s.name);t=go.transform;t.SetParent(oldNodes[parent],false);oldNodes[path]=t;
                    }
                    t.localPosition=s.localPosition;t.localRotation=s.localRotation;t.localScale=s.localScale;
                    var sm=s.GetComponent<MeshFilter>();
                    if(sm!=null)
                    {
                        var mf=t.GetComponent<MeshFilter>();if(mf==null)mf=t.gameObject.AddComponent<MeshFilter>();mf.sharedMesh=sm.sharedMesh;
                        var mr=t.GetComponent<MeshRenderer>();if(mr==null)mr=t.gameObject.AddComponent<MeshRenderer>();
                        mr.sharedMaterials=s.GetComponent<MeshRenderer>().sharedMaterials.Select(m=>materialMap[m.name]).ToArray();
                        bool legacy=false;for(var p=t;p!=target.transform;p=p.parent)if(p.name.Contains("_Key_")&&!Primary.Contains(p.name)){legacy=true;break;}
                        mr.enabled=!legacy;
                    }
                }
                var driver=target.GetComponent<DualDeckVisual>();var patched=new List<string>();
                foreach(var clip in driver.clips)
                {
                    bool changed=false;
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        string node=binding.path.Split('/').Last();
                        if(!Primary.Contains(node)||!binding.propertyName.StartsWith("m_LocalPosition."))continue;
                        var t=oldNodes[binding.path];int axis=binding.propertyName.EndsWith(".x")?0:binding.propertyName.EndsWith(".y")?1:2;
                        float delta=t.localPosition[axis]-keyHomes[node][axis];
                        if(Mathf.Abs(delta)<1e-10f)continue;
                        var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
                        for(int i=0;i<keys.Length;i++)keys[i].value+=delta;
                        curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);changed=true;
                    }
                    if(changed){EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);patched.Add(clip.name);}
                }
                foreach(var c in driver.keyHits)
                {
                    if(!c.name.Contains("_Key_"))continue;
                    c.enabled=Primary.Contains(c.name);
                    if(!c.enabled)continue;
                    var box=c as BoxCollider;if(box==null)throw new Exception("Key hit is not a box: "+c.name);
                    var points=new List<Vector3>();
                    foreach(var mf in c.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var b=mf.sharedMesh.bounds;
                        for(int i=0;i<8;i++){var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));points.Add(c.transform.InverseTransformPoint(mf.transform.TransformPoint(p)));}
                    }
                    var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
                    box.center=bounds.center;box.size=bounds.size+new Vector3(.00004f,.00003f,.00002f);
                }
                if(driver.clips.Length!=59)throw new Exception("Clip library changed");
                foreach(var clip in driver.clips)foreach(var b in AnimationUtility.GetCurveBindings(clip))if(!oldNodes.ContainsKey(b.path))throw new Exception("Lost binding: "+clip.name+" "+b.path);
                PrefabUtility.SaveAsPrefabAsset(target,prefabPath);
                File.Copy(Out+"/animation_manifest.json",Folder+"/animation_manifest.json",true);
                AssetDatabase.ImportAsset(Folder+"/animation_manifest.json");
                File.WriteAllText(Out+"/validation/unity_apply.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {prefab=prefabPath,fbx=fbxPath,clips=driver.clips.Length,patchedClips=patched,visibleKeys=Primary,disabledLegacyHits=driver.keyHits.Where(c=>c.name.Contains("_Key_")&&!c.enabled).Select(c=>c.name).ToArray(),sceneSaved=false},Newtonsoft.Json.Formatting.Indented));
                return "Updated existing visual prefab; "+patched.Count+" key clips rebased; 5 visible/hittable transport keys";
            }
            finally{PrefabUtility.UnloadPrefabContents(target);}
        }
    }
}
