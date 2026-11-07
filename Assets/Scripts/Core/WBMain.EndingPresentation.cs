using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        float endingElapsed;
        bool endingMenuLoading;
        float endingMenuRetryAt;
        string endingMenuError="";
        public float EndingFade=>Mathf.SmoothStep(0,1,Mathf.Clamp01((endingElapsed-.5f)/2));
        const float CreditsStart=EndingCreditsUI.StartSeconds;
        EndingCreditsUI endingCredits;
        float CreditsDuration=>endingCredits!=null?endingCredits.Duration:45f;
        public bool EndingCardVisible=>StoryFinished && (endingCredits!=null?endingCredits.Finished:endingElapsed>=CreditsStart+CreditsDuration);

        void TickEndingPresentation(float dt)
        {
            Room.StopMotion();
            endingElapsed+=Mathf.Max(0,dt);
            if(endingCredits==null)endingCredits=Object.FindObjectOfType<EndingCreditsUI>(true);
            if(endingCredits!=null){endingCredits.gameObject.SetActive(true);endingCredits.Present(endingElapsed,EndingFade,UIFont);}
            PlayerInputPolicy.SetCursor(false);
            if(EndingCardVisible && !endingMenuLoading && !BrushSceneTransition.IsBusy && endingElapsed>=endingMenuRetryAt)
            {
                // Retry failures at a bounded rate without asking for a final confirmation click.
                endingMenuRetryAt=endingElapsed+2;
                ReturnFromEnding();
            }
        }

        void DrawEndingPresentation()
        {
            // IMGUI renders after Canvas overlays; it must not hide the transition.
            if(BrushSceneTransition.IsBusy)return;
            var old=GUI.color;
            GUI.color=new Color(1,.98f,.91f,EndingFade);
            if(endingCredits==null)GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=old;
            if(!string.IsNullOrEmpty(endingMenuError))
                GUI.Label(new Rect(Screen.width*.5f-230,Screen.height*.65f+55,460,60),endingMenuError);
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
