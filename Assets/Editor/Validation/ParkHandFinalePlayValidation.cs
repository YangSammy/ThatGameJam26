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
 public static class ParkHandFinalePlayValidation
 {
  public const string Out="output/QA/HandFinale20261107/Play";
  const string Key="Park.HandFinale20261107";
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static readonly List<object> frames=new List<object>();static readonly List<object> events=new List<object>();static readonly List<string> errors=new List<string>();
  static float start,lastFrame,readyAt=-1,shutterAt=-1,joinAt=-1;static int imageIndex;static string film,taskCamera="";static bool setup,looked,offered;static Camera review,contactCamera,portrait;
  static string pendingShot;static int pendingCount;static float pendingAt;
  static int checksStage;static float checksAt,pausedOffer;static Vector3 pausedPalm;
  static ParkHandFinalePlayValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Log;}
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
   if(state==PlayModeStateChange.EnteredPlayMode){checksStage=0;pendingShot=null;setup=false;looked=offered=false;taskCamera="";imageIndex=0;readyAt=shutterAt=joinAt=-1;start=Time.realtimeSinceStartup;lastFrame=-1;frames.Clear();events.Clear();errors.Clear();film=ParkFilmStore.SaveCameraLoading(null,new ParkCameraLoading{phase=PhotoChapter.FilmPhase.Ready,threaded=true,mechanicalStep=7,iso=100}).id;}
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
   if(checksStage==4){CheckDarkroom(now);return;}
   var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();if(p==null)return;if(!p.enabled)throw new InvalidOperationException("Photography controller disabled.");var c=p.companion;
   var routeNode=(ParkPhotoWaypoint)Get(p,"bridgeNode");if(routeNode!=null&&!c.AtPhotoNode&&!p.GuidedDeparturePending)c.ResumeAtPhotoNode(routeNode);
   if(!setup)
   {
    if(Get(p,"motor")==null)return;setup=true;Set(p,"roll",ParkFilmStore.Load(film));Call(p,"InitializeBridge");Call(p,"OnApplicationFocus",true);p.Resume();var hands=p.view.GetComponent<FirstPersonHands>();if(hands!=null)Call(hands,"OnApplicationFocus",true);
    review=new GameObject("TemporaryGirlPerformanceCamera").AddComponent<Camera>();review.enabled=false;review.fieldOfView=38;review.nearClipPlane=.05f;review.farClipPlane=150;review.cullingMask&=~(1<<FirstPersonHands.HandLayer);
    contactCamera=new GameObject("TemporaryMeadowContactCamera").AddComponent<Camera>();contactCamera.enabled=false;contactCamera.fieldOfView=40;contactCamera.nearClipPlane=.03f;
    portrait=new GameObject("TemporaryPrivacyPortrait").AddComponent<Camera>();portrait.enabled=false;portrait.fieldOfView=30;portrait.nearClipPlane=.03f;Directory.CreateDirectory(Out+"/Frames/Portrait");Directory.CreateDirectory(Out+"/Frames/Contact");Directory.CreateDirectory(Out+"/Frames/Observer");Directory.CreateDirectory(Out+"/Frames/Player");
   }
   if(pendingShot!=null)
   {
    if(p.ExposedFrames<pendingCount){if(now-pendingAt>15)throw new InvalidOperationException("Photo save timed out.");return;}
    var saved=p.Records[p.ExposedFrames-1];File.Copy(Path.Combine(ParkFilmStore.Root,film,saved.negativeFile),Out+"/"+pendingShot+"-exposure.png",true);
    Event(pendingShot+"_saved");if(pendingShot=="entrance")shutterAt=now;
    pendingShot=null;readyAt=-1;Call(p,"SetPhotographyMode",ParkPhotoPrototype.PhotographyMode.Exploration);return;
   }
   if(joinAt>0&&now-joinAt>4f){CheckCompletion(p,now);return;}
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
     Position(p,checkpoint.position-Vector3.up*PlayerControlSettings.Current.eyeHeight,Quaternion.Euler(checkpoint.euler));Set(p,"virtualPointer",new Vector2(.5f,.5f));Set(p,"apertureIndex",3);
     // Meter the current scene instead of assuming the historical lighting EV.
     int bestSpeed=0;float bestExposure=float.MaxValue;
     int speedCount=((float[])typeof(ParkPhotoPrototype).GetField("speeds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Length;
     for(int i=0;i<speedCount;i++)
     {Set(p,"speedIndex",i);float error=Mathf.Abs(p.ExposureError);if(error<bestExposure){bestExposure=error;bestSpeed=i;}}
     Set(p,"speedIndex",bestSpeed);
     if(task=="entrance"){Set(p,"apertureIndex",0);Set(p,"speedIndex",0);}
     if(task=="bridge"){Set(p,"apertureIndex",5);Set(p,"speedIndex",speedCount-1);}
     if(task!="meadow")p.focusDistance=p.maximumFocus;
     if(task=="bridge"&&c.photoLookBack&&!looked){looked=true;Event("bridge_turn_start");}
     if(task=="meadow"&&c.photoOfferHand&&!offered){offered=true;Event("meadow_offer_start");}
     bool ready=(bool)Get(p,"bridgeReady");if(ready&&readyAt<0){readyAt=now;Event(task+"_ready");}
     if(ready&&readyAt>0&&task!="meadow"&&now-readyAt>1.2f)
     {
      if(task=="bridge")CaptureOrbit(p,"Bridge");pendingShot=task;pendingCount=p.ExposedFrames+1;pendingAt=now;Event(task+"_shutter");Call(p,"Capture");return;
     }
     if(ready&&task=="meadow"&&readyAt>0&&now-readyAt>2&&joinAt<0)
     {
      var screen=p.view.WorldToViewportPoint(p.PhotoHandPosition);if(screen.z<=0||screen.x<0||screen.x>1||screen.y<0||screen.y>1)throw new InvalidOperationException("Hand target outside view.");
      CaptureOrbit(p,"OfferHold");Set(p,"virtualPointer",new Vector2(screen.x,screen.y));int before=p.ExposedFrames;Call(p,"Capture");if(!p.HoldingPhotoHand||p.ExposedFrames!=before)throw new InvalidOperationException("Normal offered-hand click failed or used film.");joinAt=now;Event("hand_joined");
     }
    }
   }
   if(interesting&&elapsed-lastFrame>=.10f)
   {
    lastFrame=elapsed;string id=imageIndex.ToString("D4");Capture(review,Out+"/Frames/Observer/frame_"+id+".png",720,720);Capture(p.view,Out+"/Frames/Player/frame_"+id+".png",960,600);
    var visual=c.visualPresentation;float error=c.photoOfferHand?Vector3.Distance(visual.PalmPosition,p.PhotoHandPosition):-1;
    var hands=p.view.GetComponent<FirstPersonHands>();
    var palm=hands!=null?hands.PhotoPalmPosition:Vector3.zero;
    if(task=="meadow")
    {
     var target=p.PhotoHandPosition;
     contactCamera.transform.position=target+c.transform.forward*.7f+c.transform.right*.4f+Vector3.up*.35f;contactCamera.transform.LookAt(target);
     Capture(contactCamera,Out+"/Frames/Contact/frame_"+id+".png",720,600);
    }
    var targetHead=visual.headBone.position+Vector3.up*.07f;
    var viewDirection=p.view.transform.position-targetHead;portrait.transform.position=targetHead+viewDirection.normalized*1.05f;portrait.transform.LookAt(targetHead);Capture(portrait,Out+"/Frames/Portrait/frame_"+id+".png",480,480);
    var state=c.animator.GetCurrentAnimatorStateInfo(0);
    float playerError=p.HoldingPhotoHand?Vector3.Distance(palm,visual.PalmPosition+visual.PalmNormal*.024f):-1;
    frames.Add(new{index=imageIndex,time=elapsed,game_time=Time.time,task=afterEntrance?"entrance":task,look_back=c.photoLookBack,offer_hand=c.photoOfferHand,joined=p.HoldingPhotoHand,hand_error_m=error,player_palm_error_m=playerError,offer_elapsed=visual.OfferElapsed,offer_weight=visual.OfferWeight,reach_state=state.IsName("OfferHandReach"),hold_state=state.IsName("OfferHandHold"),in_transition=c.animator.IsInTransition(0),privacy_pitch=visual.PrivacyPitch,privacy_yaw=visual.PrivacyYaw,girl_palm=visual.PalmPosition.ToString("F5"),player_palm=palm.ToString("F5"),shoulder_yaw=visual.ShoulderYaw,hair_lag=visual.HairLag});imageIndex++;
   }
   Write("progress.json",new{elapsed,task,at_node=c.AtPhotoNode,frames=p.ExposedFrames,ready=(bool)Get(p,"bridgeReady"),joined=p.HoldingPhotoHand,images=imageIndex});
  }
  static void CheckCompletion(ParkPhotoPrototype p,float now)
  {
   var hands=p.view.GetComponent<FirstPersonHands>();var v=p.companion.visualPresentation;
   if(checksStage==0)
   {
    var wrist=hands.skeletalRig.wrists[0];var wristBefore=wrist.position;var girlBefore=v.PalmPosition;var viewBefore=p.view.transform.position;
    var offset=p.view.GetComponent<ParkHandResponseView>().RenderPosition-viewBefore;
    if(Mathf.Abs(offset.magnitude-.1f)>.001f)errors.Add("Approach did not settle at 0.10m.");
    Capture(p.view,Out+"/joined.png",960,600);
    if(Vector3.Distance(wristBefore,wrist.position)>.00001f||Vector3.Distance(girlBefore,v.PalmPosition)>.00001f||Vector3.Distance(viewBefore,p.view.transform.position)>.00001f)errors.Add("Rendering changed gameplay transforms.");
    foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Exploration,ParkPhotoPrototype.PhotographyMode.Body,ParkPhotoPrototype.PhotographyMode.Meter})
    {Call(p,"SetPhotographyMode",mode);if(p.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Viewfinder)errors.Add("Locked camera entered "+mode);}
    foreach(var mode in new[]{ParkPhotoPrototype.PhotographyMode.Aperture,ParkPhotoPrototype.PhotographyMode.Shutter})
    {Call(p,"SetPhotographyMode",mode);hands.PosePhotoHandForShutter();if(!p.HoldingPhotoHand || hands.PhotoHandReachProgress!=1 || Vector3.Distance(wristBefore,wrist.position)>.001f)errors.Add("Dial changed joined pose: "+mode);Call(p,"StepInstrumentControl",1);Call(p,"LeaveParkContext");}
    Call(p,"LeaveParkContext");if(p.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Viewfinder)errors.Add("Back left locked finder.");
    Write("contact.json",new{offset_m=offset.magnitude,hand_progress=hands.PhotoHandReachProgress,locked=p.HandResponseLocked,mode=p.PhotoMode.ToString()});
    pausedPalm=v.PalmPosition;pausedOffer=v.OfferElapsed;GamePauseMenu.Open();checksAt=now;checksStage=1;return;
   }
   if(checksStage==1&&now-checksAt>.6f)
   {
    if(Vector3.Distance(pausedPalm,v.PalmPosition)>.001f || Mathf.Abs(v.OfferElapsed-pausedOffer)>.001f)errors.Add("Pause changed offer pose.");
    Call(p,"OnApplicationFocus",true);UnityEngine.Object.FindObjectOfType<GamePauseMenu>().Resume();checksAt=now;checksStage=2;return;
   }
   if(checksStage==2&&now-checksAt>1 && (float)Get(p,"handJoinTransition")<=0 && (bool)Get(p,"bridgeReady"))
   {
    bool failed=false;
    Set(p,"exposurePersistence",new ParkExposurePersistence(ParkFilmStore.Load,(current,record,bytes)=>{if(!failed){failed=true;throw new IOException("Injected final-frame save failure");}return ParkFilmStore.SaveExposure(current,record,bytes);}));
    Set(p,"apertureIndex",5);Set(p,"speedIndex",8);Call(p,"Capture");checksAt=now;checksStage=4;Event("final_shutter");
   }
  }
  static void CheckDarkroom(float now)
  {
   if(now-checksAt>100){errors.Add("Automatic darkroom transition timed out.");Finish(false,"Transition timeout");return;}
   var p=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
   if(p!=null)
   {
    if(((string)Get(p,"filmError")).StartsWith("Save failed"))
    {
     bool kept=p.ExposedFrames==2 && p.HandResponseLocked && p.PhotoMode==ParkPhotoPrototype.PhotographyMode.Viewfinder && !(bool)Get(p,"changingScene");
     Write("save-failure.json",new{retained=kept,frames=p.ExposedFrames});if(!kept)errors.Add("Save failure left locked contact or charged film.");
     Call(p,"Capture");Event("retry_save");return;
    }
    if(p.ExposedFrames==3 && !File.Exists(Out+"/Reports/committed.json"))
    {
     Write("committed.json",new{frames=p.ExposedFrames,records=p.Records.Select(r=>new{r.taskId,r.aperture,r.shutterSeconds,r.exposureError,r.accepted,r.joinedHands,r.sharp,r.framed}).ToArray(),state=p.FilmState.ToString(),locked=p.HandResponseLocked});
     if(!p.HoldingPhotoHand)errors.Add("Contact released before transition.");
     Capture(p.view,Out+"/committed-contact.png",960,600);
    }
    return;
   }
   var house=WBMain.Instance;
   if(house==null || !house.ScenePresentationReady || BrushSceneTransition.IsBusy)return;
   var filmRoll=ParkFilmStore.Load(film);
   if(!house.DirectDarkroomActive)errors.Add("House did not enter direct darkroom.");
   if(filmRoll.frames.Count!=3 || filmRoll.frames.Any(r=>!r.accepted) || !filmRoll.frames[2].joinedHands)errors.Add("Committed film is incomplete.");
   Write("darkroom.json",new{ready=house.ScenePresentationReady,direct_darkroom=house.DirectDarkroomActive,frames=filmRoll.frames.Count,records=filmRoll.frames.Select(r=>new{r.taskId,r.aperture,r.shutterSeconds,r.exposureError,r.accepted,r.joinedHands,r.sharp,r.framed}).ToArray()});
   var camera=house.Room.Camera;if(camera!=null)Capture(camera,Out+"/darkroom.png",960,600);
   Finish(errors.Count==0,"Unrestricted exposures, locked contact, pause, and automatic darkroom validated");
  }
  static void CaptureOrbit(ParkPhotoPrototype p,string state)
  {
   Directory.CreateDirectory(Out+"/Preview/Orbit/"+state);var c=p.companion;var target=c.visualPresentation.headBone.position+Vector3.up*.07f;
   var direction=p.view.transform.position-target;direction.y=0;direction.Normalize();
   foreach(float yaw in new[]{-90f,-60f,-30f,0f,30f,60f,90f,180f})foreach(float elevation in new[]{0f,15f,30f})
   {
    var radial=Quaternion.Euler(0,yaw,0)*direction;
    portrait.transform.position=target+radial*1.05f+Vector3.up*Mathf.Tan(elevation*Mathf.Deg2Rad)*1.05f;portrait.transform.LookAt(target);
    Capture(portrait,Out+"/Preview/Orbit/"+state+"/yaw_"+yaw+"_elevation_"+elevation+".png",480,480);
   }
  }
  static void Capture(Camera camera,string path,int width,int height)
  {
   var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
   finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
  }
  static void Finish(bool pass,string reason)
  {
   try{Write("recording.json",new{pass,reason,seconds=Time.realtimeSinceStartup-start,normal_controller=true,native_mouse_keyboard=false,production_assets_modified=false,scope="Live story checkpoints with shortened walking. Extreme optical settings; native input simulated through existing handlers. Real asynchronous encoding/save and scene transition; existing girl animation untouched.",events=events.ToArray(),frames=frames.ToArray(),errors=errors.ToArray()});}
   finally{EditorApplication.isPlaying=false;}
  }
 }
}
