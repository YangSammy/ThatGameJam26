using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    [Serializable] public sealed class ParkPhotoRecord
    {
        public string id,filmId,capturedUtc,negativeFile,moment,feedback,taskId;
        public int frameNumber,iso;
        public float aperture,shutterSeconds,sceneEv100,exposureError;
        public Vector3 cameraPosition,cameraEuler,subjectPosition;
        public Vector3 subjectEuler,headEuler,handContact;
        public bool joinedHands;
        public float focusDistance,subjectDepth,fieldOfView,subjectScreenHeight;
        public bool sharp,visible,framed,accepted;
    }
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class ParkPhotoPrototype : MonoBehaviour
    {
        [Header("场景引用")]
        public Camera view;
        public ChapterSceneBinding DataBinding=>GetComponent<ChapterSceneBinding>();
        public Transform spawn;
        public ParkCompanion companion;
        public ParkPhotoFocus focusEffect;
        public AudioSource sound;
        public AudioClip shutter,callSound;
        // Serialized legacy fields retained for compatibility; shared tuning now
        // comes from Resources/PlayerControlSettings.asset in both scenes.
        [HideInInspector]
        [Min(.1f)] public float walkSpeed=2.7f;
        [HideInInspector,Min(.1f)] public float aimWalkSpeed=1.1f;
        [HideInInspector,Min(.1f)] public float lookSensitivity=1.7f;
        [Header("取景与手动调焦")]
        [HideInInspector,Range(30,90)] public float walkingFov=65;
        [HideInInspector,Range(25,70)] public float viewfinderFov=48;
        [Min(.1f)] public float minimumFocus=1.2f,maximumFocus=18,focusStep=.25f;
        [Tooltip("沿用上一张的焦距，玩家需要根据人物距离亲手重新调焦。") ] public float focusDistance=4;
        [Header("胶卷记录：新卷固定 3 张，曝光后自动保存")]
        [Range(640,1920)] public int photoWidth=960;
        public event Action<ParkPhotoRecord> PhotoTaken;
        public event Action<string> FilmFilled;
        public string CurrentFilmId=>roll!=null?roll.id:"";
        public int ExposedFrames=>roll!=null?roll.frames.Count:0;
        public int RollCapacity=>roll!=null?roll.Capacity:ParkFilmRoll.FrameCapacity;
        public int RemainingFrames=>roll!=null?roll.Remaining:0;
        public bool CanExpose=>roll!=null && filmError=="" && roll.ReadyForPark && roll.Remaining>0;
        public bool CanReturnToDarkroom=>roll!=null && filmError=="" && roll.Remaining==0 && roll.state!=ParkFilmState.Loaded;
        public ParkFilmState FilmState=>roll!=null?roll.state:ParkFilmState.Loaded;
        public IReadOnlyList<ParkPhotoRecord> Records=>roll!=null?(IReadOnlyList<ParkPhotoRecord>)roll.frames.AsReadOnly():Array.Empty<ParkPhotoRecord>();
        public bool Aiming {get;private set;}
        public bool Paused {get;private set;}
        public float MovementSpeed=>motor!=null?new Vector2(motor.velocity.x,motor.velocity.z).magnitude:0;
        public float ShutterPulse=>flash;
        ParkFilmRoll roll;
        CharacterController motor;Font font;
        float yaw,pitch,verticalSpeed,shotCooldown,flash,noticeUntil;
        string notice="",filmError="";
        bool inputGate=true,focused=true;
        bool returnUiGate,changingScene;
        readonly PlayerInputReleaseGate handoff=new PlayerInputReleaseGate();
        Vector3 moveVelocity;
        float aimBlend;
        public PlayerControlState ControlState=>!focused?PlayerControlState.Unfocused:changingScene?
            PlayerControlState.Transition:Paused?PlayerControlState.Paused:Aiming?PlayerControlState.Operation:PlayerControlState.Exploration;
        void Start()
        {
            if(DataBinding!=null && DataBinding.ChapterId!=ChapterIds.Photography){Debug.LogError("Park chapter binding must reference photography.",this);enabled=false;return;}
            motor=GetComponent<CharacterController>();
            if(view==null || spawn==null || companion==null || focusEffect==null){Debug.LogError("Park photo prototype is missing scene references.",this);enabled=false;return;}
            var story=FiveDayProgress.Load();
            if(!story.CanEnter(1))
            {
                companion.Paused=true;enabled=false;
                if(Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))
                    UnityEngine.SceneManagement.SceneManager.LoadScene(ParkFilmRoute.HouseScenePath);
                else Debug.LogError("Photography is locked; HouseScene must be available.",this);
                return;
            }
            motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);motor.enabled=true;
            var controls=PlayerControlSettings.Current;
            yaw=spawn.eulerAngles.y;pitch=controls.ClampPitch(Mathf.DeltaAngle(0,spawn.eulerAngles.x));
            transform.rotation=Quaternion.Euler(0,yaw,0);
            view.transform.localPosition=new Vector3(0,controls.eyeHeight,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            view.fieldOfView=controls.parkWalkingFov;handoff.Arm();
            try{roll=ParkFilmStore.LoadOrCreate();if(roll.frames.Count>0)focusDistance=Mathf.Clamp(roll.frames[roll.frames.Count-1].focusDistance,minimumFocus,maximumFocus);}
            catch(Exception ex){filmError="Film unavailable. Shooting paused; records kept.";Debug.LogWarning("Park film load: "+ex.Message);}
            InitializePhotographyModes();InitializeBridge();
            SetCursor(false);Notify(roll!=null && roll.state!=ParkFilmState.Loaded?"Film sealed. Develop in the darkroom.": "Film: " + RollCapacity + " frames. RMB to aim; scroll to focus.",7);
            if(CanReturnToDarkroom)OpenFilmReturnPanel();
        }
        void SetCursor(bool free){PlayerInputPolicy.SetCursor(free);}
        void Notify(string text,float seconds=4){notice=text;noticeUntil=Time.unscaledTime+seconds;}
        public void Resume(){SetPhotographyMode(PhotographyMode.Exploration);Paused=false;inputGate=true;moveVelocity=Vector3.zero;handoff.Arm();SyncParkInput();InputContext.Handoff();}
        void PauseControls()
        {
            SavePhotoSettings();
            SetPhotographyMode(PhotographyMode.Exploration);Paused=true;Aiming=false;moveVelocity=Vector3.zero;verticalSpeed=0;handoff.Arm();inputGate=true;
            if(companion!=null)companion.Paused=true;
            if(focusEffect!=null)focusEffect.activeFocus=false;SetCursor(true);
            SyncParkInput();InputContext.Handoff();
        }
        void OpenFilmReturnPanel()
        {
            PauseControls();returnUiGate=true;inputGate=true;
        }
        public void ReturnToDarkroom()
        {
            if(!CanReturnToDarkroom || changingScene || returnUiGate || !InputContext.Allows(InputContextId.Paused))return;
            try
            {
                // Reload the committed roll before leaving; never pass a transient capture.
                roll=ParkFilmStore.Load(roll.id);
                if(!CanReturnToDarkroom)return;
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))throw new InvalidOperationException("HouseScene is not in Build Settings.");
                changingScene=true;
                PauseControls();
                ParkFilmRoute.EnterDarkroomOnLoad=true;
                UnityEngine.SceneManagement.SceneManager.LoadScene(ParkFilmRoute.HouseScenePath);
            }
            catch(Exception ex)
            {
                ParkFilmRoute.EnterDarkroomOnLoad=false;
                changingScene=false;
                Notify("Return unavailable. Film saved.",6);Debug.LogWarning("Park return: "+ex.Message);
            }
        }
        void OnApplicationFocus(bool value)
        {focused=value;if(!value)PauseControls();else {handoff.Arm();InputContext.Handoff();}SyncParkInput();}
        void OnApplicationPause(bool value)
        {applicationPaused=value;if(value)PauseControls();else {inputGate=true;handoff.Arm();InputContext.Handoff();}SyncParkInput();}
        void Update()
        {
            SyncParkInput();
            if(!focused || applicationPaused || changingScene)return;
            // The final shutter press must be released before it can activate UI.
            if(returnUiGate && handoff.Ready)returnUiGate=false;
            var controls=PlayerControlSettings.Current;
            float dt=Mathf.Min(Time.deltaTime,.1f);shotCooldown=Mathf.Max(0,shotCooldown-dt);flash=Mathf.Max(0,flash-dt*4);
            if(ExitParkContext())return;
            companion.Paused=Paused || (photoMode!=PhotographyMode.Exploration && photoMode!=PhotographyMode.Viewfinder);
            focusEffect.activeFocus=Aiming && !Paused;focusEffect.distance=focusDistance;
            if(Paused)return;
            if(!InputContext.Ready){moveVelocity=Vector3.zero;return;}
            if(inputGate){if(handoff.Ready)inputGate=false;return;}
            if(TickPhotographyModes(dt))return;
            if(Input.GetKeyDown(KeyCode.R))
            {companion.RestartWalk();motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);motor.enabled=true;yaw=spawn.eulerAngles.y;pitch=controls.ClampPitch(Mathf.DeltaAngle(0,spawn.eulerAngles.x));verticalSpeed=0;moveVelocity=Vector3.zero;Aiming=false;inputGate=true;handoff.Arm();Notify("Walk restarted. Film kept.");return;}
            if(Input.GetKeyDown(KeyCode.E) && !BridgePending)
            {if(companion.Call()){if(sound!=null && callSound!=null)sound.PlayOneShot(callSound,.3f);}else Notify("Move closer. Wait for a response.");}
            aimBlend=Mathf.MoveTowards(aimBlend,Aiming?1:0,dt/Mathf.Max(.01f,controls.aimTransitionSeconds));
            var look=edgeCandidate>=0?Vector2.zero:PlayerInputPolicy.LookDelta;float sensitivity=controls.lookSensitivity*Mathf.Lerp(1,controls.aimingLookMultiplier,aimBlend);
            yaw+=look.x*sensitivity;pitch=controls.ClampPitch(pitch-look.y*sensitivity);
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var move=Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)),1);
            float speed=controls.walkSpeed*Mathf.Lerp(controls.parkSpeedMultiplier,controls.aimingSpeedMultiplier,aimBlend);
            moveVelocity=controls.ApproachVelocity(moveVelocity,(transform.right*move.x+transform.forward*move.y)*speed,dt);
            // Bounded moves keep gravity/ground contact stable over bridge seams
            // and on slower frames. Ground geometry remains authored in ParkScene.
            float moveRemaining=dt;
            while(moveRemaining>0)
            {
                float step=Mathf.Min(.02f,moveRemaining);moveRemaining-=step;
                if(motor.isGrounded && verticalSpeed<0)verticalSpeed=-2;
                verticalSpeed-=18*step;
                var collisions=motor.Move((moveVelocity+Vector3.up*verticalSpeed)*step);
                if((collisions & CollisionFlags.Above)!=0 && verticalSpeed>0)verticalSpeed=0;
            }
            if(transform.position.y< -3){motor.enabled=false;transform.position=spawn.position;motor.enabled=true;verticalSpeed=0;Notify("Back at the path. Film kept.");}
            view.fieldOfView=Mathf.Lerp(controls.parkWalkingFov,controls.parkAimingFov,Mathf.SmoothStep(0,1,aimBlend));
            TickBridge(dt);
            if(Aiming)
            {
                focusDistance=Mathf.Clamp(focusDistance+Input.mouseScrollDelta.y*focusStep,minimumFocus,Mathf.Max(minimumFocus,maximumFocus));
                focusEffect.activeFocus=true;focusEffect.distance=focusDistance;
                TickBridge(0); // Recheck a wheel change before accepting this frame's shutter.
                if(Input.GetMouseButtonDown(0) && shotCooldown<=0)Capture();
            }
            else focusEffect.activeFocus=false;
        }
        bool Visible(Vector3 point)
        {
            RaycastHit hit;var delta=point-view.transform.position;
            if(!Physics.Raycast(view.transform.position,delta.normalized,out hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore))return true;
            return hit.transform.IsChildOf(companion.transform);
        }
        ParkPhotoRecord Assess()
        {
            var head=view.WorldToViewportPoint(companion.head.position);var hips=view.WorldToViewportPoint(companion.hips.position);
            var feet=view.WorldToViewportPoint(companion.feet.position);var chest=view.WorldToViewportPoint(companion.chest.position);
            bool inFrame=head.z>0 && hips.z>0 && head.x>.06f && head.x<.94f && head.y>.08f && head.y<.92f && hips.x>.06f && hips.x<.94f && hips.y>.07f && hips.y<.93f;
            float height=Mathf.Abs(head.y-feet.y);
            var photo=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),capturedUtc=DateTime.UtcNow.ToString("o"),cameraPosition=view.transform.position,cameraEuler=view.transform.eulerAngles,subjectPosition=companion.transform.position,
                iso=FilmIso,aperture=Aperture,shutterSeconds=ShutterSeconds,sceneEv100=sceneEv100,exposureError=ExposureError,
                focusDistance=focusDistance,subjectDepth=chest.z,fieldOfView=view.fieldOfView,subjectScreenHeight=height,
                sharp=Mathf.Abs(chest.z-focusDistance)<Mathf.Max(.2f,chest.z*.065f),visible=Visible(companion.head.position)&&Visible(companion.chest.position),framed=inFrame && height>.18f && height<.95f};
            var direction=view.transform.position-companion.transform.position;direction.y=0;float facing=Vector3.Dot(companion.transform.forward,direction.normalized);
            int moment=-1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Walking && facing<-.2f)moment=0;
            if(companion.CurrentMoment==ParkCompanion.Moment.Waterside && Mathf.Abs(facing)<.8f)moment=1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Response && facing>.65f)moment=2;
            photo.moment=moment>=0?MomentName(moment):"Candid";
            photo.accepted=photo.sharp && photo.visible && photo.framed && moment>=0;
            photo.feedback=!photo.framed?"Keep head and body in frame.":!photo.visible?"Subject blocked. Move sideways.":!photo.sharp?"Out of focus. Scroll to adjust.":moment<0?"In focus. Watch for a moment.":"Captured: "+photo.moment+".";
            return photo;
        }
        static string MomentName(int i){return i==0?"Walking together":i==1?"By the bridge":"Looking back";}
        void Capture()
        {
            if(roll==null || filmError!=""){Notify(filmError!=""?filmError:"No film loaded.");return;}
            if(!roll.ReadyForPark || roll.Remaining<=0){Notify("Roll complete. Develop to view.",5);return;}
            if(roll.version>=3 && !taskRoutingReady)return;
            if(TryJoinPhotoHand())return;
            if(BridgePending && CurrentTaskId=="meadow" && !HoldingPhotoHand)return;
            if(BridgePending && !bridgeReady){shotCooldown=.25f;return;}
            shotCooldown=.7f;var record=Assess();
            if(BridgePending)
            {
                record.taskId=CurrentTaskId;record.moment=CurrentTaskId=="entrance"?"Entrance candid":CurrentTaskId=="bridge"?"Bridge look-back":"Meadow joined hands";record.accepted=true;
                if(CurrentTaskId=="entrance")companion.PoseEntranceShutter();
                if(HoldingPhotoHand)view.GetComponent<FirstPersonHands>()?.PosePhotoHandForShutter();
            }
            record.subjectEuler=companion.transform.eulerAngles;
            record.headEuler=companion.head!=null?companion.head.eulerAngles:Vector3.zero;
            record.joinedHands=HoldingPhotoHand;
            if(record.joinedHands)record.handContact=PhotoHandPosition;
            Texture2D image=null;
            var previousTarget=view.targetTexture;var previousActive=RenderTexture.active;int previousMask=view.cullingMask;
            RenderTexture target=null;
            try
            {
                target=RenderTexture.GetTemporary(photoWidth,Mathf.Max(1,Mathf.RoundToInt(photoWidth/view.aspect)),24,RenderTextureFormat.ARGB32);
                if(!HoldingPhotoHand)view.cullingMask&=~(1<<FirstPersonHands.HandLayer);
                else view.cullingMask|=1<<FirstPersonHands.HandLayer;
                view.targetTexture=target;view.Render();RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            }
            catch(Exception ex){if(image!=null)Destroy(image);Notify("Shot not saved. Frame kept; try again.");Debug.LogWarning("Park capture: "+ex.Message);return;}
            finally{view.targetTexture=previousTarget;view.cullingMask=previousMask;RenderTexture.active=previousActive;if(target!=null)RenderTexture.ReleaseTemporary(target);}
            try
            {
                roll=ParkFilmStore.SaveExposure(roll,record,image.EncodeToPNG());
            }
            catch(Exception ex){filmError="Save failed. Shooting paused; records kept.";Notify(filmError,7);Debug.LogWarning("Park film save: "+ex.Message);return;}
            finally{if(image!=null)Destroy(image);}
            InitializeBridge();
            flash=1;Notify(roll.Remaining==0?"Frame " + RollCapacity + " exposed. Roll complete.":"Frame "+record.frameNumber+" exposed  /  Remaining "+roll.Remaining+" frames",4);
            if(sound!=null && shutter!=null)sound.PlayOneShot(shutter,.6f);
            if(roll.Remaining==0)OpenFilmReturnPanel();
            PhotoTaken?.Invoke(record);
            if(roll.Remaining==0)FilmFilled?.Invoke(roll.id);
        }
        void OnDisable(){SavePhotoSettings();CancelInstrumentDrag();moveVelocity=Vector3.zero;InputContext.Select(InputContextId.Unfocused,PlayerControlState.Unfocused);InputContext.Handoff();if(companion!=null)companion.Paused=true;if(focusEffect!=null)focusEffect.activeFocus=false;}
        void OnDestroy(){DisposePhotographyModes();if(bridgeReference!=null)Destroy(bridgeReference);if(font!=null)Destroy(font);}
        void OnGUI()
        {
            if(view==null || companion==null)return;
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            var text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};var small=new GUIStyle(text){fontSize=15};var button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
            if(Paused)
            {

                InteractionUI.Label(new Rect(Screen.width*.5f-150,Screen.height*.5f-50,300,50),CanReturnToDarkroom?"Roll complete  " + RollCapacity + " / " + RollCapacity + " frames\nDevelop to view.":"Paused",text);
                bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled && !returnUiGate && !changingScene && InputContext.Allows(InputContextId.Paused);
                if(InteractionUI.Button(new Rect(Screen.width*.5f-100,Screen.height*.5f+10,200,40),"Resume  /  Esc",button))InputContext.RequestExit(InputContextId.Paused,null,Resume);
                if(CanReturnToDarkroom && InteractionUI.Button(new Rect(Screen.width*.5f-145,Screen.height*.5f+66,290,40),"Return to darkroom",button))ReturnToDarkroom();
                GUI.enabled=previousEnabled;
                if(Time.unscaledTime<noticeUntil)InteractionUI.Label(new Rect(24,Screen.height-75,Screen.width-48,50),notice,text);
                return;
            }

            DrawPhotographyModes();DrawBridgeReference();
            if(flash>0){var color=GUI.color;GUI.color=new Color(1,1,1,flash*.55f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;}
        }
    }
}
