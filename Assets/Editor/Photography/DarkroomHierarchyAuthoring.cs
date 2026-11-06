using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    public static class DarkroomHierarchyAuthoring
    {
        const string RootName="Darkroom - 暗室摆放";
        [MenuItem("Memory/Photography/Organize darkroom hierarchy")]
        public static void Organize()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Use Edit Mode");
            var room=UnityEngine.Object.FindObjectOfType<SummerPhotoRoom>();
            if(!room)throw new InvalidOperationException("Open Bedroom_Summer first");
            var binding=room.darkroomStation;
            if(binding.GetComponentsInChildren<DarkroomLayoutItem>(true).Length>0)
            {
                GroupLooseParts(binding);Debug.Log(Validate(room));
                EditorSceneManager.MarkSceneDirty(room.gameObject.scene);EditorSceneManager.SaveScene(room.gameObject.scene);return;
            }
            var root=New(RootName,null,Vector3.zero,room.gameObject.scene);
            var furniture=New("00 Worktop - 工作台与碰撞",root,Vector3.zero,root.gameObject.scene);
            var table=binding.transform.parent;
            Undo.SetTransformParent(binding.transform,root,"Organize darkroom");
            binding.name="01 Items - 移动物品父节点";
            Undo.SetTransformParent(table,furniture,"Organize darkroom");
            Undo.SetTransformParent(room.counterBlocker.transform,furniture,"Organize darkroom");
            var settings=New("02 View - 入口视角",root,Vector3.zero,root.gameObject.scene);
            var view=New("View_Darkroom_Worktop",settings,room.darkroomView.position,root.gameObject.scene);
            view.rotation=room.darkroomView.rotation;
            Undo.RecordObject(room,"Bind organized layout");room.darkroomView=view;

            Transform Group(string id,string name,params string[] members)
            {
                var primary=binding.Get(id).target;
                var group=New(name,binding.transform,primary.position,root.gameObject.scene);
                var layout=Undo.AddComponent<DarkroomLayoutItem>(group.gameObject);
                layout.itemId=id;layout.primaryTarget=primary;
                var moving=new HashSet<Transform>();
                void Include(string key)
                {
                    var prop=binding.Get(key);if(prop==null)return;
                    moving.Add(prop.target);if(prop.placement)moving.Add(prop.placement);
                }
                Include(id);foreach(var member in members)Include(member);
                // Keep nested lids, hit shapes and models intact. Only move the
                // highest selected transforms, preserving world poses and references.
                foreach(var item in moving)
                {
                    bool nested=false;
                    foreach(var other in moving)if(item!=other && item.IsChildOf(other)){nested=true;break;}
                    if(!nested)Undo.SetTransformParent(item,group,"Group darkroom item");
                }
                return group;
            }
            Group("bag","02 Dark Bag - 暗袋","bag-grip","film-bag");
            Group("tank","03 Developing Tank - 显影罐","tank-lid","film-tank");
            Group("chemical-0","04 Film Developer - 显影液","film-chemical-0");
            Group("chemical-1","05 Film Stop Bath - 停显液","film-chemical-1");
            Group("chemical-2","06 Film Fixer - 定影液","film-chemical-2");
            Group("film-drain-basin","07 Film Basin and Tap - 废液盆与水龙头","water","film-tap","film-sink");
            Group("print-negative","08 Negative - 负片起始位置","print-negative-home");
            Group("print-negative-clip","09 Negative Clip - 负片晾夹");
            Group("print-frame","10 Contact Frame - 接触印相框","print-frame-lid");
            var lamp=Group("print-exposure","11 Exposure Lamp - 曝光灯与开关");
            Undo.SetTransformParent(binding.exposureLight.transform,lamp,"Group exposure light");
            Group("print-paper","12 Photo Paper - 相纸起始位置","print-paper-home");
            Group("print-developer","13 Developer Tray - 显影盘");
            Group("print-stop","14 Stop Tray - 停显盘");
            Group("print-fixer","15 Fixer Tray - 定影盘");
            var wash=Group("print-wash","16 Wash Tray and Tap - 水洗盘与水龙头","print-tap");
            Undo.SetTransformParent(binding.waterStream.transform,wash,"Group wash stream");
            Group("print-photo-clip","17 Photo Rack - 照片晾架");
            var timer=binding.Get("film-clock-hand").target.parent;
            var timerGroup=New("18 Timer - 计时器",binding.transform,timer.position,root.gameObject.scene);
            var timerItem=Undo.AddComponent<DarkroomLayoutItem>(timerGroup.gameObject);
            timerItem.itemId="film-clock-hand";timerItem.primaryTarget=timer;
            Undo.SetTransformParent(timer,timerGroup,"Group timer");
            Undo.SetTransformParent(binding.Get("film-clock").target,timerGroup,"Group timer anchor");

            var camera=New("01 Camera - 相机摆放",binding.transform,room.cameraDock.position,root.gameObject.scene);
            camera.rotation=room.cameraDock.rotation;
            var cameraItem=Undo.AddComponent<DarkroomLayoutItem>(camera.gameObject);
            cameraItem.itemId="camera";cameraItem.primaryTarget=camera;
            var preview=PhotographyCameraVisual.Create(camera,new Vector3(0,.005f,0),3.1f,8);
            preview.name="Scene Preview - 相机外观预览";
            preview.transform.localRotation=Quaternion.Euler(0,180,0);
            preview.Apply(false,false,100,0,0,2,8,1f/125,0,0,true);
            foreach(var collider in preview.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            cameraItem.scenePreview=preview.gameObject;room.cameraDock=camera;
            PrefabUtility.RecordPrefabInstancePropertyModifications(room);
            EditorUtility.SetDirty(room);
            GroupLooseParts(binding);
            // Remove only empty organizational folders, never props or binding components.
            for(int i=binding.transform.childCount-1;i>=0;i--)
            {
                var child=binding.transform.GetChild(i);
                if(child.childCount==0 && child.GetComponents<Component>().Length==1)Undo.DestroyObjectImmediate(child.gameObject);
            }
            var ordered=new List<Transform>();foreach(Transform child in binding.transform)ordered.Add(child);
            ordered.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));for(int i=0;i<ordered.Count;i++)ordered[i].SetSiblingIndex(i);
            Debug.Log(Validate(room));
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
            Selection.activeGameObject=root.gameObject;
        }
        static Transform New(string name,Transform parent,Vector3 world,Scene scene)
        {
            var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);
            Undo.RegisterCreatedObjectUndo(go,"Organize darkroom");go.transform.SetParent(parent,false);
            go.transform.position=world;return go.transform;
        }
        static void GroupLooseParts(DarkroomPrintSceneBinding binding)
        {
            foreach(var item in binding.GetComponentsInChildren<Transform>(true))
            {
                if(item.GetComponentInParent<DarkroomLayoutItem>()!=null)continue;
                string id=item.name.StartsWith("Developer Tray Rim")?"print-developer":
                    item.name.StartsWith("Stop Bath Tray Rim")?"print-stop":
                    item.name.StartsWith("Fixer Tray Rim")?"print-fixer":
                    item.name=="Drying Stand" || item.name=="Drying Rail"?"print-photo-clip":null;
                if(id!=null)Undo.SetTransformParent(item,binding.Get(id).target.GetComponentInParent<DarkroomLayoutItem>().transform,"Group remaining darkroom visuals");
            }
            for(int i=binding.transform.childCount-1;i>=0;i--)
            {
                var child=binding.transform.GetChild(i);
                if(child.childCount==0 && child.GetComponents<Component>().Length==1)Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
        public static string Validate(SummerPhotoRoom room)
        {
            var b=room.darkroomStation;int count=0;
            foreach(var prop in b.props)
            {
                if(!prop.target)throw new Exception("Missing target: "+prop.id);
                var owner=prop.target.GetComponentInParent<DarkroomLayoutItem>();
                if(!owner)throw new Exception("Ungrouped target: "+prop.id);
                if(prop.placement && !prop.placement.IsChildOf(owner.transform))throw new Exception("Detached placement: "+prop.id);
                count++;
            }
            foreach(var model in b.GetComponent<DarkroomAssetVisuals>().models)
                if(!model.root.GetComponentInParent<DarkroomLayoutItem>())throw new Exception("Ungrouped model: "+model.id);
            foreach(var renderer in b.GetComponentsInChildren<Renderer>(true))
                if(!renderer.GetComponentInParent<DarkroomLayoutItem>())throw new Exception("Ungrouped renderer: "+renderer.name);
            foreach(var collider in b.GetComponentsInChildren<Collider>(true))
                if(!collider.GetComponentInParent<DarkroomLayoutItem>())throw new Exception("Ungrouped hit: "+collider.name);
            if(!b.exposureLight.GetComponentInParent<DarkroomLayoutItem>() || !b.waterStream.GetComponentInParent<DarkroomLayoutItem>())throw new Exception("Detached effect");
            foreach(var owner in b.GetComponentsInChildren<DarkroomLayoutItem>(true))
            {
                var nodes=owner.GetComponentsInChildren<Transform>(true);var points=new Vector3[nodes.Length];
                for(int i=0;i<nodes.Length;i++)points[i]=owner.transform.InverseTransformPoint(nodes[i].position);
                var p=owner.transform.position;var q=owner.transform.rotation;
                try
                {
                    owner.transform.SetPositionAndRotation(p+new Vector3(.13f,.07f,-.11f),Quaternion.Euler(0,17,0)*q);
                    for(int i=0;i<nodes.Length;i++)if(Vector3.Distance(nodes[i].position,owner.transform.TransformPoint(points[i]))>.0001f)throw new Exception("Child did not follow "+owner.itemId);
                }
                finally{owner.transform.SetPositionAndRotation(p,q);}
            }
            return "PASS: "+count+" prop bindings grouped; models, hits, placements and effects follow their authoring parents";
        }
    }
}
