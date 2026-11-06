using System;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    // Owns only an unacknowledged exposure. Delegates keep recovery checks away from user files.
    public sealed class ParkExposurePersistence
    {
        readonly Func<string,ParkFilmRoll> load;
        readonly Func<ParkFilmRoll,ParkPhotoRecord,byte[],ParkFilmRoll> save;
        ParkPhotoRecord pending;
        byte[] encoded;
        public bool HasPending=>pending!=null;
        public ParkExposurePersistence(Func<string,ParkFilmRoll> load,Func<ParkFilmRoll,ParkPhotoRecord,byte[],ParkFilmRoll> save)
        {this.load=load??throw new ArgumentNullException(nameof(load));this.save=save??throw new ArgumentNullException(nameof(save));}
        public void Queue(ParkPhotoRecord record,byte[] bytes)
        {
            if(HasPending)throw new InvalidOperationException("An exposure is still awaiting save.");
            if(record==null || bytes==null || bytes.Length==0)throw new InvalidDataException("No exposure data.");
            pending=record;encoded=bytes;
        }
        public bool TryCommit(ParkFilmRoll current,out ParkFilmRoll committed,out ParkPhotoRecord record,out Exception error)
        {
            committed=null;record=null;error=null;
            if(!HasPending)return false;
            try
            {
                if(current==null)throw new InvalidDataException("No film loaded.");
                // A previous manifest write may have committed before reporting an IO failure.
                var stored=load(current.id);
                if(stored==null || stored.id!=current.id)throw new InvalidDataException("Film identity mismatch.");
                var existing=stored.frames.Find(frame=>frame!=null && frame.id==pending.id);
                if(existing!=null)
                {
                    if(JsonUtility.ToJson(existing)!=JsonUtility.ToJson(pending))throw new InvalidDataException("Exposure ID belongs to a different record.");
                    committed=stored;record=existing;
                }
                else
                {
                    if(stored.frames.Count!=current.frames.Count)throw new InvalidOperationException("Film changed in another session; reload before continuing.");
                    committed=save(stored,pending,encoded);record=pending;
                }
                pending=null;encoded=null;return true;
            }
            catch(Exception ex){error=ex;return false;}
        }
    }

    public sealed partial class ParkPhotoPrototype
    {
        ParkExposurePersistence exposurePersistence;
        bool leavingSavedOnly;
        ParkExposureEncoder exposureEncoder;
        bool exposureEncodingDisposed;
        bool HasPendingExposure=>shutterSampling || (exposurePersistence!=null && exposurePersistence.HasPending) || (exposureEncoder!=null && exposureEncoder.HasPending);
        void BeginExposureEncoding(ParkPhotoRecord record,byte[] rawRgb,int width,int height)
        {
            if(exposureEncodingDisposed || leavingSavedOnly || changingScene)throw new InvalidOperationException("Photography is leaving.");
            if(HasPendingExposure)throw new InvalidOperationException("An exposure is still pending.");
            if(exposureEncoder==null)exposureEncoder=new ParkExposureEncoder();
            exposureEncoder.Start(record,rawRgb,width,height);Notify("Saving exposure...",3);
        }
        void TickExposureEncoding()
        {
            if(exposureEncodingDisposed || leavingSavedOnly || changingScene || exposureEncoder==null)return;
            ParkPhotoRecord record;byte[] png;Exception error;
            if(!exposureEncoder.TryTake(out record,out png,out error))return;
            if(error!=null)
            {
                filmError="Encoding failed. Press shutter to retry, or leave with saved progress.";
                Notify(filmError,7);Debug.LogWarning("Park exposure encoding: "+error.Message);return;
            }
            SavePendingExposure(record,png);
        }
        void CancelExposureEncoding()
        {exposureEncodingDisposed=true;if(exposureEncoder!=null)exposureEncoder.Cancel();}
        bool SavePendingExposure(ParkPhotoRecord record,byte[] bytes)
        {
            if(exposurePersistence==null)exposurePersistence=new ParkExposurePersistence(ParkFilmStore.Load,ParkFilmStore.SaveExposure);
            exposurePersistence.Queue(record,bytes);
            return RetryPendingExposure();
        }
        bool RetryPendingExposure()
        {
            if(exposureEncodingDisposed || leavingSavedOnly || changingScene)return false;
            if(exposureEncoder!=null && exposureEncoder.HasPending)
            {
                if(exposureEncoder.HasFailed)
                {
                    exposureEncoder.Retry();filmError="";Notify("Retrying exposure encoding...",3);
                }
                else Notify("Saving exposure...",3);
                return false;
            }
            if(exposurePersistence==null || !exposurePersistence.HasPending)return false;
            ParkFilmRoll committed;ParkPhotoRecord record;Exception error;
            if(!exposurePersistence.TryCommit(roll,out committed,out record,out error))
            {
                filmError="Save failed. Press shutter to retry, or leave with saved progress.";
                Notify(filmError,7);Debug.LogWarning("Park film save: "+(error!=null?error.Message:"Exposure unavailable."));return false;
            }
            roll=committed;filmError="";ExposureCommitted(record);return true;
        }
        bool TryLoadCommittedForExit()
        {
            try
            {
                // Never create a replacement roll when an existing save cannot be read.
                var committed=roll!=null?ParkFilmStore.Load(roll.id):ParkFilmStore.LoadActive();
                if(committed==null)throw new InvalidDataException("No committed film available.");
                roll=committed;return true;
            }
            catch(Exception ex){Notify("Return unavailable. Film progress kept.",6);Debug.LogWarning("Park memory return: "+ex.Message);return false;}
        }
        public bool LeaveWithSavedProgress()
        {
            if(!Paused || changingScene || returnUiGate || !InputContext.Allows(InputContextId.Paused))return false;
            if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))
            {Notify("Return unavailable. Film files kept.",5);return false;}
            // An explicit exit is allowed even if both metadata copies are unreadable.
            // House will report that existing roll as unavailable rather than replacing it.
            TryLoadCommittedForExit();leavingSavedOnly=true;changingScene=true;
            ParkFilmRoute.EnterDarkroomOnLoad=false;SyncParkInput();InputContext.Handoff();
            try{BrushSceneTransition.LoadScene(ParkFilmRoute.HouseScenePath);return true;}
            catch(Exception ex){leavingSavedOnly=false;changingScene=false;Notify("Return unavailable. Film files kept.",5);Debug.LogWarning(ex.Message);return false;}
        }
    }
}
