using System;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Pure checkpoint and transition tests: no scene, transport, or player save access.
    public static class IndependentTapeModelChecks
    {
        [Serializable] sealed class Payload
        {
            public int flags;
            public float position;
            public bool broken;
            public string coverage;
        }

        [Serializable] sealed class Save
        {
            public int version,commission;
            public bool sourceLoaded,auditionComplete,completed;
            public int workingTape=-1,benchTape=-1;
            public int[] flags;
            public float[] positions;
            public bool[] broken;
            public string[] coverage;
            public ItemSaveRecord[] items;
            public string recordingTapeId;
        }

        static readonly string[] Ids={"tape.work.1","tape.work.2","tape.work.3","tape.work.4","tape.work.5"};

        [UnityEditor.MenuItem("Tools/Validation/Independent Work Tape Model")]
        public static void Run()
        {
            var chapter=new RadioTapeChapter();
            Check(chapter.Tape.Candidates.Length==5,"Expected five physical work tape candidates.");
            Check(!chapter.NeedsLegacyRecordingBinding,"A fresh chapter must not assume a recording tape identity.");
            Check(chapter.Tape.Candidates[0].Breaks.Length>0 && chapter.Tape.Candidates[1].Breaks.Length>0,
                "The two original damaged tapes must keep their breaks.");
            Check(chapter.Tape.Candidates[2].Breaks.Length==0 && chapter.Tape.Candidates[3].Breaks.Length==0 &&
                chapter.Tape.Candidates[4].Breaks.Length==0,"Three independent tapes must be usable.");
            DetachedSubmissionsPreservePreparation();
            RecordsFollowIdentity();
            RecordedTapeCanBeResealedAfterReload();
            LegacyCopiesRetainCurrentWork();
            LegacyRecordingRecoversPeeledTape();
            RejectCorruptOwnership();
            Debug.Log("Independent work tape model checks passed.");
        }

        static void DetachedSubmissionsPreservePreparation()
        {
            var chapter=new RadioTapeChapter();
            foreach(int i in new[]{2,3,4})Prepare(chapter.Tape.Candidates[i]);
            var partial=chapter.Tape.Candidates[1];
            partial.Position=3;partial.LeftUsed=true;partial.Seen[175]=true;partial.Scanned=1;
            chapter.SourceLoaded=chapter.AuditionComplete=chapter.LooseStripReady=true;
            chapter.AuditionSeconds=12;
            chapter.WorkingTapeIndex=4;chapter.WorkbenchTapeIndex=2;
            Check(chapter.BindRecordingTape(4),"Prepared tape must accept recording.");
            chapter.WorkingTapeIndex=3;
            Check(!chapter.BindRecordingTape(3),"A loaded replacement must not inherit an existing recording.");
            Expect<InvalidOperationException>(()=>chapter.PrepareSubmission(out _));
            chapter.WorkingTapeIndex=4;
            string first=chapter.PrepareSubmission(out bool complete);
            Check(!complete && chapter.CommissionIndex==0 && !chapter.IsArchived(4) && chapter.RecordingTapeIndex==4 &&
                chapter.WorkingTapeIndex==4 && chapter.LooseStripReady,"Preparing the commit must leave the live chapter intact.");
            chapter.RestoreCheckpoint(first);
            Check(chapter.FinishedCommission(4)==0 && chapter.Tape.Candidates[2].Prepared && chapter.Tape.Candidates[3].Prepared,
                "First commit must archive the recorded tape and preserve preparation on other tapes.");
            Check(partial.Position==3 && partial.LeftUsed && partial.Seen[175] && partial.Scanned==1,
                "First commit must keep partial inspection on other tapes.");
            Check(!chapter.SourceLoaded && !chapter.AuditionComplete && chapter.AuditionSeconds==0 &&
                !chapter.LooseStripReady && chapter.WorkbenchTapeIndex==-1 && chapter.WorkingTapeIndex==-1 &&
                chapter.RecordingTapeIndex==-1,"Next commission must start without transient state.");
            Check(!chapter.BindRecordingTape(4),"Archived tape must refuse recording.");
            chapter.WorkingTapeIndex=4;
            Expect<InvalidOperationException>(()=>chapter.PrepareSubmission(out _));
            chapter.WorkingTapeIndex=0;
            Expect<InvalidOperationException>(()=>chapter.PrepareSubmission(out _));
            foreach(int i in new[]{2,3})
            {
                chapter.WorkingTapeIndex=i;
                Check(chapter.BindRecordingTape(i),"An unused tape must bind the new copy.");
                chapter.RestoreCheckpoint(chapter.PrepareSubmission(out complete));
            }
            Check(complete && chapter.Completed && chapter.FinishedCommission(4)==0 && chapter.FinishedCommission(2)==1 &&
                chapter.FinishedCommission(3)==2,"Three commissions must own three distinct physical tapes.");
        }

        static void RecordsFollowIdentity()
        {
            var chapter=new RadioTapeChapter();
            Prepare(chapter.Tape.Candidates[4]);
            chapter.Tape.Candidates[0].Broken=true;
            chapter.WorkingTapeIndex=4;chapter.WorkbenchTapeIndex=2;
            Check(chapter.BindRecordingTape(4),"Recording must bind to the chosen tape.");
            var reordered=new RadioTapeChapter();
            reordered.BindItemIds(new[]{Ids[4],Ids[0],Ids[3],Ids[1],Ids[2]});
            reordered.RestoreCheckpoint(chapter.CaptureCheckpoint());
            Check(reordered.WorkingTapeIndex==0 && reordered.WorkbenchTapeIndex==4 && reordered.RecordingTapeIndex==0 &&
                reordered.Tape.Candidates[0].Prepared && reordered.Tape.Candidates[1].Broken,
                "Working, bench, recording and preparation identities must survive a reordered scene array.");
            reordered.RestoreCheckpoint(reordered.PrepareSubmission(out _));
            chapter.RestoreCheckpoint(reordered.CaptureCheckpoint());
            Check(chapter.FinishedCommission(4)==0,"Finished ownership must follow the recorded tape ID.");
        }

        static void RecordedTapeCanBeResealedAfterReload()
        {
            var chapter=new RadioTapeChapter();
            var tape=chapter.Tape.Candidates[4];
            Prepare(tape);
            chapter.WorkingTapeIndex=4;
            Check(chapter.BindRecordingTape(4),"Prepared tape must accept a recording.");
            chapter.WorkingTapeIndex=-1;
            chapter.WorkbenchTapeIndex=4;
            tape.HoleA=false;

            var reload=new RadioTapeChapter();
            reload.RestoreCheckpoint(chapter.CaptureCheckpoint());
            var restoredTape=reload.Tape.Candidates[4];
            Check(reload.RecordingTapeIndex==4 && reload.WorkbenchTapeIndex==4 && restoredTape.Intact &&
                !restoredTape.HoleA && restoredTape.HoleB,"Reload must keep recording ownership and the peeled seal.");

            reload.WorkbenchTapeIndex=-1;
            reload.WorkingTapeIndex=4;
            Expect<InvalidOperationException>(()=>reload.PrepareSubmission(out _));
            restoredTape.HoleA=true;
            reload.RestoreCheckpoint(reload.PrepareSubmission(out _));
            Check(reload.FinishedCommission(4)==0,"Resealing must allow the recorded tape to be submitted.");
        }

        static void LegacyCopiesRetainCurrentWork()
        {
            foreach(int version in new[]{7,8})
            for(int completed=0;completed<=3;completed++)
            {
                var payloads=new[]{
                    new Payload{position=2.2f,broken=true,coverage=new string('0',400)},
                    new Payload{position=4,coverage=new string('0',400)},
                    new Payload{position=7,flags=63,coverage=new string('1',400)}
                };
                var save=new Save{version=version,commission=completed,workingTape=2,sourceLoaded=true,auditionComplete=true};
                if(version==8)
                {
                    save.items=new ItemSaveRecord[3];
                    for(int i=0;i<3;i++)
                        save.items[2-i]=new ItemSaveRecord{instanceId=Ids[i],definitionId="tape.work",ownerChapterId=ChapterIds.Tape,
                            locationId=i==2?"tape.slot.b":"tape.home",payload=JsonUtility.ToJson(payloads[i])};
                }
                else
                {
                    save.positions=Array.ConvertAll(payloads,p=>p.position);
                    save.flags=Array.ConvertAll(payloads,p=>p.flags);
                    save.broken=Array.ConvertAll(payloads,p=>p.broken);
                    save.coverage=Array.ConvertAll(payloads,p=>p.coverage);
                }
                var chapter=new RadioTapeChapter();
                chapter.BindItemIds(new[]{Ids[0],Ids[1],Ids[2]});
                chapter.RestoreCheckpoint(JsonUtility.ToJson(save));
                Check(chapter.NeedsLegacyRecordingBinding==(completed<3),
                    "Only a legacy chapter with work remaining may infer the original recording tape.");
                var reload=new RadioTapeChapter();
                reload.RestoreCheckpoint(chapter.CaptureCheckpoint());
                Check(!reload.NeedsLegacyRecordingBinding,"A current checkpoint must not guess a recording tape.");
                Check(reload.CommissionIndex==completed && reload.Completed==(completed==3) && reload.Tape.Candidates[0].Broken &&
                    reload.Tape.Candidates[2].Position==7 && reload.Tape.Candidates[2].Prepared,"Legacy tape preparation and completion must survive migration.");
                Check(reload.FinishedCommission(3)==(completed>0?0:-1) && reload.FinishedCommission(4)==(completed>1?1:-1) &&
                    reload.FinishedCommission(2)==(completed==3?2:-1) && reload.WorkingTapeIndex==(completed==3?-1:2),
                    "Old finished copies must reserve the added tapes before the original good tape.");
            }
        }

        static void LegacyRecordingRecoversPeeledTape()
        {
            var save=new Save{version=8,commission=1,items=new ItemSaveRecord[3]};
            for(int i=0;i<3;i++)
            {
                var payload=new Payload{position=4,flags=i==2?47:0,broken=false,
                    coverage=new string(i==2?'1':'0',400)};
                save.items[i]=new ItemSaveRecord{instanceId=Ids[i],definitionId="tape.work",ownerChapterId=ChapterIds.Tape,
                    locationId=i==2?"tape.workbench":"tape.home",payload=JsonUtility.ToJson(payload)};
            }
            var chapter=new RadioTapeChapter();
            chapter.BindItemIds(new[]{Ids[2],Ids[0],Ids[1],Ids[3],Ids[4]});
            chapter.RestoreCheckpoint(JsonUtility.ToJson(save));
            Check(chapter.BindLegacyRecordingTape(),"Legacy PCM must bind the original tape with a peeled seal.");
            Check(chapter.RecordingTapeIndex==0 && !chapter.NeedsLegacyRecordingBinding &&
                !chapter.Tape.Candidates[0].HoleA && chapter.Tape.Candidates[0].HoleB,
                "Legacy ownership must follow the original tape ID and preserve the peeled seal.");
            var reload=new RadioTapeChapter();
            reload.RestoreCheckpoint(chapter.CaptureCheckpoint());
            Check(reload.RecordingTapeIndex==2 && !reload.NeedsLegacyRecordingBinding && !reload.BindLegacyRecordingTape(),
                "After migration the recorded tape must keep its identity through a current checkpoint.");
        }

        static void RejectCorruptOwnership()
        {
            var chapter=new RadioTapeChapter();
            Prepare(chapter.Tape.Candidates[2]);chapter.WorkingTapeIndex=2;
            var save=JsonUtility.FromJson<Save>(chapter.PrepareSubmission(out _));
            save.items[3].locationId="tape.finished.1";
            Expect<InvalidDataException>(()=>chapter.RestoreCheckpoint(JsonUtility.ToJson(save)));
            save.items[3].locationId="tape.home";save.items[2].locationId="tape.home";
            Expect<InvalidDataException>(()=>chapter.RestoreCheckpoint(JsonUtility.ToJson(save)));
            save.items[2].locationId="tape.finished.1";save.recordingTapeId=Ids[2];
            Expect<InvalidDataException>(()=>chapter.RestoreCheckpoint(JsonUtility.ToJson(save)));
            save.recordingTapeId=null;save.completed=true;
            Expect<InvalidDataException>(()=>chapter.RestoreCheckpoint(JsonUtility.ToJson(save)));
            save.completed=false;save.items[2].instanceId=save.items[3].instanceId;
            Expect<InvalidDataException>(()=>chapter.RestoreCheckpoint(JsonUtility.ToJson(save)));
            Check(chapter.CommissionIndex==0 && chapter.WorkingTapeIndex==2 && !chapter.IsArchived(2),
                "Rejected checkpoint must leave live ownership intact.");
        }

        static void Prepare(TapeInspectionState.Candidate tape)
        {
            tape.LeftUsed=tape.RightUsed=tape.StartReached=tape.EndReached=tape.HoleA=tape.HoleB=true;
            for(int i=0;i<tape.Seen.Length;i++)tape.Seen[i]=true;
            tape.Scanned=tape.Seen.Length;
        }

        static void Expect<T>(Action action) where T:Exception
        {
            try{action();}
            catch(T){return;}
            throw new InvalidOperationException("Expected "+typeof(T).Name+".");
        }

        static void Check(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
        }
    }
}
