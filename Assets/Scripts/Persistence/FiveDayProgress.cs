using System;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    public enum StoryDay { SpringTape, SummerPhoto, AutumnWriting, WinterPainting, SpringDeparture }

    [Serializable]
    public sealed class FiveDaySave
    {
        public int version=1;
        public int currentDay;
        public bool finished;
        public bool[] completed=new bool[5];
        public string[] checkpoints=new string[5];
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
        public bool IsComplete(int index)=>data.completed[index];
        public string Checkpoint(int index)=>data.checkpoints[index];

        FiveDaySave Parse(string text)
        {
            var save=JsonUtility.FromJson<FiveDaySave>(text);
            if(save==null || save.version!=1 || save.currentDay<0 || save.currentDay>4 ||
               save.completed==null || save.completed.Length!=5 || save.checkpoints==null || save.checkpoints.Length!=5)
                throw new InvalidDataException("Unsupported or damaged five-day save.");
            for(int i=0;i<5;i++)
                if(!development && save.completed[i]!=(i<save.currentDay || (i==4 && save.finished)))
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
            if(!CanEnter(index) || data.completed[index])return false;
            if(data.checkpoints[index]==snapshot)return true;
            var next=Copy();next.checkpoints[index]=snapshot;return Commit(next);
        }
        public bool Complete(int index,string snapshot)
        {
            if(!CanEnter(index))return false;
            if(data.completed[index])return true;
            if(!development && index!=data.currentDay)return false;
            var next=Copy();next.checkpoints[index]=snapshot;next.completed[index]=true;
            if(development)
            {
                next.currentDay=0;while(next.currentDay<4 && next.completed[next.currentDay])next.currentDay++;
                next.finished=System.Array.TrueForAll(next.completed,value=>value);
            }
            else if(index==4)next.finished=true;else next.currentDay++;
            return Commit(next);
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
