using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class TapeTransportPanelAuthoring
    {
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.sizeDelta=size;r.anchoredPosition=position;go.layer=5;return r;
        }
        static Text Label(Transform parent,string name,string value,Vector2 size,Vector2 position,int fontSize)
        {
            var t=Rect(parent,name,size,position).gameObject.AddComponent<Text>();t.text=value;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.13f,.15f,.13f);t.raycastTarget=false;return t;
        }
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var d=Object.FindObjectOfType<HouseRadioDeck>();
            if(Object.FindObjectOfType<TapeTransportPanel>()!=null)return "Panel already authored.";
            var root=new GameObject("TapeTransportCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(TapeTransportPanel));
            Undo.RegisterCreatedObjectUndo(root,"Author tape transport UI");root.layer=5;
            var systems=GameObject.Find("Systems");if(systems!=null)root.transform.SetParent(systems.transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=60;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var panel=Rect(root.transform,"TransportPanel",new Vector2(900,128),new Vector2(0,24));
            panel.gameObject.AddComponent<Image>().color=new Color(.12f,.14f,.13f,.94f);
            var controller=root.GetComponent<TapeTransportPanel>();controller.deck=d;controller.panel=panel.gameObject;
            Undo.RecordObject(d,"Assign tape panel");d.transportPanel=controller;EditorUtility.SetDirty(d);
            controller.status=Label(panel,"Status","B  RECORDING TAPE",new Vector2(860,30),new Vector2(0,90),18);controller.status.color=new Color(.9f,.9f,.82f);
            string[] labels={"REC","PLAY","REWIND","FAST FWD","STOP","EJECT"};
            string[] names={"RecordButton","PlayButton","RewindButton","FastForwardButton","StopButton","EjectButton"};
            controller.buttons=new Button[6];
            for(int i=0;i<6;i++)
            {
                var r=Rect(panel,names[i],new Vector2(132,62),new Vector2((i-2.5f)*145,19));
                var image=r.gameObject.AddComponent<Image>();image.color=Color.white;
                var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
                var colors=button.colors;colors.normalColor=new Color(.87f,.85f,.76f);colors.highlightedColor=new Color(.98f,.97f,.85f);colors.pressedColor=new Color(.6f,.72f,.6f);button.colors=colors;
                controller.buttons[i]=button;UnityEventTools.AddIntPersistentListener(button.onClick,controller.Command,i);
                var text=Label(r,"Label",labels[i],new Vector2(128,60),Vector2.zero,20);if(i==0)text.color=new Color(.65f,.08f,.06f);
            }
            if(Object.FindObjectOfType<EventSystem>()==null)
            {var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));Undo.RegisterCreatedObjectUndo(events,"Author UI event system");if(systems!=null)events.transform.SetParent(systems.transform,false);}
            // Retain serialized references and key homes, but remove the five world controls from rendering and picking.
            for(int i=3;i<=7;i++)
            {
                Undo.RecordObject(d.keys3d[i].gameObject,"Replace world controls with UI");d.keys3d[i].gameObject.SetActive(false);
                foreach(var subject in Resources.FindObjectsOfTypeAll<SafeInspectionSubject>())if(subject.source==d.transform)
                    foreach(var pair in subject.poses)if(pair.source==d.keys3d[i]){Undo.RecordObject(pair.display.gameObject,"Hide inspection control");pair.display.gameObject.SetActive(false);}
            }
            EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);
            return root.name+"/TransportPanel: six persistent buttons";
        }
    }
}
