using System;
using UnityEngine;
namespace MemoryWhitebox
{
    // Inspection state only: transport, playback and loading belong to HouseRadioDeck.
    public sealed class TapeInspectionState
    {
        public const float Duration=8;
        public const int Samples=400;
        [Serializable] public sealed class Candidate
        {
            public float Position=Duration*.5f;
            public bool LeftUsed,RightUsed,StartReached,EndReached,HoleA,HoleB;
            // Legacy inspection fields are retained solely for checkpoint compatibility.
            public bool Intact=>true;
            public bool Prepared=>HoleA && HoleB;
            public float[] Breaks;
            public bool Broken {get=>false;set{}}
            public readonly bool[] Seen=new bool[Samples];
            public int Scanned;
            public Candidate(params float[] breaks){Breaks=breaks;}
        }
        public readonly Candidate[] Candidates={
            new Candidate(BreaksFor(0)),new Candidate(BreaksFor(1)),new Candidate(),
            new Candidate(),new Candidate()
        };
        internal static float[] BreaksFor(int index)=>Array.Empty<float>();
        public int Selected=-1;
        public bool SourceVerified;
        int heldSpindle=-1;
        public float InspectionProgress=>Selected<0?0:Candidates[Selected].Scanned/(float)Samples;
        public float InspectPosition=>Selected<0?0:Candidates[Selected].Position;
        public void BeginSpindle(int spindle){heldSpindle=spindle;}
        public void EndSpindle(){heldSpindle=-1;}
        public void Turn(float delta){} // Retired inspection API; old callers cannot change preparation.
    }
}
