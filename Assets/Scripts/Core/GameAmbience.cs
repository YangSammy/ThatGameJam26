using UnityEngine;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox
{
    // Single persistent bed: scene changes never restart SFX-001 or stack duplicate loops.
    public sealed class GameAmbience:MonoBehaviour
    {
        public GameAmbienceSettings settings;
        AudioSource bed;
        readonly AudioSource[] seasonal=new AudioSource[2];
        int selected=-1,voice;
        bool focused=true,gameScene;
        StoryDay fallbackDay;
        WBMain house;
        public int SeasonIndex=>selected;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {if(FindObjectOfType<GameAmbience>()==null)new GameObject("Game Ambience").AddComponent<GameAmbience>();}
        AudioSource Make(string name){var g=new GameObject(name);g.transform.SetParent(transform,false);var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=true;a.spatialBlend=0;a.volume=0;return a;}
        void Awake()
        {
            settings=Resources.Load<GameAmbienceSettings>("GameAmbience");
            if(settings==null){Destroy(gameObject);return;}
            DontDestroyOnLoad(gameObject);bed=Make("Global bed 001");bed.clip=settings.globalLoop;
            seasonal[0]=Make("Season A");seasonal[1]=Make("Season B");
            SceneManager.sceneLoaded+=Loaded;BindScene();if(bed.clip!=null)bed.Play();
        }
        void Loaded(Scene scene,LoadSceneMode mode){BindScene();}
        void BindScene()
        {
            house=FindObjectOfType<WBMain>();bool park=FindObjectOfType<ParkPhotoPrototype>()!=null;
            gameScene=house!=null||park;fallbackDay=park?StoryDay.SummerPhoto:StoryDay.SpringTape;
        }
        public void SetSeason(StoryDay day)
        {
            int index=day==StoryDay.SpringDeparture?0:Mathf.Clamp((int)day,0,3);
            if(index==selected)return;selected=index;voice=1-voice;
            var a=seasonal[voice];a.Stop();a.clip=settings.seasons[index];a.volume=0;if(a.clip!=null)a.Play();
        }
        void Update()
        {
            if(settings==null)return;
            SetSeason(house!=null?house.CurrentDay:fallbackDay);
            bool audible=gameScene&&focused&&(house==null||(!house.Muted&&!house.ShowHelp));
            float step=Time.unscaledDeltaTime/settings.fadeSeconds;
            bed.volume=Mathf.MoveTowards(bed.volume,audible?settings.globalVolume:0,step*settings.globalVolume);
            for(int i=0;i<2;i++)
            {
                var a=seasonal[i];a.volume=Mathf.MoveTowards(a.volume,audible&&i==voice?settings.seasonVolume:0,step*settings.seasonVolume);
                if(i!=voice&&a.volume==0)a.Stop();
            }
        }
        void OnApplicationFocus(bool value){focused=value;}
        void OnDisable(){if(bed!=null)bed.Stop();foreach(var a in seasonal)if(a!=null)a.Stop();}
        void OnDestroy(){SceneManager.sceneLoaded-=Loaded;}
    }
}
