using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public RadioTapeChapter Chapter {get;private set;}
        public bool StationActive {get;private set;}
        public bool WaitingForRelease=>waitingForRelease;
        public string Notice {get;private set;}="";
        public Light stationLight;
        public bool DraggingTape=>draggingTape;
        bool draggingTape,dropReady;
        Ray dragRay;
        readonly ObjectDragSession tapeDrag=new ObjectDragSession();
        bool TapeReceiver(Collider receiver,Ray ray)=>TapeDropAllowed(receiver,ray,tapeDrag.Item);
        bool waitingForRelease,leftWasDown,rightWasDown,focused=true;
        float statusHeight=60;
        int heldSpindle=-1;
        readonly float[] visualTapePositions=new float[3];
        public bool IsEntryHit(Collider hit)
        {
            if(workbenchVisuals!=null && (hit==workbenchVisuals.lidHit || hit==workbenchVisuals.finishedHit))return true;
            if(workbenchVisuals!=null && hit!=null)foreach(var grip in workbenchVisuals.candidateGrips)if(hit==grip)return true;
            if(hit==null)return false;if(dualDeck!=null && dualDeck.Command(hit)!=null)return true;if(SourceHit(hit))return true;if(BenchHit(hit))return true;if(RecordingHit(hit))return true;if(hit==radioHit || hit==slotHit)return true;
            foreach(var t in tapes)if(hit==t.hit || hit==t.leftSpindleHit || hit==t.rightSpindleHit)return true;
            return false;
        }
        public Transform InspectionSource(Ray ray)
        {
            var hit=Pick(ray);if(hit==radioHit || hit==slotHit || hit==sourceSlot || dualDeck!=null&&dualDeck.Command(hit)!=null)return transform;
            if(originals!=null)foreach(var original in originals)if(hit==original.hit)return original.tape;
            if(workbenchVisuals!=null && hit!=null)for(int i=0;i<workbenchVisuals.candidateGrips.Length;i++)if(hit==workbenchVisuals.candidateGrips[i])return tapes[i].root;
            foreach(var t in tapes)if(hit==t.hit || hit==t.leftSpindleHit || hit==t.rightSpindleHit)return t.root;
            return null;
        }
        public bool TryInspect(Ray ray)
        {
            bool workspace=WorkbenchMode || RecordingMode;
            if(radioCameraMoving || preparationCameraMoving || house==null || house.safeInspection==null || waitingForRelease || draggingTape || movingOriginal || benchDrag.Active || tapeDrag.Active || Smoothing ||
                benchGesture!=BenchGesture.None || benchLeft || benchRight || (!workspace && (leftWasDown || rightWasDown)) || heldSpindle>=0 ||
                DeckBusy(0) || DeckBusy(1) || transferA || transferB || State==Phase.Loading || State==Phase.Returning || State==Phase.Playing)return false;
            var source=InspectionSource(ray);if(source==null && State==Phase.Dragging)source=tapes[Selected].root;
            if(source==null)return false;
            house.safeInspection.feedback.mute=house.Muted;
            return house.safeInspection.Open(house.safeInspection.ForSource(source));
        }
        bool PollWorkspaceInspection()
        {
            ReleaseInputGate(PlayerInputPolicy.ActionsReleased);
            if(!Input.GetKeyDown(KeyCode.E) || Input.GetMouseButton(0) || Input.GetMouseButton(1) || TapeUIAt(Input.mousePosition))return false;
            return TryInspect(view.ScreenPointToRay(Input.mousePosition));
        }
        public void SetStationActive(bool active)
        {
            if(!initialized)return;
            if(!active){focusRadioAfterLoad=false;RestoreRadioCamera();RestorePreparationCamera();StopRecordingInput();}
            CancelTapeDrag();ReleaseSpindle();StationActive=active;waitingForRelease=active;leftWasDown=rightWasDown=false;
            if(stationLight!=null)stationLight.enabled=active;
            if(active && house!=null)speaker.mute=house.Muted;
            if(!active)
            {
                if(State==Phase.Dragging || State==Phase.Returning)ResetDeck();
                if(State==Phase.Playing)TogglePlayback();
                animator.speed=0;
            }
        }
        public void ReleaseInputGate(bool released)
        {if(waitingForRelease && released && focused && StationActive && (house==null || (!house.ShowHelp && !house.safeInspection.BlocksInput))){waitingForRelease=false;leftWasDown=rightWasDown=false;if(State==Phase.Loading)animator.speed=1;}}
        public void SuspendInput()
        {
            focusRadioAfterLoad=false;
            if(WorkbenchMode)CloseWorkbench();
            StopRecordingInput();
            if(State==Phase.Dragging)PutBack();
            CancelTapeDrag();ReleaseSpindle();waitingForRelease=true;leftWasDown=rightWasDown=false;
            if(State==Phase.Playing)TogglePlayback();animator.speed=0;
        }
        void OnApplicationFocus(bool value){focused=value;if(!value && initialized)SuspendInput();}
        public void ReleaseSpindle(){heldSpindle=-1;Chapter?.Tape.EndSpindle();}
        void SetHeldHits(bool enabled)
        {
            if(Selected<0)return;var t=tapes[Selected];t.hit.enabled=enabled;
            if(t.leftSpindleHit!=null)t.leftSpindleHit.enabled=enabled;
            if(t.rightSpindleHit!=null)t.rightSpindleHit.enabled=enabled;
        }
        void BeginTapeDrag(Ray ray)
        {
            if(State!=Phase.Dragging)return;
            ReleaseSpindle();draggingTape=true;dropReady=false;dragRay=ray;
            tapeDrag.BeginScreenCarry(tapes[Selected].root,ray,view.transform.forward);
            // The held cassette must not intercept the cursor ray aimed at the slot.
            SetHeldHits(false);
        }
        void CancelTapeDrag(){tapeDrag.End(false);ShowTapeDropPreview(null);if(draggingTape)SetHeldHits(true);draggingTape=dropReady=false;}
        void EndTapeDrag(bool overSlot)
        {
            if(!overSlot && tapeDrag.TryPlaceInArea()){CancelTapeDrag();PutBack();return;}
            CancelTapeDrag();
            if(overSlot && InsertDragged())return;
            PutBack();
        }
        public void PoseDraggedTape(float dt)
        {
            if(Selected<0 || State!=Phase.Dragging || !draggingTape)return;
            MoveRadioTape(tapeDrag,dragRay,TapeReceiver(slotHit,dragRay)?slotHit.transform:TapeReceiver(workMat,dragRay)?workMat.transform:null);
        }

        public void TickStation(float dt,Ray ray,bool left,bool right,float wheel,bool inspect,bool allReleased)
        {
            if(!StationActive || !focused)return;
            if(house!=null && (house.ShowHelp || house.safeInspection.BlocksInput))return;
            if(waitingForRelease){ReleaseInputGate(allReleased);return;}
            var point=view.WorldToScreenPoint(ray.GetPoint(1));
            if(PointerOverUI(point))
            {
                if(draggingTape){dragRay=ray;dropReady=false;if(!left)EndTapeDrag(false);}
                if(!left)ReleaseSpindle();leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);return;
            }
            if(inspect && !left && !right && TryInspect(ray))return;
            var hit=Pick(ray);
            if(draggingTape)
            {
                dragRay=ray;bool overSlot=TapeReceiver(slotHit,ray),overMat=TapeReceiver(workMat,ray);
                MoveRadioTape(tapeDrag,ray,overSlot?slotHit.transform:overMat?workMat.transform:null);
                dropReady=!tapeDrag.Blocked && (overSlot || overMat);
                if(!left){if(overMat && !tapeDrag.Blocked)PlaceOnWorkbench(Selected);else EndTapeDrag(overSlot && !tapeDrag.Blocked);}
                leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);return;
            }
            if(left && !leftWasDown)
            {
                if(StorageClick(hit)){leftWasDown=true;return;}
                if(dualDeck!=null && PressDualControl(dualDeck.Command(hit))){leftWasDown=left;return;}
                if(TryBeginOriginalInteraction(ray)){leftWasDown=left;return;}
                if(TryBeginWorkbenchInteraction(ray)){leftWasDown=left;return;}
                if(State!=Phase.Loading && State!=Phase.Returning)
                {
                    if(hit==radioHit || hit==slotHit || RecordingHit(hit))TogglePlayback();
                    for(int i=0;i<tapes.Length;i++)if(hit==tapes[i].hit || hit==tapes[i].leftSpindleHit || hit==tapes[i].rightSpindleHit)
                    {
                        if(i==Selected)BeginLoadedDrag(ray);
                        else if(State==Phase.Empty && SelectForDrag(i))BeginTapeDrag(ray);
                        break;
                    }
                }
            }
            if(!left)ReleaseSpindle();
            leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);
        }
        public void AdvanceInspection(float dt)
        {
            if(Chapter==null)return;
            for(int i=0;i<tapes.Length;i++)
            {
                var t=tapes[i];var data=Chapter.Tape.Candidates[i];
                if(workbenchVisuals!=null){workbenchVisuals.Inspect(i,data);continue;}
                if(t.preparation!=null)t.preparation.ShowState(data);
                if(t.damageMarker!=null)t.damageMarker.SetActive(data.Broken);
                if(i==Selected && (State==Phase.Loading || State==Phase.Playing || State==Phase.Paused))continue;
                visualTapePositions[i]=Mathf.Lerp(visualTapePositions[i],data.Position,1-Mathf.Exp(-Mathf.Max(0,dt)/.055f));
                // Imported PlaybackLoop rotates the spindle around local Y (not the shell's forward axis).
                var turn=Quaternion.AngleAxis(-visualTapePositions[i]*420,Vector3.up);
                t.leftReel.localRotation=turn;t.rightReel.localRotation=turn;
            }
        }
        public bool PointerOverUI(Vector2 point)
        {
            if(house!=null && !house.InStation)return false;
            var gui=new Vector2(point.x,Screen.height-point.y);
            // The footer is passive help text, not a control. It must not make
            // authored cassettes near the lower edge of the view unpickable.
            return new Rect(12,12,Mathf.Min(740,Screen.width-250),statusHeight).Contains(gui)
                || ((State==Phase.Playing || State==Phase.Paused) && new Rect(20,132,230,32).Contains(gui));
        }
        void BeginLoadedDrag(Ray ray)
        {
            if(Selected<0 || State==Phase.Loading)return;
            int index=Selected;var root=tapes[index].root;
            var position=root.position;var rotation=root.rotation;
            ResetDeck();SelectForDrag(index);root.SetPositionAndRotation(position,rotation);BeginTapeDrag(ray);
        }
        public void TakeOut(){if(dualDeck!=null)EjectDualDeck(1);else ResetDeck();}
        public void PutBack(){if(State==Phase.Dragging){CancelTapeDrag();ReleaseSpindle();State=Phase.Returning;}}
        public void AdvanceReturn(float dt)
        {
            if(State!=Phase.Returning || Selected<0)return;
            var t=tapes[Selected];var position=t.homeParent.TransformPoint(t.homePosition);var rotation=t.homeParent.rotation*t.homeRotation;
            Vector3 placed;Quaternion facing;if(OperationArea.TryGetPose(t.root,out placed,out facing)){position=placed;rotation=facing;}
            t.root.position=Vector3.MoveTowards(t.root.position,position,Mathf.Max(0,dt)*1.8f);
            t.root.rotation=Quaternion.RotateTowards(t.root.rotation,rotation,Mathf.Max(0,dt)*540);
            if(Vector3.Distance(t.root.position,position)<.001f && Quaternion.Angle(t.root.rotation,rotation)<.1f)ResetDeck();
        }
        public void DrawStation(GUIStyle style)
        {
            if(RecordingMode || WorkbenchMode)return;
            if(!StationActive)return;
            string status=draggingTape?(dropReady?"Release LMB to insert   /   RMB  Return":"Drag to slot; release to insert."):
                State==Phase.Dragging?"Drag to slot; release outside to return.":
                Chapter.Completed?"Tape restored. Playing.":"Hold LMB to pick up; drag to slot.";
            if(State==Phase.Loading)status="Loading tape...";if(State==Phase.Returning)status="Returning tape...";
            if(Selected>=0)status+="\nTape "+(Selected+1)+"  /  Checked "+Mathf.RoundToInt(Chapter.Tape.InspectionProgress*100)+"%";
            if(Notice!="")status+="\n"+Notice;
            float textWidth=Mathf.Min(716,Screen.width-274);statusHeight=Mathf.Clamp(style.CalcHeight(new GUIContent(status),textWidth)+16,46,108);
            InteractionUI.Label(new Rect(24,20,textWidth,statusHeight-16),status,style);
            if(waitingForRelease || house.ShowHelp)return;
            if(State==Phase.Playing || State==Phase.Paused)
            {
                if(InteractionUI.Button(new Rect(20,132,110,32),State==Phase.Playing?"Pause":"Play"))TogglePlayback();
                if(InteractionUI.Button(new Rect(140,132,110,32),"Eject")){TakeOut();waitingForRelease=true;}
            }
        }
    }
}
