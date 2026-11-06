using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Editor
{
    // Copies only active collision geometry into an isolated physics scene.
    // No production transforms, inputs, saves or lifecycle methods are changed.
    public static class ParkCompanionTraversalChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [UnityEditor.MenuItem("Tools/Remember Seasons/Validation/Park Companion Traversal")]
        static void RunFromMenu(){Debug.Log(Run());}
        public static string Run(bool injectBlocker=false)
        {
            if(Application.isPlaying)throw new InvalidOperationException("Run in Edit mode with ParkScene loaded");
            var source=UnityEngine.Object.FindObjectOfType<ParkCompanion>();
            if(source==null || source.gameObject.scene.name!="ParkScene")throw new InvalidOperationException("Load the playable ParkScene first");
            var sourceMotor=source.GetComponent<CharacterController>();
            var scene=EditorSceneManager.NewPreviewScene();
            var lines=new List<string>();
            try
            {
                foreach(var original in UnityEngine.Object.FindObjectsOfType<Collider>())
                {
                    if(original.gameObject.scene!=source.gameObject.scene || !original.enabled || original.isTrigger || original is CharacterController)continue;
                    var go=new GameObject(Path(original.transform));SceneManager.MoveGameObjectToScene(go,scene);
                    go.layer=original.gameObject.layer;
                    go.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);go.transform.localScale=original.transform.lossyScale;
                    Collider copy;
                    var box=original as BoxCollider;var mesh=original as MeshCollider;var sphere=original as SphereCollider;var capsule=original as CapsuleCollider;
                    if(box!=null){var c=go.AddComponent<BoxCollider>();c.center=box.center;c.size=box.size;copy=c;}
                    else if(mesh!=null){var c=go.AddComponent<MeshCollider>();c.cookingOptions=mesh.cookingOptions;c.sharedMesh=mesh.sharedMesh;c.convex=mesh.convex;copy=c;}
                    else if(sphere!=null){var c=go.AddComponent<SphereCollider>();c.center=sphere.center;c.radius=sphere.radius;copy=c;}
                    else if(capsule!=null){var c=go.AddComponent<CapsuleCollider>();c.center=capsule.center;c.radius=capsule.radius;c.height=capsule.height;c.direction=capsule.direction;copy=c;}
                    else throw new InvalidOperationException("Unsupported collider: "+original.GetType().Name+" "+go.name);
                    copy.sharedMaterial=original.sharedMaterial;
                }
                if(injectBlocker)
                {
                    var obstacle=new GameObject("QA deliberate route obstruction");SceneManager.MoveGameObjectToScene(obstacle,scene);
                    obstacle.transform.position=(source.route[2].transform.position+source.route[3].transform.position)*.5f+Vector3.up;
                    var forward=source.route[3].transform.position-source.route[2].transform.position;forward.y=0;
                    obstacle.transform.rotation=Quaternion.LookRotation(forward);
                    obstacle.AddComponent<BoxCollider>().size=new Vector3(20,3,.5f);
                }
                var physics=scene.GetPhysicsScene();
                foreach(float dt in new[]{1f/60,1f/30,.1f})
                {
                    var go=new GameObject("QA mover");SceneManager.MoveGameObjectToScene(go,scene);go.layer=source.gameObject.layer;
                    var girl=go.AddComponent<ParkCompanion>();girl.enabled=false;
                    var motor=go.GetComponent<CharacterController>();motor.enabled=false;
                    motor.center=sourceMotor.center;motor.radius=sourceMotor.radius;motor.height=sourceMotor.height;motor.stepOffset=sourceMotor.stepOffset;motor.slopeLimit=sourceMotor.slopeLimit;motor.skinWidth=sourceMotor.skinWidth;motor.minMoveDistance=sourceMotor.minMoveDistance;
                    go.transform.localScale=source.transform.lossyScale;go.transform.position=source.spawn!=null?source.spawn.position:source.transform.position;motor.enabled=true;
                    var player=new GameObject("QA follower");SceneManager.MoveGameObjectToScene(player,scene);girl.player=player.transform;
                    girl.route=source.route;girl.walkSpeed=source.walkSpeed;girl.gravity=source.gravity;girl.waypointTolerance=source.waypointTolerance;girl.waitDistance=source.waitDistance;girl.personalSpace=source.personalSpace;
                    var type=typeof(ParkCompanion);type.GetField("motor",Hidden).SetValue(girl,motor);type.GetField("started",Hidden).SetValue(girl,true);
                    var tick=type.GetMethod("TickTravel",Hidden);var index=type.GetField("index",Hidden);
                    Physics.SyncTransforms();physics.Simulate(.02f);
                    int previousIndex=0;float blocked=0;bool failed=false;float minY=go.transform.position.y,maxY=minY;
                    for(int frame=0;frame<Mathf.CeilToInt(120/dt);frame++)
                    {
                        int current=(int)index.GetValue(girl);if(current>=girl.route.Length)break;
                        var direction=girl.route[current].transform.position-go.transform.position;direction.y=0;
                        player.transform.position=go.transform.position-direction.normalized*2;
                        var before=go.transform.position;
                        tick.Invoke(girl,new object[]{dt});physics.Simulate(dt);
                        var horizontal=go.transform.position-before;horizontal.y=0;
                        if(horizontal.magnitude>girl.walkSpeed*dt+.25f)
                        {lines.Add("FAIL unexpected displacement/recovery at target="+current);failed=true;break;}
                        minY=Mathf.Min(minY,go.transform.position.y);maxY=Mathf.Max(maxY,go.transform.position.y);
                        if(girl.TravelRequestedLastStep && girl.TravelDistanceLastStep<.001f)blocked+=dt;else blocked=0;
                        int nextIndex=(int)index.GetValue(girl);
                        if(nextIndex!=previousIndex){lines.Add("node "+(nextIndex-1)+" @ "+go.transform.position.ToString("F3")+" dt="+dt.ToString("F3"));previousIndex=nextIndex;}
                        if(blocked>2 || go.transform.position.y< -2)
                        {
                            var hits=new Collider[32];var centre=go.transform.TransformPoint(motor.center);
                            int count=physics.OverlapCapsule(centre+Vector3.up*(motor.height*.5f-motor.radius),centre-Vector3.up*(motor.height*.5f-motor.radius),motor.radius+.04f,hits,~0,QueryTriggerInteraction.Ignore);
                            var blockers=new List<string>();for(int j=0;j<count;j++)if(hits[j]!=motor)blockers.Add(hits[j].name);
                            lines.Add("FAIL target="+current+" position="+go.transform.position.ToString("F3")+" nearby="+string.Join(";",blockers));failed=true;break;
                        }
                    }
                    if(!failed)lines.Add((girl.Finished?"PASS":"FAIL timeout")+" complete route dt="+dt.ToString("F3")+" height="+minY.ToString("F3")+".."+maxY.ToString("F3"));
                    UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(player);
                }
                return string.Join("\n",lines);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        static string Path(Transform t){var path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
    }
}
