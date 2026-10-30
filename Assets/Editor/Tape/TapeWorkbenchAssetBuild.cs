using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace MemoryWhitebox.Editor
{
    public static partial class TapeWorkbenchAssetBuild
    {
        public const string Folder="Assets/Art/Props/TapeWorkbenchV1";
        [Serializable] class Export {public Asset[] assets;}
        [Serializable] class Asset {public string name;public Node[] nodes;public Clip[] clips;}
        [Serializable] class Node {public string name,parent;public float[] position,rotation,scale;public Weight[] weights;}
        [Serializable] class Weight {public string name;public float value;}
        [Serializable] class Clip {public string name;public bool loop;public Curve[] curves;}
        [Serializable] class Curve {public string node,property,shape;public int component;public float[] values;}
        [Serializable] class Mats {public Mat[] materials;}
        [Serializable] class Mat {public string name;public float[] color;public float metallic,roughness,alpha;}
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        static Transform Maybe(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        public static string Build()
        {
            if(Application.isPlaying)throw new Exception("Stop Play first");
            Directory.CreateDirectory(Folder+"/Animations");Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Prefabs");AssetDatabase.Refresh();
            var materials=new Dictionary<string,Material>();
            foreach(var info in JsonUtility.FromJson<Mats>(File.ReadAllText(Folder+"/materials.json")).materials)
            {
                string path=Folder+"/Materials/"+info.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
                mat.color=new Color(info.color[0],info.color[1],info.color[2],info.alpha);mat.SetFloat("_Metallic",info.metallic);mat.SetFloat("_Glossiness",1-info.roughness);
                if(info.alpha<1){mat.SetFloat("_Mode",3);mat.SetInt("_SrcBlend",1);mat.SetInt("_DstBlend",10);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;}
                materials[info.name]=mat;EditorUtility.SetDirty(mat);
            }
            int count=0,shapes=0;
            foreach(var a in JsonUtility.FromJson<Export>(File.ReadAllText(Folder+"/samples.json")).assets)
            {
                var prefabPath=Folder+"/Prefabs/"+a.name+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null)throw new Exception("Already built: "+a.name);
                var root=new GameObject(a.name+"_Visual");
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Import/"+a.name+".fbx"),root.transform);
                PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                model.name=a.name;
                foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>materials.TryGetValue(m.name,out var mat)?mat:m).ToArray();
                foreach(var n in a.nodes)
                {
                    var skin=Find(model.transform,n.name).GetComponent<SkinnedMeshRenderer>();
                    if(skin==null)continue;shapes+=skin.sharedMesh.blendShapeCount;
                    foreach(var w in n.weights){int idx=skin.sharedMesh.GetBlendShapeIndex(w.name);if(idx<0)throw new Exception("Lost shape "+n.name+"/"+w.name);skin.SetBlendShapeWeight(idx,w.value*100);}
                }
                var clips=new List<AnimationClip>();
                foreach(var c in a.clips)
                {
                    var clip=new AnimationClip{name=c.name,legacy=true,frameRate=24,wrapMode=c.loop?WrapMode.Loop:WrapMode.ClampForever};
                    foreach(var curve in c.curves)
                    {
                        var node=Find(model.transform,curve.node);bool isRoot=node==model.transform;
                        string path=AnimationUtility.CalculateTransformPath(node,root.transform),property;Type type=typeof(Transform);float factor=1;
                        if(curve.property=="weights"){property="blendShape."+curve.shape;type=typeof(SkinnedMeshRenderer);factor=100;}
                        else if(curve.property=="location"){property="m_LocalPosition."+"xyz"[curve.component];factor=(curve.component==0?-1:1)*.01f;}
                        else if(curve.property=="scale")property="m_LocalScale."+"xyz"[curve.component];
                        else {property="m_LocalRotation."+"xyzw"[curve.component];factor=curve.component==1||curve.component==2?-1:1;}
                        var keys=new Keyframe[curve.values.Length];
                        for(int i=0;i<keys.Length;i++)
                        {
                            float value=curve.values[i]*factor;
                            if(isRoot && curve.property=="location")
                            {
                                var channels=c.curves.Where(x=>x.node==curve.node && x.property=="location").ToArray();
                                var v=new Vector3(-channels.First(x=>x.component==0).values[i],channels.First(x=>x.component==1).values[i],channels.First(x=>x.component==2).values[i]);value=(Quaternion.Euler(-90,0,0)*v)[curve.component];
                            }
                            if(isRoot && curve.property=="rotation_euler")
                            {
                                var ch=c.curves.Where(x=>x.node==curve.node && x.property=="rotation_euler").OrderBy(x=>x.component).ToArray();
                                var q=Quaternion.Euler(-90,0,0)*new Quaternion(ch[0].values[i],-ch[1].values[i],-ch[2].values[i],ch[3].values[i]);value=q[curve.component];
                            }
                            keys[i]=new Keyframe(i/24f,value);
                        }
                        var ac=new AnimationCurve(keys);
                        for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(ac,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(ac,i,AnimationUtility.TangentMode.Linear);}
                        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,type,property),ac);
                    }
                    clip.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(clip,Folder+"/Animations/"+c.name+".anim");clips.Add(clip);count++;
                }
                var view=root.AddComponent<WorkbenchAssetView>();view.clips=clips.ToArray();view.leftReel=Maybe(model.transform,a.name+"_Reel_L_PIVOT");view.rightReel=Maybe(model.transform,a.name+"_Reel_R_PIVOT");
                var l=Maybe(model.transform,a.name+"_Winding_L");var rgt=Maybe(model.transform,a.name+"_Winding_R");if(l!=null)view.leftWinding=l.GetComponent<SkinnedMeshRenderer>();if(rgt!=null)view.rightWinding=rgt.GetComponent<SkinnedMeshRenderer>();
                for(int i=0;i<2;i++){string p=(a.name.StartsWith("A03")?"A03":"A06")+"_Patch_"+(i==0?"L":"R");view.patches[i]=Maybe(model.transform,p+"_ROOT");var mesh=Maybe(model.transform,p);if(mesh!=null)view.patchMeshes[i]=mesh.GetComponent<SkinnedMeshRenderer>();}
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();return "Built 8 prefabs, "+count+" clips, "+shapes+" blend shapes";
        }
    }
}
