using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MemoryWhitebox
{
    public sealed class WorldTooltipPresenter:MonoBehaviour
    {
        public RectTransform bubble;
        public Text label;
        WBMain house;
        ParkPhotoPrototype park;
        Camera fallback;
        PhotoWorld3D guidedPhoto;
        readonly StringBuilder content=new StringBuilder();
        void Start(){house=FindObjectOfType<WBMain>();park=FindObjectOfType<ParkPhotoPrototype>();fallback=Camera.main;bubble.gameObject.SetActive(false);}
        public string TextFor(Collider hit)
        {
            content.Length=0;
            foreach(var item in WorldTooltipLabel.Active)
            {
                if(item==null || !item.isActiveAndEnabled || !item.Matches(hit) || string.IsNullOrWhiteSpace(item.Content))continue;
                if(content.Length>0)content.Append('\n');content.Append(item.Content);
            }
            return content.ToString();
        }
        HouseRadioDeck highlightedDeck;
        readonly HoverOutline outline=new HoverOutline();
        Camera outlineCamera;
        Renderer[] outlineSurfaces;
        RectTransform taskMark;
        bool taskMarkVisible;
        void ShowTaskMark(Vector2 position)
        {
            if(!taskMark)
            {
                var mark=new GameObject("Darkroom next step",typeof(RectTransform),typeof(Image));
                taskMark=mark.GetComponent<RectTransform>();
                taskMark.SetParent(bubble.parent,false);
                taskMark.SetAsFirstSibling();
                taskMark.sizeDelta=new Vector2(10,10);
                taskMark.localRotation=Quaternion.Euler(0,0,45);
                var border=mark.GetComponent<Image>();border.raycastTarget=false;border.color=new Color(.20f,.13f,.06f);
                var fill=new GameObject("Amber",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                fill.raycastTarget=false;fill.color=new Color(1f,.72f,.30f);
                fill.rectTransform.SetParent(taskMark,false);
                fill.rectTransform.sizeDelta=new Vector2(7,7);
            }
            taskMarkVisible=true;
            taskMark.position=position;
            taskMark.gameObject.SetActive(true);
        }
        void LateUpdate()
        {
            var previous=highlightedDeck;highlightedDeck=null;
            outlineCamera=null;outlineSurfaces=null;taskMarkVisible=false;
            Present();
            if(!taskMarkVisible && taskMark && taskMark.gameObject.activeSelf)taskMark.gameObject.SetActive(false);
            outline.Show(outlineCamera,outlineSurfaces,contrast:guidedPhoto==null,
                afterEffects:house!=null && house.CurrentWorld is PaintingWorld3D painting && !painting.State.BookOpen);
            if(previous && previous!=highlightedDeck)previous.ClearTapeHover();
        }
        void Present()
        {
            bubble.gameObject.SetActive(false);
            var previousGuide=guidedPhoto;
            guidedPhoto=house!=null?(house.DirectDarkroomActive?house.DarkroomWorld:house.InStation?house.CurrentWorld as PhotoWorld3D:null):null;
            if(previousGuide!=null && previousGuide!=guidedPhoto)previousGuide.ClearDarkroomHighlight();
            if(guidedPhoto!=null)guidedPhoto.RefreshDarkroomHighlight();
            if(house!=null && house.DirectDarkroomActive && house.DarkroomWorld.ParkGalleryOpen)return;
            if(house!=null && (house.ShowHelp || (house.radioDeck!=null && house.radioDeck.SuccessPopupOpen)))return;
            Vector2 point=Cursor.lockState==CursorLockMode.Locked?new Vector2(Screen.width*.5f,Screen.height*.5f):(Vector2)Input.mousePosition;
            Camera view=house!=null?house.playerCamera:park!=null?park.view:fallback;
            if(view==null)return;
            bool inspection=house!=null && house.safeInspection.Active;
            if(inspection)view=house.safeInspection.displayCamera;
            bool overUI=EventSystem.current!=null && Cursor.lockState!=CursorLockMode.Locked && EventSystem.current.IsPointerOverGameObject();
            var radio=house!=null && !inspection?house.radioDeck:null;
            string radioUI=radio!=null && radio.transportPanel!=null?radio.transportPanel.TooltipAt(point):null;
            bool darkroom=guidedPhoto!=null && guidedPhoto.DarkroomGuidanceActive && !inspection;
            if(darkroom && guidedPhoto.TaskMarkerPosition(view,out var mark))ShowTaskMark(mark);
            if(overUI && radioUI==null)return;
            Collider hit=null;
            if(park!=null && park.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Exploration)
            {point=park.TooltipPoint;hit=park.TooltipTarget;}
            else if(house!=null && !house.InStation && !house.DirectDarkroomActive && !inspection)hit=StudyEntry.PickVisible(view.ScreenPointToRay(point));
            else
            {
                RaycastHit info;
                if(Physics.Raycast(view.ScreenPointToRay(point),out info,inspection?20:60,inspection?view.cullingMask:~0,QueryTriggerInteraction.Collide))hit=info.collider;
            }
            if(radio!=null && (radio.WorkbenchMode || radio.RecordingMode))hit=radio.PickWorkbenchTarget(view.ScreenPointToRay(point));
            if(darkroom && guidedPhoto.PointerOverUI)return;
            var ray=view.ScreenPointToRay(point);
            outlineCamera=view;
            string value=darkroom?guidedPhoto.DarkroomTooltip(ray,out outlineSurfaces):TextFor(hit);
            if(radioUI!=null)value=radioUI;
            else if(radio!=null && radio.dualDeck!=null)
            {
                string hint=radio.WorkbenchObjectTooltip(hit);
                if(hint==null)hint=radio.DeckControlTooltip(radio.dualDeck.Command(hit));
                if(hint!=null)value=hint;
            }
            if(!overUI && !darkroom && radio!=null && (radio.WorkbenchMode || radio.RecordingMode))
            {
                highlightedDeck=radio;
                string drop=radio.PresentTapeHover(hit,point);
                if(drop!=null)value=drop;
            }
            if(!inspection && !darkroom && !overUI && radioUI==null && (radio==null || !(radio.WorkbenchMode || radio.RecordingMode)))
            {
                Transform target=null;
                if(house!=null && house.InputContext.Ready)
                {
                    if(house.InStation && house.CurrentWorld!=null)
                    {
                        var world=house.CurrentWorld;
                        target=world.HoverVisual(point,out outlineCamera);
                    }
                    else target=house.RoomHoverVisual(hit,ray);
                }
                else if(park!=null)target=park.InstrumentHoverVisual(hit);
                outlineSurfaces=outline.Surfaces(target);
            }
            if(string.IsNullOrWhiteSpace(value))return;
            label.text=value;
            float width=Mathf.Min(320,Screen.width-32);float height=Mathf.Clamp(label.preferredHeight+24,42,180);
            bubble.sizeDelta=new Vector2(width,height);
            bubble.position=new Vector3(Mathf.Clamp(point.x+18,8,Screen.width-width-8),Mathf.Clamp(point.y-18,height+8,Screen.height-8),0);
            bubble.gameObject.SetActive(true);
        }
        void OnDestroy(){outline.Dispose();if(taskMark)Destroy(taskMark.gameObject);}
        void OnDisable(){if(taskMark)taskMark.gameObject.SetActive(false);outline.Clear();if(highlightedDeck)highlightedDeck.ClearTapeHover();highlightedDeck=null;if(guidedPhoto!=null)guidedPhoto.ClearDarkroomHighlight();if(bubble!=null)bubble.gameObject.SetActive(false);}
    }
}
