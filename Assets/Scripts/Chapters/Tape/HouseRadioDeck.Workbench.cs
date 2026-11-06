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
            if(storageDelivery!=null){CancelStorageDelivery();return true;}
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
            if(!internalDeckHandoff)focusRadioAfterLoad=false;
            CancelWorkbenchGesture();RestorePreparationCamera();RestoreRadioCamera();StopRecordingInput();
            ShowTapeDropPreview(null);
            adhesiveRibbon.gameObject.SetActive(false);benchGesture=BenchGesture.None;benchLeft=benchRight=false;
            WorkbenchMode=false;house.SaveStableProgress();house.SuspendHouseInput();
            house.SyncInputContext();
        }
        public void PlaceOnWorkbench(int index)
        {
            if(!Chapter.AuditionComplete || index<0 || index>=tapes.Length || State==Phase.Loading)return;
            if(Chapter.IsArchived(index) || RecordingOwnerLock(index)!=null)return;
            if(BenchIndex>=0 && BenchIndex!=index){var old=tapes[BenchIndex];old.root.SetPositionAndRotation(old.homeParent.TransformPoint(old.homePosition),old.homeParent.rotation*old.homeRotation);OperationArea.RestorePose(old.root);}
            if(State==Phase.Dragging){CancelTapeDrag();State=Phase.Empty;Selected=-1;Chapter.Tape.Selected=-1;}
            if(Chapter.WorkbenchTapeIndex!=index){var tape=tapes[index];Chapter.WorkbenchEuler=(Quaternion.Inverse(tape.homeParent.rotation*tape.homeRotation)*tape.root.rotation).eulerAngles;}
            Chapter.WorkbenchTapeIndex=index;PoseWorkbenchTape();OpenWorkbench();
        }
        void PoseWorkbenchTape()
        {
            if(BenchIndex<0 || Chapter.IsArchived(BenchIndex))return;var t=tapes[BenchIndex];
            if(OperationArea.For(t.root)!=null)
            {
                bool placed=OperationArea.RestorePose(t.root);
                var stableRotation=t.root.rotation;
                t.root.rotation=t.homeParent.rotation*t.homeRotation*Quaternion.Euler(Chapter.WorkbenchEuler);
                // A tilted shell needs more vertical clearance than the stored flat pose.
                if(placed && OnPreparationMat(t.root) && !OperationArea.For(t.root).Place(t.root,workMat,preparationMatClearance))
                {
                    t.root.rotation=stableRotation;
                    Chapter.WorkbenchEuler=(Quaternion.Inverse(t.homeParent.rotation*t.homeRotation)*stableRotation).eulerAngles;
                }
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
            OperationArea.ClearPlacement(adhesiveStrip);
            adhesiveStrip.SetPositionAndRotation(stripRest.position,stripRest.rotation);
        }
        public void TickWorkbench()
        {
            TickHeldTargetDuringHandoff();
            house.Room.StopMotion();TickPreparationCamera(Time.unscaledDeltaTime);TickRadioCamera(Time.unscaledDeltaTime);
            if(dualDeck!=null && transport!=null)TickAudition();
            if(house.InputContext.ExitPressed(InputContextId.TapeWorkbench,CancelWorkbenchGesture,ExitTapeWorkspace))return;
            if(preparationCameraMoving || radioCameraMoving || storageDelivery!=null)return;
            if(benchWaiting){if(benchGate.Ready)benchWaiting=false;return;}
            if(PollWorkspaceInspection())return;
            if(benchGesture==BenchGesture.None && !movingOriginal)PollLatchedTransportKeys();

            AdvanceInspection(Time.unscaledDeltaTime);
        }
        void ExitTapeWorkspace()
        {
            if(radioFocused){FocusRadio(false);return;}
            if(preparationFocused){FocusPreparation(false);return;}
            if(house.InStation){house.ExitTapeDeskView();return;}
            CloseWorkbench();CloseRecording();house.SuspendHouseInput();
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
            if(storageDelivery!=null || radioCameraMoving || (preparationCameraMoving && !preparationCarryReturn) || !WorkbenchMode || benchWaiting || !house.InputContext.Allows(InputContextId.TapeWorkbench))return;
            var ray=view.ScreenPointToRay(point);Physics.SyncTransforms();
            if(type==EventType.MouseDown && !movingOriginal && benchGesture==BenchGesture.None && TapeUIAt(point))return;
            if(type==EventType.MouseDown && button==0 && StorageClick(PickWorkbenchTarget(ray)))return;
            if(SourcePointer(type,button,ray))return;
            if(type==EventType.MouseDown && button==0 && benchGesture==BenchGesture.None)
            {
                var visible=PickWorkbenchTarget(ray);
                if(StorageClick(visible))return;
                if(visible==sourceSlot || visible==slotHit)
                {FinishWorkbenchTransfer();OpenRecording();EjectDualDeck(visible==sourceSlot?0:1);return;}
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
            if(type==EventType.ScrollWheel)return;
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
                var tape=tapes[BenchIndex];var target=PickWorkbenchTarget(ray);benchSpindle=-1;
                if(candidate==BenchIndex || benchSpindle>=0){benchGesture=benchSpindle>=0?BenchGesture.Spindle:benchRight?BenchGesture.Rotate:BenchGesture.Pending;}
                return;
            }
            if(type==EventType.MouseDrag)
            {
                if(benchGesture==BenchGesture.Rotate && benchLeft && benchRight)
                {Chapter.WorkbenchEuler+=new Vector3(-delta.y*.4f,delta.x*.4f,0);PoseWorkbenchTape();}
                if(benchGesture==BenchGesture.Pending && benchSpindle<0 && Vector2.Distance(point,benchPress)>7)
                {preparationDragFromMat=preparationFocused && OnPreparationMat(tapes[BenchIndex].root);preparationDragPoint=benchPress;benchGesture=BenchGesture.MoveTape;TapeSound(5,tapes[BenchIndex].root.position);benchDrag.BeginScreenCarry(tapes[BenchIndex].root,benchGrabRay,view.transform.forward);}
                if(benchGesture==BenchGesture.MoveTape && BenchIndex>=0)
                {MoveRadioTape(benchDrag,ray,FinishedDropReady(BenchIndex,ray)?workbenchVisuals.finishedHit.transform:CanReceive(slotHit,ray)?slotHit.transform:PreparationDrop(ray)?workMat.transform:null);TryReturnWhileDragging(ray,point);}
                if(benchGesture==BenchGesture.PullStrip || benchGesture==BenchGesture.MoveStrip)
                {
                    // Pulling remains on its initial plane. A detached patch can approach the visible cassette surface.
                    int side=benchGesture==BenchGesture.MoveStrip?PatchTarget(point):-1;
                    if(benchGesture==BenchGesture.PullStrip)
                    {
                        var plane=new Plane(view.transform.forward,pullStart);
                        if(plane.Raycast(ray,out float distance) && plane.Raycast(benchGrabRay,out float startDistance))
                        {
                            float pulled=Mathf.Clamp(Vector3.Dot(ray.GetPoint(distance)-benchGrabRay.GetPoint(startDistance),-view.transform.up),0,.12f);
                            adhesiveStrip.position=pullStart-view.transform.up*pulled;
                        }
                    }
                    else if(side>=0)
                    {
                        var tape=tapes[BenchIndex];
                        benchDrag.MoveOverSurface(tape.preparation.pieces[side].transform.position,tape.root,true);
                    }
                    else benchDrag.Move(ray);
                    if(benchGesture==BenchGesture.PullStrip)
                    {
                        if(workbenchVisuals!=null)workbenchVisuals.Pull(Vector3.Distance(pullStart,adhesiveStrip.position));
                        // Screen-down pull replaces the pixel-precise cutter crossing.
                        if(Vector3.Dot(adhesiveStrip.position-pullStart,-view.transform.up)>=.035f)
                        {workbenchVisuals?.Cut();benchGesture=BenchGesture.MoveStrip;Chapter.LooseStripReady=true;benchDrag.End(false);benchDrag.Begin(adhesiveStrip,ray,view.transform.forward,.008f,1.8f,true,workbenchVisuals!=null?workbenchVisuals.dispenser.transform:null);TapeSound(8,adhesiveCutter.bounds.center);}
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
                        if(FinishStorageDrop(index,ray,benchDrag))
                        {benchGesture=benchRight?BenchGesture.WaitRelease:BenchGesture.None;return;}
                        bool over=CanReceive(slotHit,ray);benchDrag.MoveScreenCarry(ray);
                        bool accepted=!benchDrag.Blocked && over;
                        string rejection=accepted?null:TapeDropHint(slotHit,ray,benchDrag);
                        bool onMat=!accepted && !benchDrag.Blocked && PreparationDrop(ray);
                        bool placed=!accepted && !onMat && !OverStorageFootprint(tapes[index].root) && benchDrag.TryPlaceInArea();benchDrag.End(!accepted && !placed && !onMat);
                        if(!accepted && !onMat && !placed)ShowTapePointerNotice(rejection);
                        if(onMat || placed)TapeSound(5,tapes[BenchIndex].root.position);
                        if(onMat){if(!PlaceTapeOnPreparationMat(false))PoseWorkbenchTape();}

                        if(accepted && State==Phase.Empty && SelectForDrag(index) && InsertDragged()){Chapter.WorkbenchTapeIndex=-1;FinishWorkbenchTransfer();return;}
                        PoseWorkbenchTape();
                    }
                    if(benchGesture==BenchGesture.MoveStrip)
                    {
                        int side=LooseStripDropTarget(point);
                        if(side>=0)
                        {
                            var data=Chapter.Tape.Candidates[BenchIndex];
                            if(side==0)data.HoleA=true;else data.HoleB=true;
                            if(workbenchVisuals!=null)workbenchVisuals.intact[BenchIndex].Cover(side,1);
                            TapeSound(9,tapes[BenchIndex].root.position);
                            OperationArea.ClearPlacement(adhesiveStrip);
                            Chapter.LooseStripReady=false;adhesiveStrip.gameObject.SetActive(false);
                        }
                        else
                        {
                            RestStrip();
                            ShowTapePointerNotice(PatchLockReason(BenchIndex)??"Aim at an open, visible protection hole.");
                        }
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
            if(Chapter.Completed)return "All 3 copies saved. RMB back to the desk, then RMB to stand up.";
            if(Selected<0 && Chapter.RecordingTapeIndex>=0 && transport.RecordedSeconds>0)
                return "Open the box. Drag the recorded B tape into its finished compartment; reload B to revise.";
            if(!Chapter.SourceLoaded)return "Open the box. Choose Original "+(CurrentCopy+1).ToString("00")+" into A.";
            if(!Chapter.AuditionComplete)return transport.SourcePlaying?"Listen to A: notice the noise and what follows.":"Press F / A PLAY to hear the original.";
            if(State==Phase.Loading || DeckBusy(0) || DeckBusy(1))return "Wait for the tape and door to settle.";
            if(RecordingMode)
            {
                if(Selected<0)return "Choose a work tape from the box. Seal both holes.";
                var loaded=Chapter.Tape.Candidates[Selected];
                if(!loaded.Prepared)return Chapter.ProtectionDiscovered?"EJECT B. Cover both upper-edge holes, then reload.":"Press S / B REC to try recording.";
                if(dualDeck!=null && !dualDeck.Closed(1))return "Close B's door to use the work tape.";
                if(dualDeck!=null && !dualDeck.Closed(0))return "Close A's door before recording.";
                if(transport.Writing)return transport.SourceStatus=="END"?"A has ended. Press S to stop B, then rewind B to listen.":"Press S before noise to pause B. Press S after it to resume.";
                if(transport.TargetStatus=="PLAYBACK")return "Listen to your copy, eject B, then drag it into the box's finished compartment.";
                if(transport.SourceStatus=="END" && transport.RecordedSeconds>0)return "Pause B with S. J / L wind both loaded tapes. Eject B to return the copy.";
                if(transport.RecordedSeconds>0)return transport.SourcePlaying?"Press S to resume B. Press S again before noise.":"J / L wind A and B. Eject B and return it to the box when ready. R retries.";
                return "J / L wind both loaded tapes; F plays A. S starts / pauses B recording.";
            }
            if(Selected>=0)return "Open the recorder controls.";
            if(BenchIndex<0)return "Click a work tape in the box to place it on the centre mat.";
            var data=Chapter.Tape.Candidates[BenchIndex];
            if(!OnPreparationMat(tapes[BenchIndex].root))return "Drag this tape onto the centre mat.";
            if(!data.Prepared)
                return Chapter.LooseStripReady?"Cover both upper-edge holes.":"Rotate to reveal the holes, then drag the tape tab down.";
            return "Load the work tape into B.";
        }
        string WorkbenchHint()
        {
            if(benchGesture==BenchGesture.MoveTape && BenchIndex>=0 && OverFinishedCompartment(view.ScreenPointToRay(Input.mousePosition),tapes[BenchIndex].root))
                return FinishedTapeGate(BenchIndex)??"Release LMB · Return copy for checking";
            if(benchDrag.AtEdge)return "Keep on this work area / Release LMB to place";
            if(benchDrag.Blocked)return "Path blocked — move around the obstacle / RMB Cancel";
            if(benchGesture==BenchGesture.MoveTape && PreparationDrop(view.ScreenPointToRay(Input.mousePosition)))return "Release LMB · Place on preparation mat";
            if(movingOriginal || benchGesture==BenchGesture.MoveTape)
            {var slot=movingOriginal?sourceSlot:slotHit;return TapeDropHint(slot,view.ScreenPointToRay(Input.mousePosition),benchDrag);}
            if(benchGesture==BenchGesture.PullStrip)return "Drag down to tear off a strip";
            if(benchGesture==BenchGesture.MoveStrip)return "Release over a hole to cover";
            if(benchGesture==BenchGesture.Rotate)return "LMB + RMB  Rotate";
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
                var e=Event.current;
                if(e.type==EventType.MouseDown || e.type==EventType.MouseUp || e.type==EventType.MouseDrag || e.type==EventType.ScrollWheel)
                    WorkbenchPointer(e.type,e.button,new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y),new Vector2(e.delta.x,-e.delta.y),-e.delta.y/3);
                var hint=WorkbenchHint();
                if(hint!="")GUI.Label(new Rect(24,76,Screen.width-48,30),hint,style);
            }
            if(WorkbenchMode || RecordingMode)
            {
                GUI.Label(new Rect(24,24,Mathf.Min(440,Screen.width-160),48),NextTapeTask(),style);
                var exit=new GUIStyle(style){alignment=TextAnchor.UpperRight,fontSize=13};
                exit.normal.textColor=new Color(.7f,.72f,.7f);
                GUI.Label(new Rect(Screen.width-224,24,200,24),"E  Inspect   /   RMB  Back",exit);
            }
        }
        bool DirectTaskNearby()=>StationActive && Vector3.Distance(view.transform.position,transform.position)<2.5f;
    }
}
