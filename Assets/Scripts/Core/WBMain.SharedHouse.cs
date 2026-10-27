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
            InStation?PlayerControlState.Operation:PlayerControlState.Exploration;
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
            ((PaintingWorld3D)worlds[3]).BindSharedScarf((EndingWorld3D)worlds[4]);
            var hands=playerHands;hands.BindHouse(this);hands.Register(0,worlds[0]);
            for(int i=1;i<5;i++)hands.Register(i,worlds[i]);
            if(deskStudy){StudyEntries=houseShell.transform.Find("Study entry props");Curtain=houseShell.GetComponentInChildren<StudyCurtain>();SetStudyPresentation(-1);}
            // Writing's memory is rendered only by its dedicated camera.
            Room.Camera.cullingMask &= ~(1<<26);
            if(playerSpawn!=null)playerSpawn.Apply(Room.Camera);
            ParkFilmRoute.RestoreHouseView(Room.Camera);
            Room.EnableLook(true);houseReady=true;SuspendHouseInput();
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
            if(deskStudy && index==5 && !InStation && transition<=0){var curtainEntry=StudyEntry.Pick(Room.Camera);if(curtainEntry==null || curtainEntry.chapter!=5)return false;Curtain.Toggle();return true;}
            if(!houseReady || InStation || transition>0 || index<0 || index>=5)return false;
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
            if(!houseReady || InStation || transition>0 || index<0 || index>=5)return;
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
            if(!HouseInputReady || !deskStudy || ChapterIndex!=1)return false;
            try
            {
                var film=ParkFilmStore.LoadActive();
                if(film==null || !film.ReadyForPark)
                    throw new System.InvalidOperationException("Camera loading has not been saved as ready.");
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.ParkScenePath))
                    throw new System.InvalidOperationException("ParkScene is not in Build Settings.");
                ParkFilmRoute.RememberHouseView(roomEye,roomRotation,roomFov);
                UnityEngine.SceneManagement.SceneManager.LoadScene(ParkFilmRoute.ParkScenePath);
                return true;
            }
            catch(System.Exception e)
            {
                ParkFilmRoute.ForgetHouseView();
                cameraRouteNotice="暂时无法进入公园，请检查场景或胶卷记录；原进度已保留。";
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
        // Only the interaction buttons held at entry must be released; WASD is independent.
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
            InputState.Keys.Clear();InputState.Down=false;wasDown=wasRightDown=false;
            Done[ChapterIndex]=Current.Completed;
            Room.Camera.rect=new Rect(0,0,1,1);Room.Camera.cullingMask &= ~(1<<26);
            Room.Camera.ResetAspect();
            InStation=false;ShowHelp=false;awaitRelease=false;
            audioSource.Stop();
        }
        void UpdateHouse()
        {
            if(!houseReady || Room==null || !houseFocused)return;
            RestoreHouseCursor();
            if(safeInspection!=null && safeInspection.BlocksInput){Room.StopMotion();return;}
            if(houseReturnGate){Room.StopMotion();ReleaseHouseReturnInput(houseHandoff.Ready);return;}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                var gallery=InStation?CurrentWorld as PhotoWorld3D:null;
                if(gallery!=null && gallery.CloseParkGallery())return;
                if(!InStation && radioDeck!=null && radioDeck.State==HouseRadioDeck.Phase.Holding){radioDeck.ResetDeck();return;}
                if(InStation)ExitHouseStation();
                else RestoreHouseCursor();
                return;
            }
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(transition>0){AdvanceHouseTransition(dt);return;}
            if(!InStation)
            {
                var controls=PlayerControlSettings.Current;
                Room.WalkSpeed=controls.walkSpeed*controls.houseSpeedMultiplier;Room.LookSensitivity=controls.lookSensitivity;Room.WalkTick(dt);
                if(Input.GetKeyDown(KeyCode.E) && TryInspectStudyEntry())return;
                if(Input.GetMouseButtonDown(0) && Room.AimedStation>=0)TryEnterHouseStation(Room.AimedStation);
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
                Done[0]=Current.Completed;return;
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
            Done[ChapterIndex]=Current.Completed;wasDown=down;wasRightDown=right;
        }
        // House owns cursor policy outside the inspection modal. Focus/Editor unlocks
        // must not become the next saved exploration state.
        void RestoreHouseCursor()
        {
            if(!sharedHouse || !houseReady || Room==null || !houseFocused || !isActiveAndEnabled)return;
            if(safeInspection!=null && safeInspection.Active)return;
            if(ControlState==PlayerControlState.Exploration && (Cursor.lockState!=CursorLockMode.Locked || Cursor.visible))Room.EnableLook(true);
            PlayerInputPolicy.ApplyCursor(ControlState,CurrentWorld!=null && CurrentWorld.WantsMouseLook);
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
                GUI.Label(new Rect(28,60,Screen.width-56,60),cameraRouteNotice,style);
            if(!InStation && transition<=0)
            {
                GUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                string hint=Room.AimedStation>=0?"左键进入："+HouseLayout.Names[Room.AimedStation]+(safeInspection!=null && Room.AimedStation!=5?" · E 安全检视":""):"WASD 行走 · 鼠标环视 · 走近物件后左键进入 / E 检视";
                if(radioDeck!=null)hint=radioDeck.GetHint(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f)))??hint;
                if(safeInspection!=null && Room.AimedStation<0)
                {
                    var item=safeInspection.ForHit(StudyEntry.PickVisible(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f))));
                    if(item!=null)hint="E 安全检视："+item.title;
                }
                if(deskStudy && Room.AimedStation==1)
                    hint=((PhotoWorld3D)worlds[1]).HouseCameraEntryHint+(safeInspection!=null?" · E 安全检视":"");
                if(Time.unscaledTime<cameraRouteNoticeUntil)hint=cameraRouteNotice;
                GUI.Box(new Rect(16,Screen.height-56,600,40),"");GUI.Label(new Rect(28,Screen.height-50,580,34),hint,style);
            }
            else if(InStation)
            {
                if(ChapterIndex==0 && radioDeck!=null){radioDeck.DrawStation(style);if(ShowHelp){GUI.Box(new Rect(20,130,Mathf.Min(850,Screen.width-40),190),"");GUI.Label(new Rect(32,140,Mathf.Min(826,Screen.width-64),170),CurrentWorld.Help,style);}}
                GUI.Label(new Rect(Screen.width-230,12,220,30),"Esc 返回屋内",style);
                if(awaitRelease && transition<=0)GUI.Label(new Rect(20,70,600,40),"松开刚才的交互按键，开始操作",style);
                if(ChapterIndex!=0)
                {
                    GUI.Label(new Rect(20,12,600,35),HouseLayout.Names[ChapterIndex]+" · H 操作说明",style);
                    if(ShowHelp){GUI.Box(new Rect(20,60,Mathf.Min(900,Screen.width-40),230),"");GUI.Label(new Rect(32,72,Mathf.Min(870,Screen.width-64),210),CurrentWorld.Help,style);}
                    if(CurrentWorld.WantsMouseLook)GUI.Label(new Rect(Screen.width*.5f-8,Screen.height*.5f-14,24,28),"·",style);
                    if(deskStudy && ChapterIndex==1 && transition<=0 && !awaitRelease && !ShowHelp)((PhotoWorld3D)CurrentWorld).DrawProgress(style);
                }
            }
        }
    }
}


