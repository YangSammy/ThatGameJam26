using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MemoryWhitebox.EditorTools
{
    public static class LeftHandGestureChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
        static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
        static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        static GameObject Obj(UnityEngine.SceneManagement.Scene scene,string name)
        {var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}

        public static string Run(bool authoredMeadow=false)
        {
            var scene=EditorSceneManager.NewPreviewScene();var cursor=Cursor.lockState;bool visible=Cursor.visible;
            ParkPhotoPrototype player=null;
            var folder=authoredMeadow?"output/QA/PalmUpCover_20261106/meadow":"output/QA/PalmUpCover_20261106/front";Directory.CreateDirectory(folder);
            var report=new List<string>();
            try
            {
                player=Obj(scene,"Player").AddComponent<ParkPhotoPrototype>();
                var camera=Obj(scene,"Camera").AddComponent<Camera>();camera.scene=scene;camera.fieldOfView=48;camera.aspect=1.6f;camera.nearClipPlane=.02f;
                camera.transform.SetParent(player.transform,false);player.view=camera;
                camera.transform.position=new Vector3(0,1.6f,1.2f);
                camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,1.24f,0)-camera.transform.position);
                if(authoredMeadow)camera.transform.SetPositionAndRotation(new Vector3(-1.3000002f,1.65f,-.6999998f),new Quaternion(.11323516f,.5083067f,-.06763471f,.8510157f));
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.16f);
                var node=Obj(scene,"Node").AddComponent<ParkPhotoWaypoint>();node.handContact=Obj(scene,"HandContact").transform;
                node.handContact.position=new Vector3(-.15f,1.06f,.38f);
                if(authoredMeadow)node.handContact.position=new Vector3(-.47071266f,1.08f,.030477524f);
                var companion=Obj(scene,"Companion").AddComponent<ParkCompanion>();player.companion=companion;
                companion.player=player.transform;companion.route=new[]{node};companion.photoHoldNode=node;companion.photoOfferHand=true;
                companion.photoHandTarget=node.handContact.position;Set(companion,"atNode",true);
                if(authoredMeadow)companion.transform.rotation=Quaternion.LookRotation(new Vector3(camera.transform.position.x,0,camera.transform.position.z));
                companion.transform.rotation*=Quaternion.Euler(0,-12,0);
                var girlObject=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/ParkGirlTripoRigged/Prefabs/PF_ParkGirlTripo_Locomotion.prefab"),scene);
                girlObject.transform.SetParent(companion.transform,false);
                var girl=girlObject.GetComponentInChildren<ParkGirlPresentation>();girl.companion=companion;companion.visualPresentation=girl;
                Call(girl,"Awake");Set(girl,"<OfferWeight>k__BackingField",1f);
                var girlAnimator=girlObject.GetComponentInChildren<Animator>();girlAnimator.Rebind();girlAnimator.Play("OfferHandHold",0,0);girlAnimator.Update(0);girl.ApplyPhotoPose();
                var rigObject=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FirstPersonArmAssets.PrefabPath),scene);
                var rig=rigObject.GetComponent<FirstPersonArmRig>();rigObject.transform.SetParent(camera.transform,false);
                var hands=camera.gameObject.AddComponent<FirstPersonHands>();hands.park=player;hands.skeletalRig=rig;
                hands.parkAimTargets=new Transform[2];
                for(int i=0;i<2;i++){var t=Obj(scene,"Aim").transform;t.SetParent(camera.transform,false);t.localPosition=new Vector3(i==0?-.2f:.22f,-.2f,.4f);hands.parkAimTargets[i]=t;}
                Set(player,"inputGate",false);Set(player,"photoMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                var roll=new ParkFilmRoll{meadow=new BridgePhotoCheckpoint{aligned=true}};
                roll.frames.Add(new ParkPhotoRecord());roll.frames.Add(new ParkPhotoRecord());Set(player,"roll",roll);Set(player,"bridgeNode",node);
                for(int i=0;i<60;i++)hands.Advance(1f/60);
                Check(rig.animator.GetLayerWeight(1)>.99f,"Fixture must start in the old camera grip");
                var light=Obj(scene,"Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,-25,0);
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                roll.meadow.handJoined=true;
                for(int frame=0;frame<=60;frame++)
                {
                    // Joining must not curl the girl's supporting palm into a fist.
                    girl.ApplyPhotoPose();
                    var girlPalmBefore=girl.PalmPosition;var girlArmBefore=girl.rightUpperArm.rotation;var girlWristBefore=girl.rightHand.rotation;
                    hands.Advance(1f/60);
                    Check(Vector3.Distance(girlPalmBefore,girl.PalmPosition)<.000001f && Quaternion.Angle(girlArmBefore,girl.rightUpperArm.rotation)<.001f && Quaternion.Angle(girlWristBefore,girl.rightHand.rotation)<.001f,"Player pose changed the girl's offered hand");
                    float clearance=(float)typeof(FirstPersonHands).GetMethod("HandholdFingerClearance",Private).Invoke(hands,null);
                    if(clearance<-.001f){Call(hands,"ApplyDeskHandVisibility");Render(scene,camera,folder+"/clearance-failure.png");}
                    Check(clearance>=-.001f,"Finger clearance failed during reach at frame "+frame+" gap="+clearance+" palm="+Vector3.Distance(hands.PhotoPalmPosition,girl.PalmPosition));
                    Call(hands,"ApplyDeskHandVisibility");
                    if(frame==0)Check(rig.animator.GetLayerWeight(1)<.01f,"Left hand appears already closed");
                    if(frame==10)Check(hands.PhotoHandGrip==0,"Fingers close before contact");
                    if(frame==6)
                    {
                        var before=rig.wrists[0].position;float grip=hands.PhotoHandGrip;
                        Set(player,"inputGate",true);hands.Advance(.5f);Call(hands,"ApplyDeskHandVisibility");
                        Check(skin.forceRenderingOff,"Input gate left an arm visible");
                        Check(Vector3.Distance(before,rig.wrists[0].position)<.001f && hands.PhotoHandGrip==grip,"Input gate advanced gesture");
                        Set(player,"inputGate",false);hands.PosePhotoHandForShutter();
                        Check(Vector3.Distance(before,rig.wrists[0].position)<.001f,"Shutter jumps ahead of reach animation");
                    }
                    if(frame==0||frame==10||frame==17||frame==26||frame==60)
                    {
                        report.Add("Frame "+frame+" grip="+hands.PhotoHandGrip.ToString("F3")+" palm-anchor separation="+Vector3.Distance(hands.PhotoPalmPosition,girl.PalmPosition).ToString("F4")+" finger envelope clearance="+((float)typeof(FirstPersonHands).GetMethod("HandholdFingerClearance",Private).Invoke(hands,null)).ToString("F4"));
                        Render(scene,camera,folder+"/reach-"+frame+".png");
                    }
                }
                Check(hands.PhotoHandGrip>.99f,"Grip never completes");
                Check(Vector3.Dot(-girl.rightHand.right,Vector3.up)>.98f,"Girl's anatomical palm must face up");
                foreach(var bone in girl.rightHand.GetComponentsInChildren<Transform>())
                    if(!bone.name.Contains("Thumb") && bone.name.EndsWith("Distal") && bone.childCount>0)
                        Check(Vector3.Angle(bone.GetChild(0).position-bone.position,girl.rightHand.up)<25,"Girl must keep an open supporting palm: "+bone.name);
                Check(Vector3.Dot(hands.PhotoPalmPosition-girl.PalmPosition,Vector3.up)>.02f,"Player palm must cover the girl from above");
                report.Add("Palm up="+Vector3.Dot(-girl.rightHand.right,Vector3.up)+" player height above palm="+(hands.PhotoPalmPosition.y-girl.PalmPosition.y));
                var detail=Obj(scene,"Hand contact detail").AddComponent<Camera>();
                try
                {
                    detail.CopyFrom(camera);detail.scene=scene;
                    var centre=(hands.PhotoPalmPosition+girl.PalmPosition)*.5f;
                    detail.transform.position=centre+(camera.transform.position-centre).normalized*.38f;
                    detail.transform.rotation=Quaternion.LookRotation(centre-detail.transform.position,camera.transform.up);
                    Render(scene,detail,folder+"/contact-detail.png");
                }
                finally{UnityEngine.Object.DestroyImmediate(detail.gameObject);}
                var shoulder=rig.upperArms[0].position;var wrist=rig.wrists[0].position;var elbow=rig.forearms[0].position;
                var axis=(wrist-shoulder).normalized;
                var bend=Vector3.ProjectOnPlane(elbow-shoulder,axis).normalized;
                var down=Vector3.ProjectOnPlane(-camera.transform.up,axis).normalized;
                Check(Vector3.Angle(wrist-elbow,rig.wrists[0].up)<25,"Wrist bends more than 25 degrees");
                float armGap=(float)typeof(FirstPersonHands).GetMethod("HandholdForearmClearance",Private).Invoke(hands,null);
                Check(armGap>=-.001f,"Player forearm crosses girl's forearm: "+armGap);
                report.Add("Elbow plane alignment="+Vector3.Dot(bend,down).ToString("F3")+"; forearm envelope clearance="+armGap.ToString("F4"));
                report.Add("Wrist angle="+Vector3.Angle(wrist-elbow,rig.wrists[0].up)+" shoulder viewport="+camera.WorldToViewportPoint(shoulder));
                Check(Vector3.Distance(hands.PhotoPalmPosition,girl.PalmPosition)<.045f,"Palm contact exceeds skin clearance budget");
                Check(camera.WorldToViewportPoint(shoulder).y<-.1f,"Arm root enters the picture");
                Check((float)typeof(FirstPersonHands).GetMethod("HandholdFingerClearance",Private).Invoke(hands,null)>=0,"Player fingers intersect girl's hand envelopes");
                Check(Vector3.Dot(rig.wrists[0].right,girl.PalmNormal)>.98f,"Palms are not facing one another");
                Check(skin.sharedMesh==Resources.Load<Mesh>("Player/FirstPersonLeftArm"),"Wrong visible hand mesh");
                Check(rig.animator.GetLayerWeight(3)==0,"Right hand grip leaked into handhold");
                var held=rig.wrists[0].position;hands.PosePhotoHandForShutter();
                Check(Vector3.Distance(held,rig.wrists[0].position)<.001f,"Held shutter capture pops");
                roll.meadow.handJoined=false;
                Set(player,"<CurrentPauseReason>k__BackingField",ParkPhotoPrototype.PauseReason.FilmComplete);Set(player,"inputGate",true);
                for(int frame=0;frame<36;frame++)
                {
                    hands.Advance(1f/60);Call(hands,"ApplyDeskHandVisibility");
                    if(frame==2)Check(!skin.forceRenderingOff && hands.PhotoHandGrip<1,"Release should remain visible and open fingers");
                    if(frame==5||frame==16||frame==35)Render(scene,camera,folder+"/release-"+frame+".png");
                }
                Check(skin.forceRenderingOff,"Left arm remains after withdrawal");
                Set(player,"<CurrentPauseReason>k__BackingField",ParkPhotoPrototype.PauseReason.None);Set(player,"inputGate",false);
                roll.meadow.handJoined=true;hands.Advance(.1f);
                Set(player,"photoMode",ParkPhotoPrototype.PhotographyMode.Meter);hands.Advance(.1f);Call(hands,"ApplyDeskHandVisibility");
                Check(skin.forceRenderingOff && hands.PhotoHandGrip==0,"Meter view retained hand gesture");
                Set(player,"photoMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);hands.Advance(.01f);Call(hands,"ApplyDeskHandVisibility");
                Check(!skin.forceRenderingOff && hands.PhotoHandGrip==0,"Re-entry must start with an open left hand");
                Call(hands,"OnApplicationPause",true);hands.Advance(.5f);Check(skin.forceRenderingOff,"Application pause retained hand");
                Call(hands,"OnApplicationPause",false);
                Check(roll.frames.Count==2 && roll.meadow.handJoined,"Presentation mutated film progress");
                roll.meadow.handJoined=false;
                for(int i=0;i<36;i++)hands.Advance(1f/60);
                hands.PosePhotoHandForShutter();Check(skin.forceRenderingOff,"Cancel during reach did not finish withdrawal");
                report.Add("PASS: open reach; contact before grip; left-only; input gate; shutter no skip; release; hidden meter; re-entry; pause; unchanged fixture checkpoint. Isolated real-model poses, not real input or ParkScene playthrough.");
                File.WriteAllLines(folder+"/checks.txt",report);return string.Join("\n",report);
            }
            finally{if(player!=null)Set(player,"roll",null);EditorSceneManager.ClosePreviewScene(scene);Cursor.lockState=cursor;Cursor.visible=visible;}
        }

        static void Render(UnityEngine.SceneManagement.Scene scene,Camera camera,string file)
        {
            // Explicit CPU bake makes each within-frame sample independent of GPU skinning caches.
            var clones=new List<GameObject>();var meshes=new List<Mesh>();var skins=new List<SkinnedMeshRenderer>();
            var rt=RenderTexture.GetTemporary(960,600,24);var old=RenderTexture.active;Texture2D image=null;
            var renderObject=Obj(scene,"Pose preview camera");var renderCamera=renderObject.AddComponent<Camera>();
            renderCamera.CopyFrom(camera);renderCamera.scene=scene;renderCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(!skin.enabled || skin.forceRenderingOff)continue;
                    var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                    // Imported FBX root scale is already included by BakeMesh in this rig.
                    var baked=Obj(scene,"Baked pose");baked.layer=skin.gameObject.layer;baked.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);
                    baked.AddComponent<MeshFilter>().sharedMesh=mesh;baked.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                    clones.Add(baked);skins.Add(skin);skin.forceRenderingOff=true;
                }
                renderCamera.targetTexture=rt;renderCamera.Render();RenderTexture.active=rt;
                image=new Texture2D(960,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,600),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
            }
            finally
            {
                renderCamera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(renderObject);
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                foreach(var go in clones)UnityEngine.Object.DestroyImmediate(go);
                foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var skin in skins)skin.forceRenderingOff=false;
            }
        }
    }
}
