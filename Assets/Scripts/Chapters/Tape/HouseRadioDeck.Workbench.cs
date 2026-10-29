using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public Collider workMat,adhesiveTab,adhesiveCutter;
        public Transform workAnchor,stripRest,adhesiveStrip,adhesiveRibbon;
        public bool WorkbenchMode {get;private set;}
        readonly PlayerInputReleaseGate benchGate=new PlayerInputReleaseGate();
        bool benchWaiting;
        enum BenchGesture {None,Pending,Spindle,Rotate,MoveTape,PullStrip,MoveStrip,WaitRelease}
        BenchGesture benchGesture;
        int benchSpindle;
        bool benchLeft,benchRight;
        float benchPending;
        Vector2 benchLast,benchPress;
        Vector3 pullStart;
        int BenchIndex=>Chapter.WorkbenchTapeIndex;
        bool BenchHit(Collider hit)=>hit!=null && (hit==workMat || hit==adhesiveTab || hit==adhesiveCutter || (adhesiveTab!=null && hit.transform.IsChildOf(adhesiveTab.transform.parent)));
        public void OpenWorkbench()
        {
            if(workMat==null || workAnchor==null)return;
            if(RecordingMode)CloseRecording();
            StopRecordingInput();
            WorkbenchMode=true;benchWaiting=true;benchLeft=benchRight=false;benchGesture=BenchGesture.None;benchGate.Arm();house.Room.StopMotion();PlayerInputPolicy.SetCursor(true);
            house.SyncInputContext();
        }
        public bool TryBeginWorkbenchInteraction(Ray ray)
        {
            var hit=StudyEntry.PickVisible(ray);
            int candidate=TapeAt(ray);
            if(candidate>=0)
            {
                if(!Chapter.AuditionComplete || State==Phase.Loading || State==Phase.Returning)return true;
                if(Selected>=0 && candidate!=Selected)return true;
                if(RecordingMode)CloseRecording();
                if(candidate==Selected)TakeOut();
                PlaceOnWorkbench(candidate);return true;
            }
            if(!BenchHit(hit))return false;
            OpenWorkbench();
            if(hit==adhesiveTab || (adhesiveStrip!=null && hit!=null && hit.transform==adhesiveStrip))
            {
                benchWaiting=false;
                house.AdoptPointerGesture();
                WorkbenchPointer(EventType.MouseDown,0,view.WorldToScreenPoint(ray.GetPoint(1)),Vector2.zero,0);
            }
            return true;
        }
        public bool CancelWorkbenchGesture()
        {
            bool active=movingOriginal || (benchGesture!=BenchGesture.None && benchGesture!=BenchGesture.WaitRelease);
            if(!active)return false;
            if(movingOriginal){movingOriginal=false;originalHit.enabled=true;RestoreSource();}
            if(benchGesture==BenchGesture.MoveTape)PoseWorkbenchTape();
            if(benchGesture==BenchGesture.PullStrip)adhesiveStrip.gameObject.SetActive(false);
            if(benchGesture==BenchGesture.MoveStrip)RestStrip();
            adhesiveRibbon.gameObject.SetActive(false);ReleaseSpindle();
            benchGesture=BenchGesture.None;benchLeft=benchRight=false;benchWaiting=true;benchGate.Arm();
            house.InputContext.Handoff();
            house.SaveStableProgress();return true;
        }
        public void CloseWorkbench()
        {
            CancelWorkbenchGesture();StopRecordingInput();
            adhesiveRibbon.gameObject.SetActive(false);benchGesture=BenchGesture.None;benchLeft=benchRight=false;
            WorkbenchMode=false;house.SaveStableProgress();house.SuspendHouseInput();
            house.SyncInputContext();
        }
        public void PlaceOnWorkbench(int index)
        {
            if(!Chapter.AuditionComplete || index<0 || index>=tapes.Length || State==Phase.Loading)return;
            if(BenchIndex>=0 && BenchIndex!=index){var old=tapes[BenchIndex];old.root.SetPositionAndRotation(old.homeParent.TransformPoint(old.homePosition),old.homeParent.rotation*old.homeRotation);}
            if(State==Phase.Dragging){CancelTapeDrag();State=Phase.Empty;Selected=-1;Chapter.Tape.Selected=-1;}
            if(Chapter.WorkbenchTapeIndex!=index)Chapter.WorkbenchEuler=Vector3.zero;
            Chapter.WorkbenchTapeIndex=index;PoseWorkbenchTape();OpenWorkbench();
        }
        void PoseWorkbenchTape()
        {
            if(BenchIndex<0)return;var t=tapes[BenchIndex];
            t.root.SetPositionAndRotation(workAnchor.position,workAnchor.rotation*Quaternion.Euler(Chapter.WorkbenchEuler));
        }
        public void RestoreWorkbench()
        {
            if(BenchIndex>=0 && BenchIndex!=Chapter.WorkingTapeIndex)PoseWorkbenchTape();
            if(Chapter.LooseStripReady)RestStrip();
        }
        void RestStrip()
        {
            Chapter.LooseStripReady=true;adhesiveStrip.gameObject.SetActive(true);
            adhesiveStrip.SetPositionAndRotation(stripRest.position,stripRest.rotation);
        }
        public void TickWorkbench()
        {
            house.Room.StopMotion();
            if(house.InputContext.ExitPressed(InputContextId.TapeWorkbench,CancelWorkbenchGesture,CloseWorkbench))return;
            if(benchWaiting){if(benchGate.Ready)benchWaiting=false;return;}
            if(benchGesture==BenchGesture.Pending){benchPending+=Time.unscaledDeltaTime;if(benchPending>.1f && benchSpindle>=0)benchGesture=BenchGesture.Spindle;}
            AdvanceInspection(Time.unscaledDeltaTime);
        }
        bool Hit(Collider col,Ray ray){RaycastHit h;return col!=null && col.enabled && col.gameObject.activeInHierarchy && col.Raycast(ray,out h,20);}
        int TapeAt(Ray ray)
        {
            var visible=StudyEntry.PickVisible(ray);
            for(int i=0;i<tapes.Length;i++)if(visible==tapes[i].hit || visible==tapes[i].leftSpindleHit || visible==tapes[i].rightSpindleHit)return i;
            return -1;
        }
        public void WorkbenchPointer(EventType type,int button,Vector2 point,Vector2 delta,float wheel)
        {
            if(!WorkbenchMode || benchWaiting || !house.InputContext.Allows(InputContextId.TapeWorkbench))return;
            var ray=view.ScreenPointToRay(point);Physics.SyncTransforms();
            if(SourcePointer(type,button,ray))return;
            if(type==EventType.MouseDown && button==0 && benchGesture==BenchGesture.None)
            {
                var visible=StudyEntry.PickVisible(ray);
                if(visible==radioHit || visible==slotHit || RecordingHit(visible) || (Chapter.SourceLoaded && visible==originalHit))
                {CloseWorkbench();OpenRecording();return;}
            }
            if(!Chapter.AuditionComplete)return;
            if(type==EventType.ScrollWheel)
            {
                if((benchGesture==BenchGesture.Pending || benchGesture==BenchGesture.Spindle) && benchLeft && !benchRight && benchSpindle>=0 && BenchIndex>=0)
                {
                    benchGesture=BenchGesture.Spindle;var state=Chapter.Tape;int selected=state.Selected;bool verified=state.SourceVerified;
                    state.Selected=BenchIndex;state.SourceVerified=state.Candidates[BenchIndex].Intact;
                    state.BeginSpindle(benchSpindle);state.Turn(wheel);state.EndSpindle();state.Selected=selected;state.SourceVerified=verified;
                }
                return;
            }
            if(type==EventType.MouseDown)
            {
                if(button==0)benchLeft=true;if(button==1)benchRight=true;
                if(benchGesture==BenchGesture.Pending && benchLeft && benchRight){benchGesture=BenchGesture.Rotate;return;}
                if(benchGesture!=BenchGesture.None || button!=0)return;
                benchPress=benchLast=point;
                if(Chapter.LooseStripReady && Hit(adhesiveStrip.GetComponent<Collider>(),ray)){benchGesture=BenchGesture.MoveStrip;pullStart=adhesiveStrip.position;return;}
                if(Hit(adhesiveTab,ray))
                {
                    if(Chapter.LooseStripReady)return;
                    benchGesture=BenchGesture.PullStrip;pullStart=adhesiveTab.bounds.center;adhesiveStrip.position=pullStart;adhesiveStrip.gameObject.SetActive(true);return;
                }
                int candidate=TapeAt(ray);
                if(candidate>=0 && candidate!=Selected && candidate!=BenchIndex){PlaceOnWorkbench(candidate);return;}
                if(BenchIndex<0)return;
                var tape=tapes[BenchIndex];benchSpindle=Hit(tape.leftSpindleHit,ray)?0:Hit(tape.rightSpindleHit,ray)?1:-1;
                if(candidate==BenchIndex || benchSpindle>=0){benchGesture=benchRight?BenchGesture.Rotate:BenchGesture.Pending;benchPending=0;}
                return;
            }
            if(type==EventType.MouseDrag)
            {
                if(benchGesture==BenchGesture.Rotate && benchLeft && benchRight)
                {Chapter.WorkbenchEuler+=new Vector3(-delta.y*.4f,delta.x*.4f,0);PoseWorkbenchTape();}
                if(benchGesture==BenchGesture.Pending && benchSpindle<0 && Vector2.Distance(point,benchPress)>7)benchGesture=BenchGesture.MoveTape;
                if(benchGesture==BenchGesture.MoveTape && BenchIndex>=0)
                {float t;var plane=new Plane(view.transform.forward,workAnchor.position);if(plane.Raycast(ray,out t))tapes[BenchIndex].root.position=ray.GetPoint(t);}
                if(benchGesture==BenchGesture.PullStrip || benchGesture==BenchGesture.MoveStrip)
                {
                    float t;var plane=new Plane(view.transform.forward,pullStart);if(plane.Raycast(ray,out t))adhesiveStrip.position=ray.GetPoint(t);
                    if(benchGesture==BenchGesture.PullStrip)
                    {
                        Vector2 cut=view.WorldToScreenPoint(adhesiveCutter.bounds.center),segment=point-benchLast;
                        float along=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(cut-benchLast,segment)/segment.sqrMagnitude):0;
                        if(Vector2.Distance(cut,benchLast+segment*along)<20 && Vector3.Distance(pullStart,adhesiveStrip.position)>.025f)
                        {benchGesture=BenchGesture.MoveStrip;Chapter.LooseStripReady=true;WBMain.Tone(180,.08f);}
                    }
                    adhesiveRibbon.gameObject.SetActive(benchGesture==BenchGesture.PullStrip);
                    var length=adhesiveStrip.position-pullStart;adhesiveRibbon.position=(adhesiveStrip.position+pullStart)*.5f;
                    if(length.sqrMagnitude>.00001f)adhesiveRibbon.rotation=Quaternion.LookRotation(length,view.transform.forward);
                    var scale=adhesiveRibbon.parent.lossyScale;adhesiveRibbon.localScale=new Vector3(.012f/scale.x,.001f/scale.y,length.magnitude/scale.z);
                }
                benchLast=point;return;
            }
            if(type==EventType.MouseUp)
            {
                if(button==0)benchLeft=false;if(button==1)benchRight=false;
                if(benchGesture==BenchGesture.Rotate){benchGesture=BenchGesture.WaitRelease;}
                if(button==0)
                {
                    if(benchGesture==BenchGesture.MoveTape && BenchIndex>=0)
                    {
                        int index=BenchIndex;
                        if(Hit(slotHit,ray) && State==Phase.Empty && SelectForDrag(index) && InsertDragged()){Chapter.WorkbenchTapeIndex=-1;CloseWorkbench();return;}
                        PoseWorkbenchTape();
                    }
                    if(benchGesture==BenchGesture.MoveStrip)
                    {
                        bool attached=false;
                        if(BenchIndex>=0 && Chapter.Tape.Candidates[BenchIndex].Intact)
                        {
                            for(int i=0;i<2;i++)
                            {
                                var hole=tapes[BenchIndex].preparation.pieces[i];var target=(Vector2)view.WorldToScreenPoint(hole.transform.position);
                                // Tolerant screen target, but require a direct visible surface ray.
                                var toward=new Ray(view.transform.position,hole.transform.position-view.transform.position);
                                bool clear=Vector3.Dot(hole.transform.up,(view.transform.position-hole.transform.position).normalized)>.08f;
                                float distance=Vector3.Distance(view.transform.position,hole.transform.position);
                                foreach(var near in Physics.RaycastAll(toward,distance-.004f,view.cullingMask))
                                {
                                    var tape=tapes[BenchIndex];
                                    if(near.collider==tape.hit || near.collider==tape.leftSpindleHit || near.collider==tape.rightSpindleHit || near.transform==adhesiveStrip || near.collider==hole.GetComponent<Collider>())continue;
                                    clear=false;break;
                                }
                                if(Vector2.Distance(point,target)<22 && clear){if(i==0)Chapter.Tape.Candidates[BenchIndex].HoleA=true;else Chapter.Tape.Candidates[BenchIndex].HoleB=true;attached=true;break;}
                            }
                        }
                        if(attached){Chapter.LooseStripReady=false;adhesiveStrip.gameObject.SetActive(false);WBMain.Tone(330,.06f);}else RestStrip();
                    }
                    else if(benchGesture==BenchGesture.PullStrip)adhesiveStrip.gameObject.SetActive(false);
                    adhesiveRibbon.gameObject.SetActive(false);
                    benchGesture=benchRight?BenchGesture.WaitRelease:BenchGesture.None;house.SaveStableProgress();
                }
                if(!benchLeft && !benchRight)benchGesture=BenchGesture.None;
            }
        }
        string NextTapeTask()
        {
            if(Chapter.Completed)return "All 3 copies complete.";
            if(!Chapter.SourceLoaded)return "Load Original "+(CurrentCopy+1).ToString("00")+" into A.";
            if(!Chapter.AuditionComplete)return transport.SourcePlaying?"Listen to the original.":"Play A to listen.";
            if(State==Phase.Loading)return "Loading tape...";
            if(RecordingMode)
            {
                if(Selected<0)return "Open WORKBENCH to find a work tape.";
                var loaded=Chapter.Tape.Candidates[Selected];
                if(!loaded.Intact)return "Eject B and check both reels.";
                if(!loaded.Prepared)return Chapter.ProtectionDiscovered?"Eject B and cover both holes.":"Try REC on B.";
                if(transport.Writing)return "STOP B to leave sounds out.";
                if(transport.TargetStatus=="PLAYBACK" && !transport.SourcePlaying)return "Listen, then submit your copy.";
                if(transport.SourcePlaying)return "REC to resume B.";
                return transport.RecordedSeconds>0?"Listen to your copy.":"REC to start your copy.";
            }
            if(Selected>=0)return "Open the recorder controls.";
            if(BenchIndex<0)return "Choose a work tape.";
            var data=Chapter.Tape.Candidates[BenchIndex];
            if(data.Broken)return "Broken tape. Choose another.";
            if(!data.Intact)return "Check both reels.";
            if(!data.Prepared && Chapter.ProtectionDiscovered)
                return Chapter.LooseStripReady?"Cover both upper-edge holes.":"Pull tape across the cutter.";
            return "Load the work tape into B.";
        }
        string WorkbenchHint()
        {
            if(movingOriginal || benchGesture==BenchGesture.MoveTape)return "Release over the slot to insert";
            if(benchGesture==BenchGesture.PullStrip)return "Drag across the cutter";
            if(benchGesture==BenchGesture.MoveStrip)return "Release over a hole to cover";
            if(benchGesture==BenchGesture.Rotate)return "LMB + RMB  Rotate";
            if(benchGesture==BenchGesture.Spindle)return "Hold LMB + Scroll  Wind reel";
            var hit=StudyEntry.PickVisible(view.ScreenPointToRay(Input.mousePosition));
            var sourceHint=SourceHint(hit);if(sourceHint!=null)return sourceHint;
            if(hit==adhesiveTab)return "LMB drag  Pull tape";
            if(BenchIndex>=0)
            {
                var t=tapes[BenchIndex];
                if(hit==t.leftSpindleHit || hit==t.rightSpindleHit)return "Hold LMB + Scroll  Wind reel";
                if(hit==t.hit)return "LMB drag  Move   /   LMB + RMB  Rotate";
            }
            return "";
        }
        void DrawWorkbench()
        {
            if(house==null || Chapter==null || house.safeInspection.Active)return;
            var style=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
            style.normal.textColor=new Color(.94f,.94f,.88f);
            if(WorkbenchMode && !benchWaiting)
            {
                var e=Event.current;
                if(e.type==EventType.MouseDown || e.type==EventType.MouseUp || e.type==EventType.MouseDrag || e.type==EventType.ScrollWheel)
                    WorkbenchPointer(e.type,e.button,new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y),new Vector2(e.delta.x,-e.delta.y),-e.delta.y/3);
                var hint=WorkbenchHint();
                if(hint!="")GUI.Label(new Rect(24,Screen.height-44,Screen.width-48,30),hint,style);
            }
            if(WorkbenchMode || RecordingMode)
            {
                GUI.Label(new Rect(24,24,Mathf.Min(440,Screen.width-160),48),NextTapeTask(),style);
                var exit=new GUIStyle(style){alignment=TextAnchor.UpperRight,fontSize=13};
                exit.normal.textColor=new Color(.7f,.72f,.7f);
                GUI.Label(new Rect(Screen.width-124,24,100,24),"Esc  Back",exit);
            }
        }
        bool DirectTaskNearby()=>StationActive && Vector3.Distance(view.transform.position,transform.position)<2.5f;
    }
}
