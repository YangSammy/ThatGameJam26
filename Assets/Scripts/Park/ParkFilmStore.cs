using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    public enum ParkFilmState { Loaded, AwaitingDevelopment, Developed }

    [Serializable] public sealed class ParkCameraLoading
    {
        public PhotoChapter.FilmPhase phase;
        public bool threaded;
        public int iso=100;
    }

    [Serializable] public sealed class ParkDarkroomProgress
    {
        public PhotoChapter.FilmPhase phase=PhotoChapter.FilmPhase.DarkBag;
        public int chemistry;
        public float remaining,bagTurns,clockDuration;
        public bool agitated;
        public int selectedFrame=1;
    }

    [Serializable] public sealed class ParkFilmRoll
    {
        public const int FrameCapacity=8;
        public int version=1;
        public string id,createdUtc,updatedUtc;
        public ParkFilmState state;
        public ParkDarkroomProgress darkroom;
        public ParkCameraLoading cameraLoading;
        public List<ParkPhotoRecord> frames=new List<ParkPhotoRecord>();
        public int Remaining=>Mathf.Max(0,FrameCapacity-frames.Count);
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
            if(roll==null || roll.version!=1 || roll.frames==null || roll.frames.Count>ParkFilmRoll.FrameCapacity)throw new InvalidDataException("Unsupported or damaged film record.");
            RollFolder(roll.id);
            if(roll.cameraLoading!=null)
            {
                var c=roll.cameraLoading;
                if(c.phase<PhotoChapter.FilmPhase.Empty || c.phase>PhotoChapter.FilmPhase.Ready ||
                   (c.iso!=100 && c.iso!=200 && c.iso!=400) ||
                   (c.phase>=PhotoChapter.FilmPhase.Closed && !c.threaded))throw new InvalidDataException("Invalid camera loading progress.");
            }
            if(!Enum.IsDefined(typeof(ParkFilmState),roll.state) || (roll.state==ParkFilmState.Loaded && roll.Remaining==0))throw new InvalidDataException("Invalid film state.");
            if(roll.state!=ParkFilmState.Loaded && roll.frames.Count!=ParkFilmRoll.FrameCapacity)throw new InvalidDataException("Incomplete sealed film.");
            if(roll.darkroom!=null)
            {
                var d=roll.darkroom;
                if(d.phase<PhotoChapter.FilmPhase.DarkBag || d.phase>PhotoChapter.FilmPhase.Developed ||
                   d.chemistry<0 || d.chemistry>4 || !FiniteRange(d.remaining,0,3) || !FiniteRange(d.bagTurns,0,1) ||
                   !FiniteRange(d.clockDuration,0,3) || d.selectedFrame<1 || d.selectedFrame>ParkFilmRoll.FrameCapacity ||
                   (d.phase==PhotoChapter.FilmPhase.Developed)!=(roll.state==ParkFilmState.Developed) ||
                   (d.phase==PhotoChapter.FilmPhase.Developed && d.chemistry!=4) ||
                   (d.phase!=PhotoChapter.FilmPhase.Developed && d.chemistry==4))throw new InvalidDataException("Invalid darkroom progress.");
            }
            var ids=new HashSet<string>();
            for(int i=0;i<roll.frames.Count;i++)
            {
                var frame=roll.frames[i];Guid parsed;
                if(frame==null || frame.filmId!=roll.id || frame.frameNumber!=i+1 || !Guid.TryParseExact(frame.id,"N",out parsed) || !ids.Add(frame.id) || frame.negativeFile!=frame.id+".frame")throw new InvalidDataException("Invalid film frame.");
                if(!File.Exists(Path.Combine(RollFolder(roll.id),frame.negativeFile)))throw new InvalidDataException("A film frame is missing.");
            }
        }
        static bool FiniteRange(float value,float min,float max)=>!float.IsNaN(value) && !float.IsInfinity(value) && value>=min && value<=max;
        public static ParkFilmRoll SaveCameraLoading(string id,ParkCameraLoading progress)
        {
            var roll=string.IsNullOrEmpty(id)?LoadOrCreate():Load(id);
            if(progress==null || roll.state!=ParkFilmState.Loaded ||
               (roll.frames.Count>0 && progress.phase!=PhotoChapter.FilmPhase.Ready))
                throw new InvalidOperationException("Cannot reset an exposed film.");
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
            if(stored.state!=ParkFilmState.Loaded || stored.Remaining<=0)throw new InvalidOperationException("Film cannot accept another exposure.");
            if(stored.frames.Count!=current.frames.Count)throw new InvalidOperationException("Film changed in another session; reload before continuing.");
            frame.filmId=stored.id;frame.frameNumber=stored.frames.Count+1;
            Guid parsed;if(!Guid.TryParseExact(frame.id,"N",out parsed))throw new InvalidDataException("Invalid frame ID.");
            frame.negativeFile=frame.id+".frame";
            // Frame bytes are committed before the manifest; a failed manifest write cannot
            // consume a slot or overwrite earlier frames. Orphan bytes are never exposed in UI.
            string negative=Path.Combine(RollFolder(stored.id),frame.negativeFile);
            if(File.Exists(negative))throw new InvalidOperationException("Duplicate exposure ID.");
            WriteAtomic(negative,encodedImage);
            stored.frames.Add(frame);stored.updatedUtc=DateTime.UtcNow.ToString("o");
            if(stored.Remaining==0)stored.state=ParkFilmState.AwaitingDevelopment;
            WriteJson(Manifest(stored.id),stored);return stored;
        }
        // Only the darkroom uses this after persisting the completed processing state.
        public static bool TryReadDevelopedFrame(string rollId,int frameNumber,out byte[] image,out string reason)
        {
            image=null;reason="";
            try
            {
                var roll=Load(rollId);
                if(roll.state!=ParkFilmState.Developed){reason="胶卷尚未显影。";return false;}
                if(frameNumber<1 || frameNumber>roll.frames.Count){reason="没有这张底片。";return false;}
                image=File.ReadAllBytes(Path.Combine(RollFolder(rollId),roll.frames[frameNumber-1].negativeFile));return true;
            }
            catch(Exception){reason="胶卷数据暂时无法读取。";return false;}
        }
    }
}
