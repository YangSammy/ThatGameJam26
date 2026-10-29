using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class TapeSuccessPopupAuthoring
    {
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play first.");
            var ui=Object.FindObjectOfType<TapeTransportPanel>();
            if(ui.successPopup!=null)return "Success popup already authored.";
            Undo.RecordObject(ui,"Author recording success popup");
            var root=new GameObject("RecordingSuccessPopup",typeof(RectTransform),typeof(Image));Undo.RegisterCreatedObjectUndo(root,"Recording success popup");
            root.transform.SetParent(ui.transform,false);root.layer=5;
            var r=root.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color(0,0,0,.6f);
            var card=new GameObject("ClickToContinue",typeof(RectTransform),typeof(Image),typeof(Button));card.layer=5;card.transform.SetParent(root.transform,false);
            var cr=card.GetComponent<RectTransform>();cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(.5f,.5f);cr.sizeDelta=new Vector2(430,170);
            card.GetComponent<Image>().color=new Color(.13f,.17f,.15f,1);
            var button=card.GetComponent<Button>();button.targetGraphic=card.GetComponent<Image>();var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
            var label=new GameObject("Message",typeof(RectTransform),typeof(Text));label.layer=5;label.transform.SetParent(card.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(24,20);lr.offsetMax=new Vector2(-24,-20);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=22;text.color=new Color(.9f,.95f,.9f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.text="Copy saved.\n\nClick to continue";
            ui.successPopup=root;ui.successText=text;ui.successDismiss=button;
            UnityEventTools.AddPersistentListener(button.onClick,ui.DismissSuccess);root.SetActive(false);
            EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveScene(ui.gameObject.scene);
            return "Authored TapeTransportCanvas/RecordingSuccessPopup; click card to dismiss.";
        }
    }
}
