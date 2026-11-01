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
        Vector2 benchLast,benchPress;
        Vector3 pullStart;
        Ray benchGrabRay;
        readonly ObjectDragSession benchDrag=new ObjectDragSession();
        bool CanReceive(Collider receiver,Ray ray)=>TapeDropAllowed(receiver,ray,benchDrag.Item);
        int BenchIndex=>Chapter.WorkbenchTapeIndex;
        bool BenchHit(Collider hit)=>hit!=null && (hit==workMat || hit==adhesiveTab || hit==adhesiveCutter || (adhesiveTab!=null && hit.transform.IsChildOf(adhesiveTab.transform.parent)));
        public void OpenWorkbench()
        {
            if(workMat==null || workAnchor==null)return;
            bool preserve=internalDeckHandoff;internalDeckHandoff=dualDeck!=null;
            try{if(RecordingMode)CloseRecording();StopRecordingInput();}finally{internalDeckHandoff=preserve;}
            WorkbenchMode=true;benchWaiting=true;benchLeft=benchRight=false;benchGesture=BenchGesture.None;benchGate.Arm();house.Room.StopMotion();PlayerInputPolicy.SetCursor(true);
            house.SyncInputContext();
        }
        public bool TryBeginWorkbenchInteraction(Ray ray)
        {
            if(movingOriginal || benchGesture!=BenchGesture.None || TapeUIAt(view.WorldToScreenPoint(ray.GetPoint(1))))return false;
            var hit=PickWorkbenchTarget(ray);
            if(StorageClick(hit))return true;
            int candidate=TapeAt(ray);
            if(candidate>=0)
            {
                var reason=WorkTapeLockReason(candidate);if(reason!=null){ShowTapePointerNotice(reason);return true;}
                if(candidate==Selected && dualDeck!=null){EjectDualDeck(1);return true;}
                if(RecordingMode){internalDeckHandoff=dualDeck!=null;try{CloseRecording();}finally{internalDeckHandoff=false;}}
                if(candidate==Selected)TakeOut();
                PlaceOnWorkbench(candidate);benchWaiting=false;house.AdoptPointerGesture();
                WorkbenchPointer(EventType.MouseDown,0,view.WorldToScreenPoint(ray.GetPoint(1)),Vector2.zero,0);return true;
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
            if(Smoothing){CancelSmooth();house.SaveStableProgress();return true;}
            bool active=movingOriginal || (benchGesture!=BenchGesture.None && benchGesture!=BenchGesture.WaitRelease);
            if(!active)return false;
            benchDrag.End(true);
            ShowTapeDropPreview(null);
            if(movingOriginal){movingOriginal=false;originalHit.enabled=true;RestoreSource();}
            if(benchGesture==BenchGesture.MoveTape)PoseWorkbenchTape();
            if(benchGesture==BenchGesture.PullStrip){adhesiveStrip.gameObject.SetActive(false);workbenchVisuals?.CancelPull();}
            if(benchGesture==BenchGesture.MoveStrip)RestStrip();
            adhesiveRibbon.gameObject.SetActive(false);ReleaseSpindle();
            benchGesture=BenchGesture.None;benchLeft=benchRight=false;benchWaiting=true;benchGate.Arm();
            house.InputContext.Handoff();
            house.SaveStableProgress();return true;
        }
        public void CloseWorkbench()
        {
            CancelWorkbenchGesture();RestorePreparationCamera();StopRecordingInput();
            ShowTapeDropPreview(null);
            adhesiveRibbon.gameObject.SetActive(false);benchGesture=BenchGesture.None;benchLeft=benchRight=false;
            WorkbenchMode=false;house.SaveStableProgress();house.SuspendHouseInput();
            house.SyncInputContext();
        }
        public void PlaceOnWorkbench(int index)
        {
            if(!Chapter.AuditionComplete || index<0 || index>=tapes.Length || State==Phase.Loading)return;
            if(BenchIndex>=0 && BenchIndex!=index){var old=tapes[BenchIndex];old.root.SetPositionAndRotation(old.homeParent.TransformPoint(old.homePosition),old.homeParent.rotation*old.homeRotation);OperationArea.RestorePose(old.root);}
            if(State==Phase.Dragging){CancelTapeDrag();State=Phase.Empty;Selected=-1;Chapter.Tape.Selected=-1;}
            if(Chapter.WorkbenchTapeIndex!=index)Chapter.WorkbenchEuler=Vector3.zero;
            Chapter.WorkbenchTapeIndex=index;PoseWorkbenchTape();OpenWorkbench();
        }
        void PoseWorkbenchTape()
        {
            if(BenchIndex<0)return;var t=tapes[BenchIndex];
            if(OperationArea.For(t.root)!=null)
            {
                OperationArea.RestorePose(t.root);
                t.root.rotation=t.homeParent.rotation*t.homeRotation*Quaternion.Euler(Chapter.WorkbenchEuler);
            }
            else t.root.SetPositionAndRotation(workAnchor.position,workAnchor.rotation*Quaternion.Euler(Chapter.WorkbenchEuler));
        }
        public void RestoreWorkbench()
        {
            if(BenchIndex>=0 && BenchIndex!=Chapter.WorkingTapeIndex)PoseWorkbenchTape();
            if(Chapter.LooseStripReady)RestStrip();
        }
        void RestStrip()
        {
            Chapter.LooseStripReady=true;adhesiveStrip.gameObject.SetActive(true);
            adhesiveStrip.SetPositionAndRotation(stripRest.position,stripRest.rotation);OperationArea.RestorePose(adhesiveStrip);
        }
        public void TickWorkbench()
        {
            house.Room.StopMotion();TickPreparationCamera(Time.unscaledDeltaTime);
            if(dualDeck!=null && transport!=null)TickAudition();
            if(house.InputContext.ExitPressed(InputContextId.TapeWorkbench,CancelWorkbenchGesture,ExitTapeWorkspace))return;
            if(preparationCameraMoving)return;
            if(benchWaiting){if(benchGate.Ready)benchWaiting=false;return;}
            if(PollWorkspaceInspection())return;

            AdvanceInspection(Time.unscaledDeltaTime);
        }
        void ExitTapeWorkspace()
        {
            if(radioFocused){FocusRadio(false);return;}
            if(preparationFocused){FocusPreparation(false);return;}
            if(house!=null && house.tapeDeskView && house.InStation && house.ChapterIndex==0)house.ExitTapeDeskView();
            else {CloseWorkbench();CloseRecording();}
        }
        bool Hit(Collider col,Ray ray){RaycastHit h;return col!=null && col.enabled && col.gameObject.activeInHierarchy && col.Raycast(ray,out h,20);}
        int TapeAt(Ray ray)
        {
            var visible=PickWorkbenchTarget(ray);
            if(workbenchVisuals!=null)for(int i=0;i<workbenchVisuals.candidateGrips.Length;i++)if(visible!=null && visible==workbenchVisuals.candidateGrips[i])return i;
            for(int i=0;i<tapes.Length;i++)if(visible==tapes[i].hit || visible==tapes[i].leftSpindleHit || visible==tapes[i].rightSpindleHit)return i;
            return -1;
        }
        public void WorkbenchPointer(EventType type,int button,Vector2 point,Vector2 delta,float wheel)
        {
            if((preparationCameraMoving && !preparationCarryReturn) || !WorkbenchMode || benchWaiting || !house.InputContext.Allows(InputContextId.TapeWorkbench))return;
            if(Smoothing)return;
            var ray=view.ScreenPointToRay(point);Physics.SyncTransforms();
            if(type==EventType.MouseDown && !movingOriginal && benchGesture==BenchGesture.None && TapeUIAt(point))return;
            if(SourcePointer(type,button,ray))return;
            if(type==EventType.MouseDown && button==0 && benchGesture==BenchGesture.None)
            {
                var visible=PickWorkbenchTarget(ray);
                if(StorageClick(visible))return;
                if(dualDeck!=null && dualDeck.Command(visible)!=null){FinishWorkbenchTransfer();OpenRecording();PressDualControl(dualDeck.Command(visible));return;}
                if(visible==radioHit || visible==slotHit || visible==sourceSlot || RecordingHit(visible) || (Chapter.SourceLoaded && visible==originalHit))
                {FinishWorkbenchTransfer();OpenRecording();if(visible==radioHit || visible==slotHit || visible==sourceSlot)FocusRadio(true);return;}
            }
            if(type==EventType.MouseDown && button==0)
            {
                int locked=TapeAt(ray);string reason=locked>=0?WorkTapeLockReason(locked):null;
                if(reason!=null){ShowTapePointerNotice(reason);return;}
            }
            if(!Chapter.AuditionComplete)return;
            if(type==EventType.ScrollWheel)
            {
                if((benchGesture==BenchGesture.Pending || benchGesture==BenchGesture.Spindle) && benchLeft && !benchRight && benchSpindle>=0 && BenchIndex>=0)
                {
                    benchGesture=BenchGesture.Spindle;var state=Chapter.Tape;int selected=state.Selected;bool verified=state.SourceVerified;
                    state.Selected=BenchIndex;state.SourceVerified=state.Candidates[BenchIndex].Intact;
                    state.BeginSpindle(benchSpindle);state.Turn(wheel);state.EndSpindle();state.Selected=selected;state.SourceVerified=verified;
                    if(workbenchVisuals!=null)workbenchVisuals.Turn(BenchIndex,benchSpindle,wheel,state.Candidates[BenchIndex].Broken);
                }
                return;
            }
            if(type==EventType.MouseDown)
            {
                if(button==0)benchLeft=true;if(button==1)benchRight=true;
                if(benchGesture==BenchGesture.Pending && benchLeft && benchRight){benchGesture=BenchGesture.Rotate;return;}
                if(benchGesture!=BenchGesture.None || button!=0)return;
                benchPress=benchLast=point;benchGrabRay=ray;
                if(workbenchVisuals!=null && BenchIndex>=0 && !Chapter.LooseStripReady)
                {
                    var data=Chapter.Tape.Candidates[BenchIndex];
                    for(int i=0;i<2;i++)
                    {
                        var socket=tapes[BenchIndex].preparation.pieces[i];
                        if(!(i==0?data.HoleA:data.HoleB) || !Hit(socket.GetComponent<Collider>(),ray))continue;
                        if(i==0)data.HoleA=false;else data.HoleB=false;
                        Chapter.LooseStripReady=true;adhesiveStrip.gameObject.SetActive(true);workbenchVisuals.strip.gameObject.SetActive(true);
                        adhesiveStrip.position=socket.transform.position;benchGesture=BenchGesture.MoveStrip;
                        benchDrag.Begin(adhesiveStrip,ray,view.transform.forward,.008f,1.8f,true,tapes[BenchIndex].root);
                        return;
                    }
                }
                if(Chapter.LooseStripReady && Hit(adhesiveStrip.GetComponent<Collider>(),ray)){benchGesture=BenchGesture.MoveStrip;pullStart=adhesiveStrip.position;benchDrag.Begin(adhesiveStrip,ray,view.transform.forward);return;}
                if(Hit(adhesiveTab,ray))
                {
                    if(Chapter.LooseStripReady)return;
                    benchGesture=BenchGesture.PullStrip;pullStart=adhesiveTab.bounds.center;adhesiveStrip.position=pullStart;adhesiveStrip.gameObject.SetActive(true);benchDrag.Begin(adhesiveStrip,ray,view.transform.forward,0,.12f,false,workbenchVisuals!=null?workbenchVisuals.dispenser.transform:null);return;
                }
                int candidate=TapeAt(ray);
                if(candidate>=0 && candidate!=Selected && candidate!=BenchIndex){PlaceOnWorkbench(candidate);benchWaiting=false;WorkbenchPointer(type,button,point,delta,wheel);return;}
                if(BenchIndex<0)return;
                var tape=tapes[BenchIndex];var target=PickWorkbenchTarget(ray);benchSpindle=target==tape.leftSpindleHit?0:target==tape.rightSpindleHit?1:-1;
                if(candidate==BenchIndex || benchSpindle>=0){benchGesture=benchSpindle>=0?BenchGesture.Spindle:benchRight?BenchGesture.Rotate:BenchGesture.Pending;}
                return;
            }
            if(type==EventType.MouseDrag)
            {
                if(benchGesture==BenchGesture.Rotate && benchLeft && benchRight)
                {Chapter.WorkbenchEuler+=new Vector3(-delta.y*.4f,delta.x*.4f,0);PoseWorkbenchTape();}
                if(benchGesture==BenchGesture.Pending && benchSpindle<0 && Vector2.Distance(point,benchPress)>7)
                {preparationDragFromMat=preparationFocused && OnPreparationMat(tapes[BenchIndex].root);preparationDragPoint=benchPress;benchGesture=BenchGesture.MoveTape;benchDrag.BeginScreenCarry(tapes[BenchIndex].root,benchGrabRay,view.transform.forward);}
                if(benchGesture==BenchGesture.MoveTape && BenchIndex>=0)
                {MoveRadioTape(benchDrag,ray,CanReceive(slotHit,ray)?slotHit.transform:PreparationDrop(ray)?workMat.transform:null);TryReturnWhileDragging(ray,point);}
                if(benchGesture==BenchGesture.PullStrip || benchGesture==BenchGesture.MoveStrip)
                {
                    // Pulling remains on its initial plane. A detached patch can approach the visible cassette surface.
                    if(workbenchVisuals!=null)
                    {
                        Transform receiver=null;
                        if(benchGesture==BenchGesture.MoveStrip && BenchIndex>=0)
                            for(int i=0;i<2;i++){var hole=tapes[BenchIndex].preparation.pieces[i].transform;if(Vector2.Distance(point,view.WorldToScreenPoint(hole.position))<22){receiver=hole;break;}}
                        if(benchGesture==BenchGesture.PullStrip)
                        {
                            float distance;var plane=new Plane(view.transform.forward,pullStart);
                            if(plane.Raycast(ray,out distance))benchDrag.MoveTo(pullStart+Vector3.ClampMagnitude(ray.GetPoint(distance)-pullStart,.12f),workbenchVisuals.dispenser.transform);
                        }
                        else if(receiver!=null)benchDrag.MoveOverSurface(receiver.position,tapes[BenchIndex].root,true);
                        else benchDrag.Move(ray);
                    }
                    else benchDrag.Move(ray,benchGesture==BenchGesture.PullStrip?adhesiveTab.transform.parent:BenchIndex>=0?tapes[BenchIndex].root:null);
                    if(benchGesture==BenchGesture.PullStrip)
                    {
                        if(workbenchVisuals!=null)workbenchVisuals.Pull(Vector3.Distance(pullStart,adhesiveStrip.position));
                        Vector2 cut=view.WorldToScreenPoint(adhesiveCutter.bounds.center),segment=point-benchLast;
                        Vector2 fromHead=(Vector2)view.WorldToScreenPoint(pullStart)-cut;
                        bool crossed=workbenchVisuals==null || (Vector2.Dot(benchLast-cut,fromHead)>0 && Vector2.Dot(point-cut,fromHead)<=0);
                        float along=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(cut-benchLast,segment)/segment.sqrMagnitude):0;
                        if(crossed && !benchDrag.Blocked && Vector2.Distance(cut,benchLast+segment*along)<20 && Vector3.Distance(pullStart,adhesiveStrip.position)>.025f)
                        {workbenchVisuals?.Cut();benchGesture=BenchGesture.MoveStrip;Chapter.LooseStripReady=true;benchDrag.End(false);benchDrag.Begin(adhesiveStrip,ray,view.transform.forward,.008f,1.8f,true,workbenchVisuals!=null?workbenchVisuals.dispenser.transform:null);WBMain.Tone(180,.08f);}
                    }
                    adhesiveRibbon.gameObject.SetActive(workbenchVisuals==null && benchGesture==BenchGesture.PullStrip);
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
                        ShowTapeDropPreview(null);
                        int index=BenchIndex;
                        bool over=CanReceive(slotHit,ray);benchDrag.MoveScreenCarry(ray);
                        bool accepted=!benchDrag.Blocked && over;
                        bool onMat=!accepted && PreparationDrop(ray);
                        bool placed=!accepted && !onMat && benchDrag.TryPlaceInArea();benchDrag.End(!accepted && !placed && !onMat);
                        if(onMat){if(!PlaceTapeOnPreparationMat(false))PoseWorkbenchTape();}

                        if(accepted && State==Phase.Empty && SelectForDrag(index) && InsertDragged()){Chapter.WorkbenchTapeIndex=-1;FinishWorkbenchTransfer();return;}
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
                                if(!benchDrag.Blocked && Vector2.Distance(point,target)<22 && clear)
                                {
                                    var data=Chapter.Tape.Candidates[BenchIndex];if(i==0?data.HoleA:data.HoleB)continue;
                                    if(workbenchVisuals!=null)StartSmooth(BenchIndex,i);else if(i==0)data.HoleA=true;else data.HoleB=true;
                                    attached=true;break;
                                }
                            }
                        }
                        if(attached){OperationArea.ClearPlacement(adhesiveStrip);Chapter.LooseStripReady=Smoothing;adhesiveStrip.gameObject.SetActive(false);WBMain.Tone(330,.06f);}else {benchDrag.TryPlaceInArea();RestStrip();}
                    }
                    else if(benchGesture==BenchGesture.PullStrip){adhesiveStrip.gameObject.SetActive(false);workbenchVisuals?.CancelPull();}
                    benchDrag.End(false);
                    adhesiveRibbon.gameObject.SetActive(false);
                    benchGesture=benchRight?BenchGesture.WaitRelease:BenchGesture.None;house.SaveStableProgress();
                }
                if(!benchLeft && !benchRight)benchGesture=BenchGesture.None;
            }
        }
        string NextTapeTask()
        {
            if(Smoothing)return "Smoothing the patch. Wait a moment.";
            if(Chapter.Completed)return "All 3 copies saved. PLAY COPY to listen again.";
            if(!Chapter.SourceLoaded)return "Load Original "+(CurrentCopy+1).ToString("00")+" into A.";
            if(!Chapter.AuditionComplete)return transport.SourcePlaying?"Listen to A: notice the noise and what follows.":"Click A PLAY to hear the original.";
            if(State==Phase.Loading || DeckBusy(0) || DeckBusy(1))return "Wait for the tape and door to settle.";
            if(RecordingMode)
            {
                if(Selected<0)return "Find a work tape on the desk. Check both reels.";
                var loaded=Chapter.Tape.Candidates[Selected];
                if(!loaded.Intact)return "Eject B and check both reels.";
                if(!loaded.Prepared)return Chapter.ProtectionDiscovered?"EJECT B. Cover both upper-edge holes, then reload.":"Click B RECORD to try recording.";
                if(dualDeck!=null && !dualDeck.Closed(1))return "Close B's door to use the work tape.";
                if(dualDeck!=null && !dualDeck.Closed(0))return "Close A's door before recording.";
                if(transport.Writing)return "Before noise, click B STOP. Let A keep playing.";
                if(transport.TargetStatus=="PLAYBACK")return "Listen to your copy. Click Submit when satisfied.";
                if(transport.SourceStatus=="END" && transport.RecordedSeconds>0)return "Original finished. Rewind B, then PLAY COPY.";
                if(transport.RecordedSeconds>0)return transport.SourcePlaying?"Let noise pass on A, then click B RESUME REC.":"RESUME REC to continue; rewind B + PLAY COPY to listen.";
                return "Click B RECORD to start both tapes from the beginning.";
            }
            if(Selected>=0)return "Open the recorder controls.";
            if(BenchIndex<0)return "Drag a work tape from the right onto the centre mat.";
            var data=Chapter.Tape.Candidates[BenchIndex];
            if(data.Broken)return "Broken tape. Choose another.";
            if(!data.Intact)return "Hold LMB on each reel + scroll. Check both ends.";
            if(!data.Prepared && Chapter.ProtectionDiscovered)
                return Chapter.LooseStripReady?"Cover both upper-edge holes.":"Show tabs on the mat, then pull tape across the cutter.";
            return "Load the work tape into B.";
        }
        string WorkbenchHint()
        {
            if(benchDrag.AtEdge)return "Keep on this work area / Release LMB to place";
            if(benchDrag.Blocked)return "Path blocked — move around the obstacle / Esc Cancel";
            if(benchGesture==BenchGesture.MoveTape && PreparationDrop(view.ScreenPointToRay(Input.mousePosition)))return "Release LMB · Place on preparation mat";
            if(movingOriginal || benchGesture==BenchGesture.MoveTape)
            {var slot=movingOriginal?sourceSlot:slotHit;return CanReceive(slot,view.ScreenPointToRay(Input.mousePosition))?movingOriginal?"Release LMB · Load A":"Release LMB · Load B":"Drag to the outlined slot / Release on desk / Esc Cancel";}
            if(benchGesture==BenchGesture.PullStrip)return "Drag across the cutter";
            if(benchGesture==BenchGesture.MoveStrip)return "Release over a hole to cover";
            if(benchGesture==BenchGesture.Rotate)return "LMB + RMB  Rotate";
            if(benchGesture==BenchGesture.Spindle)return "Hold LMB + Scroll  Wind reel";
            if(Time.unscaledTime<tapePointerNoticeUntil)return tapePointerNotice;
            if(benchGesture==BenchGesture.Pending)return "Shell · Drag to move / LMB + RMB to rotate";
            return "";
        }
        void DrawWorkbench()
        {
            if(house==null || Chapter==null || house.safeInspection.Active)return;
            var style=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
            style.normal.textColor=new Color(.94f,.94f,.88f);
            if(WorkbenchMode && !benchWaiting)
            {
                if(preparationFocused && PreparationIdle && GUI.Button(PreparationBackRect,"Back to recorder"))FocusPreparation(false);
                if(PreparationButtonVisible)
                {
                    var tape=tapes[BenchIndex];bool tabs=Vector3.Dot(tape.preparation.pieces[0].transform.up,(view.transform.position-tape.root.position).normalized)>.85f;
                    if(GUI.Button(PreparationButtonRect,tabs?"Lay flat":"Show tabs"))PlaceTapeOnPreparationMat(!tabs);
                }
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
                GUI.Label(new Rect(Screen.width-224,24,200,24),"E  Inspect   /   Esc  Back",exit);
            }
        }
        bool DirectTaskNearby()=>StationActive && Vector3.Distance(view.transform.position,transform.position)<2.5f;
    }
}
