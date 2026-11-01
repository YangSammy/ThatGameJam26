using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MemoryWhitebox.EditorTools
{
    public static class DarkroomAssetIntegration
    {
        const string Folder="Assets/Art/Props/DarkroomC/";
        [MenuItem("Tools/Photography/Bind delivered darkroom models")]
        public static void Bind()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var binding=UnityEngine.Object.FindObjectOfType<DarkroomPrintSceneBinding>(true);
            if(binding==null)throw new InvalidOperationException("Open HouseScene first.");
            if(binding.GetComponent<DarkroomAssetVisuals>()!=null)throw new InvalidOperationException("Already bound; retain authored visual adjustments.");
            if(binding.gameObject.scene.isDirty)throw new InvalidOperationException("Unsaved scene changes: save or back up before binding.");
            var adapter=Undo.AddComponent<DarkroomAssetVisuals>(binding.gameObject);
            var list=new List<DarkroomAssetVisuals.Model>();
            string[] ids={"bag","tank","chemical-0","chemical-1","chemical-2","film-drain-basin","print-negative","print-negative-clip","print-frame","print-exposure","print-paper","print-developer","print-stop","print-fixer","print-wash","print-photo-clip"};
            string[] names={"DarkBag_C01","DevelopingTank_C03","DeveloperBottle_C04","StopBottle_C04","FixerBottle_C04","DrainBasin_C05","NegativeStrip_C06","NegativeClip_C06","ContactFrame_C07","ExposureLamp_C08","PhotoPaper_C09","DeveloperTray_C10","StopTray_C10","FixerTray_C10","WashTray_C10","PhotoDryingRack_C11"};
            float[] scales={1,2.4f,1.5f,1.5f,1.5f,1.25f,1.25f,1.25f,1.25f,1,1.25f,1.25f,1.25f,1.25f,1.25f,1.25f};
            // Keep every gameplay root, receiver and placement unchanged. Only child visual poses adapt.
            for(int i=0;i<ids.Length;i++)
            {
                var target=binding.Get(ids[i]).target;
                foreach(var r in target.GetComponentsInChildren<Renderer>(true)){Undo.RecordObject(r,"Replace darkroom visual");r.enabled=false;}
                var visual=Create(names[i],target,scales[i]);list.Add(new DarkroomAssetVisuals.Model{id=ids[i],root=visual});
                if(ids[i]=="tank")Align(visual,"FillTarget",binding.Get("tank-lid").target.position);
                else if(ids[i].StartsWith("chemical-"))visual.transform.position=target.position-Vector3.up*.165f;
                else if(ids[i]=="bag")visual.transform.position=target.position-Vector3.up*.045f;
                else if(ids[i]=="print-frame")Align(visual,"PaperAnchor",binding.Placement(ids[i]).position);
                else if(ids[i]=="print-exposure")Align(visual,"SwitchAnchor",target.position);
                else if(ids[i]=="print-photo-clip"){visual.transform.rotation=Quaternion.Euler(0,270,0);Align(visual,"PaperHangAnchor",binding.Placement(ids[i]).position);}
                else if(ids[i]=="print-negative-clip"){visual.transform.rotation=Quaternion.Euler(0,270,0);Align(visual,"PaperGrip",binding.Placement(ids[i]).position);}
                else if(ids[i]=="print-developer" || ids[i]=="print-stop" || ids[i]=="print-fixer" || ids[i]=="print-wash")Align(visual,"PaperRest",binding.Placement(ids[i]).position);
            }
            adapter.models=list.ToArray();
            var tank=adapter.ModelRoot("tank");
            var reel=Create("FilmReel_C02",tank,2.4f);
            reel.transform.position=DarkroomAssetVisuals.Node(tank,"ReelSeat").position;
            list.Add(new DarkroomAssetVisuals.Model{id="reel",root=reel});adapter.models=list.ToArray();
            var coil=Create("NegativeStrip_C06",reel.transform,2.4f);
            coil.transform.position=DarkroomAssetVisuals.Node(reel.transform,"FilmCoilAnchor").position;
            list.Add(new DarkroomAssetVisuals.Model{id="internal-negative",root=coil});adapter.models=list.ToArray();
            var clips=new List<AnimationClip>();foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{Folder+"Animations"}))clips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid)));adapter.clips=clips.ToArray();
            foreach(var id in new[]{"bag-grip","tank-lid","print-frame-lid","water","print-tap"})foreach(var r in binding.Get(id).target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var id in new[]{"bag-grip","tank-lid","print-frame-lid","water","print-tap","print-exposure"})foreach(var c in binding.Get(id).target.GetComponentsInChildren<Collider>(true))if(c.GetComponent<PhotographyInteractionHit>()==null)c.gameObject.AddComponent<PhotographyInteractionHit>();
            MoveParts(adapter.ModelRoot("film-drain-basin"),"ValveHandle",binding.Get("water").target.position,new[]{"TapBody","ValveHandle","WaterStream","WaterOutlet"});
            MoveParts(adapter.ModelRoot("print-wash"),"WashValve",binding.Get("print-tap").target.position,new[]{"WashTap","WashValve","InletStream","WaterOutlet"});
            // Existing light object still owns activation; only its visual location follows the lamp head.
            if(binding.exposureLight!=null)
            {
                var source=DarkroomAssetVisuals.Node(adapter.ModelRoot("print-exposure"),"LightAnchor");
                binding.exposureLight.transform.position=source.position;
                binding.exposureLight.transform.rotation=Quaternion.LookRotation(binding.Placement("print-frame").position-source.position);
                foreach(var renderer in binding.exposureLight.GetComponents<Renderer>())renderer.enabled=false;
                var light=binding.exposureLight.GetComponent<Light>();if(light==null)light=binding.exposureLight.AddComponent<Light>();
                light.type=LightType.Spot;light.range=2;light.spotAngle=65;light.intensity=1.8f;light.color=new Color(1,.94f,.83f);
            }
            if(binding.waterStream!=null)foreach(var r in binding.waterStream.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            adapter.ResetModels();
            EditorUtility.SetDirty(adapter);EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);EditorSceneManager.SaveScene(binding.gameObject.scene);
        }
        static GameObject Create(string file,Transform parent,float worldScale)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Prefabs/PF_"+file+".prefab");if(prefab==null)throw new InvalidOperationException(file);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
            var s=parent.lossyScale;go.transform.localScale=new Vector3(worldScale/s.x,worldScale/s.y,worldScale/s.z);
            // Never allow the suggested PhysicsProxy/Hit colliders to be re-enabled by legacy SetColliders.
            foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=parent.gameObject.layer;
            var split=file.LastIndexOf('_');go.AddComponent<DarkroomVisualIdentity>().assetName=file.Substring(split+1)+"_"+file.Substring(0,split);
            return go;
        }
        static void Align(GameObject visual,string anchor,Vector3 point){var node=DarkroomAssetVisuals.Node(visual.transform,anchor);if(node==null)throw new InvalidOperationException(anchor);visual.transform.position+=point-node.position;}
        static void MoveParts(Transform root,string reference,Vector3 point,string[] names)
        {var delta=point-DarkroomAssetVisuals.Node(root,reference).position;foreach(var name in names){var t=DarkroomAssetVisuals.Node(root,name);if(t!=null)t.position+=delta;}}
    }
}
