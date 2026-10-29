using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    public enum ParkFilmState { Loaded, AwaitingDevelopment, Developed }
    public enum ParkPrintStage { None, NegativeDrying, ContactReady, Exposing, PaperDeveloper, PaperStop, PaperFixer, PaperWash, PaperDrying, Complete }

    [Serializable] public sealed class ParkCameraLoading
    {
        public PhotoChapter.FilmPhase phase;
        public bool threaded;
        public int mechanicalStep;
        public int iso=100;
    }

    [Serializable] public sealed class ParkDarkroomProgress
    {
        public int workflowVersion;
        public bool tankSealed,needsDrain,backOpen;
        public float rewindTurns;
        public ParkPrintStage printStage;
        public float printRemaining;
        public bool printProcessing;
        public DarkroomPrintState printObjects;
        public PhotoChapter.FilmPhase phase=PhotoChapter.FilmPhase.DarkBag;
        public int chemistry;
        public float remaining,bagTurns,clockDuration;
        public bool agitated;
        public int selectedFrame=1;
    }

    [Serializable] public sealed class ParkFilmRoll
    {
        public const int FrameCapacity=3;
        public int Capacity=>version==1?4:FrameCapacity;
        public int version=3;
        public string chapterId=ChapterIds.Photography;
        public string id,createdUtc,updatedUtc;
        public ParkFilmState state;
        public ParkDarkroomProgress darkroom;
        public ParkCameraLoading cameraLoading;
        public BridgePhotoCheckpoint bridge;
        public BridgePhotoCheckpoint entrance,meadow;
        public BridgePhotoCheckpoint Task(string id)=>id=="entrance"?entrance:id=="meadow"?meadow:bridge;
        public string NextTask=>frames.Count==0?"entrance":frames.Count==1?"bridge":"meadow";
        public List<ParkPhotoRecord> frames=new List<ParkPhotoRecord>();
        public int Remaining=>Mathf.Max(0,Capacity-frames.Count);
        public bool ReadyForPark=>state==ParkFilmState.Loaded &&
            (frames.Count>0 || (cameraLoading!=null && cameraLoading.phase==PhotoChapter.FilmPhase.Ready));
    }

    // No texture gallery is exposed by the park. Undeveloped bytes are stored separately
    // from the earlier free-shoot prototype, and require Developed state to be read as a photo.
    public static class ParkFilmStore
    {
        [Serializable] sealed class ActiveRoll { public string id; }
        public static string Root=>Path.Combine(GameSavePaths.Root,"ParkFilm");
        static string ActivePath=>Path.Combine(Root,"active-roll.json");
        static string RollFolder(string id)
        {Guid parsed;if(!Guid.TryParseExact(id,"N",out parsed))throw new InvalidDataException("Invalid film ID.");return Path.Combine(Root,id);}
        static string Manifest(string id)=>Path.Combine(RollFolder(id),"roll.json");
        static void Validate(ParkFilmRoll roll)
        {
            if(roll==null || (roll.version!=1 && roll.version!=2 && roll.version!=3) || roll.frames==null || roll.frames.Count>roll.Capacity)throw new InvalidDataException("Unsupported or damaged film record.");
            if(string.IsNullOrEmpty(roll.chapterId))roll.chapterId=ChapterIds.Photography;
            if(roll.chapterId!=ChapterIds.Photography)throw new InvalidDataException("Film belongs to a different chapter.");
            RollFolder(roll.id);
            foreach(string taskId in new[]{"entrance","bridge","meadow"})
            {
                var b=roll.Task(taskId);
                if(b==null)continue;
                if(!FiniteRange(b.fov,20,120) || !FiniteRange(b.ev100,0,20) ||
                   !FiniteRange(b.position.x,-100000,100000) || !FiniteRange(b.position.y,-100000,100000) || !FiniteRange(b.position.z,-100000,100000) ||
                   !FiniteRange(b.euler.x,-360,360) || !FiniteRange(b.euler.y,-360,360) || !FiniteRange(b.euler.z,-360,360) ||
                   !FiniteRange(b.subjectPoint.x,-100000,100000) || !FiniteRange(b.subjectPoint.y,-100000,100000) || !FiniteRange(b.subjectPoint.z,-100000,100000) ||
                   ((b.aligned || b.captured) && !b.hasReference) || (b.handJoined && (!b.aligned || taskId!="meadow")) || (b.captured && !b.aligned))throw new InvalidDataException("Invalid photography checkpoint.");
                if(b.hasReference && !File.Exists(Path.Combine(RollFolder(roll.id),taskId+"-reference.png")))throw new InvalidDataException("Photography reference is missing.");
                if(!FiniteRange(b.savedFocus,0,10000) || !FiniteRange(b.savedAperture,0,64) || !FiniteRange(b.savedShutter,0,30))throw new InvalidDataException("Invalid saved camera settings.");
                if(roll.version>=3 && b.captured!=roll.frames.Exists(f=>f!=null && f.taskId==taskId))throw new InvalidDataException("Photography checkpoint does not match exposures.");
            }
            if(roll.cameraLoading!=null)
            {
                var c=roll.cameraLoading;
                if(c.phase<PhotoChapter.FilmPhase.Empty || c.phase>PhotoChapter.FilmPhase.Ready ||
                   (c.iso!=100 && c.iso!=200 && c.iso!=400 && c.iso!=800) ||
                   (c.mechanicalStep<0 || c.mechanicalStep>7) || (c.phase>=PhotoChapter.FilmPhase.Closed && !c.threaded))throw new InvalidDataException("Invalid camera loading progress.");
            }
            if(!Enum.IsDefined(typeof(ParkFilmState),roll.state) || (roll.state==ParkFilmState.Loaded && roll.Remaining==0))throw new InvalidDataException("Invalid film state.");
            if(roll.state!=ParkFilmState.Loaded && roll.frames.Count!=roll.Capacity)throw new InvalidDataException("Incomplete sealed film.");
            if(roll.darkroom!=null)
            {
                var d=roll.darkroom;
                if(d.workflowVersion<0 || d.workflowVersion>3 ||
                   d.phase<(d.workflowVersion>=1?PhotoChapter.FilmPhase.Exposed:PhotoChapter.FilmPhase.DarkBag) || d.phase>PhotoChapter.FilmPhase.Developed ||
                   d.chemistry<0 || d.chemistry>4 || !FiniteRange(d.remaining,0,3) || !FiniteRange(d.bagTurns,0,1) ||
                   !FiniteRange(d.clockDuration,0,3) || d.selectedFrame<1 || d.selectedFrame>roll.Capacity ||
                   (d.phase==PhotoChapter.FilmPhase.Developed)!=(roll.state==ParkFilmState.Developed) ||
                   (d.phase==PhotoChapter.FilmPhase.Developed && d.chemistry!=4) ||
                   (d.phase!=PhotoChapter.FilmPhase.Developed && d.chemistry==4))throw new InvalidDataException("Invalid darkroom progress.");
                if(d.workflowVersion>=1 && (!FiniteRange(d.rewindTurns,0,1) ||
                   (d.phase>=PhotoChapter.FilmPhase.Rewound && d.rewindTurns<.999f) ||
                   (d.phase>=PhotoChapter.FilmPhase.Processing && !d.tankSealed) ||
                   (d.needsDrain && (d.phase!=PhotoChapter.FilmPhase.Tank || d.chemistry<1 || !d.tankSealed))))throw new InvalidDataException("Invalid tank checkpoint.");
                if(d.workflowVersion>=2)
                {
                    if(!Enum.IsDefined(typeof(ParkPrintStage),d.printStage) || !FiniteRange(d.printRemaining,0,d.workflowVersion>=3?3600:10) ||
                       (d.printStage==ParkPrintStage.Complete)!=(d.phase==PhotoChapter.FilmPhase.Developed) ||
                       (d.printStage!=ParkPrintStage.None && (!d.tankSealed || d.needsDrain)) ||
                       (d.printStage!=ParkPrintStage.None && d.printStage!=ParkPrintStage.Complete && (d.phase!=PhotoChapter.FilmPhase.Tank || d.chemistry!=3)) ||
                       (d.printProcessing && (d.printStage==ParkPrintStage.None || d.printStage==ParkPrintStage.ContactReady || d.printStage==ParkPrintStage.Complete)))
                        throw new InvalidDataException("Invalid contact print checkpoint.");
                }
                if(d.workflowVersion>=3)
                {
                    if(d.printObjects==null)throw new InvalidDataException("Missing print item placements.");
                    d.printObjects.ValidateProgress(d.printStage,d.printProcessing);
                }
            }
            var ids=new HashSet<string>();
            for(int i=0;i<roll.frames.Count;i++)
            {
                var frame=roll.frames[i];Guid parsed;
                if(frame==null || frame.filmId!=roll.id || frame.frameNumber!=i+1 || !Guid.TryParseExact(frame.id,"N",out parsed) || !ids.Add(frame.id) || frame.negativeFile!=frame.id+".frame")throw new InvalidDataException("Invalid film frame.");
                if(roll.version>=3)
                {
                    string taskId=i==0?"entrance":i==1?"bridge":"meadow";
                    var checkpoint=roll.Task(taskId);
                    if(frame.taskId!=taskId || checkpoint==null || !checkpoint.captured || (taskId=="meadow" && !checkpoint.handJoined))throw new InvalidDataException("Invalid photography sequence.");
                }
                if(!File.Exists(Path.Combine(RollFolder(roll.id),frame.negativeFile)))throw new InvalidDataException("A film frame is missing.");
            }
        }
        static bool FiniteRange(float value,float min,float max)=>!float.IsNaN(value) && !float.IsInfinity(value) && value>=min && value<=max;
        public static ParkFilmRoll SaveBridge(string id,BridgePhotoCheckpoint checkpoint,byte[] reference=null)
        {
            var roll=Load(id);
            if(roll.state!=ParkFilmState.Loaded)throw new InvalidOperationException("Film is sealed.");
            if(checkpoint==null || (roll.version>=3 && roll.NextTask!="bridge"))throw new InvalidOperationException("Bridge task is not current.");
            if(roll.bridge!=null && roll.bridge.captured)throw new InvalidOperationException("Bridge exposure already committed.");
            if(reference!=null && (roll.bridge==null || !roll.bridge.hasReference))WriteAtomic(Path.Combine(RollFolder(id),"bridge-reference.png"),reference);
            roll.bridge=checkpoint;Validate(roll);WriteJson(Manifest(id),roll);return roll;
        }
        public static byte[] ReadBridgeReference(string id)
        {return File.ReadAllBytes(Path.Combine(RollFolder(id),"bridge-reference.png"));}
        static void CheckTaskId(string task)
        {if(task!="entrance" && task!="bridge" && task!="meadow")throw new InvalidDataException("Unknown photography task.");}
        public static ParkFilmRoll SaveTask(string id,string task,BridgePhotoCheckpoint checkpoint,byte[] reference=null)
        {
            CheckTaskId(task);
            if(checkpoint==null)throw new InvalidDataException("Missing photography checkpoint.");
            if(task=="bridge")return SaveBridge(id,checkpoint,reference);
            var roll=Load(id);var old=roll.Task(task);
            if(roll.state!=ParkFilmState.Loaded || roll.NextTask!=task || (old!=null && old.captured))throw new InvalidOperationException("Photography task is not current.");
            if(reference!=null && (old==null || !old.hasReference))WriteAtomic(Path.Combine(RollFolder(id),task+"-reference.png"),reference);
            if(task=="entrance")roll.entrance=checkpoint;else roll.meadow=checkpoint;
            Validate(roll);WriteJson(Manifest(id),roll);return roll;
        }
        public static byte[] ReadTaskReference(string id,string task)
        {CheckTaskId(task);return File.ReadAllBytes(Path.Combine(RollFolder(id),task+"-reference.png"));}
        public static ParkFilmRoll SaveCameraLoading(string id,ParkCameraLoading progress)
        {
            var roll=string.IsNullOrEmpty(id)?LoadOrCreate():Load(id);
            if(progress==null || roll.state!=ParkFilmState.Loaded ||
               (roll.frames.Count>0 && progress.phase!=PhotoChapter.FilmPhase.Ready))
                throw new InvalidOperationException("Cannot reset an exposed film.");
            if(roll.cameraLoading!=null && roll.cameraLoading.phase!=PhotoChapter.FilmPhase.Empty && roll.cameraLoading.iso!=progress.iso)
                throw new InvalidOperationException("ISO is fixed once film has been loaded.");
            roll.cameraLoading=progress;roll.updatedUtc=DateTime.UtcNow.ToString("o");
            Validate(roll);WriteJson(Manifest(roll.id),roll);return roll;
        }
        public static ParkFilmRoll LoadActive()
        {
            if(!File.Exists(ActivePath))return null;
            var active=JsonUtility.FromJson<ActiveRoll>(File.ReadAllText(ActivePath));
            if(active==null)throw new InvalidDataException("Active film record is damaged.");
            return Load(active.id);
        }
        // Progress and preview permission are committed together. No preview is unlocked
        // if the final rinse could not be saved. Frame bytes are never rewritten here.
        public static ParkFilmRoll SaveDevelopment(string rollId,ParkDarkroomProgress progress)
        {
            var roll=Load(rollId);
            if(progress==null || roll.state==ParkFilmState.Loaded)throw new InvalidOperationException("Film is not ready for development.");
            if(roll.state==ParkFilmState.Developed && progress.phase!=PhotoChapter.FilmPhase.Developed)throw new InvalidOperationException("Developed film cannot be reset.");
            roll.darkroom=progress;
            if(progress.phase==PhotoChapter.FilmPhase.Developed)roll.state=ParkFilmState.Developed;
            roll.updatedUtc=DateTime.UtcNow.ToString("o");Validate(roll);WriteJson(Manifest(roll.id),roll);return roll;
        }
        internal static Texture2D CreateContactSheetPreview(string rollId)
        {
            var roll=Load(rollId);
            if(roll.state!=ParkFilmState.Developed && (roll.darkroom==null || roll.darkroom.printStage<ParkPrintStage.PaperDeveloper))
                throw new InvalidOperationException("Contact sheet has not been exposed.");
            const int width=256,height=144,border=12;
            int sheetWidth=(width+border)*roll.frames.Count+border;
            var sheet=new Texture2D(sheetWidth,Mathf.RoundToInt(sheetWidth*9f/16),TextureFormat.RGB24,false);
            try
            {
                var background=new Color[sheet.width*sheet.height];for(int i=0;i<background.Length;i++)background[i]=new Color(.93f,.91f,.84f);sheet.SetPixels(background);
                for(int n=0;n<roll.frames.Count;n++)
                {
                    var image=new Texture2D(2,2);
                    try
                    {
                        if(!image.LoadImage(File.ReadAllBytes(Path.Combine(RollFolder(roll.id),roll.frames[n].negativeFile))))throw new InvalidDataException("Negative unavailable.");
                        float fit=Mathf.Min(width/(float)image.width,height/(float)image.height);
                        int w=Mathf.Max(1,Mathf.RoundToInt(image.width*fit)),h=Mathf.Max(1,Mathf.RoundToInt(image.height*fit));
                        for(int y=0;y<h;y++)for(int x=0;x<w;x++)sheet.SetPixel(border+n*(width+border)+(width-w)/2+x,(sheet.height-h)/2+y,image.GetPixelBilinear((x+.5f)/w,(y+.5f)/h));
                    }
                    finally{UnityEngine.Object.Destroy(image);}
                }
                sheet.name="Contact sheet "+roll.id;sheet.Apply();return sheet;
            }
            catch{UnityEngine.Object.Destroy(sheet);throw;}
        }
        static void WriteAtomic(string path,byte[] bytes)
        {
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        static void WriteJson(string path,object value)
        {WriteAtomic(path,System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(value,true)));}
        public static ParkFilmRoll LoadOrCreate()
        {
            Directory.CreateDirectory(Root);
            if(File.Exists(ActivePath))
            {
                var active=JsonUtility.FromJson<ActiveRoll>(File.ReadAllText(ActivePath));
                if(active==null)throw new InvalidDataException("Active film record is damaged.");
                return Load(active.id);
            }
            var now=DateTime.UtcNow.ToString("o");
            var roll=new ParkFilmRoll{id=Guid.NewGuid().ToString("N"),createdUtc=now,updatedUtc=now,state=ParkFilmState.Loaded};
            Directory.CreateDirectory(RollFolder(roll.id));WriteJson(Manifest(roll.id),roll);
            WriteJson(ActivePath,new ActiveRoll{id=roll.id});return roll;
        }
        public static ParkFilmRoll Load(string id)
        {
            var roll=JsonUtility.FromJson<ParkFilmRoll>(File.ReadAllText(Manifest(id)));
            Validate(roll);if(roll.id!=id)throw new InvalidDataException("Film identity mismatch.");return roll;
        }
        public static ParkFilmRoll SaveExposure(ParkFilmRoll current,ParkPhotoRecord frame,byte[] encodedImage)
        {
            if(current==null || frame==null || encodedImage==null || encodedImage.Length==0)throw new InvalidDataException("No exposure data.");
            var stored=Load(current.id);
            if(stored.version>=3)
            {
                var task=stored.Task(stored.NextTask);
                if(frame.taskId!=stored.NextTask || task==null || !task.aligned || task.captured ||
                   (frame.taskId=="meadow" && !task.handJoined))throw new InvalidOperationException("Photography task conditions are incomplete.");
            }
            if(!stored.ReadyForPark || stored.Remaining<=0)throw new InvalidOperationException("Film cannot accept another exposure.");
            if(stored.frames.Count!=current.frames.Count)throw new InvalidOperationException("Film changed in another session; reload before continuing.");
            if(frame.taskId=="bridge" && (stored.bridge==null || !stored.bridge.aligned || stored.bridge.captured))throw new InvalidOperationException("Bridge exposure is not ready or is already committed.");
            frame.filmId=stored.id;frame.frameNumber=stored.frames.Count+1;
            Guid parsed;if(!Guid.TryParseExact(frame.id,"N",out parsed))throw new InvalidDataException("Invalid frame ID.");
            frame.negativeFile=frame.id+".frame";
            // Frame bytes are committed before the manifest; a failed manifest write cannot
            // consume a slot or overwrite earlier frames. Orphan bytes are never exposed in UI.
            string negative=Path.Combine(RollFolder(stored.id),frame.negativeFile);
            if(File.Exists(negative))throw new InvalidOperationException("Duplicate exposure ID.");
            WriteAtomic(negative,encodedImage);
            stored.frames.Add(frame);stored.updatedUtc=DateTime.UtcNow.ToString("o");
            if(frame.taskId=="bridge" && stored.bridge!=null)stored.bridge.captured=true;
            if(frame.taskId=="entrance" && stored.entrance!=null)stored.entrance.captured=true;
            if(frame.taskId=="meadow" && stored.meadow!=null)stored.meadow.captured=true;
            if(stored.Remaining==0)stored.state=ParkFilmState.AwaitingDevelopment;
            Validate(stored);WriteJson(Manifest(stored.id),stored);return stored;
        }
        // Only the darkroom uses this after persisting the completed processing state.
        public static bool TryReadDevelopedFrame(string rollId,int frameNumber,out byte[] image,out string reason)
        {
            image=null;reason="";
            try
            {
                var roll=Load(rollId);
                if(roll.state!=ParkFilmState.Developed){reason="Develop film first.";return false;}
                if(frameNumber<1 || frameNumber>roll.frames.Count){reason="Frame unavailable.";return false;}
                image=File.ReadAllBytes(Path.Combine(RollFolder(rollId),roll.frames[frameNumber-1].negativeFile));return true;
            }
            catch(Exception){reason="Film data unavailable.";return false;}
        }
    }
}
