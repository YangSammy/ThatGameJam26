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
        bool stopLatch,recordingLeft,wasWriting;
        readonly PlayerInputReleaseGate recordingGate=new PlayerInputReleaseGate();
        bool recordingWait;
        int mouseKey=-1;
        int handKey=-1;
        public int UITransportCommand {get;private set;}=-1;
        public bool SuccessPopupOpen=>transportPanel!=null && transportPanel.SuccessOpen;
        public bool RecordingInputReady=>RecordingMode && !recordingWait && house!=null && house.InputContext.Allows(InputContextId.TapeTransport);
        public void DismissRecordingSuccess()
        {
            if(Chapter.Completed)OpenRecording();else OpenWorkbench();
            house.SuspendHouseInput();
        }
        public void SetUITransportCommand(int command)
        {
            if(SuccessPopupOpen || !RecordingMode || recordingWait || State!=Phase.Paused || Selected<0)return;
            SubmissionMessage="";lastFeedback="";
            UITransportCommand=-1;
            ApplyRecordingKeys(false,false,false,false,true);
            if(command==5)
            {
                CloseRecording();TakeOut();house.SaveStableProgress();return;
            }
            if(command<0 || command>=4 || (Chapter.Completed && command==0))return;
            if(command==0 && !Chapter.Tape.Candidates[Selected].Intact){SubmissionMessage="Check both reels on the workbench first.";return;}
            if(command==0 && !Chapter.AuditionComplete){SubmissionMessage="Listen to this original first.";return;}
            if(command==0 && !Chapter.SourceLoaded){SubmissionMessage="Load the original tape into A first.";return;}
            if(command==0 && !Chapter.Tape.Candidates[Selected].Prepared)
            {Chapter.ProtectionDiscovered=true;house.SaveStableProgress();SubmissionMessage="Recording protected. Eject B, cover both upper-edge holes, then reload.";return;}
            // First recording starts both decks from their beginning; later REC resumes only B.
            if(command==0 && transport.RecordedSeconds==0){transport.RewindTarget();transport.SourceCommand(2);transport.SourceCommand(1);}
            else if(command==0 && !transport.SourcePlaying)transport.SourceCommand(1);
            ApplyRecordingKeys(false,false,false,false,false);
            UITransportCommand=command;
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
                    throw new System.InvalidOperationException("Recording parts must be authored and assigned in HouseScene.");
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
                commissionAudio=TapeCommissionCompiler.Build(commissions[index],AudioSettings.outputSampleRate);
                transport.Initialize(commissionAudio.Samples,AudioSettings.outputSampleRate,index);
                loadedCommission=index;
                recordingReady=transport.SaveError=="";
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
            RecordingMode=true;recordingWait=true;recordingGate.Arm();recordingLeft=false;mouseKey=-1;stopLatch=true;
            house.Room.StopMotion();PlayerInputPolicy.SetCursor(true);
            house.SyncInputContext();
        }
        public void CloseRecording()
        {
            UITransportCommand=-1;
            handKey=-1;
            transport?.StopAll();RecordingMode=false;wasWriting=false;stopLatch=true;recordingWait=true;
            RestoreRecordingControls();
            house?.SaveStableProgress();house?.SuspendHouseInput();
            house?.SyncInputContext();
        }
        void RestoreRecordingControls()
        {
            if(!recordingReady)return;
            for(int i=0;i<keys3d.Length;i++)if(keys3d[i]!=null)keys3d[i].localPosition=keyHomes[i];
            UpdateTransportDisplay();
        }
        void StopRecordingInput()
        {UITransportCommand=-1;transport?.StopAll();RestoreRecordingControls();wasWriting=false;mouseKey=-1;handKey=-1;stopLatch=true;recordingWait=true;recordingGate.Arm();}
        public void RetryCurrentCommission()
        {
            if(SuccessPopupOpen || Chapter.Completed || !recordingReady || transport==null)return;
            lastFeedback="";
            StopRecordingInput();
            bool saved=transport.RetryCurrent();
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
            if(SuccessPopupOpen)return;
            lastFeedback="";
            UITransportCommand=-1;
            SubmissionSucceeded=false;
            if(Chapter.Completed){SubmissionSucceeded=true;SubmissionMessage="All 3 copies saved. Esc to return.";return;}
            if(!Chapter.SourceLoaded || !Chapter.AuditionComplete){SubmissionMessage="Load and listen to the current original first.";return;}
            if(Selected<0 || !Chapter.Tape.Candidates[Selected].Prepared || State!=Phase.Paused)
            {SubmissionMessage="Insert a checked, sealed tape into B first.";return;}
            transport.StopAll();wasWriting=false;
            if(!transport.Save()){SubmissionMessage="Could not save. Recording kept in memory; try Submit again.";return;}
            string reason=commissionAudio.Reject(transport.SnapshotMapping());
            if(reason!=""){SubmissionMessage="Not accepted: "+reason;WBMain.Tone(95,.2f);return;}
            int finished=Chapter.CommissionIndex+1;
            StopRecordingInput();
            bool complete;
            string prepared=Chapter.PrepareSubmission(out complete);
            var result=house.CommitPreparedChapter(ChapterIds.Tape,prepared,complete,transport.Artifact);
            if(!result.Success){SubmissionMessage="Could not save progress. Copy kept; try Submit again. "+result.Error;return;}
            house.ApplyCommittedChapter(ChapterIds.Tape,prepared);
            if(!Chapter.Completed)
            {
                ResetDeck();
                adhesiveStrip.gameObject.SetActive(false);adhesiveRibbon.gameObject.SetActive(false);
                SynchronizeCommission();lastAuditionPosition=0;
            }
            RestoreSource();
            SubmissionSucceeded=true;
            SubmissionMessage=Chapter.Completed?"All 3 copies saved. Esc to return.":"Copy "+finished+" saved. Load Original "+(Chapter.CommissionIndex+1).ToString("00")+".";
            if(!Chapter.Completed)RecordingMode=false;
            transportPanel.ShowSuccess(Chapter.Completed?"All 3 copies saved.":"Copy "+finished.ToString("00")+" saved.");
            WBMain.Tone(660,.2f);
        }
        public void TickRecording()
        {
            if(SuccessPopupOpen || !RecordingMode || transport==null)return;
            house.Room.StopMotion();
            TickAudition();
            if(recordingWait){if(recordingGate.Ready)recordingWait=false;return;}
            if(house.InputContext.ExitPressed(InputContextId.TapeTransport,null,CloseRecording))return;
            bool left=Input.GetMouseButton(0);
            bool overPanel=UnityEngine.EventSystems.EventSystem.current!=null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if(left && !recordingLeft && !overPanel && !RetryButtonRect.Contains(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)))
            {
                if(TryBeginOriginalInteraction(view.ScreenPointToRay(Input.mousePosition)))return;
                if(TryBeginWorkbenchInteraction(view.ScreenPointToRay(Input.mousePosition)))return;
                var hit=StudyEntry.PickVisible(view.ScreenPointToRay(Input.mousePosition));
                for(int i=0;i<controlHits.Length;i++)if(hit==controlHits[i])
                {
                    mouseKey=i;
                    if(i==0)SourceControl(1);if(i==1)SourceControl(0);if(i==2)SourceControl(2);
                    if(i==8){UITransportCommand=-1;transport.RewindTarget();}if(i==9 && !Chapter.Completed){UITransportCommand=-1;transport.ClearTarget();}
                    if(i==10){SubmitCurrentRecording();return;}
                }
            }
            recordingLeft=left;
            if(!left)mouseKey=-1;
            bool f=Input.GetKey(KeyCode.F)||mouseKey==4,s=Input.GetKey(KeyCode.S)||mouseKey==3,j=Input.GetKey(KeyCode.J)||mouseKey==5,l=Input.GetKey(KeyCode.L)||mouseKey==6,stop=Input.GetKey(KeyCode.Quote)||mouseKey==7;
            if(f || s || j || l || stop)UITransportCommand=-1;
            if(UITransportCommand>=0){f=UITransportCommand==0 || UITransportCommand==1;s=UITransportCommand==0;j=UITransportCommand==2;l=UITransportCommand==3;}
            handKey=mouseKey>=0?mouseKey:stop?7:s?3:f?4:j?5:l?6:-1;
            if(UITransportCommand>=0 || (handKey>=3 && handKey<=7))handKey=-1;
            ApplyRecordingKeys(f,s,j,l,stop);
            if(UITransportCommand==0 && transport.SourceStatus=="END")UITransportCommand=-1;
            if(UITransportCommand==1 && transport.TargetSeconds>=transport.RecordedSeconds)UITransportCommand=-1;
            bool[] pressed={transport.SourcePlaying,false,false,s,f,j,l,stop,false,false,false};
            for(int i=0;i<keys3d.Length;i++)keys3d[i].localPosition=keyHomes[i]+(pressed[i]?Vector3.down*keyTravel:Vector3.zero);
            UpdateTransportDisplay();
        }
        public void ApplyRecordingKeys(bool f,bool s,bool j,bool l,bool stop)
        {
            if(transport==null || SuccessPopupOpen)return;
            if(stop)stopLatch=true;else if(!f)stopLatch=false;
            bool ready=Selected>=0 && State==Phase.Paused;
            bool play=ready && f && !stopLatch && !stop && (!s || !Chapter.Completed);
            bool writing=play && s && !j && !l && Chapter.SourceLoaded && Chapter.AuditionComplete && Chapter.Tape.Candidates[Selected].Prepared && transport.SourceSeconds<transport.SourceDuration;
            if(writing && !wasWriting && !transport.SourcePlaying)transport.SourceCommand(1);
            if(play && s && !writing)
            {
                if(transport.SourceStatus=="END")SubmissionMessage="Original finished. Listen to your copy.";
                else {Chapter.ProtectionDiscovered=true;SubmissionMessage="Load A and cover both write-protect holes on B.";}
            }
            int winding=ready && !stop && !stopLatch && !f && !s && j!=l?(j?-1:1):0;
            transport.TargetCommand(play && !j && !l && (!s || writing),writing,winding,house.Muted);
            if(wasWriting && !writing)transport.Save();wasWriting=writing;
        }
        void UpdateTransportDisplay()
        {
            if(transport==null)return;
            sourceWheel.localRotation=sourceWheelHome*Quaternion.Euler(0,transport.SourceSeconds*180,0);
            // Both visible spools in the source window belong to A. B uses its own cassette reels.
            targetWheel.localRotation=targetWheelHome*Quaternion.Euler(0,transport.SourceSeconds*180,0);
            if(originalReels!=null)foreach(var reel in originalReels)if(reel!=null)reel.localRotation=Quaternion.Euler(0,transport.SourceSeconds*180,0);
            if(Selected>=0){var turn=Quaternion.Euler(0,transport.TargetSeconds*180,0);tapes[Selected].leftReel.localRotation=turn;tapes[Selected].rightReel.localRotation=turn;}
            if(sourceCounter!=null)sourceCounter.text="A SOURCE  "+transport.SourceSeconds.ToString("000.0");
            if(targetCounter!=null)targetCounter.text="B WORK  "+transport.TargetSeconds.ToString("000.0");
            if(recordingLamp!=null){if(lampProperties==null)lampProperties=new MaterialPropertyBlock();recordingLamp.GetPropertyBlock(lampProperties);lampProperties.SetColor("_Color",transport.Writing?Color.red:new Color(.12f,.025f,.02f));recordingLamp.SetPropertyBlock(lampProperties);}
        }
    }
}
