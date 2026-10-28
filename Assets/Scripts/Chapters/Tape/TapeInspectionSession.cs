using UnityEngine;

namespace MemoryWhitebox
{
    // A tape-only inspection action. Other inspection parts remain presentation-only.
    public sealed class TapeInspectionSession
    {
        readonly HouseRadioDeck deck;
        readonly SafeInspectionSubject subject;
        readonly int index;
        readonly Transform left,right,damage;
        bool changed;
        readonly TapePreparationView preparation;
        public bool Preparing=>preparation.Busy;
        public bool Handle(EventType type,int button,Ray ray,Camera camera)=>preparation.Handle(type,button,ray,camera);
        public void Cancel()=>preparation.Cancel();
        public string ActionHint=>preparation.Hint;
        public void DrawPreparationHints(Camera camera,GUIStyle style)=>preparation.DrawHints(camera,style);
        TapeInspectionState.Candidate Data=>deck.Chapter.Tape.Candidates[index];
        public string Status=>Data.Broken?"BREAK FOUND - Reel stopped. Try another tape.":
            Data.Prepared?"Ready to insert":Data.Intact?"Seal holes "+((Data.HoleA?1:0)+(Data.HoleB?1:0))+"/2  /  Drag adhesive to hole":
            "Checked "+Mathf.RoundToInt(100f*Data.Scanned/TapeInspectionState.Samples)+"%  /  Hold LMB on reel + Scroll";
        public static TapeInspectionSession Create(SafeInspectionSubject subject)
        {
            var house=WBMain.Instance;var deck=house!=null?house.radioDeck:null;
            if(deck==null || deck.Chapter==null)return null;
            for(int i=0;i<deck.tapes.Length;i++)if(deck.tapes[i].root==subject.source)return new TapeInspectionSession(deck,subject,i);
            return null;
        }
        TapeInspectionSession(HouseRadioDeck deck,SafeInspectionSubject subject,int index)
        {
            this.deck=deck;this.subject=subject;this.index=index;
            foreach(var pair in subject.poses)
            {
                if(pair.source==deck.tapes[index].leftReel)left=pair.display;
                if(pair.source==deck.tapes[index].rightReel)right=pair.display;
                if(deck.tapes[index].damageMarker!=null && pair.source==deck.tapes[index].damageMarker.transform)damage=pair.display;
            }
            preparation=new TapePreparationView(subject,Data);
        }
        public bool Turn(SafeInspectionPart part,float delta)
        {
            if(part==null || (part.transform!=left && part.transform!=right))return false;
            var state=deck.Chapter.Tape;int selected=state.Selected;bool verified=state.SourceVerified;
            try
            {
                float position=Data.Position;int scanned=Data.Scanned;bool broken=Data.Broken;
                state.Selected=index;state.SourceVerified=Data.Intact;
                state.BeginSpindle(part.transform==left?0:1);state.Turn(delta);state.EndSpindle();
                changed|=position!=Data.Position || scanned!=Data.Scanned || broken!=Data.Broken;
            }
            finally{state.Selected=selected;state.SourceVerified=verified;}
            Sync();return true;
        }
        public void Sync()
        {
            preparation.Sync();
            var rotation=Quaternion.AngleAxis(-Data.Position*420,Vector3.up);
            if(left!=null)left.localRotation=rotation;if(right!=null)right.localRotation=rotation;
            if(damage!=null)damage.gameObject.SetActive(Data.Broken);
        }
        public void Close(){preparation.Dispose();deck.AdvanceInspection(1);WBMain.Instance?.SaveStableProgress();}
    }
}
