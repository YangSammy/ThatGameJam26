using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    public static class SafeInspectionBuild
    {
        const string Folder="Assets/Art/Interaction/Inspection";
        static AudioClip click;
        static Material wear,paper;
        static SafeInspection viewer;
        static Transform library;
        static readonly List<SafeInspectionSubject> subjects=new List<SafeInspectionSubject>();
        [MenuItem("Tools/Inspection/Install Scene Inspection")]
        public static string Install()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var house=Object.FindObjectOfType<WBMain>();
            if(house==null || house.playerCamera==null || house.radioDeck==null)
                throw new InvalidOperationException("House player camera and radio must be assigned.");
            if(house.safeInspection!=null)return "Already installed; authored objects preserved";
            var scene=house.gameObject.scene;
            wear=Material("Worn edges",new Color(.56f,.49f,.37f));paper=Material("Aged label",new Color(.77f,.72f,.59f));click=Contact();
            var camera=house.playerCamera;
            viewer=camera.gameObject.AddComponent<SafeInspection>();viewer.view=camera;
            library=new GameObject("Safe inspection - authored display models").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(library.gameObject,scene);
            var overlay=new GameObject("Safe inspection camera");overlay.transform.SetParent(camera.transform,false);
            viewer.displayCamera=overlay.AddComponent<Camera>();viewer.displayCamera.clearFlags=CameraClearFlags.Depth;
            viewer.displayCamera.cullingMask=1<<SafeInspection.DisplayLayer;viewer.displayCamera.depth=camera.depth+1;
            viewer.displayCamera.nearClipPlane=.03f;viewer.displayCamera.farClipPlane=30;viewer.displayCamera.enabled=false;
            viewer.inspectionLight=overlay.AddComponent<Light>();viewer.inspectionLight.type=LightType.Point;
            viewer.inspectionLight.range=4;viewer.inspectionLight.intensity=.12f;viewer.inspectionLight.color=new Color(1,.93f,.82f);
            viewer.inspectionLight.cullingMask=1<<SafeInspection.DisplayLayer;viewer.inspectionLight.enabled=false;
            viewer.feedback=overlay.AddComponent<AudioSource>();viewer.feedback.playOnAwake=false;viewer.feedback.spatialBlend=0;viewer.feedback.volume=.25f;
            house.safeInspection=viewer;subjects.Clear();
            var entries=house.houseShell.transform.Find("Study entry props");
            foreach(Transform source in entries)
            {
                var entry=source.GetComponent<StudyEntry>();
                if(entry!=null && entry.chapter>0 && entry.chapter<5)PrepareEntry(source,entry);
            }
            viewer.subjects=subjects.ToArray();
            RefreshRadioSubjects(house.radioDeck.transform,house.radioDeck.tapes[0].root.parent);
            EditorUtility.SetDirty(house);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();return "Installed "+viewer.subjects.Length+" inspection subjects.";
        }
        static Material Material(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m!=null)return m;m=new Material(Resources.Load<Material>("WhiteboxMaterial"));m.color=color;AssetDatabase.CreateAsset(m,path);return m;
        }
        static AudioClip Contact()
        {
            string path=Folder+"/MechanicalContact.wav";
            if(!File.Exists(path))using(var file=new BinaryWriter(File.Create(path)))
            {
                int count=2205;file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));file.Write(36+count*2);file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16);file.Write((short)1);file.Write((short)1);file.Write(44100);file.Write(88200);file.Write((short)2);file.Write((short)16);
                file.Write(System.Text.Encoding.ASCII.GetBytes("data"));file.Write(count*2);
                for(int i=0;i<count;i++){double t=i/44100.0;file.Write((short)(6000*Math.Exp(-t*110)*(Math.Sin(t*2600)+.3*Math.Sin(t*6900))));}
            }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
        }
        static void PrepareEntry(Transform source,StudyEntry entry)
        {
            Transform partA=null,partB=null;string title=source.name;
            bool fixedDevice=source.name.StartsWith("Radio");
            Vector3 angles=entry.chapter==1 || entry.chapter==4 || fixedDevice?Vector3.zero:new Vector3(-90,0,0);
            if(fixedDevice){title="收音机";partA=source.Find("Tuning dial");Box(source,"Dial finger wear",new Vector3(.2f,.12f,-.155f),new Vector3(.05f,.004f,.002f),wear);}
            else if(entry.chapter==0){title="桌面磁带";}
            else if(entry.chapter==1)
            {
                title="胶片相机";
                partA=Box(source,"Back inspection hinge",new Vector3(0,.14f,.108f),new Vector3(.38f,.25f,.016f),wear);
                partB=Box(source,"Shutter inspection button",new Vector3(.14f,.29f,0),new Vector3(.045f,.025f,.045f),paper);
                Box(source,"Grip polished edge",new Vector3(.201f,.14f,0),new Vector3(.003f,.15f,.06f),wear);
                CameraChamber(source);
            }
            else if(entry.chapter==2)
            {
                title="书写纸与钢笔";
                partA=Box(source,"Pen cap",new Vector3(.2f,.025f,.16f),new Vector3(.025f,.025f,.075f),wear);
                for(int i=0;i<3;i++)Box(source,"Old paper crease",new Vector3(-.09f+i*.018f,.009f,0),new Vector3(.0015f,.001f,.36f),wear);
            }
            else if(entry.chapter==3){title="绘画本";partA=source.Find("Pages");Box(source,"Cover corner abrasion",new Vector3(.23f,.026f,.195f),new Vector3(.04f,.002f,.04f),wear);}
            else if(entry.chapter==4)
            {
                title="围巾";partA=source.Find("Short scarf end");
                for(int i=0;i<5;i++)Box(source,"Hem repair stitch",new Vector3(.08f+i*.035f,.73f,-.12f),new Vector3(.018f,.004f,.002f),paper);
            }
            var subject=Copy(source,title,false,angles);subject.entry=entry;
            if(fixedDevice)Part(subject,partA,"调谐旋钮（仅检查转动）",SafeInspectionPart.Motion.Dial,Vector3.forward,240);
            else if(entry.chapter==0)
            {
                var reels=source.Cast<Transform>().Where(x=>x.name=="Cassette reel").Take(2).ToArray();
                for(int i=0;i<reels.Length;i++)Part(subject,reels[i],i==0?"左卷轴":"右卷轴",SafeInspectionPart.Motion.Dial,Vector3.up,300);
            }
            else if(entry.chapter==1)
            {
                Part(subject,partA,"相机后盖（仅检查铰链）",SafeInspectionPart.Motion.Hinge,Vector3.up,-110,0,new Vector3(-.19f,0,0));
                Part(subject,partB,"快门按钮（不会拍摄）",SafeInspectionPart.Motion.Button,Vector3.down,0,.012f);
            }
            else if(entry.chapter==2)Part(subject,partA,"笔帽（轻推查看）",SafeInspectionPart.Motion.Button,Vector3.forward,0,.08f);
            else if(entry.chapter==3)Part(subject,partA,"翻起纸页",SafeInspectionPart.Motion.Hinge,Vector3.forward,130,0,new Vector3(-.24f,0,0));
            else if(entry.chapter==4)Part(subject,partA,"掀起围巾短端",SafeInspectionPart.Motion.Hinge,Vector3.right,-55,0,new Vector3(0,.42f,0));
            Finish(subject);
        }
        public static void RefreshRadioSubjects(Transform radio,Transform tapes)
        {
            viewer=Object.FindObjectOfType<SafeInspection>();
            if(viewer==null)return;
            library=GameObject.Find("Safe inspection - authored display models").transform;
            click=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/MechanicalContact.wav");
            var deck=radio.GetComponent<HouseRadioDeck>();
            var sources=new[]{radio}.Concat(deck.tapes.Select(t=>t.root)).ToArray();
            var keep=viewer.subjects.Where(s=>s!=null && s.source!=radio && s.source!=tapes && !s.source.IsChildOf(tapes)).ToList();
            foreach(var old in viewer.subjects.Where(s=>s!=null && (s.source==radio || s.source==tapes || s.source.IsChildOf(tapes))).ToArray())Object.DestroyImmediate(old.gameObject);
            subjects.Clear();
            foreach(var source in sources)
            {
                bool device=source==radio;
                // Radio and tapes share the portable display viewer; gameplay camera anchors are independent.
                var subject=Copy(source,device?"收录机":source.name,false,Vector3.zero,source.Cast<Transform>().ToList());
                subject.entry=source.GetComponentInParent<StudyEntry>();
                foreach(var t in source.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="Tuning_KNOB_PIVOT")Part(subject,t,"调谐旋钮",SafeInspectionPart.Motion.Dial,Vector3.forward,240);
                    if(device && t.name=="Key_Play_PIVOT")Part(subject,t,"播放键（仅检查回弹）",SafeInspectionPart.Motion.Button,Vector3.forward,0,.003f/Mathf.Max(.001f,Mathf.Abs(t.lossyScale.z)));
                    if(!device && t.name.Contains("Reel_") && t.name.EndsWith("PIVOT"))Part(subject,t,"磁带卷轴",SafeInspectionPart.Motion.Dial,Vector3.up,-300);
                }
                // Imported cassette shells have holes; a single solid bounds box hides both spindles.
                foreach(var pair in subject.renderers)
                {
                    var filter=pair.display.GetComponent<MeshFilter>();if(filter==null)continue;
                    var old=pair.display.GetComponent<Collider>();if(old!=null)Object.DestroyImmediate(old);
                    var mesh=pair.display.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=filter.sharedMesh;mesh.enabled=pair.source.enabled;
                }
                foreach(var part in subject.GetComponentsInChildren<SafeInspectionPart>(true))
                {
                    var touch=part.gameObject.AddComponent<SphereCollider>();touch.radius=.009f/Mathf.Max(.001f,Mathf.Abs(part.transform.lossyScale.x));
                }
                Finish(subject);
                if(device)ConfigureRadioFraming(subject,deck);
                keep.Add(subject);
            }
            viewer.subjects=keep.ToArray();
        }
        public static void ConfigureRadioFraming(SafeInspectionSubject subject,HouseRadioDeck deck)
        {
            ConfigureMainFace(subject);
            var toRoot=Quaternion.Inverse(subject.source.rotation);
            subject.centerOffset=Vector3.zero;subject.RefreshFraming();
            // Center on the casing rather than the raised carrying handle.
            foreach(var pair in subject.renderers)
                if(pair.source!=null && pair.source.name=="Boombox_Radio_WarmCream")
                {
                    var mesh=pair.source.GetComponent<MeshFilter>();
                    var bodyCenter=mesh!=null && mesh.sharedMesh!=null?pair.source.transform.TransformPoint(mesh.sharedMesh.bounds.center):pair.source.bounds.center;
                    subject.centerOffset=toRoot*(bodyCenter-subject.source.position)-subject.localCenter;
                    break;
                }
            subject.RefreshFraming();
        }
        public static void ConfigureMainFace(SafeInspectionSubject subject)
        {
            if(subject==null || subject.source==null)return;
            var source=subject.source;
            var deck=source.GetComponent<HouseRadioDeck>();
            Transform frame=source;Vector3 normal=Vector3.back,up=Vector3.up,tilt=Vector3.zero;
            var shell=source.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Tape_") && t.name.EndsWith("_Shell"));
            if(deck!=null && deck.animator!=null)
            {frame=deck.animator.transform;normal=Vector3.down;up=Vector3.back;}
            else if(shell!=null)
            {frame=shell;normal=Vector3.down;up=Vector3.back;}
            else if(source.Find("Paper")!=null)
            {frame=source.Find("Paper");normal=Vector3.up;up=Vector3.forward;}
            else if(source.Find("Pages")!=null)
            {
                // Use the book root, not the movable page hinge, as the stable observation frame.
                normal=Vector3.up;up=Vector3.forward;
            }
            else if(source.Find("Lens")!=null)
            {tilt=new Vector3(-12,20,0);}
            else if(source.Find("Long scarf end")!=null)
            {normal=Vector3.back;up=Vector3.up;}
            else if(subject.entry!=null && subject.entry.chapter==0 && !source.name.StartsWith("Radio"))
            {normal=Vector3.up;up=Vector3.forward;}
            subject.mainFaceFrame=frame;subject.mainFaceNormal=normal;subject.mainFaceUp=up;subject.mainFaceTilt=tilt;
            subject.displayEuler=Vector3.zero;
        }
        [MenuItem("Tools/检视/统一当前场景物品观察面")]
        public static void ApplyMainFacesInCurrentScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var v in Object.FindObjectsOfType<SafeInspection>(true))
            {
                if(v.gameObject.scene!=scene || v.subjects==null)continue;
                foreach(var subject in v.subjects)
                {
                    if(subject==null)continue;
                    Undo.RecordObject(subject,"统一物品观察面");ConfigureMainFace(subject);EditorUtility.SetDirty(subject);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
        static SafeInspectionSubject Copy(Transform source,string title,bool fixedDevice,Vector3 angles,List<Transform> selected=null)
        {
            var root=new GameObject("Inspect - "+title).transform;root.SetParent(library,false);
            var subject=root.gameObject.AddComponent<SafeInspectionSubject>();subject.title=title;subject.source=source;subject.displayRoot=root;subject.fixedDevice=fixedDevice;subject.displayEuler=angles;
            var poses=new List<SafeInspectionSubject.PosePair>();var renders=new List<SafeInspectionSubject.RenderPair>();
            var content=new GameObject(source.name).transform;content.SetParent(root,false);content.localScale=source.lossyScale;subject.content=content;
            poses.Add(new SafeInspectionSubject.PosePair{source=source,display=content});
            if(selected==null)
            {
                CopyRenderer(source,content,renders);
                foreach(Transform child in source)CopyChild(child,content,poses,renders);
            }
            else foreach(var child in selected)CopyChild(child,content,poses,renders);
            subject.poses=poses.ToArray();subject.renderers=renders.ToArray();
            var bounds=new Bounds();bool first=true;
            foreach(var pair in renders){if(first){bounds=pair.display.bounds;first=false;}else bounds.Encapsulate(pair.display.bounds);}
            subject.localCenter=bounds.center;subject.diameter=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            // Root is at identity during authoring, so bounds center is already in display-root coordinates.
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=SafeInspection.DisplayLayer;
            subjects.Add(subject);return subject;
        }
        public static SafeInspectionSubject CreateAuthoredSubject(Transform source,Transform container,string title)
        {
            var previous=library;SafeInspectionSubject result=null;
            try
            {
                library=container;result=Copy(source,title,false,Vector3.zero);
                result.entry=source.GetComponentInParent<StudyEntry>();Finish(result);return result;
            }
            finally{library=previous;if(result!=null)subjects.Remove(result);}
        }
        static void CopyChild(Transform source,Transform parent,List<SafeInspectionSubject.PosePair> poses,List<SafeInspectionSubject.RenderPair> renders)
        {
            var entry=source.parent!=null?source.parent.GetComponent<InspectionEntry>():null;
            if(entry!=null && entry.pickup!=null && entry.pickup.transform==source)return;
            if(source.GetComponent<SafeInspectionSubject>()!=null)return;
            var target=new GameObject(source.name).transform;target.SetParent(parent,false);target.localPosition=source.localPosition;target.localRotation=source.localRotation;target.localScale=source.localScale;
            poses.Add(new SafeInspectionSubject.PosePair{source=source,display=target});CopyRenderer(source,target,renders);
            foreach(Transform child in source)CopyChild(child,target,poses,renders);
            target.gameObject.SetActive(source.gameObject.activeSelf);
        }
        static void CopyRenderer(Transform source,Transform target,List<SafeInspectionSubject.RenderPair> renders)
        {
            var mesh=source.GetComponent<MeshFilter>();var original=source.GetComponent<MeshRenderer>();var text=source.GetComponent<TextMesh>();
            if(text!=null)
            {
                var copy=target.gameObject.AddComponent<TextMesh>();copy.text=text.text;copy.font=text.font;copy.fontSize=text.fontSize;copy.characterSize=text.characterSize;copy.anchor=text.anchor;copy.alignment=text.alignment;copy.color=text.color;
                var r=target.GetComponent<Renderer>();r.sharedMaterials=original.sharedMaterials;r.enabled=original.enabled;renders.Add(new SafeInspectionSubject.RenderPair{source=original,display=r});return;
            }
            if(mesh==null || original==null || mesh.sharedMesh==null)return;
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
            var renderer=target.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;renderer.enabled=original.enabled;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var collider=target.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;collider.enabled=original.enabled;
            renders.Add(new SafeInspectionSubject.RenderPair{source=original,display=renderer});
        }
        static void Part(SafeInspectionSubject subject,Transform source,string label,SafeInspectionPart.Motion motion,Vector3 axis,float range,float travel=0,Vector3 pivot=default(Vector3))
        {
            var pair=subject.poses.First(x=>x.source==source);var part=pair.display.gameObject.AddComponent<SafeInspectionPart>();part.label=label;part.motion=motion;part.axis=axis;part.range=range;part.travel=travel;part.pivotOffset=pivot;part.contact=click;
        }
        static void Finish(SafeInspectionSubject subject)
        {
            ConfigureMainFace(subject);
            subject.parts=subject.GetComponentsInChildren<SafeInspectionPart>(true);subject.surfaces=subject.GetComponentsInChildren<Collider>(true);
            subject.displayRoot.gameObject.SetActive(false);
        }
        static void CameraChamber(Transform source)
        {
            if(source.Find("Camera chamber floor")!=null)return;
            var shell=source.Find("Camera body");var dark=shell.GetComponent<Renderer>().sharedMaterial;
            shell.GetComponent<Renderer>().enabled=false;
            Box(source,"Camera chamber floor",new Vector3(0,.14f,-.075f),new Vector3(.4f,.28f,.05f),dark);
            foreach(float x in new[]{-.19f,.19f})Box(source,"Camera chamber side "+x,new Vector3(x,.14f,.012f),new Vector3(.02f,.28f,.15f),dark);
            foreach(float y in new[]{.01f,.27f})Box(source,"Camera chamber rim "+y,new Vector3(0,y,.012f),new Vector3(.38f,.02f,.15f),dark);
            Box(source,"Empty exposure frame",new Vector3(0,.14f,-.04f),new Vector3(.16f,.12f,.01f),wear);
            foreach(float x in new[]{-.13f,.13f})Box(source,"Empty spool post "+x,new Vector3(x,.14f,.01f),new Vector3(.025f,.19f,.025f),paper);
        }
    }
}
