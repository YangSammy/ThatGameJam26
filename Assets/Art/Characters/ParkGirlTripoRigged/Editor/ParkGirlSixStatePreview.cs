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
 [InitializeOnLoad]
 public static class ParkGirlSixStatePreview
 {
  public const string Out="output/Characters/ParkGirlTripo/SixStatePreview";
  const string Key="ParkGirl.SixStatePreview";
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static readonly List<object> frames=new List<object>();static readonly List<object> events=new List<object>();static readonly List<string> errors=new List<string>();
  static float start,lastFrame,readyAt=-1,shutterAt=-1,joinAt=-1;static int imageIndex;static string film,taskCamera="";static bool setup,looked,offered;static Camera review;
  static ParkGirlSixStatePreview(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
  static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
  static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
  static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
  static void Write(string file,object o){Directory.CreateDirectory(Out+"/Reports");File.WriteAllText(Out+"/Reports/"+file,JsonConvert.SerializeObject(o,Formatting.Indented));}
  static void Log(string m,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception))errors.Add(m);}
  static void Event(string name){events.Add(new{name,time=Time.realtimeSinceStartup-start});}
  public static void Start()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor is busy.");
   if(!ParkGirlPerformanceValidation.SaveTreesEqual(GameSavePaths.EditorSessionRoot,Out+"/Backup/EditorPlaySession"))throw new InvalidOperationException("Current save slot must match this preview's fresh backup.");
   var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();if(scenes.Any(s=>s.isDirty||string.IsNullOrEmpty(s.path)))throw new InvalidOperationException("Unsaved scene work.");
   SessionState.SetString(Key+".restore",JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));SessionState.SetString(Key+".active",SceneManager.GetActiveScene().path);SessionState.SetBool(Key+".pref",GameSavePaths.ResetOnEditorPlay);SessionState.SetBool(Key,true);
   EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,true);EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;
  }
  static void Changed(PlayModeStateChange state)
  {
   if(!SessionState.GetBool(Key,false))return;
   if(state==PlayModeStateChange.EnteredPlayMode){setup=false;looked=offered=false;taskCamera="";imageIndex=0;readyAt=shutterAt=joinAt=-1;start=Time.realtimeSinceStartup;lastFrame=-1;frames.Clear();events.Clear();errors.Clear();film=ParkFilmStore.SaveCameraLoading(null,new ParkCameraLoading{phase=PhotoChapter.FilmPhase.Ready,threaded=true,mechanicalStep=7,iso=100}).id;}
   if(state==PlayModeStateChange.EnteredEditMode){EditorPrefs.SetBool(GameSavePaths.ResetPreferenceKey,SessionState.GetBool(Key+".pref",true));SessionState.SetBool(Key,false);SessionState.SetBool(Key+".restorePending",true);}
  }
  static void Tick()
  {
   if(SessionState.GetBool(Key+".restorePending",false)&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Key+".restorePending",false);var paths=JsonConvert.DeserializeObject<string[]>(SessionState.GetString(Key+".restore","[]"));for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);var active=SceneManager.GetSceneByPath(SessionState.GetString(Key+".active",""));if(active.IsValid())SceneManager.SetActiveScene(active);}
   if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
   try{Run();}catch(Exception e){errors.Add(e.ToString());Finish(false,"Recording exception");}
  }
  static void Position(ParkPhotoPrototype p,Vector3 position,Quaternion rotation)
  {
   var motor=p.GetComponent<CharacterController>();motor.enabled=false;p.transform.SetPositionAndRotation(position,Quaternion.Euler(0,rotation.eulerAngles.y,0));motor.enabled=true;
   float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);Set(p,"yaw",rotation.eulerAngles.y);Set(p,"pitch",pitch);Set(p,"verticalSpeed",0f);p.view.transform.localRotation=Quaternion.Euler(pitch,0,0);
  }
  static void Run()
  {
   float now=Time.realtimeSinceStartup,elapsed=now-start;if(elapsed>160){Finish(false,"Timed out");return;}
   var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();if(p==null)return;if(!p.enabled)throw new InvalidOperationException("Photography controller disabled.");var c=p.companion;
   if(!setup)
   {
    if(Get(p,"motor")==null)return;setup=true;Set(p,"roll",ParkFilmStore.Load(film));Call(p,"InitializeBridge");Call(p,"OnApplicationFocus",true);p.Resume();var hands=p.view.GetComponent<FirstPersonHands>();if(hands!=null)Call(hands,"OnApplicationFocus",true);
    review=new GameObject("TemporaryGirlPerformanceCamera").AddComponent<Camera>();review.enabled=false;review.fieldOfView=38;review.nearClipPlane=.05f;review.farClipPlane=150;review.cullingMask&=~(1<<FirstPersonHands.HandLayer);
    Directory.CreateDirectory(Out+"/Frames/Observer");Directory.CreateDirectory(Out+"/Frames/Player");
   }
   if(p.Paused){Call(p,"OnApplicationFocus",true);p.Resume();}
   var roll=(ParkFilmRoll)Get(p,"roll");string task=roll.NextTask;var checkpoint=roll.Task(task);
   bool afterEntrance=shutterAt>0&&now-shutterAt<1.2f;
   bool interesting=afterEntrance||(c.AtPhotoNode&&checkpoint!=null&&checkpoint.hasReference);
   if(!afterEntrance)
   {
    if(!c.AtPhotoNode||checkpoint==null||!checkpoint.hasReference)
    {
     if(p.Aiming)Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);
     var pos=c.transform.position-c.transform.forward*2.7f+c.transform.right*.4f;Position(p,pos,Quaternion.LookRotation(c.transform.position+Vector3.up*1.2f-(pos+Vector3.up*PlayerControlSettings.Current.eyeHeight)));
    }
    else
    {
     if(taskCamera!=task)
     {
      taskCamera=task;readyAt=-1;var direction=checkpoint.position-c.transform.position;direction.y=0;direction.Normalize();
      if(task=="entrance")direction=(c.transform.forward+c.transform.right*.45f).normalized;else direction=Quaternion.Euler(0,-28,0)*direction;
      review.transform.position=c.transform.position+direction*2.6f+Vector3.up*1.42f;review.transform.LookAt(c.transform.position+Vector3.up*1.05f);Event(task+"_reference_ready");
     }
     if(!p.Aiming)Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Viewfinder);
     Position(p,checkpoint.position-Vector3.up*PlayerControlSettings.Current.eyeHeight,Quaternion.Euler(checkpoint.euler));Set(p,"virtualPointer",new Vector2(.5f,.5f));Set(p,"apertureIndex",3);Set(p,"speedIndex",5);
     p.focusDistance=Mathf.Clamp(Vector3.Dot(checkpoint.subjectPoint-p.view.transform.position,p.view.transform.forward),p.minimumFocus,p.maximumFocus);
     if(task=="bridge"&&c.photoLookBack&&!looked){looked=true;Event("bridge_turn_start");}
     if(task=="meadow"&&c.photoOfferHand&&!offered){offered=true;Event("meadow_offer_start");}
     bool ready=(bool)Get(p,"bridgeReady");if(ready&&readyAt<0){readyAt=now;Event(task+"_ready");}
     if(ready&&readyAt>0&&task!="meadow"&&now-readyAt>(task=="entrance"?1.2f:4.5f))
     {
      int before=p.ExposedFrames;Event(task+"_shutter");Call(p,"Capture");if(p.ExposedFrames!=before+1)throw new InvalidOperationException("Shutter did not save a frame.");var record=p.Records[p.ExposedFrames-1];
      File.Copy(Path.Combine(ParkFilmStore.Root,film,record.negativeFile),Out+"/"+task+"-exposure.png",true);
      if(task=="entrance"){shutterAt=now;Capture(review,Out+"/entrance-shutter-observer.png",720,720);}readyAt=-1;
     }
     if(ready&&task=="meadow"&&readyAt>0&&now-readyAt>5&&joinAt<0)
     {
      var screen=p.view.WorldToViewportPoint(p.PhotoHandPosition);if(screen.z<=0||screen.x<0||screen.x>1||screen.y<0||screen.y>1)throw new InvalidOperationException("Hand target outside view.");
      Set(p,"virtualPointer",new Vector2(screen.x,screen.y));int before=p.ExposedFrames;Call(p,"Capture");if(!p.HoldingPhotoHand||p.ExposedFrames!=before)throw new InvalidOperationException("Normal offered-hand click failed or used film.");joinAt=now;Event("hand_joined");
     }
    }
   }
   if(interesting&&elapsed-lastFrame>=.10f)
   {
    lastFrame=elapsed;string id=imageIndex.ToString("D4");Capture(review,Out+"/Frames/Observer/frame_"+id+".png",720,720);Capture(p.view,Out+"/Frames/Player/frame_"+id+".png",960,600);
    var visual=c.visualPresentation;float error=c.photoOfferHand?Vector3.Distance(visual.rightHand.position,p.PhotoHandPosition):-1;
    frames.Add(new{index=imageIndex,time=elapsed,task=afterEntrance?"entrance":task,look_back=c.photoLookBack,offer_hand=c.photoOfferHand,joined=p.HoldingPhotoHand,hand_error_m=error,shoulder_yaw=visual.ShoulderYaw,hair_lag=visual.HairLag});imageIndex++;
   }
   Write("progress.json",new{elapsed,task,at_node=c.AtPhotoNode,frames=p.ExposedFrames,ready=(bool)Get(p,"bridgeReady"),joined=p.HoldingPhotoHand,images=imageIndex});
   if(joinAt>0&&now-joinAt>4.5f)Finish(errors.Count==0,"Six current states recorded");
  }
  static void Capture(Camera camera,string path,int width,int height)
  {
   var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
   finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
  }
  static void Finish(bool pass,string reason)
  {
   try{Write("recording.json",new{pass,reason,seconds=Time.realtimeSinceStartup-start,normal_controller=true,native_mouse_keyboard=false,production_assets_modified=false,scope="Current procedural performance, not final clips. Editor controls player/viewpoint and invokes normal shutter; existing Update performs alignment, look-back, offer and hand-join gates. Observer camera is temporary. Player camera retains existing rendering effects.",events=events.ToArray(),frames=frames.ToArray(),errors=errors.ToArray()});}
   finally{EditorApplication.isPlaying=false;}
  }
 }
}
