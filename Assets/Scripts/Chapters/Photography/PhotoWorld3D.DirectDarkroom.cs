using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        public bool DirectDarkroom {get;private set;}
        public bool DirectDarkroomAvailable=>UsesProgress && ParkMode && !parkFilmUnavailable;
        public void SetDirectDarkroom(bool value)
        {
            if(DirectDarkroom==value)return;
            CancelInteraction();SaveProgress();DirectDarkroom=value;
            if(value)DarkroomNotice("LMB: interact / RMB: look / WASD: move");
            else{handLeft.gameObject.SetActive(false);handRight.gameObject.SetActive(false);armLeft.gameObject.SetActive(false);armRight.gameObject.SetActive(false);ClearDarkroomHighlight();}
        }
        public void PresentDirectDarkroom()
        {
            if(!DirectDarkroomAvailable)return;
            Root.SetActive(true);SyncProps();
            if(!DirectDarkroom)
            {handLeft.gameObject.SetActive(false);handRight.gameObject.SetActive(false);armLeft.gameObject.SetActive(false);armRight.gameObject.SetActive(false);}
        }
        public bool ReachableDarkroomObject(Ray ray,out Transform target)
        {
            target=null;if(!DirectDarkroomAvailable)return false;
            string id=Pick(ray);GameObject item;
            if(string.IsNullOrEmpty(id) || DarkroomName(id)==null || !Nodes.TryGetValue(id,out item))return false;
            target=item.transform;return true;
        }
        void AdvancePhotoChemistry(float dt)
        {
            if(!ParkMode){Photo.Tick(dt,new WBInput());return;}
            // A negative rinse is not a completed photograph. Only final saved prints complete the roll.
            if(!Photo.Agitated && Photo.Chemistry<3)return;
            Photo.ProcessingTime=Mathf.Max(0,Photo.ProcessingTime-dt);
            if(Photo.ProcessingTime>0)return;
            Photo.Chemistry++;
            Photo.Phase=Photo.Chemistry>=4?PhotoChapter.FilmPhase.Developed:PhotoChapter.FilmPhase.Tank;
        }
        public void TickDirectDarkroom(float dt)
        {
            if(!DirectDarkroom)return;
            SaveProgress(false);
            if(parkGallery)return;
            var previous=Photo.Phase;
            if(Photo.Phase==PhotoChapter.FilmPhase.Processing)AdvancePhotoChemistry(dt);
            if(darkroomWorkflow>=2 && previous==PhotoChapter.FilmPhase.Processing && Photo.Phase==PhotoChapter.FilmPhase.Developed)BeginPrintWorkflow();
            TickPrintWorkflow(dt);
            if(ManualTank && !Printing && previous==PhotoChapter.FilmPhase.Processing && Photo.Phase==PhotoChapter.FilmPhase.Tank)needsDrain=true;
            if(Photo.Phase!=previous)SaveProgress();
            Eye=View.transform.position;lookRotation=View.transform.rotation;
            if(tankHeld) tankBody.localRotation=homeRotations["tank"]*Quaternion.Euler(Vector3.Scale(printObjects.tankAgitationEuler,new Vector3(Mathf.Sin(Time.unscaledTime*14),0,Mathf.Cos(Time.unscaledTime*11))));
            else if(dragged!="tank")tankBody.localRotation=homeRotations["tank"];
            if(Photo.Phase==PhotoChapter.FilmPhase.Processing && clockDuration>0)clockHand.localRotation=clockHomeRotation*Quaternion.Euler(0,0,-(1-Photo.ProcessingTime/clockDuration)*360);
            if(Photo.Phase==PhotoChapter.FilmPhase.Processing && Photo.Chemistry==3)
            {pour.gameObject.SetActive(true);pour.SetPosition(0,Nodes["water"].transform.position);pour.SetPosition(1,tankLid.position);}
            else if(dragged==null)pour.gameObject.SetActive(false);
            UpdateBody();SyncProps();
        }
    }
}
