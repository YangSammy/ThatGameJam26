using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    // Editor-only controller integration fixture. Never changes production acceptance flags.
    [InitializeOnLoad]
    public static class ParkGirlPerformanceValidation
    {
        const string Key="ParkGirl.PerformanceV4";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly List<object> frames=new List<object>();
        static readonly List<string> errors=new List<string>();
        static readonly List<object> events=new List<object>();
        static float start,lastSample,stageTime,readySince=-1;
        static int stage,imageIndex,negativeTask=-1;
        static bool setup,pausePassed;
        static Vector3 pausePosition;
        static string film;
        static Camera review;
        static ParkGirlPerformanceValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
        static object Get(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Flags).Invoke(target,args);
        public static bool SaveTreesEqual(string current,string backup)
        {
            if(Directory.Exists(current)!=Directory.Exists(backup))return false;
            return SaveSignature(current).SequenceEqual(SaveSignature(backup));
        }
        static string[] SaveSignature(string root)
        {
            if(!Directory.Exists(root))return new string[0];
            var info=new DirectoryInfo(root);
            if((info.Attributes&FileAttributes.ReparsePoint)!=0||info.EnumerateFileSystemInfos("*",SearchOption.AllDirectories).Any(x=>(x.Attributes&FileAttributes.ReparsePoint)!=0))throw new InvalidOperationException("Save fixture must not contain reparse points.");
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return Directory.GetFiles(root,"*",SearchOption.AllDirectories).Select(file=>{using(var stream=File.OpenRead(file))return file.Substring(root.Length).Replace('\\','/')+":"+BitConverter.ToString(sha.ComputeHash(stream));}).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        }
        static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception))errors.Add(message);}
        [MenuItem("Tools/Art/Park Girl Performance/Validate Enabled Photography")]
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Directory.Exists(ParkGirlPrivacyBuild.Out+"/Backup/EditorPlaySession")&&!File.Exists(ParkGirlPrivacyBuild.Out+"/Backup/save-slot-absent.txt"))throw new InvalidOperationException("Back up the EditorPlaySession save slot first.");
            if(!SaveTreesEqual(GameSavePaths.EditorSessionRoot,ParkGirlPrivacyBuild.Out+"/Backup/EditorPlaySession"))throw new InvalidOperationException("Current test saves differ from the backup. Archive and back up current data before running this fixture.");
            var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            if(scenes.Any(s=>s.isDirty||string.IsNullOrEmpty(s.path)))throw new InvalidOperationException("Unsaved scene work must be preserved first.");
            SessionState.SetString(Key+".restore",JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));
            SessionState.SetString(Key+".active",SceneManager.GetActiveScene().path);
            SessionState.SetBool(Key+".pref",GameSavePaths.ResetOnEditorPlay);
            SessionState.SetBool(Key,true);
            EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,true);
            EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                setup=false;stage=0;negativeTask=-1;imageIndex=0;readySince=-1;pausePassed=false;frames.Clear();events.Clear();errors.Clear();
                start=Time.realtimeSinceStartup;lastSample=-1;
                // Loaded camera is a prerequisite fixture; alignment, turn, shutter and persistence use normal code.
                var roll=ParkFilmStore.SaveCameraLoading(null,new ParkCameraLoading{phase=PhotoChapter.FilmPhase.Ready,threaded=true,mechanicalStep=7,iso=100});film=roll.id;
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,SessionState.GetBool(Key+".pref",true));
                SessionState.SetBool(Key,false);SessionState.SetBool(Key+".restorePending",true);
            }
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
            try{Run();}catch(Exception e){errors.Add(e.ToString());Finish(false,"Fixture exception");}
        }
        static void Position(ParkPhotoPrototype p,Vector3 position,Quaternion rotation)
        {
            var motor=p.GetComponent<CharacterController>();motor.enabled=false;
            p.transform.SetPositionAndRotation(position,Quaternion.Euler(0,rotation.eulerAngles.y,0));motor.enabled=true;
            float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);Set(p,"yaw",rotation.eulerAngles.y);Set(p,"pitch",pitch);
            Set(p,"verticalSpeed",0f);p.view.transform.localRotation=Quaternion.Euler(pitch,0,0);
        }
        static void Run()
        {
            float elapsed=Time.realtimeSinceStartup-start;
            if(elapsed>125){Finish(false,"Timed out");return;}
            var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();if(p==null)return;
            if(!p.enabled)throw new InvalidOperationException("Normal photography controller disabled.");
            var c=p.companion;
            if(!setup)
            {
                if(Get(p,"motor")==null)return;
                setup=true;Set(p,"roll",ParkFilmStore.Load(film));Call(p,"InitializeBridge");Call(p,"OnApplicationFocus",true);p.Resume();
                review=new GameObject("TemporaryPerformanceReviewCamera").AddComponent<Camera>();review.enabled=false;review.fieldOfView=40;review.nearClipPlane=.05f;review.farClipPlane=150;review.cullingMask&=~(1<<FirstPersonHands.HandLayer);
                Directory.CreateDirectory(ParkGirlPrivacyBuild.Out+"/Preview/Gameplay");
            }
            if(stage==4)
            {
                if(Time.realtimeSinceStartup-stageTime<1.5f)return;
                var loaded=ParkFilmStore.LoadActive();
                bool retained=loaded!=null&&loaded.id==film&&loaded.frames.Count==2&&p.ExposedFrames==2&&loaded.entrance.captured&&loaded.bridge.captured;
                events.Add(new{kind="scene_reload",retained,frames=p.ExposedFrames,controller_enabled=p.enabled});
                Finish(retained&&pausePassed&&errors.Count==0,"Entrance + bridge normal controller integration");return;
            }
            if(p.Paused&&stage!=1){Call(p,"OnApplicationFocus",true);p.Resume();}
            if(stage==1)
            {
                if(Time.realtimeSinceStartup-stageTime<.5f)return;
                float drift=Vector3.Distance(c.transform.position,pausePosition);pausePassed=drift<.005f;
                events.Add(new{kind="pause_resume",drift_m=drift,pass=pausePassed});p.Resume();stage=2;
            }
            if(stage==3)
            {
                if(Time.realtimeSinceStartup-stageTime<.7f)return;
                Call(p,"PauseControls");stage=4;stageTime=Time.realtimeSinceStartup;
                SceneManager.LoadScene("ParkScene");return;
            }
            if(stage==0||stage==2)
            {
                var roll=(ParkFilmRoll)Get(p,"roll");string task=roll.NextTask;var checkpoint=roll.Task(task);
                if(!c.AtPhotoNode||checkpoint==null||!checkpoint.hasReference)
                {
                    if(p.Aiming)Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
                    var pos=c.transform.position-c.transform.forward*2.7f+c.transform.right*.4f;
                    Position(p,pos,Quaternion.LookRotation(c.transform.position+Vector3.up*1.2f-(pos+Vector3.up*PlayerControlSettings.Current.eyeHeight)));
                }
                else
                {
                    if(!p.Aiming)Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
                    Position(p,checkpoint.position-Vector3.up*PlayerControlSettings.Current.eyeHeight,Quaternion.Euler(checkpoint.euler));
                    Set(p,"virtualPointer",new Vector2(.5f,.5f));Set(p,"apertureIndex",3);Set(p,"speedIndex",5);
                    float depth=Vector3.Dot(checkpoint.subjectPoint-p.view.transform.position,p.view.transform.forward);
                    if(negativeTask!=p.ExposedFrames)
                    {
                        negativeTask=p.ExposedFrames;p.focusDistance=Mathf.Min(p.maximumFocus,depth+3);Call(p,"TickBridge",0f);int before=p.ExposedFrames;Call(p,"Capture");
                        if(p.ExposedFrames!=before)throw new InvalidOperationException("Wrong focus consumed film.");
                        events.Add(new{kind="wrong_focus_rejected",task,frames=before});
                    }
                    p.focusDistance=Mathf.Clamp(depth,p.minimumFocus,p.maximumFocus);
                    bool ready=(bool)Get(p,"bridgeReady");if(ready&&readySince<0)readySince=Time.realtimeSinceStartup;if(!ready)readySince=-1;
                    if(readySince>0&&Time.realtimeSinceStartup-readySince>.45f)
                    {
                        int before=p.ExposedFrames;Call(p,"Capture");if(p.ExposedFrames!=before+1)throw new InvalidOperationException("Ready shutter did not save a frame.");
                        var record=p.Records[p.ExposedFrames-1];if(!record.accepted)throw new InvalidOperationException("Photo not accepted.");
                        File.Copy(Path.Combine(ParkFilmStore.Root,film,record.negativeFile),ParkGirlPrivacyBuild.Out+"/Preview/"+task+"-exposure.png",true);
                        events.Add(new{kind="exposure",record=Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(record))});readySince=-1;stageTime=Time.realtimeSinceStartup;
                        if(p.ExposedFrames==1){Call(p,"PauseControls");pausePosition=c.transform.position;stage=1;}else stage=3;
                    }
                }
            }
            if(elapsed-lastSample<.12f)return;lastSample=elapsed;
            var visual=c.visualPresentation;
            frames.Add(new{time=elapsed,task=((ParkFilmRoll)Get(p,"roll")).NextTask,stage,at_node=c.AtPhotoNode,paused=p.Paused,aiming=p.Aiming,ready=(bool)Get(p,"bridgeReady"),moment=c.CurrentMoment.ToString(),root=c.transform.position.ToString("F4"),root_yaw=c.transform.eulerAngles.y,head_yaw=visual.headBone.eulerAngles.y,shoulder_yaw=visual.ShoulderYaw,hair_lag=visual.HairLag,speed=c.animator.GetFloat("Speed"),left_foot=visual.leftFoot.position.ToString("F4"),right_foot=visual.rightFoot.position.ToString("F4")});
            if(review!=null)
            {
                var target=c.transform.position+Vector3.up*.95f;review.transform.position=c.transform.position+c.transform.forward*2.8f+c.transform.right*1.6f+Vector3.up*1.5f;review.transform.LookAt(target);
                ParkGirlLocomotionValidation.Capture(review,ParkGirlPrivacyBuild.Out+"/Preview/Gameplay/frame_"+imageIndex++.ToString("D4")+".png");
            }
            ParkGirlPrivacyBuild.Report("integration-progress.json",new{elapsed,stage,task=((ParkFilmRoll)Get(p,"roll")).NextTask,frames=p.ExposedFrames,at_node=c.AtPhotoNode,aiming=p.Aiming,ready=(bool)Get(p,"bridgeReady"),notice=Get(p,"notice")});
        }
        static void Finish(bool pass,string reason)
        {
            try{ParkGirlPrivacyBuild.Report("enabled-photography-result.json",new{pass,reason,unity=Application.unityVersion,prefab_dependency_hash=AssetDatabase.GetAssetDependencyHash(ParkGirlLocomotionBuild.Prefab).ToString(),seconds=Time.realtimeSinceStartup-start,normal_photo_controller=true,native_mouse_keyboard_test=false,fixture="Loaded ISO100 film; editor drives player pose/camera controls and invokes existing shutter. Normal Update aligns and turns. No forced accepted/aligned flags. Test save backed up externally; restore after run.",film,events=events.ToArray(),samples=frames.ToArray(),errors=errors.ToArray(),art_anonymity_pass=false});}
            finally{EditorApplication.isPlaying=false;}
        }
    }
}
