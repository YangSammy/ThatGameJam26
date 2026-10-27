using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        ParkFilmRoll parkFilm;
        ParkFilmRoll loadingFilm;
        bool parkLoading;
        bool parkFilmUnavailable,parkGallery;
        int parkFrame=1;
        string frameNotice="";
        public bool ParkGalleryOpen=>parkGallery;
        public string HouseCameraEntryHint=>parkFilmUnavailable?"胶卷记录暂不可用":ParkMode?
            (ParkPrintsReady?"左键进入暗室查看成片":"左键进入暗室继续冲洗"):"左键操作相机 · 装卷后前往公园";
        bool ParkMode=>parkFilm!=null;
        bool ParkPrintsReady=>ParkMode && parkFilm.state==ParkFilmState.Developed;
        const string ParkHelp="公园胶卷共 8 张，已经封存并放入暗袋。按住暗袋并滚轮装片；依次把 1、2、3 号药瓶拖到罐口，每次按住罐子拖动摇晃，再等计时结束；最后点击水龙头。整卷处理完成后点击“逐张查看成片”，用按钮或左右方向键翻页。Esc 先收起照片，再按 Esc 返回书房。离开会保存进度并暂停计时。";

        const string LoadingHelp="将胶卷拖入相机左仓 → 将片头拖到右侧收片轴 → 点击后盖合上 → 点击过片杆。装卷完成后，点击屏幕下方“前往公园拍摄”。中途 Esc 返回书房，下次从已保存的装卷步骤继续；公园已曝光的胶卷无需重新装载。";
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
                var d=active.darkroom ?? new ParkDarkroomProgress();
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
                saveNotice="公园胶卷暂时无法读取，原记录已保留；请恢复胶卷文件后重新进入场景。";
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
                saveNotice="装卷进度暂未保存，暂不能进入公园；请检查磁盘空间后重试。";
                nextSave=Time.unscaledTime+5;Debug.LogWarning("Camera loading save: "+e.Message);
            }
        }
        void DrawParkLoading(GUIStyle style)
        {
            GUI.Box(new Rect(16,Screen.height-108,Screen.width-32,92),"");
            GUI.Label(new Rect(28,Screen.height-100,Screen.width-56,42),ProgressHint,style);
            if(Photo.Phase==PhotoChapter.FilmPhase.Ready &&
               GUI.Button(new Rect(28,Screen.height-51,260,28),"装卷完成 · 前往公园拍摄"))
            {CancelInteraction();SaveProgress();Host.TryStartParkPhotography();}
        }

        void SaveParkFilm()
        {
            var d=new ParkDarkroomProgress{phase=Photo.Phase,chemistry=Photo.Chemistry,
                remaining=Mathf.Max(0,Photo.ProcessingTime),bagTurns=Photo.BagTurns,
                agitated=Photo.Agitated,clockDuration=clockDuration,selectedFrame=parkFrame};
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
                saveNotice="胶卷进度暂未保存，请检查磁盘空间；系统会重试，成片须保存成功后才能查看。";
                Debug.LogWarning("Darkroom film save: "+e.Message);nextSave=Time.unscaledTime+5;
            }
        }

        void LoadParkFrame()
        {
            if(exposedTexture!=null){ReleaseObject(exposedTexture);exposedTexture=null;}
            byte[] bytes;
            if(!ParkFilmStore.TryReadDevelopedFrame(parkFilm.id,parkFrame,out bytes,out frameNotice))return;
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                if(!texture.LoadImage(bytes))throw new InvalidOperationException("Invalid image data.");
                texture.name="Developed park frame "+parkFrame;exposedTexture=texture;frameNotice="";
            }
            catch(Exception){ReleaseObject(texture);frameNotice="这一张成片暂时无法读取，原底片已保留。";}
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
            int next=Mathf.Clamp(parkFrame+delta,1,ParkFilmRoll.FrameCapacity);
            if(next==parkFrame)return;
            parkFrame=next;LoadParkFrame();SaveProgress();
        }
        void DrawParkFilm(GUIStyle style)
        {
            if(!parkGallery)
            {
                GUI.Box(new Rect(16,Screen.height-108,Screen.width-32,92),"");
                string hint=ProgressHint;
                if(ParkPrintsReady && saveNotice=="")hint="整卷 8 张已显影并保存，可以逐张查看；Esc 返回书房。";
                GUI.Label(new Rect(28,Screen.height-100,Screen.width-56,42),"公园胶卷 · 8 / 8 张\n"+hint,style);
                if(ParkPrintsReady && GUI.Button(new Rect(28,Screen.height-51,200,28),"逐张查看成片"))OpenParkGallery();
                return;
            }
            GUI.Box(new Rect(12,56,Screen.width-24,Screen.height-68),"");
            var bounds=new Rect(36,91,Screen.width-72,Mathf.Max(1,Screen.height-184));
            // ScaleToFit preserves the actual viewfinder aspect ratio; no second blur
            // or quality scoring is applied to the immutable park exposures.
            if(exposedTexture!=null)GUI.DrawTexture(bounds,exposedTexture,ScaleMode.ScaleToFit);
            else GUI.Label(bounds,frameNotice,style);
            GUI.Label(new Rect(36,61,Screen.width-72,30),"公园成片 · 第 "+parkFrame+" / 8 张",style);
            bool enabled=GUI.enabled;
            GUI.enabled=parkFrame>1;if(GUI.Button(new Rect(36,Screen.height-70,140,36),"← 上一张"))TurnParkFrame(-1);
            GUI.enabled=parkFrame<8;if(GUI.Button(new Rect(188,Screen.height-70,140,36),"下一张 →"))TurnParkFrame(1);
            GUI.enabled=enabled;
            if(GUI.Button(new Rect(Screen.width-204,Screen.height-70,168,36),"收起照片 [Esc]"))CloseParkGallery();
        }
    }
}
