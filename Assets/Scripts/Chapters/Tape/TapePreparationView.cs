using UnityEngine;

namespace MemoryWhitebox
{
    // Uses authored scene bindings; closing inspection restores the movable strip pose.
    public sealed class TapePreparationView
    {
        readonly TapeInspectionState.Candidate data;
        readonly Transform root;
        readonly GameObject[] pieces;
        readonly TapePreparationBindings bindings;
        readonly Vector3 stripPosition;
        readonly Quaternion stripRotation;
        readonly Vector3 stripScale;
        int held=-1;
        Vector3 start;
        bool torn;
        float size;
        public bool Busy=>held>=0;
        public TapePreparationView(SafeInspectionSubject subject,TapeInspectionState.Candidate candidate)
        {
            data=candidate;root=subject.displayRoot;bindings=subject.tapePreparation;
            if(bindings==null || bindings.pieces==null || bindings.pieces.Length!=4)
                throw new System.InvalidOperationException("Tape preparation scene bindings are missing.");
            pieces=bindings.pieces;
            foreach(var piece in pieces)if(piece==null)throw new System.InvalidOperationException("Tape preparation part is missing.");
            size=bindings.tearDistance;
            stripPosition=pieces[3].transform.localPosition;stripRotation=pieces[3].transform.localRotation;stripScale=pieces[3].transform.localScale;
            pieces[3].SetActive(false);Sync();
        }

        public bool Handle(EventType type,int button,Ray ray,Camera camera)
        {
            if(button!=0 && !Busy)return false;
            int hit=-1;float nearest=float.MaxValue;RaycastHit h;
            for(int i=0;i<3;i++)if(pieces[i].GetComponent<Collider>().Raycast(ray,out h,100) && h.distance<nearest){nearest=h.distance;hit=i;}
            if(type==EventType.MouseDown && button==0 && hit>=0)
            {
                if(hit<2 && !(hit==0?data.HoleA:data.HoleB))return true;
                held=hit;torn=hit<2;
                if(hit==0)data.HoleA=false;if(hit==1)data.HoleB=false;
                start=pieces[hit].transform.position;pieces[3].SetActive(true);pieces[3].transform.position=start;Sync();return true;
            }
            if(!Busy)return false;
            var plane=new Plane(camera.transform.forward,start);float distance;
            if(plane.Raycast(ray,out distance))pieces[3].transform.position=ray.GetPoint(distance);
            if(!torn && Vector3.Distance(start,pieces[3].transform.position)>size*Mathf.Abs(root.lossyScale.x)){torn=true;WBMain.Tone(180,.06f);}
            if(type==EventType.MouseUp && button==0)
            {
                if(torn && hit<2 && hit>=0 && data.Intact){if(hit==0)data.HoleA=true;else data.HoleB=true;WBMain.Tone(330,.06f);}
                Cancel();WBMain.Instance?.SaveStableProgress();
            }
            return true;
        }
        public void Sync(){bindings.ShowState(data);}
        public void Cancel()
        {
            held=-1;torn=false;pieces[3].SetActive(false);
            pieces[3].transform.localPosition=stripPosition;pieces[3].transform.localRotation=stripRotation;pieces[3].transform.localScale=stripScale;Sync();
        }
        public void Dispose(){Cancel();}
    }
}
