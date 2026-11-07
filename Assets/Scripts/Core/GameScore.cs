using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    // Music follows committed gameplay states; it never supplies tape samples or advances a chapter.
    public sealed partial class GameScore : MonoBehaviour
    {
        public static GameScore Instance {get; private set;}
        [SerializeField] GameScoreSettings score;
        [SerializeField] AudioSource[] beds;
        [SerializeField] AudioSource response;
        [SerializeField] int active;
        WBMain house;
        ParkPhotoPrototype park;
        PaintingChapter wardNarrativeOwner;
        [SerializeField] bool paused;
        bool focused=true, finishedObserved, finale, waitingAtDoor;
        float gainScale;

        AudioSource Voice(string label)
        {
            var go=new GameObject(label);
            go.transform.SetParent(transform,false);
            var a=go.AddComponent<AudioSource>();
            a.playOnAwake=false;a.spatialBlend=0;a.volume=0;
            return a;
        }
        void Awake()
        {
            score=Resources.Load<GameScoreSettings>("GameScore");
            if(!score){enabled=false;return;}
            beds=new[]{Voice("Music A"),Voice("Music B")};
            response=Voice("Photography response");
            InitializeLayers();
        }
        void OnEnable()
        {
            Instance=this;SceneManager.sceneLoaded+=SceneLoaded;BindScenes();
        }
        void BindScenes()
        {
            var next=FindObjectOfType<WBMain>();
            if(house!=next){finishedObserved=false;finale=false;waitingAtDoor=false;wardNarrativeOwner=null;}
            house=next;park=FindObjectOfType<ParkPhotoPrototype>();
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode){BindScenes();}
        void OnDisable()
        {
            SceneManager.sceneLoaded-=SceneLoaded;
            if(Instance==this)Instance=null;
            PauseVoices(true);
            wardNarrativeOwner=null;StopWardVoices();
        }
        void OnApplicationFocus(bool value){focused=value;if(!value)PauseVoices(true);}
        void PauseVoices(bool value)
        {
            if(beds==null || paused==value)return;
            paused=value;
            foreach(var a in beds){if(value)a.Pause();else a.UnPause();}
            if(value)response.Pause();else response.UnPause();
            PauseLayers(value);
        }
        public void PhotoAnswered()
        {
            if(!isActiveAndEnabled || park==null || park.Paused || !focused || paused || score.photoResponse==null)return;
            response.volume=score.gain;response.clip=score.photoResponse;response.Play();
        }
        public void BeginWardNarrative(PaintingChapter chapter)
        {
            // Only the live white-to-ward edge can arm the one-shot music onset.
            if(chapter!=null && chapter.Phase==3)wardNarrativeOwner=chapter;
        }
        public void CutWardNarrative(PaintingChapter chapter)
        {
            if(wardNarrativeOwner!=chapter)return;
            wardNarrativeOwner=null;
            // Synchronous stop: no fade, delayed callback, or paper-SFX bus mutation.
            StopWardVoices();
        }
        void Update()
        {
            if(wardNarrativeOwner!=null && (wardNarrativeOwner.Phase!=3 || house==null || !house.InStation || house.Current!=wardNarrativeOwner))
            {wardNarrativeOwner=null;StopWardVoices();}
            PauseVoices(!focused || (!InMainMenu && ((park!=null && park.Paused) || (house!=null && (house.ShowHelp || house.Muted)))));
            if(paused)
            {
                // A cut during pause must not resume an obsolete ward tail later.
                if(wardNarrativeOwner==null && score.ward!=null)
                    StopWardVoices();
                return;
            }
            bool loop;
            gainScale=1;
            var next=Select(out loop);
            var previous=beds[active].clip;
            // Day five, ending and menu share the same looping MUS01.
            // A scene change can keep the same clip, so update its mode separately.
            if(previous==next && next!=null)
            {
                beds[active].loop=loop;
                if(InMainMenu && !beds[active].isPlaying && AudioSettings.dspTime>=scheduledStart)beds[active].Play();
            }
            bool cut=previous==score.ward || next==null;
            if(previous!=next)
            {
                active=1-active;
                var a=beds[active];
                a.Stop();a.clip=next;a.loop=loop;a.volume=0;
                ScheduleArrangement(next,loop);
            }
            var dt=Time.unscaledDeltaTime;
            UpdateLayers(dt);
            float volume=score.gain*(response.isPlaying?.42f:gainScale);
            for(int i=0;i<beds.Length;i++)
            {
                var a=beds[i];bool playing=i==active && next!=null;
                float seconds=playing?(IsRoom(next)?2.5f:.55f):(a.clip==score.ward || cut?.10f:.55f);
                a.volume=score.gain<=0?0:Mathf.MoveTowards(a.volume,playing?volume:0,dt*score.gain/seconds);
                if(!playing && a.volume<=0 && a.clip!=null){a.Stop();a.clip=null;}
            }
            if(park==null && response.isPlaying)response.Stop();
        }
        bool InMainMenu=>SceneManager.GetActiveScene().path=="Assets/Scenes/MainMenuScene.unity";
        bool IsRoom(AudioClip clip)=>System.Array.IndexOf(score.roomSeasons,clip)>=0 || clip==score.springWaiting;
        AudioClip Select(out bool loop)
        {
            loop=true;
            if(InMainMenu)return score.theme;
            // The ending-to-menu load can temporarily have no active house owner.
            // Keep its already-playing theme through that gap; do not stop/reschedule it.
            if(BrushSceneTransition.IsBusy && beds[active].clip==score.theme)return score.theme;
            if(park!=null)return score.parkLoop;
            if(house==null || !house.HouseReady)return null;
            if(house.StoryFinished)
            {
                finale=true;return score.theme;
            }
            finishedObserved=true;finale=false;
            if(house.CurrentDay==StoryDay.SpringDeparture)
            {
                // Same clip and loop through the doorway, white hold and credits.
                return score.theme;
            }
            waitingAtDoor=false;
            var deck=house.radioDeck;
            if(deck!=null && (deck.WorkbenchMode || deck.RecordingMode || (deck.transport!=null && (deck.transport.SourcePlaying || deck.transport.TargetRunning))))return null;
            if(house.DirectDarkroomActive || house.InStation && house.ChapterIndex<=1)return null;
            if(house.InStation && house.Current is WritingChapter w)
            {
                if(w.Phase=="crumple")return null;
                if(w.Phase=="arrival" || w.Phase=="farewell" || w.Completed){loop=false;return score.farewell;}
                gainScale=w.Phase=="tutorial"?.48f:1;
                return score.letterLoop;
            }
            if(house.InStation && house.Current is PaintingChapter album)
            {
                if(album.Phase>=4)return null;
                if(album.Phase>=2)
                {
                    if(album.Phase!=3 || wardNarrativeOwner!=album)return null;
                    loop=false;
                    gainScale=.45f+.45f*Mathf.Clamp01(album.WardRevealHeight/Mathf.Max(.01f,album.NoseHeight));
                    return score.ward;
                }
                return album.Phase==0?score.winterPark:score.winterRoom;
            }
            if(house.safeInspection!=null && house.safeInspection.Active)gainScale=.55f;
            return score.roomSeasons[Mathf.Clamp((int)house.CurrentDay,0,3)];
        }
    }
}
