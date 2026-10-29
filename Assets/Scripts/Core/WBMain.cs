using UnityEngine;
namespace MemoryWhitebox
{
    [DefaultExecutionOrder(-50)]
    public sealed partial class WBMain : MonoBehaviour
    {
        public static WBMain Instance;
        public static Font UIFont;
        public static bool IsQA;
        public Chapter Current {get;private set;}
        public WorldChapter3D CurrentWorld {get;private set;}
        public int ChapterIndex {get;private set;}
        public bool[] Done=new bool[5];
        public WBInput InputState=new WBInput();
        public bool ShowHelp,Muted;
        public bool InStation {get;private set;}
        public RoomWorld Room {get;private set;}
        public bool IsTransitioning=>transition>0;
        readonly Chapter[] chapters=new Chapter[5];
        readonly WorldChapter3D[] worlds=new WorldChapter3D[5];
        AudioSource audioSource;
        Vector3 roomEye;
        Quaternion roomRotation;
        float roomFov,transition,lookYaw,lookPitch;
        bool wasDown,wasRightDown;
        Ray pointerRay;
        static readonly KeyCode[] keys={KeyCode.W,KeyCode.A,KeyCode.S,KeyCode.D,KeyCode.E,KeyCode.F,KeyCode.J,KeyCode.L,KeyCode.Quote,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow,KeyCode.Space,KeyCode.LeftShift};
        public void Select(int index){EnterHouseStation(index);}
        public void LeaveStation(){ExitHouseStation();}
        public void ChapterComplete(Chapter chapter){int index=System.Array.IndexOf(chapters,chapter);if(index>=0 && CommitStoryCompletion(index))Tone(660,.24f);}
        public static void Tone(float hz,float duration=.12f)
        {
            if(Instance==null || Instance.Muted || IsQA || Instance.audioSource==null)return;
            int n=Mathf.Max(1,(int)(44100*duration));var samples=new float[n];
            for(int i=0;i<n;i++)samples[i]=Mathf.Sin(2*Mathf.PI*hz*i/44100)*Mathf.Sin(Mathf.PI*i/n)*.5f;
            var clip=AudioClip.Create("procedural-memory-sound",n,1,44100,false);clip.SetData(samples,0);
            Instance.audioSource.PlayOneShot(clip);Destroy(clip,duration+1);
        }
        void Update(){UpdateHouse();}
        void OnGUI(){DrawHouse();}
        public void SetHelpVisible(bool visible)
        {
            if(ShowHelp==visible)return;
            var photo=CurrentWorld as PhotoWorld3D;
            if(photo!=null)photo.CancelInteraction();else CurrentWorld?.Release("",pointerRay);
            InputState.Keys.Clear();InputState.Down=false;
            wasDown=Input.GetMouseButton(0);wasRightDown=Input.GetMouseButton(1);
            ShowHelp=visible;SuspendHouseInput();SyncInputContext();
        }
        void OnApplicationFocus(bool focus)
        {
            houseFocused=focus;if(houseReady){SyncInputContext();SuspendHouseInput();}
            if(focus){RestoreHouseCursor();return;}
            PlayerInputPolicy.ApplyCursor(PlayerControlState.Unfocused);
            safeInspection?.Close();
            InputState.Keys.Clear();InputState.Down=false;wasDown=wasRightDown=false;
            var photo=CurrentWorld as PhotoWorld3D;
            if(photo!=null){photo.CancelInteraction();photo.SaveProgress();}
            else if(ChapterIndex==0)radioDeck?.SuspendInput();else CurrentWorld?.Release("",pointerRay);
            radioDeck?.SuspendInput();SaveStableProgress();
        }
        void OnDestroy(){SaveStableProgress();foreach(var world in worlds)world?.Dispose();if(Instance==this)Instance=null;}
    }
}
