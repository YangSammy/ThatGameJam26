using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public bool RecordingPanelVisible=>RecordingMode && (radioCloseView==null || radioFocused) &&
            (house==null || house.safeInspection==null || !house.safeInspection.Active);
        float TransportPanelTop
        {
            get
            {
                if(transportPanel==null)return Screen.height-140;
                var corners=new Vector3[4];transportPanel.panel.GetComponent<RectTransform>().GetWorldCorners(corners);
                return Screen.height-corners[1].y;
            }
        }
        Rect RetryButtonRect=>new Rect(Screen.width-116,TransportPanelTop-39,92,28);
        string lastFeedback="";
        float feedbackUntil;
        void OnGUI()
        {
            if(SuccessPopupOpen)return;
            var pointer=Event.current;
            if(RecordingMode && pointer.type==EventType.MouseDown && pointer.button==0)
                RecordingPointerDown(new Vector2(pointer.mousePosition.x,Screen.height-pointer.mousePosition.y));
            DrawWorkbench();
            if(!RecordingPanelVisible || transport==null || house==null)return;

            var text=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            text.normal.textColor=new Color(.94f,.94f,.88f);
            GUI.Label(new Rect(24,Screen.height-44,Screen.width-48,26),
                "A ORIGINAL  "+(Chapter.SourceLoaded?transport.SourceStatus+"  "+transport.SourceSeconds.ToString("0.0")+"s":"EMPTY")+
                "     |     B COPY  "+(Selected>=0?transport.TargetStatus+"  "+transport.TargetSeconds.ToString("0.0")+"s":"EMPTY"),text);
        }
    }
}
