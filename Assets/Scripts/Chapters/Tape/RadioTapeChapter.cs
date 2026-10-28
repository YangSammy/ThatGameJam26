namespace MemoryWhitebox
{
    // Current radio chapter: inspect, replace, load and play an intact tape.
    public sealed class RadioTapeChapter : Chapter
    {
        public readonly TapeInspectionState Tape=new TapeInspectionState();
        public const int CommissionCount=3;
        public bool SourceLoaded,AuditionComplete,ProtectionDiscovered;
        public float AuditionSeconds;
        public int CommissionIndex {get;private set;}
        public int WorkingTapeIndex=-1;
        public int WorkbenchTapeIndex=-1;
        public UnityEngine.Vector3 WorkbenchEuler;
        public bool LooseStripReady;
        [System.Serializable] sealed class Checkpoint
        {
            public int version=7;
            public bool sourceLoaded,auditionComplete,protectionDiscovered;
            public float auditionSeconds;
            public int benchTape=-1;
            public UnityEngine.Vector3 benchEuler;
            public bool looseStrip;
            public int workingTape=-1;
            public int commission;
            public int[] flags=new int[3];
            public float[] positions=new float[3];
            public bool[] broken=new bool[3];
            public string[] coverage=new string[3];
        }
        public override string CaptureCheckpoint()
        {
            var node=new Checkpoint{sourceLoaded=SourceLoaded,auditionComplete=AuditionComplete,protectionDiscovered=ProtectionDiscovered,auditionSeconds=AuditionSeconds,commission=CommissionIndex,workingTape=WorkingTapeIndex,benchTape=WorkbenchTapeIndex,benchEuler=WorkbenchEuler,looseStrip=LooseStripReady};
            for(int i=0;i<3;i++)
            {
                var c=Tape.Candidates[i];node.positions[i]=c.Position;node.broken[i]=c.Broken;
                node.flags[i]=(c.LeftUsed?1:0)|(c.RightUsed?2:0)|(c.StartReached?4:0)|(c.EndReached?8:0)|(c.HoleA?16:0)|(c.HoleB?32:0);
                var bits=new char[TapeInspectionState.Samples];for(int j=0;j<bits.Length;j++)bits[j]=c.Seen[j]?'1':'0';
                node.coverage[i]=new string(bits);
            }
            return UnityEngine.JsonUtility.ToJson(node);
        }
        public override void RestoreCheckpoint(string json)
        {
            if(string.IsNullOrEmpty(json))return;
            var node=UnityEngine.JsonUtility.FromJson<Checkpoint>(json);
            if(node==null || (node.version<1 || node.version>7) || node.positions==null || node.positions.Length!=3 || node.broken==null || node.broken.Length!=3 || node.coverage==null || node.coverage.Length!=3)
                throw new System.IO.InvalidDataException("Invalid tape checkpoint.");
            if(node.commission<0 || node.commission>3)throw new System.IO.InvalidDataException("Invalid tape commission.");
            CommissionIndex=node.version>=3?System.Math.Min(node.commission,CommissionCount):0;
            SourceLoaded=node.version>=6 && node.sourceLoaded;
            AuditionComplete=node.version>=6?node.auditionComplete:node.workingTape>=0 || node.benchTape>=0 || node.commission>0;
            ProtectionDiscovered=node.version>=6 && node.protectionDiscovered;
            AuditionSeconds=node.version>=6 && !float.IsNaN(node.auditionSeconds) && !float.IsInfinity(node.auditionSeconds)?UnityEngine.Mathf.Max(0,node.auditionSeconds):0;
            WorkingTapeIndex=node.version>=4?node.workingTape:-1;
            WorkbenchTapeIndex=node.version>=5?node.benchTape:-1;WorkbenchEuler=node.benchEuler;LooseStripReady=node.version>=5 && node.looseStrip;
            if(WorkbenchTapeIndex< -1 || WorkbenchTapeIndex>=3 || float.IsNaN(WorkbenchEuler.sqrMagnitude) || float.IsInfinity(WorkbenchEuler.sqrMagnitude))throw new System.IO.InvalidDataException("Invalid workbench checkpoint.");
            if(WorkingTapeIndex< -1 || WorkingTapeIndex>=3)throw new System.IO.InvalidDataException("Invalid loaded tape.");
            for(int i=0;i<3;i++)
            {
                if(float.IsNaN(node.positions[i]) || node.positions[i]<0 || node.positions[i]>TapeInspectionState.Duration || node.coverage[i]==null || node.coverage[i].Length!=TapeInspectionState.Samples)
                    throw new System.IO.InvalidDataException("Invalid tape coverage.");
                var c=Tape.Candidates[i];
                int flags=node.version>=2 && node.flags!=null && node.flags.Length==3?node.flags[i]:0;
                c.LeftUsed=(flags&1)!=0;c.RightUsed=(flags&2)!=0;c.StartReached=(flags&4)!=0;c.EndReached=(flags&8)!=0;c.HoleA=(flags&16)!=0;c.HoleB=(flags&32)!=0;
                c.Position=node.positions[i];c.Broken=node.broken[i];c.Scanned=0;
                for(int j=0;j<c.Seen.Length;j++)
                {
                    if(node.coverage[i][j]!='0' && node.coverage[i][j]!='1')throw new System.IO.InvalidDataException("Invalid tape sample.");
                    c.Seen[j]=node.coverage[i][j]=='1';if(c.Seen[j])c.Scanned++;
                }
            }
            Tape.Selected=-1;Tape.SourceVerified=false;Tape.EndSpindle();
            // A completed v6 MVP represents the first copy, not a loaded second original.
            if(node.version==6 && CommissionIndex>0)PrepareNextCopy();
            Completed=CommissionIndex==CommissionCount;
        }
        public override string Help => "LMB drag  Tape to slot / Release outside  Return\nE  Inspect / Hold LMB on reel + Scroll  Check\nEsc / RMB  Cancel drag";
        public override void Draw(){}
        public void VerifyPlayback(){} // Loading alone no longer completes the chapter.
        public void SubmitRecording(bool accepted)
        {
            if(!accepted || Completed || CommissionIndex>=CommissionCount)return;
            CommissionIndex++;if(CommissionIndex==CommissionCount)Finish();else PrepareNextCopy();
        }
        void PrepareNextCopy()
        {
            SourceLoaded=false;AuditionComplete=false;AuditionSeconds=0;
            WorkingTapeIndex=WorkbenchTapeIndex=-1;WorkbenchEuler=UnityEngine.Vector3.zero;LooseStripReady=false;
            Tape.Selected=-1;Tape.SourceVerified=false;Tape.EndSpindle();
            foreach(var c in Tape.Candidates)
            {
                if(c.Broken)continue; // Known bad candidates remain marked; no repeated guessing.
                c.LeftUsed=c.RightUsed=c.StartReached=c.EndReached=c.HoleA=c.HoleB=false;
                c.Position=TapeInspectionState.Duration*.5f;c.Scanned=0;System.Array.Clear(c.Seen,0,c.Seen.Length);
            }
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
