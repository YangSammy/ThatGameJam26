using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    public static class HouseRadioBuild
    {
        const string Folder="Assets/Art/Props/Radio";
        static Transform Find(Transform r,string n){return r.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);}
        static Material Material(string name)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));mat.name=name;AssetDatabase.CreateAsset(mat,path);}
            string prefix=name.StartsWith("Controls_")?name.Substring(9):"Boombox";
            mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+prefix+"_BaseColor.png"));
            mat.color=Color.white;
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+prefix+"_MetallicSmoothness.png"));
            mat.EnableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_GlossMapScale",1);mat.SetFloat("_Glossiness",.25f);
            if(name=="Radio_Glass")
            {
                mat.color=new Color(1,1,1,.14f);mat.SetFloat("_Mode",2);mat.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHABLEND_ON");mat.renderQueue=3000;
            }
            return mat;
        }
        static void Materials(GameObject model)
        {
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>Material(m.name)).ToArray();
        }
        static AnimationClip Clip(string name,bool loop,bool hold=false)
        {
            var original=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Models/"+(name=="PlaybackLoop"?name:"LoadCassette")+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            var clip=Object.Instantiate(original);clip.name=name;
            // Placement belongs to the authored scene transform, never to an animation state.
            foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                if(binding.path=="" && binding.type==typeof(Transform))AnimationUtility.SetEditorCurve(clip,binding,null);
            if(hold)foreach(var b in AnimationUtility.GetCurveBindings(clip))
            {
                var curve=AnimationUtility.GetEditorCurve(clip,b);float v=curve.Evaluate(0);
                AnimationUtility.SetEditorCurve(clip,b,new AnimationCurve(new Keyframe(0,v),new Keyframe(1f/24,v)));
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.startTime=0;settings.stopTime=hold?1f/24:original.length;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string path=Folder+"/Animations/"+name+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing!=null){EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);return existing;}
            AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static AnimatorController Controller()
        {
            var load=Clip("LoadCassette",false);var loop=Clip("PlaybackLoop",true);var ready=Clip("Ready_Open",true,true);
            string path=Folder+"/Animations/Radio.controller";
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(c!=null)return c;
            c=AnimatorController.CreateAnimatorControllerAtPath(path);var sm=c.layers[0].stateMachine;
            var a=sm.AddState("Ready_Open",new Vector3(100,60));a.motion=ready;sm.defaultState=a;
            var b=sm.AddState("LoadCassette",new Vector3(350,60));b.motion=load;
            var d=sm.AddState("PlaybackLoop",new Vector3(600,60));d.motion=loop;
            var t=b.AddTransition(d);t.hasExitTime=true;t.exitTime=1;t.duration=0;t.hasFixedDuration=true;t.interruptionSource=TransitionInterruptionSource.None;
            return c;
        }
        static void ArchiveChildren(Transform root)
        {
            var children=root.Cast<Transform>().ToArray();var backup=new GameObject("Replaced whitebox props").transform;backup.SetParent(root,false);
            foreach(var t in children)t.SetParent(backup,true);backup.gameObject.SetActive(false);
        }
        public static string Install()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before installation.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/HouseScene.unity")throw new InvalidOperationException("Open HouseScene.");
            var house=Object.FindObjectOfType<WBMain>();
            var entries=house.houseShell.transform.Find("Study entry props");
            var radio=entries.Find("Radio - tape chapter");var tapeGroup=entries.Find("Cassettes - tape chapter");
            if(radio.GetComponent<HouseRadioDeck>()!=null)return "Radio is already installed.";
            foreach(var tex in AssetDatabase.FindAssets("t:Texture2D",new[]{Folder+"/Textures"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(tex);
                bool normal=tex.EndsWith("_Normal.png",StringComparison.OrdinalIgnoreCase);
                imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                imp.convertToNormalmap=false;
                imp.sRGBTexture=tex.Contains("BaseColor");imp.maxTextureSize=2048;imp.SaveAndReimport();
            }
            var controller=Controller();ArchiveChildren(radio);ArchiveChildren(tapeGroup);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/LoadCassette.fbx"),radio);
            model.name="Radio case";Materials(model);
            var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var deck=radio.gameObject.AddComponent<HouseRadioDeck>();deck.animator=animator;deck.house=house;deck.view=house.playerCamera;house.radioDeck=deck;
            deck.carrier=Find(model.transform,"Tape_LOAD_MOTION");deck.proxyTape=Find(model.transform,"Tape_01_ROOT");
            deck.leftDriver=Find(model.transform,"Tape_01_Reel_L_PIVOT");deck.rightDriver=Find(model.transform,"Tape_01_Reel_R_PIVOT");deck.startSocket=Find(model.transform,"Socket_TapeStart");
            foreach(var r in deck.proxyTape.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            deck.speaker=radio.gameObject.AddComponent<AudioSource>();deck.speaker.playOnAwake=false;deck.speaker.spatialBlend=1;deck.speaker.minDistance=.7f;deck.speaker.maxDistance=8;deck.speaker.volume=.35f;
            var body=radio.GetComponent<BoxCollider>();body.center=new Vector3(0,.13f,0);body.size=new Vector3(.38f,.27f,.17f);deck.radioHit=body;
            var zone=new GameObject("Cassette insertion area");zone.transform.SetParent(radio,false);zone.transform.position=deck.startSocket.position;zone.layer=25;
            var z=zone.AddComponent<BoxCollider>();z.isTrigger=true;z.size=new Vector3(.15f,.10f,.11f);deck.slotHit=z;
            Object.DestroyImmediate(tapeGroup.GetComponent<BoxCollider>());
            deck.tapes=new HouseRadioDeck.Cassette[3];
            for(int n=0;n<3;n++)
            {
                var wrapper=new GameObject("Tape "+(n+1)).transform;wrapper.SetParent(tapeGroup,false);wrapper.localPosition=new Vector3((n-1)*.145f,-.013f,n==1?.04f:0);wrapper.localRotation=Quaternion.Euler(90,n==1?8:-8,0);wrapper.gameObject.layer=25;
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Models/Tape_0"+(n+1)+".fbx"),wrapper);Materials(obj);
                var hit=wrapper.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.105f,.066f,.014f);
                deck.tapes[n]=new HouseRadioDeck.Cassette{root=wrapper,model=obj.transform,hit=hit,leftReel=Find(obj.transform,"Tape_0"+(n+1)+"_Reel_L_PIVOT"),rightReel=Find(obj.transform,"Tape_0"+(n+1)+"_Reel_R_PIVOT"),audio=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/TestTape"+(n+1)+".wav"),label="磁带 "+(n+1)};
            }
            SafeInspectionBuild.RefreshRadioSubjects(radio,tapeGroup);
            EditorUtility.SetDirty(house);EditorUtility.SetDirty(deck);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Replaced desk radio and three tapes in HouseScene; controller, audio and inspection wired.";
        }
    }
}
