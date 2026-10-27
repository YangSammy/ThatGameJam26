using System;
using System.IO;
using UnityEngine;
namespace MemoryWhitebox
{
    [Serializable] public sealed class PhotoProgress
    {
        public int version=1;
        public PhotoChapter.FilmPhase phase;
        public PhotoChapter.CameraSettings settings,captured;
        public int chemistry,attempts;
        public float remaining,bagTurns,rewindTurns,clockDuration;
        public bool threaded,fogged,agitated,backOpen,completed;
        public string image;
    }
    public static class PhotoProgressStore
    {
        // Test runners use an isolated directory, never the player's progress.
        public static string DirectoryOverride;
        public static bool SuppressWrites;
        public static string PathName => Path.Combine(DirectoryOverride??GameSavePaths.Root,"study-photo-v1.json");
        static bool SettingsValid(PhotoChapter.CameraSettings s)
        {
            return s!=null && Finite(s.X)&&Finite(s.Z)&&Finite(s.Yaw)&&Finite(s.Focus) && s.Focus>0 && s.Focal>=28&&s.Focal<=70 && s.Aperture>=2.8f&&s.Aperture<=16 && s.Shutter>=.001f&&s.Shutter<=.25f && (s.ISO==100||s.ISO==200||s.ISO==400);
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static PhotoProgress Read(string path)
        {
            var d=JsonUtility.FromJson<PhotoProgress>(File.ReadAllText(path));
            if(d==null||d.version!=1 || !Enum.IsDefined(typeof(PhotoChapter.FilmPhase),d.phase) || !SettingsValid(d.settings) || d.chemistry<0||d.chemistry>4 || !Finite(d.remaining)||d.remaining<0||d.remaining>3 || !Finite(d.bagTurns)||d.bagTurns<0||d.bagTurns>1 || !Finite(d.rewindTurns)||d.rewindTurns<0||d.rewindTurns>1 || !Finite(d.clockDuration)||d.clockDuration<0||d.clockDuration>3 || d.attempts<0)throw new InvalidDataException("Invalid photo progress");
            if(d.phase>=PhotoChapter.FilmPhase.Exposed && (!SettingsValid(d.captured)||string.IsNullOrEmpty(d.image)))throw new InvalidDataException("Missing exposure");
            if(d.completed&&d.phase!=PhotoChapter.FilmPhase.Developed)throw new InvalidDataException("Invalid completed phase");
            if(d.phase==PhotoChapter.FilmPhase.Developed&&d.chemistry!=4)throw new InvalidDataException("Invalid developed phase");
            if(!string.IsNullOrEmpty(d.image)) {
                var texture=new Texture2D(2,2);
                try {if(!texture.LoadImage(Convert.FromBase64String(d.image))||texture.width!=720||texture.height!=400)throw new InvalidDataException("Invalid photograph");}
                finally {UnityEngine.Object.Destroy(texture);}
            }
            return d;
        }
        public static PhotoProgress Load(out string warning,out bool blocked)
        {
            warning="";blocked=false;
            foreach(string path in new[]{PathName,PathName+".bak"}) {
                if(!File.Exists(path))continue;
                try {
                    var d=Read(path);
                    if(path.EndsWith(".bak")) {
                        warning="已从备份恢复摄影进度";
                        // Retain the damaged file without letting the next save replace the good backup with it.
                        try {if(File.Exists(PathName))File.Copy(PathName,PathName+".damaged-"+DateTime.UtcNow.Ticks);File.Copy(path,PathName,true);}
                        catch(Exception){blocked=true;warning="已读取备份，但存档目录不可写；原文件已保留";}
                    }
                    return d;
                }
                catch(Exception e){Debug.LogWarning("Photo save could not be loaded: "+e.Message);}
            }
            if(File.Exists(PathName)||File.Exists(PathName+".bak")){warning="摄影存档无法读取，原文件已保留；本次进度暂不覆盖存档";blocked=true;}
            return null;
        }
        public static void Save(PhotoProgress d)
        {
            if(SuppressWrites)return;
            string path=PathName,temp=path+".tmp";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(d));
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
        }
    }
}
