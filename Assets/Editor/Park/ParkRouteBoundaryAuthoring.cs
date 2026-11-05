using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    // One-time scene authoring, never called by gameplay or at scene load.
    public static class ParkRouteBoundaryAuthoring
    {
        public const string Group="RouteBoundary";
        static readonly Vector2[] Edge={
            new Vector2(-18,-15),new Vector2(-19,-12),new Vector2(-16,-7),
            new Vector2(-11,-3),new Vector2(-5,1),new Vector2(-1,2),
            new Vector2(0,1.28f),new Vector2(1.5f,1.28f),new Vector2(8.5f,1.28f),
            new Vector2(9.6f,1.5f),new Vector2(10.8f,4.5f),new Vector2(11,7),
            new Vector2(10,10),new Vector2(12,12),new Vector2(16,12),
            new Vector2(18,10),new Vector2(18,7),new Vector2(16,3),
            new Vector2(12,-2),new Vector2(10,-2),new Vector2(8.5f,-1.28f),
            new Vector2(1.5f,-1.28f),new Vector2(0,-1.28f),new Vector2(-4,-4),
            new Vector2(-9,-8),new Vector2(-13,-13),new Vector2(-15,-16)};
        static Vector3 World(Vector2 p,float y=0)=>new Vector3(p.x,y,p.y);
        static bool Bridge(int index)=>index==7 || index==20;
        static float Floor(Vector2 point)
        {
            // Keep the authored bridge elevation; the rest of this park is level.
            if(Mathf.Abs(point.y)<=1.45f && point.x>=0 && point.x<=10)
                return Mathf.Lerp(.02f,.55f,Mathf.Clamp01(Mathf.Min(point.x,10-point.x)/1.5f));
            return .02f;
        }
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/ParkScene.unity");
            if(!scene.isLoaded)throw new InvalidOperationException("Load ParkScene first");
            if(scene.isDirty)throw new InvalidOperationException("Unsaved ParkScene changes; no changes made");
            var environment=scene.GetRootGameObjects().First(r=>r.name=="Environment").transform;
            if(environment.Find(Group)!=null)throw new InvalidOperationException("RouteBoundary already exists; adjust authored objects directly");
            var fence=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environments/Park/V08/Prefabs/Struct_Fence2_Mid_A_Park.prefab");
            string hedgePath=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ParkExtras/ParkTidyGarden_Ready"})
                .Select(AssetDatabase.GUIDToAssetPath).First(p=>p.Contains("Hedge_SoftStrip.prefab"));
            var hedge=AssetDatabase.LoadAssetAtPath<GameObject>(hedgePath);
            var root=new GameObject(Group);SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetParent(environment,false);
            for(int i=0;i<Edge.Length;i++)
            {
                if(Bridge(i))continue; // existing real bridge rails close these two edges
                var a=Edge[i];var b=Edge[(i+1)%Edge.Length];float length=Vector2.Distance(a,b);
                var direction=(b-a).normalized;var outward=new Vector2(-direction.y,direction.x);
                int pieces=Mathf.Max(1,Mathf.CeilToInt(length/4.8f));
                for(int n=0;n<pieces;n++)
                {
                    var first=Vector2.Lerp(a,b,n/(float)pieces);var last=Vector2.Lerp(a,b,(n+1f)/pieces);
                    var centre=(first+last)*.5f;float width=Vector2.Distance(first,last);
                    float ground=Mathf.Min(Floor(first),Floor(last));
                    var panel=new GameObject("Fence_"+i.ToString("00")+"_"+n.ToString("00"));
                    panel.transform.SetParent(root.transform,false);panel.transform.position=World(centre,ground);
                    panel.transform.rotation=Quaternion.Euler(0,-Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,0);
                    Fit(fence,panel.transform,new Vector3(width+.12f,1.15f,.20f),Quaternion.identity,"Visual");
                    var collider=panel.AddComponent<BoxCollider>();collider.center=new Vector3(0,.60f,0);collider.size=new Vector3(width+.16f,1.30f,.22f);
                    // Foliage sits outside the fence, not in the walkable corridor.
                    if(width>1.8f && !(centre.x>-.5f && centre.x<10))
                    {
                        int plants=Mathf.CeilToInt(width/2.2f);
                        for(int plant=0;plant<plants;plant++)
                        {
                            var spot=Vector2.Lerp(first,last,(plant+.5f)/plants)+outward*.47f;
                            var shrub=new GameObject("Hedge_"+i.ToString("00")+"_"+n.ToString("00")+"_"+plant.ToString("00"));
                            shrub.transform.SetParent(root.transform,false);shrub.transform.SetPositionAndRotation(World(spot,.02f),panel.transform.rotation);
                            Fit(hedge,shrub.transform,new Vector3(width/plants+.15f,.95f,.75f),Quaternion.Euler(-90,0,0),"Visual");
                        }
                    }
                }
            }
            Physics.SyncTransforms();
            string report=Validate(scene);
            if(report.Contains("FAIL"))throw new InvalidOperationException(report);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return report;
        }
        static void Fit(GameObject prefab,Transform parent,Vector3 size,Quaternion rotation,string name)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=name;
            go.transform.localPosition=Vector3.zero;go.transform.localRotation=rotation;
            // A wrapper accommodates the delivered source orientation without editing its GUID.
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=LocalBounds(parent,renderers);
            var wrapper=new GameObject("ModelFit").transform;wrapper.SetParent(parent,false);
            go.transform.SetParent(wrapper,true);go.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            wrapper.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        }
        static Bounds LocalBounds(Transform parent,Renderer[] renderers)
        {
            bool first=true;var result=new Bounds();
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null || filter.sharedMesh==null)continue;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p=parent.InverseTransformPoint(renderer.transform.TransformPoint(p));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            return result;
        }
        public static string Validate(Scene scene)
        {
            Physics.SyncTransforms();
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            var walls=all.Where(c=>c.transform.IsChildOf(scene.GetRootGameObjects().First(r=>r.name=="Environment").transform.Find(Group)) || c.name=="BridgeRailCollision").ToArray();
            var lines=new List<string>();int probes=0,misses=0;
            for(int i=0;i<Edge.Length;i++)
            {
                var a=Edge[i];var b=Edge[(i+1)%Edge.Length];var dir=(b-a).normalized;var normal=new Vector2(-dir.y,dir.x);
                int count=Mathf.CeilToInt(Vector2.Distance(a,b)/.12f);
                for(int n=0;n<=count;n++)
                {
                    var point=Vector2.Lerp(a,b,(float)n/count);var origin=World(point-normal*.7f,Floor(point)+.65f);
                    var ray=new Ray(origin,World(normal));RaycastHit hit;probes++;
                    if(!walls.Any(c=>c.Raycast(ray,out hit,1.5f)))misses++;
                }
            }
            lines.Add((misses==0?"PASS":"FAIL")+" perimeter probes "+probes+", gaps="+misses);
            var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ParkPhotoPrototype>()).First();
            var anchors=new List<Transform>{player.spawn};anchors.AddRange(player.companion.route.Select(n=>n.transform));anchors.AddRange(player.companion.route.Where(n=>n.referenceView!=null).Select(n=>n.referenceView));
            foreach(var anchor in anchors)
            {
                var point=anchor.position;point.y=Floor(new Vector2(point.x,point.z))+.8f;
                bool blocked=walls.Any(c=>Vector3.Distance(c.ClosestPoint(point),point)<.38f);
                lines.Add((blocked?"FAIL":"PASS")+" clear anchor "+anchor.name);
            }
            return string.Join("\n",lines);
        }
    }
}
