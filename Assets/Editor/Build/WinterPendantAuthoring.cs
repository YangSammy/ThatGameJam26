using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    public static class WinterPendantAuthoring
    {
        const string Folder="Assets/Art/Props/AlbumWinterE/Materials/Discovery";
        static Material Ink(string name,Color color)
        {
            var path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Unlit/Color"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;EditorUtility.SetDirty(material);return material;
        }
        static Transform Node(Transform parent,string name,Vector3 position)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.gameObject.layer=parent.gameObject.layer;return t;
        }
        static Material DoorWood()
        {
            var path=Folder+"/DoorWood.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Props/AlbumWinterE/Materials/M_E_Wood.mat"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap",material.mainTexture);
            material.SetColor("_EmissionColor",new Color(.65f,.65f,.65f));
            EditorUtility.SetDirty(material);return material;
        }
        static Transform Panel(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=parent.gameObject.layer;
            go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        public static string Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var s=SceneManager.GetSceneByName("Bedroom_Winter");
            if(!s.isLoaded || s.isDirty)throw new InvalidOperationException("Open clean winter scene first.");
            var w=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WinterAlbumWorkspace>(true)).Single();
            if(w.observedSuitcase.Find("PendantPocket")!=null)throw new InvalidOperationException("Already authored; adjust the existing scene objects.");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/Props/AlbumWinterE/Materials","Discovery");
            var cloth=Ink("PocketCloth",new Color(.23f,.24f,.21f));
            var lining=Ink("PocketLining",new Color(.12f,.14f,.13f));
            var seam=Ink("PocketSeam",new Color(.37f,.35f,.28f));
            var metal=Ink("PendantMetal",new Color(.52f,.46f,.29f));
            var enamel=Ink("PendantEnamel",new Color(.25f,.43f,.36f));
            var face=Ink("DoorRecess",new Color(.045f,.052f,.048f));
            var wood=DoorWood();
            var frame=Ink("DoorFrame",new Color(.48f,.44f,.34f));
            var pocket=Node(w.observedSuitcase,"PendantPocket",new Vector3(.10f,.30f,-.155f));
            Panel(pocket,"InnerLining",new Vector3(0,.025f,.008f),new Vector3(.32f,.21f,.014f),lining);
            var flap=Panel(pocket,"OpenPocketFlap",new Vector3(0,-.035f,-.042f),new Vector3(.33f,.14f,.018f),cloth);
            flap.localRotation=Quaternion.Euler(-18,0,0);
            Panel(pocket,"LeftSeam",new Vector3(-.16f,-.03f,-.02f),new Vector3(.012f,.14f,.055f),seam);
            Panel(pocket,"RightSeam",new Vector3(.16f,-.03f,-.02f),new Vector3(.012f,.14f,.055f),seam);
            w.observedPendant.SetParent(pocket,false);w.observedPendant.localPosition=new Vector3(.025f,.015f,-.016f);
            w.observedPendant.localRotation=Quaternion.Euler(0,180,-12);w.observedPendant.localScale=Vector3.one*2.2f;
            foreach(var r in w.observedPendant.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=r.name.Contains("PendantBody")?enamel:metal;
            // The chain belongs to the same discovered object and disappears with it.
            var chain=Node(w.observedPendant,"VisibleChain",Vector3.zero).gameObject.AddComponent<LineRenderer>();
            chain.useWorldSpace=false;chain.widthMultiplier=.0008f;chain.sharedMaterial=metal;chain.positionCount=5;
            chain.SetPositions(new[]{new Vector3(0,.05f,0),new Vector3(.012f,.063f,0),new Vector3(.026f,.057f,0),new Vector3(.037f,.039f,0),new Vector3(.042f,.02f,0)});
            chain.numCornerVertices=3;chain.numCapVertices=2;
            // A forgiving, shallow hit region behind the flap: the flap remains a real occluder.
            w.pendantFocus.transform.localPosition=new Vector3(0,.035f,0);
            w.pendantFocus.transform.localRotation=Quaternion.identity;w.pendantFocus.transform.localScale=Vector3.one/2.2f;
            w.pendantFocus.size=new Vector3(.14f,.14f,.024f);w.pendantFocus.center=Vector3.zero;
            w.pagePendantAnchor=Node(w.paperCarrier,"PagePendantAnchor",new Vector3(-.49f,-.16f,-.055f));
            w.doorViewSize=1.65f;w.doorViewSeconds=.75f;w.doorViewOffset=new Vector2(-.35f,.12f);w.pendantDropRadius=.075f;
            var embedded=w.door.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="KeyVisual");
            foreach(var r in w.door.GetComponentsInChildren<Renderer>(true))
            {
                if(r.transform.IsChildOf(embedded))r.sharedMaterial=r.name.Contains("PendantBody")?enamel:metal;
                else if(r.name=="DoorHinge_Mesh")
                {
                    // E07 slot zero is the carved cavity, slot one is the wooden face.
                    var materials=r.sharedMaterials;materials[0]=face;materials[1]=wood;r.sharedMaterials=materials;
                }
                else if(r.name=="DoorFrame_Mesh")r.sharedMaterial=frame;
            }
            w.door.SetDoorState(1,0,0,true);
            var outline=Node(w.keySocket,"RecessOutline",Vector3.zero);w.recessOutline=outline;
            foreach(var source in embedded.GetComponentsInChildren<MeshFilter>(true))
            {
                var t=Node(outline,source.name,Vector3.zero);t.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                t.localScale=new Vector3(source.transform.lossyScale.x/outline.lossyScale.x,source.transform.lossyScale.y/outline.lossyScale.y,source.transform.lossyScale.z/outline.lossyScale.z);
                t.gameObject.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=metal;r.enabled=false;t.gameObject.AddComponent<HoverOutlineProxy>();
            }
            w.door.SetDoorState(0,0,0,false);
            EditorUtility.SetDirty(w);EditorSceneManager.MarkSceneDirty(s);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(s);
            return "Pocket, visible chain, page anchor, door materials and same-mesh recess outline saved.";
        }
    }
}
