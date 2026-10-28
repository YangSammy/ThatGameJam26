using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    /// <summary>Isolated, additive v3 entry point. The existing House/Park prototypes are untouched.</summary>
    public sealed class WBMain : MonoBehaviour
    {
        public static WBMain Instance;
        public static Font UIFont;
        public static bool IsQA;
        public Chapter Current { get; private set; }
        public WorldChapter3D CurrentWorld { get; private set; }
        public int ChapterIndex { get; private set; }
        public int Day { get; private set; }
        public bool[] Done = new bool[5];
        public WBInput InputState = new WBInput();
        public bool ShowHelp, Muted;
        public bool InStation { get; private set; }
        public RoomWorld Room { get; private set; }
        public bool IsTransitioning => fade > 0 || completing;
        public bool GameEnded { get; private set; }
        readonly Chapter[] chapters = new Chapter[5];
        readonly WorldChapter3D[] worlds = new WorldChapter3D[4];
        static readonly KeyCode[] keys = {KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.E,KeyCode.F,KeyCode.J,KeyCode.L,KeyCode.Quote,KeyCode.Space,KeyCode.LeftShift,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow};
        AudioSource audioSource;
        Vector3 hubPosition;
        Quaternion hubRotation;
        float hubFov, fade;
        bool wasDown, wasRightDown, waitRelease, completing, fresh;
        bool cursorWasLooking,focused=true,qaFlowPassed=true;
        Ray pointerRay;
        string SaveFolder => Path.Combine(Application.persistentDataPath,"WhiteboxV3");

        [Serializable] sealed class Checkpoint { public int schema=3; public int day; public bool[] done; public bool ended; }

        void Awake()
        {
            Instance=this;
            Application.targetFrameRate=60;
            QualitySettings.vSyncCount=0;
            string[] args=Environment.GetCommandLineArgs();
            IsQA=Array.IndexOf(args,"--v3-qa-capture")>=0;
            fresh=IsQA||Array.IndexOf(args,"--v3-fresh")>=0;
            UIFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.volume=.12f;
            Camera camera=Camera.main;
            if(camera==null){var go=new GameObject("Camera_Player");go.tag="MainCamera";camera=go.AddComponent<Camera>();go.AddComponent<AudioListener>();}
            Room=new RoomWorld();Room.Create(camera);
            LoadCheckpoint();Room.SetDay(Day);
            RememberHub();Room.EnableLook(!IsQA);
            if(IsQA)StartCoroutine(CapturePass());
        }

        void RememberHub(){hubPosition=Room.Camera.transform.position;hubRotation=Room.Camera.transform.rotation;hubFov=Room.Camera.fieldOfView;}

        public void Select(int index)
        {
            if(IsTransitioning&&!IsQA)return;
            index=Mathf.Clamp(index,0,4);
            if(index==4)
            {
                if(InStation)LeaveStation();
                ChapterIndex=4;Day=4;Room.SetDay(4);Room.OpenDoor();
                if(chapters[4]==null)chapters[4]=new EndingChapter();
                Current=chapters[4];((EndingChapter)Current).Open();return;
            }
            if(!InStation)RememberHub();
            CurrentWorld?.SuspendInput();CurrentWorld?.Deactivate();
            ChapterIndex=index;
            if(chapters[index]==null)
                switch(index){case 0:chapters[index]=new TapeChapter();break;case 1:chapters[index]=new PhotoChapter();break;case 2:chapters[index]=new WritingChapter();break;case 3:chapters[index]=new PaintingChapter();break;}
            Current=chapters[index];
            if(worlds[index]==null)
                switch(index){case 0:worlds[index]=new TapeWorld3D(this,(TapeChapter)Current);break;case 1:worlds[index]=new PhotoWorld3D(this,(PhotoChapter)Current);break;case 2:worlds[index]=new WritingWorld3D(this,(WritingChapter)Current);break;case 3:worlds[index]=new PaintingWorld3D(this,(PaintingChapter)Current);break;}
            CurrentWorld=worlds[index];InStation=true;Room.EnableLook(false);Room.SetVisible(false);
            CurrentWorld.Activate();CurrentWorld.ApplyPose();
            InputState.Keys.Clear();InputState.Down=false;InputState.RightDown=false;InputState.Pointer=new Vector2(640,380);InputState.PointerDelta=Vector2.zero;
            wasDown=wasRightDown=false;waitRelease=!IsQA;cursorWasLooking=CurrentWorld.WantsMouseLook;SetStationCursor();
            fade=IsQA?0:1;
        }

        public void LeaveStation()
        {
            CurrentWorld?.SuspendInput();CurrentWorld?.Deactivate();CurrentWorld=null;InStation=false;
            InputState.Keys.Clear();InputState.Down=InputState.RightDown=false;wasDown=wasRightDown=false;
            Room.SetVisible(true);Room.Camera.rect=new Rect(0,0,1,1);
            Room.Camera.transform.SetPositionAndRotation(hubPosition,hubRotation);Room.Camera.fieldOfView=hubFov;
            Room.EnableLook(!IsQA);fade=IsQA?0:.65f;
        }

        public void ChapterComplete()
        {
            if(completing||GameEnded)return;
            Done[ChapterIndex]=true;
            if(ChapterIndex==1)PersistPhotos(CurrentWorld?.CompletionImages);
            if(ChapterIndex==4)
            {
                GameEnded=true;Room.EnableLook(false);SaveCheckpoint();return;
            }
            SaveCheckpoint(Mathf.Min(4,ChapterIndex+1));
            if(!IsQA)StartCoroutine(AdvanceDay());
        }

        IEnumerator AdvanceDay()
        {
            completing=true;
            yield return new WaitForSecondsRealtime(ChapterIndex==3?.12f:2.1f);
            for(float t=0;t<.6f;t+=Time.unscaledDeltaTime){fade=Mathf.Clamp01(t/.6f);yield return null;}
            int next=Mathf.Min(4,ChapterIndex+1);
            LeaveStation();Day=next;Room.SetDay(Day);SaveCheckpoint();
            fade=1;completing=false;
        }

        void ResetCurrent()
        {
            if(!InStation||completing)return;
            int index=ChapterIndex;CurrentWorld?.Deactivate();worlds[index]?.Dispose();worlds[index]=null;chapters[index]=null;Done[index]=false;fade=0;Select(index);
        }

        void Update()
        {
            if(IsQA)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(!completing)fade=Mathf.Max(0,fade-dt*1.5f);
            if(Input.GetKeyDown(KeyCode.H))
            {
                ShowHelp=!ShowHelp;
                if(ShowHelp){SuspendInput();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
                else if(InStation)SetStationCursor();
                if(!InStation)Room.EnableLook(!ShowHelp);AudioListener.pause=Muted||ShowHelp||!focused;
            }
            if(Input.GetKeyDown(KeyCode.M)){Muted=!Muted;AudioListener.pause=Muted||ShowHelp||!focused;}
            if(Input.GetKeyDown(KeyCode.Escape)&&!completing)
            {
                if(InStation)LeaveStation();else Room.EnableLook(Cursor.lockState!=CursorLockMode.Locked);
                return;
            }
            // Developer shortcuts are deliberately unavailable during the normal wordless flow.
            if(ShowHelp&&!completing)
            {
                for(int i=0;i<5;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){fade=0;Select(i);return;}
                if(Input.GetKeyDown(KeyCode.R))ResetCurrent();
                if(Input.GetKeyDown(KeyCode.Backspace)&&Input.GetKey(KeyCode.LeftControl)){StartCoroutine(RestartGame());return;}
            }
            if(GameEnded||IsTransitioning||ShowHelp||!focused)return;
            if(!InStation)
            {
                Room.WalkTick(dt);
                if(Room.AimedStation>=0&&(Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(0)))Select(Room.AimedStation);
                if(Day==4&&Room.ThroughDoor)
                {
                    ChapterIndex=4;if(chapters[4]==null)chapters[4]=new EndingChapter();Current=chapters[4];
                    ((EndingChapter)Current).CrossThreshold();
                }
                return;
            }
            if(waitRelease)
            {
                if(!Input.GetMouseButton(0)&&!Input.GetMouseButton(1)&&!Input.GetKey(KeyCode.E)&&!Input.GetKey(KeyCode.S)&&!Input.GetKey(KeyCode.F)&&!Input.GetKey(KeyCode.J)&&!Input.GetKey(KeyCode.L)&&!Input.GetKey(KeyCode.Quote))waitRelease=false;
                return;
            }
            InputState.Keys.Clear();foreach(var k in keys)if(Input.GetKey(k))InputState.Keys.Add(k);
            InputState.Pointer=new Vector2(Input.mousePosition.x/Screen.width*1280,(1-Input.mousePosition.y/Screen.height)*760);
            InputState.PointerDelta=new Vector2(Input.GetAxisRaw("Mouse X")*11,-Input.GetAxisRaw("Mouse Y")*11);
            InputState.Down=Input.GetMouseButton(0);InputState.RightDown=Input.GetMouseButton(1);InputState.Time=Time.unscaledTime;
            CurrentWorld.Tick(dt,InputState);CurrentWorld.ApplyPose();
            if(cursorWasLooking!=CurrentWorld.WantsMouseLook){cursorWasLooking=CurrentWorld.WantsMouseLook;SetStationCursor();}
            Vector2 pointer=CurrentWorld.WantsMouseLook?new Vector2(Screen.width*.5f,Screen.height*.5f):(Vector2)Input.mousePosition;
            pointerRay=CurrentWorld.PointerRay(pointer);string id=CurrentWorld.Pick(pointerRay);CurrentWorld.HoverId=id;
            if(InputState.Down&&!wasDown)CurrentWorld.Press(id,pointerRay,0);
            if(InputState.RightDown&&!wasRightDown)CurrentWorld.Press(id,pointerRay,1);
            CurrentWorld.Move(pointerRay);
            if(!InputState.Down&&wasDown||!InputState.RightDown&&wasRightDown)CurrentWorld.Release(id,pointerRay);
            if(Mathf.Abs(Input.mouseScrollDelta.y)>.01f)CurrentWorld.Scroll(id,Input.mouseScrollDelta.y,pointerRay);
            wasDown=InputState.Down;wasRightDown=InputState.RightDown;
        }

        IEnumerator RestartGame()
        {
            if(InStation)LeaveStation();
            foreach(var w in worlds)w?.Dispose();Array.Clear(worlds,0,worlds.Length);Array.Clear(chapters,0,chapters.Length);
            Done=new bool[5];Day=ChapterIndex=0;GameEnded=false;Room.Destroy();
            yield return null;
            Room=new RoomWorld();Room.Create(Camera.main);Room.SetDay(0);RememberHub();Room.EnableLook(true);ShowHelp=false;AudioListener.pause=Muted||!focused;SaveCheckpoint();
        }

        void SetStationCursor(){Cursor.lockState=CurrentWorld.WantsMouseLook?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!CurrentWorld.WantsMouseLook;}
        void SuspendInput()
        {
            InputState.Keys.Clear();InputState.Down=InputState.RightDown=false;wasDown=wasRightDown=false;waitRelease=true;
            CurrentWorld?.SuspendInput();
        }
        void OnApplicationFocus(bool value){focused=value;AudioListener.pause=Muted||ShowHelp||!focused;if(!focused)SuspendInput();}

        void OnGUI()
        {
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/760f,1));
            Color pale=new Color(.96f,.92f,.82f);
            if(!GameEnded&&!ShowHelp)
            {
                if(InStation)CurrentWorld?.DrawOverlay();
                else
                {
                    G.Ring(new Vector2(640,380),Room.AimedStation>=0?7:2.5f,1,pale,1);
                    if(Room.AimedStation>=0)G.Icon(Room.AimedStation,new Vector2(640,420),15,pale);
                }
            }
            if(ShowHelp)
            {
                G.Box(new Rect(75,40,1130,265),new Color(.07f,.09f,.09f,.96f));
                string description=InStation?CurrentWorld.Help:"WASD行走，鼠标环视；对准正在发光的物品，鼠标左键或E进入。依次完成春磁带、夏摄影、秋写信、冬绘画，最后走出发光的门。";
                G.Text("开发帮助（H关闭，正常游戏不显示文字）\n"+description+"\nEsc 回卧室 / 释放鼠标；M静音；帮助打开时1–5调试关卡，R重置当前关，Ctrl+Backspace从第一天重来。\n自动存档保存已完成的天数；未完成关卡重新启动后从本关开始。",new Rect(100,58,1080,230),18,pale);
            }
            if(fade>0)G.Box(new Rect(0,0,1280,760),new Color(1,1,1,Mathf.Clamp01(fade)));
            if(GameEnded)G.Box(new Rect(0,0,1280,760),Color.white);
        }

        void LoadCheckpoint()
        {
            if(fresh)return;
            try
            {
                string path=Path.Combine(SaveFolder,"progress.json");if(!File.Exists(path))return;
                Checkpoint c=JsonUtility.FromJson<Checkpoint>(File.ReadAllText(path));if(c==null||c.schema!=3)return;
                Day=Mathf.Clamp(c.day,0,4);if(c.done!=null&&c.done.Length==5)Done=c.done;
                // A finished save reopens the thawed room, allowing the ending to be visited again.
                if(Done[1])
                {
                    var images=new Texture2D[3];
                    for(int i=0;i<3;i++){string p=Path.Combine(SaveFolder,"photo-"+i+".png");if(File.Exists(p)){images[i]=new Texture2D(2,2);images[i].LoadImage(File.ReadAllBytes(p));}}
                    Room.SetPhotos(images);
                }
            }
            catch(Exception e){Debug.LogWarning("V3 checkpoint could not be read: "+e.Message);Day=0;Done=new bool[5];}
        }
        void SaveCheckpoint(int resumeDay=-1)
        {
            if(IsQA)return;
            try{Directory.CreateDirectory(SaveFolder);File.WriteAllText(Path.Combine(SaveFolder,"progress.json"),JsonUtility.ToJson(new Checkpoint{day=resumeDay>=0?resumeDay:Day,done=Done,ended=GameEnded},true));}
            catch(Exception e){Debug.LogWarning("V3 checkpoint could not be saved: "+e.Message);}
        }
        void PersistPhotos(Texture2D[] images)
        {
            Room.SetPhotos(images);if(IsQA||images==null)return;
            Directory.CreateDirectory(SaveFolder);
            for(int i=0;i<Mathf.Min(3,images.Length);i++)if(images[i]!=null)File.WriteAllBytes(Path.Combine(SaveFolder,"photo-"+i+".png"),images[i].EncodeToPNG());
        }
        public static void Tone(float hz,float duration=.12f)
        {
            if(Instance==null||Instance.Muted||IsQA)return;
            int n=Mathf.Max(1,(int)(22050*duration));var samples=new float[n];
            for(int i=0;i<n;i++)samples[i]=Mathf.Sin(2*Mathf.PI*hz*i/22050f)*Mathf.Sin(Mathf.PI*i/n)*.35f;
            var clip=AudioClip.Create("V3-mechanical-feedback",n,1,22050,false);clip.SetData(samples,0);Instance.audioSource.PlayOneShot(clip);Destroy(clip,duration+1);
        }

        IEnumerator CapturePass()
        {
            string folder=Path.Combine(Application.dataPath,"..","QA-V3");Directory.CreateDirectory(folder);
            string previousFailure=Path.Combine(folder,"FAILURE.txt");if(File.Exists(previousFailure))File.Delete(previousFailure);
            yield return new WaitForSecondsRealtime(.8f);
            for(int season=0;season<4;season++)
            {
                Room.ApplySeason(season);yield return new WaitForEndOfFrame();WhiteboxV3Validation.CaptureCamera(Path.Combine(folder,"hub-season-"+season+".png"),Room.Camera);yield return null;
            }
            Room.ApplySeason(0);
            for(int i=0;i<4;i++)
            {
                Select(i);
                for(int f=0;f<8;f++){CurrentWorld.Tick(.02f,InputState);CurrentWorld.ApplyPose();yield return null;}
                yield return new WaitForEndOfFrame();WhiteboxV3Validation.Capture(Path.Combine(folder,"chapter-"+(i+1)+".png"),CurrentWorld);yield return null;
                LeaveStation();
            }
            string failure=null;
            try{WhiteboxV3Validation.Run(this,folder);}
            catch(Exception e){failure=e.ToString();File.WriteAllText(Path.Combine(folder,"FAILURE.txt"),failure);Debug.LogException(e);}
            if(failure==null)yield return StartCoroutine(FlowChecks(folder));
            yield return new WaitForSecondsRealtime(.4f);Application.Quit(failure==null&&qaFlowPassed?0:2);
        }
        sealed class CompletionProbe : Chapter {public void Trigger(){Finish();}public override void Draw(){}}
        [Serializable] sealed class FlowReport {public bool passed;public int completedDays;public bool ended;public string error;}
        IEnumerator FlowChecks(string folder)
        {
            string error=null;
            for(int i=0;i<4;i++)
            {
                fade=0;Select(i);var probe=new CompletionProbe();Current=probe;probe.Trigger();
                if(!Done[i]){error="Chapter completion event did not mark day "+i;break;}
                yield return StartCoroutine(AdvanceDay());fade=0;
                if(Day!=i+1||InStation){error="Transition failed after day "+i;break;}
            }
            if(error==null)
            {
                if(Room.Season!=0)error="Final day is not thawed spring";
                Select(4);
                if(!Room.DoorOpen)error="Final door did not open";
                Room.Camera.transform.position=new Vector3(0,1.62f,-3.5f);
                if(!Room.ThroughDoor)error="Final threshold not detected";
                ((EndingChapter)Current).CrossThreshold();
                if(!GameEnded||!Done[4])error="Final chapter completion was not routed";
            }
            File.WriteAllText(Path.Combine(folder,"flow.json"),JsonUtility.ToJson(new FlowReport{passed=error==null,completedDays=Day+1,ended=GameEnded,error=error},true));
            qaFlowPassed=error==null;
            if(error!=null){File.WriteAllText(Path.Combine(folder,"FAILURE.txt"),error);Debug.LogError(error);}
        }
        void OnDestroy(){foreach(var world in worlds)world?.Dispose();if(Instance==this)Instance=null;}
    }
}
