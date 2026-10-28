using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
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
            DrawWorkbench();
            if(!RecordingMode || transport==null || house==null || house.safeInspection.Active)return;
            if(SubmissionMessage!=lastFeedback)
            {lastFeedback=SubmissionMessage;feedbackUntil=Time.unscaledTime+8;}
            var text=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            text.normal.textColor=new Color(.94f,.94f,.88f);
            bool canRetry=!Chapter.Completed && transport.RecordedSeconds>0;
            if(canRetry && GUI.Button(RetryButtonRect,"Retry"))RetryCurrentCommission();
            string feedback=Time.unscaledTime<feedbackUntil?SubmissionMessage:"";
            if(transport.Writing && !transport.SourcePlaying)feedback="A is stopped. B is recording silence.";
            else if(transport.TargetStatus=="RUNNING BLANK")feedback="B is running blank. STOP to pause.";
            if(feedback!="")
            {
                text.normal.textColor=SubmissionSucceeded?new Color(.65f,.9f,.7f):new Color(1,.8f,.55f);
                GUI.Label(new Rect(24,TransportPanelTop-42,Screen.width-(canRetry?160:48),40),feedback,text);
            }
        }
    }
}
