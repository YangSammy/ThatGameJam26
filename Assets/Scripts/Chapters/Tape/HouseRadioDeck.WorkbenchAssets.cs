using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public TapeWorkbenchVisuals workbenchVisuals;
        int smoothingTape=-1,smoothingHole;
        float smoothingTime;
        bool Smoothing=>smoothingTape>=0;
        public LineRenderer preparationDropPreview;
        public Transform preparationTabsPose;
        public SceneViewAnchor radioCloseView;
        bool radioFocused,radioCameraMoving;
        float radioCameraTime,radioStartFov,radioReturnFov;
        Vector3 radioStartPosition,radioReturnPosition;
        Quaternion radioStartRotation,radioReturnRotation;
        bool focusRadioAfterLoad;
        void QueueLoadedRadioFocus()
        {
            // RestoreWorkingTape can load during scene restoration; only a live workspace changes the camera.
            if(WorkbenchMode || RecordingMode)focusRadioAfterLoad=true;
        }
        void FocusLoadedRadio()
        {
            if(!focusRadioAfterLoad || benchDrag.Active || movingOriginal || draggingTape)return;
            focusRadioAfterLoad=false;
            if(!WorkbenchMode && !RecordingMode && !transferA && !transferB)return;
            if(WorkbenchMode)FinishWorkbenchTransfer();
            if(!RecordingMode)OpenRecording();
            if(RecordingMode)FocusRadio(true);
        }
        Rect RadioBackRect=>new Rect(24,100,150,30);
        public void FocusRadio(bool focus)
        {
            if(view==null || radioCloseView==null || focus==radioFocused || preparationCameraMoving || benchDrag.Active)return;
            if(focus){RestorePreparationCamera();radioReturnPosition=view.transform.position;radioReturnRotation=view.transform.rotation;radioReturnFov=view.fieldOfView;}
            radioStartPosition=view.transform.position;radioStartRotation=view.transform.rotation;radioStartFov=view.fieldOfView;
            radioFocused=focus;radioCameraMoving=true;radioCameraTime=0;
        }
        void TickRadioCamera(float dt)
        {
            if(!radioCameraMoving || house.safeInspection.Active)return;
            radioCameraTime+=dt;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(radioCameraTime/preparationViewDuration));
            view.transform.SetPositionAndRotation(Vector3.Lerp(radioStartPosition,radioFocused?radioCloseView.transform.position:radioReturnPosition,t),Quaternion.Slerp(radioStartRotation,radioFocused?radioCloseView.transform.rotation:radioReturnRotation,t));
            view.fieldOfView=Mathf.Lerp(radioStartFov,radioFocused?radioCloseView.fieldOfView:radioReturnFov,t);
            if(t>=1)radioCameraMoving=false;
        }
        void RestoreRadioCamera()
        {
            if(!radioFocused && !radioCameraMoving)return;
            view.transform.SetPositionAndRotation(radioReturnPosition,radioReturnRotation);view.fieldOfView=radioReturnFov;
            radioFocused=radioCameraMoving=false;
        }
        public SceneViewAnchor preparationView;
        [Min(.1f)] public float preparationViewDuration=.45f;
        bool preparationFocused,preparationCameraMoving,preparationCarryReturn,preparationDragFromMat;
        Vector2 preparationDragPoint;
        void TryReturnWhileDragging(Ray ray,Vector2 point)
        {
            preparationDragPoint=point;
            if(!preparationFocused || preparationCameraMoving || !preparationDragFromMat)return;
            var bounds=workMat.bounds;bounds.Expand(.06f);float distance;
            if(bounds.IntersectRay(ray,out distance))return;
            preparationCarryReturn=true;FocusPreparation(false);
        }
        float preparationCameraTime,preparationReturnFov,preparationStartFov;
        Vector3 preparationReturnPosition,preparationStartPosition;
        Quaternion preparationReturnRotation,preparationStartRotation;
        Rect PreparationBackRect=>new Rect(24,138,150,30);
        void FocusPreparation(bool focus)
        {
            if(view==null || preparationView==null || focus==preparationFocused)return;
            if(focus){preparationReturnPosition=view.transform.position;preparationReturnRotation=view.transform.rotation;preparationReturnFov=view.fieldOfView;}
            preparationStartPosition=view.transform.position;preparationStartRotation=view.transform.rotation;preparationStartFov=view.fieldOfView;
            preparationFocused=focus;preparationCameraMoving=true;preparationCameraTime=0;
        }
        void TickPreparationCamera(float dt)
        {
            if(!preparationCameraMoving || house.safeInspection.Active)return;
            bool carrying=preparationCarryReturn && benchGesture==BenchGesture.MoveTape && benchDrag.Active;
            Vector3 grip=carrying?benchDrag.ScreenCarryGrip:Vector3.zero;
            Vector3 screen=carrying?view.WorldToScreenPoint(grip):Vector3.zero;
            float oldFov=view.fieldOfView;
            preparationCameraTime+=dt;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(preparationCameraTime/preparationViewDuration));
            view.transform.SetPositionAndRotation(Vector3.Lerp(preparationStartPosition,preparationFocused?preparationView.transform.position:preparationReturnPosition,t),Quaternion.Slerp(preparationStartRotation,preparationFocused?preparationView.transform.rotation:preparationReturnRotation,t));
            view.fieldOfView=Mathf.Lerp(preparationStartFov,preparationFocused?preparationView.fieldOfView:preparationReturnFov,t);
            if(carrying)
            {
                screen.z*=Mathf.Tan(oldFov*Mathf.Deg2Rad*.5f)/Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f);
                benchDrag.Item.position+=view.ScreenToWorldPoint(screen)-grip;
                benchDrag.RebaseScreenCarry(view.ScreenPointToRay(preparationDragPoint),view.transform.forward);
                ShowTapeDropPreview(null);
            }
            if(t>=1)
            {
                preparationCameraMoving=false;preparationCarryReturn=false;
                if(!carrying){benchWaiting=true;benchGate.Arm();house.InputContext.Handoff();}
            }
        }
        void RestorePreparationCamera()
        {
            if(!preparationFocused && !preparationCameraMoving)return;
            view.transform.SetPositionAndRotation(preparationReturnPosition,preparationReturnRotation);view.fieldOfView=preparationReturnFov;
            preparationFocused=preparationCameraMoving=preparationCarryReturn=false;
        }
        bool PreparationIdle=>!preparationCameraMoving && WorkbenchMode && BenchIndex>=0 && Selected!=BenchIndex && !benchWaiting && !Smoothing && !movingOriginal && benchGesture==BenchGesture.None;
        bool OnPreparationMat(Transform item)=>workAnchor!=null && Vector2.Distance(new Vector2(item.position.x,item.position.z),new Vector2(workAnchor.position.x,workAnchor.position.z))<.13f;
        bool PreparationAvailable(Transform item)
        {
            foreach(var tape in tapes)if(tape.root!=item && tape.root.gameObject.activeInHierarchy && OnPreparationMat(tape.root) && Selected!=System.Array.IndexOf(tapes,tape))return false;
            return true;
        }
        bool PreparationDrop(Ray ray)=>BenchIndex>=0 && PreparationAvailable(tapes[BenchIndex].root) &&
            !TapeUIAt(view.WorldToScreenPoint(ray.GetPoint(1))) && DragReceiverProbe.Accepts(ray,workMat.transform,tapes[BenchIndex].root,.015f,3f,c=>c==workMat,workMat);
        Rect PreparationButtonRect=>new Rect(24,100,150,30);
        bool PreparationButtonVisible=>PreparationIdle && OnPreparationMat(tapes[BenchIndex].root);
        public bool PlaceTapeOnPreparationMat(bool showTabs)
        {
            if(BenchIndex<0 || workAnchor==null || !PreparationAvailable(tapes[BenchIndex].root))return false;
            var tape=tapes[BenchIndex];var oldPosition=tape.root.position;var oldRotation=tape.root.rotation;
            var pose=showTabs && preparationTabsPose!=null?preparationTabsPose:workAnchor;
            tape.root.SetPositionAndRotation(pose.position,pose.rotation);
            // Imported intact and broken shells have different node orientations: align the actual hole surface.
            if(showTabs)
            {
                var hole=tape.preparation.pieces[0].transform;
                tape.root.rotation=Quaternion.FromToRotation(hole.up,(view.transform.position-hole.position).normalized)*tape.root.rotation;
            }
            var area=OperationArea.For(tape.root);
            if(area==null || !area.Place(tape.root,workMat)){tape.root.SetPositionAndRotation(oldPosition,oldRotation);ShowTapePointerNotice("Clear the preparation mat first");return false;}
            Chapter.WorkbenchEuler=(Quaternion.Inverse(tape.homeParent.rotation*tape.homeRotation)*tape.root.rotation).eulerAngles;
            FocusPreparation(true);house.SaveStableProgress();return true;
        }
        string tapePointerNotice;
        float tapePointerNoticeUntil;
        void ShowTapePointerNotice(string text){tapePointerNotice=text;tapePointerNoticeUntil=Time.unscaledTime+2.5f;}
        int CandidateForHit(Collider hit)
        {
            if(hit==null)return -1;
            for(int i=0;i<tapes.Length;i++)
            {
                var t=tapes[i];
                if(hit==t.hit || hit==t.leftSpindleHit || hit==t.rightSpindleHit ||
                    (workbenchVisuals!=null && i<workbenchVisuals.candidateGrips.Length && hit==workbenchVisuals.candidateGrips[i]))return i;
                if(t.preparation!=null)foreach(var piece in t.preparation.pieces)if(piece!=null && hit.gameObject==piece)return i;
            }
            return -1;
        }
        // Resolve small parts only inside the foremost cassette; never select through another object.
        public Collider PickWorkbenchTarget(Ray ray)
        {
            var visible=StudyEntry.PickVisible(ray);
            // Broad loading receivers must not swallow their own recorder's physical keys.
            if(dualDeck!=null && (visible==sourceSlot || visible==slotHit || visible==radioHit))
            {
                Collider key=null;float nearest=3f;
                foreach(var candidate in dualDeck.keyHits)
                {
                    RaycastHit hit;
                    if(candidate!=null && candidate.enabled && candidate.gameObject.activeInHierarchy && candidate.Raycast(ray,out hit,nearest))
                    {key=candidate;nearest=hit.distance;}
                }
                if(key!=null)return key;
            }
            int index=CandidateForHit(visible);
            if(index<0)return visible;
            var tape=tapes[index];
            if(tape.preparation!=null)foreach(var piece in tape.preparation.pieces)
                if(piece!=null && Hit(piece.GetComponent<Collider>(),ray))return piece.GetComponent<Collider>();
            if(Hit(tape.leftSpindleHit,ray))return tape.leftSpindleHit;
            if(Hit(tape.rightSpindleHit,ray))return tape.rightSpindleHit;
            return visible;
        }
        string WorkTapeLockReason(int index)
        {
            if(Chapter.Completed)return "All copies complete";
            if(StorageClosed(tapes[index].root))return "Open the storage box first";
            if(!Chapter.AuditionComplete)return "Listen to A first";
            if(State==Phase.Loading || State==Phase.Returning || DeckBusy(1))return "Wait for B to finish loading";
            if(Selected>=0 && index!=Selected)return "Eject the tape from B first";
            return null;
        }
        public string WorkbenchObjectTooltip(Collider hit)
        {
            if(workbenchVisuals==null || Chapter==null || hit==null)return null;
            var source=SourceHint(hit);if(source!=null)return source;
            int candidate=CandidateForHit(hit);
            if(candidate>=0)
            {
                var reason=WorkTapeLockReason(candidate);if(reason!=null)return reason;
                var tape=tapes[candidate];
                if(hit==tape.leftSpindleHit || hit==tape.rightSpindleHit)
                    return (hit==tape.leftSpindleHit?"Left reel":"Right reel")+" · Hold LMB + Scroll";
                if(hit==tape.hit || (candidate<workbenchVisuals.candidateGrips.Length && hit==workbenchVisuals.candidateGrips[candidate]))
                    return "Shell · LMB drag to move · LMB + RMB to rotate";
            }
            if(hit==workMat)return "Preparation mat · Drop a work tape here";
            if(hit==adhesiveTab)return "Tape tab · LMB drag across the cutter";
            var hint=workbenchVisuals.Tooltip(hit);if(hint!=null)return hint;
            for(int n=0;n<tapes.Length;n++)for(int side=0;side<2;side++)
            {
                if(hit.gameObject!=tapes[n].preparation.pieces[side])continue;
                var data=Chapter.Tape.Candidates[n];bool covered=side==0?data.HoleA:data.HoleB;
                return (side==0?"Left hole · ":"Right hole · ")+(covered?"Covered · Drag to peel":"Drop one patch here");
            }
            return null;
        }
        bool StorageClick(Collider hit)
        {
            if(workbenchVisuals==null)return false;
            if(hit==workbenchVisuals.lidHit){workbenchVisuals.ToggleLid();return true;}
            if(hit==workbenchVisuals.finishedHit){OpenRecording();return true;}
            return false;
        }
        bool StorageClosed(Transform tape)=>workbenchVisuals!=null && workbenchVisuals.InStorage(tape) && (!workbenchVisuals.LidOpen || workbenchVisuals.LidBusy);
        void StartSmooth(int tape,int hole)
        {smoothingTape=tape;smoothingHole=hole;smoothingTime=0;}
        void CancelSmooth()
        {
            if(!Smoothing)return;smoothingTape=-1;RestStrip();
        }
        void TickWorkbenchAssets(float dt)
        {
            if(workbenchVisuals==null || Chapter==null)return;
            workbenchVisuals.Tick(dt);
            // A06's torn piece is offset inside the legacy strip root. Keep its
            // grab region on the visible piece without moving the authored rest pose.
            if(adhesiveStrip!=null && adhesiveStrip.gameObject.activeInHierarchy &&
                workbenchVisuals.loosePiece!=null && workbenchVisuals.loosePiece.gameObject.activeInHierarchy)
            {
                var stripHit=adhesiveStrip.GetComponent<BoxCollider>();
                if(stripHit!=null)stripHit.center=adhesiveStrip.InverseTransformPoint(workbenchVisuals.loosePiece.bounds.center);
            }
            for(int i=0;i<tapes.Length;i++)workbenchVisuals.Inspect(i,Chapter.Tape.Candidates[i]);
            if(Smoothing)
            {
                if(!WorkbenchMode || !focused || house.safeInspection.Active){CancelSmooth();}
                else
                {
                    smoothingTime+=dt;float v=Mathf.Clamp01(smoothingTime/.45f);
                    var visual=workbenchVisuals.intact[smoothingTape];visual.Cover(smoothingHole,Mathf.Lerp(.5f,1,v));
                    visual.Sample(smoothingHole==0?"A03_SmoothGesture_L":"A03_SmoothGesture_R",v);
                    if(v>=1)
                    {
                        var data=Chapter.Tape.Candidates[smoothingTape];if(smoothingHole==0)data.HoleA=true;else data.HoleB=true;
                        Chapter.LooseStripReady=false;smoothingTape=-1;house.SaveStableProgress();
                    }
                }
            }
            if(transport!=null)
            {
                float p=transport.SourceDuration>0?transport.SourceSeconds/transport.SourceDuration:0;
                for(int i=0;i<workbenchVisuals.sourceProxies.Length;i++)
                {
                    var v=workbenchVisuals.sourceProxies[i];v.gameObject.SetActive(i==CurrentCopy);
                    if(i==CurrentCopy){v.Winding(p);v.Sample("A02_Source"+(i+1).ToString("00")+"_Play",Mathf.Repeat(transport.SourceSeconds/2,1));workbenchVisuals.sources[i].Winding(p);}
                }
                var b=workbenchVisuals.targetProxy;
                if(Selected>=0){var data=Chapter.Tape.Candidates[Selected];b.Cover(0,data.HoleA?1:0);b.Cover(1,data.HoleB?1:0);b.Winding(Mathf.Clamp01(transport.TargetSeconds/Mathf.Max(1,transport.TargetCapacitySeconds)));b.Sample("A03_WorkTape_Play",Mathf.Repeat(transport.TargetSeconds/2,1));}
            }
        }
    }
}
