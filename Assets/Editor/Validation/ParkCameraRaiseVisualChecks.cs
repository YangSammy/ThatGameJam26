using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkCameraRaiseVisualChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Capture(string folder)
        {
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || p==null || !p.CanExpose)throw new InvalidOperationException("Loaded Park Play required; no save files are changed");
            Directory.CreateDirectory(folder);
            var type=p.GetType();
            Action<string,object> set=(name,value)=>type.GetField(name,Hidden).SetValue(p,value);
            Func<string,object[],object> call=(name,args)=>type.GetMethod(name,Hidden).Invoke(p,args);
            string[] fields={"aimBlend","inputGate","focused","virtualPointer","dwell","cooldown","edgeCandidate","edgeArmed","moveVelocity","verticalSpeed","handJoinTransition","raiseReleasePresentation"};
            var before=new Dictionary<string,object>();foreach(var field in fields)before[field]=type.GetField(field,Hidden).GetValue(p);
            var mode=p.PhotoMode;var camera=p.view;float fov=camera.fieldOfView,aspect=camera.aspect;
            var target=camera.targetTexture;var active=RenderTexture.active;
            var hands=camera.GetComponent<FirstPersonHands>();
            var handsType=hands.GetType();
            var handFocus=handsType.GetField("handFocused",Hidden);var handPause=handsType.GetField("handPaused",Hidden);
            object oldHandFocus=handFocus.GetValue(hands),oldHandPause=handPause.GetValue(hands);
            var transforms=hands.GetComponentsInChildren<Transform>(true);
            var positions=new Vector3[transforms.Length];var rotations=new Quaternion[transforms.Length];
            for(int i=0;i<transforms.Length;i++){positions[i]=transforms[i].localPosition;rotations[i]=transforms[i].localRotation;}
            var worldPosition=camera.transform.position;var worldRotation=camera.transform.rotation;
            var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var rows=new List<string>();
            try
            {
                call("SetPhotographyMode",new object[]{ParkPhotoPrototype.PhotographyMode.Body});
                set("inputGate",false);set("focused",true);camera.fieldOfView=PlayerControlSettings.Current.parkWalkingFov;camera.aspect=16f/9;
                handFocus.SetValue(hands,true);handPause.SetValue(hands,false);
                hands.Advance(.2f);call("BeginCameraRaise",null);
                for(int frame=0;frame<=60;frame++)
                {
                    if(frame>0)call("TickCameraRaise",new object[]{1f/60});
                    hands.Advance(1f/60);
                    if(frame!=0 && frame!=6 && frame!=12 && frame!=18 && frame!=24 && frame!=27 && frame!=29 && frame!=30 && frame!=31 && frame!=34 && frame!=48 && frame!=60)continue;
                    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"frame-"+frame.ToString("00")+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                    var body=(Transform)type.GetField("bodyRoot",Hidden).GetValue(p);float nearest=float.PositiveInfinity;
                    foreach(var mesh in body.GetComponentsInChildren<MeshFilter>())
                    {
                        if(!mesh.gameObject.activeInHierarchy)continue;
                        foreach(var vertex in mesh.sharedMesh.vertices)nearest=Mathf.Min(nearest,camera.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex)).z);
                    }
                    rows.Add("frame="+frame+" fov="+camera.fieldOfView+" mode="+p.PhotoMode+" positionError="+Vector3.Distance(worldPosition,camera.transform.position)+" angleError="+Quaternion.Angle(worldRotation,camera.transform.rotation)+" nearestVisibleCamera="+nearest+" occlusion="+p.focusEffect.raiseOcclusion+" handBlocked="+p.HandPresentationBlocked+" grip="+(p.CameraRaiseGrip!=null)+" rightWrist="+camera.transform.InverseTransformPoint(hands.skeletalRig.wrists[1].position).ToString("F3"));
                }
                File.WriteAllLines(Path.Combine(folder,"frames.txt"),rows);return string.Join("\n",rows);
            }
            finally
            {
                call("SetPhotographyMode",new object[]{mode});foreach(var pair in before)set(pair.Key,pair.Value);
                handFocus.SetValue(hands,oldHandFocus);handPause.SetValue(hands,oldHandPause);
                camera.fieldOfView=fov;camera.aspect=aspect;camera.targetTexture=target;RenderTexture.active=active;
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];}
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
