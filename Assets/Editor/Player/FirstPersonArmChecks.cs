using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    public static class FirstPersonArmChecks
    {
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        public static string Run()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FirstPersonArmAssets.PrefabPath);
            Check(prefab!=null,"Shared prefab missing");
            var scene=EditorSceneManager.NewPreviewScene();
            var originalCursor=Cursor.lockState;bool originalVisible=Cursor.visible;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            try
            {
                var rig=go.GetComponent<FirstPersonArmRig>();Check(rig.Valid,"Missing rig references");
                Check(!rig.animator.isHuman,"Partial skeleton must stay Generic");
                var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();
                Check(skin.bones.Length==52 && skin.bones.All(b=>b!=null),"Skin bindings lost");
                Check(skin.sharedMaterial.mainTexture.name=="arms_01","Bare skin missing");
                Check(go.GetComponentsInChildren<Transform>().All(t=>t.gameObject.layer==27),"Layer mismatch");
                var transforms=go.GetComponentsInChildren<Transform>();var scales=transforms.Select(t=>t.localScale).ToArray();
                var leftFinger=transforms.Single(t=>t.name=="f_index.02.L");var rightFinger=transforms.Single(t=>t.name=="f_index.02.R");
                rig.Evaluate(default(FirstPersonArmRig.Pose),default(FirstPersonArmRig.Pose),0,true);
                Check(rig.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle.Relax"),"Idle state path mismatch");
                var l=leftFinger.localRotation;var r=rightFinger.localRotation;
                var grip=new FirstPersonArmRig.Pose{grip=1};
                rig.Evaluate(grip,default(FirstPersonArmRig.Pose),0,true);
                Check(Quaternion.Angle(l,leftFinger.localRotation)>5,"Left grab clip did not animate fingers");
                Check(Quaternion.Angle(r,rightFinger.localRotation)<.1f,"Left grab contaminates right hand");
                rig.Evaluate(default(FirstPersonArmRig.Pose),grip,0,true);
                Check(Quaternion.Angle(r,rightFinger.localRotation)>5,"Right grab clip did not animate fingers");
                Check(Quaternion.Angle(l,leftFinger.localRotation)<.1f,"Right grab contaminates left hand");
                rig.Evaluate(default(FirstPersonArmRig.Pose),new FirstPersonArmRig.Pose{push=1},0,true);
                Check(rig.animator.GetCurrentAnimatorStateInfo(4).IsName("RightPush.Push") && rig.animator.GetLayerWeight(4)==1,"Right press not connected");
                rig.Evaluate(new FirstPersonArmRig.Pose{push=1},default(FirstPersonArmRig.Pose),0,true);
                Check(rig.animator.GetCurrentAnimatorStateInfo(2).IsName("LeftPush.Push") && rig.animator.GetLayerWeight(2)==1,"Left press not connected");
                var target=new Vector3(-.2f,-.22f,.4f);
                rig.Evaluate(FirstPersonArmRig.Pose.At(target,Quaternion.identity,1),default(FirstPersonArmRig.Pose),0,true);
                Check(Vector3.Distance(rig.wrists[0].position,target)<.015f,"Reachable contact inaccurate");
                float length=Vector3.Distance(rig.upperArms[0].position,rig.forearms[0].position)+Vector3.Distance(rig.forearms[0].position,rig.wrists[0].position);
                rig.Evaluate(FirstPersonArmRig.Pose.At(new Vector3(-2,1,4),Quaternion.identity,1),default(FirstPersonArmRig.Pose),0,true);
                Check(Vector3.Distance(rig.upperArms[0].position,rig.wrists[0].position)<=length*.99f,"Unreachable target overextends arm");
                for(int i=0;i<transforms.Length;i++)Check(Vector3.Distance(scales[i],transforms[i].localScale)<.00001f,"Bone scale changed: "+transforms[i].name);
                rig.ResetPose();Check(Quaternion.Angle(l,leftFinger.localRotation)<.1f,"Reset leaves stale grasp");
                for(int i=1;i<5;i++)Check(rig.animator.GetLayerWeight(i)==0,"Reset leaves active gesture layer");
                var sustained=FirstPersonArmRig.Pose.At(new Vector3(-.15f,-.25f,.8f),Quaternion.identity,1);
                for(int frame=0;frame<180;frame++)rig.Evaluate(sustained,default(FirstPersonArmRig.Pose),1f/60);
                Check(Vector3.Distance(rig.wrists[0].position,sustained.position)<.015f,"Normal frame progression fails to reach contact");
                var steady=rig.model.localPosition;
                for(int frame=0;frame<120;frame++)rig.Evaluate(sustained,default(FirstPersonArmRig.Pose),1f/60);
                Check(Vector3.Distance(steady,rig.model.localPosition)<.005f,"Sustained reach drifts");
                for(int frame=0;frame<60;frame++)rig.Evaluate(default(FirstPersonArmRig.Pose),default(FirstPersonArmRig.Pose),1f/60);
                Check(rig.animator.GetLayerWeight(1)==0 && Vector3.Distance(rig.model.localPosition,rig.modelOffset)<.005f,"Release retains grip or body lean");
                rig.Evaluate(FirstPersonArmRig.Pose.At(target,Quaternion.identity,1),FirstPersonArmRig.Pose.At(new Vector3(.22f,-.18f,.4f),Quaternion.identity,1),0,true);
                Render(scene,rig,"QA/PSXArms-contact.png");
                rig.ResetPose();Render(scene,rig,"QA/PSXArms-idle.png");
                CheckPark(scene,rig);
                return "PASS: prefab, Generic, 52 bindings, bare texture, HandLayer, independent left/right grasp and press, reachable contact, bounded reach, unchanged bone scales, reset, camera raise/lower, shutter feedback, synchronous joined-hand pose at actual meadow fallback geometry, focus/release gate; four preview renders. No gameplay or save writes.";
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);Cursor.lockState=originalCursor;Cursor.visible=originalVisible;}
        }
        static void Set(object instance,string name,object value)
        {instance.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(instance,value);}
        static void Callback(object instance,string name,bool value)
        {instance.GetType().GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(instance,new object[]{value});}
        static GameObject ObjectIn(UnityEngine.SceneManagement.Scene scene,string name)
        {var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
        static void CheckPark(UnityEngine.SceneManagement.Scene scene,FirstPersonArmRig rig)
        {
            var cursor=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                var player=ObjectIn(scene,"TestPlayer").AddComponent<ParkPhotoPrototype>();
                var camera=ObjectIn(scene,"TestCamera").AddComponent<Camera>();camera.transform.SetParent(player.transform,false);player.view=camera;
                var hands=camera.gameObject.AddComponent<FirstPersonHands>();hands.park=player;hands.skeletalRig=rig;rig.transform.SetParent(camera.transform,false);
                Set(player,"inputGate",false);
                hands.parkRestTargets=new Transform[2];hands.parkAimTargets=new Transform[2];
                for(int i=0;i<2;i++)
                {
                    hands.parkRestTargets[i]=ObjectIn(scene,"Rest").transform;hands.parkRestTargets[i].SetParent(camera.transform,false);hands.parkRestTargets[i].localPosition=new Vector3(i==0?-.23f:.23f,-.27f,.65f);
                    hands.parkAimTargets[i]=ObjectIn(scene,"Aim").transform;hands.parkAimTargets[i].SetParent(camera.transform,false);hands.parkAimTargets[i].localPosition=new Vector3(i==0?-.2f:.22f,-.2f,.4f);
                }
                Set(player,"photoMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);hands.Advance(1);
                Check(rig.animator.GetLayerWeight(1)==1 && rig.animator.GetLayerWeight(3)==1,"Camera requires both grip layers");
                Set(player,"flash",1f);hands.Advance(1);Check(rig.animator.GetLayerWeight(4)==1,"Accepted shutter has no press feedback");
                Set(player,"flash",0f);Set(player,"photoMode",ParkPhotoPrototype.PhotographyMode.Exploration);hands.Advance(1);
                Check(rig.animator.GetLayerWeight(1)==0 && rig.animator.GetLayerWeight(3)==0 && rig.animator.GetLayerWeight(4)==0,"Lowering camera retains grasp");
                // In-memory fixture only: never starts gameplay, advances story, exposes film or writes a save.
                var node=ObjectIn(scene,"PhotoNode").AddComponent<ParkPhotoWaypoint>();node.handContact=ObjectIn(scene,"Contact").transform;
                node.handContact.position=new Vector3(-.2f,-.22f,.4f);
                var companion=ObjectIn(scene,"Companion").AddComponent<ParkCompanion>();player.companion=companion;
                companion.route=new[]{node};companion.photoHoldNode=node;Set(companion,"atNode",true);
                var roll=new ParkFilmRoll{meadow=new BridgePhotoCheckpoint{handJoined=true,aligned=true}};
                roll.frames.Add(new ParkPhotoRecord());roll.frames.Add(new ParkPhotoRecord());Set(player,"roll",roll);Set(player,"bridgeNode",node);
                Check(player.HoldingPhotoHand,"Joined-hand fixture unavailable");
                hands.PosePhotoHandForShutter();
                Check(Vector3.Distance(rig.wrists[0].position,player.PhotoHandPosition)<.015f,"Shutter did not synchronously solve joined hand");
                Render(scene,rig,"QA/PSXArms-shutter.png");
                // Current scene fallback: chest 1.24m, eye 1.6m, camera 1.2m forward,
                // offered contact .38m forward / .18m below chest. Keep those anchors unchanged.
                camera.transform.position=new Vector3(0,1.6f,1.2f);
                camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.24f,0)-camera.transform.position);
                node.handContact.position=new Vector3(-.15f,1.06f,.38f);
                hands.PosePhotoHandForShutter();
                Check(Vector3.Distance(rig.wrists[0].position,player.PhotoHandPosition)<.015f,"Actual meadow fallback geometry exceeds configured reach: "+Vector3.Distance(rig.wrists[0].position,player.PhotoHandPosition)+" lean="+(rig.model.localPosition-rig.modelOffset));
                Check((rig.model.localPosition-rig.modelOffset).magnitude<=rig.maximumBodyReach+.001f,"Body lean exceeds limit");
                Render(scene,rig,"QA/PSXArms-meadow.png");
                Callback(hands,"OnApplicationFocus",false);Check(rig.animator.GetLayerWeight(1)==0,"Focus loss retains grasp");
                Callback(hands,"OnApplicationFocus",true);Set(player,"inputGate",true);hands.Advance(1);
                Check(rig.animator.GetLayerWeight(1)==0,"Handoff gate resumes stale joined-hand gesture");
                Set(player,"inputGate",false);Callback(hands,"OnApplicationPause",true);hands.Advance(1);
                Check(rig.animator.GetLayerWeight(1)==0,"Application pause resumes joined-hand gesture");Callback(hands,"OnApplicationPause",false);
            }
            finally{Cursor.lockState=cursor;Cursor.visible=visible;}
        }
        static void Render(UnityEngine.SceneManagement.Scene scene,FirstPersonArmRig rig,string path)
        {
            var cameraObject=new GameObject("PreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.fieldOfView=65;camera.nearClipPlane=.02f;camera.cullingMask=1<<27;
            camera.transform.SetPositionAndRotation(rig.transform.position,rig.transform.rotation);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.18f,.21f);
            var lightObject=new GameObject("PreviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(30,20,0);
            var rt=RenderTexture.GetTemporary(960,600,24);var old=RenderTexture.active;Texture2D texture=null;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(960,600,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,600),0,0);texture.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);}
        }
    }
}
