using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        SpringTapeRoom springTapeRoom;
        SummerPhotoRoom summerPhotoRoom;
        bool loadingSpringRoom;
        bool summerGallerySeen;
        public DepartureBedroom DepartureRoom {get;private set;}

        IEnumerator OpenDepartureBedroom(bool keepView,Vector3 eye,Quaternion rotation,int debugKey=0)
        {
            loadingSpringRoom=true;
            yield return SceneManager.LoadSceneAsync(SpringTapeRoom.ScenePath,LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath(SpringTapeRoom.ScenePath);
            loadingSpringRoom=false;
            foreach(var root in scene.GetRootGameObjects())
                if(root.TryGetComponent<DepartureBedroom>(out var room)){DepartureRoom=room;break;}
            if(DepartureRoom==null){Debug.LogError("Missing departure doorway in the spring bedroom");SceneManager.UnloadSceneAsync(scene);yield break;}
            var ending=(EndingWorld3D)worlds[4];
            ending.RestoreDoorPose();
            DepartureRoom.Enter(this,ending.Ending,(PaintingWorld3D)worlds[3],keepView,eye,rotation);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(debugKey!=0)DebugEnterChapter(debugKey);
#endif
        }

        NarrativeBedroom narrativeBedroom;
        internal WinterAlbumWorkspace WinterAlbum => narrativeBedroom != null ? narrativeBedroom.winterAlbum : null;
        SafeInspectionSubject narrativeInspection;

        static bool HasNarrativeBedroom(StoryDay day)=>day==StoryDay.AutumnWriting || day==StoryDay.WinterPainting;

        IEnumerator OpenNarrativeBedroom(StoryDay day,int debugKey=0)
        {
            string path=day==StoryDay.AutumnWriting?NarrativeBedroom.AutumnScene:NarrativeBedroom.WinterScene;
            if(!Application.CanStreamedLevelBeLoaded(path)){Debug.LogError("Missing bedroom in Build Settings: "+path);yield break;}
            loadingSpringRoom=true;
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath(path);
            foreach(var root in scene.GetRootGameObjects())
                if(root.TryGetComponent<NarrativeBedroom>(out narrativeBedroom))break;
            loadingSpringRoom=false;
            if(narrativeBedroom==null){Debug.LogError("Missing bedroom chapter entry in "+path);SceneManager.UnloadSceneAsync(scene);yield break;}
            foreach(var entry in StudyEntries.GetComponentsInChildren<StudyEntry>(true))
                if(entry.chapter==narrativeBedroom.chapterEntry.chapter){narrativeInspection=safeInspection?.ForEntry(entry);break;}
            if(narrativeBedroom.writingDesk!=null)((WritingWorld3D)worlds[2]).AttachDesk(narrativeBedroom.writingDesk);
            narrativeBedroom.Enter(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(debugKey!=0)DebugEnterChapter(debugKey);
#endif
        }

        void LeaveNarrativeBedroom()
        {
            if(narrativeBedroom==null)return;
            if(narrativeBedroom.writingDesk!=null)((WritingWorld3D)worlds[2]).DetachDesk();
            narrativeBedroom.Leave(this);narrativeBedroom=null;narrativeInspection=null;
        }

        bool TickNarrativeBedroom(float dt)
        {
            if(narrativeBedroom==null)return false;
            narrativeBedroom.TickDiscovery(dt,Muted);
            Room.WalkTick(dt);
            if(!narrativeBedroom.IsEntry(StudyEntry.Pick(Room.Camera)))return true;
            if(Input.GetKeyDown(KeyCode.E) && narrativeInspection!=null)
            {safeInspection.feedback.mute=Muted;safeInspection.Open(narrativeInspection);}
            else if(Input.GetMouseButtonDown(0))TryEnterHouseStation(narrativeBedroom.chapterEntry.chapter);
            return true;
        }


        void StartSeasonBedroom(bool keepParkView)
        {
            if(gameObject.scene.path!=ParkFilmRoute.HouseScenePath || !story.Available)return;
            if(CurrentDay==StoryDay.SpringDeparture){StartCoroutine(OpenDepartureBedroom(false,Vector3.zero,Quaternion.identity));return;}
            if(HasNarrativeBedroom(CurrentDay)){StartCoroutine(OpenNarrativeBedroom(CurrentDay));return;}
            bool summer=CurrentDay==StoryDay.SummerPhoto;
            if(!summer && CurrentDay!=StoryDay.SpringTape)return;
            if(!summer)
            {
                try{var film=ParkFilmStore.LoadActive();summer=film!=null && film.state!=ParkFilmState.Developed && film.HasPhotographyProgress;}
                catch(System.Exception ex){Debug.LogWarning("Bedroom deferred: "+ex.Message);return;}
            }
            StartCoroutine(OpenSeasonBedroom(summer,keepParkView));
        }

        IEnumerator OpenSeasonBedroom(bool summer,bool keepView,int debugKey=0)
        {
            var path=summer?SummerPhotoRoom.ScenePath:SpringTapeRoom.ScenePath;
            if(!Application.CanStreamedLevelBeLoaded(path))
            {Debug.LogError("Missing bedroom in Build Settings: "+path);yield break;}
            loadingSpringRoom=true;
            yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath(path);
            loadingSpringRoom=false;
            if(summer)
            {
                foreach(var root in scene.GetRootGameObjects())
                    if(root.TryGetComponent<SummerPhotoRoom>(out summerPhotoRoom))break;
                if(summerPhotoRoom==null){Debug.LogError("Missing summer room bindings.");SceneManager.UnloadSceneAsync(scene);yield break;}
                summerPhotoRoom.Enter(this,keepView);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(debugKey!=0)DebugEnterChapter(debugKey);
#endif
                yield break;
            }
            foreach(var root in scene.GetRootGameObjects())
                if(root.TryGetComponent<SpringTapeRoom>(out springTapeRoom))break;
            if(springTapeRoom==null)
            {
                Debug.LogError("Spring bedroom has no tape layout.");
                SceneManager.UnloadSceneAsync(scene);
                yield break;
            }
            springTapeRoom.Enter(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(debugKey!=0)DebugEnterChapter(debugKey);
#endif
        }

        void FinishSpringBedroomIfReady()
        {
            if(springTapeRoom!=null && radioDeck!=null && CurrentDay!=StoryDay.SpringTape && !InStation &&
                transition<=0 && !radioDeck.WorkbenchMode && !radioDeck.RecordingMode && !radioDeck.SuccessPopupOpen)
            {
                LeaveSpringTapeRoom();
                if(CurrentDay==StoryDay.SummerPhoto)StartCoroutine(OpenSeasonBedroom(true,false));
            }
            if(narrativeBedroom!=null && !InStation && transition<=0 && narrativeBedroom.chapterEntry.chapter!=(int)CurrentDay)
            {
                var eye=Room.Camera.transform.position;var rotation=Room.Camera.transform.rotation;
                LeaveNarrativeBedroom();
                if(CurrentDay==StoryDay.SpringDeparture)StartCoroutine(OpenDepartureBedroom(true,eye,rotation));
                else if(HasNarrativeBedroom(CurrentDay))StartCoroutine(OpenNarrativeBedroom(CurrentDay));
            }
            if(summerPhotoRoom==null)return;
            summerPhotoRoom.Refresh(this);
            if(DarkroomWorld.ParkGalleryOpen)summerGallerySeen=true;
            if(summerGallerySeen && !DarkroomWorld.ParkGalleryOpen && CurrentDay>=StoryDay.AutumnWriting)
            {
                summerGallerySeen=false;
                LeaveSummerPhotoRoom();
                if(HasNarrativeBedroom(CurrentDay))StartCoroutine(OpenNarrativeBedroom(CurrentDay));
            }
        }

        void LeaveSpringTapeRoom()
        {
            if(springTapeRoom==null)return;
            springTapeRoom.Leave(this);springTapeRoom=null;
        }

        void LeaveSummerPhotoRoom()
        {
            if(summerPhotoRoom==null)return;
            summerGallerySeen=false;darkroomReachTarget=null;
            summerPhotoRoom.Leave(this);summerPhotoRoom=null;
        }

        bool TickSummerPhotography(float dt)
        {
            if(summerPhotoRoom==null)return false;
            summerPhotoRoom.TickCurtain(this,dt);
            if(!DarkroomWorld.HasActiveGesture && !DarkroomWorld.ParkGalleryOpen && Input.GetMouseButtonDown(0))
            {
                var ray=DirectDarkroomActive?Room.Camera.ScreenPointToRay(Input.mousePosition):
                    Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f));
                if(summerPhotoRoom.Click(this,ray))return true;
            }
            if(!TickContinuousDarkroom(dt))Room.WalkTick(dt);
            return true;
        }

        public void ViewSummerPrint(Transform frame,int number)
        {
            if(DarkroomWorld.DevelopedRollId==null)return;
            darkroomReachTarget=frame;DarkroomWorld.SetDirectDarkroom(true);Room.EnableLook(false);
            DarkroomWorld.OpenDevelopedFrame(number);summerGallerySeen=true;SyncInputContext();
        }

        public Vector3 FilmCarryTarget(Vector3 eye,Vector3 hand)=>summerPhotoRoom!=null?summerPhotoRoom.CarryThroughDoor(eye,hand):hand;
    }
}

