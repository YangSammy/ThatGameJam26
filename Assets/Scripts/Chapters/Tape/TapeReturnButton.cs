using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryWhitebox
{
    // A fixed hover zone keeps the sliding button reachable without hover flicker.
    public sealed class TapeReturnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        System.Func<bool> available,canReturn;
        Canvas canvas;
        RectTransform hoverZone, panel;
        Button button;
        Image handle;
        bool hovered;
        float reveal;

        public static TapeReturnButton Create(HouseRadioDeck owner)
        {
            return Create(owner.transform,
                ()=>owner!=null && owner.isActiveAndEnabled && owner.house!=null && owner.house.InStation && owner.house.ChapterIndex==0 &&
                    owner.house.HouseInputReady && !GamePauseMenu.BlocksGameplay && !owner.SuccessPopupOpen,
                ()=>owner.house.InputContext.Ready && owner.CanReturnFromWorkspace,
                ()=>owner.house.ReturnFromTapeButton());
        }
        public static TapeReturnButton Create(Transform parent,System.Func<bool> available,System.Func<bool> canReturn,System.Action onReturn)
        {
            var root=new GameObject("Tape return navigation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(parent,false);
            var ui=root.AddComponent<TapeReturnButton>();ui.available=available;ui.canReturn=canReturn;
            ui.canvas=root.GetComponent<Canvas>();ui.canvas.renderMode=RenderMode.ScreenSpaceOverlay;ui.canvas.sortingOrder=1000;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            ui.hoverZone=Rect("Hover zone",root.transform,new Vector2(240,76));
            var shield=ui.hoverZone.gameObject.AddComponent<Image>();shield.color=Color.clear;
            var handleRect=Rect("Handle",ui.hoverZone,new Vector2(36,4));handleRect.anchoredPosition=new Vector2(0,8);
            ui.handle=handleRect.gameObject.AddComponent<Image>();ui.handle.color=new Color(.88f,.82f,.68f,.8f);ui.handle.raycastTarget=false;
            ui.panel=Rect("Return button",ui.hoverZone,new Vector2(220,48));ui.panel.anchoredPosition=new Vector2(0,-48);
            var background=ui.panel.gameObject.AddComponent<Image>();background.color=new Color(.16f,.14f,.12f,.96f);
            ui.button=ui.panel.gameObject.AddComponent<Button>();ui.button.targetGraphic=background;
            ui.button.navigation=new Navigation{mode=Navigation.Mode.None};
            var labelRect=Rect("Label",ui.panel,new Vector2(220,48));
            var label=labelRect.gameObject.AddComponent<Text>();label.text="返回上一级";label.alignment=TextAnchor.MiddleCenter;
            label.font=WBMain.UIFont!=null?WBMain.UIFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize=21;label.color=new Color(.96f,.92f,.83f);label.raycastTarget=false;
            ui.button.onClick.AddListener(()=>{if(ui.Available && canReturn())onReturn();});
            ui.canvas.enabled=false;
            return ui;
        }

        static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=size;
            return rect;
        }
        bool Available=>available!=null && available();
        public bool ContainsScreenPoint(Vector2 point)=>Available && RectTransformUtility.RectangleContainsScreenPoint(hoverZone,point,null);
        public void OnPointerEnter(PointerEventData data){hovered=true;}
        public void OnPointerExit(PointerEventData data){hovered=false;}
        void Update()
        {
            canvas.enabled=Available;
            if(!canvas.enabled){hovered=false;reveal=0;panel.anchoredPosition=new Vector2(0,-48);return;}
            reveal=Mathf.MoveTowards(reveal,hovered?1:0,Time.unscaledDeltaTime*7);
            panel.anchoredPosition=new Vector2(0,Mathf.Lerp(-48,12,Mathf.SmoothStep(0,1,reveal)));
            handle.enabled=reveal<.2f;
            button.interactable=hovered && reveal>.9f && canReturn();
        }
    }
}
