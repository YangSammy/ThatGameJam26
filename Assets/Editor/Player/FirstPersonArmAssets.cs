using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    // Explicit, narrowly scoped authoring. Never rebuilds gameplay or layout.
    public static class FirstPersonArmAssets
    {
        public const string Folder="Assets/Art/Characters/Player/PSXArms";
        public const string PrefabPath=Folder+"/FirstPersonArms.prefab";
        const string Source="Assets/psx-first-person-arms-free-game-assets/arms_rig.fbx";
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null)throw new InvalidOperationException("Prefab already exists; edit its configuration instead of overwriting it.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if(source==null)throw new InvalidOperationException("Root FBX missing.");
            var importer=(ModelImporter)AssetImporter.GetAtPath(Source);
            if(importer.animationType!=ModelImporterAnimationType.Generic)throw new InvalidOperationException("Expected imported Generic rig; source left unchanged.");
            var clips=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            Func<string,AnimationClip> clip=name=>clips.Single(c=>c.name=="ArmsRig|"+name);
            var material=new Material(Shader.Find("Standard"));material.name="BareArms";
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/psx-first-person-arms-free-game-assets/arms_01.png");
            material.SetFloat("_Glossiness",0);AssetDatabase.CreateAsset(material,Folder+"/BareArms.mat");
            var root=new GameObject("FirstPersonArms");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
                var rig=root.AddComponent<FirstPersonArmRig>();rig.model=model.transform;
                var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();rig.animator=animator;
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Arms.controller");
                var layers=controller.layers;layers[0].name="Idle";layers[0].stateMachine.name="Idle";controller.layers=layers;
                var idle=controller.layers[0].stateMachine.AddState("Relax");idle.motion=clip("relax");idle.speed=0;controller.layers[0].stateMachine.defaultState=idle;
                for(int side=0;side<2;side++)
                {
                    string suffix=side==0?".L":".R",prefix=side==0?"Left":"Right";
                    var bones=model.GetComponentsInChildren<Transform>(true);
                    rig.upperArms[side]=bones.Single(t=>t.name=="upper_arm"+suffix);
                    rig.forearms[side]=bones.Single(t=>t.name=="forearm"+suffix);
                    rig.wrists[side]=bones.Single(t=>t.name=="hand"+suffix);
                    var shoulder=bones.Single(t=>t.name=="shoulder"+suffix);
                    var mask=new AvatarMask();mask.name=prefix+"Arm";
                    for(int part=0;part<(int)AvatarMaskBodyPart.LastBodyPart;part++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)part,false);
                    mask.transformCount=bones.Length;
                    for(int i=0;i<mask.transformCount;i++)
                    {
                        string path=AnimationUtility.CalculateTransformPath(bones[i],model.transform),branch=AnimationUtility.CalculateTransformPath(shoulder,model.transform);
                        mask.SetTransformPath(i,path);
                        mask.SetTransformActive(i,path==branch || path.StartsWith(branch+"/",StringComparison.Ordinal));
                    }
                    AssetDatabase.CreateAsset(mask,Folder+"/"+prefix+"Arm.mask");
                    foreach(string action in new[]{"Grab","Push"})
                    {
                        controller.AddLayer(prefix+action);layers=controller.layers;
                        var layer=layers[layers.Length-1];layer.avatarMask=mask;layer.defaultWeight=0;controller.layers=layers;
                        var state=layer.stateMachine.AddState(action);state.motion=clip(action.ToLowerInvariant()+suffix);state.speed=0;layer.stateMachine.defaultState=state;
                    }
                }
                animator.runtimeAnimatorController=controller;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=FirstPersonHands.HandLayer;
                foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.updateWhenOffscreen=true;}
                rig.model.localPosition=rig.modelOffset;
                // Persist the natural base pose for an immediately useful Inspector preview.
                clip("relax").SampleAnimation(model,0);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
                return "Created complete Generic rig, bare material, controller and two arm masks.";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static string Connect(string scenePath)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
            var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.IsValid() || !scene.isLoaded;
            if(!opened && scene.isDirty)throw new InvalidOperationException("Scene has unsaved work; refusing to save over it: "+scenePath);
            string original=File.ReadAllText(scenePath);
            if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                var hands=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FirstPersonHands>(true)).Single();
                if(hands.skeletalRig!=null)return "Already connected: "+scenePath;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if(prefab==null)throw new InvalidOperationException("Build shared assets first.");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.SetParent(hands.transform,false);
                Undo.RegisterCreatedObjectUndo(instance,"Connect skeletal arms");Undo.RecordObject(hands,"Connect skeletal arms");
                hands.skeletalRig=instance.GetComponent<FirstPersonArmRig>();
                if(hands.authoredRoot!=null){Undo.RecordObject(hands.authoredRoot.gameObject,"Disable legacy display");hands.authoredRoot.gameObject.SetActive(false);}
                EditorUtility.SetDirty(hands);EditorSceneManager.MarkSceneDirty(scene);
                if(File.ReadAllText(scenePath)!=original)throw new InvalidOperationException("Scene changed externally; new setup remains unsaved to preserve that work.");
                EditorSceneManager.SaveScene(scene);return "Connected and saved: "+scenePath;
            }
            finally{if(opened && !scene.isDirty)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
