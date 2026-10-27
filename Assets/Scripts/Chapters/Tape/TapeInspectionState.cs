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
            public float Position;
            public float[] Breaks;
            public bool Broken;
            public readonly bool[] Seen=new bool[Samples];
            public int Scanned;
            public Candidate(params float[] breaks){Breaks=breaks;}
        }
        public readonly Candidate[] Candidates={new Candidate(2.2f,5.8f),new Candidate(1.4f,6.3f),new Candidate()};
        public int Selected=-1;
        public bool SourceVerified;
        int heldSpindle=-1;
        public float InspectionProgress=>Selected<0?0:Candidates[Selected].Scanned/(float)Samples;
        public float InspectPosition=>Selected<0?0:Candidates[Selected].Position;
        public void BeginSpindle(int spindle){heldSpindle=spindle;}
        public void EndSpindle(){heldSpindle=-1;}
        public void Turn(float delta)
        {
            if(SourceVerified || Selected<0 || heldSpindle<0 || heldSpindle>1)return;
            var cassette=Candidates[Selected];
            if(cassette.Broken || Mathf.Abs(delta)<.001f)return;
            float old=cassette.Position;
            float next=Mathf.Clamp(old+Mathf.Clamp(delta,-3,3)*.28f*(heldSpindle==0?-1:1),0,Duration);
            if(Mathf.Abs(next-old)<.0001f)return;
            float lo=Mathf.Min(old,next),hi=Mathf.Max(old,next);
            foreach(float brokenAt in cassette.Breaks)
                if(brokenAt>=lo && brokenAt<=hi){cassette.Position=brokenAt;cassette.Broken=true;WBMain.Tone(95,.15f);return;}
            cassette.Position=next;
            for(int i=Mathf.Max(0,Mathf.FloorToInt(lo/Duration*Samples));i<Mathf.Min(Samples,Mathf.CeilToInt(hi/Duration*Samples));i++)
                if(!cassette.Seen[i]){cassette.Seen[i]=true;cassette.Scanned++;}
            if(cassette.Scanned>=Samples && next>=Duration-.001f)
            {SourceVerified=true;heldSpindle=-1;WBMain.Tone(440,.15f);}
        }
    }
}
