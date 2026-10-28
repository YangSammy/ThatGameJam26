using UnityEngine;

namespace MemoryWhitebox
{
    // Shared immediate-mode presentation; no gameplay or input ownership.
    public static class InteractionUI
    {
        static Font font;
        public static string ChapterName(int index)
        {return index>=0 && index<6?new[]{"Tape","Camera","Writing","Painting","Scarf","Curtain"}[index]:"Interact";}
        public static GUIStyle Text(bool centered=false)
        {
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},16);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Clamp(Mathf.RoundToInt(Screen.width/85f),12,16),wordWrap=true,alignment=centered?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft};
            style.normal.textColor=new Color(.88f,.88f,.85f);return style;
        }
        public static void Label(Rect rect,string text,GUIStyle unused=null)
        {
            var style=Text();var shade=new GUIStyle(style);shade.normal.textColor=new Color(0,0,0,.8f);
            GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),text,shade);GUI.Label(rect,text,style);
        }
        public static bool Button(Rect rect,string text,GUIStyle unused=null)
        {
            bool hover=rect.Contains(Event.current.mousePosition);var color=GUI.color;
            GUI.color=new Color(0,0,0,hover?.45f:.25f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,hover?.65f:.24f);GUI.DrawTexture(new Rect(rect.x,rect.y,2,rect.height),Texture2D.whiteTexture);GUI.color=color;
            var style=new GUIStyle(GUIStyle.none){font=Text().font,fontSize=Text().fontSize,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,8,0,0)};
            style.normal.textColor=style.hover.textColor=style.active.textColor=new Color(.9f,.9f,.87f);
            return GUI.Button(rect,text,style);
        }
    }
}
