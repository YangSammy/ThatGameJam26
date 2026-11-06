using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Header("Authored recording parts")]
        [Tooltip("按顺序配置三份委托；源音频、噪声插入和验收规则分别编辑。")]
        public TapeCommissionDefinition[] commissions=new TapeCommissionDefinition[3];
        TapeCommissionAudio commissionAudio;
        int loadedCommission=-1;
        public Transform recordingPanel;
        public TapeSampleTransport transport;
        [Tooltip("A play / stop / rewind; B record / play / rewind / fast / stop; rewind-to-start / clear / submit.")]
        public Transform[] keys3d=new Transform[11];
        public Collider[] controlHits=new Collider[11];
        public Transform sourceWheel,targetWheel;
        public WorldTooltipLabel sourceCounter,targetCounter;
        public Renderer recordingLamp;
        public TapeTransportPanel transportPanel;
        MaterialPropertyBlock lampProperties;
        [Min(0)] public float keyTravel=.007f;
        readonly Vector3[] keyHomes=new Vector3[11];
        Quaternion sourceWheelHome,targetWheelHome;
        bool recordingReady;
        public string SubmissionMessage {get;private set;}="";
        public bool SubmissionSucceeded {get;private set;}
        public string RecordingError {get;private set;}="";
        public bool RecordingMode {get;private set;}
        bool wasWriting;
        float retryConfirmUntil;
        readonly PlayerInputReleaseGate recordingGate=new PlayerInputReleaseGate();
        bool recordingWait;
        int mouseKey=-1;
        int handKey=-1;
        public int UITransportCommand {get;private set;}=-1;
        public bool SuccessPopupOpen=>transportPanel!=null && transportPanel.SuccessOpen;
        // Key feedback is independent of the tape motor and recording lamp.
        int pointerFeedbackKey=-1;
        public bool TargetKeyDown(int key)
        {
            if(transport==null || Selected<0)return false;
            return key==0?transport.Writing:key==1?transport.TargetStatus=="PLAYBACK":
                key==2?transport.TargetStatus=="REWINDING":key==3?transport.TargetStatus=="FAST FORWARD":false;
        }
        void ClearKeyFeedback()
        {
            pointerFeedbackKey=-1;
        }
        public bool RecordingInputReady=>RecordingMode && !recordingWait && house!=null && house.InputContext.Allows(InputContextId.TapeTransport);
        public void DismissRecordingSuccess()
        {
            if(Chapter.Completed)OpenRecording();
            else {RestoreRadioCamera();OpenWorkbench();}
            house.SuspendHouseInput();
        }
        public void SetUITransportCommand(int command)
        {
            if(SuccessPopupOpen || !(RecordingMode && !recordingWait || WorkbenchMode && !benchWaiting) || State!=Phase.Paused || Selected<0 || DeckBusy(1))return;
            SubmissionMessage="";lastFeedback="";
            if(command>=0 && command<4 && TargetKeyDown(command)){StopTargetOnly();return;}
            pointerFeedbackKey=command;
            UITransportCommand=-1;
            if(command==4)
            {
                StopTargetOnly();
                if(dualDeck!=null)dualDeck.Press(1,"Stop");
                return;
            }
            if(command==5)
            {
                if(dualDeck!=null){dualDeck.Press(1,"Eject");EjectDualDeck(1);}else {CloseRecording();TakeOut();}house.SaveStableProgress();return;
            }
            if(dualDeck!=null && !dualDeck.Closed(1))return;
            if(command<0 || command>=4 || (Chapter.Completed && command==0))return;
            if(command==0 && !Chapter.AuditionComplete){SubmissionMessage="Listen to this original first.";return;}
            if(command==0 && !Chapter.SourceLoaded){SubmissionMessage="Load the original tape into A first.";return;}
            if(command==0 && dualDeck!=null && !dualDeck.Closed(0)){SubmissionMessage="Close A before recording.";return;}
            if(command==0 && !Chapter.Tape.Candidates[Selected].Prepared)
            {Chapter.ProtectionDiscovered=true;house.SaveStableProgress();SubmissionMessage="Recording protected. Eject B, cover both upper-edge holes, then reload.";return;}
            if(command==0)
            {
                var ownerLock=RecordingOwnerLock(Selected);if(ownerLock!=null){SubmissionMessage=ownerLock;return;}
                if(transport.RecordedSeconds<=0)Chapter.ClearRecordingTapeBinding();
                if(!Chapter.BindRecordingTape(Selected)){SubmissionMessage="This recording belongs to another tape. Retry before changing tapes.";return;}
                if(!house.SaveStableProgress()){SubmissionMessage="Could not save tape identity. Try recording again. "+house.LastSaveError;return;}
            }
            // B keys never alter A's position or motor. The player starts A explicitly.
            UITransportCommand=command;
            // Toggle commands own their motor until a subsequent press or context exit.
            // Changing from recording to playback must not retain the old blank-writing gesture.
            transport.TargetCommand(false,false,0,house.Muted);
            transport.TargetCommand(command==0 || command==1,command==0,command==2?-1:command==3?1:0,house.Muted);
            wasWriting=transport.Writing;
        }
        public void ReleaseTransportKey(int command)
        {
            // Pointer-up releases the finger, not the latched mechanical key.
            if(pointerFeedbackKey==command)pointerFeedbackKey=-1;
        }
        public bool HandPressing=>RecordingMode && !recordingWait && handKey>=0;
        public Transform HandContactTarget=>HandPressing?keys3d[handKey]:
            StationActive && !waitingForRelease && focused && draggingTape && Selected>=0?tapes[Selected].root:null;
        public void InitializeRecording()
        {
            recordingReady=false;RecordingError="";
            try
            {
                if(transport==null || recordingPanel==null || sourceWheel==null || targetWheel==null || keys3d.Length!=11 || controlHits.Length!=11)
                    throw new System.InvalidOperationException("Recording parts must be authored and assigned in the spring workspace.");
                for(int i=0;i<11;i++)
                {
                    if(keys3d[i]==null || controlHits[i]==null)throw new System.InvalidOperationException("Recording key reference missing.");
                    keyHomes[i]=keys3d[i].localPosition;
                }
                sourceWheelHome=sourceWheel.localRotation;targetWheelHome=targetWheel.localRotation;
                int index=Mathf.Min(Chapter.CommissionIndex,RadioTapeChapter.CommissionCount-1);
                if(Chapter.Definition!=null && Chapter.Definition.gameplayConfigurations.Length==RadioTapeChapter.CommissionCount)
                {
                    var configured=new TapeCommissionDefinition[RadioTapeChapter.CommissionCount];
                    for(int i=0;i<configured.Length;i++)
                    {configured[i]=Chapter.Definition.gameplayConfigurations[i] as TapeCommissionDefinition;if(configured[i]==null)throw new System.InvalidOperationException("Invalid tape commission configuration.");}
                    commissions=configured;
                }
                if(commissions==null || commissions.Length<RadioTapeChapter.CommissionCount)throw new System.InvalidOperationException("Assign the original tape commission definition.");
                commissionAudio=TapeRecordingSourceResolver.Resolve(commissions[index],AudioSettings.outputSampleRate,index);
                transport.Initialize(commissionAudio.Samples,AudioSettings.outputSampleRate,index);
                // Legacy saves reused work tape 03 for every commission.
                if(!Chapter.Completed && Chapter.NeedsLegacyRecordingBinding && transport.RecordedSeconds>0 && Chapter.RecordingTapeIndex<0)Chapter.BindLegacyRecordingTape();
                lastAuditionPosition=transport.SourceSeconds;
                loadedCommission=index;
                recordingReady=transport.SaveError=="";
                if(recordingReady)UpdateTransportCounters();
            }
            catch(System.Exception e){RecordingError=e.Message;Debug.LogError("Tape commission unavailable: "+e.Message,this);}
        }
        public bool RecordingHit(Collider hit){foreach(var c in controlHits)if(c!=null && c==hit)return true;return false;}
        public void SynchronizeCommission()
        {if(Chapter!=null && loadedCommission!=Mathf.Min(Chapter.CommissionIndex,RadioTapeChapter.CommissionCount-1))InitializeRecording();}
        public void OpenRecording()
        {
            SynchronizeCommission();
            if(!recordingReady)InitializeRecording();
            if(!recordingReady || transport==null || transport.SaveError!="" || house==null)
            {
                Notice="Recording unavailable";
                Debug.LogWarning("Recording entry blocked: "+(RecordingError!=""?RecordingError:transport!=null?transport.SaveError:"Missing transport"),this);return;
            }
            RecordingMode=true;recordingWait=true;recordingGate.Arm();mouseKey=-1;
            house.Room.StopMotion();PlayerInputPolicy.SetCursor(true);
            house.SyncInputContext();
        }
        public void CloseRecording()
        {
            if(!internalDeckHandoff)focusRadioAfterLoad=false;
            RestoreRadioCamera();ClearKeyFeedback();
            if(!internalDeckHandoff || dualDeck==null){UITransportCommand=-1;transport?.StopAll();dualDeck?.ReleaseStoppedTarget();}
            handKey=-1;
            RecordingMode=false;wasWriting=false;recordingWait=true;
            RestoreRecordingControls();
            house?.SaveStableProgress();house?.SuspendHouseInput();
            house?.SyncInputContext();
        }
        void RestoreRecordingControls()
        {
            if(!recordingReady)return;
            for(int i=0;i<keys3d.Length;i++)if(dualDeck==null && keys3d[i]!=null)keys3d[i].localPosition=keyHomes[i];
            UpdateTransportDisplay();
        }
        void StopRecordingInput()
        {ClearKeyFeedback();if(!internalDeckHandoff || dualDeck==null){UITransportCommand=-1;transport?.StopAll();dualDeck?.ReleaseStoppedTarget();}RestoreRecordingControls();wasWriting=false;mouseKey=-1;handKey=-1;recordingWait=true;recordingGate.Arm();}
        public void RetryCurrentCommission()
        {
            if(SuccessPopupOpen || Chapter.Completed || !recordingReady || transport==null)return;
            lastFeedback="";
            StopRecordingInput();
            bool saved=transport.RetryCurrent();
            Chapter.ClearRecordingTapeBinding();
            SubmissionSucceeded=false;
            SubmissionMessage=saved?"Current commission reset. A and B are at the start; prepared tape retained.":"Reset kept in memory, but save failed. Try again before exiting.";
            UpdateTransportDisplay();house?.SaveStableProgress();
        }
        public bool SaveRecordingProgress()=>transport==null || !recordingReady?RecordingError=="":transport.Save();
        public void RestoreWorkingTape()
        {
            RestoreSource();
            int index=Chapter.WorkingTapeIndex;
            if(State!=Phase.Empty || index<0 || index>=tapes.Length)return;
            if(SelectForDrag(index))InsertDragged();
        }
        public void SubmitCurrentRecording()
        {
            SubmissionSucceeded=false;
            SubmissionMessage="Eject B, then drag the recorded tape into the box's finished compartment.";
            ShowTapePointerNotice(SubmissionMessage);
        }
        void SubmitRecordingFromStorage(int tapeIndex)
        {
            if(SuccessPopupOpen)return;
            lastFeedback="";
            UITransportCommand=-1;
            SubmissionSucceeded=false;
            if(Chapter.Completed){SubmissionSucceeded=true;SubmissionMessage="All 3 copies saved. Esc to return.";return;}
            string gate=FinishedTapeGate(tapeIndex,false);
            if(gate!=null){SubmissionMessage=gate;return;}
            transport.StopAll();wasWriting=false;
            if(!transport.Save()){SubmissionMessage="Could not save. Recording kept in memory; return the tape to the box again.";return;}
            string reason=commissionAudio.Reject(transport.SnapshotMapping());
            if(reason!=""){SubmissionMessage="Not accepted: "+reason;WBMain.Tone(95,.2f);return;}
            int finished=Chapter.CommissionIndex+1;
            StopRecordingInput();
            bool complete;
            string prepared=Chapter.PrepareSubmission(tapeIndex,out complete);
            var result=house.CommitPreparedChapter(ChapterIds.Tape,prepared,complete,transport.Artifact);
            if(!result.Success){SubmissionMessage="Could not save progress. Copy kept; return it to the box again. "+result.Error;return;}
            house.ApplyCommittedChapter(ChapterIds.Tape,prepared);
            ResetDeck();
            adhesiveStrip.gameObject.SetActive(false);adhesiveRibbon.gameObject.SetActive(false);
            if(!Chapter.Completed)
            {
                SynchronizeCommission();lastAuditionPosition=transport.SourceSeconds;
            }
            RestoreSource();
            SubmissionSucceeded=true;
            SubmissionMessage=Chapter.Completed?"All 3 copies saved in the box. Esc to return.":"Copy "+finished+" saved in the box. Load Original "+(Chapter.CommissionIndex+1).ToString("00")+" and choose a NEW work tape.";
            if(!Chapter.Completed)RecordingMode=false;
            transportPanel.ShowSuccess(Chapter.Completed?"All 3 copies saved in the box.":"Copy "+finished.ToString("00")+" saved. Choose a NEW work tape for the next copy.");
            WBMain.Tone(660,.2f);
        }
        void RecordingPointerDown(Vector2 point)
        {
            if(!RecordingInputReady || SuccessPopupOpen || radioCameraMoving || storageDelivery!=null || house.safeInspection.Active || TapeUIAt(point))return;

            // Use the native press location, not the later polled cursor position.
            // A quick desk-to-slot drag can cross the whole recorder in one frame.
            var ray=view.ScreenPointToRay(point);
            var hit=PickWorkbenchTarget(ray);
            if(hit==sourceSlot){EjectDualDeck(0);return;}
            if(hit==slotHit){EjectDualDeck(1);return;}
            if(hit==radioHit){FocusRadio(true);return;}
            if(TryBeginOriginalInteraction(ray) || TryBeginWorkbenchInteraction(ray))return;
            if(dualDeck!=null && PressDualControl(dualDeck.Command(hit)))return;
            for(int i=0;i<controlHits.Length;i++)if(hit==controlHits[i])
            {
                mouseKey=i;
                if(i==0)SourceControl(1);if(i==1)SourceControl(0);if(i==2)SourceControl(2);
                if(i>=3 && i<=7)SetUITransportCommand(i-3);
                if(i==8){UITransportCommand=-1;transport.RewindTarget();}if(i==9 && !Chapter.Completed){UITransportCommand=-1;transport.ClearTarget();}
                if(i==10){SubmitCurrentRecording();return;}
            }
        }
        public void TickRecording()
        {
            if(SuccessPopupOpen || !RecordingMode || transport==null)return;
            house.Room.StopMotion();TickRadioCamera(Time.unscaledDeltaTime);
            TickAudition();
            if(recordingWait){if(recordingGate.Ready)recordingWait=false;return;}
            if(!radioCameraMoving && PollWorkspaceInspection())return;
            if(house.InputContext.ExitPressed(InputContextId.TapeTransport,null,ExitTapeWorkspace))return;
            PollLatchedTransportKeys();
            if(Input.GetKeyDown(KeyCode.R) && !Chapter.Completed && transport.RecordedSeconds>0)
            {
                if(Time.unscaledTime<retryConfirmUntil){retryConfirmUntil=0;RetryCurrentCommission();}
                else {retryConfirmUntil=Time.unscaledTime+3;SubmissionMessage="Press R again to erase this copy and retry.";}
            }
            if(wasWriting && !transport.Writing)transport.Save();
            wasWriting=transport.Writing;
            UpdateTransportDisplay();
        }
        void PollLatchedTransportKeys()
        {
            if(Input.GetKeyDown(KeyCode.Quote))PressTransportKey(KeyCode.Quote);
            else if(Input.GetKeyDown(KeyCode.F))PressTransportKey(KeyCode.F);
            else if(Input.GetKeyDown(KeyCode.J))PressTransportKey(KeyCode.J);
            else if(Input.GetKeyDown(KeyCode.L))PressTransportKey(KeyCode.L);
            if(Input.GetKeyDown(KeyCode.S))PressTransportKey(KeyCode.S);
        }
        public void PressTransportKey(KeyCode key)
        {
            if(key==KeyCode.S)SetUITransportCommand(0);
            else if(key==KeyCode.F)SourceControl(1);
            else if(key==KeyCode.J)SourceControl(2);
            else if(key==KeyCode.L)SourceControl(4);
            else if(key==KeyCode.Quote)SourceControl(0);
        }

        void TickHeldTargetDuringHandoff()
        {
            // Moving another cassette is not a release of a latched transport key.
            // Explicit stop, eject, inspection or leaving the workspace handles stopping.
            if(wasWriting && transport!=null && !transport.Writing){transport.Save();wasWriting=false;}
        }
        void UpdateTransportCounters()
        {
            if(transport==null)return;
            if(sourceCounter!=null)sourceCounter.text="A SOURCE  "+transport.SourceSeconds.ToString("000.0");
            if(targetCounter!=null)targetCounter.text="B WORK  "+transport.TargetSeconds.ToString("000.0");
        }
        void UpdateTransportDisplay()
        {
            if(transport==null)return;
            UpdateTransportCounters();
            if(dualDeck!=null)return;
            sourceWheel.localRotation=sourceWheelHome*Quaternion.Euler(0,transport.SourceSeconds*180,0);
            // Both visible spools in the source window belong to A. B uses its own cassette reels.
            targetWheel.localRotation=targetWheelHome*Quaternion.Euler(0,transport.SourceSeconds*180,0);
            if(originalReels!=null)foreach(var reel in originalReels)if(reel!=null)reel.localRotation=Quaternion.Euler(0,transport.SourceSeconds*180,0);
            if(Selected>=0){var turn=Quaternion.Euler(0,transport.TargetSeconds*180,0);tapes[Selected].leftReel.localRotation=turn;tapes[Selected].rightReel.localRotation=turn;}
            if(recordingLamp!=null){if(lampProperties==null)lampProperties=new MaterialPropertyBlock();recordingLamp.GetPropertyBlock(lampProperties);lampProperties.SetColor("_Color",transport.Writing?Color.red:new Color(.12f,.025f,.02f));recordingLamp.SetPropertyBlock(lampProperties);}
        }
    }
}
