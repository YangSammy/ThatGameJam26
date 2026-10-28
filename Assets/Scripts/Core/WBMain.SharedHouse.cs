using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        public bool sharedHouse;
        public bool deskStudy;
        public Transform StudyEntries {get;private set;}
        public StudyCurtain Curtain {get;private set;}
        bool studyRevealed;
        // Tape handling uses the current exploration pose, never an operation anchor.
        public bool DirectTapeHandling => !InStation && radioDeck!=null && radioDeck.State==HouseRadioDeck.Phase.Dragging;
        public bool DirectRadioReady => houseReady && !InStation && transition<=0 && !houseReturnGate && houseFocused && !(safeInspection!=null && safeInspection.BlocksInput);
        bool tapePointerWasFree;
        public Camera playerCamera;
        public FirstPersonHands playerHands;
        public GameObject houseShell;
        public SafeInspection safeInspection;
        public HouseRadioDeck radioDeck;
        public SceneViewAnchor playerSpawn;
        [System.NonSerialized] bool houseReady;
        [HideInInspector,Range(.5f,3)] public float houseWalkSpeed=1.8f;
        [HideInInspector,Range(.1f,8)] public float houseLookSensitivity=2.1f;
        bool awaitRelease,houseFocused=true,houseReturnGate;
        readonly PlayerInputReleaseGate houseHandoff=new PlayerInputReleaseGate();
        bool returningToRoom,houseFade;
        float houseTransitionDuration;
        public PlayerControlState ControlState=>!houseFocused?PlayerControlState.Unfocused:
            safeInspection!=null && safeInspection.Active?PlayerControlState.Inspection:
            transition>0 || awaitRelease?PlayerControlState.Transition:ShowHelp?PlayerControlState.Paused:
            InStation && CurrentWorld is PhotoWorld3D && ((PhotoWorld3D)CurrentWorld).ParkGalleryOpen?PlayerControlState.Gallery:
            InStation || DirectTapeHandling || (radioDeck!=null && radioDeck.RecordingMode)?PlayerControlState.Operation:PlayerControlState.Exploration;
        public void SuspendHouseInput()
        {
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
        public bool HouseInputReady => houseReady && houseFocused && InStation && transition<=0 && !awaitRelease && !houseReturnGate && !ShowHelp && !(safeInspection!=null && safeInspection.BlocksInput);

        void Start()
        {
            if(!sharedHouse)return;
            Instance=this;IsQA=false;Application.targetFrameRate=60;
            UIFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=.12f;
            if(playerCamera==null || playerHands==null || radioDeck==null || radioDeck.Chapter==null)
            {Debug.LogError("House requires its authored player camera, hands and initialized radio.",this);enabled=false;return;}
            Room=new RoomWorld();Room.CreateShared(playerCamera,houseShell);
            chapters[0]=radioDeck.Chapter;
            worlds[0]=new RadioTapeWorld(radioDeck);
            for(int i=1;i<5;i++)CreateHouseChapter(i);
            LoadStoryProgress();
            ((PaintingWorld3D)worlds[3]).BindSharedScarf((EndingWorld3D)worlds[4]);
            var hands=playerHands;hands.BindHouse(this);hands.Register(0,worlds[0]);
            for(int i=1;i<5;i++)hands.Register(i,worlds[i]);
            if(deskStudy){StudyEntries=houseShell.transform.Find("Study entry props");Curtain=houseShell.GetComponentInChildren<StudyCurtain>();SetStudyPresentation(-1);}
            // Writing's memory is rendered only by its dedicated camera.
            Room.Camera.cullingMask &= ~(1<<26);
            if(playerSpawn!=null)playerSpawn.Apply(Room.Camera);
            story.RestoreReturn(Room.Camera);
            ParkFilmRoute.RestoreHouseView(Room.Camera);
            story.ClearReturn();
            Room.EnableLook(true);houseReady=true;radioDeck.SetStationActive(true);SuspendHouseInput();
            if(ParkFilmRoute.EnterDarkroomOnLoad){ParkFilmRoute.EnterDarkroomOnLoad=false;EnterHouseStation(1);}
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
            worlds[index].Root.name="House station - "+HouseLayout.Names[index];
        }
        public bool TryEnterHouseStation(int index)
        {
            if(index==0 || DirectTapeHandling)return false;
            if(deskStudy && index==5 && !InStation && transition<=0){var curtainEntry=StudyEntry.Pick(Room.Camera);if(curtainEntry==null || curtainEntry.chapter!=5)return false;Curtain.Toggle();return true;}
            if(!houseReady || InStation || transition>0 || index<0 || index>=5)return false;
            if(!CanEnterDay(index))return false;
            if(deskStudy){var entry=StudyEntry.Pick(Room.Camera);if(entry==null || entry.chapter!=index)return false;EnterHouseStation(index);return true;}
            Vector3 delta=HouseLayout.Targets[index]-Room.Camera.transform.position;
            if(delta.magnitude>2.5f || Vector3.Dot(Room.Camera.transform.forward,delta.normalized)<.9f)return false;
            EnterHouseStation(index);return true;
        }
        public bool TryInspectStudyEntry()
        {
            if(!houseReady || !deskStudy || InStation || transition>0 || safeInspection==null || safeInspection.BlocksInput)return false;
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
            if(!houseReady || InStation || DirectTapeHandling || transition>0 || index<=0 || index>=5)return;
            if(!CanEnterDay(index))return;
            SaveStableProgress();
            radioDeck.SetStationActive(false);
            roomEye=Room.Camera.transform.position;roomRotation=Room.Camera.transform.rotation;roomFov=Room.Camera.fieldOfView;
            ChapterIndex=index;Current=chapters[index];CurrentWorld=worlds[index];
            if(index==0 && CurrentWorld is RadioTapeWorld)((RadioTapeWorld)CurrentWorld).ReadViewAnchor();
            InStation=true;ShowHelp=false;awaitRelease=true;houseHandoff.Arm();houseReturnGate=false;returningToRoom=false;
            Room.EnableLook(false);InputState.Keys.Clear();InputState.Down=false;
            studyRevealed=false;
            if(index==0 && radioDeck!=null)BeginHouseTransition(CurrentWorld.Eye,radioDeck.operationView!=null?radioDeck.operationView.transform.rotation:Quaternion.LookRotation(CurrentWorld.Look-CurrentWorld.Eye),CurrentWorld.Fov);
            else BeginHouseTransition(CurrentWorld.Eye,Quaternion.LookRotation(CurrentWorld.Look-CurrentWorld.Eye),CurrentWorld.Fov);
        }
        // Called by the loading UI only after the mechanical steps have been saved.
        public bool TryStartParkPhotography()
        {
            if(!HouseInputReady || !deskStudy || ChapterIndex!=1 || !CanEnterDay(1))return false;
            try
            {
                var film=ParkFilmStore.LoadOrCreate();
                if(film.state!=ParkFilmState.Loaded)
                    throw new System.InvalidOperationException("Film must be developed before another park visit.");
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.ParkScenePath))
                    throw new System.InvalidOperationException("ParkScene is not in Build Settings.");
                SaveStableProgress();
                if(!story.RememberReturn(roomEye,roomRotation,roomFov))return false;
                SuspendHouseInput();
                ParkFilmRoute.RememberHouseView(roomEye,roomRotation,roomFov);
                UnityEngine.SceneManagement.SceneManager.LoadScene(ParkFilmRoute.ParkScenePath);
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
            for(int i=1;i<5;i++)worlds[i].Root.SetActive(i==index || (index==3 && i==4));
            if(StudyEntries!=null){if(index>0 && radioDeck!=null)radioDeck.PrepareForHide();StudyEntries.gameObject.SetActive(index<0 || (index==0 && radioDeck!=null));}
        }
        public bool TryToggleStudyCurtain(Ray ray)
        {
            RaycastHit hit;if(!deskStudy || Curtain==null || !Physics.Raycast(ray,out hit,2.5f,1<<25))return false;
            var entry=hit.collider.GetComponent<StudyEntry>();if(entry==null || entry.chapter!=5)return false;Curtain.Toggle();return true;
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
            if(ChapterIndex==0)radioDeck.SetStationActive(true);
            else CurrentWorld.Activate();
            var angles=Room.Camera.transform.eulerAngles;lookYaw=angles.y;lookPitch=angles.x>180?angles.x-360:angles.x;
            PlayerInputPolicy.ApplyCursor(ControlState,CurrentWorld.WantsMouseLook);
            SuspendHouseInput();
        }
        void ExitHouseStation()
        {
            if(!houseReady || !InStation || returningToRoom)return;
            SaveStableProgress();
            returningToRoom=true;ShowHelp=false;awaitRelease=false;houseReturnGate=false;
            InputState.Keys.Clear();InputState.Down=false;
            var photo=CurrentWorld as PhotoWorld3D;if(photo!=null){photo.CancelInteraction();photo.SaveProgress();}
            else CurrentWorld?.Release("",pointerRay);
            BeginHouseTransition(roomEye,roomRotation,roomFov);
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
            radioDeck.SetStationActive(true);
            audioSource.Stop();
            story.ClearReturn();
        }
        void UpdateHouse()
        {
            if(!houseReady || Room==null || !houseFocused)return;
            TickStoryProgress();
            RestoreHouseCursor();
            if(safeInspection!=null && safeInspection.BlocksInput){Room.StopMotion();return;}
            if(houseReturnGate){Room.StopMotion();ReleaseHouseReturnInput(houseHandoff.Ready);return;}
            if(radioDeck.RecordingMode){radioDeck.TickRecording();return;}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                var gallery=InStation?CurrentWorld as PhotoWorld3D:null;
                if(gallery!=null && gallery.CloseParkGallery())return;
                if(DirectTapeHandling){radioDeck.PutBack();SyncTapePointer();SuspendHouseInput();return;}
                if(InStation)ExitHouseStation();
                else RestoreHouseCursor();
                return;
            }
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(transition>0){AdvanceHouseTransition(dt);return;}
            if(!InStation)
            {
                if(!radioDeck.StationActive && CanEnterDay(0))radioDeck.SetStationActive(true);
                if(!CanEnterDay(0) && radioDeck.StationActive)radioDeck.SetStationActive(false);
                SyncTapePointer();
                var controls=PlayerControlSettings.Current;
                Room.WalkSpeed=controls.walkSpeed*controls.houseSpeedMultiplier;Room.LookSensitivity=controls.lookSensitivity;
                if(DirectTapeHandling)Room.StopMotion();else Room.WalkTick(dt);
                var ray=DirectTapeHandling?Room.Camera.ScreenPointToRay(Input.mousePosition):Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
                bool radioTarget=radioDeck.IsEntryHit(StudyEntry.PickVisible(ray));
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
            if(Input.GetKeyDown(KeyCode.H))SetHelpVisible(!ShowHelp);
            if(Input.GetKeyDown(KeyCode.M))Muted=!Muted;
            if(ShowHelp)return;
            if(CurrentWorld.WantsMouseLook){var look=PlayerInputPolicy.LookDelta;var controls=PlayerControlSettings.Current;lookYaw+=look.x*controls.lookSensitivity;lookPitch=controls.ClampPitch(lookPitch-look.y*controls.lookSensitivity);Room.Camera.transform.rotation=Quaternion.Euler(lookPitch,lookYaw,0);}
            InputState.Keys.Clear();foreach(var key in keys)if(Input.GetKey(key))InputState.Keys.Add(key);
            bool down=Input.GetMouseButton(0),right=Input.GetMouseButton(1);InputState.Down=down;InputState.Time=Time.unscaledTime;
            Vector2 pointer=CurrentWorld.WantsMouseLook?new Vector2(Screen.width*.5f,Screen.height*.5f):(Vector2)Input.mousePosition;
            pointerRay=CurrentWorld.PointerRay(pointer);string id=CurrentWorld.Pick(pointerRay);CurrentWorld.HoverId=id;
            var photoWorld=CurrentWorld as PhotoWorld3D;
            bool photoUI=deskStudy && photoWorld!=null && photoWorld.PointerOverUI;
            if(down&&!wasDown && !photoUI){bool curtainClick=deskStudy && photoWorld!=null && !photoWorld.Photo.InViewfinder && !photoWorld.Photo.MeterVisible && TryToggleStudyCurtain(pointerRay);if(!curtainClick)CurrentWorld.Press(id,pointerRay,0);}if(right&&!wasRightDown && !photoUI)CurrentWorld.Press(id,pointerRay,1);
            CurrentWorld.Move(pointerRay);if(!down&&wasDown || !right&&wasRightDown)CurrentWorld.Release(id,pointerRay);
            if(!photoUI && Mathf.Abs(Input.mouseScrollDelta.y)>.01f)CurrentWorld.Scroll(id,Input.mouseScrollDelta.y,pointerRay);
            CurrentWorld.Tick(dt,InputState);if(!CurrentWorld.WantsMouseLook)CurrentWorld.ApplyPose();
            Done[ChapterIndex]=story.IsComplete(ChapterIndex);wasDown=down;wasRightDown=right;
        }
        // House owns cursor policy outside the inspection modal. Focus/Editor unlocks
        // must not become the next saved exploration state.
        void RestoreHouseCursor()
        {
            if(!sharedHouse || !houseReady || Room==null || !houseFocused || !isActiveAndEnabled)return;
            if(safeInspection!=null && safeInspection.Active)return;
            if(ControlState==PlayerControlState.Exploration && (Cursor.lockState!=CursorLockMode.Locked || Cursor.visible))Room.EnableLook(true);
            PlayerInputPolicy.ApplyCursor(ControlState,InStation && CurrentWorld!=null && CurrentWorld.WantsMouseLook);
        }
        void SyncTapePointer()
        {
            bool free=DirectTapeHandling;
            if(free!=tapePointerWasFree){Room.EnableLook(!free);tapePointerWasFree=free;PlayerInputPolicy.SuppressLook();}
            PlayerInputPolicy.ApplyCursor(ControlState,InStation && CurrentWorld!=null && CurrentWorld.WantsMouseLook);
        }
        void DrawHouse()
        {
            if(!houseReady || Room==null)return;
            if(safeInspection!=null && safeInspection.BlocksInput)return;
            if(transition>0)
            {
                if(houseFade){var previous=GUI.color;float progress=1-transition/houseTransitionDuration;GUI.color=new Color(0,0,0,Mathf.SmoothStep(0,1,1-Mathf.Abs(progress*2-1)));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=previous;}
                return;
            }
            var style=new GUIStyle(GUI.skin.label){font=UIFont,fontSize=18,wordWrap=true};
            if(InStation && Time.unscaledTime<cameraRouteNoticeUntil)
                InteractionUI.Label(new Rect(28,60,Screen.width-56,60),cameraRouteNotice,style);
            if(!InStation && transition<=0)
            {
                if(DirectTapeHandling || radioDeck.RecordingMode)return;
                InteractionUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                string hint=Room.AimedStation>=0?"LMB  "+InteractionUI.ChapterName(Room.AimedStation)+(safeInspection!=null && Room.AimedStation!=5?"  /  E  Inspect":""):"WASD  Move   /   Mouse  Look   /   LMB  Interact   /   E  Inspect";
                if(radioDeck!=null)hint=radioDeck.GetHint(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f)))??hint;
                if(safeInspection!=null && Room.AimedStation<0)
                {
                    var item=safeInspection.ForHit(StudyEntry.PickVisible(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f))));
                    if(item!=null)hint="E  Inspect";
                }
                if(deskStudy && Room.AimedStation==1)
                    hint=((PhotoWorld3D)worlds[1]).HouseCameraEntryHint+(safeInspection!=null?"  /  E  Inspect":"");
                if(Time.unscaledTime<cameraRouteNoticeUntil)hint=cameraRouteNotice;
                InteractionUI.Label(new Rect(28,Screen.height-50,580,34),hint,style);
            }
            else if(InStation)
            {
                if(ChapterIndex==0 && radioDeck!=null){radioDeck.DrawStation(style);if(ShowHelp){InteractionUI.Label(new Rect(32,140,Mathf.Min(826,Screen.width-64),170),CurrentWorld.Help,style);}}
                InteractionUI.Label(new Rect(Screen.width-230,12,220,30),"Esc  Back",style);
                if(awaitRelease && transition<=0)InteractionUI.Label(new Rect(20,70,600,40),"Release controls to continue",style);
                if(ChapterIndex!=0)
                {
                    InteractionUI.Label(new Rect(20,12,600,35),InteractionUI.ChapterName(ChapterIndex)+"  /  H  Help",style);
                    if(ShowHelp){InteractionUI.Label(new Rect(32,72,Mathf.Min(870,Screen.width-64),210),CurrentWorld.Help,style);}
                    else if(ChapterIndex>=2)InteractionUI.Label(new Rect(28,Screen.height-72,Screen.width-56,52),OperationHint(),style);
                    if(CurrentWorld.WantsMouseLook)InteractionUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                    if(deskStudy && ChapterIndex==1 && transition<=0 && !awaitRelease && !ShowHelp)((PhotoWorld3D)CurrentWorld).DrawProgress(style);
                }
            }
        }
        string OperationHint()
        {
            if(Current.Completed)return "Complete  /  Esc  Back";
            var writing=Current as WritingChapter;
            if(writing!=null)return writing.Phase=="crumple"?"LMB drag paper to bin":writing.Phase=="return"?
                "WASD  Carry scarf to window  /  LMB drag  Keep writing":"LMB drag  Write  /  WASD  Move memory  /  E  Hold door";
            var painting=Current as PaintingChapter;
            if(painting!=null)return "Hold LMB  Focus  /  "+(painting.Phase==0?"Scarf":painting.Phase==1?"Cup contact":"Joined hands")+(painting.Ready?"":"  /  Wait for contact");
            var scarf=Current as EndingChapter;
            if(scarf!=null)return scarf.Phase==0?"LMB drag  Smooth corner":scarf.Phase==1?"LMB drag  Fold end":"Hold WASD + LMB drag  Tighten";
            return "LMB  Interact  /  Esc  Back";
        }
    }
}


