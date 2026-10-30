using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public Transform originalTape, originalHome, sourceSocket;
        public Collider originalHit, sourceSlot;
        public Transform[] originalReels;
        [System.Serializable] public sealed class OriginalCassette
        {
            public Transform tape,home;
            public Collider hit;
            public Transform[] reels;
        }
        public OriginalCassette[] originals;
        public GameObject[] completedCopies;
        public int CurrentCopy=>Mathf.Min(Chapter.CommissionIndex,RadioTapeChapter.CommissionCount-1);
        [Min(1)] public float auditionDuration=10;
        bool movingOriginal;
        float lastAuditionPosition;
        bool SourceHit(Collider hit)
        {
            if(hit==null)return false;
            if(hit==originalHit || hit==sourceSlot)return true;
            if(originals!=null)foreach(var item in originals)if(item.hit==hit)return true;
            return false;
        }
        public string SourceHint(Collider hit)
        {
            if(!SourceHit(hit))return null;
            if(Chapter.Completed)return "All copies complete";
            if(hit==originalHit && StorageClosed(originalTape))return "Open the storage box first";
            if(hit==originalHit && DeckBusy(0))return "Wait for A to finish loading";
            if(hit!=originalHit && hit!=sourceSlot)return "Use Original "+(CurrentCopy+1).ToString("00")+" first";
            return Chapter.SourceLoaded?"LMB  Open A controls":"LMB drag  Original "+(CurrentCopy+1).ToString("00")+" into A";
        }
        // Both exploration and the recorder UI must hand a source drag to the same input owner.
        public bool TryBeginOriginalInteraction(Ray ray)
        {
            if(movingOriginal || benchGesture!=BenchGesture.None || TapeUIAt(view.WorldToScreenPoint(ray.GetPoint(1))))return false;
            var hit=StudyEntry.PickVisible(ray);
            if(StorageClick(hit))return true;
            if(!SourceHit(hit))return false;
            if(hit!=sourceSlot && (hit!=originalHit || Chapter.Completed || DeckBusy(0) || StorageClosed(originalTape)))
            {ShowTapePointerNotice(SourceHint(hit));return true;}
            if(Chapter.SourceLoaded){if(!RecordingMode)OpenRecording();return true;}
            HandoffToWorkbench();
            if(hit==originalHit){benchWaiting=false;house.AdoptPointerGesture();SourcePointer(EventType.MouseDown,0,ray);}
            return true;
        }
        public void RestoreSource()
        {
            if(Chapter==null)return;
            if(originals!=null && originals.Length==RadioTapeChapter.CommissionCount)
            {
                for(int i=0;i<originals.Length;i++)
                {
                    var item=originals[i];item.tape.SetPositionAndRotation(item.home.position,item.home.rotation);OperationArea.RestorePose(item.tape);
                    VisibleCassette(item.tape,true);item.hit.enabled=true; // Future originals still need hover/sequence feedback.
                }
                var current=originals[CurrentCopy];originalTape=current.tape;originalHome=current.home;originalHit=current.hit;originalReels=current.reels;
            }
            if(completedCopies!=null)for(int i=0;i<completedCopies.Length;i++)if(completedCopies[i]!=null)completedCopies[i].SetActive(i<Chapter.CommissionIndex);
            if(originalTape==null || Chapter==null)return;
            var anchor=Chapter.SourceLoaded?sourceSocket:originalHome;
            originalTape.SetPositionAndRotation(anchor.position,anchor.rotation);
            if(!Chapter.SourceLoaded)OperationArea.RestorePose(originalTape);
            if(dualDeck!=null){VisibleCassette(originalTape,!Chapter.SourceLoaded);if(!transferA)dualDeck.Snap(0,Chapter.SourceLoaded);}
            foreach(var tape in tapes)tape.root.gameObject.SetActive(true);
        }
        public bool LoadOriginal()
        {
            if(Chapter.SourceLoaded || Chapter.Completed || DeckBusy(0))return false;
            OperationArea.ClearPlacement(originalTape);Chapter.SourceLoaded=true;
            if(dualDeck!=null){transferA=true;RestoreSource();dualDeck.Transfer(0,true,()=>{transferA=false;house.SaveStableProgress();});}
            else RestoreSource();QueueLoadedRadioFocus();house.SaveStableProgress();return true;
        }
        public void SourceControl(int command)
        {
            if(SuccessPopupOpen || !RecordingMode || recordingWait || !Chapter.SourceLoaded)return;
            if(command==3)
            {
                if(dualDeck!=null){dualDeck.Press(0,"Eject");EjectDualDeck(0);return;}
                UITransportCommand=-1;transport.StopAll();Chapter.SourceLoaded=false;RestoreSource();house.SaveStableProgress();return;
            }
            if(dualDeck!=null && !dualDeck.Closed(0))return;
            if(dualDeck!=null && command!=1)dualDeck.Press(0,command==2?"Rewind":"Stop");
            if(command==2 && !Chapter.AuditionComplete)Chapter.AuditionSeconds=0;
            transport.SourceCommand(command);lastAuditionPosition=transport.SourceSeconds;
        }
        void TickAudition()
        {
            if(!Chapter.SourceLoaded || Chapter.AuditionComplete || house.Muted)return;
            float position=transport.SourceSeconds,delta=position-lastAuditionPosition;
            // Only audible forward playback counts; rewinding cannot satisfy the tutorial.
            if(delta>0 && delta<.5f)Chapter.AuditionSeconds+=delta;
            lastAuditionPosition=position;
            float duration=CurrentCopy==0?auditionDuration:Mathf.Max(1,transport.SourceDuration-.1f);
            if(Chapter.AuditionSeconds>=duration)
            {Chapter.AuditionComplete=true;RestoreSource();house.SaveStableProgress();}
        }
        bool SourcePointer(EventType type,int button,Ray ray)
        {
            if(type==EventType.MouseDown && button==0 && !movingOriginal && benchGesture==BenchGesture.None)
            {
                var hit=StudyEntry.PickVisible(ray);
                if(SourceHit(hit) && hit!=sourceSlot && (hit!=originalHit || Chapter.Completed || DeckBusy(0) || StorageClosed(originalTape)))
                {ShowTapePointerNotice(SourceHint(hit));return true;}
            }
            if(type==EventType.MouseDown && button==0 && !movingOriginal && benchGesture==BenchGesture.None &&
                StudyEntry.PickVisible(ray)==originalHit && !Chapter.SourceLoaded && !Chapter.Completed && !DeckBusy(0) && !StorageClosed(originalTape))
            {movingOriginal=true;benchDrag.BeginScreenCarry(originalTape,ray,view.transform.forward);return true;}
            if(!movingOriginal)return false;
            if(type==EventType.MouseDrag)
            {MoveRadioTape(benchDrag,ray,CanReceive(sourceSlot,ray)?sourceSlot.transform:null);}
            if(type==EventType.MouseUp && button==0)
            {
                bool over=CanReceive(sourceSlot,ray);benchDrag.MoveScreenCarry(ray);ShowTapeDropPreview(null);
                bool insert=!benchDrag.Blocked && over;
                bool placed=!insert && benchDrag.TryPlaceInArea();benchDrag.End(!insert && !placed);movingOriginal=false;originalHit.enabled=true;
                if(insert)LoadOriginal();else RestoreSource();
                if(placed)house.SaveStableProgress();
                if(insert){internalDeckHandoff=true;try{CloseWorkbench();OpenRecording();}finally{internalDeckHandoff=false;}}
            }
            return true;
        }
        public void OpenPreparation()
        {CloseRecording();OpenWorkbench();}
        public void ListenToCopy()
        {
            if(!RecordingMode || recordingWait || Selected<0 || transport.RecordedSeconds<=0)return;
            transport.SourceCommand(0);transport.RewindTarget();SetUITransportCommand(1);
        }
    }
}
