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
        static readonly string[] targetLabels={"● REC [S]","▶ PLAY [F]","REW [J]","FF [L]","STOP [']","EJECT"};
        public static string TargetKeyLabel(int index)=>targetLabels[index];
        ColorBlock[] idleColors;
        static readonly string[] targetTooltipCommands={"Record","Play","Rewind","Forward","Stop","Eject"};
        static readonly string[] sourceTooltipCommands={"Play","Stop","Rewind","Eject"};
        readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> tooltipHits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        public string TooltipAt(Vector2 point)
        {
            var events=UnityEngine.EventSystems.EventSystem.current;
            if(events==null || deck==null || !panel.activeInHierarchy || SuccessOpen)return null;
            tooltipHits.Clear();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events){position=point},tooltipHits);
            if(tooltipHits.Count==0)return null;
            var hit=tooltipHits[0].gameObject.transform;
            for(int i=0;i<buttons.Length && i<targetTooltipCommands.Length;i++)if(hit.IsChildOf(buttons[i].transform))return deck.DeckControlTooltip("B_"+targetTooltipCommands[i]);
            for(int i=0;sourceButtons!=null && i<sourceButtons.Length && i<sourceTooltipCommands.Length;i++)if(hit.IsChildOf(sourceButtons[i].transform))return deck.DeckControlTooltip("A_"+sourceTooltipCommands[i]);
            if(submitAction!=null && hit.IsChildOf(submitAction.transform))return "Submit copy · Check and save this recording";
            if(prepareAction!=null && hit.IsChildOf(prepareAction.transform))return "Workbench · Check reels and cover protection holes";
            return null;
        }
        bool CanOperate=>deck!=null && deck.RecordingInputReady;
        void Awake()
        {
            if(successPopup!=null)successPopup.SetActive(false);
            idleColors=new ColorBlock[buttons.Length];
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].GetComponentInChildren<Text>(true).text=TargetKeyLabel(i);
                idleColors[i]=buttons[i].colors;
                idleColors[i].normalColor=new Color(.2f,.23f,.22f);
            }
        }
        void Update()
        {
            if(panel.activeSelf)panel.SetActive(false);
            if(SuccessOpen)successDismiss.interactable=successGate.Ready && deck.house.InputContext.Allows(InputContextId.Modal);
        }
        public void Hold(int command){if(CanOperate)deck.SetUITransportCommand(command);}
        public void Command(int command){if(CanOperate && command>=4)deck.SetUITransportCommand(command);}
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
