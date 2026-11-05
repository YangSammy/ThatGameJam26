using System;
using System.IO;
namespace MemoryWhitebox.Editor
{
    // Pure in-memory regression: never loads a scene or accesses the player's save root.
    public static class ParkExposureRecoveryChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception("PARK_EXPOSURE_RECOVERY: "+message);}
        public static string Run()
        {
            var disk=new ParkFilmRoll{id="test",version=2};int writes=0;bool fail=true,ambiguous=false;
            var pending=new ParkExposurePersistence(id=>disk,(current,record,bytes)=>{
                writes++;Check(bytes.Length==3,"original encoded bytes retained");
                if(fail && !ambiguous)throw new IOException("manifest write failed");
                record.frameNumber=disk.frames.Count+1;disk.frames.Add(record);
                if(ambiguous)throw new IOException("commit succeeded but caller saw failure");
                return disk;
            });
            var frame=new ParkPhotoRecord{id="frame"};pending.Queue(frame,new byte[]{1,2,3});
            ParkFilmRoll committed;ParkPhotoRecord saved;Exception error;
            Check(!pending.TryCommit(disk,out committed,out saved,out error),"failure is reported");
            Check(pending.HasPending && disk.frames.Count==0,"failure retains pending without charging a frame");
            fail=false;Check(pending.TryCommit(disk,out committed,out saved,out error),"retry succeeds");
            Check(!pending.HasPending && disk.frames.Count==1 && saved==frame,"same exposure committed once");
            Check(!pending.TryCommit(disk,out committed,out saved,out error) && writes==2,"completed retry cannot duplicate");
            ambiguous=true;pending.Queue(new ParkPhotoRecord{id="second"},new byte[]{1,2,3});
            Check(!pending.TryCommit(disk,out committed,out saved,out error) && pending.HasPending,"ambiguous failure retained");
            int before=writes;Check(pending.TryCommit(disk,out committed,out saved,out error),"committed manifest recovered");
            Check(writes==before && disk.frames.Count==2 && !pending.HasPending,"manifest checked before another write");
            var collision=new ParkExposurePersistence(id=>disk,(r,f,b)=>{throw new Exception("must not write collision");});
            collision.Queue(new ParkPhotoRecord{id="frame",moment="different exposure"},new byte[]{1});
            Check(!collision.TryCommit(disk,out committed,out saved,out error) && collision.HasPending,"same ID with different record is not acknowledged");
            var damaged=new ParkExposurePersistence(id=>{throw new InvalidDataException("damaged");},(r,f,b)=>{throw new Exception("must not write");});
            damaged.Queue(new ParkPhotoRecord{id="third"},new byte[]{1});
            Check(!damaged.TryCommit(disk,out committed,out saved,out error) && damaged.HasPending,"damaged manifest blocks writes and retains pending");
            Check(ParkFilmStore.MatchingExposureBytes(new byte[]{1,2,3},new byte[]{1,2,3}),"identical orphan bytes reusable");
            Check(!ParkFilmStore.MatchingExposureBytes(new byte[]{1,2,3},new byte[]{1,4,3}),"orphan collision cannot overwrite");
            Check(!ParkFilmStore.MatchingExposureBytes(new byte[]{1,2},new byte[]{1,2,3}),"truncated orphan rejected");
            string temporary=Path.Combine(Path.GetTempPath(),"park-metadata-check-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            try
            {
                string metadata=Path.Combine(temporary,"roll.json");
                ParkFilmStore.WriteMetadata(metadata,System.Text.Encoding.UTF8.GetBytes("one"));
                ParkFilmStore.WriteMetadata(metadata,System.Text.Encoding.UTF8.GetBytes("two"));
                Check(File.ReadAllText(metadata+".bak")=="one","metadata replacement retains prior version");
                File.WriteAllText(metadata,"damaged");
                Func<string,string> parse=text=>{if(text=="damaged")throw new InvalidDataException("damaged");return text;};
                Check(ParkFilmStore.ReadMetadata(metadata,parse)=="one","damaged metadata recovers backup");
                Check(File.ReadAllText(metadata)=="damaged","fallback never rewrites source");
                ParkFilmStore.WriteMetadata(metadata,System.Text.Encoding.UTF8.GetBytes("three"));
                Check(File.ReadAllText(metadata+".bak")=="one" && Directory.GetFiles(temporary,"*.damaged").Length==1,"recovery write preserves damaged source and valid backup");
                File.WriteAllText(metadata,"damaged");File.WriteAllText(metadata+".bak","damaged");
                bool rejected=false;try{ParkFilmStore.ReadMetadata(metadata,parse);}catch(InvalidDataException){rejected=true;}
                Check(rejected,"both damaged copies reject load");
            }
            finally{Directory.Delete(temporary,true);}
            return "Park exposure recovery checks passed (in-memory and isolated temporary metadata; no user files touched).";
        }
    }
}
