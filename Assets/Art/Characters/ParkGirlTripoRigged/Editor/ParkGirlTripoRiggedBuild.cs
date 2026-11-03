using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    public static class ParkGirlTripoRiggedBuild
    {
        const string Art="Assets/Art/Characters/ParkGirlTripoRigged";
        const string Out="output/Characters/ParkGirlTripo/RiggedV2";
        const string Model=Art+"/Models/CH_ParkGirlTripo_Rigged.fbx";
        const string Prefab=Art+"/Prefabs/PF_ParkGirlTripo_RiggedReview.prefab";
        public const string ScenePath=Art+"/Scenes/ParkGirlTripo_RiggedReview.unity";
        public static void Report(string name,object data)=>File.WriteAllText(Out+"/Reports/"+name,JsonConvert.SerializeObject(data,Formatting.Indented));
        public static string ClipName(string name)=>name.Split('|').Last().Replace("Armature_","");

        [MenuItem("Tools/Art/Park Girl Tripo Rigged/Build and Audit")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(Out+"/Reports");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var p in Directory.GetFiles(Art+"/Textures").Where(p=>!p.EndsWith(".meta")))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(p);
                ti.sRGBTexture=p.Contains("_BaseColor.");ti.textureType=p.Contains("_Normal.")?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.mipmapEnabled=true;ti.alphaIsTransparency=false;ti.SaveAndReimport();
            }
            var mi=(ModelImporter)AssetImporter.GetAtPath(Model);
            mi.animationType=ModelImporterAnimationType.Generic;mi.importAnimation=true;mi.optimizeGameObjects=false;mi.optimizeBones=false;
            mi.importCameras=false;mi.importLights=false;mi.isReadable=true;mi.meshCompression=ModelImporterMeshCompression.Off;
            mi.animationCompression=ModelImporterAnimationCompression.Off;mi.importNormals=ModelImporterNormals.Import;mi.importTangents=ModelImporterTangents.CalculateMikk;
            mi.skinWeights=ModelImporterSkinWeights.Custom;mi.maxBonesPerVertex=4;mi.minBoneWeight=.000001f;mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.SaveAndReimport();
            var sourceClips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            if(sourceClips.Length!=7)throw new InvalidOperationException("Expected the seven audited input takes; found "+sourceClips.Length);
            var controllerPath=Art+"/Animations/AC_ParkGirlTripo_Review.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;
            foreach(var source in sourceClips)
            {
                string name=ClipName(source.name),path=Art+"/Animations/AN_"+name+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
                EditorUtility.CopySerialized(source,clip);clip.name="AN_"+name;
                // Preserve source timing and motion for audit. Loop seams are measured, not silently forced.
                var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);
                state.motion=clip;if(name=="idle")machine.defaultState=state;
                EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            string matPath=Art+"/Materials/M_ParkGirlTripoRigged.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,matPath);}
            mat.color=Color.white;mat.mainTexture=Tex("BaseColor.jpg");mat.SetTexture("_BumpMap",Tex("Normal.png"));mat.SetTexture("_MetallicGlossMap",Tex("MetallicSmoothness.png"));
            mat.SetFloat("_BumpScale",1);mat.SetFloat("_GlossMapScale",1);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("PF_ParkGirlTripo_RiggedReview");SceneManager.MoveGameObjectToScene(root,scene);
                var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
                // Tripo normalizes the imported character to one metre; restore the approved sample height.
                visual.transform.localScale=Vector3.one*1.6f;
                visual.transform.localRotation=Quaternion.Euler(0,-90,0);
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model),visual.transform);model.name="Model";
                var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterial=mat;skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;}
                var anchor=new GameObject("PlacementAnchor");anchor.transform.SetParent(root.transform,false);
                var center=new GameObject("InspectCenter");center.transform.SetParent(root.transform,false);center.transform.localPosition=new Vector3(0,.8f,0);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
            CreateScene();Audit();
        }
        static Texture2D Tex(string name)
        {
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/T_ParkGirlTripo_"+name);if(t==null)throw new FileNotFoundException(name);return t;
        }
        static Camera Setup(Scene scene)
        {
            var go=new GameObject("ReviewCamera");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();cam.scene=scene;cam.orthographic=true;cam.orthographicSize=.94f;
            cam.nearClipPlane=.02f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.20f,.21f);
            cam.transform.position=new Vector3(0,.85f,3);cam.transform.LookAt(new Vector3(0,.8f,0));
            foreach(var pair in new[]{new Vector2(-35,1),new Vector2(140,.55f)}){var lo=new GameObject("ReviewLight");SceneManager.MoveGameObjectToScene(lo,scene);var light=lo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=pair.y;lo.transform.rotation=Quaternion.Euler(32,pair.x,0);}
            return cam;
        }
        static void CreateScene()
        {
            var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);RenderSettings.fog=false;PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);Setup(scene);EditorSceneManager.SaveScene(scene,ScenePath);}
            finally{if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        }
        static bool Finite(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
        [MenuItem("Tools/Art/Park Girl Tripo Rigged/Check Animator")]
        public static void CheckAnimator()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
                var model=root.transform.Find("Visual/Model").gameObject;
                var animator=model.GetComponent<Animator>();
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var bones=model.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                var rows=new List<object>();bool pass=true;
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                {
                    string state=clip.name.Replace("AN_","");
                    animator.Rebind();animator.Play(state,0,0);animator.Update(0);
                    var start=bones.Select(b=>b.position).ToArray();
                    animator.Update(.5f);
                    var evaluated=bones.Select(b=>b.position).ToArray();
                    float change=bones.Select((b,i)=>Vector3.Distance(evaluated[i],start[i])).Max();
                    clip.SampleAnimation(model,.5f);
                    float error=bones.Select((b,i)=>Vector3.Distance(b.position,evaluated[i])).Max();
                    bool valid=change>0.000001f&&error<.005f;pass&=valid;
                    rows.Add(new{state,pass=valid,maximum_bone_change_m=change,maximum_difference_from_sample_m=error});
                }
                Report("animator-evaluation.json",new{pass,method="Editor manual Animator.Update: each controller state evaluated 0 to 0.5 seconds and compared with AnimationClip.SampleAnimation; not Play Mode",avatar_present=animator.avatar!=null,clips=rows});
                if(!pass)throw new InvalidOperationException("Animator evaluation differs from source clips; see report.");
                Debug.Log("TRIPO_ANIMATOR_EVALUATION_PASS");
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [MenuItem("Tools/Art/Park Girl Tripo Rigged/Audit and Render")]
        public static void Audit()
        {
            var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D image=null,sheet=null;Mesh baked=null;
            try
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);
                var model=root.transform.Find("Visual/Model").gameObject;var animator=model.GetComponent<Animator>();animator.enabled=false;
                var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
                if(skin==null||skin.bones.Any(b=>b==null)||skin.sharedMaterial==null)throw new InvalidOperationException("Missing skin, bone or material.");
                var hips=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hips");
                var hand=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Right_Hand");
                var rows=new List<object>();var cam=Setup(scene);cam.enabled=false;
                const int w=420,h=640;
                rt=new RenderTexture(w,h,24){antiAliasing=4};rt.Create();cam.targetTexture=rt;
                image=new Texture2D(w,h,TextureFormat.RGB24,false);sheet=new Texture2D(w*4,h,TextureFormat.RGB24,false);baked=new Mesh();
                var go=new GameObject("EvaluatedSkin");SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=baked;go.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
                Directory.CreateDirectory(Out+"/Preview/Unity");
                foreach(string path in Directory.GetFiles(Art+"/Animations","*.anim"))
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);var samples=new List<object>();Vector3[] first=null,last=null;Vector3 startHips=Vector3.zero,endHips=Vector3.zero;
                    float minY=float.PositiveInfinity,maxY=float.NegativeInfinity;int invalid=0;
                    for(int frame=0;frame<=Mathf.CeilToInt(clip.length*30);frame++)
                    {
                        float time=Mathf.Min(frame/30f,clip.length);clip.SampleAnimation(model,time);skin.BakeMesh(baked,true);var vertices=baked.vertices;
                        for(int i=0;i<vertices.Length;i++){vertices[i]=skin.transform.TransformPoint(vertices[i]);if(!Finite(vertices[i]))invalid++;minY=Mathf.Min(minY,vertices[i].y);maxY=Mathf.Max(maxY,vertices[i].y);}
                        if(frame==0){first=vertices;startHips=hips.position;}last=vertices;endHips=hips.position;
                        samples.Add(new{time,hips=new[]{hips.position.x,hips.position.y,hips.position.z},rightHand=new[]{hand.position.x,hand.position.y,hand.position.z}});
                    }
                    float seamMax=0;for(int i=0;i<first.Length;i++)seamMax=Mathf.Max(seamMax,Vector3.Distance(first[i]-startHips,last[i]-endHips));
                    for(int i=0;i<4;i++)
                    {
                        clip.SampleAnimation(model,clip.length*i/4);skin.BakeMesh(baked,true);skin.enabled=false;
                        var target=new Vector3(hips.position.x,.8f,hips.position.z);cam.transform.position=target+new Vector3(0,.06f,3);cam.transform.LookAt(target);cam.orthographicSize=.94f;cam.Render();Read(rt,image,w,h);sheet.SetPixels(i*w,0,w,h,image.GetPixels());skin.enabled=true;
                        File.WriteAllBytes(Out+"/Preview/Unity/"+clip.name+"_"+i+".png",image.EncodeToPNG());
                    }
                    sheet.Apply();File.WriteAllBytes(Out+"/Preview/Unity/"+clip.name+"_sheet.png",sheet.EncodeToPNG());
                    rows.Add(new{name=clip.name,duration=clip.length,fps=clip.frameRate,sample_count=samples.Count,invalid_vertices=invalid,min_y=minY,max_y=maxY,hips_displacement_m=Vector3.Distance(startHips,endHips),hips_horizontal_displacement_m=Vector2.Distance(new Vector2(startHips.x,startHips.z),new Vector2(endHips.x,endHips.z)),root_stable=root.transform.position==Vector3.zero&&root.transform.localScale==Vector3.one,root_aligned_loop_seam_max_m=seamMax,samples});
                    if(invalid>0)throw new InvalidOperationException("Non-finite skin vertices in "+clip.name);
                }
                var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/AN_idle.anim");idle.SampleAnimation(model,0);skin.BakeMesh(baked,true);skin.enabled=false;
                for(int view=0;view<4;view++){float angle=view*90*Mathf.Deg2Rad;cam.transform.position=new Vector3(Mathf.Sin(angle)*3,.85f,Mathf.Cos(angle)*3);cam.transform.LookAt(new Vector3(0,.8f,0));cam.Render();Read(rt,image,w,h);sheet.SetPixels(view*w,0,w,h,image.GetPixels());}
                sheet.Apply();File.WriteAllBytes(Out+"/Preview/Unity/turnaround.png",sheet.EncodeToPNG());
                Report("unity-animation-audit.json",new{pass=true,unity=Application.unityVersion,rig="Generic; source motion preserved",triangles=mesh.triangles.Length/3,vertices=mesh.vertexCount,bones=skin.bones.Length,materials=1,missing_scripts=root.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),animation_count=rows.Count,clips=rows});
                Debug.Log("TRIPO_RIGGED_AUDIT_PASS: 7 takes sampled; see report for motion/art limitations.");
            }
            finally{if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(sheet!=null)UnityEngine.Object.DestroyImmediate(sheet);if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Read(RenderTexture rt,Texture2D image,int w,int h){var old=RenderTexture.active;try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();}finally{RenderTexture.active=old;}}
    }
}
