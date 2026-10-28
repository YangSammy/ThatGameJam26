using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class TapeRecordingSceneAuthoring
    {
        const string ScenePath="Assets/Scenes/HouseScene.unity";
        const string MaterialFolder="Assets/Art/Props/Radio/Materials";
        [MenuItem("Tools/Memory Whitebox/Tape/Author Recording Parts In HouseScene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play before authoring scene parts.");
            var scene=SceneManager.GetSceneByPath(ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            var deck=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HouseRadioDeck>(true)).Single();
            Undo.RegisterFullObjectHierarchyUndo(deck.gameObject,"Author tape recording parts");
            var dark=Material("Tape_SourceDeck",new Color(.17f,.18f,.15f));
            var key=Material("Tape_Controls",new Color(.73f,.69f,.57f));
            var hole=Material("Tape_ProtectionSocket",new Color(.08f,.07f,.06f));
            var adhesive=Material("Tape_Adhesive",new Color(.76f,.66f,.43f));
            if(deck.recordingPanel==null)
            {
                var root=new GameObject("TapeRecording").transform;
                SceneManager.MoveGameObjectToScene(root.gameObject,scene);
                root.SetParent(deck.transform,false);
                root.localPosition=new Vector3(-.05f,.49f,-.36f);
                root.localRotation=Quaternion.Euler(0,180,0);
                root.localScale=Vector3.one*.85f;deck.recordingPanel=root;
                var source=Group(root,"SourceDeck_A");
                Cube(source,"CassetteWindow",new Vector3(0,.02f,0),new Vector3(.24f,.035f,.14f),dark);
                deck.sourceWheel=Cube(source,"SourceSpool",new Vector3(-.06f,.045f,0),new Vector3(.045f,.008f,.045f),key);
                deck.targetWheel=Cube(source,"TargetCounterWheel",new Vector3(.06f,.045f,0),new Vector3(.045f,.008f,.045f),key);
                var controls=Group(root,"TransportControls");
                string[] names={"SourcePlay","SourceStop","SourceRewind","TargetRecord","TargetPlay","TargetRewind","TargetFastForward","TargetStop","TargetRewindToStart","TargetClear","Submit"};
                string[] marks={">","[]","<<","S","F","J","L","'","<","X","O"};
                deck.keys3d=new Transform[11];deck.controlHits=new Collider[11];
                for(int i=0;i<11;i++)
                {
                    var part=Cube(controls,names[i],new Vector3((i%5-2)*.047f,.02f,.105f+(i/5)*.055f),new Vector3(.039f,.018f,.036f),key);
                    deck.keys3d[i]=part;deck.controlHits[i]=part.GetComponent<Collider>();
                    var label=Group(part,"Marking");label.localPosition=new Vector3(0,.55f,0);label.localRotation=Quaternion.Euler(90,0,0);
                    var text=label.gameObject.AddComponent<TextMesh>();text.text=marks[i];text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.22f;text.color=Color.black;
                }
            }
            if(deck.transport==null)
            {
                var transport=Group(deck.recordingPanel,"SampleTransport");
                deck.transport=transport.gameObject.AddComponent<TapeSampleTransport>();
                var audio=transport.GetComponent<AudioSource>();audio.playOnAwake=false;audio.loop=true;
            }
            var subjects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SafeInspectionSubject>(true)).ToArray();
            foreach(var cassette in deck.tapes)
            {
                var subject=subjects.Single(s=>s.source==cassette.root);
                if(cassette.preparation==null)
                {
                    var root=new GameObject("TapePreparation").transform;
                    SceneManager.MoveGameObjectToScene(root.gameObject,scene);
                    root.SetParent(cassette.root,false);
                    var binding=root.gameObject.AddComponent<TapePreparationBindings>();cassette.preparation=binding;
                    binding.socketMaterial=hole;binding.adhesiveMaterial=adhesive;binding.tearDistance=.02f;
                    string[] names={"ProtectionSocket_A","ProtectionSocket_B","AdhesiveDispenser","AdhesiveStrip"};
                    for(int i=0;i<4;i++)
                    {
                        var position=i==0?new Vector3(-.028f,.022f,-.012f):i==1?new Vector3(.028f,.022f,.012f):new Vector3(-.073f,-.012f,-.014f);
                        var scale=i==2?new Vector3(.018f,.015f,.01f):new Vector3(.014f,.009f,.003f);
                        var part=Cube(root,names[i],position,scale,i<2?hole:adhesive);
                        binding.pieces[i]=part.gameObject;
                    }
                    binding.pieces[3].SetActive(false);
                }
                if(subject.tapePreparation==null)
                {
                    var original=cassette.preparation;
                    var display=Object.Instantiate(original.gameObject,subject.content,false);display.name="TapePreparation_Display";
                    subject.tapePreparation=display.GetComponent<TapePreparationBindings>();
                    var poses=new List<SafeInspectionSubject.PosePair>(subject.poses);
                    var renderers=new List<SafeInspectionSubject.RenderPair>(subject.renderers);
                    poses.Add(new SafeInspectionSubject.PosePair{source=original.transform,display=display.transform});
                    for(int i=0;i<4;i++)
                    {
                        var from=original.pieces[i];var to=subject.tapePreparation.pieces[i];
                        poses.Add(new SafeInspectionSubject.PosePair{source=from.transform,display=to.transform});
                        renderers.Add(new SafeInspectionSubject.RenderPair{source=from.GetComponent<Renderer>(),display=to.GetComponent<Renderer>()});
                    }
                    subject.poses=poses.ToArray();subject.renderers=renderers.ToArray();
                    foreach(var t in display.GetComponentsInChildren<Transform>(true))t.gameObject.layer=subject.displayRoot.gameObject.layer;
                    EditorUtility.SetDirty(subject);
                }
            }
            EditorUtility.SetDirty(deck);
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene))throw new System.IO.IOException("HouseScene was not saved.");
            Selection.activeGameObject=deck.recordingPanel.gameObject;
        }
        static Transform Group(Transform parent,string name)
        {var go=new GameObject(name);go.layer=25;go.transform.SetParent(parent,false);return go.transform;}
        static Transform Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=25;
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static Material Material(string name,Color color)
        {
            string path=MaterialFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null)return material;
            material=new Material(Shader.Find("Standard")){name=name,color=color};AssetDatabase.CreateAsset(material,path);return material;
        }
    }
}
