using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    public static class ParkGirlLocomotionBuild
    {
        public const string Art="Assets/Art/Characters/ParkGirlTripoRigged";
        public const string Out="output/Characters/ParkGirlTripo/LocomotionV3";
        public const string Prefab=Art+"/Prefabs/PF_ParkGirlTripo_Locomotion.prefab";
        public const string ScenePath=Art+"/Scenes/ParkGirlTripo_LocomotionPreview.unity";
        public const string Walk=Art+"/Animations/AN_ParkGirl_WalkInPlace.anim";
        public static void Report(string name,object data){Directory.CreateDirectory(Out+"/Reports");File.WriteAllText(Out+"/Reports/"+name,JsonConvert.SerializeObject(data,Formatting.Indented));}

        [MenuItem("Tools/Art/Park Girl Locomotion/Build Independent Sample")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building.");
            var idle=CopyClip("AN_idle", "AN_ParkGirl_IdleLoop");
            // The full source idle steps sideways by 25 cm halfway through. Use its quiet
            // opening breath for locomotion, closing the last half-second back to the start.
            var idleSource=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/AN_idle.anim");
            idle.ClearCurves();
            foreach(var binding in AnimationUtility.GetCurveBindings(idleSource))
            {
                var original=AnimationUtility.GetEditorCurve(idleSource,binding);var keys=new Keyframe[121];
                for(int i=0;i<keys.Length;i++)
                {
                    float time=i/30f,blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.5f,4,time));
                    keys[i]=new Keyframe(time,Mathf.Lerp(original.Evaluate(time),original.Evaluate(0),blend));
                }
                var curve=new AnimationCurve(keys);
                for(int i=0;i<keys.Length;i++)curve.SmoothTangents(i,0);
                AnimationUtility.SetEditorCurve(idle,binding,curve);
            }
            idle.EnsureQuaternionContinuity();
            var idleSettings=AnimationUtility.GetAnimationClipSettings(idle);idleSettings.startTime=0;idleSettings.stopTime=4;AnimationUtility.SetAnimationClipSettings(idle,idleSettings);
            var walk=CopyClip("AN_walk", "AN_ParkGirl_WalkInPlace");
            // FBX Armature rotates its local Z up into Unity Y. Horizontal axes are hips X/Y.
            float distance=0;
            foreach(var binding in AnimationUtility.GetCurveBindings(walk).Where(b=>b.path=="Armature/Hips" && (b.propertyName=="m_LocalPosition.x"||b.propertyName=="m_LocalPosition.y")))
            {
                var curve=AnimationUtility.GetEditorCurve(walk,binding);var idleCurve=AnimationUtility.GetEditorCurve(idle,binding);
                float begin=curve.Evaluate(0),travel=curve.Evaluate(walk.length)-begin,slope=travel/walk.length;
                if(binding.propertyName.EndsWith(".x"))distance=Mathf.Abs(travel)*1.6f;
                var keys=curve.keys;
                for(int i=0;i<keys.Length;i++){keys[i].value-=slope*keys[i].time+begin-idleCurve.Evaluate(0);keys[i].inTangent-=slope;keys[i].outTangent-=slope;}
                curve.keys=keys;AnimationUtility.SetEditorCurve(walk,binding,curve);
            }
            foreach(var clip in new[]{idle,walk})
            {
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;
                AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
            }
            string path=Art+"/Animations/AC_ParkGirl_Locomotion.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float}};
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Locomotion");
            if(state==null)state=machine.AddState("Locomotion");
            var tree=state.motion as BlendTree;
            if(tree==null){tree=new BlendTree{name="Idle_Walk",hideFlags=HideFlags.HideInHierarchy};AssetDatabase.AddObjectToAsset(tree,controller);state.motion=tree;}
            float sourceSpeed=distance/walk.length;
            tree.blendType=BlendTreeType.Simple1D;tree.blendParameter="Speed";tree.useAutomaticThresholds=false;
            tree.children=new[]{new ChildMotion{motion=idle,threshold=0,timeScale=1},new ChildMotion{motion=walk,threshold=1.25f,timeScale=1.25f/sourceSpeed}};
            machine.defaultState=state;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/PF_ParkGirlTripo_RiggedReview.prefab"));SceneManager.MoveGameObjectToScene(go,preview);go.name="PF_ParkGirlTripo_Locomotion";
                var animator=go.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                var presentation=go.AddComponent<ParkGirlPresentation>();
                var bones=go.GetComponentsInChildren<Transform>();
                presentation.headBone=bones.Single(t=>t.name=="Head");presentation.rightUpperArm=bones.Single(t=>t.name=="Right_UpperArm");presentation.rightLowerArm=bones.Single(t=>t.name=="Right_LowerArm");presentation.rightHand=bones.Single(t=>t.name=="Right_Hand");
                PrefabUtility.SaveAsPrefabAsset(go,Prefab);
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            CreatePreviewScene();
            Report("clip-processing.json",new{source_distance_m=distance,source_speed_mps=sourceSpeed,target_speed_mps=1.25f,walk_playback_rate=1.25f/sourceSpeed,speed_damping_seconds=.18f,method="Subtract linear hips X/Y travel from derivative only; keep vertical gait and lateral residual. Blend idle/walk using existing Speed parameter."});
        }
        static AnimationClip CopyClip(string from,string name)
        {
            string path=Art+"/Animations/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/"+from+".anim"),clip);clip.name=name;return clip;
        }
        static void CreatePreviewScene()
        {
            var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);RenderSettings.fog=false;
                var actor=new GameObject("SampleCompanion");var c=actor.AddComponent<ParkCompanion>();
                var motor=actor.GetComponent<CharacterController>();motor.height=1.65f;motor.center=Vector3.up*.84f;motor.radius=.22f;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);visual.transform.SetParent(actor.transform,false);
                c.animator=visual.GetComponentInChildren<Animator>();c.visualPresentation=visual.GetComponent<ParkGirlPresentation>();c.visualPresentation.companion=c;
                c.player=new GameObject("PreviewPlayerPosition").transform;c.player.position=Vector3.back*3;
                c.spawn=new GameObject("PreviewSpawn").transform;c.spawn.position=Vector3.up*.03f;
                var waypoint=new GameObject("PreviewWalkTarget").AddComponent<ParkPhotoWaypoint>();waypoint.transform.position=Vector3.forward*90;c.route=new[]{waypoint};
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="PreviewFloor";floor.transform.position=new Vector3(0,-.15f,40);floor.transform.localScale=new Vector3(20,.3f,120);
                const string floorPath=Art+"/Materials/M_ParkGirl_ReviewFloor.mat";
                var floorMat=AssetDatabase.LoadAssetAtPath<Material>(floorPath);
                if(floorMat==null){floorMat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(floorMat,floorPath);}
                floorMat.color=new Color(.28f,.29f,.27f);floorMat.SetFloat("_Glossiness",.05f);EditorUtility.SetDirty(floorMat);AssetDatabase.SaveAssetIfDirty(floorMat);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
                var camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.20f);camera.fieldOfView=34;camera.nearClipPlane=.05f;camera.farClipPlane=180;
                camera.transform.position=new Vector3(2.6f,1.25f,3.7f);camera.transform.LookAt(Vector3.up*.85f);camera.gameObject.AddComponent<AudioListener>();
                var light=new GameObject("KeyLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(35,-35,0);
                var fill=new GameObject("FillLight").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.transform.rotation=Quaternion.Euler(20,140,0);
                var driver=new GameObject("PreviewSequence").AddComponent<ParkGirlLocomotionPreview>();driver.companion=c;driver.reviewCamera=camera;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally{SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        }
        [MenuItem("Tools/Art/Park Girl Locomotion/Install Park Visual")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            if(!File.Exists(Out+"/Reports/sample-play-result.json"))throw new InvalidOperationException("Standalone Play Mode validation must pass first.");
            var evidence=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Out+"/Reports/sample-play-result.json"));
            if(!(bool)evidence["pass"] || (string)evidence["asset_hash"]!=AssetDatabase.GetAssetDependencyHash(Prefab).ToString())throw new InvalidOperationException("Sample evidence is stale; validate the current prefab first.");
            var existing=SceneManager.GetSceneByPath("Assets/Scenes/ParkScene.unity");bool opened=!existing.isLoaded;
            var scene=opened?EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Additive):existing;
            try
            {
                if(scene.isDirty)throw new InvalidOperationException("ParkScene has unsaved user changes.");
                var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParkCompanion>(true)).Single();
                if(c.visualPresentation!=null)throw new InvalidOperationException("Park visual already installed; inspect before changing.");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),scene);visual.name="ParkGirl_Tripo_Visual";visual.transform.SetParent(c.transform,false);
                foreach(var r in c.visual.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                c.animator=visual.GetComponentInChildren<Animator>();c.visualPresentation=visual.GetComponent<ParkGirlPresentation>();c.visualPresentation.companion=c;
                PrefabUtility.RecordPrefabInstancePropertyModifications(c.visualPresentation);
                EditorUtility.SetDirty(c);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Report("scene-install.json",new{scene=scene.path,companion=c.name,renderer_replacement_only=true,old_visual_kept_for_photo_proxies=c.visual.name,route_count=c.route.Length,head=c.head.name,chest=c.chest.name,hips=c.hips.name,feet=c.feet.name});
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
