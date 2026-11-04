using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    // Music follows committed gameplay states; it never supplies tape samples or advances a chapter.
    public sealed class GameScore : MonoBehaviour
    {
        public static GameScore Instance {get; private set;}
        [SerializeField] GameScoreSettings score;
        [SerializeField] AudioSource[] beds;
        [SerializeField] AudioSource response;
        [SerializeField] int active;
        WBMain house;
        ParkPhotoPrototype park;
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
        }
        void OnEnable()
        {
            Instance=this;SceneManager.sceneLoaded+=SceneLoaded;BindScenes();
        }
        void BindScenes()
        {
            var next=FindObjectOfType<WBMain>();
            if(house!=next){finishedObserved=false;finale=false;waitingAtDoor=false;}
            house=next;park=FindObjectOfType<ParkPhotoPrototype>();
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode){BindScenes();}
        void OnDisable()
        {
            SceneManager.sceneLoaded-=SceneLoaded;
            if(Instance==this)Instance=null;
            PauseVoices(true);
        }
        void OnApplicationFocus(bool value){focused=value;if(!value)PauseVoices(true);}
        void PauseVoices(bool value)
        {
            if(beds==null || paused==value)return;
            paused=value;
            foreach(var a in beds){if(value)a.Pause();else a.UnPause();}
            if(value)response.Pause();else response.UnPause();
        }
        public void PhotoAnswered()
        {
            if(!isActiveAndEnabled || park==null || park.Paused || !focused || paused || score.photoResponse==null)return;
            response.volume=score.gain;response.clip=score.photoResponse;response.Play();
        }
        void Update()
        {
            PauseVoices(!focused || (park!=null && park.Paused) || (house!=null && (house.ShowHelp || house.Muted)));
            if(paused)return;
            bool loop;
            gainScale=1;
            var next=Select(out loop);
            var previous=beds[active].clip;
            bool cut=previous==score.ward || next==null;
            if(previous!=next)
            {
                active=1-active;
                var a=beds[active];
                a.Stop();a.clip=next;a.loop=loop;a.volume=0;
                if(next!=null)a.Play();
            }
            var dt=Time.unscaledDeltaTime;
            float volume=score.gain*(response.isPlaying?.42f:gainScale);
            for(int i=0;i<beds.Length;i++)
            {
                var a=beds[i];bool playing=i==active && next!=null;
                float seconds=playing?(IsRoom(next)?2.5f:.55f):(a.clip==score.ward || cut?.10f:.55f);
                a.volume=Mathf.MoveTowards(a.volume,playing?volume:0,dt*Mathf.Max(.001f,score.gain)/seconds);
                if(!playing && a.volume<=0 && a.clip!=null){a.Stop();a.clip=null;}
            }
            if(park==null && response.isPlaying)response.Stop();
        }
        bool IsRoom(AudioClip clip)=>System.Array.IndexOf(score.roomSeasons,clip)>=0 || clip==score.springWaiting;
        AudioClip Select(out bool loop)
        {
            loop=true;
            if(park!=null)return score.parkLoop;
            if(house==null || !house.HouseReady)return null;
            if(house.StoryFinished)
            {
                // Only score a live crossing. A cold load of an already-finished save stays quiet.
                if(!finishedObserved)return null;
                loop=false;
                if(!finale && beds[active].clip==score.springDoor && beds[active].isPlaying)return score.springDoor;
                finale=true;return score.theme;
            }
            finishedObserved=true;finale=false;
            if(house.CurrentDay==StoryDay.SpringDeparture)
            {
                if(house.DepartureRoom==null)return null;
                if(!house.DepartureDoorOpen){waitingAtDoor=true;return score.springWaiting;}
                loop=false;
                return waitingAtDoor?score.springDoor:null;
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
