using UnityEngine;
using UnityEngine.UI;
namespace MemoryWhitebox
{
    // Authored Canvas controller; no UI objects are created at runtime.
    public sealed class TapeTransportPanel:MonoBehaviour
    {
        public HouseRadioDeck deck;
        public GameObject panel;
        public Text status;
        public Button[] buttons;
        public Text sourceStatus;
        public Button[] sourceButtons;
        public GameObject prepareAction,listenAction,submitAction;
        public GameObject successPopup;
        public Text successText;
        public Button successDismiss;
        public bool SuccessOpen=>successPopup!=null && successPopup.activeSelf;
        readonly PlayerInputReleaseGate successGate=new PlayerInputReleaseGate();
        Text recordLabel,playLabel;
        bool CanOperate=>deck!=null && deck.RecordingInputReady;
        void Awake()
        {
            if(successPopup!=null)successPopup.SetActive(false);
            recordLabel=buttons[0].GetComponentInChildren<Text>(true);
            playLabel=buttons[1].GetComponentInChildren<Text>(true);
            recordLabel.fontSize=12;
            recordLabel.text="RECORD";
            playLabel.text="PLAY COPY";
        }
        void Update()
        {
            if(SuccessOpen){successDismiss.interactable=successGate.Ready && deck.house.InputContext.Allows(InputContextId.Modal);return;}
            bool show=deck!=null && deck.RecordingMode;
            if(panel.activeSelf!=show)panel.SetActive(show);
            if(!show)return;
            bool loaded=deck.Selected>=0 && deck.State==HouseRadioDeck.Phase.Paused;
            bool protectedTape=loaded && !deck.Chapter.Tape.Candidates[deck.Selected].Prepared;
            status.text="B  COPY "+(deck.CurrentCopy+1).ToString("00")+"   ·   "+(loaded?(protectedTape?"PROTECTED":deck.transport.TargetStatus):"EMPTY")+(loaded?"   "+deck.transport.TargetSeconds.ToString("0.0")+"s":"");
            if(sourceStatus!=null)sourceStatus.text="A  ORIGINAL "+(deck.CurrentCopy+1).ToString("00")+" / 03   ·   "+(deck.Chapter.SourceLoaded?deck.transport.SourceStatus+"   "+deck.transport.SourceSeconds.ToString("0.0")+"s":"EMPTY");
            if(prepareAction!=null)prepareAction.SetActive(!deck.Chapter.Completed);
            bool hasCopy=loaded && deck.transport.RecordedSeconds>0;
            recordLabel.text=hasCopy && !deck.transport.Writing?"RESUME REC":"RECORD";
            if(listenAction!=null)listenAction.SetActive(hasCopy);
            if(submitAction!=null)submitAction.SetActive(hasCopy && !deck.Chapter.Completed);
            if(sourceButtons!=null)foreach(var button in sourceButtons)button.interactable=CanOperate && deck.Chapter.SourceLoaded;
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].interactable=CanOperate && loaded && (i!=0 || !deck.Chapter.Completed);
                var colors=buttons[i].colors;colors.normalColor=deck.UITransportCommand==i?new Color(.3f,.46f,.36f):new Color(.2f,.23f,.22f);buttons[i].colors=colors;
            }
        }
        public void Command(int command){if(CanOperate)deck.SetUITransportCommand(command);}
        public void SourceCommand(int command){if(CanOperate)deck.SourceControl(command);}
        public void Prepare(){if(CanOperate)deck.OpenPreparation();}
        public void Listen(){if(CanOperate)deck.ListenToCopy();}
        public void Submit(){if(CanOperate)deck.SubmitCurrentRecording();}
        public void ShowSuccess(string message)
        {
            successText.text=message+"\n\nClick to continue";
            panel.SetActive(false);successPopup.SetActive(true);successPopup.transform.SetAsLastSibling();
            successDismiss.interactable=false;successGate.Arm();PlayerInputPolicy.SetCursor(true);
            deck.house.SyncInputContext();deck.house.InputContext.Handoff();
        }
        public void DismissSuccess()
        {
            if(!SuccessOpen || !successGate.Ready || !deck.house.InputContext.Allows(InputContextId.Modal))return;
            deck.house.InputContext.RequestExit(InputContextId.Modal,null,CloseSuccess);
        }
        void CloseSuccess()
        {
            successPopup.SetActive(false);deck.DismissRecordingSuccess();
        }
    }
}
