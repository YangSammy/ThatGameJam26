using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Header("Authored recording parts")]
        public AudioClip commissionClip;
        public Transform recordingPanel;
        public TapeSampleTransport transport;
        [Tooltip("A play / stop / rewind; B record / play / rewind / fast / stop; rewind-to-start / clear / submit.")]
        public Transform[] keys3d=new Transform[11];
        public Collider[] controlHits=new Collider[11];
        public Transform sourceWheel,targetWheel;
        [Min(0)] public float keyTravel=.007f;
        readonly Vector3[] keyHomes=new Vector3[11];
        Quaternion sourceWheelHome,targetWheelHome;
        bool recordingReady;
        public bool RecordingMode {get;private set;}
        bool stopLatch,recordingLeft,wasWriting;
        readonly PlayerInputReleaseGate recordingGate=new PlayerInputReleaseGate();
        bool recordingWait;
        int mouseKey=-1;
        public void InitializeRecording()
        {
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
                transport.Initialize(TapeCommissionSource.Build(commissionClip!=null?commissionClip:tapes[2].audio,AudioSettings.outputSampleRate),AudioSettings.outputSampleRate);
                recordingReady=transport.SaveError=="";
            }
            catch(System.Exception e){Debug.LogError("Tape commission unavailable: "+e.Message,this);}
        }
        public bool RecordingHit(Collider hit){foreach(var c in controlHits)if(c!=null && c==hit)return true;return false;}
        public void OpenRecording()
        {
            if(!recordingReady || transport==null || transport.SaveError!="" || house==null)return;
            RecordingMode=true;recordingWait=true;recordingGate.Arm();recordingLeft=false;mouseKey=-1;stopLatch=true;
            house.Room.StopMotion();PlayerInputPolicy.SetCursor(true);
        }
        public void CloseRecording()
        {
            transport?.StopAll();RecordingMode=false;wasWriting=false;stopLatch=true;recordingWait=true;
            house?.SaveStableProgress();house?.SuspendHouseInput();
        }
        void StopRecordingInput()
        {transport?.StopAll();wasWriting=false;mouseKey=-1;stopLatch=true;recordingWait=true;recordingGate.Arm();}
        public void TickRecording()
        {
            if(!RecordingMode || transport==null)return;
            house.Room.StopMotion();
            if(recordingWait){if(recordingGate.Ready)recordingWait=false;return;}
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)){CloseRecording();return;}
            bool left=Input.GetMouseButton(0);
            if(left && !recordingLeft)
            {
                var hit=StudyEntry.PickVisible(view.ScreenPointToRay(Input.mousePosition));
                for(int i=0;i<controlHits.Length;i++)if(hit==controlHits[i])
                {
                    mouseKey=i;
                    if(i==0)transport.SourceCommand(1);if(i==1)transport.SourceCommand(0);if(i==2)transport.SourceCommand(2);
                    if(i==8)transport.RewindTarget();if(i==9)transport.ClearTarget();
                    if(i==10 && Selected>=0 && Chapter.Tape.Candidates[Selected].Prepared)
                    {
                        transport.StopAll();if(!transport.Save())return;
                        bool accepted=TapeCommissionSource.Accept(transport.SnapshotMapping(),transport.Rate);
                        Chapter.SubmitRecording(accepted);WBMain.Tone(accepted?660:95,.2f);
                        if(accepted){CloseRecording();return;}
                    }
                }
            }
            recordingLeft=left;
            if(!left)mouseKey=-1;
            bool f=Input.GetKey(KeyCode.F)||mouseKey==4,s=Input.GetKey(KeyCode.S)||mouseKey==3,j=Input.GetKey(KeyCode.J)||mouseKey==5,l=Input.GetKey(KeyCode.L)||mouseKey==6,stop=Input.GetKey(KeyCode.Quote)||mouseKey==7;
            if(stop)stopLatch=true;else if(!f)stopLatch=false;
            bool ready=Selected>=0 && Chapter.Tape.Candidates[Selected].Prepared && State==Phase.Paused;
            bool play=ready && f && !stopLatch && !stop;
            bool writing=play && s && !j && !l;
            int winding=ready && !stop && !stopLatch && !f && !s && j!=l?(j?-1:1):0;
            transport.TargetCommand(play && !j && !l,writing,winding,house.Muted);
            if(wasWriting && !writing)transport.Save();wasWriting=writing;
            bool[] pressed={transport.SourcePlaying,false,false,s,f,j,l,stop,false,false,false};
            for(int i=0;i<keys3d.Length;i++)keys3d[i].localPosition=keyHomes[i]+(pressed[i]?Vector3.down*keyTravel:Vector3.zero);
            sourceWheel.localRotation=sourceWheelHome*Quaternion.Euler(0,transport.SourceSeconds*180,0);
            targetWheel.localRotation=targetWheelHome*Quaternion.Euler(0,transport.TargetSeconds*180,0);
            if(Selected>=0){tapes[Selected].leftReel.localRotation=targetWheel.localRotation;tapes[Selected].rightReel.localRotation=targetWheel.localRotation;}
        }
    }
}
