using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        Texture2D parkReferenceTexture;
        ParkFilmRoll parkFilm;
        ParkFilmRoll loadingFilm;
        bool parkLoading;
        int darkroomWorkflow;
        bool tankSealed,needsDrain;
        bool ManualTank=>ParkMode && darkroomWorkflow>=1;
        ParkPrintStage printStage;
        float printRemaining;
        bool printProcessing,printHeld;
        bool Printing=>ParkMode && darkroomWorkflow>=2 && printStage!=ParkPrintStage.None && printStage!=ParkPrintStage.Complete;
        public ParkPrintStage CurrentPrintStage=>printStage;
        string PrintHint=>printProcessing?(printStage==ParkPrintStage.Exposing?"Hold LMB  Expose":"Processing"):
            printStage==ParkPrintStage.NegativeDrying?"LMB tank  Hang negative":
            printStage==ParkPrintStage.ContactReady?"Hold LMB on paper  Expose":
            printStage==ParkPrintStage.PaperWash?"LMB tap  Wash paper":
            printStage==ParkPrintStage.PaperDrying?"LMB paper  Hang print":"Drag bottle "+((int)printStage-(int)ParkPrintStage.PaperDeveloper+1)+" to paper";
        bool parkFilmUnavailable,parkGallery;
        int parkFrame=1;
        string frameNotice="";
        public bool ParkGalleryOpen=>parkGallery;
        public string HouseCameraEntryHint=>parkFilmUnavailable?"Film unavailable":ParkMode?
            (ParkPrintsReady?"LMB  View prints":"LMB  Continue developing"):"LMB  Load camera";
        bool ParkMode=>parkFilm!=null;
        bool ParkPrintsReady=>ParkMode && parkFilm.state==ParkFilmState.Developed;
        const string ParkHelp="Hold rewind + Scroll / Bag + Scroll / Seal tank / Pour > Agitate > Drain / Rinse";

        const string LoadingHelp="Drag film > Thread leader > Close back > Advance / Go to park";
        // Loading and development belong to the park roll. The old single-shot
        // photography save remains untouched and cannot bypass camera preparation.
        bool LoadParkFilm()
        {
            try
            {
                var active=ParkFilmStore.LoadActive();
                if(active==null || active.state==ParkFilmState.Loaded)
                {
                    parkLoading=true;loadingFilm=active;
                    var c=active!=null?active.cameraLoading:null;
                    Photo.Phase=c!=null?c.phase:PhotoChapter.FilmPhase.Empty;
                    Photo.FilmThreaded=c!=null && c.threaded;Photo.Settings.ISO=c!=null?c.iso:100;
                    if(active!=null && active.frames.Count>0){Photo.Phase=PhotoChapter.FilmPhase.Ready;Photo.FilmThreaded=true;}
                    Photo.RestoreCompleted(false);return true;
                }
                parkFilm=active;
                var d=active.darkroom ?? (active.version>=3 && active.state!=ParkFilmState.Developed?
                    new ParkDarkroomProgress{workflowVersion=2,phase=PhotoChapter.FilmPhase.Exposed}:new ParkDarkroomProgress());
                if(d.workflowVersion==1 && active.state!=ParkFilmState.Developed)d.workflowVersion=2;
                darkroomWorkflow=d.workflowVersion;tankSealed=d.tankSealed;needsDrain=d.needsDrain;
                printStage=d.printStage;printRemaining=d.printRemaining;printProcessing=d.printProcessing;
                if(printStage==ParkPrintStage.Exposing){printStage=ParkPrintStage.ContactReady;printRemaining=0;printProcessing=false;}
                RewindTurns=d.rewindTurns;RewoundBackOpen=d.backOpen;
                if(active.state==ParkFilmState.Developed){d.phase=PhotoChapter.FilmPhase.Developed;d.chemistry=4;}
                Photo.Phase=d.phase;Photo.Chemistry=d.chemistry;Photo.ProcessingTime=d.remaining;
                Photo.BagTurns=d.bagTurns;Photo.Agitated=d.agitated;clockDuration=d.clockDuration;
                Photo.Captured=null;Photo.Fogged=false;Photo.FilmThreaded=false;
                Photo.RestoreCompleted(ParkPrintsReady);parkFrame=d.selectedFrame;
                ReturnToCheckpoint();Eye=World(walkPosition);Look=Eye+Quaternion.Euler(pitch,yaw,0)*Vector3.forward;
                if(ParkPrintsReady)LoadParkFrame();
                return true;
            }
            catch(Exception e)
            {
                parkFilmUnavailable=true;saveBlocked=true;
                saveNotice="Film unavailable. Records kept; restore files and re-enter.";
                Debug.LogWarning("Darkroom film load: "+e.Message);return true;
            }
        }

        void SaveParkLoading()
        {
            if(loadingFilm==null && Photo.Phase==PhotoChapter.FilmPhase.Empty)return;
            var c=new ParkCameraLoading{phase=Photo.Phase,threaded=Photo.FilmThreaded,iso=Photo.Settings.ISO};
            string state=JsonUtility.ToJson(c);if(state==lastSavedState)return;
            try
            {
                loadingFilm=ParkFilmStore.SaveCameraLoading(loadingFilm!=null?loadingFilm.id:null,c);
                lastSavedState=state;saveNotice="";
            }
            catch(Exception e)
            {
                saveNotice="Loading not saved. Check storage and retry.";
                nextSave=Time.unscaledTime+5;Debug.LogWarning("Camera loading save: "+e.Message);
            }
        }
        void DrawParkLoading(GUIStyle style)
        {

            InteractionUI.Label(new Rect(28,Screen.height-100,Screen.width-56,42),ProgressHint,style);
            if(InteractionUI.Button(ProgressActionRect,"Go to park"))
            {CancelInteraction();SaveProgress();Host.TryStartParkPhotography();}
        }

        void SaveParkFilm()
        {
            var d=new ParkDarkroomProgress{phase=Photo.Phase,chemistry=Photo.Chemistry,
                remaining=Mathf.Max(0,Photo.ProcessingTime),bagTurns=Photo.BagTurns,
                agitated=Photo.Agitated,clockDuration=clockDuration,selectedFrame=parkFrame,
                workflowVersion=darkroomWorkflow,tankSealed=tankSealed,needsDrain=needsDrain,
                rewindTurns=RewindTurns,backOpen=RewoundBackOpen,
                printStage=printStage==ParkPrintStage.Exposing?ParkPrintStage.ContactReady:printStage,
                printRemaining=printStage==ParkPrintStage.Exposing?0:printRemaining,
                printProcessing=printStage!=ParkPrintStage.Exposing && printProcessing};
            string state=JsonUtility.ToJson(d);if(state==lastSavedState)return;
            try
            {
                bool wasReady=ParkPrintsReady;
                parkFilm=ParkFilmStore.SaveDevelopment(parkFilm.id,d);
                lastSavedState=state;saveNotice="";
                if(ParkPrintsReady)
                {
                    Photo.RestoreCompleted(true);
                    if(!wasReady){CancelInteraction();LoadParkFrame();parkGallery=true;Host.SuspendHouseInput();}
                }
            }
            catch(Exception e)
            {
                saveNotice="Film not saved. Check storage; retrying.";
                Debug.LogWarning("Darkroom film save: "+e.Message);nextSave=Time.unscaledTime+5;
            }
        }

        void BeginPrintWorkflow()
        {
            // The model's legacy rinse finishes the negative, not the paper print.
            Photo.Phase=PhotoChapter.FilmPhase.Tank;Photo.Chemistry=3;Photo.ProcessingTime=0;
            Photo.RestoreCompleted(false);needsDrain=false;
            printStage=ParkPrintStage.NegativeDrying;printRemaining=3;printProcessing=false;
            SaveProgress();
        }
        bool PressPrintWorkflow(string id,Ray ray)
        {
            if(!Printing)return false;
            if(printProcessing)return true;
            if(printStage==ParkPrintStage.NegativeDrying && (id=="tank" || id=="tank-lid"))printProcessing=true;
            else if(printStage==ParkPrintStage.ContactReady && id=="developed-photo")
            {printStage=ParkPrintStage.Exposing;printRemaining=2;printProcessing=true;printHeld=true;}
            else if(printStage>=ParkPrintStage.PaperDeveloper && printStage<=ParkPrintStage.PaperFixer && id.StartsWith("chemical-"))BeginDrag(id,ray);
            else if(printStage==ParkPrintStage.PaperWash && id=="water")
            {printRemaining=2;printProcessing=true;}
            else if(printStage==ParkPrintStage.PaperDrying && id=="developed-photo")
            {printRemaining=3;printProcessing=true;}
            SaveProgress();return true;
        }
        void CancelPrintExposure()
        {
            printHeld=false;
            if(printStage!=ParkPrintStage.Exposing)return;
            printStage=ParkPrintStage.ContactReady;printRemaining=0;printProcessing=false;
        }
        void PourPrintChemical(int index)
        {
            if(!Printing || printProcessing || index!=(int)printStage-(int)ParkPrintStage.PaperDeveloper || index<0 || index>2)return;
            printRemaining=index==0?3:index==1?1:2;printProcessing=true;
        }
        void TickPrintWorkflow(float dt)
        {
            if(!Printing || !printProcessing || (printStage==ParkPrintStage.Exposing && !printHeld))return;
            printRemaining=Mathf.Max(0,printRemaining-Mathf.Max(0,dt));
            clockHand.localRotation=Quaternion.Euler(0,0,-printRemaining*120);
            if(printRemaining>0)return;
            printProcessing=false;
            if(printStage==ParkPrintStage.NegativeDrying)printStage=ParkPrintStage.ContactReady;
            else if(printStage==ParkPrintStage.Exposing){printStage=ParkPrintStage.PaperDeveloper;printHeld=false;}
            else if(printStage==ParkPrintStage.PaperDrying)
            {
                printStage=ParkPrintStage.Complete;Photo.Phase=PhotoChapter.FilmPhase.Developed;Photo.Chemistry=4;
            }
            else printStage=(ParkPrintStage)((int)printStage+1);
            SaveProgress();SyncProps();
        }
        void LoadParkFrame()
        {
            if(parkReferenceTexture!=null){ReleaseObject(parkReferenceTexture);parkReferenceTexture=null;}
            if(parkFilm!=null && parkFilm.state==ParkFilmState.Developed && parkFrame<=parkFilm.frames.Count && !string.IsNullOrEmpty(parkFilm.frames[parkFrame-1].taskId))
            {
                Texture2D reference=null;
                try{reference=new Texture2D(2,2);if(!reference.LoadImage(ParkFilmStore.ReadTaskReference(parkFilm.id,parkFilm.frames[parkFrame-1].taskId)))throw new InvalidOperationException("Reference image invalid.");parkReferenceTexture=reference;}
                catch(Exception e){if(reference!=null)ReleaseObject(reference);Debug.LogWarning("Reference comparison unavailable: "+e.Message);}
            }
            if(exposedTexture!=null){ReleaseObject(exposedTexture);exposedTexture=null;}
            byte[] bytes;
            if(!ParkFilmStore.TryReadDevelopedFrame(parkFilm.id,parkFrame,out bytes,out frameNotice))return;
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                if(!texture.LoadImage(bytes))throw new InvalidOperationException("Invalid image data.");
                texture.name="Developed park frame "+parkFrame;exposedTexture=texture;frameNotice="";
            }
            catch(Exception){ReleaseObject(texture);frameNotice="Print unavailable. Negative kept.";}
            SyncProps();
        }
        void OpenParkGallery()
        {
            if(!ParkPrintsReady)return;
            CancelInteraction();Photo.MeterVisible=false;LoadParkFrame();parkGallery=true;Host.SuspendHouseInput();
        }
        public bool CloseParkGallery()
        {
            if(!parkGallery)return false;
            parkGallery=false;SaveProgress();Host.SuspendHouseInput();return true;
        }
        void TurnParkFrame(int delta)
        {
            int next=Mathf.Clamp(parkFrame+delta,1,parkFilm.Capacity);
            if(next==parkFrame)return;
            parkFrame=next;LoadParkFrame();SaveProgress();
        }
        void DrawParkFilm(GUIStyle style)
        {
            if(!parkGallery)
            {

                string hint=ProgressHint;
                if(ParkPrintsReady && saveNotice=="")hint="Roll: " + parkFilm.Capacity + " frames developed  /  Esc  Back";
                InteractionUI.Label(new Rect(28,Screen.height-100,Screen.width-56,42),"Film  " + parkFilm.Capacity + " / " + parkFilm.Capacity + " frames\n"+hint,style);
                if(ParkPrintsReady && InteractionUI.Button(ProgressActionRect,"View prints"))OpenParkGallery();
                return;
            }

            var bounds=new Rect(36,91,Screen.width-72,Mathf.Max(1,Screen.height-184));
            // ScaleToFit preserves the actual viewfinder aspect ratio; no second blur
            // or quality scoring is applied to the immutable park exposures.
            if(exposedTexture!=null)
            {
                if(parkReferenceTexture!=null)
                {
                    float half=(bounds.width-24)*.5f;
                    GUI.DrawTexture(new Rect(bounds.x,bounds.y,half,bounds.height),parkReferenceTexture,ScaleMode.ScaleToFit);
                    GUI.DrawTexture(new Rect(bounds.x+half+24,bounds.y,half,bounds.height),exposedTexture,ScaleMode.ScaleToFit);
                }
                else GUI.DrawTexture(bounds,exposedTexture,ScaleMode.ScaleToFit);
            }
            else InteractionUI.Label(bounds,frameNotice,style);
            InteractionUI.Label(new Rect(36,61,Screen.width-72,30),"Print  "+parkFrame+" / " + parkFilm.Capacity + " frames",style);
            bool enabled=GUI.enabled;
            GUI.enabled=parkFrame>1;if(InteractionUI.Button(new Rect(36,Screen.height-70,140,36),"< Previous"))TurnParkFrame(-1);
            GUI.enabled=parkFrame<parkFilm.Capacity;if(InteractionUI.Button(new Rect(188,Screen.height-70,140,36),"Next >"))TurnParkFrame(1);
            GUI.enabled=enabled;
            if(InteractionUI.Button(new Rect(Screen.width-204,Screen.height-70,168,36),"Close  /  Esc"))CloseParkGallery();
        }
    }
}
