using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    [InitializeOnLoad]
    public static class ParkGirlLocomotionValidation
    {
        const string Key="ParkGirl.LocomotionValidation";
        static readonly List<Frame> frames=new List<Frame>();
        static readonly List<string> errors=new List<string>();
        static float last=-1;static int imageIndex;static Camera captureCamera;
        [Serializable] public sealed class Frame
        {
            public float time,speed,rootZ,hipsX,hipsZ,kneeAngle,walkWeight,idleWeight;public bool walking;public float visualOffset;
        }
        static ParkGirlLocomotionValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
        static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception))errors.Add(message);}
        [MenuItem("Tools/Art/Park Girl Locomotion/Validate Sample Play Mode")]
        public static void StartSample(){Start(false);}
        [MenuItem("Tools/Art/Park Girl Locomotion/Validate Park Play Mode")]
        public static void StartPark(){Start(true);}
        static void Start(bool park)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            if(scenes.Any(s=>s.isDirty||string.IsNullOrEmpty(s.path)))throw new InvalidOperationException("Validation preserves open scenes; save unsaved work first.");
            SessionState.SetString(Key+".restore",JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));
            SessionState.SetString(Key+".active",SceneManager.GetActiveScene().path);
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+".park",park);
            SessionState.SetBool(Key+".saveReset",GameSavePaths.ResetOnEditorPlay);
            SessionState.SetBool(Key+".setup",false);
            frames.Clear();errors.Clear();last=-1;imageIndex=0;
            var scene=EditorSceneManager.OpenScene(park?"Assets/Scenes/ParkScene.unity":ParkGirlLocomotionBuild.ScenePath,OpenSceneMode.Single);
            if(park)
            {
                // Ephemeral test isolation: prevent chapter/save/input bootstraps from running.
                // These changes are discarded on restoration, never saved into ParkScene.
                var keep=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParkCompanion>(true)).Single();
                foreach(var script in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
                    if(script!=keep && !(script is ParkGirlPresentation))script.enabled=false;
                keep.Paused=true;
                var sequence=new GameObject("TemporaryParkLocomotionTest").AddComponent<ParkGirlLocomotionPreview>();sequence.companion=keep;
                var cam=new GameObject("TemporaryParkReviewCamera").AddComponent<Camera>();cam.fieldOfView=34;cam.nearClipPlane=.05f;cam.farClipPlane=150;sequence.reviewCamera=cam;
                cam.cullingMask&=~(1<<FirstPersonHands.HandLayer);
                foreach(var renderer in keep.player.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                // Keep the actual authored player, route and contact points intact. The test's nearby observer is temporary.
                keep.player=new GameObject("TemporaryObserver").transform;keep.player.position=keep.transform.position-keep.transform.forward*3;
            }
            // Art validation never reads/writes gameplay saves. Suppress the project's global
            // reset-on-play hook for this run and restore the user's preference on exit.
            EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,false);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(SessionState.GetBool(Key+".restorePending",false)&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Key+".restorePending",false);
                var paths=JsonConvert.DeserializeObject<string[]>(SessionState.GetString(Key+".restore","[]"));
                for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
                var active=SceneManager.GetSceneByPath(SessionState.GetString(Key+".active",""));if(active.IsValid())SceneManager.SetActiveScene(active);
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
            var sequence=UnityEngine.Object.FindObjectOfType<ParkGirlLocomotionPreview>();if(sequence==null)return;
            bool park=SessionState.GetBool(Key+".park",false);
            if(!SessionState.GetBool(Key+".setup",false))
            {SessionState.SetBool(Key+".setup",true);frames.Clear();last=-1;imageIndex=0;captureCamera=sequence.reviewCamera;}
            float time=sequence.Elapsed;if(time<=0||time-last<.1f)return;last=time;
            var c=sequence.companion;var animator=c.animator;
            var hips=animator.transform.Find("Armature/Hips");var knee=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="Left_LowerLeg");
            var p=c.transform.InverseTransformPoint(hips.position);
            var clips=animator.GetCurrentAnimatorClipInfo(0);
            float ww=clips.Where(x=>x.clip.name=="AN_ParkGirl_WalkInPlace").Sum(x=>x.weight),iw=clips.Where(x=>x.clip.name=="AN_ParkGirl_IdleLoop").Sum(x=>x.weight);
            frames.Add(new Frame{time=time,speed=animator.GetFloat("Speed"),walking=c.CurrentMoment==ParkCompanion.Moment.Walking&&!c.Paused,rootZ=c.transform.position.z,hipsX=p.x,hipsZ=p.z,kneeAngle=knee.localEulerAngles.x,visualOffset=c.visualPresentation.transform.localPosition.magnitude,walkWeight=ww,idleWeight=iw});
            string dir=ParkGirlLocomotionBuild.Out+"/Preview/"+(park?"Park":"Sample");Directory.CreateDirectory(dir);
            Capture(captureCamera,dir+"/frame_"+imageIndex.ToString("D4")+".png");imageIndex++;
            if(time<12.7f)return;
            bool stationary=frames.All(f=>f.visualOffset<.0001f)&&frames.All(f=>Mathf.Abs(f.hipsX)<.15f&&Mathf.Abs(f.hipsZ)<.15f);
            Func<float,float,float,bool> speedBelow=(a,b,limit)=>frames.Any(f=>f.time>a&&f.time<b)&&frames.Where(f=>f.time>a&&f.time<b).All(f=>f.speed<limit);
            bool idle=speedBelow(.6f,1.8f,.02f),stop=speedBelow(7,7.8f,.03f);
            bool walk=frames.Any(f=>f.time>3&&f.time<5&&f.speed>1.2f),resume=frames.Any(f=>f.time>9&&f.time<11&&f.speed>1.2f);
            bool actualClips=frames.Any(f=>f.time>.6f&&f.time<1.8f&&f.idleWeight>.98f)&&frames.Any(f=>f.time>3&&f.time<5&&f.walkWeight>.95f)&&frames.Any(f=>f.time>7&&f.time<7.8f&&f.idleWeight>.97f)&&frames.Any(f=>f.time>9&&f.time<11&&f.walkWeight>.95f);
            var angles=frames.Where(f=>f.walkWeight>.95f).Select(f=>Mathf.DeltaAngle(frames[0].kneeAngle,f.kneeAngle)).ToArray();
            bool kneeAnimated=angles.Length>5&&angles.Max()-angles.Min()>5;
            bool blend=frames.Any(f=>f.walkWeight>.05f&&f.walkWeight<.95f);
            bool moved=Vector3.Distance(c.transform.position,c.spawn.position)>2;
            bool photoAdapter=!park||CheckPhotoAdapter(c);
            bool pass=stationary&&idle&&stop&&walk&&resume&&blend&&moved&&actualClips&&kneeAnimated&&photoAdapter&&errors.Count==0;
            var report=new{pass,asset_hash=AssetDatabase.GetAssetDependencyHash(ParkGirlLocomotionBuild.Prefab).ToString(),play_mode=Application.isPlaying,scene=SceneManager.GetActiveScene().path,seconds=time,captures=imageIndex,stationary_visual=stationary,idle,walk,stop,resume,blend,moved,actual_clips=actualClips,knee_animated=kneeAnimated,photo_adapter=photoAdapter,errors=errors.ToArray(),scope=park?"Actual Park companion, motor, route, colliders and visual; chapter/input/save behaviours disabled only in unsaved test scene. Not end-to-end photography.":"Independent preview using real ParkCompanion/CharacterController and derived Animator clips.",frames=frames.ToArray()};
            ParkGirlLocomotionBuild.Report((park?"park":"sample")+"-play-result.json",report);
            if(pass)ParkGirlLocomotionBuild.Report((park?"park":"sample")+"-play-pass.json",report);
            EditorApplication.isPlaying=false;
        }
        static bool CheckPhotoAdapter(ParkCompanion c)
        {
            var p=c.visualPresentation;var proxy=c.photoHeadPivot.localRotation;var oldTarget=c.photoHandTarget;bool offer=c.photoOfferHand;
            var initial=p.headBone.rotation;c.photoHeadPivot.localRotation=proxy*Quaternion.Euler(0,20,0);p.ApplyPhotoPose();
            var first=p.headBone.rotation;p.ApplyPhotoPose();
            float response=Quaternion.Angle(initial,first),repeat=Quaternion.Angle(first,p.headBone.rotation);
            c.photoHandTarget=p.rightHand.position+c.transform.forward*.08f;float before=Vector3.Distance(p.rightHand.position,c.photoHandTarget);c.photoOfferHand=true;p.ApplyPhotoPose();
            float after=Vector3.Distance(p.rightHand.position,c.photoHandTarget);
            bool pass=response>15&&response<25&&repeat<.01f&&after<before*.5f;
            ParkGirlLocomotionBuild.Report("photo-adapter-smoke.json",new{pass,head_response_degrees=response,repeated_application_delta_degrees=repeat,hand_error_before_m=before,hand_error_after_m=after,scope="Nearby reachable hand target and repeat shutter pose application, not final meadow blocking or art acceptance"});
            c.photoHeadPivot.localRotation=proxy;c.photoHandTarget=oldTarget;c.photoOfferHand=offer;p.ApplyPhotoPose();return pass;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
            EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,SessionState.GetBool(Key+".saveReset",true));
            SessionState.SetBool(Key,false);SessionState.SetBool(Key+".restorePending",true);
        }
        public static void Capture(Camera camera,string path)
        {
            var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(720,720,24,RenderTextureFormat.ARGB32);var image=new Texture2D(720,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,720,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
