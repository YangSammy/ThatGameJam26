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
            try {PhotoProgressStore.Save(d);lastSavedState=state;if(saveNotice.StartsWith("摄影进度暂未保存"))saveNotice="";}
            catch(Exception e){saveNotice="摄影进度暂未保存，请检查磁盘空间；退出关卡时会重试";Debug.LogWarning("Photo save failed: "+e.Message);nextSave=Time.unscaledTime+5;}
        }
        public string ProgressHint {
            get {
                if(saveNotice!="")return saveNotice;
                if(Photo.Completed)return "照片已完成并保存，可以查看成片或 Esc 返回书房";
                switch(Photo.Phase){
                    case PhotoChapter.FilmPhase.Empty:return "将胶卷拖入相机左仓";
                    case PhotoChapter.FilmPhase.LoadedOpen:return Photo.FilmThreaded?"片头已固定，合上后盖":"将片头拖到右侧收片轴";
                    case PhotoChapter.FilmPhase.Closed:return "拨动过片杆";
                    case PhotoChapter.FilmPhase.Ready:return parkLoading?"装卷已完成，点击下方按钮前往公园拍摄。":"点击目镜拿起相机；对照参考照片调整构图，左键拍摄";
                    case PhotoChapter.FilmPhase.Exposed:return "按住倒片钮并滚轮回卷（"+Mathf.RoundToInt(RewindTurns*100)+"%），再打开后盖";
                    case PhotoChapter.FilmPhase.Rewound:return !RewoundBackOpen?"倒片完成，打开相机后盖取出胶卷":"先打开右侧帘子，再将胶卷带进暗室并拖入黑袋";
                    case PhotoChapter.FilmPhase.DarkBag:return "按住暗袋并滚轮装片（"+Mathf.RoundToInt(Photo.BagTurns*100)+"%）";
                    case PhotoChapter.FilmPhase.Tank:return Photo.Chemistry==3?"点击水龙头水洗":"将 "+(Photo.Chemistry+1)+" 号药瓶拖到罐口";
                    case PhotoChapter.FilmPhase.Processing:return !Photo.Agitated&&Photo.Chemistry<3?"按住冲洗罐，轻轻拖动摇晃":"冲洗中，剩余 "+Mathf.Max(0,Photo.ProcessingTime).ToString("0.0")+" 秒；离开时暂停";
                    default:return "查看成片，或重新装卷拍摄";
                }
            }
        }
        public bool CanRetake => !ParkMode && !parkFilmUnavailable && !Photo.Completed && Photo.Captured!=null && (Photo.Phase==PhotoChapter.FilmPhase.Exposed || Photo.Phase==PhotoChapter.FilmPhase.Rewound || Photo.Phase==PhotoChapter.FilmPhase.Developed);
        public float ProgressUIHeight => ParkMode || parkLoading?108:CanRetake && Photo.FailureReason!=""?116:60;
        public bool PointerOverUI => parkGallery || parkFilmUnavailable || Input.mousePosition.y<ProgressUIHeight || (Photo.InViewfinder && !View.pixelRect.Contains(Input.mousePosition));
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
            GUI.Box(new Rect(16,Screen.height-ProgressUIHeight,Mathf.Min(930,Screen.width-32),ProgressUIHeight-16),"");
            GUI.Label(new Rect(28,Screen.height-ProgressUIHeight+6,Mathf.Min(900,Screen.width-55),42),ProgressHint,style);
            if(CanRetake && Photo.FailureReason!="") {
                GUI.Label(new Rect(28,Screen.height-70,Mathf.Min(720,Screen.width-220),55),Photo.FailureReason,style);
                if(GUI.Button(new Rect(Mathf.Min(760,Screen.width-180),Screen.height-58,155,32),"重新装卷拍摄"))RetakePhoto();
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
