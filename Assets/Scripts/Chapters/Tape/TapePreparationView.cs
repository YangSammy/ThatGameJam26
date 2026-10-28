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
        readonly Collider[] surfaces;
        readonly Vector3 stripPosition;
        readonly Quaternion stripRotation;
        readonly Vector3 stripScale;
        int held=-1;
        Vector3 start;
        bool torn;
        Vector2 previousPointer;
        float size;
        public bool Busy=>held>=0;
        public string Hint=>data.Broken?"Broken tape - return and inspect another tape":!data.Intact?"Check both reels first  /  Hold LMB on each reel + Scroll to both ends":
            Busy?(torn?"Move to an upper-edge hole and release LMB":"Pull the strip across CUT HERE"):
            data.Prepared?"E / Esc to return  /  Drag tape into cassette slot":
            "Pull TAPE TAB across CUT HERE / Rotate tape to see both upper-edge holes";

        void PlaceTool(Camera camera)
        {
            if(bindings.toolRoot==null)return;
            bindings.toolRoot.gameObject.SetActive(true);
            bindings.toolRoot.position=camera.ViewportToWorldPoint(new Vector3(.27f,.35f,.7f));
            bindings.toolRoot.rotation=camera.transform.rotation*Quaternion.Euler(35,0,0);
            bindings.toolRoot.localScale=Vector3.one;
            // Tool coordinates are authored in metres, independent of cassette framing scale.
            var parent=bindings.toolRoot.parent;
            if(parent!=null){var scale=parent.lossyScale;bindings.toolRoot.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);}
            Physics.SyncTransforms();
        }

        bool Visible(int index,Ray ray,out float distance)
        {
            distance=0;var piece=pieces[index];
            var collider=piece.GetComponent<Collider>();RaycastHit hit;
            if(!piece.activeInHierarchy || collider==null || !collider.enabled || !collider.Raycast(ray,out hit,100))return false;
            distance=hit.distance;
            if(surfaces!=null)foreach(var surface in surfaces)
            {
                if(index==2 || surface==null || !surface.enabled || !surface.gameObject.activeInHierarchy || surface.transform.IsChildOf(bindings.transform) || (bindings.toolRoot!=null && surface.transform.IsChildOf(bindings.toolRoot)))continue;
                if(surface.Raycast(ray,out hit,100) && hit.distance<distance-.0001f)return false;
            }
            return true;
        }
        public void DrawHints(Camera camera,GUIStyle style)
        {
            PlaceTool(camera);
            if(bindings.cutter!=null){var p=camera.WorldToScreenPoint(bindings.cutter.bounds.center);GUI.Label(new Rect(p.x-70,Screen.height-p.y+12,140,24),"CUT HERE",style);}
            int hiddenHoles=0;
            for(int i=0;i<3;i++)
            {
                Vector3 point=camera.WorldToScreenPoint(pieces[i].transform.position);float distance;
                if(point.z<=0 || !Visible(i,camera.ScreenPointToRay(point),out distance)){if(i<2)hiddenHoles++;continue;}
                string label=i==2?(data.Broken?"TAPE TAB - BROKEN TAPE":data.Intact?"TAPE TAB - LMB DRAG":"TAPE TAB - CHECK REELS FIRST"):
                    (i==0?"HOLE A":"HOLE B")+((i==0?data.HoleA:data.HoleB)?" - SEALED":" - PATCH HERE");
                float x=Mathf.Clamp(point.x,140,Mathf.Max(140,Screen.width-140));
                float y=Screen.height-point.y;
                var tint=GUI.color;GUI.color=new Color(1f,.82f,.36f);
                GUI.DrawTexture(new Rect(point.x-3,y-3,6,6),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(point.x-1,y-17,2,14),Texture2D.whiteTexture);
                GUI.color=tint;
                GUI.Label(new Rect(x-140,y-43,280,24),label,style);
            }
            if(hiddenHoles>0)GUI.Label(new Rect(24,Screen.height-106,Screen.width-48,24),"Tilt tape with LMB + RMB to see the upper edge",style);
        }
        public TapePreparationView(SafeInspectionSubject subject,TapeInspectionState.Candidate candidate)
        {
            data=candidate;root=subject.displayRoot;bindings=subject.tapePreparation;surfaces=subject.surfaces;
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
            PlaceTool(camera);
            if(button!=0 && !Busy)return false;
            int hit=-1;float nearest=float.MaxValue;
            for(int i=0;i<3;i++){float hitDistance;if(Visible(i,ray,out hitDistance) && hitDistance<nearest){nearest=hitDistance;hit=i;}}
            if(type==EventType.MouseDown && button==0 && hit>=0)
            {
                if(!data.Intact)return false;
                if(hit<2 && !(hit==0?data.HoleA:data.HoleB))return false;
                held=hit;torn=hit<2;
                if(hit==0)data.HoleA=false;if(hit==1)data.HoleB=false;
                start=pieces[hit].transform.position;previousPointer=camera.WorldToScreenPoint(start);pieces[3].SetActive(true);pieces[3].transform.position=start;Sync();return true;
            }
            if(!Busy)return false;
            var plane=new Plane(camera.transform.forward,start);float distance;
            if(plane.Raycast(ray,out distance))pieces[3].transform.position=ray.GetPoint(distance);
            Vector2 pointer=camera.WorldToScreenPoint(pieces[3].transform.position);
            if(!torn && bindings.cutter!=null)
            {
                Vector2 cut=camera.WorldToScreenPoint(bindings.cutter.bounds.center);
                Vector2 segment=pointer-previousPointer;float along=segment.sqrMagnitude>.001f?Mathf.Clamp01(Vector2.Dot(cut-previousPointer,segment)/segment.sqrMagnitude):0;
                bool crossed=Vector2.Distance(previousPointer+segment*along,cut)<22;
                if(previousPointer!=Vector2.zero && crossed && Vector3.Distance(start,pieces[3].transform.position)>size)
                {torn=true;WBMain.Tone(180,.06f);}
            }
            previousPointer=pointer;
            if(bindings.ribbon!=null)
            {
                bindings.ribbon.gameObject.SetActive(!torn);
                var delta=pieces[3].transform.position-start;
                bindings.ribbon.position=(start+pieces[3].transform.position)*.5f;
                if(delta.sqrMagnitude>.000001f)bindings.ribbon.rotation=Quaternion.LookRotation(delta,camera.transform.forward);
                var parent=bindings.ribbon.parent.lossyScale;
                bindings.ribbon.localScale=new Vector3(.012f/parent.x,.001f/parent.y,delta.magnitude/parent.z);
            }
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
            held=-1;torn=false;previousPointer=Vector2.zero;pieces[3].SetActive(false);
            if(bindings.ribbon!=null)bindings.ribbon.gameObject.SetActive(false);
            pieces[3].transform.localPosition=stripPosition;pieces[3].transform.localRotation=stripRotation;pieces[3].transform.localScale=stripScale;Sync();
        }
        public void Dispose(){Cancel();}
    }
}
