using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkFilmIsolatedAcceptance
    {
        static void Check(bool value,string message,List<string> report)
        {if(!value)throw new Exception("PARK_FILM_ISOLATED: "+message);report.Add("PASS: "+message);}
        public static string Run()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA","park-film-acceptance-"+Guid.NewGuid().ToString("N")));
            var report=new List<string>();Texture2D image=null;
            try
            {
                using(ParkFilmStore.BeginValidationScope(directory))
                {
                    var roll=ParkFilmStore.LoadOrCreate();string id=roll.id;
                    Check(roll.frames.Count==0 && roll.cameraLoading==null,"fresh isolated roll",report);
                    foreach(int step in new[]{1,2,4,5,6,7})
                    {
                        roll=ParkFilmStore.SaveCameraLoading(id,ParkCameraLoading.AtStep(step,400));
                        roll=ParkFilmStore.LoadActive();
                        Check(roll.id==id && roll.cameraLoading.StableStep==step,"loading checkpoint re-entry "+step,report);
                    }
                    image=new Texture2D(2,2,TextureFormat.RGB24,false);
                    image.SetPixels(new[]{Color.red,Color.green,Color.blue,Color.white});image.Apply();
                    byte[] png=image.EncodeToPNG();
                    foreach(string task in new[]{"entrance","bridge","meadow"})
                    {
                        var checkpoint=new BridgePhotoCheckpoint{hasReference=true,aligned=true,handJoined=task=="meadow",fov=60,ev100=10,savedFocus=3.5f,savedAperture=8,savedShutter=.004f};
                        roll=ParkFilmStore.SaveTask(id,task,checkpoint,png);
                        var reopened=ParkFilmStore.LoadActive();var settings=reopened.Task(task);
                        Check(settings.savedFocus==3.5f && settings.savedAperture==8 && settings.savedShutter==.004f && reopened.FilmIso==400,"saved settings and ISO re-entry "+task,report);
                        Check(ParkFilmStore.MatchingExposureBytes(png,ParkFilmStore.ReadTaskReference(id,task)),"reference bytes round trip "+task,report);
                        int before=roll.frames.Count;bool failOnce=true;int writes=0;
                        var pending=new ParkExposurePersistence(ParkFilmStore.Load,(current,record,bytes)=>{
                            writes++;
                            if(failOnce)
                            {
                                failOnce=false;
                                // Reproduce bytes committed before a failed manifest, inside this isolated root.
                                File.WriteAllBytes(Path.Combine(ParkFilmStore.Root,id,record.id+".frame"),bytes);
                                throw new IOException("isolated manifest-failure injection");
                            }
                            return ParkFilmStore.SaveExposure(current,record,bytes);
                        });
                        var frame=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),taskId=task,iso=400,accepted=true,joinedHands=task=="meadow",capturedUtc=DateTime.UtcNow.ToString("o")};
                        pending.Queue(frame,png);ParkFilmRoll committed;ParkPhotoRecord saved;Exception error;
                        Check(!pending.TryCommit(roll,out committed,out saved,out error) && pending.HasPending && ParkFilmStore.Load(id).frames.Count==before,"failed exposure retains frame and pending "+task,report);
                        Check(pending.TryCommit(roll,out committed,out saved,out error) && writes==2 && !pending.HasPending,"retry reuses orphan and commits "+task,report);
                        roll=ParkFilmStore.LoadActive();
                        Check(roll.frames.Count==before+1 && roll.frames[before].id==frame.id && roll.Task(task).captured,"exactly one frame committed "+task,report);
                        byte[] visible;string reason;
                        Check(!ParkFilmStore.TryReadDevelopedFrame(id,before+1,out visible,out reason) && visible==null,"undeveloped gallery remains blocked "+task,report);
                    }
                    roll=ParkFilmStore.Load(id);
                    Check(roll.frames.Count==3 && roll.Remaining==0 && roll.state==ParkFilmState.AwaitingDevelopment,"whole roll sealed after three exposures",report);
                    Check(roll.darkroom!=null && roll.darkroom.workflowVersion==3 && roll.darkroom.phase==PhotoChapter.FilmPhase.Exposed && roll.darkroom.printObjects!=null,"whole roll enters real initial darkroom checkpoint",report);
                    Check(File.Exists(Path.Combine(directory,"active-roll.json.bak")) && File.Exists(Path.Combine(directory,id,"roll.json.bak")),"active metadata and roll backups exist",report);
                }
                report.Add("LIMITATION: Encoded frames are a 2x2 fixture, not native camera captures. Full darkroom controller/physical print acceptance is not exercised: workflow v3 requires bound print props and a WBMain input context; null-host PhotoWorld3D skips persistence and cannot drive physical printing. No phases were manually advanced.");
                return string.Join("\n",report.ToArray());
            }
            finally
            {
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);
                string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA"))+Path.DirectorySeparatorChar;
                if(directory.StartsWith(allowed,StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
    }
}
