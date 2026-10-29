using System;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    public enum StoryDay { SpringTape, SummerPhoto, AutumnWriting, WinterPainting, SpringDeparture }

    [Serializable]
    public sealed class FiveDaySave
    {
        public int version=2;
        public int currentDay;
        public bool finished;
        public bool[] completed=new bool[5];
        public string[] checkpoints=new string[5];
        public ChapterSaveRecord[] chapters;
        public System.Collections.Generic.List<ArtifactReference> artifacts=new System.Collections.Generic.List<ArtifactReference>();
        public bool returnFromPark;
        public Vector3 housePosition;
        public Quaternion houseRotation=Quaternion.identity;
        public float houseFov=65;
    }

    // No scene objects or transient input are serialized. Photography retains its
    // existing image/film store; this file owns only chapter order and safe nodes.
    public sealed class FiveDayProgress
    {
        FiveDaySave data=new FiveDaySave();
        bool blocked;
        bool development;
        public StoryDay Day=>(StoryDay)data.currentDay;
        public bool Finished=>data.finished;
        public bool Available=>!blocked;
        public string Error {get;private set;}="";
        string FilePath=>Path.Combine(GameSavePaths.Root,development?"five-day-development-v1.json":"five-day-progress-v1.json");
        public bool CanEnter(int index)=>!blocked && index>=0 && index<5 && (development || index<=data.currentDay);
        ChapterSaveRecord Record(FiveDaySave save,int index)=>Array.Find(save.chapters,r=>r.chapterId==ChapterIds.At(index));
        public bool IsComplete(int index)=>Record(data,index).completed;
        public string Checkpoint(int index)=>Record(data,index).payload;
        public string Checkpoint(string id)=>Checkpoint(ChapterIds.Index(id));
        public FiveDayProgress(){Normalize(data);}
        static void Normalize(FiveDaySave save)
        {
            if(save.chapters==null)
            {
                if(save.completed==null || save.completed.Length!=5 || save.checkpoints==null || save.checkpoints.Length!=5)
                    throw new InvalidDataException("Invalid legacy chapter arrays.");
                save.chapters=new ChapterSaveRecord[5];
                for(int i=0;i<5;i++)save.chapters[i]=new ChapterSaveRecord{chapterId=ChapterIds.At(i),completed=save.completed[i],payload=save.checkpoints[i]};
            }
            if(save.chapters.Length!=5)throw new InvalidDataException("Invalid chapter records.");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var record in save.chapters)
            {if(record==null || record.schemaVersion!=1 || !ids.Add(record.chapterId))throw new InvalidDataException("Invalid chapter identity.");ChapterIds.Index(record.chapterId);}
            if(save.artifacts==null)save.artifacts=new System.Collections.Generic.List<ArtifactReference>();
            save.version=2;save.completed=null;save.checkpoints=null;
        }

        FiveDaySave Parse(string text)
        {
            var save=JsonUtility.FromJson<FiveDaySave>(text);
            if(save==null || (save.version!=1 && save.version!=2) || save.currentDay<0 || save.currentDay>4 || (save.version==2 && save.chapters==null))
                throw new InvalidDataException("Unsupported or damaged five-day save.");
            Normalize(save);
            for(int i=0;i<5;i++)
                if(!development && Record(save,i).completed!=(i<save.currentDay || (i==4 && save.finished)))
                    throw new InvalidDataException("Non-sequential chapter progress.");
            if(save.finished && save.currentDay!=4)throw new InvalidDataException("Invalid ending state.");
            if(save.returnFromPark && (!Finite(save.housePosition.x) || !Finite(save.housePosition.y) || !Finite(save.housePosition.z) ||
                !Finite(save.houseRotation.x) || !Finite(save.houseRotation.y) || !Finite(save.houseRotation.z) || !Finite(save.houseRotation.w) ||
                !Finite(save.houseFov) || save.houseFov<10 || save.houseFov>150 ||
                save.houseRotation.x*save.houseRotation.x+save.houseRotation.y*save.houseRotation.y+save.houseRotation.z*save.houseRotation.z+save.houseRotation.w*save.houseRotation.w<.5f))
                throw new InvalidDataException("Invalid house return pose.");
            return save;
        }
        static bool Finite(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        public static FiveDayProgress Load(bool development=false)
        {
            var progress=new FiveDayProgress{development=development};
            string FilePath=progress.FilePath;
            if(!File.Exists(FilePath) && !File.Exists(FilePath+".bak"))return progress;
            try{progress.data=progress.Parse(File.ReadAllText(FilePath));}
            catch(Exception first)
            {
                try
                {
                    progress.data=progress.Parse(File.ReadAllText(FilePath+".bak"));
                    // Preserve the damaged primary and the valid backup before any later write.
                    if(File.Exists(FilePath))File.Move(FilePath,FilePath+".damaged-"+Guid.NewGuid().ToString("N"));
                    Debug.LogWarning("Recovered five-day progress from backup: "+first.Message);
                }
                catch(Exception second){progress.blocked=true;progress.Error=second.Message;Debug.LogError("Five-day save unavailable; files preserved. "+first.Message+" / "+second.Message);}
            }
            return progress;
        }
        FiveDaySave Copy()=>JsonUtility.FromJson<FiveDaySave>(JsonUtility.ToJson(data));
        bool Commit(FiveDaySave next)
        {
            if(blocked)return false;
            try
            {
                string json=JsonUtility.ToJson(next,true);Parse(json);
                Directory.CreateDirectory(GameSavePaths.Root);
                using(var stream=new FileStream(FilePath+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
                {
                    var bytes=System.Text.Encoding.UTF8.GetBytes(json);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                if(File.Exists(FilePath))File.Replace(FilePath+".tmp",FilePath,FilePath+".bak");
                else File.Move(FilePath+".tmp",FilePath);
                data=next;Error="";return true;
            }
            catch(Exception e){if(Error!=e.Message)Debug.LogWarning("Five-day progress not saved: "+e.Message);Error=e.Message;return false;}
        }
        public bool SaveCheckpoint(int index,string snapshot)
        {
            if(!CanEnter(index) || IsComplete(index))return false;
            if(Checkpoint(index)==snapshot)return true;
            var next=Copy();Record(next,index).payload=snapshot;return Commit(next);
        }
        public bool Complete(int index,string snapshot)
        {
            return CommitChapter(ChapterIds.At(index),snapshot,true).Success;
        }
        public SaveResult CommitChapter(string chapterId,string snapshot,bool complete,ArtifactReference artifact=null)
        {
            int index=ChapterIds.Index(chapterId);
            if(!CanEnter(index))return SaveResult.Failed(Error!=""?Error:"Chapter is unavailable.");
            if(IsComplete(index))return SaveResult.Ok();
            if(complete && !development && index!=data.currentDay)return SaveResult.Failed("Previous chapter is incomplete.");
            var next=Copy();var record=Record(next,index);record.payload=snapshot;record.completed=complete;
            if(artifact!=null)
            {
                if(artifact.chapterId!=chapterId || string.IsNullOrEmpty(artifact.artifactId) || string.IsNullOrEmpty(artifact.relativePath))return SaveResult.Failed("Invalid artifact reference.");
                next.artifacts.RemoveAll(a=>a.artifactId==artifact.artifactId);next.artifacts.Add(artifact);
            }
            if(complete)
            {
            if(development)
            {
                next.currentDay=0;while(next.currentDay<4 && Record(next,next.currentDay).completed)next.currentDay++;
                next.finished=System.Array.TrueForAll(next.chapters,value=>value.completed);
            }
            else if(index==4)next.finished=true;else next.currentDay++;
            }
            return Commit(next)?SaveResult.Ok():SaveResult.Failed(Error);
        }
        public bool RememberReturn(Vector3 position,Quaternion rotation,float fov)
        {
            var next=Copy();next.returnFromPark=true;next.housePosition=position;next.houseRotation=rotation;next.houseFov=fov;return Commit(next);
        }
        public void RestoreReturn(Camera camera)
        {
            if(!data.returnFromPark || !Available)return;
            camera.transform.SetPositionAndRotation(data.housePosition,data.houseRotation);camera.fieldOfView=data.houseFov;
        }
        public void ClearReturn()
        {
            if(!data.returnFromPark)return;
            var next=Copy();next.returnFromPark=false;Commit(next);
        }
        public void BlockRestore(Exception error)
        {blocked=true;Error=error.Message;Debug.LogError("Chapter checkpoint unavailable; save preserved. "+Error);}
    }
}
