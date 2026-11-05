using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    // Each image is a 90-degree view of the shipping scene, centred on one still
    // observation point. The actual album camera and clue colliders remain live.
    public static class AlbumObservationBaker
    {
        const string Folder="Assets/Art/Environments/AlbumObservations";
        const string Park="Assets/Scenes/ParkScene.unity";
        static readonly Vector3 parkEye=new Vector3(-.2f,1.75f,-3.8f);
        static readonly Vector3 roomEye=new Vector3(0,1.60f,2.48f);
        static readonly Quaternion parkHeading=Quaternion.Euler(0,32,0);
        static readonly Quaternion roomHeading=Quaternion.Euler(0,180,0);
        static readonly Quaternion[] faces={Quaternion.identity,Quaternion.Euler(0,90,0),
            Quaternion.Euler(0,180,0),Quaternion.Euler(0,-90,0),Quaternion.Euler(-90,0,0),Quaternion.Euler(90,0,0)};

        [MenuItem("Tools/Album/Rebake observation views from game scenes")]
        public static void Bake()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save open scene edits before capturing the observation views.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Directory.CreateDirectory(Folder);
                var park=EditorSceneManager.OpenScene(Park);
                foreach(var t in park.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)))
                    if(t.name=="Player"||t.name=="Player_FirstPerson"||t.name=="Companion_Photography") t.gameObject.SetActive(false);
                Capture("Park",parkEye,parkHeading,null);
                WriteBridgeOutline(park);
                var room=EditorSceneManager.OpenScene(NarrativeBedroom.WinterScene);
                var bedroom=room.GetRootGameObjects().Select(o=>o.GetComponentInChildren<NarrativeBedroom>(true)).Single(b=>b!=null);
                var w=bedroom.winterAlbum;
                bool active=w.gameObject.activeSelf;w.gameObject.SetActive(false);
                try { Capture("Winter",roomEye,roomHeading,bedroom.cameraProfile); }
                finally { w.gameObject.SetActive(active); }
                Configure("Park",w.parkObservation,w.parkView,new[]{w.observedSwing,w.observedBridge});
                Configure("Winter",w.roomObservation,w.roomView,new[]{w.observedSuitcase,w.observedPendant,w.flowerFocus.transform.parent});
                // The bridge itself is in the park capture. Its former proxy supplies only a focus hit.
                foreach(var r in w.observedBridge.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
                var bridgePoint=w.parkView.position+Quaternion.Inverse(parkHeading)*(new Vector3(5,.56f,0)-parkEye);
                w.bridgeFocus.transform.position=bridgePoint;
                w.bridgeFocus.center=Vector3.zero;
                w.bridgeFocus.size=new Vector3(1.6f,.9f,1);
                w.observedSwing.localPosition=new Vector3(-2.55f,.01f,.35f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(w.observedSwing);
                w.observedSuitcase.localScale=Vector3.one;  // source-scale suitcase beside the bed
                AlignBottom(w.observedSuitcase,w.roomView,new Vector3(.92f,.015f,.18f));
                AlignBottom(w.observedPendant,w.roomView,new Vector3(.08f,.47f,.60f));
                AlignBottom(w.flowerFocus.transform.parent,w.roomView,new Vector3(-.83f,1.14f,-2.54f));
                w.roomView.rotation=Quaternion.Euler(12,0,0);
                EditorSceneManager.MarkSceneDirty(room);
                if(!EditorSceneManager.SaveScene(room)) throw new IOException("Could not save winter observation views");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        // Same capture frame as the six park backgrounds. This mesh is used only
        // by the hover renderer; it never replaces the baked environment.
        public static void WriteBridgeOutline(Scene park)
        {
            var root=park.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true))
                .First(t=>t.name=="WoodenBridge_Reused");
            var capture=Matrix4x4.Rotate(Quaternion.Inverse(parkHeading))*Matrix4x4.Translate(-parkEye);
            var parts=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh &&
                f.GetComponent<Renderer>() && f.GetComponent<Renderer>().enabled).Select(f=>new CombineInstance
                {mesh=f.sharedMesh,transform=capture*f.transform.localToWorldMatrix}).ToArray();
            if(parts.Length==0)throw new InvalidOperationException("Park bridge outline has no source meshes.");
            var mesh=new Mesh{name="Park bridge observation silhouette",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.CombineMeshes(parts,true,true);
            const string path="Assets/Resources/AlbumBridgeHover.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
            else AssetDatabase.CreateAsset(mesh,path);
            AssetDatabase.SaveAssets();
        }

        static void AlignBottom(Transform root,Transform view,Vector3 source)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var bottom=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            root.position+=view.position+Quaternion.Inverse(roomHeading)*(source-roomEye)-bottom;
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }

        static void Capture(string label,Vector3 eye,Quaternion heading,Material profile)
        {
            const int size=1024;
            var go=new GameObject("Observation bake camera");var cam=go.AddComponent<Camera>();
            var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(size,size,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                cam.enabled=false;cam.fieldOfView=90;cam.aspect=1;cam.nearClipPlane=.035f;cam.farClipPlane=400;
                cam.cullingMask=~((1<<27)|(1<<28)|(1<<29));cam.targetTexture=rt;
                if(profile!=null)go.AddComponent<BedroomColorResponse>().Profile=profile;
                for(int i=0;i<faces.Length;i++)
                {
                    cam.transform.SetPositionAndRotation(eye,heading*faces[i]);cam.Render();
                    RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,size,size),0,0);image.Apply();
                    var path=Folder+"/"+label+i+".png";File.WriteAllBytes(path,image.EncodeToPNG());
                    AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.SaveAndReimport();
                }
            }
            finally { cam.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(go); }
        }

        static void Configure(string label,Transform set,Transform view,Transform[] clues)
        {
            foreach(Transform child in set)
                if(child!=view && !clues.Contains(child) && child.GetComponent<Light>()==null && child.name!="Scene_Observation") child.gameObject.SetActive(false);
            var old=set.Find("Scene_Observation");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var root=new GameObject("Scene_Observation").transform;root.SetParent(set,false);root.position=view.position;
            for(int i=0;i<faces.Length;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);obj.layer=28;obj.name=label+"_View_"+i;
                Object.DestroyImmediate(obj.GetComponent<Collider>());
                var t=obj.transform;t.SetParent(root,false);t.localRotation=faces[i];
                t.localPosition=faces[i]*Vector3.forward*16;t.localScale=Vector3.one*32.002f;
                var path=Folder+"/"+label+i+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("Memory/Album Observation"));AssetDatabase.CreateAsset(m,path);}
                m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+label+i+".png");EditorUtility.SetDirty(m);
                var r=obj.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            }
            AssetDatabase.SaveAssets();
        }
    }
}
