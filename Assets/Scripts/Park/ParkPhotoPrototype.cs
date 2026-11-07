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
        [Tooltip("沿用上一张的对焦距离，玩家需要根据人物距离亲手重新调焦。") ] public float focusDistance=4;
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
        public enum PauseReason { None, FilmComplete=2 }
        public PauseReason CurrentPauseReason {get;private set;}
        public bool Paused=>CurrentPauseReason!=PauseReason.None;
        public float MovementSpeed=>motor!=null?new Vector2(motor.velocity.x,motor.velocity.z).magnitude:0;
        public float ShutterPulse=>flash;
        ParkFilmRoll roll;
        CharacterController motor;Font font;
        float yaw,pitch,verticalSpeed,shotCooldown,flash,noticeUntil;
        string notice="",filmError="";
        bool inputGate=true,focused=true;
        bool returnUiGate,changingScene;
        public bool ScenePresentationReady {get;private set;}
        readonly PlayerInputReleaseGate handoff=new PlayerInputReleaseGate(ignoreLocomotion:true);
        Vector3 moveVelocity;
        PlayerFootsteps footsteps;
        float aimBlend;
        public PlayerControlState ControlState=>!focused?PlayerControlState.Unfocused:changingScene?
            PlayerControlState.Transition:Paused?PlayerControlState.Paused:Aiming?PlayerControlState.Operation:PlayerControlState.Exploration;
        void Start()
        {
            if(DataBinding!=null && DataBinding.ChapterId!=ChapterIds.Photography){Debug.LogError("Park chapter binding must reference photography.",this);enabled=false;return;}
            motor=GetComponent<CharacterController>();
            if(view==null || spawn==null || companion==null || focusEffect==null){Debug.LogError("Park photo prototype is missing scene references.",this);enabled=false;return;}
            var story=FiveDayProgress.LoadForSession();
            if(!story.CanEnter(1))
            {
                companion.Paused=true;enabled=false;
                if(Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))
                    BrushSceneTransition.RedirectTo(ParkFilmRoute.HouseScenePath);
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
            InitializePhotographyModes();InitializeBridge();ResumeUnfinishedMemory();InitializeGuidedWalk();InitializePhotoFeedback();UpdateRegionalMetering();
            InitializeHandResponseView();
            SetCursor(false);Notify(roll!=null && roll.state!=ParkFilmState.Loaded?"Film sealed. Develop in the darkroom.":OpeningPhotoInstruction(),7);
            if(CanReturnToDarkroom){if(roll.frames[roll.frames.Count-1].joinedHands)QueueHandPhotoReturn();else OpenFilmReturnPanel();}
            ScenePresentationReady=true;
        }
        void SetCursor(bool free){PlayerInputPolicy.SetCursor(free);}
        void Notify(string text,float seconds=4){notice=text;noticeUntil=Time.unscaledTime+seconds;}
        public void Resume(){SetPhotographyMode(PhotographyMode.Exploration);CurrentPauseReason=PauseReason.None;ReleaseGuidedDeparture();inputGate=true;moveVelocity=Vector3.zero;handoff.Arm();SyncParkInput();InputContext.Handoff();}
        // Only completed rolls use this handoff; Esc belongs to GamePauseMenu.
        void OpenFilmReturnPanel()
        {
            if(!CanReturnToDarkroom)return;
            footsteps.Reset();
            automaticFilmPanelPause=true;
            try{PausePhotographySfx();}
            finally{automaticFilmPanelPause=false;}
            SavePhotoSettings();CurrentPauseReason=PauseReason.FilmComplete;
            SetPhotographyMode(PhotographyMode.Exploration);Aiming=false;
            moveVelocity=Vector3.zero;verticalSpeed=0;handoff.Arm();inputGate=true;
            if(companion!=null)companion.Paused=true;
            if(focusEffect!=null)focusEffect.activeFocus=false;
            SetCursor(true);SyncParkInput();InputContext.Handoff();returnUiGate=true;
        }
        public void ReturnToDarkroom()
        {
            if(!CanReturnToDarkroom || changingScene || HasPendingExposure || (!finishingHandPhoto && (returnUiGate || !InputContext.Allows(InputContextId.Paused))))return;
            try
            {
                // Reload the committed roll before leaving; never pass a transient capture.
                roll=ParkFilmStore.Load(roll.id);
                if(!CanReturnToDarkroom)return;
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))throw new InvalidOperationException("HouseScene is not in Build Settings.");
                changingScene=true;
                if(finishingHandPhoto && companion!=null)companion.Paused=true;
                PreparePauseInput();
                ParkFilmRoute.EnterDarkroomOnLoad=true;
                BrushSceneTransition.LoadScene(ParkFilmRoute.HouseScenePath);
            }
            catch(Exception ex)
            {
                ParkFilmRoute.EnterDarkroomOnLoad=false;
                changingScene=false;
                Notify("Return unavailable. Film saved.",6);Debug.LogWarning("Park return: "+ex.Message);
            }
        }
        void OnApplicationFocus(bool value)
        {focused=value;RefreshApplicationSuspension();}
        void OnApplicationPause(bool value)
        {applicationPaused=value;RefreshApplicationSuspension();}
        void RefreshApplicationSuspension()
        {
            ResetCameraEdgeGesture();
            moveVelocity=Vector3.zero;footsteps.Reset();
            if(!focused || applicationPaused)
            {
                StopPhotographySfx();CancelInstrumentDrag();CancelCameraRaise();SavePhotoSettings();
                if(companion!=null){companion.Paused=true;companion.TravelPaused=true;}
            }
            inputGate=true;handoff.Arm();InputContext.Handoff();SyncParkInput();
        }
        void Update()
        {
            // Receiving finished encoding is save IO, not gameplay time. A pause
            // must not strand a completed image behind its own Save/Exit guard.
            TickExposureEncoding();
            if(GamePauseMenu.PollEscape())return;
            if(shutterSampling)return;
            TickPhotographySfx(Mathf.Min(Time.unscaledDeltaTime,.1f));
            SyncParkInput();
            if(!focused || applicationPaused || changingScene)return;
            if(finishingHandPhoto){TickHandPhotoReturn();return;}
            int shortcut=StageCompletionShortcuts.Pressed();
            if(shortcut!=0 && !HandResponseLocked){CompleteStageShortcut(shortcut);return;}
            // The final shutter press must be released before it can activate UI.
            if(returnUiGate && handoff.Ready)returnUiGate=false;
            var controls=PlayerControlSettings.Current;
            float dt=Mathf.Min(Time.deltaTime,.1f);shotCooldown=Mathf.Max(0,shotCooldown-dt);flash=Mathf.Max(0,flash-dt*4);
            if(ExitParkContext())return;
            UpdateRegionalMetering();
            TickLoweredDeparture();
            SyncCompanionControl();
            SyncCameraPreview();
            if(Paused)return;
            if(TickGuidedRecovery(dt)){footsteps.Reset();return;}
            if(TickCameraRaise(dt))return;
            // Returning the pointer is navigation, not a new gameplay action. Allow it
            // before release gates; then stop this frame before wheel/shutter handling.
            if(ReturnCameraDetailAtCentre(new Vector2(Input.mousePosition.x/Mathf.Max(1,Screen.width),Input.mousePosition.y/Mathf.Max(1,Screen.height))))return;
            if(!InputContext.Ready){moveVelocity=Vector3.zero;return;}
            if(inputGate){if(handoff.Ready)inputGate=false;return;}
            bool instrumentMode=TickPhotographyModes(dt);SyncCameraPreview();
            if(instrumentMode)return;
            aimBlend=Mathf.MoveTowards(aimBlend,Aiming || CameraDetailMode?1:0,dt/Mathf.Max(.01f,controls.aimTransitionSeconds));
            var look=PreciseInstrumentPointer || HandResponseLocked?Vector2.zero:PlayerInputPolicy.LookDelta;float sensitivity=controls.lookSensitivity*Mathf.Lerp(1,controls.aimingLookMultiplier,aimBlend);
            yaw+=look.x*sensitivity;pitch=controls.ClampPitch(pitch-look.y*sensitivity);
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var move=Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)),1);
            float speed=controls.walkSpeed*Mathf.Lerp(controls.parkSpeedMultiplier,controls.aimingSpeedMultiplier,aimBlend);
            bool handLocksMovement=PhotoHandMovementLocked;
            var requestedMove=handLocksMovement?Vector3.zero:GuidedDesiredVelocity((transform.right*move.x+transform.forward*move.y)*speed,dt);
            moveVelocity=handLocksMovement?Vector3.zero:controls.ApproachVelocity(moveVelocity,requestedMove,dt);
            if(handLocksMovement){verticalSpeed=0;footsteps.Reset();}
            // Bounded moves keep gravity/ground contact stable over bridge seams
            // and on slower frames. Ground geometry remains authored in ParkScene.
            var beforeMove = transform.position;
            float moveRemaining=handLocksMovement?0:dt;
            while(moveRemaining>0)
            {
                float step=Mathf.Min(.02f,moveRemaining);moveRemaining-=step;
                if(motor.isGrounded && verticalSpeed<0)verticalSpeed=-2;
                verticalSpeed-=18*step;
                var collisions=motor.Move(ConstrainGuidedMove((moveVelocity+Vector3.up*verticalSpeed)*step));
                if((collisions & CollisionFlags.Above)!=0 && verticalSpeed>0)verticalSpeed=0;
            }
            if(transform.position.y< -3)
            {
                footsteps.Reset();
                RecoverGuidedWalk();
            }
            else if (motor.isGrounded)
                footsteps.Advance(beforeMove, transform.position, transform.position);
            else
                footsteps.Reset();
            if(!handLocksMovement)TickGuidedSafety(dt,requestedMove);
            view.fieldOfView=Mathf.Lerp(controls.parkWalkingFov,controls.parkAimingFov,Mathf.SmoothStep(0,1,aimBlend));
            TickBridge(dt);
            if(Aiming)
            {
                bool returningPointer=Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButton(2);
                if(!returningPointer)StepFinderWheel(Input.mouseScrollDelta.y);
                SyncCameraPreview();
                TickBridge(0); // Recheck a wheel change before accepting this frame's shutter.
                TickPhotoFeedback(dt);
                if(Input.GetMouseButtonDown(0) && !returningPointer && shotCooldown<=0 && FinderClickAllowed(virtualPointer,PointerOnHand))Capture();
            }
            else {SyncCameraPreview();TickPhotoFeedback(dt);}
        }
        void SyncCameraPreview()
        {
            focusEffect.activeFocus=(Aiming || CameraDetailMode) && !Paused;
            focusEffect.detailBackdrop=CameraDetailMode;
            focusEffect.showFocusAid=false;
            // Use the very same subject depth as shot acceptance. Authored tasks
            // already retain a stable reference point rather than a moving face/hand.
            focusEffect.subjectDepth=BridgePending && CurrentTask!=null && CurrentTask.hasReference?
                TaskFocusDepth(CurrentTask):companion!=null && companion.chest!=null?
                    Vector3.Dot(companion.chest.position-OpticalViewPosition,view.transform.forward):0;
            focusEffect.distance=focusDistance;focusEffect.aperture=Aperture;focusEffect.exposureError=ExposureError;
        }
        bool Visible(Vector3 point)
        {
            RaycastHit hit;var delta=point-view.transform.position;
            if(!Physics.Raycast(view.transform.position,delta.normalized,out hit,delta.magnitude,~(1<<FirstPersonHands.HandLayer),QueryTriggerInteraction.Ignore))return true;
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
            };
            if(BridgePending && CurrentTask!=null && CurrentTask.hasReference)
            {
                // Evaluate the authored composition before posing for the shutter.
                // A close-up of the offered hand need not satisfy the candid full-body rule.
                photo.taskId=CurrentTaskId;
                photo.moment=photo.taskId=="entrance"?"Entrance candid":photo.taskId=="bridge"?"Bridge look-back":"Meadow joined hands";
                photo.subjectDepth=TaskFocusDepth(CurrentTask);
                photo.framed=TaskFramed(CurrentTask);photo.visible=SubjectVisible();
                photo.sharp=ParkCameraResponse.IsSharp(photo.subjectDepth,focusDistance,Aperture);
                photo.accepted=true;
                photo.feedback="Captured: "+photo.moment+".";
                return photo;
            }
            photo.sharp=ParkCameraResponse.IsSharp(chest.z,focusDistance,Aperture);
            photo.visible=Visible(companion.head.position)&&Visible(companion.chest.position);
            photo.framed=inFrame && height>.18f && height<.95f;
            var direction=view.transform.position-companion.transform.position;direction.y=0;float facing=Vector3.Dot(companion.transform.forward,direction.normalized);
            int moment=-1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Walking && facing<-.2f)moment=0;
            if(companion.CurrentMoment==ParkCompanion.Moment.Waterside && Mathf.Abs(facing)<.8f)moment=1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Response && facing>.65f)moment=2;
            photo.moment=moment>=0?MomentName(moment):"Candid";
            photo.accepted=true;
            photo.feedback="Captured: "+photo.moment+".";
            return photo;
        }
        static string MomentName(int i){return i==0?"Walking together":i==1?"By the bridge":"Looking back";}
        void Capture()
        {
            if(finishingHandPhoto || changingScene)return;
            if(HasPendingExposure){RetryPendingExposure();return;}
            if(guidedDeparturePending){Notify(PhotographyNextAction());return;}
            if(CameraRaising)return;
            if(handJoinTransition>0)return;
            if(roll==null || filmError!=""){Notify(filmError!=""?filmError:"No film loaded.");return;}
            if(roll.Remaining<=0){Notify("Roll complete. Develop to view.",5);return;}
            if(!roll.ReadyForPark){Notify(LoadingNextAction(),5);return;}
            if(roll.version>=3 && !taskRoutingReady){Notify(PhotographyNextAction());return;}
            if(TryJoinPhotoHand())return;
            if(BridgePending && CurrentTaskId=="meadow" && !HoldingPhotoHand){Notify(PhotographyNextAction());return;}
            if(BridgePending && !bridgeReady){Notify(PhotographyNextAction());shotCooldown=.25f;return;}
            SyncCameraPreview();shotCooldown=.7f;var record=Assess();
            if(handResponseView!=null)record.cameraPosition=handResponseView.RenderPosition;
            if(BridgePending)
            {
                companion.PosePhotoResponseForShutter();
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
                view.targetTexture=target;RenderPhotographicFrame();RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            }
            catch(Exception ex){if(image!=null)Destroy(image);Notify("Shot not saved. Frame kept; try again.");Debug.LogWarning("Park capture: "+ex.Message);return;}
            finally{view.targetTexture=previousTarget;view.cullingMask=previousMask;RenderTexture.active=previousActive;if(target!=null)RenderTexture.ReleaseTemporary(target);}
            PlayOpticalShutterSfx();
            if(record.taskId=="bridge" && record.shutterSeconds>=1f/60)
            {BeginBridgeExposure(record,image);return;}
            QueueFinishedExposure(record,image);Destroy(image);
        }
        void ExposureCommitted(ParkPhotoRecord record)
        {
            if(record.joinedHands && roll.Remaining==0)
            {
                QueueHandPhotoReturn();
                PhotoTaken?.Invoke(record);FilmFilled?.Invoke(roll.id);return;
            }
            if(roll.Remaining>0)HoldGuidedDeparture();else InitializeBridge();
            flash=1;Notify(roll.Remaining==0?"Frame " + RollCapacity + " exposed. Roll complete.":"Frame "+record.frameNumber+" exposed  /  Remaining "+roll.Remaining+" frames",4);
            if(roll.Remaining==0)OpenFilmReturnPanel();
            PhotoTaken?.Invoke(record);
            if(roll.Remaining==0)FilmFilled?.Invoke(roll.id);
        }
        void OnDisable(){CancelShutterSampling();StopPhotographySfx();CancelCameraRaise();SavePhotoSettings();CancelInstrumentDrag();moveVelocity=Vector3.zero;InputContext.Select(InputContextId.Unfocused,PlayerControlState.Unfocused);InputContext.Handoff();if(companion!=null)companion.Paused=true;if(focusEffect!=null)focusEffect.activeFocus=false;}
        void OnDestroy(){StopPhotographySfx();CancelExposureEncoding();DisposePhotographyModes();DisposePhotoFeedback();if(bridgeReference!=null)Destroy(bridgeReference);if(font!=null)Destroy(font);}
        void OnGUI()
        {
            if(GamePauseMenu.BlocksGameplay)return;
            if(finishingHandPhoto)
            {
                if(!changingScene && !handPhotoReturnPending && GUI.Button(new Rect(Screen.width*.5f-145,Screen.height*.5f,290,40),"Retry return to darkroom"))ReturnToDarkroom();
                return;
            }
            if(view==null || companion==null)return;
            CaptureBodyPointer(Event.current);
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            var text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
            if(!focused || applicationPaused)return;
            if(CurrentPauseReason==PauseReason.FilmComplete)
            {
                bool previousEnabled=GUI.enabled;
                GUI.enabled=previousEnabled && !returnUiGate && !changingScene && InputContext.Allows(InputContextId.Paused);
                if(InteractionUI.Button(new Rect(Screen.width*.5f-145,Screen.height*.5f+20,290,40),"Return to darkroom",button))ReturnToDarkroom();
                GUI.enabled=previousEnabled;
                return;
            }

            DrawPhotographyModes();DrawBridgeReference();DrawPhotoFeedback();
            if(flash>0){var color=GUI.color;GUI.color=new Color(1,1,1,flash*.55f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;}
            DrawGuidedRecovery(text,button);
        }
    }
}
