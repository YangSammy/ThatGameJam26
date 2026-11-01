using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace MemoryWhitebox.Editor
{
    public static class DualDeckV6Build
    {
        const string Folder="Assets/Art/Props/Radio/DualDeckV6";
        [Serializable] class MaterialList {public MaterialInfo[] materials;}
        [Serializable] class MaterialInfo {public string name,alpha,texture;public float[] color;public float metallic,roughness;}
        static Transform Find(Transform t,string name)=>t.GetComponentsInChildren<Transform>(true).First(x=>x.name==name);
        public static string Build()
        {
            if(Application.isPlaying)throw new Exception("Stop Play first");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Radio_DualDeck.prefab")!=null)return "Existing prefab retained. Use the export staging workflow for an explicit rebuild.";
            Directory.CreateDirectory(Folder+"/Animations");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Import/Radio_DualDeck.fbx");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(imported);model.name="Radio_DualDeck_ROOT";
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var clips=new List<AnimationClip>();
            // Only manifest-declared object paths are retained. FBX rest curves must not reset other channels.
            foreach(var file in Directory.GetFiles(Folder+"/Import","*.fbx"))
            {
                string name=Path.GetFileNameWithoutExtension(file);if(name=="Radio_DualDeck")continue;
                var source=AssetDatabase.LoadAllAssetsAtPath(file.Replace('\\','/')).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                var targets=DualDeckClipBindings.Targets(name);
                var clip=new AnimationClip{name=name,frameRate=24,legacy=true};
                foreach(var binding in AnimationUtility.GetCurveBindings(source))
                {
                    string node=binding.path.Split('/').Last();
                    if(targets.Contains(node))AnimationUtility.SetEditorCurve(clip,binding,AnimationUtility.GetEditorCurve(source,binding));
                }
                if(AnimationUtility.GetCurveBindings(clip).Length==0)throw new Exception("No bindings: "+name);
                clip.EnsureQuaternionContinuity();AssetDatabase.CreateAsset(clip,Folder+"/Animations/"+name+".anim");clips.Add(clip);
            }
            var materials=new Dictionary<string,Material>();
            foreach(var info in JsonUtility.FromJson<MaterialList>(File.ReadAllText(Folder+"/materials.json")).materials)
            {
                var m=new Material(Shader.Find("Standard")){name=info.name,color=new Color(info.color[0],info.color[1],info.color[2],info.color[3])};
                m.SetFloat("_Metallic",info.metallic);m.SetFloat("_Glossiness",1-info.roughness);
                if(!string.IsNullOrEmpty(info.texture))m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/"+info.texture);
                if(info.alpha=="MASK"){m.SetFloat("_Mode",1);m.SetFloat("_Cutoff",.5f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
                if(info.alpha=="BLEND"){m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",1);m.SetInt("_DstBlend",10);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;}
                AssetDatabase.CreateAsset(m,Folder+"/Materials/"+info.name+".mat");materials[info.name]=m;
            }
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>materials.ContainsKey(m.name)?materials[m.name]:m).ToArray();
            var visual=model.AddComponent<DualDeckVisual>();visual.clips=clips.ToArray();visual.tapeA=Find(model.transform,"Deck_A_Tape_ROOT");visual.tapeB=Find(model.transform,"Deck_B_Tape_ROOT");
            var hits=new List<Collider>();var commands=new List<string>();
            foreach(string deck in new[]{"A","B"})
            {
                foreach(string key in new[]{"Play","Rewind","Forward","Stop","Pause","Eject","Record"})
                {
                    var keyNode=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Deck_"+deck+"_Key_"+key);if(keyNode==null)continue;
                    var col=keyNode.gameObject.AddComponent<BoxCollider>();
                    var points=keyNode.GetComponentsInChildren<MeshFilter>().SelectMany(m=>new[]{m.transform.TransformPoint(m.sharedMesh.bounds.min),m.transform.TransformPoint(m.sharedMesh.bounds.max)}).Select(p=>keyNode.InverseTransformPoint(p)).ToArray();
                    var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);col.center=b.center;col.size=b.size+Vector3.one*.000025f;
                    keyNode.gameObject.layer=25;hits.Add(col);commands.Add(deck+"_"+key);
                }
                var door=Find(model.transform,"Deck_"+deck+"_Door_HINGE");
                var panel=door.GetComponentsInChildren<MeshRenderer>().First(r=>!r.transform.IsChildOf(deck=="A"?visual.tapeA:visual.tapeB));
                var doorHit=panel.gameObject.AddComponent<BoxCollider>();panel.gameObject.layer=25;hits.Add(doorHit);commands.Add(deck+"_Door");
            }
            visual.keyHits=hits.ToArray();visual.keyCommands=commands.ToArray();
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,Folder+"/Radio_DualDeck.prefab");Object.DestroyImmediate(model);
            AssetDatabase.SaveAssets();return "Created "+clips.Count+" sparse clips and prefab";
        }
        public static string Install()
        {
            if(Application.isPlaying)throw new Exception("Stop Play first");
            var d=Object.FindObjectOfType<HouseRadioDeck>();if(d.dualDeck!=null)throw new Exception("Already installed");
            var binding=d.GetComponent<RadioPlacementBinding>();var old=binding.model;
            var oldBounds=old.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Radio_DualDeck.prefab"),d.transform);
            instance.name="Radio_DualDeck_ROOT";
            // Imported FBX axis conversion lives on its root. Keep it and apply the old tabletop yaw only.
            instance.transform.rotation=Quaternion.Euler(0,old.eulerAngles.y+180,0)*instance.transform.rotation;
            var nb=instance.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            instance.transform.position+=new Vector3(oldBounds.center.x-nb.center.x,oldBounds.min.y-nb.min.y,oldBounds.center.z-nb.center.z);
            d.dualDeck=instance.GetComponent<DualDeckVisual>();
            foreach(var r in old.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var c in old.GetComponentsInChildren<Collider>(true))c.enabled=false;
            d.animator.enabled=false;
            foreach(var r in d.recordingPanel.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var c in d.recordingPanel.GetComponentsInChildren<Collider>(true))c.enabled=false;
            binding.model=instance.transform;binding.rearInteractionDepth=.04f;binding.Sync();
            PlaceSlot(d.sourceSlot,Find(instance.transform,"Deck_A_TapeAboveSlot"));
            PlaceSlot(d.slotHit,Find(instance.transform,"Deck_B_TapeAboveSlot"));
            d.sourceSocket.SetPositionAndRotation(Find(instance.transform,"Deck_A_TapeSeated").position,Find(instance.transform,"Deck_A_TapeSeated").rotation);
            d.radioHit.enabled=true;
            EditorUtility.SetDirty(d);EditorUtility.SetDirty(binding);
            EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);return "Installed on "+d.name;
        }
        static void PlaceSlot(Collider col,Transform target)
        {
            col.transform.SetPositionAndRotation(target.position,target.rotation);col.enabled=true;col.isTrigger=true;
            var box=col as BoxCollider;if(box!=null){box.center=Vector3.zero;var s=box.transform.lossyScale;box.size=new Vector3(.14f/Mathf.Abs(s.x),.08f/Mathf.Abs(s.y),.065f/Mathf.Abs(s.z));}
        }
        public static string UpdateInspection()
        {
            var d=Object.FindObjectOfType<HouseRadioDeck>();var viewer=d.house.safeInspection;
            var old=viewer.subjects.First(s=>s.source==d.transform);
            var subject=SafeInspectionBuild.CreateAuthoredSubject(d.transform,old.transform.parent,"Radio");
            subject.mainFaceFrame=d.dualDeck.transform;subject.mainFaceNormal=Vector3.down;subject.mainFaceUp=Vector3.forward;
            foreach(var command in d.dualDeck.keyCommands.Where(c=>!c.EndsWith("Door")))
            {
                var node=Find(d.dualDeck.transform,"Deck_"+command[0]+"_Key_"+command.Substring(2));
                var clip=d.dualDeck.clips.First(c=>c.name==command[0]+"_Key_"+command.Substring(2)+"_Latch");
                var home=node.localPosition;clip.SampleAnimation(d.dualDeck.gameObject,0);var start=node.localPosition;
                clip.SampleAnimation(d.dualDeck.gameObject,clip.length);var delta=node.localPosition-start;node.localPosition=home;
                var pair=subject.poses.First(p=>p.source==node);var part=pair.display.gameObject.AddComponent<SafeInspectionPart>();part.label=command.Replace('_',' ');part.motion=SafeInspectionPart.Motion.Button;part.axis=delta.normalized;part.travel=delta.magnitude;
            }
            foreach(string prefix in new[]{"A","B"})
            {
                var node=Find(d.dualDeck.transform,"Deck_"+prefix+"_Door_HINGE");var home=node.localRotation;
                var clip=d.dualDeck.clips.First(c=>c.name==prefix+"_Open");clip.SampleAnimation(d.dualDeck.gameObject,0);var start=node.localRotation;
                clip.SampleAnimation(d.dualDeck.gameObject,clip.length);var delta=node.localRotation*Quaternion.Inverse(start);node.localRotation=home;
                float angle;Vector3 axis;delta.ToAngleAxis(out angle,out axis);
                var part=subject.poses.First(p=>p.source==node).display.gameObject.AddComponent<SafeInspectionPart>();part.label=prefix+" door";part.motion=SafeInspectionPart.Motion.Hinge;part.axis=axis;part.range=angle;
            }
            subject.parts=subject.GetComponentsInChildren<SafeInspectionPart>(true);subject.surfaces=subject.GetComponentsInChildren<Collider>(true);subject.RefreshFraming();
            viewer.subjects=viewer.subjects.Select(s=>s==old?subject:s).ToArray();var entry=d.GetComponent<InspectionEntry>();if(entry!=null){entry.subject=subject;EditorUtility.SetDirty(entry);}Object.DestroyImmediate(old.gameObject);EditorUtility.SetDirty(viewer);
            EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);return "Updated inspection buttons and independent hinges";
        }
    }
}
