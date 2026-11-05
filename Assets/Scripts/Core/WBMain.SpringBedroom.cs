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
            => ChangeBedroom(SpringTapeRoom.ScenePath,()=>LoadDepartureBedroom(keepView,eye,rotation),debugKey);

        IEnumerator LoadDepartureBedroom(bool keepView,Vector3 eye,Quaternion rotation)
        {
            if(!Application.CanStreamedLevelBeLoaded(SpringTapeRoom.ScenePath))throw new System.InvalidOperationException("Missing spring bedroom in Build Settings.");
            using(bedroomTrace?.Measure("scene-load"))yield return SceneManager.LoadSceneAsync(SpringTapeRoom.ScenePath,LoadSceneMode.Additive);
            bedroomCover.ShowLoading("Preparing objects");
            using(bedroomTrace?.Measure("departure-bind-and-camera"))
            {
                var scene=SceneManager.GetSceneByPath(SpringTapeRoom.ScenePath);
                foreach(var root in scene.GetRootGameObjects())
                    if(root.TryGetComponent<DepartureBedroom>(out var room)){DepartureRoom=room;break;}
                if(DepartureRoom==null)throw new System.InvalidOperationException("Missing departure doorway in the spring bedroom");
                var ending=(EndingWorld3D)worlds[4];
                ending.RestoreDoorPose();
                DepartureRoom.Enter(this,ending.Ending,(PaintingWorld3D)worlds[3],keepView,eye,rotation);
            }
        }

        NarrativeBedroom narrativeBedroom;
        internal WinterAlbumWorkspace WinterAlbum => narrativeBedroom != null ? narrativeBedroom.winterAlbum : null;
        SafeInspectionSubject narrativeInspection;

        static bool HasNarrativeBedroom(StoryDay day)=>day==StoryDay.AutumnWriting || day==StoryDay.WinterPainting;

        IEnumerator OpenNarrativeBedroom(StoryDay day,int debugKey=0)
            => ChangeBedroom(day==StoryDay.AutumnWriting?NarrativeBedroom.AutumnScene:NarrativeBedroom.WinterScene,()=>LoadNarrativeBedroom(day),debugKey);

        IEnumerator LoadNarrativeBedroom(StoryDay day)
        {
            string path=day==StoryDay.AutumnWriting?NarrativeBedroom.AutumnScene:NarrativeBedroom.WinterScene;
            if(!Application.CanStreamedLevelBeLoaded(path))throw new System.InvalidOperationException("Missing bedroom in Build Settings: "+path);
            using(bedroomTrace?.Measure("scene-load"))yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            bedroomCover.ShowLoading("Preparing objects");
            using(bedroomTrace?.Measure("narrative-bind-and-camera"))
            {
                var scene=SceneManager.GetSceneByPath(path);
                foreach(var root in scene.GetRootGameObjects())
                    if(root.TryGetComponent<NarrativeBedroom>(out narrativeBedroom))break;
                if(narrativeBedroom==null)throw new System.InvalidOperationException("Missing bedroom chapter entry in "+path);
                foreach(var entry in StudyEntries.GetComponentsInChildren<StudyEntry>(true))
                    if(entry.chapter==narrativeBedroom.chapterEntry.chapter){narrativeInspection=safeInspection?.ForEntry(entry);break;}
                if(narrativeBedroom.writingDesk!=null)((WritingWorld3D)worlds[2]).AttachDesk(narrativeBedroom.writingDesk);
                narrativeBedroom.Enter(this);
            }
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
            if(gameObject.scene.path!=ParkFilmRoute.HouseScenePath)return;
            if(!story.Available){FailBedroomTransition("Story unavailable: "+story.Error);return;}
            if(CurrentDay==StoryDay.SpringDeparture){StartCoroutine(OpenDepartureBedroom(false,Vector3.zero,Quaternion.identity));return;}
            if(HasNarrativeBedroom(CurrentDay)){StartCoroutine(OpenNarrativeBedroom(CurrentDay));return;}
            bool summer=CurrentDay==StoryDay.SummerPhoto;
            if(!summer && CurrentDay!=StoryDay.SpringTape){FailBedroomTransition("Unknown story day: "+CurrentDay);return;}
            // A legacy film can outlive the story that created it. Route by story day
            // or an explicit return from the park, independently of film recovery.
            StartCoroutine(OpenSeasonBedroom(summer || keepParkView,keepParkView));
        }

        IEnumerator OpenSeasonBedroom(bool summer,bool keepView,int debugKey=0)
            => ChangeBedroom(summer?SummerPhotoRoom.ScenePath:SpringTapeRoom.ScenePath,()=>LoadSeasonBedroom(summer,keepView),debugKey);

        IEnumerator LoadSeasonBedroom(bool summer,bool keepView)
        {
            var path=summer?SummerPhotoRoom.ScenePath:SpringTapeRoom.ScenePath;
            if(!Application.CanStreamedLevelBeLoaded(path))
                throw new System.InvalidOperationException("Missing bedroom in Build Settings: "+path);
            using(bedroomTrace?.Measure("scene-load"))
                if(!SceneManager.GetSceneByPath(path).isLoaded)yield return SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
            bedroomCover.ShowLoading("Preparing objects");
            using(bedroomTrace?.Measure("season-bind-and-camera"))
            {
                var scene=SceneManager.GetSceneByPath(path);
                if(summer)
                {
                    foreach(var root in scene.GetRootGameObjects())
                        if(root.TryGetComponent<SummerPhotoRoom>(out summerPhotoRoom))break;
                    if(summerPhotoRoom==null)throw new System.InvalidOperationException("Missing summer room bindings.");
                    summerPhotoRoom.Enter(this,keepView);
                    yield break;
                }
                foreach(var root in scene.GetRootGameObjects())
                    if(root.TryGetComponent<SpringTapeRoom>(out springTapeRoom))break;
                if(springTapeRoom==null)
                    throw new System.InvalidOperationException("Spring bedroom has no tape layout.");
                springTapeRoom.Enter(this);
                summerPreloadRequested=false;
            }
        }

        void FinishSpringBedroomIfReady()
        {
            if(springTapeRoom!=null && radioDeck!=null && CurrentDay!=StoryDay.SpringTape && !InStation &&
                transition<=0 && !radioDeck.WorkbenchMode && !radioDeck.RecordingMode && !radioDeck.SuccessPopupOpen)
            {
                if(CurrentDay==StoryDay.SummerPhoto)StartCoroutine(OpenSeasonBedroom(true,false));
                return;
            }
            if(narrativeBedroom!=null && !InStation && transition<=0 && narrativeBedroom.chapterEntry.chapter!=(int)CurrentDay)
            {
                var eye=Room.Camera.transform.position;var rotation=Room.Camera.transform.rotation;
                if(CurrentDay==StoryDay.SpringDeparture)StartCoroutine(OpenDepartureBedroom(true,eye,rotation));
                else if(HasNarrativeBedroom(CurrentDay))StartCoroutine(OpenNarrativeBedroom(CurrentDay));
                return;
            }
            if(summerPhotoRoom==null)return;
            summerPhotoRoom.Refresh(this);
            if(DarkroomWorld.ParkGalleryOpen)summerGallerySeen=true;
            if(summerGallerySeen && !DarkroomWorld.ParkGalleryOpen && CurrentDay>=StoryDay.AutumnWriting)
            {
                summerGallerySeen=false;
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

        internal Transform RoomHoverVisual(Collider hit,Ray ray)
        {
            if(DepartureRoom!=null)
                return !StoryFinished && !DepartureDoorOpen && DepartureRoom.HitHandle(ray)?DepartureRoom.handle.transform:null;
            if(!hit)return null;
            if(summerPhotoRoom!=null && hit==summerPhotoRoom.curtainHit)return summerPhotoRoom.curtain;
            if(radioDeck!=null && radioDeck.StationActive && radioDeck.IsEntryHit(hit))
            {
                if(radioDeck.dualDeck && radioDeck.dualDeck.Command(hit)!=null)return hit.transform;
                var source=radioDeck.InspectionSource(ray);
                if(source==radioDeck.transform && radioDeck.dualDeck)return radioDeck.dualDeck.transform;
                if(source)return source;
            }
            var inspection=safeInspection?.ForHit(hit);
            if(inspection!=null)return inspection.source;
            var entry=hit.GetComponentInParent<StudyEntry>();
            if(entry!=null)return CanEnterDay(entry.chapter)?entry.transform:null;
            // The seasonal photo frames have their actionable collider on the visible frame.
            if(summerPhotoRoom!=null)foreach(var frame in summerPhotoRoom.frames)
                if(frame && frame.transform==hit.transform)return frame.transform;
            return null;
        }

        public Vector3 FilmCarryTarget(Vector3 eye,Vector3 hand)=>summerPhotoRoom!=null?summerPhotoRoom.CarryThroughDoor(eye,hand):hand;
    }
}

