using System;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryWhitebox
{
    // Presentation only. The chapter and its saved checkpoint remain authoritative.
    public sealed class BedroomTransitionOverlay : MonoBehaviour
    {
        Canvas canvas;
        Image shade;
        Text message;
        Button retry;
        Action retryAction;
        string loadingStatus;
        public float Alpha {get;private set;}

        public static BedroomTransitionOverlay Create(Transform owner,Font font)
        {
            var root=new GameObject("Bedroom transition",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            root.transform.SetParent(owner,false);
            var view=root.AddComponent<BedroomTransitionOverlay>();
            view.canvas=root.GetComponent<Canvas>();view.canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            view.canvas.sortingOrder=32767;
            view.shade=new GameObject("Cover",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            view.shade.transform.SetParent(root.transform,false);
            var full=view.shade.rectTransform;full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
            view.message=Label(root.transform,"Message",font,new Vector2(0,30),new Vector2(620,90));
            view.message.text="";
            var button=new GameObject("Retry",typeof(RectTransform),typeof(Image),typeof(Button));
            button.transform.SetParent(root.transform,false);
            var rect=(RectTransform)button.transform;rect.sizeDelta=new Vector2(180,44);rect.anchoredPosition=new Vector2(0,-50);
            button.GetComponent<Image>().color=new Color(.19f,.2f,.2f);
            view.retry=button.GetComponent<Button>();view.retry.onClick.AddListener(()=>view.retryAction?.Invoke());
            Label(button.transform,"Label",font,Vector2.zero,new Vector2(180,44)).text="Retry";
            button.SetActive(false);view.SetAlpha(1);return view;
        }
        static Text Label(Transform parent,string name,Font font,Vector2 position,Vector2 size)
        {
            var label=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent,false);label.rectTransform.anchoredPosition=position;label.rectTransform.sizeDelta=size;
            label.font=font;label.fontSize=18;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.raycastTarget=false;return label;
        }
        public void SetAlpha(float value)
        {Alpha=Mathf.Clamp01(value);shade.color=new Color(0,0,0,Alpha);message.color=new Color(1,1,1,Alpha);canvas.enabled=Alpha>0;}
        public void ClearError(){loadingStatus=null;message.text="";retry.gameObject.SetActive(false);retryAction=null;}
        public void ShowLoading(string status)
        {loadingStatus=status;message.text=status+"…";}
        void Update()
        {
            if(loadingStatus==null || Alpha<=0)return;
            // A small pulse indicates that the loading screen is alive, not fake progress.
            message.color=new Color(.9f,.9f,.9f,Alpha*(.7f+.3f*Mathf.Sin(Time.unscaledTime*2)*Mathf.Sin(Time.unscaledTime*2)));
        }
        public void Fail(Action action)
        {loadingStatus=null;SetAlpha(1);message.text="Could not load the bedroom.\nYour saved progress is kept.";retryAction=action;retry.gameObject.SetActive(true);}
    }
}
