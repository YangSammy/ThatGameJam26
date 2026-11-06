using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        float endingElapsed;
        bool endingMenuLoading;
        string endingMenuError="";
        public float EndingFade=>Mathf.SmoothStep(0,1,Mathf.Clamp01((endingElapsed-.5f)/2));
        public bool EndingCardVisible=>StoryFinished && endingElapsed>=3;

        void TickEndingPresentation(float dt)
        {
            Room.StopMotion();
            endingElapsed+=Mathf.Max(0,dt);
            PlayerInputPolicy.SetCursor(EndingCardVisible);
        }

        void DrawEndingPresentation()
        {
            var old=GUI.color;
            GUI.color=new Color(1,.98f,.91f,EndingFade);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=old;
            if(!EndingCardVisible)return;
            PlayerInputPolicy.SetCursor(true);
            bool enabledBefore=GUI.enabled;var contentBefore=GUI.contentColor;
            GUI.enabled=true;GUI.contentColor=Color.white;GUI.color=Color.white;
            float opacity=Mathf.Clamp01((endingElapsed-3)/.7f);
            var title=new GUIStyle(GUI.skin.label){font=UIFont,fontSize=Mathf.Clamp(Screen.height/16,28,64),alignment=TextAnchor.MiddleCenter};
            title.normal.textColor=new Color(.24f,.29f,.25f,opacity);
            GUI.Label(new Rect(20,Screen.height*.38f,Screen.width-40,100),Application.productName,title);
            var button=new GUIStyle(GUI.skin.button){font=UIFont,fontSize=18};
            var before=GUI.enabled;GUI.enabled=!endingMenuLoading && opacity>=1;
            if(GUI.Button(new Rect(Screen.width*.5f-110,Screen.height*.65f,220,44),"返回主菜单",button))ReturnFromEnding();
            GUI.enabled=before;
            if(!string.IsNullOrEmpty(endingMenuError))
                GUI.Label(new Rect(Screen.width*.5f-230,Screen.height*.65f+55,460,60),endingMenuError);
            GUI.enabled=enabledBefore;GUI.contentColor=contentBefore;GUI.color=old;
        }

        public bool ReturnFromEnding()
        {
            if(!EndingCardVisible || endingMenuLoading || BrushSceneTransition.IsBusy)return false;
            if(!SaveStableProgress()){endingMenuError="存档未完成，请稍后重试。";return false;}
            if(!Application.CanStreamedLevelBeLoaded(MainMenuSaveAccess.ScenePath))
            {endingMenuError="主菜单场景不可用。";return false;}
            try
            {
                endingMenuLoading=true;endingMenuError="";
                BrushSceneTransition.LoadScene(MainMenuSaveAccess.ScenePath);return true;
            }
            catch(System.Exception error)
            {endingMenuLoading=false;endingMenuError="返回失败，请重试。";Debug.LogException(error,this);return false;}
        }
    }
}
