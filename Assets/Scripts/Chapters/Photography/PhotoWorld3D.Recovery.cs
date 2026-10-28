using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        string savedImage,lastSavedState,saveNotice="";
        bool saveBlocked;
        float nextSave;
        bool UsesProgress => SharedRoom && Host.deskStudy;
        bool InDarkroomPhase => Photo.Phase>=PhotoChapter.FilmPhase.DarkBag && Photo.Phase<=PhotoChapter.FilmPhase.Processing;
        public override string Pick(Ray ray)
        {
            if(!UsesProgress)return base.Pick(ray);
            var hits=Physics.RaycastAll(ray,2.5f,(1<<8)|(1<<25)|1,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits) {
                var node=hit.collider.GetComponent<WorldHotspot>();
                if(node!=null && node.Owner==this)return node.Id;
                // Room navigation volumes and curtains are also physical occluders.
                if(hit.collider.gameObject.layer==25 || hit.collider.name.StartsWith("Walk blocker"))return "";
            }
            return "";
        }
        void ReturnToCheckpoint()
        {
            ReturnToBench();
            if(UsesProgress && (InDarkroomPhase || ParkMode)){walkPosition=new Vector3(1.85f,1.65f,2.9f)-Origin;yaw=90;pitch=28;}
        }
        void LoadProgress()
        {
            if(!UsesProgress)return;
            if(LoadParkFilm())return;
            var d=PhotoProgressStore.Load(out saveNotice,out saveBlocked);if(d==null)return;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(d.settings),Photo.Settings);
            Photo.Captured=d.captured;Photo.Phase=d.phase;Photo.Chemistry=d.chemistry;Photo.Attempts=d.attempts;
            Photo.ProcessingTime=d.remaining;Photo.BagTurns=d.bagTurns;Photo.FilmThreaded=d.threaded;Photo.Fogged=d.fogged;Photo.Agitated=d.agitated;
            Photo.RestoreCompleted(d.completed);RewindTurns=d.rewindTurns;RewoundBackOpen=d.backOpen;clockDuration=d.clockDuration;savedImage=d.image;
            if(!string.IsNullOrEmpty(savedImage)){exposedTexture=new Texture2D(2,2);exposedTexture.LoadImage(Convert.FromBase64String(savedImage));}
            Photo.Assess();ReturnToCheckpoint();Eye=World(walkPosition);Look=Eye+Quaternion.Euler(pitch,yaw,0)*Vector3.forward;
        }
        public void SaveProgress(bool force=true)
        {
            if(!UsesProgress || saveBlocked || PhotoProgressStore.SuppressWrites)return;
            if(!force && Time.unscaledTime<nextSave)return;nextSave=Time.unscaledTime+.5f;
            if(ParkMode){SaveParkFilm();return;}
            if(parkLoading){SaveParkLoading();return;}
            var d=new PhotoProgress{phase=Photo.Phase,settings=Photo.Settings.Copy(),captured=Photo.Captured,chemistry=Photo.Chemistry,attempts=Photo.Attempts,remaining=Mathf.Max(0,Photo.ProcessingTime),bagTurns=Photo.BagTurns,rewindTurns=RewindTurns,clockDuration=clockDuration,threaded=Photo.FilmThreaded,fogged=Photo.Fogged,agitated=Photo.Agitated,backOpen=RewoundBackOpen,completed=Photo.Completed};
            string state=JsonUtility.ToJson(d);if(state==lastSavedState)return;d.image=savedImage;
            try {PhotoProgressStore.Save(d);lastSavedState=state;if(saveNotice.StartsWith("Photo not saved"))saveNotice="";}
            catch(Exception e){saveNotice="Photo not saved. Check storage; retry on exit.";Debug.LogWarning("Photo save failed: "+e.Message);nextSave=Time.unscaledTime+5;}
        }
        public string ProgressHint {
            get {
                if(saveNotice!="")return saveNotice;
                if(Photo.Completed)return "Photo saved  /  Esc  Back";
                switch(Photo.Phase){
                    case PhotoChapter.FilmPhase.Empty:return "Drag film into the left chamber";
                    case PhotoChapter.FilmPhase.LoadedOpen:return Photo.FilmThreaded?"Close the camera back":"Drag leader to the take-up spool";
                    case PhotoChapter.FilmPhase.Closed:return "Click the advance lever";
                    case PhotoChapter.FilmPhase.Ready:return parkLoading?"Film ready. Head to the park.":"Click eyepiece  /  LMB  Shoot";
                    case PhotoChapter.FilmPhase.Exposed:return "Hold rewind + Scroll  "+Mathf.RoundToInt(RewindTurns*100)+"%  /  Then open back";
                    case PhotoChapter.FilmPhase.Rewound:return !RewoundBackOpen?"Open back and remove film":"Open curtain; drag film into dark bag";
                    case PhotoChapter.FilmPhase.DarkBag:return "Hold bag + Scroll  "+Mathf.RoundToInt(Photo.BagTurns*100)+"%";
                    case PhotoChapter.FilmPhase.Tank:return ManualTank && !tankSealed?"LMB  Seal tank":ManualTank && needsDrain?"Drag tank to sink  /  Release to drain":Photo.Chemistry==3?"Click tap to rinse":"Drag bottle "+(Photo.Chemistry+1)+" to tank";
                    case PhotoChapter.FilmPhase.Processing:return !Photo.Agitated&&Photo.Chemistry<3?"LMB drag tank to agitate":"Processing  "+Mathf.Max(0,Photo.ProcessingTime).ToString("0.0")+" s  /  Pauses on exit";
                    default:return "View photo or reload film";
                }
            }
        }
        public bool CanRetake => !ParkMode && !parkFilmUnavailable && !Photo.Completed && Photo.Captured!=null && (Photo.Phase==PhotoChapter.FilmPhase.Exposed || Photo.Phase==PhotoChapter.FilmPhase.Rewound || Photo.Phase==PhotoChapter.FilmPhase.Developed);
        public float ProgressUIHeight => ParkMode || parkLoading?108:CanRetake && Photo.FailureReason!=""?116:60;
        public Rect ProgressActionRect => new Rect(28,Screen.height-51,220,30);
        public bool PointerOverUI {
            get {
                var point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
                return parkGallery || parkFilmUnavailable ||
                    ((parkLoading && Photo.Phase==PhotoChapter.FilmPhase.Ready || ParkPrintsReady) && ProgressActionRect.Contains(point)) ||
                    (CanRetake && Photo.FailureReason!="" && new Rect(Mathf.Min(760,Screen.width-180),Screen.height-58,155,32).Contains(point)) ||
                    (Photo.InViewfinder && !View.pixelRect.Contains(Input.mousePosition));
            }
        }
        void UpdateFinderViewport()
        {
            if(View==null)return;
            View.ResetAspect();
            View.rect=new Rect(0,0,1,1);
            if(!Photo.InViewfinder)return;
            var full=View.pixelRect;float aspect=full.width/Mathf.Max(1,full.height);
            if(aspect>1.8f){float width=1.8f/aspect;View.rect=new Rect((1-width)/2,0,width,1);}
            else {float height=aspect/1.8f;View.rect=new Rect(0,(1-height)/2,1,height);}
            View.aspect=1.8f;
        }
        public void RetakePhoto(){if(!CanRetake)return;CancelInteraction();ResetFilm();}
        public void DrawProgress(GUIStyle style)
        {
            if(ParkMode){DrawParkFilm(style);return;}
            if(parkLoading){DrawParkLoading(style);return;}
            if(Photo.InViewfinder){var r=View.pixelRect;var old=GUI.color;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,r.x,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.xMax,0,Screen.width-r.xMax,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height-r.yMax),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-r.y,Screen.width,r.y),Texture2D.whiteTexture);GUI.color=old;}

            InteractionUI.Label(new Rect(28,Screen.height-ProgressUIHeight+6,Mathf.Min(900,Screen.width-55),42),ProgressHint,style);
            if(CanRetake && Photo.FailureReason!="") {
                InteractionUI.Label(new Rect(28,Screen.height-70,Mathf.Min(720,Screen.width-220),55),Photo.FailureReason,style);
                if(InteractionUI.Button(new Rect(Mathf.Min(760,Screen.width-180),Screen.height-58,155,32),"Reload film"))RetakePhoto();
            }
        }
        static void ApplyPhotoBlur(Texture2D texture,float focus,float motion)
        {
            int radius=Mathf.Clamp(Mathf.CeilToInt(focus*texture.width/730f),0,16);
            int trail=Mathf.Clamp(Mathf.CeilToInt(motion*texture.width/730f),0,24);
            if(radius==0&&trail==0)return;
            var source=texture.GetPixels();var horizontal=new Color[source.Length];var output=new Color[source.Length];int width=texture.width,height=texture.height;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){Color sum=Color.clear;int count=0;for(int dx=-radius-trail;dx<=radius;dx++){sum+=source[y*width+Mathf.Clamp(x+dx,0,width-1)];count++;}horizontal[y*width+x]=sum/count;}
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){Color sum=Color.clear;for(int dy=-radius;dy<=radius;dy++)sum+=horizontal[Mathf.Clamp(y+dy,0,height-1)*width+x];output[y*width+x]=sum/(radius*2+1);}
            texture.SetPixels(output);texture.Apply();
        }
    }
}
