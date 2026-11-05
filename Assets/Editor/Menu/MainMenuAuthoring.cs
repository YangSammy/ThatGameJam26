using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemoryWhitebox.Editor
{
    public static class MainMenuAuthoring
    {
        static readonly Color Paper=new Color(.87f,.84f,.77f),Ink=new Color(.22f,.25f,.24f),Soft=new Color(.43f,.46f,.42f);
        static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        [MenuItem("Tools/Menu/Create Main Menu Scene")]
        public static string Create()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            if(File.Exists(MainMenuSaveAccess.ScenePath))throw new InvalidOperationException("Main menu already exists. Edit the authored scene instead of overwriting it.");
            var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera=new GameObject("Menu Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Paper;camera.cullingMask=0;
                camera.gameObject.AddComponent<AudioListener>();
                new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                var canvas=new GameObject("MenuCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.matchWidthOrHeight=.5f;
                var frame=new GameObject("Content",typeof(RectTransform));frame.transform.SetParent(canvas.transform,false);
                var rect=frame.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1600,900);
                var controller=canvas.AddComponent<MainMenuController>();
                // Flat paper artwork: no heavy seasonal scene is loaded behind the menu.
                Box(frame.transform,"Background",0,0,1600,900,Paper);
                Box(frame.transform,"Accent",0,0,22,900,Soft);
                Box(frame.transform,"Paper Shadow",968,150,438,590,new Color(.68f,.68f,.61f));
                var sheet=Box(frame.transform,"Paper",944,130,438,590,new Color(.95f,.93f,.87f));sheet.transform.localRotation=Quaternion.Euler(0,0,-5);
                for(int i=0;i<6;i++)Box(sheet.transform,"Ink Line "+i,50,180+i*38,270-i%3*35,2,new Color(.60f,.61f,.55f,.45f));
                Text(frame.transform,"Edition",140,74,620,35,"A STORY IN FIVE DAYS",18,Soft);
                Text(frame.transform,"Title",132,120,730,110,"留在手边的事",64,Ink);
                Text(frame.transform,"Subtitle",140,224,690,45,"Some things stay with us.",23,Soft);
                Text(frame.transform,"Footer",140,835,1200,30,"WASD / Arrow keys · Navigate     Enter · Select     Esc · Back",16,Soft);
                controller.home=Page(frame.transform,"Home");controller.settings=Page(frame.transform,"Settings");
                controller.credits=Page(frame.transform,"Credits");controller.confirmation=Page(frame.transform,"Confirmation");controller.loading=Page(frame.transform,"Loading");
                var h=controller.home.transform;
                controller.continueButton=Button(h,"Continue",140,320,420,"Continue");
                controller.newGameButton=Button(h,"New Game",140,388,420,"New Game");
                controller.settingsButton=Button(h,"Settings",140,456,420,"Settings");
                controller.creditsButton=Button(h,"Credits",140,524,420,"Credits");
                controller.quitButton=Button(h,"Quit",140,592,420,"Quit");
                controller.status=Text(h,"Save Status",140,680,610,80,"",20,Soft);
                var s=controller.settings.transform;
                Box(s,"Settings Backdrop",110,285,680,515,new Color(.92f,.90f,.84f));
                Label(s,"Master volume",330);controller.volume=Slider(s,"Master Volume",415,330,0,1);controller.volumeValue=Text(s,"Volume Value",700,330,70,32,"100%",18,Ink);
                Label(s,"Mouse sensitivity",390);controller.sensitivity=Slider(s,"Mouse Sensitivity",415,390,.25f,3);controller.sensitivityValue=Text(s,"Sensitivity Value",700,390,80,32,"1.00×",18,Ink);
                Label(s,"Graphics quality",450);controller.quality=Dropdown(s,"Graphics Quality",415,450);
                controller.fullscreen=Toggle(s,"Fullscreen",150,520,"Fullscreen");controller.vsync=Toggle(s,"VSync",430,520,"VSync");
                controller.settingsStatus=Text(s,"Settings Status",150,586,600,38,"",18,Soft);
                controller.settingsApply=Button(s,"Apply",150,646,175,"Apply");controller.settingsDefaults=Button(s,"Defaults",345,646,175,"Defaults");controller.settingsBack=Button(s,"Back",540,646,175,"Back");
                var c=controller.credits.transform;
                Box(c,"Credits Backdrop",110,285,680,515,new Color(.92f,.90f,.84f));
                // ScrollRect supports long confirmed contributor lists without clipping.
                var scroll=new GameObject("Credits Scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect));scroll.transform.SetParent(c,false);Place(scroll,150,318,600,350);
                scroll.GetComponent<Image>().color=new Color(1,1,1,0);
                var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(scroll.transform,false);Stretch(viewport.GetComponent<RectTransform>());
                viewport.GetComponent<Mask>().showMaskGraphic=false;
                var body=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));body.transform.SetParent(viewport.transform,false);
                var bodyRect=body.GetComponent<RectTransform>();bodyRect.anchorMin=new Vector2(0,1);bodyRect.anchorMax=new Vector2(1,1);bodyRect.pivot=new Vector2(.5f,1);bodyRect.sizeDelta=Vector2.zero;
                var layout=body.GetComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
                body.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                controller.creditsText=Text(body.transform,"Credits Text",0,0,600,600,controller.creditsCopy,23,Ink);
                controller.creditsText.gameObject.AddComponent<LayoutElement>().minHeight=350;
                var sc=scroll.GetComponent<ScrollRect>();sc.viewport=viewport.GetComponent<RectTransform>();sc.content=bodyRect;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;
                controller.creditsBack=Button(c,"Back",150,700,230,"Back");
                var q=controller.confirmation.transform;
                Box(q,"Confirmation Backdrop",110,285,680,390,new Color(.92f,.90f,.84f));
                controller.confirmText=Text(q,"Message",150,330,600,180,"",25,Ink);
                controller.confirmButton=Button(q,"Confirm",150,560,240,"Confirm");controller.cancelButton=Button(q,"Cancel",420,560,240,"Cancel");
                Box(controller.loading.transform,"Loading Cover",0,0,1600,900,new Color(.06f,.07f,.065f));
                controller.loadingText=Text(controller.loading.transform,"Loading Status",500,420,600,70,"Loading your room…",24,Paper);controller.loadingText.alignment=TextAnchor.MiddleCenter;
                StyleControls(controller);
                foreach(var page in new[]{controller.settings,controller.credits,controller.confirmation,controller.loading})page.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,MainMenuSaveAccess.ScenePath))throw new IOException("Could not save main menu.");
                EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainMenuSaveAccess.ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(x=>x.path!=MainMenuSaveAccess.ScenePath)).ToArray();
                return "Created MainMenuScene with editable Canvas, pages and controls; first in Build Settings.";
            }
            finally {if(active.IsValid())SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        }
        public static string PolishControls()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(MainMenuSaveAccess.ScenePath,OpenSceneMode.Additive);
            try
            {
                var menu=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MainMenuController>(true)).Single();
                StyleControls(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Updated slider, toggle and dropdown contrast.";
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}
        }
        static void StyleControls(MainMenuController menu)
        {
            foreach(var slider in new[]{menu.volume,menu.sensitivity})
            {
                slider.transform.Find("Background").GetComponent<Image>().color=new Color(.69f,.70f,.64f);
                slider.fillRect.GetComponent<Image>().color=Soft;
                slider.handleRect.GetComponent<Image>().color=Ink;
            }
            foreach(var toggle in new[]{menu.fullscreen,menu.vsync})
            {toggle.targetGraphic.color=new Color(.69f,.70f,.64f);toggle.graphic.color=Ink;}
            foreach(var image in menu.quality.GetComponentsInChildren<Image>(true))
                if(image.name=="Arrow" || image.name=="Item Checkmark")image.color=Ink;
        }
        static GameObject Page(Transform parent,string name){var p=new GameObject(name,typeof(RectTransform));p.transform.SetParent(parent,false);Stretch(p.GetComponent<RectTransform>());return p;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(GameObject o,float x,float y,float w,float h){var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color){var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);Place(o,x,y,w,h);var image=o.GetComponent<Image>();image.color=color;image.raycastTarget=false;return o;}
        static Text Text(Transform parent,string name,float x,float y,float w,float h,string content,int size,Color color){var o=new GameObject(name,typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);Place(o,x,y,w,h);var text=o.GetComponent<Text>();text.font=Font;text.fontSize=size;text.text=content;text.color=color;text.raycastTarget=false;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;}
        static Button Button(Transform parent,string name,float x,float y,float width,string label){var o=Box(parent,name,x,y,width,56,new Color(.80f,.81f,.75f));var image=o.GetComponent<Image>();image.raycastTarget=true;var b=o.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(.88f,.95f,.88f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.72f,.65f);colors.disabledColor=new Color(.8f,.8f,.8f,.35f);b.colors=colors;var text=Text(o.transform,"Label",22,8,width-44,40,label,25,Ink);text.alignment=TextAnchor.MiddleLeft;return b;}
        static void Label(Transform parent,string label,float y)=>Text(parent,label+" Label",150,y,260,40,label,22,Ink);
        static Slider Slider(Transform parent,string name,float x,float y,float min,float max){var o=DefaultControls.CreateSlider(new DefaultControls.Resources());o.name=name;o.transform.SetParent(parent,false);Place(o,x,y,260,32);var s=o.GetComponent<Slider>();s.minValue=min;s.maxValue=max;return s;}
        static Toggle Toggle(Transform parent,string name,float x,float y,string label){var o=DefaultControls.CreateToggle(new DefaultControls.Resources());o.name=name;o.transform.SetParent(parent,false);Place(o,x,y,230,36);var text=o.GetComponentInChildren<Text>();text.text=label;text.font=Font;text.fontSize=22;text.color=Ink;return o.GetComponent<Toggle>();}
        static Dropdown Dropdown(Transform parent,string name,float x,float y){var o=DefaultControls.CreateDropdown(new DefaultControls.Resources());o.name=name;o.transform.SetParent(parent,false);Place(o,x,y,300,40);foreach(var text in o.GetComponentsInChildren<Text>(true)){text.font=Font;text.fontSize=18;text.color=Ink;}return o.GetComponent<Dropdown>();}
    }
}
