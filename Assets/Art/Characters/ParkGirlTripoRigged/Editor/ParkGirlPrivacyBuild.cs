using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
namespace MemoryWhitebox.Art.Editor
{
 public static class ParkGirlPrivacyBuild
 {
  const string Art=ParkGirlLocomotionBuild.Art;
  public const string StudyPrefab=Art+"/Prefabs/PF_ParkGirl_PrivacyStudy.prefab";
  public const string Out="output/Characters/ParkGirlTripo/PerformanceV4";
  public static void Report(string name,object value){Directory.CreateDirectory(Out+"/Reports");File.WriteAllText(Out+"/Reports/"+name,JsonConvert.SerializeObject(value,Formatting.Indented));}
  [MenuItem("Tools/Art/Park Girl Performance/Build Privacy Study")]
  public static void Build(){BuildAppearance(StudyPrefab,true);}
  [MenuItem("Tools/Art/Park Girl Performance/Build Hair Motion")]
  public static void BuildMotion(){BuildAppearance(ParkGirlLocomotionBuild.Prefab,false);}
  static void BuildAppearance(string path,bool privacy)
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
   var root=PrefabUtility.LoadPrefabContents(path);var baked=new Mesh();var pixels=new Texture2D(2,2);
   try
   {
    var p=root.GetComponent<ParkGirlPresentation>();var model=root.transform.Find("Visual/Model").gameObject;
    var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name!="FacePrivacyLayer");
    var original=AssetDatabase.LoadAllAssetsAtPath(Art+"/Models/CH_ParkGirlTripo_Rigged.fbx").OfType<Mesh>().Single();
    skin.sharedMesh=original;skin.bones=skin.bones.Take(original.bindposes.Length).ToArray();
    AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/AN_ParkGirl_IdleLoop.anim").SampleAnimation(model,0);skin.BakeMesh(baked,true);
    var positions=baked.vertices.Select(v=>root.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
    pixels.LoadImage(File.ReadAllBytes(Art+"/Textures/T_ParkGirlTripo_BaseColor.jpg"));
    var sampled=original.uv.Select(v=>pixels.GetPixelBilinear(v.x,v.y)).ToArray();
    Func<int,bool> facial=i=>{var v=positions[i];var c=sampled[i];return v.y>1.325f&&v.y<1.535f&&v.z>.018f&&Mathf.Abs(v.x+.008f)<.108f&&(c.b>c.g*.735f||(Mathf.Abs(v.x+.008f)<.073f&&v.y<1.485f&&v.z>.060f));};
    foreach(string old in new[]{"Hair_FringeSweepA","Hair_FringeSweepB","FacePrivacyLayer"}){var t=root.transform.Find(old);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
    foreach(string old in new[]{"FringeSweepA","FringeSweepB"}){var t=p.headBone.Find(old);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
    var tips=new List<Transform>();
    foreach(int side in new[]{-1,1})
    {
     string name=side<0?"HairTipLeft":"HairTipRight";var tip=p.headBone.Find(name);if(tip==null){tip=new GameObject(name).transform;tip.SetParent(p.headBone,false);}
     tip.position=root.transform.TransformPoint(new Vector3(side*.095f,1.445f,-.018f));tip.rotation=p.headBone.rotation;tips.Add(tip);
    }
    int headIndex=Array.IndexOf(skin.bones,p.headBone);if(headIndex<0)throw new InvalidOperationException("Head bone is missing from source skin.");
    var bones=skin.bones.Concat(tips).ToArray();
    // New children inherit the source Head bind space, not the sampled idle mesh space.
    var bind=original.bindposes.Concat(tips.Select(t=>t.worldToLocalMatrix*p.headBone.localToWorldMatrix*original.bindposes[headIndex])).ToArray();
    var weights=original.boneWeights;int weighted=0;var sourceVertices=original.vertices;var selected=new HashSet<Vector3>();
    for(int i=0;i<weights.Length;i++)
    {
     var v=positions[i];var c=sampled[i];bool hair=v.y>1.29f&&v.y<1.445f&&Mathf.Abs(v.x)<.17f&&(Mathf.Abs(v.x)>.095f||v.z<-.055f)&&v.z<.045f&&c.b<c.g*.8f;
     var w=weights[i];float headWeight=(w.boneIndex0==headIndex?w.weight0:0)+(w.boneIndex1==headIndex?w.weight1:0)+(w.boneIndex2==headIndex?w.weight2:0)+(w.boneIndex3==headIndex?w.weight3:0);
     if(!hair||w.boneIndex0!=headIndex||headWeight<.35f)continue;
     // Replace only the existing Head influence. Preserve all four source weights
     // and all neck/shoulder influences, so zero lag exactly preserves the source pose.
     selected.Add(sourceVertices[i]);
    }
    // UV/normal seams duplicate source vertices. Move every coincident Head influence
    // together so a material-boundary selection cannot open a crack during secondary motion.
    for(int i=0;i<weights.Length;i++){var w=weights[i];if(w.boneIndex0!=headIndex||!selected.Contains(sourceVertices[i]))continue;w.boneIndex0=original.bindposes.Length+(positions[i].x<0?0:1);weights[i]=w;weighted++;}
    var visible=new List<int>();var faceTriangles=new List<int>();var triangles=original.triangles;
    for(int i=0;i<triangles.Length;i+=3){bool protect=privacy&&(facial(triangles[i])||facial(triangles[i+1])||facial(triangles[i+2]));var list=protect?faceTriangles:visible;list.Add(triangles[i]);list.Add(triangles[i+1]);list.Add(triangles[i+2]);}
    var mesh=UnityEngine.Object.Instantiate(original);mesh.bindposes=bind;mesh.boneWeights=weights;
    mesh.colors=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Color(1,1,1,privacy&&facial(i)?1:0)).ToArray();
    var originalMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/M_ParkGirlTripoRigged.mat");var faceNode=skin.transform.Find("FacePrivacyLayer");
    if(privacy)
    {
     string matPath=Art+"/Materials/M_ParkGirl_FacePrivacy.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(mat==null){mat=new Material(Shader.Find("MemoryWhitebox/ParkGirlPrivacy"));AssetDatabase.CreateAsset(mat,matPath);}
     mat.shader=Shader.Find("MemoryWhitebox/ParkGirlPrivacy");mat.SetTexture("_MainTex",originalMat.mainTexture);mat.SetTexture("_BumpMap",originalMat.GetTexture("_BumpMap"));mat.SetTexture("_MetallicGlossMap",originalMat.GetTexture("_MetallicGlossMap"));mat.SetColor("_PrivacyColor",new Color(.012f,.009f,.007f));EditorUtility.SetDirty(mat);
     if(faceNode==null){faceNode=new GameObject("FacePrivacyLayer").transform;faceNode.SetParent(skin.transform,false);}
     var face=faceNode.GetComponent<SkinnedMeshRenderer>();if(face==null)face=faceNode.gameObject.AddComponent<SkinnedMeshRenderer>();var fm=UnityEngine.Object.Instantiate(mesh);fm.triangles=faceTriangles.ToArray();
     face.sharedMesh=SaveMesh(fm,"FacePrivacyLayer");face.sharedMaterial=mat;face.bones=bones;face.rootBone=skin.rootBone;face.localBounds=skin.localBounds;face.updateWhenOffscreen=true;skin.sharedMaterial=mat;
    }
    else{if(faceNode!=null)UnityEngine.Object.DestroyImmediate(faceNode.gameObject);skin.sharedMaterial=originalMat;}
    mesh.triangles=visible.ToArray();skin.sharedMesh=SaveMesh(mesh,privacy?"ParkGirl_PrivacyStudyBody":"ParkGirl_HairMotionBody");skin.bones=bones;
    p.hairBones=tips.ToArray();p.upperChest=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="UpperChest");p.visualRoot=root.transform;p.leftFoot=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Left_Foot");p.rightFoot=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Right_Foot");
    EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();PrefabUtility.SaveAsPrefabAsset(root,path);
    Report(privacy?"privacy-build.json":"hair-motion-build.json",new{prefab=path,triangles=triangles.Length/3,source_geometry_unchanged=true,source_face_preserved=true,added_bones=2,hair_vertices_weighted=weighted,privacy_study=privacy,art_accepted=false,method="Original Tripo hair shape with lower-tip secondary weights. Study only: original face triangles with local shadow shader; no flattening or added fringe panels."});
   }
   finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(pixels);PrefabUtility.UnloadPrefabContents(root);}
  }
  static Mesh SaveMesh(Mesh source,string name)
  {
   string path=Art+"/Meshes/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(asset==null){source.name=name;AssetDatabase.CreateAsset(source,path);return source;}
   asset.Clear();asset.indexFormat=source.indexFormat;asset.vertices=source.vertices;asset.normals=source.normals;asset.uv=source.uv;asset.tangents=source.tangents;asset.colors=source.colors;asset.boneWeights=source.boneWeights;asset.bindposes=source.bindposes;asset.triangles=source.triangles;asset.bounds=source.bounds;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);UnityEngine.Object.DestroyImmediate(source);return asset;
  }
  [MenuItem("Tools/Art/Park Girl Performance/Render Privacy Orbit")]
  public static void RenderOrbit()
  {
   var scene=EditorSceneManager.NewPreviewScene();try
   {
    var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StudyPrefab),scene);var model=root.transform.Find("Visual/Model").gameObject;root.GetComponentInChildren<Animator>().enabled=false;
    AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/AN_ParkGirl_IdleLoop.anim").SampleAnimation(model,0);
    foreach(var s in root.GetComponentsInChildren<SkinnedMeshRenderer>()){var m=new Mesh();s.BakeMesh(m,true);var go=new GameObject("Evaluated_"+s.name);go.transform.SetParent(s.transform,false);go.AddComponent<MeshFilter>().sharedMesh=m;go.AddComponent<MeshRenderer>().sharedMaterial=s.sharedMaterial;s.enabled=false;}
    var camera=new GameObject("OrbitCamera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;camera.fieldOfView=32;camera.nearClipPlane=.015f;camera.farClipPlane=10;camera.backgroundColor=new Color(.3f,.31f,.32f);camera.clearFlags=CameraClearFlags.SolidColor;
    foreach(var angle in new[]{-35f,135f}){var light=new GameObject("ReviewLight").AddComponent<Light>();SceneManager.MoveGameObjectToScene(light.gameObject,scene);light.type=LightType.Directional;light.intensity=angle<0?1.2f:.6f;light.transform.rotation=Quaternion.Euler(30,angle,0);}
    Directory.CreateDirectory(Out+"/Preview/Orbit");int count=0;
    foreach(float elevation in new[]{-75f,-35f,0f,35f,75f})foreach(float yaw in new[]{0f,45f,90f,135f,180f,225f,270f,315f}){var target=new Vector3(-.008f,1.44f,.01f);camera.transform.position=target+Quaternion.Euler(-elevation,yaw,0)*Vector3.forward*.75f;camera.transform.LookAt(target);ParkGirlLocomotionValidation.Capture(camera,Out+"/Preview/Orbit/angle_"+count.ToString("D2")+".png");count++;}
    Report("orbit-render.json",new{views=count,elevations=new[]{-75,-35,0,35,75},yaw_steps=45,scope="Independent privacy study, fixed idle pose and strong direct light. Not final art acceptance."});
   }
   finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
 }
}
