using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.EditorTools
{
    public static class ParkPhotoPrototypeBuild
    {
        const string Folder="Assets/Art/Gameplay/ParkPhotography";
        [MenuItem("Tools/公园摄影/在 ParkScene 安装跟随拍照原型")]
        public static string Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("请先停止 Play。");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/ParkScene.unity")throw new InvalidOperationException("请先打开 ParkScene；工具不会修改其他场景。");
            if(Object.FindObjectOfType<ParkPhotoPrototype>(true)!=null)return "原型已存在；保留现有调整，不重复生成。";
            var environment=GameObject.Find("Environment_ParkBlockout_V01")??GameObject.Find("Park Blockout v01");if(environment==null)throw new InvalidOperationException("未找到现有公园环境。");
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("ParkScene 需要 Main Camera。");
            EnsureFolder(Folder);EnsureFolder(Folder+"/Materials");EnsureFolder(Folder+"/Audio");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/ParkPhoto/ManualFocus.shader");
            if(shader==null)throw new InvalidOperationException("请等待调焦 Shader 导入。");
            var focusMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/ParkPhoto/ManualFocus.mat");
            if(focusMaterial==null){focusMaterial=new Material(shader);AssetDatabase.CreateAsset(focusMaterial,"Assets/Resources/ParkPhoto/ManualFocus.mat");}
            var coat=Material("Coat",new Color(.24f,.42f,.48f));var trousers=Material("Trousers",new Color(.19f,.22f,.24f));
            var skin=Material("Skin",new Color(.72f,.55f,.40f));var hair=Material("HairAndShoes",new Color(.12f,.10f,.09f));var scarf=Material("Scarf",new Color(.80f,.44f,.24f));
            foreach(var mesh in environment.GetComponentsInChildren<MeshFilter>(true))
            {
                if(mesh.sharedMesh==null || mesh.name=="Creek_Surface" || mesh.GetComponent<Collider>()!=null)continue;
                var collider=Undo.AddComponent<MeshCollider>(mesh.gameObject);collider.sharedMesh=mesh.sharedMesh;
            }
            var root=new GameObject("Park Photo Prototype - 公园陪伴抓拍");Undo.RegisterCreatedObjectUndo(root,"创建公园摄影原型");
            var anchors=Node("Scene Anchors - 可编辑定位",root.transform,Vector3.zero);
            var spawn=Node("Player Spawn - 玩家出生点",anchors,new Vector3(-22.5f,.09f,-17.5f));spawn.rotation=Quaternion.Euler(0,25,0);
            var companionSpawn=Node("Companion Spawn - 同伴起点",anchors,new Vector3(-20.5f,.09f,-13));companionSpawn.rotation=Quaternion.Euler(0,25,0);
            var waterLook=Node("Water Look - 望水目标",anchors,new Vector3(5,1,-12));
            var treeLook=Node("Tree Look - 沿路停留",anchors,new Vector3(-16,3,9));
            var routeRoot=Node("Route - 按顺序移动定位物体",root.transform,Vector3.zero);
            var positions=new[]{new Vector3(-19,.09f,-9.5f),new Vector3(-14.5f,.09f,-3),new Vector3(-8,.09f,0),new Vector3(.35f,.19f,0),new Vector3(.75f,.37f,0),new Vector3(1.18f,.55f,0),new Vector3(2,.55f,0),new Vector3(5,.55f,0),new Vector3(8.4f,.55f,0),new Vector3(8.85f,.37f,0),new Vector3(9.3f,.19f,0),new Vector3(10,.09f,0),new Vector3(14,.09f,1)};
            var nodes=new List<ParkPhotoWaypoint>();
            for(int i=0;i<positions.Length;i++)
            {
                string name=i==7?"07 - 桥边望水":i==positions.Length-1?"12 - 等候玩家":i.ToString("00")+" - 路线点";
                var node=Node(name,routeRoot,positions[i]).gameObject.AddComponent<ParkPhotoWaypoint>();
                if(i==2){node.pauseSeconds=4;node.lookAt=treeLook;}
                if(i==7){node.pauseSeconds=12;node.lookAt=waterLook;node.watersideMoment=true;}
                nodes.Add(node);
            }
            var actorRoot=Node("Companion - 同伴逻辑与拍摄标记",root.transform,companionSpawn.position);actorRoot.rotation=companionSpawn.rotation;
            var actor=actorRoot.gameObject.AddComponent<ParkCompanion>();actor.spawn=companionSpawn;actor.route=nodes.ToArray();
            var body=actorRoot.gameObject.AddComponent<CapsuleCollider>();body.height=1.78f;body.center=new Vector3(0,.9f,0);body.radius=.24f;
            actor.head=Node("Photo Head - 头部",actorRoot,new Vector3(0,1.66f,0));actor.chest=Node("Photo Chest - 对焦位置",actorRoot,new Vector3(0,1.24f,0));
            actor.hips=Node("Photo Hips - 构图下沿",actorRoot,new Vector3(0,.83f,0));actor.feet=Node("Photo Feet - 人物尺寸",actorRoot,new Vector3(0,.08f,0));
            var visual=Node("Visual - 临时人形（可替换）",actorRoot,Vector3.zero);actor.visual=visual;
            Part("Torso",visual,new Vector3(0,1.18f,0),new Vector3(.44f,.31f,.28f),coat,PrimitiveType.Capsule);
            Part("Hips",visual,new Vector3(0,.88f,0),new Vector3(.36f,.26f,.25f),trousers,PrimitiveType.Sphere);
            Part("Neck",visual,new Vector3(0,1.49f,0),new Vector3(.12f,.14f,.12f),skin,PrimitiveType.Capsule);
            Part("Head",visual,new Vector3(0,1.66f,0),new Vector3(.25f,.29f,.25f),skin,PrimitiveType.Sphere);
            Part("Hair",visual,new Vector3(0,1.76f,-.025f),new Vector3(.26f,.14f,.25f),hair,PrimitiveType.Sphere);
            Part("Nose",visual,new Vector3(0,1.65f,.126f),new Vector3(.046f,.06f,.045f),skin,PrimitiveType.Sphere);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Eye",visual,new Vector3(side*.055f,1.69f,.115f),Vector3.one*.018f,hair,PrimitiveType.Sphere);
                var leg=Node(side<0?"Left Leg":"Right Leg",visual,new Vector3(side*.11f,.87f,0));
                Part("Trouser leg",leg,new Vector3(0,-.34f,0),new Vector3(.14f,.33f,.16f),trousers,PrimitiveType.Capsule);
                Part("Shoe",leg,new Vector3(0,-.79f,.045f),new Vector3(.17f,.12f,.27f),hair,PrimitiveType.Cube);
                var arm=Node(side<0?"Left Arm":"Right Arm",visual,new Vector3(side*.25f,1.41f,0));
                Part("Sleeve",arm,new Vector3(0,-.23f,0),new Vector3(.13f,.25f,.14f),coat,PrimitiveType.Capsule);
                Part("Hand",arm,new Vector3(0,-.52f,.01f),new Vector3(.11f,.14f,.1f),skin,PrimitiveType.Sphere);
                if(side<0){actor.leftLeg=leg;actor.leftArm=arm;}else{actor.rightLeg=leg;actor.rightArm=arm;}
            }
            Part("Scarf neck",visual,new Vector3(0,1.47f,0),new Vector3(.27f,.12f,.27f),scarf,PrimitiveType.Cube);
            Part("Scarf end",visual,new Vector3(.09f,1.25f,.15f),new Vector3(.11f,.38f,.04f),scarf,PrimitiveType.Cube);
            actor.footsteps=actorRoot.gameObject.AddComponent<AudioSource>();actor.footsteps.spatialBlend=1;actor.footsteps.rolloffMode=AudioRolloffMode.Linear;actor.footsteps.minDistance=1;actor.footsteps.maxDistance=14;actor.footsteps.playOnAwake=false;actor.footstep=Sound("Footstep",0);
            var player=Node("Player - 第一人称与拍照",root.transform,spawn.position);player.rotation=spawn.rotation;
            var motor=player.gameObject.AddComponent<CharacterController>();motor.height=1.8f;motor.radius=.25f;motor.center=new Vector3(0,.9f,0);motor.stepOffset=.3f;motor.skinWidth=.025f;
            Undo.SetTransformParent(camera.transform,player,"设置公园第一人称镜头");Undo.RecordObject(camera.transform,"设置玩家眼睛位置");camera.transform.localPosition=new Vector3(0,1.65f,0);camera.transform.localRotation=Quaternion.identity;
            Undo.RecordObject(camera,"设置公园取景镜头");camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=200;
            var focus=Undo.AddComponent<ParkPhotoFocus>(camera.gameObject);focus.template=focusMaterial;
            var prototype=player.gameObject.AddComponent<ParkPhotoPrototype>();prototype.view=camera;prototype.spawn=spawn;prototype.companion=actor;prototype.focusEffect=focus;
            prototype.sound=player.gameObject.AddComponent<AudioSource>();prototype.sound.playOnAwake=false;prototype.shutter=Sound("Shutter",1);prototype.callSound=Sound("Call",2);actor.player=player;
            if(camera.GetComponent<AudioListener>()==null)Undo.AddComponent<AudioListener>(camera.gameObject);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=player.gameObject;
            return "已在现有 ParkScene 中保存跟随拍照原型；环境布局保留。未运行 Play。";
        }
        static Transform Node(string name,Transform parent,Vector3 local)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=local;return go.transform;}
        static void Part(string name,Transform parent,Vector3 p,Vector3 size,Material material,PrimitiveType shape)
        {var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
        static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');EnsureFolder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
        static Material Material(string name,Color color)
        {
            string path=Folder+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;
            var template=Resources.Load<Material>("WhiteboxMaterial");mat=template!=null?new Material(template):new Material(Shader.Find("Standard"));mat.color=color;
            if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.1f);AssetDatabase.CreateAsset(mat,path);return mat;
        }
        static AudioClip Sound(string name,int kind)
        {
            string path=Folder+"/Audio/"+name+".wav";
            if(!File.Exists(path))using(var file=new BinaryWriter(File.Create(path)))
            {
                const int rate=22050;int count=kind==2?rate/3:rate/8;var random=new System.Random(32+kind);
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+count*2);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(rate);file.Write(rate*2);file.Write((short)2);file.Write((short)16);file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    double t=i/(double)rate;double sample;
                    if(kind==2)sample=Math.Sin(t*2*Math.PI*(t<.14?520:660))*Math.Sin(Math.PI*i/count)*.18;
                    else sample=((random.NextDouble()*2-1)*.45+Math.Sin(t*2*Math.PI*(kind==0?95:1700))*.3)*Math.Exp(-t*(kind==0?30:55));
                    file.Write((short)(Math.Max(-1,Math.Min(1,sample))*14000));
                }
            }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
