using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class RadioTapeChapter : Chapter
    {
        public const int WorkTapeCount=5;
        public const int CommissionCount=3;
        const int LegacyTapeCount=3;
        const int CheckpointVersion=9;
        const int InspectionFlags=15;
        const int PreparedFlags=63;
        const string Home="tape.home", SlotB="tape.slot.b", Bench="tape.workbench";
        static readonly string[] DefaultItemIds={"tape.work.1","tape.work.2","tape.work.3","tape.work.4","tape.work.5"};
        static readonly string[] FinishedLocations={"tape.finished.1","tape.finished.2","tape.finished.3"};

        public readonly TapeInspectionState Tape=new TapeInspectionState();
        public bool SourceLoaded,AuditionComplete,ProtectionDiscovered;
        public float AuditionSeconds;
        public int CommissionIndex {get;private set;}
        public int RecordingTapeIndex {get;private set;}=-1;
        public bool NeedsLegacyRecordingBinding {get;private set;}
        public int WorkingTapeIndex=-1;
        public int WorkbenchTapeIndex=-1;
        public Vector3 WorkbenchEuler;
        public bool LooseStripReady;
        string[] itemIds=(string[])DefaultItemIds.Clone();
        readonly int[] finishedCommissions=NewFinishedCommissions();

        static int[] NewFinishedCommissions()
        {
            var copies=new int[WorkTapeCount];
            for(int i=0;i<copies.Length;i++)copies[i]=-1;
            return copies;
        }

        static bool ValidTapeIndex(int index)=>index>=0 && index<WorkTapeCount;
        public int FinishedCommission(int index)=>ValidTapeIndex(index)?finishedCommissions[index]:-1;
        public bool IsArchived(int index)=>FinishedCommission(index)>=0;
        bool CanUsePreparedTape(int index)=>ValidTapeIndex(index) && !IsArchived(index) &&
            Tape.Candidates[index].Prepared;

        public bool BindRecordingTape(int index)
        {
            if(Completed || !CanUsePreparedTape(index) || (RecordingTapeIndex>=0 && RecordingTapeIndex!=index))return false;
            RecordingTapeIndex=index;NeedsLegacyRecordingBinding=false;return true;
        }

        public bool BindLegacyRecordingTape()
        {
            if(!NeedsLegacyRecordingBinding || Completed || RecordingTapeIndex>=0)return false;
            int index=Array.IndexOf(itemIds,DefaultItemIds[2]);
            if(!ValidTapeIndex(index) || IsArchived(index))return false;
            var tape=Tape.Candidates[index];
            // Legacy PCM proves this tape was recorded before a seal may have been removed.
            RecordingTapeIndex=index;
            NeedsLegacyRecordingBinding=false;
            return true;
        }

        public void ClearRecordingTapeBinding()
        {
            RecordingTapeIndex=-1;NeedsLegacyRecordingBinding=false;
        }

        public void BindItemIds(string[] ids)
        {
            if(ids==null || (ids.Length!=WorkTapeCount && ids.Length!=LegacyTapeCount))
                throw new ArgumentException("Five work tape IDs are required.");
            var bound=(string[])DefaultItemIds.Clone();
            Array.Copy(ids,bound,ids.Length);
            var unique=new HashSet<string>(StringComparer.Ordinal);
            foreach(string id in bound)
                if(string.IsNullOrEmpty(id) || !unique.Add(id))throw new ArgumentException("Invalid work tape IDs.");
            itemIds=bound;
            for(int i=0;i<WorkTapeCount;i++)
            {
                int original=Array.IndexOf(DefaultItemIds,itemIds[i]);
                Tape.Candidates[i].Breaks=TapeInspectionState.BreaksFor(original>=0?original:i);
            }
        }

        [Serializable] sealed class TapeItemPayload
        {
            public int flags;
            public float position;
            public bool broken;
            public string coverage;
        }

        [Serializable] sealed class Checkpoint
        {
            public int version;
            public ItemSaveRecord[] items;
            public bool sourceLoaded,auditionComplete,protectionDiscovered,completed;
            public float auditionSeconds;
            public int benchTape=-1;
            public Vector3 benchEuler;
            public bool looseStrip;
            public int workingTape=-1;
            public int commission;
            public string recordingTapeId;
            // Legacy v1–7 positional records.
            public int[] flags=null;
            public float[] positions=null;
            public bool[] broken=null;
            public string[] coverage=null;
        }

        public override string CaptureCheckpoint()
        {
            var node=new Checkpoint
            {
                version=CheckpointVersion,items=new ItemSaveRecord[WorkTapeCount],
                sourceLoaded=SourceLoaded,auditionComplete=AuditionComplete,protectionDiscovered=ProtectionDiscovered,
                auditionSeconds=AuditionSeconds,commission=CommissionIndex,completed=Completed,
                benchEuler=WorkbenchEuler,looseStrip=LooseStripReady,
                recordingTapeId=ValidTapeIndex(RecordingTapeIndex)?itemIds[RecordingTapeIndex]:null
            };
            for(int i=0;i<WorkTapeCount;i++)
            {
                var c=Tape.Candidates[i];
                var bits=new char[TapeInspectionState.Samples];
                for(int j=0;j<bits.Length;j++)bits[j]=c.Seen[j]?'1':'0';
                var payload=new TapeItemPayload
                {
                    flags=(c.LeftUsed?1:0)|(c.RightUsed?2:0)|(c.StartReached?4:0)|(c.EndReached?8:0)|(c.HoleA?16:0)|(c.HoleB?32:0),
                    position=c.Position,broken=c.Broken,coverage=new string(bits)
                };
                node.items[i]=new ItemSaveRecord
                {
                    instanceId=itemIds[i],definitionId="tape.work",ownerChapterId=ChapterIds.Tape,
                    locationId=IsArchived(i)?FinishedLocations[FinishedCommission(i)]:WorkingTapeIndex==i?SlotB:WorkbenchTapeIndex==i?Bench:Home,
                    payload=JsonUtility.ToJson(payload)
                };
            }
            return JsonUtility.ToJson(node);
        }

        public override void RestoreCheckpoint(string json)
        {
            if(string.IsNullOrEmpty(json))return;
            var node=ReadJson<Checkpoint>(json);
            if(node.version<1 || node.version>CheckpointVersion || node.commission<0 || node.commission>CommissionCount)
                throw new InvalidDataException("Invalid tape checkpoint version or commission.");
            int commission=node.version>=3?node.commission:0;
            bool complete=commission==CommissionCount;
            bool current=node.version==CheckpointVersion;
            if(current && node.completed!=complete)throw new InvalidDataException("Tape completion disagrees with submitted copies.");
            if(!Finite(node.benchEuler.sqrMagnitude))throw new InvalidDataException("Invalid workbench pose.");

            var payloads=new TapeItemPayload[WorkTapeCount];
            var finished=NewFinishedCommissions();
            int working=-1,bench=-1,recording=-1;
            if(node.version>=8)
            {
                int count=current?WorkTapeCount:LegacyTapeCount;
                if(node.items==null || node.items.Length!=count)throw new InvalidDataException("Missing tape item records.");
                var claimedCopies=new bool[CommissionCount];
                foreach(var item in node.items)
                {
                    if(item==null || string.IsNullOrEmpty(item.instanceId) || item.schemaVersion!=1 ||
                        item.definitionId!="tape.work" || item.ownerChapterId!=ChapterIds.Tape)
                        throw new InvalidDataException("Invalid tape item identity.");
                    int i=Array.IndexOf(itemIds,item.instanceId);
                    if(i<0 || payloads[i]!=null)throw new InvalidDataException("Unknown or repeated work tape: "+item.instanceId);
                    if(!current && !IsLegacyTape(i))throw new InvalidDataException("Unexpected work tape in legacy checkpoint: "+item.instanceId);
                    payloads[i]=ReadJson<TapeItemPayload>(item.payload);
                    if(item.locationId==SlotB)
                    {
                        if(working>=0)throw new InvalidDataException("Two tapes in B.");
                        working=i;
                    }
                    else if(item.locationId==Bench)
                    {
                        if(bench>=0)throw new InvalidDataException("Two workbench tapes.");
                        bench=i;
                    }
                    else if(item.locationId!=Home)
                    {
                        int copy=Array.IndexOf(FinishedLocations,item.locationId);
                        if(!current || copy<0 || copy>=commission || claimedCopies[copy])
                            throw new InvalidDataException("Invalid finished work tape location.");
                        claimedCopies[copy]=true;finished[i]=copy;
                    }
                }
                if(current)
                {
                    for(int copy=0;copy<commission;copy++)
                        if(!claimedCopies[copy])throw new InvalidDataException("A finished commission is missing its tape.");
                    if(!string.IsNullOrEmpty(node.recordingTapeId))
                    {
                        recording=Array.IndexOf(itemIds,node.recordingTapeId);
                        if(recording<0 || finished[recording]>=0)throw new InvalidDataException("Invalid recording tape identity.");
                    }
                    if(complete && (working>=0 || bench>=0 || recording>=0))
                        throw new InvalidDataException("A completed tape chapter still has work in progress.");
                }
            }
            else
            {
                if(node.positions==null || node.positions.Length!=LegacyTapeCount ||
                    node.broken==null || node.broken.Length!=LegacyTapeCount ||
                    node.coverage==null || node.coverage.Length!=LegacyTapeCount)
                    throw new InvalidDataException("Invalid legacy tape checkpoint.");
                if(node.workingTape< -1 || node.workingTape>=LegacyTapeCount || node.benchTape< -1 || node.benchTape>=LegacyTapeCount)
                    throw new InvalidDataException("Invalid legacy tape placement.");
                for(int legacy=0;legacy<LegacyTapeCount;legacy++)
                {
                    int i=Array.IndexOf(itemIds,DefaultItemIds[legacy]);
                    if(i<0)throw new InvalidDataException("Legacy tape ID needs explicit migration: "+DefaultItemIds[legacy]);
                    payloads[i]=new TapeItemPayload
                    {
                        flags=node.version>=2 && node.flags!=null && node.flags.Length==LegacyTapeCount?node.flags[legacy]:0,
                        position=node.positions[legacy],broken=node.broken[legacy],coverage=node.coverage[legacy]
                    };
                }
                if(node.version>=4 && node.workingTape>=0)working=Array.IndexOf(itemIds,DefaultItemIds[node.workingTape]);
                if(node.version>=5 && node.benchTape>=0)bench=Array.IndexOf(itemIds,DefaultItemIds[node.benchTape]);
                if(working>=0 && working==bench)throw new InvalidDataException("Legacy tape is in both the workbench and B.");
            }

            // Validate every original record before migration can normalize archived copies.
            for(int i=0;i<WorkTapeCount;i++)
            {
                if(payloads[i]==null)
                {
                    if(current)throw new InvalidDataException("A work tape is missing its preparation.");
                    payloads[i]=new TapeItemPayload{position=TapeInspectionState.Duration*.5f,coverage=new string('0',TapeInspectionState.Samples)};
                }
                ValidatePayload(payloads[i]);
            }
            if(!current)
            {
                int[] archiveOrder={FindOriginalIndex(3),FindOriginalIndex(4),FindOriginalIndex(2)};
                for(int copy=0;copy<commission;copy++)
                {
                    int i=archiveOrder[copy];
                    if(finished[i]>=0)throw new InvalidDataException("Legacy finished copies cannot share a work tape.");
                    finished[i]=copy;
                    var p=payloads[i];
                    p.flags=PreparedFlags;p.broken=false;p.coverage=new string('1',TapeInspectionState.Samples);
                }
            }
            for(int i=0;i<WorkTapeCount;i++)
            {
                if(finished[i]<0 && recording!=i)continue;
                var payload=payloads[i];
                // A recorded tape can be ejected and have a seal removed before submission.
                if(finished[i]>=0 && (payload.flags&48)!=48)
                    throw new InvalidDataException("A finished tape is not prepared.");
            }

            CommissionIndex=commission;Completed=complete;
            SourceLoaded=node.version>=6 && node.sourceLoaded;
            AuditionComplete=node.version>=6?node.auditionComplete:working>=0 || bench>=0 || commission>0;
            ProtectionDiscovered=node.version>=6 && node.protectionDiscovered;
            AuditionSeconds=node.version>=6 && Finite(node.auditionSeconds)?Mathf.Max(0,node.auditionSeconds):0;
            WorkingTapeIndex=working;WorkbenchTapeIndex=bench;WorkbenchEuler=node.benchEuler;
            LooseStripReady=node.version>=5 && node.looseStrip;
            RecordingTapeIndex=recording;
            Array.Copy(finished,finishedCommissions,WorkTapeCount);
            for(int i=0;i<WorkTapeCount;i++)RestoreCandidate(Tape.Candidates[i],payloads[i]);
            ResetInspection();
            // A completed v6 MVP was the first copy; its loaded original did not belong to copy two.
            if(!current && (complete || (node.version==6 && commission>0)))ResetCommission();
            NeedsLegacyRecordingBinding=!current && !complete;
        }

        bool IsLegacyTape(int i)
        {
            int original=Array.IndexOf(DefaultItemIds,itemIds[i]);
            return (original>=0?original:i)<LegacyTapeCount;
        }

        int FindOriginalIndex(int original)
        {
            int index=Array.IndexOf(itemIds,DefaultItemIds[original]);
            return index>=0?index:original;
        }

        static T ReadJson<T>(string json) where T:class
        {
            if(string.IsNullOrEmpty(json))throw new InvalidDataException("Missing tape checkpoint data.");
            try{return JsonUtility.FromJson<T>(json)??throw new InvalidDataException("Invalid tape checkpoint data.");}
            catch(ArgumentException ex){throw new InvalidDataException("Invalid tape checkpoint data.",ex);}
        }

        static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);

        static void ValidatePayload(TapeItemPayload p)
        {
            if(!Finite(p.position) || p.position<0 || p.position>TapeInspectionState.Duration ||
                p.flags<0 || p.flags>PreparedFlags || p.coverage==null || p.coverage.Length!=TapeInspectionState.Samples)
                throw new InvalidDataException("Invalid tape coverage or preparation.");
            foreach(char bit in p.coverage)
                if(bit!='0' && bit!='1')throw new InvalidDataException("Invalid tape sample.");
        }

        static void RestoreCandidate(TapeInspectionState.Candidate candidate,TapeItemPayload p)
        {
            candidate.LeftUsed=(p.flags&1)!=0;candidate.RightUsed=(p.flags&2)!=0;
            candidate.StartReached=(p.flags&4)!=0;candidate.EndReached=(p.flags&8)!=0;
            candidate.HoleA=(p.flags&16)!=0;candidate.HoleB=(p.flags&32)!=0;
            candidate.Position=p.position;candidate.Broken=p.broken;candidate.Scanned=0;
            for(int j=0;j<candidate.Seen.Length;j++)
            {
                candidate.Seen[j]=p.coverage[j]=='1';
                if(candidate.Seen[j])candidate.Scanned++;
            }
        }

        public override string Help => "LMB drag  Tape to slot / Release outside  Return\nE  Inspect / Seal both holes on the mat\nEsc / RMB  Cancel drag";
        public override void Draw(){}
        public void VerifyPlayback(){} // Loading alone no longer completes the chapter.

        public string PrepareSubmission(out bool completed)
            =>PrepareSubmission(WorkingTapeIndex,out completed);

        public string PrepareSubmission(int tapeIndex,out bool completed)
        {
            RequireSubmissionTape(tapeIndex);
            // Stage the transition on a detached model; storage failure leaves the live chapter unchanged.
            var next=new RadioTapeChapter();
            next.BindItemIds(itemIds);next.RestoreCheckpoint(CaptureCheckpoint());
            next.ArchiveSubmission(next.RequireSubmissionTape(tapeIndex));
            completed=next.Completed=next.CommissionIndex==CommissionCount;
            return next.CaptureCheckpoint();
        }

        public void SubmitRecording(bool accepted)
        {
            if(!accepted || Completed || CommissionIndex>=CommissionCount)return;
            ArchiveSubmission(RequireSubmissionTape());
            if(CommissionIndex==CommissionCount)Finish();
        }

        int RequireSubmissionTape()=>RequireSubmissionTape(WorkingTapeIndex);
        int RequireSubmissionTape(int index)
        {
            if(Completed || CommissionIndex>=CommissionCount)throw new InvalidOperationException("All copies are already submitted.");
            if(!CanUsePreparedTape(index))throw new InvalidOperationException("Choose a prepared, unarchived work tape.");
            if(RecordingTapeIndex>=0 && RecordingTapeIndex!=index)throw new InvalidOperationException("Reload the tape that owns this recording.");
            return index;
        }

        void ArchiveSubmission(int index)
        {
            finishedCommissions[index]=CommissionIndex++;
            ResetCommission();
        }

        void ResetInspection()
        {
            Tape.Selected=-1;Tape.SourceVerified=false;Tape.EndSpindle();
        }

        void ResetCommission()
        {
            SourceLoaded=false;AuditionComplete=false;AuditionSeconds=0;
            WorkingTapeIndex=WorkbenchTapeIndex=-1;WorkbenchEuler=Vector3.zero;LooseStripReady=false;
            ClearRecordingTapeBinding();ResetInspection();
        }
    }
    public sealed class RadioTapeWorld : WorldChapter3D
    {
        readonly HouseRadioDeck deck;
        public RadioTapeWorld(HouseRadioDeck deck):base(deck.Chapter,deck.gameObject,deck.view)
        {
            this.deck=deck;ReadViewAnchor();
        }
        public void ReadViewAnchor()
        {
            var anchor=deck.operationView;
            var pose=anchor!=null?anchor.transform:deck.view.transform;
            Eye=pose.position;Look=Eye+pose.forward;Fov=anchor!=null?anchor.fieldOfView:deck.view.fieldOfView;
        }
    }
}
