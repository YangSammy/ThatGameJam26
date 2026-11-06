using UnityEngine;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox
{
    // Single persistent bed: scene changes never restart SFX-001 or stack duplicate loops.
    public sealed class GameAmbience:MonoBehaviour
    {
        public GameAmbienceSettings settings;
        AudioSource bed;
        [SerializeField,HideInInspector] AudioSource[] seasonal=new AudioSource[2];
        int selected=-1,voice;
        bool focused=true,gameScene;
        StoryDay fallbackDay;
        WBMain house;
        BedroomWindowAmbience[] windows=new BedroomWindowAmbience[0];
        bool parkScene;
        public int SeasonIndex=>selected;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            SceneManager.sceneLoaded-=EnsureGameAudio;SceneManager.sceneLoaded+=EnsureGameAudio;
            EnsureGameAudio(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        }
        static void EnsureGameAudio(Scene scene,LoadSceneMode mode)
        {
            if(scene.path!=ParkFilmRoute.HouseScenePath && scene.path!=ParkFilmRoute.ParkScenePath)return;
            if(FindObjectOfType<GameAmbience>()==null)new GameObject("Game Ambience").AddComponent<GameAmbience>();
        }
        AudioSource Make(string name){var g=new GameObject(name);g.transform.SetParent(transform,false);var a=g.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=true;a.spatialBlend=0;a.volume=0;return a;}
        void Awake()
        {
            settings=Resources.Load<GameAmbienceSettings>("GameAmbience");
            if(settings==null){Destroy(gameObject);return;}
            DontDestroyOnLoad(gameObject);if(GetComponent<GameScore>()==null)gameObject.AddComponent<GameScore>();bed=Make("Global bed 001");bed.clip=settings.globalLoop;
            seasonal[0]=Make("Season A");seasonal[1]=Make("Season B");
            BindScene();if(bed.clip!=null)bed.Play();
        }
        void OnEnable()
        {
            SceneManager.sceneLoaded+=Loaded;SceneManager.sceneUnloaded+=Unloaded;BindScene();
            if(bed!=null)bed.UnPause();
            foreach(var a in seasonal)if(a!=null)a.UnPause();
        }
        void Loaded(Scene scene,LoadSceneMode mode){BindScene();}
        void Unloaded(Scene scene){BindScene();}
        void BindScene()
        {
            house=FindObjectOfType<WBMain>();parkScene=FindObjectOfType<ParkPhotoPrototype>()!=null;
            windows=FindObjectsOfType<BedroomWindowAmbience>(true);
            gameScene=house!=null||parkScene;fallbackDay=parkScene?StoryDay.SummerPhoto:StoryDay.SpringTape;
        }
        bool PositionSeasonalVoice(StoryDay day)
        {
            var source=seasonal[voice];
            // Outdoor ambience stays diffuse. Indoors it comes only from the authored window.
            if(parkScene){source.spatialBlend=0;source.dopplerLevel=0;return true;}
            // Day five already owns a distinct melting-eaves loop in DepartureBedroom.
            if(day==StoryDay.SpringDeparture)return false;
            foreach(var window in windows)
                if(window!=null && window.isActiveAndEnabled && window.season==selected)
                {window.Configure(source);return true;}
            return false; // Never leak an origin-based/global seasonal loop while rooms load.
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
            var day=house!=null?house.CurrentDay:fallbackDay;
            SetSeason(day);
            bool seasonalAudible=PositionSeasonalVoice(day);
            bool audible=gameScene&&focused&&(house==null||(!house.Muted&&!house.ShowHelp));
            float step=Time.unscaledDeltaTime/settings.fadeSeconds;
            bed.volume=Mathf.MoveTowards(bed.volume,audible?settings.globalVolume:0,step*settings.globalVolume);
            for(int i=0;i<2;i++)
            {
                var a=seasonal[i];a.volume=Mathf.MoveTowards(a.volume,audible&&seasonalAudible&&i==voice?settings.seasonVolume:0,step*settings.seasonVolume);
                if(i!=voice&&a.volume==0)a.Stop();
            }
        }
        void OnApplicationFocus(bool value){focused=value;}
        // Domain reload disables and re-enables the behaviour without Awake.
        // Keep the existing sources, playback positions and scene subscription.
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneUnloaded-=Unloaded;if(bed!=null)bed.Pause();foreach(var a in seasonal)if(a!=null)a.Pause();}
    }
}
