using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    public static class AutumnWinterPresentationAuthoring
    {
        const string Character="Assets/Art/Characters/ParkGirlTripoRigged";
        public const string FarewellPrefab="Assets/Art/Characters/ParkGirlTripoRigged/Prefabs/PF_AutumnFarewell.prefab";

        public static string AuthorFarewell()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Character+"/Prefabs/PF_ParkGirlTripo_Locomotion.prefab"));
            try
            {
                root.name="PF_AutumnFarewell";
                var old=root.GetComponent<ParkGirlPresentation>();
                var v=root.AddComponent<AutumnFarewellVisual>();
                v.rig=root.GetComponentInChildren<Animator>();v.rig.enabled=false;v.rig.runtimeAnimatorController=null;
                v.idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Character+"/Animations/AN_ParkGirl_IdleLoop.anim");
                v.wave=AssetDatabase.LoadAssetAtPath<AnimationClip>(Character+"/Animations/AN_wave_goodbye_01.anim");
                v.walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(Character+"/Animations/AN_ParkGirl_WalkInPlace.anim");
                v.hips=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hips");
                v.head=old.headBone;v.chest=old.upperChest;v.hair=old.hairBones;
                v.groundedBones=root.GetComponentsInChildren<Transform>().Where(t=>t==v.hips ||
                    t.name.EndsWith("_UpperLeg") || t.name.EndsWith("_LowerLeg") || t.name.EndsWith("_Foot") || t.name.EndsWith("_Toes")).ToArray();
                if(v.hair.Length==0 || v.chest==null)throw new InvalidOperationException("Source girl is missing authored hair/shoulder bindings.");
                Object.DestroyImmediate(old);
                foreach(var c in root.GetComponentsInChildren<Collider>(true))c.enabled=false;
                v.Present(0,0,false);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,FarewellPrefab);
                var assets=WritingVisualAssets.Load();assets.farewell=prefab.GetComponent<AutumnFarewellVisual>();
                EditorUtility.SetDirty(assets);AssetDatabase.SaveAssetIfDirty(assets);
                return "Autumn farewell bound: original skinned body, weighted hair and source wave take.";
            }
            finally{Object.DestroyImmediate(root);}
        }

        // Copy geometry, not gameplay. Source scene hierarchy and source assets stay untouched.
        static Transform CopyGeometry(Transform source,Transform parent)
        {
            if(!source.gameObject.activeInHierarchy)return null;
            var copy=new GameObject(source.name).transform;copy.SetParent(parent,false);
            copy.localPosition=source.localPosition;copy.localRotation=source.localRotation;copy.localScale=source.localScale;
            copy.gameObject.layer=28;
            // Repeated observation geometry must keep sharing its source meshes.
            // Baking every tree into static batches duplicated nearly 1 GiB in winter.
            GameObjectUtility.SetStaticEditorFlags(copy.gameObject,(StaticEditorFlags)0);
            var mf=source.GetComponent<MeshFilter>();var mr=source.GetComponent<MeshRenderer>();
            if(mf!=null && mf.sharedMesh!=null && mr!=null && mr.enabled)
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;
                var r=copy.gameObject.AddComponent<MeshRenderer>();r.sharedMaterials=mr.sharedMaterials;
                r.shadowCastingMode=mr.shadowCastingMode;r.receiveShadows=mr.receiveShadows;
                r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
            foreach(Transform child in source)CopyGeometry(child,copy);
            var lod=source.GetComponent<LODGroup>();
            if(lod!=null)
            {
                var group=copy.gameObject.AddComponent<LODGroup>();
                group.fadeMode=lod.fadeMode;group.animateCrossFading=lod.animateCrossFading;
                group.SetLODs(lod.GetLODs().Select(level=>new LOD(level.screenRelativeTransitionHeight,
                    level.renderers.Where(r=>r!=null).Select(r=>copy.Find(AnimationUtility.CalculateTransformPath(r.transform,source)))
                    .Where(t=>t!=null).Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null).ToArray())
                    {fadeTransitionWidth=level.fadeTransitionWidth}).ToArray());
                group.localReferencePoint=lod.localReferencePoint;group.size=lod.size;
            }
            if(copy.childCount==0 && copy.GetComponent<Renderer>()==null){Object.DestroyImmediate(copy.gameObject);return null;}
            return copy;
        }

        static Transform GeometrySet(Transform set,Transform view,Transform source,Vector3 sourceEye,Quaternion sourceHeading)
        {
            var old=set.Find("Realtime_Observation");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var frame=new GameObject("Realtime_Observation").transform;frame.SetParent(set,false);
            frame.SetPositionAndRotation(Vector3.zero,Quaternion.identity);frame.localScale=Vector3.one;
            var copy=CopyGeometry(source,frame);
            copy.SetPositionAndRotation(source.position,source.rotation);copy.localScale=source.lossyScale;
            var rotation=Quaternion.Inverse(sourceHeading);
            frame.SetPositionAndRotation(view.position-rotation*sourceEye,rotation);
            var baked=set.Find("Scene_Observation");if(baked!=null)baked.gameObject.SetActive(false);
            return frame;
        }

        static void CopyLights(Scene scene,Transform frame,Transform set)
        {
            var lamps=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>())
                .Where(l=>l.enabled && !l.transform.IsChildOf(set) && !l.GetComponentInParent<WinterAlbumWorkspace>()).ToArray();
            if(lamps.Length==0)return;
            foreach(var light in set.GetComponentsInChildren<Light>(true))light.enabled=false;
            foreach(var source in lamps)
            {
                var t=new GameObject("Observation_"+source.name).transform;t.SetParent(frame,false);
                // frame maps original scene coordinates to the unchanged observation eye.
                t.localPosition=source.transform.position;t.localRotation=source.transform.rotation;
                var light=t.gameObject.AddComponent<Light>();EditorUtility.CopySerialized(source,light);
                light.cullingMask=1<<28;light.lightmapBakeType=LightmapBakeType.Realtime;
                light.shadows=LightShadows.None;t.gameObject.layer=28;
            }
        }

        public static string AuthorWinter(bool parkOnly=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var previous=SceneManager.GetActiveScene();
            string path=NarrativeBedroom.WinterScene;
            var winter=SceneManager.GetSceneByPath(path);bool opened=!winter.isLoaded;
            if(!opened && winter.isDirty)throw new InvalidOperationException("Winter has unsaved edits; authoring stopped without saving them.");
            if(opened)winter=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var park=SceneManager.GetSceneByPath("Assets/Scenes/ParkScene.unity");bool parkOpened=!park.isLoaded;
            // The park is a read-only source: copy its current visible geometry,
            // including unsaved edits, without saving or closing an already open scene.
            if(parkOpened)park=EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Additive);
            try
            {
                var w=winter.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WinterAlbumWorkspace>(true)).Single();
                bool active=w.gameObject.activeSelf;w.gameObject.SetActive(true);
                bool parkActive=w.parkObservation.gameObject.activeSelf,roomActive=w.roomObservation.gameObject.activeSelf;
                w.parkObservation.gameObject.SetActive(true);w.roomObservation.gameObject.SetActive(true);
                var env=park.GetRootGameObjects().Single(o=>o.name=="Environment").transform;
                var parkFrame=GeometrySet(w.parkObservation,w.parkView,env,new Vector3(-.2f,1.75f,-3.8f),Quaternion.Euler(0,32,0));
                CopyLights(park,parkFrame,w.parkObservation);
                var bridge=parkFrame.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="WoodenBridge_Reused");
                foreach(var mf in bridge.GetComponentsInChildren<MeshFilter>())
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
                if(w.observedBridge!=null && w.observedBridge!=bridge)w.observedBridge.gameObject.SetActive(false);
                w.observedBridge=bridge;w.bridgeFocus.enabled=false;
                Transform roomFrame=w.roomObservation.Find("Realtime_Observation");
                if(!parkOnly)
                {
                    var room=winter.GetRootGameObjects().Single(o=>o.name=="Bedroom_Winter_Editable").transform;
                    roomFrame=GeometrySet(w.roomObservation,w.roomView,room,new Vector3(0,1.60f,2.48f),Quaternion.Euler(0,180,0));
                    CopyLights(winter,roomFrame,w.roomObservation);
                }
                w.realtimeObservation=true;
                w.parkObservation.gameObject.SetActive(parkActive);w.roomObservation.gameObject.SetActive(roomActive);w.gameObject.SetActive(active);
                EditorSceneManager.MarkSceneDirty(winter);
                if(!EditorSceneManager.SaveScene(winter))throw new InvalidOperationException("Winter observation scene could not be saved.");
                return "Realtime observation geometry saved. Park meshes="+parkFrame.GetComponentsInChildren<MeshRenderer>(true).Length+
                    "; room meshes="+roomFrame.GetComponentsInChildren<MeshRenderer>(true).Length+"; bridge mesh hits="+bridge.GetComponentsInChildren<MeshCollider>(true).Length;
            }
            finally
            {
                if(parkOpened)EditorSceneManager.CloseScene(park,true);
                if(opened)EditorSceneManager.CloseScene(winter,true);
                if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            }
        }
    }
}
