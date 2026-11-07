using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        GUIStyle interactionPromptStyle;

        public void DrawInteractionPrompt()
        {
            if(!Active || !MemoryCamera || !MemoryCamera.enabled || State.Completed)return;
            bool goodbye=State.Phase=="arrival";
            int item=State.PromptItem;
            if(!goodbye && item<0)return;
            Transform target=goodbye?npc:collectionProps[item];
            if(!target)return;
            Vector3 point=MemoryCamera.WorldToViewportPoint(target.position+Vector3.up*(goodbye?1.65f:.45f));
            if(point.z<=0)return;
            Rect pixels=MemoryCamera.pixelRect;
            float scale=Mathf.Clamp(Screen.height/900f,.75f,1.5f);
            float width=Mathf.Min(174*scale,pixels.width-16),height=(goodbye?61:39)*scale;
            if(width<=0 || pixels.height<height+16)return;
            float left=pixels.x+point.x*pixels.width-width*.5f;
            float top=Screen.height-(pixels.y+point.y*pixels.height)-height;
            var panel=new Rect(Mathf.Clamp(left,pixels.x+8,pixels.xMax-width-8),
                Mathf.Clamp(top,Screen.height-pixels.yMax+8,Screen.height-pixels.y-height-8),width,height);
            if(interactionPromptStyle==null)interactionPromptStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,font=WBMain.UIFont};
            interactionPromptStyle.fontSize=Mathf.RoundToInt(18*scale);
            Color previous=GUI.color;
            try
            {
                GUI.color=new Color(.07f,.09f,.09f,.88f);GUI.DrawTexture(panel,Texture2D.whiteTexture);
                GUI.color=Color.white;
                GUI.Label(new Rect(panel.x,panel.y,panel.width,39*scale),goodbye?"[E]  Goodbye":"[E]  Pickup",interactionPromptStyle);
                if(goodbye)
                {
                    var bar=new Rect(panel.x+12*scale,panel.y+42*scale,panel.width-24*scale,9*scale);
                    GUI.color=new Color(.78f,.8f,.73f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
                    var inner=new Rect(bar.x+1,bar.y+1,bar.width-2,bar.height-2);
                    GUI.color=new Color(.16f,.19f,.18f);GUI.DrawTexture(inner,Texture2D.whiteTexture);
                    inner.width*=State.FarewellFill;
                    GUI.color=new Color(.89f,.85f,.65f);GUI.DrawTexture(inner,Texture2D.whiteTexture);
                }
            }
            finally{GUI.color=previous;}
        }
    }
}
