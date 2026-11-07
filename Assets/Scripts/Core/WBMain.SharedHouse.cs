using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        public bool sharedHouse;
        public bool deskStudy;
        public bool tapeDeskView=true;
        public Transform StudyEntries {get;private set;}
        public StudyCurtain Curtain {get;private set;}
        bool studyRevealed;
        // Tape handling uses the current exploration pose, never an operation anchor.
        public bool DirectTapeHandling => !InStation && radioDeck!=null && radioDeck.State==HouseRadioDeck.Phase.Dragging;
        public bool DirectRadioReady => houseReady && !loadingSpringRoom && !InStation && transition<=0 && !houseReturnGate && houseFocused && !(safeInspection!=null && safeInspection.BlocksInput);
        bool tapePointerWasFree;
        public Camera playerCamera;
        public FirstPersonHands playerHands;
        public GameObject houseShell;
        public SafeInspection safeInspection;
        public HouseRadioDeck radioDeck {get;private set;}
        public SceneViewAnchor playerSpawn;
        [System.NonSerialized] bool houseReady;
        [HideInInspector,Range(.5f,3)] public float houseWalkSpeed=1.8f;
        [HideInInspector,Range(.1f,8)] public float houseLookSensitivity=2.1f;
        bool awaitRelease,houseFocused=true,houseReturnGate;
        readonly PlayerInputReleaseGate houseHandoff=new PlayerInputReleaseGate();
        bool returningToRoom,houseFade;
        float houseTransitionDuration;
        public PlayerControlState ControlState=>!houseFocused?PlayerControlState.Unfocused:
            bedroomLoadFailed?PlayerControlState.Paused:
            safeInspection!=null && safeInspection.Active?PlayerControlState.Inspection:
            transition>0 || loadingSpringRoom || awaitRelease?PlayerControlState.Transition:ShowHelp?PlayerControlState.Paused:
            InStation && CurrentWorld is PhotoWorld3D && ((PhotoWorld3D)CurrentWorld).ParkGalleryOpen?PlayerControlState.Gallery:
            InStation || DirectDarkroomActive || DirectTapeHandling || (radioDeck!=null && (radioDeck.RecordingMode || radioDeck.WorkbenchMode || radioDeck.SuccessPopupOpen))?PlayerControlState.Operation:PlayerControlState.Exploration;
        public void SuspendHouseInput()
        {
            photoPointer.Clear();
            InputContext.Handoff();
            houseReturnGate=true;houseHandoff.Arm();Room?.StopMotion();
            InputState.Keys.Clear();InputState.Down=false;wasDown=wasRightDown=false;
        }
        public bool AwaitingHouseRelease => houseReturnGate;
        public void ReleaseHouseReturnInput(bool allReleased)
        {if(houseReturnGate && allReleased)houseReturnGate=false;}
        Vector3 houseFrom,houseTo;
        Quaternion houseFromRotation,houseToRotation;
        float houseFromFov,houseToFov;
        string cameraRouteNotice="";
        float cameraRouteNoticeUntil;
        public bool HouseReady => houseReady;
        public bool HouseInputReady => houseReady && !loadingSpringRoom && houseFocused && InStation && transition<=0 && !awaitRelease && !houseReturnGate && !ShowHelp && !(safeInspection!=null && safeInspection.BlocksInput);

        void Start()
        {
            if(!sharedHouse)return;
            Instance=this;IsQA=false;Application.targetFrameRate=60;
            var startupTrace=new BedroomLoadTrace("host-startup");
            UIFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            InitializeBedroomCover();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=.12f;
            if(playerCamera==null || playerHands==null)
            {Debug.LogError("House requires its authored player camera and hands.",this);enabled=false;return;}
            Room=new RoomWorld();Room.CreateShared(playerCamera,houseShell);
            if(safeInspection!=null){safeInspection.InputContext=InputContext;safeInspection.RestoreInputContext=()=>{SuspendHouseInput();SyncInputContext();};}
            startupTrace.Mark("player-and-input");
            chapters[0]=new RadioTapeChapter();
            for(int i=1;i<5;i++)
            {
                using(startupTrace.Measure("create-chapter-"+i))CreateHouseChapter(i);
                if(deskStudy)worlds[i].Root.SetActive(false);
            }
            ((PhotoWorld3D)worlds[1]).BindOperationAreas();
            startupTrace.Mark("all-chapters-created");
            using(startupTrace.Measure("bind-chapter-data"))BindChapterData();
            using(startupTrace.Measure("restore-story-save"))LoadStoryProgress();
            ((PaintingWorld3D)worlds[3]).BindSharedScarf((EndingWorld3D)worlds[4]);
            var hands=playerHands;hands.BindHouse(this);hands.Register(0,worlds[0]);
            for(int i=1;i<5;i++)hands.Register(i,worlds[i]);
            if(deskStudy){StudyEntries=houseShell.transform.Find("Study entry props");Curtain=houseShell.GetComponentInChildren<StudyCurtain>();SetStudyPresentation(-1);}
            if(bedroomCover!=null)HideLegacyHousePresentation();
            // Writing's memory is rendered only by its dedicated camera.
            Room.Camera.cullingMask &= ~(1<<26);
            if(playerSpawn!=null)playerSpawn.Apply(Room.Camera);
            bool returnedView=story.HasHouseReturn;
            story.RestoreReturn(Room.Camera);
            ParkFilmRoute.RestoreHouseView(Room.Camera);
            story.ClearReturn();
            Room.EnableLook(true);houseReady=true;SuspendHouseInput();
            enterReturnedDarkroom=ParkFilmRoute.EnterDarkroomOnLoad;
            if(enterReturnedDarkroom){if(Curtain!=null && !Curtain.IsOpen){Curtain.Toggle();Curtain.Advance(1);}}
            PrepareContinuousDarkroom();
            startupTrace.Mark("restore-and-presentation");startupTrace.End();
            StartSeasonBedroom(returnedView);
        }
        void CreateHouseChapter(int index)
        {
            switch(index)
            {
                case 1:chapters[index]=new PhotoChapter();worlds[index]=new PhotoWorld3D(this,(PhotoChapter)chapters[index]);break;
                case 2:chapters[index]=new WritingChapter();worlds[index]=new WritingWorld3D(this,(WritingChapter)chapters[index]);break;
                case 3:chapters[index]=new PaintingChapter();worlds[index]=new PaintingWorld3D(this,(PaintingChapter)chapters[index]);break;
                case 4:chapters[index]=new EndingChapter();worlds[index]=new EndingWorld3D(this,(EndingChapter)chapters[index]);break;
            }
            if(worlds[index].Root.scene!=gameObject.scene)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(worlds[index].Root,gameObject.scene);
            worlds[index].Root.name="House station - "+HouseLayout.Names[index];
        }
        public bool TryEnterHouseStation(int index)
        {
            if((index==0 && !tapeDeskView) || DirectTapeHandling)return false;
            if(deskStudy && index==5 && !InStation && transition<=0){var curtainEntry=StudyEntry.Pick(Room.Camera);if(curtainEntry==null || curtainEntry.chapter!=5)return false;if(DarkroomWorld!=null && DarkroomWorld.PhotographyCompleteForDarkroom)Curtain.Toggle();return true;}
            if(!houseReady || loadingSpringRoom || InStation || transition>0 || index<0 || index>=5)return false;
            if(!CanEnterDay(index))
            {
                cameraRouteNotice=ChapterEntryBlockReason(index);
                cameraRouteNoticeUntil=Time.unscaledTime+4;
#if UNITY_EDITOR
                Debug.LogWarning("Chapter entry blocked: "+ChapterEntryBlockReason(index),this);
#endif
                return false;
            }
            if(deskStudy)
            {
                var ray=Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
                var hit=StudyEntry.PickVisible(ray);
                var entry=hit!=null?hit.GetComponentInParent<StudyEntry>():null;
                bool tapeEntry=index==0 && radioDeck!=null && radioDeck.IsEntryHit(hit);
                if(!tapeEntry && (entry==null || entry.chapter!=index))return false;
                EnterHouseStation(index);return true;
            }
            Vector3 delta=HouseLayout.Targets[index]-Room.Camera.transform.position;
            if(delta.magnitude>2.5f || Vector3.Dot(Room.Camera.transform.forward,delta.normalized)<.9f)return false;
            EnterHouseStation(index);return true;
        }
        public bool TryInspectStudyEntry()
        {
            if(!SafeInspection.EnabledForGameplay || !houseReady || !deskStudy || InStation || transition>0 || safeInspection==null || safeInspection.BlocksInput)return false;
            var ray=Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
            var subject=safeInspection.ForHit(StudyEntry.PickVisible(ray));
            if(subject==null)
            {
                var source=radioDeck!=null?radioDeck.InspectionSource(ray):null;
                subject=source!=null?safeInspection.ForSource(source):safeInspection.ForEntry(StudyEntry.Pick(Room.Camera));
            }
            if(subject==null)return false;
            safeInspection.feedback.mute=Muted;return safeInspection.Open(subject);
        }
        void EnterHouseStation(int index)
        {
            if(!houseReady || InStation || DirectTapeHandling || transition>0 || index<0 || index>=5 || worlds[index]==null)return;
            if(!CanEnterDay(index))return;
            if(index==2 && (narrativeBedroom==null || narrativeBedroom.writingDesk==null))return;
            if(index==4){springPreview=true;return;}
            if(index==3 && chapters[3].Completed)return;
            if(index==1 && DarkroomWorld!=null && DarkroomWorld.DirectDarkroomAvailable){PrepareContinuousDarkroom();return;}
            if(index==1 && deskStudy){TryStartParkPhotography();return;}
            SaveStableProgress();
            radioDeck?.SetStationActive(false);
            roomEye=Room.Camera.transform.position;roomRotation=Room.Camera.transform.rotation;roomFov=Room.Camera.fieldOfView;
            ChapterIndex=index;Current=chapters[index];CurrentWorld=worlds[index];
            if(index==0 && CurrentWorld is RadioTapeWorld)((RadioTapeWorld)CurrentWorld).ReadViewAnchor();
            var configuredView=FindChapterBinding(ChapterIds.At(index))?.operationView;
            if(configuredView!=null){CurrentWorld.Eye=configuredView.transform.position;CurrentWorld.Look=CurrentWorld.Eye+configuredView.transform.forward;CurrentWorld.Fov=configuredView.fieldOfView;}
            if(index==2 && narrativeBedroom!=null && narrativeBedroom.chapterEntry.chapter==2)
                ((WritingWorld3D)CurrentWorld).FitTeaTable(narrativeBedroom.writingDesk);
            InStation=true;ShowHelp=false;awaitRelease=true;houseHandoff.Arm();houseReturnGate=false;returningToRoom=false;
            Room.EnableLook(false);InputState.Keys.Clear();InputState.Down=false;
            studyRevealed=false;
            if(index==0 && radioDeck!=null)BeginHouseTransition(CurrentWorld.Eye,radioDeck.operationView!=null?radioDeck.operationView.transform.rotation:Quaternion.LookRotation(CurrentWorld.Look-CurrentWorld.Eye),CurrentWorld.Fov);
            else BeginHouseTransition(CurrentWorld.Eye,CurrentWorld.ViewRotation,CurrentWorld.Fov);
        }
        // Enter the memory first; its mechanical loading controller owns the roll.
        public bool TryStartParkPhotography()
        {
            if(!houseReady || !houseFocused || !deskStudy || !CanEnterDay(1) || transition>0 ||
                DirectTapeHandling || ShowHelp || (safeInspection!=null && safeInspection.BlocksInput) ||
                (InStation && (ChapterIndex!=1 || !HouseInputReady)))return false;
            try
            {
                var film=ParkFilmStore.LoadOrCreate();
                if(film.state!=ParkFilmState.Loaded)
                    throw new System.InvalidOperationException("Film must be developed before another park visit.");
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.ParkScenePath))
                    throw new System.InvalidOperationException("ParkScene is not in Build Settings.");
                if(!SaveStableProgress())
                {cameraRouteNotice="Progress not saved. Stay here and retry. "+LastSaveError;cameraRouteNoticeUntil=Time.unscaledTime+6;return false;}
                var returnEye=InStation?roomEye:Room.Camera.transform.position;
                var returnRotation=InStation?roomRotation:Room.Camera.transform.rotation;
                float returnFov=InStation?roomFov:Room.Camera.fieldOfView;
                if(!story.RememberReturn(returnEye,returnRotation,returnFov))return false;
                SuspendHouseInput();
                ParkFilmRoute.RememberHouseView(returnEye,returnRotation,returnFov);
                BrushSceneTransition.LoadScene(ParkFilmRoute.ParkScenePath);
                return true;
            }
            catch(System.Exception e)
            {
                ParkFilmRoute.ForgetHouseView();
                cameraRouteNotice="Park unavailable. Film progress kept.";
                cameraRouteNoticeUntil=Time.unscaledTime+6;
                Debug.LogWarning("House camera route: "+e.Message);
            }
            return false;
        }
        void SetStudyPresentation(int index)
        {
            if(!deskStudy)return;
            for(int i=1;i<5;i++)worlds[i].Root.SetActive(i==index || (i==4 && SpringActive));
            if(StudyEntries!=null){if(index>0 && radioDeck!=null)radioDeck.PrepareForHide();StudyEntries.gameObject.SetActive(springTapeRoom==null && index<0);}
            if(narrativeBedroom!=null)
            {
                if(index<0)((WritingWorld3D)worlds[2]).ReleaseTeaTable();
                narrativeBedroom.PresentStation(this,index>=0);
            }
            else if(index<0)PrepareContinuousDarkroom();
        }
        public bool TryToggleStudyCurtain(Ray ray)
        {
            RaycastHit hit;if(!deskStudy || Curtain==null || !Physics.Raycast(ray,out hit,2.5f,1<<25))return false;
            var entry=hit.collider.GetComponent<StudyEntry>();if(entry==null || entry.chapter!=5)return false;if(DarkroomWorld!=null && DarkroomWorld.PhotographyCompleteForDarkroom)Curtain.Toggle();return true;
        }
        void BeginHouseTransition(Vector3 eye,Quaternion rotation,float fov)
        {
            houseFrom=Room.Camera.transform.position;houseFromRotation=Room.Camera.transform.rotation;houseFromFov=Room.Camera.fieldOfView;
            houseTo=eye;houseToRotation=rotation;houseToFov=fov;
            houseTransitionDuration=Mathf.Max(.1f,PlayerControlSettings.Current.stationTransitionSeconds);
            houseFade=deskStudy && !(ChapterIndex==0 && radioDeck!=null) && PlayerControlSettings.Current.fadeBetweenStudyStations;
            studyRevealed=false;transition=houseTransitionDuration;Room.StopMotion();
            PlayerInputPolicy.ApplyCursor(PlayerControlState.Transition);
        }
        public void AdvanceHouseTransition(float dt)
        {
            if(transition<=0)return;
            transition=Mathf.Max(0,transition-Mathf.Max(0,dt));
            float progress=1-transition/houseTransitionDuration;
            if(houseFade)
            {
                if(progress>=.5f && !studyRevealed)
                {
                    // Commit the spatial switch on a fully black rendered frame.
                    transition=houseTransitionDuration*.5f;studyRevealed=true;
                    if(returningToRoom)CompleteHouseExit();else SetStudyPresentation(ChapterIndex);
                }
                bool target=studyRevealed;
                Room.Camera.transform.SetPositionAndRotation(target?houseTo:houseFrom,target?houseToRotation:houseFromRotation);
                Room.Camera.fieldOfView=target?houseToFov:houseFromFov;
            }
            else
            {
                if(!studyRevealed){if(!returningToRoom)SetStudyPresentation(ChapterIndex);studyRevealed=true;}
                float t=Mathf.SmoothStep(0,1,progress);
                Room.Camera.transform.SetPositionAndRotation(Vector3.Lerp(houseFrom,houseTo,t),Quaternion.Slerp(houseFromRotation,houseToRotation,t));
                Room.Camera.fieldOfView=Mathf.Lerp(houseFromFov,houseToFov,t);
            }
            if(transition<=0 && returningToRoom)
            {
                if(InStation)CompleteHouseExit();
                Room.Camera.transform.SetPositionAndRotation(houseTo,houseToRotation);Room.Camera.fieldOfView=houseToFov;
                returningToRoom=false;Room.EnableLook(true);SuspendHouseInput();
            }
        }
        // Keys held across contexts must be released before the new context accepts input.
        public void ReleaseHouseEntryInput()
        {
            if(!InStation || transition>0 || !awaitRelease)return;
            awaitRelease=false;wasDown=wasRightDown=false;
            if(ChapterIndex==0){radioDeck.SetStationActive(true);if(tapeDeskView)radioDeck.OpenWorkbench();}
            else CurrentWorld.Activate();
            var angles=Room.Camera.transform.eulerAngles;lookYaw=angles.y;lookPitch=angles.x>180?angles.x-360:angles.x;
            PlayerInputPolicy.ApplyCursor(ControlState,CurrentWorld.WantsMouseLook);
            SuspendHouseInput();
        }
        void ExitHouseStation()
        {
            if(!houseReady || !InStation || returningToRoom)return;
            var photo=CurrentWorld as PhotoWorld3D;
            if(photo!=null)photo.CancelInteraction();
            if(!SaveStableProgress())
            {
                if(photo!=null)photo.NotifyDarkroomExitBlocked();
                SuspendHouseInput();return;
            }
            returningToRoom=true;ShowHelp=false;awaitRelease=false;houseReturnGate=false;
            InputState.Keys.Clear();InputState.Down=false;
            if(photo==null)CurrentWorld?.CancelPointerGesture();
            BeginHouseTransition(roomEye,roomRotation,roomFov);
        }
        public void ExitTapeDeskView()
        {
            if(!InStation || ChapterIndex!=0)return;
            radioDeck.CloseWorkbench();radioDeck.CloseRecording();
            ExitHouseStation();
        }
        void CompleteHouseExit()
        {
            // Finish chapter cleanup before restoring the captured exploration pose:
            // tape close-view cleanup may itself write to the shared camera.
            if(ChapterIndex==0)radioDeck.SetStationActive(false);
            else if(CurrentWorld.Active)CurrentWorld.Deactivate();
            if(deskStudy)SetStudyPresentation(-1);
            // Return to the durable node, never to a held gesture or half-transfer.
            if(!Current.Completed && story.Available)
            {
                try{Current.RestoreCheckpoint(story.Checkpoint(ChapterIndex));}
                catch(System.Exception e){story.BlockRestore(e);}
            }
            InputState.Keys.Clear();InputState.Down=false;wasDown=wasRightDown=false;
            Done[ChapterIndex]=story.IsComplete(ChapterIndex);
            Room.Camera.rect=new Rect(0,0,1,1);Room.Camera.cullingMask &= ~(1<<26);
            Room.Camera.ResetAspect();
            InStation=false;ShowHelp=false;awaitRelease=false;
            radioDeck?.SetStationActive(true);
            audioSource.Stop();
            story.ClearReturn();
        }
        void UpdateHouse()
        {
            if(!houseReady || Room==null || !houseFocused || loadingSpringRoom)return;
            FinishSpringBedroomIfReady();
            if(loadingSpringRoom)return;
            if(PollDebugChapterKeys())return;
            TickStoryProgress();
            RestoreHouseCursor();
            if(radioDeck!=null && radioDeck.SuccessPopupOpen){Room.StopMotion();return;}
            if(safeInspection!=null && safeInspection.BlocksInput){Room.StopMotion();return;}
            if(transition>0){AdvanceHouseTransition(Mathf.Min(Time.unscaledDeltaTime,.1f));return;}
            if(houseReturnGate){Room.StopMotion();ReleaseHouseReturnInput(houseHandoff.Ready);return;}
            if(awaitRelease){if(houseHandoff.Ready)ReleaseHouseEntryInput();return;}
            if(!InputContext.Ready){Room.StopMotion();return;}
            if(radioDeck!=null && radioDeck.RecordingMode){radioDeck.TickRecording();return;}
            if(radioDeck!=null && radioDeck.WorkbenchMode){radioDeck.TickWorkbench();return;}
            if(ExitHouseContext())return;
            if(ShowHelp)
            {
                Room.StopMotion();
                if(Input.GetKeyDown(KeyCode.H))InputContext.RequestExit(InputContextId.Paused,null,()=>SetHelpVisible(false));
                return;
            }
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(transition>0){AdvanceHouseTransition(dt);return;}
            if(!InStation)
            {
                if(TickNarrativeBedroom(dt) || TickSummerPhotography(dt))return;
                if(TickSpring(dt))return;
                if(TickContinuousDarkroom(dt))return;
                if(radioDeck==null){Room.WalkTick(dt);return;}
                if(!radioDeck.StationActive && CanEnterDay(0))radioDeck.SetStationActive(true);
                if(!CanEnterDay(0) && radioDeck.StationActive)radioDeck.SetStationActive(false);
                SyncTapePointer();
                var controls=PlayerControlSettings.Current;
                Room.WalkSpeed=controls.walkSpeed*controls.houseSpeedMultiplier;Room.LookSensitivity=controls.lookSensitivity;
                if(DirectTapeHandling)Room.StopMotion();else Room.WalkTick(dt);
                var ray=DirectTapeHandling?Room.Camera.ScreenPointToRay(Input.mousePosition):Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
                bool radioTarget=radioDeck.IsEntryHit(StudyEntry.PickVisible(ray));
                if(tapeDeskView)
                {
                    if(Input.GetKeyDown(KeyCode.E)){TryInspectStudyEntry();return;}
                    if(Input.GetMouseButtonDown(0))
                    {
                        // Native box/tape colliders enter through the existing desk workflow.
                        if(radioTarget){TryEnterHouseStation(0);return;}
                        var entry=StudyEntry.Pick(Room.Camera);
                        if(entry!=null)TryEnterHouseStation(entry.chapter);
                    }
                    return;
                }
                if(Input.GetKeyDown(KeyCode.E))
                {
                    if(DirectTapeHandling || radioTarget){if(radioDeck.TryInspect(ray))return;}
                    else if(TryInspectStudyEntry())return;
                }
                bool handling=DirectTapeHandling;
                radioDeck.TickStation(dt,ray,Input.GetMouseButton(0),Input.GetMouseButton(1),Input.mouseScrollDelta.y,false,PlayerInputPolicy.ActionsReleased);
                Done[0]=story.IsComplete(0);SyncTapePointer();
                if(handling && !DirectTapeHandling){SuspendHouseInput();return;}
                if(!handling && !DirectTapeHandling && !radioTarget && Input.GetMouseButtonDown(0) && Room.AimedStation>0)TryEnterHouseStation(Room.AimedStation);
                return;
            }
            if(awaitRelease)
            {
                if(houseHandoff.Ready)ReleaseHouseEntryInput();return;
            }
            if(ChapterIndex==0)
            {
                if(radioDeck!=null)
                {
                    if(Input.GetKeyDown(KeyCode.H)){SetHelpVisible(!ShowHelp);radioDeck.SuspendInput();}
                    if(Input.GetKeyDown(KeyCode.M)){Muted=!Muted;radioDeck.speaker.mute=Muted;}
                    radioDeck.TickStation(dt,Room.Camera.ScreenPointToRay(Input.mousePosition),Input.GetMouseButton(0),Input.GetMouseButton(1),Input.mouseScrollDelta.y,Input.GetKeyDown(KeyCode.E),PlayerInputPolicy.ActionsReleased);
                }
                Done[0]=story.IsComplete(0);return;
            }
            if(ChapterIndex!=1 && Input.GetKeyDown(KeyCode.H)){SetHelpVisible(!ShowHelp);return;}
            if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
            if(ShowHelp)return;
            if(CurrentWorld.WantsMouseLook){var look=PlayerInputPolicy.LookDelta;var controls=PlayerControlSettings.Current;lookYaw+=look.x*controls.lookSensitivity;lookPitch=controls.ClampPitch(lookPitch-look.y*controls.lookSensitivity);Room.Camera.transform.rotation=Quaternion.Euler(lookPitch,lookYaw,0);}
            InputState.Keys.Clear();foreach(var key in keys)if(Input.GetKey(key))InputState.Keys.Add(key);
            bool down=Input.GetMouseButton(0),right=Input.GetMouseButton(1);InputState.Down=down && !PointerOverReturnButton;InputState.Time=Time.unscaledTime;
            Vector2 pointer=CurrentWorld.WantsMouseLook?new Vector2(Screen.width*.5f,Screen.height*.5f):(Vector2)Input.mousePosition;
            pointerRay=CurrentWorld.PointerRay(pointer);string id=CurrentWorld.Pick(pointerRay);CurrentWorld.HoverId=id;
            var photoWorld=CurrentWorld as PhotoWorld3D;
            bool photoUI=PointerOverReturnButton || (deskStudy && photoWorld!=null && photoWorld.PointerOverUI);
            Ray capturedRay;
            if(photoPointer.TryBegin(out capturedRay))
            {
                bool curtainClick=photoWorld!=null && deskStudy && !photoWorld.Photo.InViewfinder && !photoWorld.Photo.MeterVisible && TryToggleStudyCurtain(capturedRay);
                if(!curtainClick)CurrentWorld.Press(CurrentWorld.Pick(capturedRay),capturedRay,0);
            }
            if(right&&!wasRightDown && !photoUI)CurrentWorld.Press(id,pointerRay,1);
            CurrentWorld.Move(pointerRay);
            if(photoPointer.TryEnd(down,out capturedRay))
            {CurrentWorld.Move(capturedRay);CurrentWorld.Release(CurrentWorld.Pick(capturedRay),capturedRay);}
            else if(!down&&!right&&wasRightDown && !CurrentWorld.HasPointerGesture)CurrentWorld.Release(id,pointerRay);
            if(!photoUI && Mathf.Abs(Input.mouseScrollDelta.y)>.01f)CurrentWorld.Scroll(id,Input.mouseScrollDelta.y,pointerRay);
            CurrentWorld.Tick(ChapterIndex==2?Time.unscaledDeltaTime:dt,InputState);if(!InStation)return;if(!CurrentWorld.WantsMouseLook)CurrentWorld.ApplyPose();
            Done[ChapterIndex]=story.IsComplete(ChapterIndex);wasDown=down;wasRightDown=right;
        }
        // House owns cursor policy outside the inspection modal. Focus/Editor unlocks
        // must not become the next saved exploration state.
        void RestoreHouseCursor()
        {
            if(!sharedHouse || !houseReady || Room==null || !houseFocused || !isActiveAndEnabled)return;
            if(safeInspection!=null && safeInspection.Active)return;
            if(ControlState==PlayerControlState.Exploration && (Cursor.lockState!=CursorLockMode.Locked || Cursor.visible))Room.EnableLook(true);
            SyncInputContext();
        }
        void SyncTapePointer()
        {
            bool free=DirectTapeHandling || (radioDeck!=null && radioDeck.WorkbenchMode);
            if(free!=tapePointerWasFree){Room.EnableLook(!free);tapePointerWasFree=free;PlayerInputPolicy.SuppressLook();}
            SyncInputContext();
        }
        void DrawHouse()
        {
            if(loadingSpringRoom)return;
            if(!houseReady || Room==null)return;
            if(StoryFinished){DrawEndingPresentation();return;}
            if(DepartureRoom!=null && !ShowHelp)return;
            if(InStation && CurrentWorld is PaintingWorld3D album && album.ActWhite>0)
            {
                var old=GUI.color;GUI.color=new Color(1,1,1,album.ActWhite);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;return;
            }
            if(radioDeck!=null && radioDeck.SuccessPopupOpen)return;
            if(safeInspection!=null && safeInspection.BlocksInput)return;
            if(transition>0)
            {
                if(houseFade){var previous=GUI.color;float progress=1-transition/houseTransitionDuration;float tone=narrativeBedroom!=null && ChapterIndex==3 && !returningToRoom?1:0;GUI.color=new Color(tone,tone,tone,Mathf.SmoothStep(0,1,1-Mathf.Abs(progress*2-1)));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=previous;}
                return;
            }
            var style=new GUIStyle(GUI.skin.label){font=UIFont,fontSize=18,wordWrap=true};
            if(DirectDarkroomActive)
            {
                if(!ShowHelp)DarkroomWorld.DrawProgress(style);
                return;
            }
            if(InStation && ChapterIndex!=1 && Time.unscaledTime<cameraRouteNoticeUntil)
                InteractionUI.Label(new Rect(28,60,Screen.width-56,60),cameraRouteNotice,style);
            if(!InStation && transition<=0)
            {
                if(DirectTapeHandling || (radioDeck!=null && (radioDeck.RecordingMode || radioDeck.WorkbenchMode)))return;
                InteractionUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                string hint=Room.AimedStation>=0?"LMB  "+InteractionUI.ChapterName(Room.AimedStation)+(SafeInspection.EnabledForGameplay && safeInspection!=null && Room.AimedStation!=5?"  /  E  Inspect":""):null;
                if(radioDeck!=null)hint=radioDeck.GetHint(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f)))??hint;
                if(SafeInspection.EnabledForGameplay && safeInspection!=null && Room.AimedStation<0)
                {
                    var item=safeInspection.ForHit(StudyEntry.PickVisible(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f))));
                    if(item!=null)hint="E  Inspect";
                }
                if(deskStudy && Room.AimedStation==1)
                    hint=((PhotoWorld3D)worlds[1]).HouseCameraEntryHint+(SafeInspection.EnabledForGameplay && safeInspection!=null?"  /  E  Inspect":"");
                if(Room.AimedStation>=0 && Room.AimedStation<5 && !CanEnterDay(Room.AimedStation))
                    hint=ChapterEntryBlockReason(Room.AimedStation)+(SafeInspection.EnabledForGameplay && safeInspection!=null?"  /  E  Inspect":"");
                if(summerPhotoRoom!=null)hint=summerPhotoRoom.HoverHint(this,Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f)))??hint;
                if(narrativeBedroom!=null)
                    hint=narrativeBedroom.IsEntry(StudyEntry.Pick(Room.Camera))
                        ?(narrativeBedroom.chapterEntry.chapter==2?"LMB  Sit and write":"LMB  Enter the drawing")+(SafeInspection.EnabledForGameplay && narrativeInspection!=null?"  /  E  Inspect":"")
                        :null;
                if(Time.unscaledTime<cameraRouteNoticeUntil)hint=cameraRouteNotice;
                if(SpringActive)hint=((EndingChapter)chapters[4]).DoorOpen?"Walk through the doorway":"LMB  Open the entrance door";
                if(CurrentDay!=StoryDay.SummerPhoto && Room.AimedStation!=1 && !string.IsNullOrEmpty(hint))InteractionUI.Label(new Rect(28,24,Screen.width-56,48),hint,style);
            }
            else if(InStation)
            {
                if(ChapterIndex==1)
                {
                    if(deskStudy && transition<=0 && !awaitRelease && !ShowHelp)
                    {bool enabled=GUI.enabled;GUI.enabled=enabled && InputContext.Ready;((PhotoWorld3D)CurrentWorld).DrawProgress(style);GUI.enabled=enabled;}
                    return;
                }
                if(ChapterIndex==3 && Current is PaintingChapter winter && winter.Phase>=2)return;
                if(ChapterIndex==0 && radioDeck!=null && (radioDeck.RecordingMode || radioDeck.WorkbenchMode))return;
                if(ChapterIndex==0 && radioDeck!=null){radioDeck.DrawStation(style);if(ShowHelp){InteractionUI.Label(new Rect(32,140,Mathf.Min(826,Screen.width-64),170),CurrentWorld.Help,style);}}

                if(awaitRelease && transition<=0)InteractionUI.Label(new Rect(20,70,600,40),"Release controls to continue",style);
                if(ChapterIndex!=0)
                {
                    InteractionUI.Label(new Rect(20,12,600,35),InteractionUI.ChapterName(ChapterIndex)+"  /  H  Help",style);
                    if(ShowHelp){InteractionUI.Label(new Rect(32,72,Mathf.Min(870,Screen.width-64),210),CurrentWorld.Help,style);}
                    else if(ChapterIndex>=2)InteractionUI.Label(new Rect(20,48,Screen.width-56,52),OperationHint(),style);
                    if(CurrentWorld.WantsMouseLook)InteractionUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                }
            }
        }
        string OperationHint()
        {
            if(Current.Completed)return "Complete";
            var writing=Current as WritingChapter;if(writing!=null)return writing.TaskHint;
            var album=CurrentWorld as PaintingWorld3D;if(album!=null)return album.Guidance;
            if(Current is EndingChapter)return "LMB  Open door / Walk through";
            return "LMB  Interact";
        }
    }
}


