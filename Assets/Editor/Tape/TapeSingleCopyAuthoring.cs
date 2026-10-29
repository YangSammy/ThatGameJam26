using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class TapeSingleCopyAuthoring
    {
        static Transform Anchor(Transform parent,string name,Vector3 position)
        {var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Author single copy");go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.sizeDelta=size;r.anchoredPosition=position;r.gameObject.layer=5;return r;}
        static Text Label(Transform parent,string name,string value,Vector2 size,Vector2 position)
        {var text=Rect(parent,name,size,position).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=16;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;}
        static Button Button(Transform parent,string name,string label,Vector2 position,float width)
        {var r=Rect(parent,name,new Vector2(width,46),position);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.3f,.36f,.33f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;Label(r,"Label",label,new Vector2(width,46),Vector2.zero);return b;}
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var d=Object.FindObjectOfType<HouseRadioDeck>();if(d==null)throw new System.InvalidOperationException("Open HouseScene.");
            Undo.RecordObject(d,"Single copy references");
            if(d.originalTape==null)
            {
                d.originalHome=Anchor(d.transform,"OriginalTapeHome",new Vector3(-.53f,.055f,-.66f));
                d.originalHome.localRotation=Quaternion.Euler(90,0,0);
                d.sourceSocket=Anchor(d.sourceWheel.parent,"SourceTapeSocket",Vector3.zero);
                d.sourceSocket.position=(d.sourceWheel.position+d.targetWheel.position)*.5f+Vector3.up*.02f;
                d.sourceSocket.rotation=d.transform.rotation*Quaternion.Euler(90,-15,0);
                var source=Object.Instantiate(d.tapes[2].root.gameObject,d.transform);source.name="OriginalTape_01";Undo.RegisterCreatedObjectUndo(source,"Author original cassette");
                var prep=source.transform.Find("TapePreparation");if(prep!=null)Object.DestroyImmediate(prep.gameObject);
                var reels=new System.Collections.Generic.List<Transform>();
                foreach(var child in source.GetComponentsInChildren<Transform>(true))if(child.name==d.tapes[2].leftReel.name || child.name==d.tapes[2].rightReel.name)reels.Add(child);
                d.originalReels=reels.ToArray();
                // Keep only the model; original tape is not a candidate for inspection or sealing.
                foreach(var component in source.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(component);
                foreach(var collider in source.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                foreach(var child in source.GetComponentsInChildren<Transform>(true))child.gameObject.layer=25;
                d.originalTape=source.transform;d.originalTape.localScale=Vector3.one*2;
                d.originalTape.position=d.originalHome.position;d.originalTape.rotation=Quaternion.identity;
                var bounds=new Bounds(d.originalTape.position,Vector3.zero);foreach(var renderer in source.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
                var hit=source.AddComponent<BoxCollider>();hit.center=d.originalTape.InverseTransformPoint(bounds.center);var scale=d.originalTape.lossyScale;hit.size=new Vector3(bounds.size.x/scale.x,bounds.size.y/scale.y,bounds.size.z/scale.z);d.originalHit=hit;
                d.originalTape.rotation=d.originalHome.rotation;
                var socketHit=d.sourceSocket.gameObject.AddComponent<BoxCollider>();socketHit.size=new Vector3(.23f,.16f,.07f);d.sourceSocket.gameObject.layer=25;d.sourceSlot=socketHit;
                var tag=Anchor(d.originalTape,"OriginalLabel",new Vector3(0,.023f,-.008f));var t=tag.gameObject.AddComponent<WorldTooltipLabel>();t.text="Original 01";t.target=d.originalTape;
                var note=Anchor(d.adhesiveTab.transform.parent,"WriteProtectGuide",new Vector3(0,.035f,.06f));note.localRotation=Quaternion.Euler(70,0,0);var n=note.gameObject.AddComponent<WorldTooltipLabel>();n.text="Cover both write-protect holes to allow recording. This does not repair broken tape.";n.target=d.adhesiveTab.transform.parent;
            }
            var ui=d.transportPanel;Undo.RecordObject(ui,"Dual transport controls");
            if(ui.sourceStatus==null)
            {
                var panel=ui.panel.GetComponent<RectTransform>();panel.sizeDelta=new Vector2(1180,174);
                ui.status.rectTransform.sizeDelta=new Vector2(660,28);ui.status.rectTransform.anchoredPosition=new Vector2(230,133);
                for(int i=0;i<6;i++){var r=ui.buttons[i].GetComponent<RectTransform>();r.sizeDelta=new Vector2(102,46);r.anchoredPosition=new Vector2(230+(i-2.5f)*110,78);var text=ui.buttons[i].GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(102,46);text.fontSize=15;}
                ui.sourceStatus=Label(panel,"SourceStatus","A ORIGINAL | EMPTY",new Vector2(440,28),new Vector2(-350,133));
                ui.sourceButtons=new Button[4];string[] labels={"PLAY","STOP","REWIND","EJECT"};int[] commands={1,0,2,3};
                for(int i=0;i<4;i++){var b=Button(panel,"Source"+labels[i],labels[i],new Vector2(-350+(i-1.5f)*105,78),98);ui.sourceButtons[i]=b;UnityEventTools.AddIntPersistentListener(b.onClick,ui.SourceCommand,commands[i]);}
                UnityEventTools.AddPersistentListener(Button(panel,"PrepareButton","WORKBENCH",new Vector2(-350,14),180).onClick,ui.Prepare);
                UnityEventTools.AddPersistentListener(Button(panel,"ListenButton","LISTEN TO COPY",new Vector2(90,14),210).onClick,ui.Listen);
                UnityEventTools.AddPersistentListener(Button(panel,"SubmitButton","SUBMIT COPY",new Vector2(370,14),210).onClick,ui.Submit);
            }
            ui.prepareAction=ui.panel.transform.Find("PrepareButton").gameObject;
            ui.listenAction=ui.panel.transform.Find("ListenButton").gameObject;
            ui.submitAction=ui.panel.transform.Find("SubmitButton").gameObject;
            SimplifyPanel(ui);
            EditorUtility.SetDirty(ui);EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);
            return "Authored original tape, source socket, protection note and two transport UI groups.";
        }
        public static void SimplifyPanel(TapeTransportPanel ui)
        {
            var panel=ui.panel.GetComponent<RectTransform>();Undo.RecordObject(panel,"Simplify tape UI");panel.sizeDelta=new Vector2(1180,142);
            ui.panel.GetComponent<Image>().color=new Color(.08f,.1f,.09f,.9f);
            foreach(var title in new[]{ui.status,ui.sourceStatus}){var r=title.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,108);title.fontSize=15;}
            foreach(var b in ui.panel.GetComponentsInChildren<Button>(true))
            {
                var r=b.GetComponent<RectTransform>();bool secondary=b.gameObject==ui.prepareAction || b.gameObject==ui.listenAction || b.gameObject==ui.submitAction;
                r.anchoredPosition=new Vector2(r.anchoredPosition.x,secondary?10:58);r.sizeDelta=new Vector2(r.sizeDelta.x,secondary?32:40);
                var text=b.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(r.sizeDelta.x,r.sizeDelta.y);text.fontSize=14;
                text.color=b==ui.buttons[0]?new Color(1,.65f,.6f):new Color(.87f,.89f,.87f);
                if(text.text=="FAST FWD")text.text="FWD";if(text.text=="REWIND")text.text="REW";
                if(b==ui.buttons[0]){text.text="RECORD";text.fontSize=12;}
                if(b==ui.buttons[1])text.text="PLAY COPY";
                b.targetGraphic.color=Color.white;var colors=b.colors;colors.normalColor=secondary?new Color(.12f,.15f,.14f):new Color(.2f,.23f,.22f);colors.highlightedColor=new Color(.32f,.38f,.34f);colors.pressedColor=new Color(.25f,.43f,.32f);colors.disabledColor=new Color(.13f,.15f,.14f,.4f);b.colors=colors;
            }
        }
    }
}
