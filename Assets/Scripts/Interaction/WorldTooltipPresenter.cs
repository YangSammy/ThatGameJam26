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
        void LateUpdate()
        {
            bubble.gameObject.SetActive(false);
            if(guidedPhoto!=null)guidedPhoto.ClearDarkroomHighlight();
            guidedPhoto=house!=null?(house.DirectDarkroomActive?house.DarkroomWorld:house.InStation?house.CurrentWorld as PhotoWorld3D:null):null;
            if(guidedPhoto!=null)guidedPhoto.RefreshDarkroomHighlight();
            if(house!=null && house.DirectDarkroomActive && house.DarkroomWorld.ParkGalleryOpen)return;
            if(house!=null && (house.ShowHelp || (house.radioDeck!=null && house.radioDeck.SuccessPopupOpen)))return;
            Vector2 point=Cursor.lockState==CursorLockMode.Locked?new Vector2(Screen.width*.5f,Screen.height*.5f):(Vector2)Input.mousePosition;
            Camera view=house!=null?house.playerCamera:park!=null?park.view:fallback;
            if(view==null)return;
            bool inspection=house!=null && house.safeInspection.Active;
            if(inspection)view=house.safeInspection.displayCamera;
            if(EventSystem.current!=null && Cursor.lockState!=CursorLockMode.Locked && EventSystem.current.IsPointerOverGameObject())return;
            Collider hit=null;
            if(park!=null && park.PhotoMode!=ParkPhotoPrototype.PhotographyMode.Exploration)
            {point=park.TooltipPoint;hit=park.TooltipTarget;}
            else if(house!=null && !house.InStation && !house.DirectDarkroomActive && !inspection)hit=StudyEntry.PickVisible(view.ScreenPointToRay(point));
            else
            {
                RaycastHit info;
                if(Physics.Raycast(view.ScreenPointToRay(point),out info,inspection?20:60,inspection?view.cullingMask:~0,QueryTriggerInteraction.Collide))hit=info.collider;
            }
            bool darkroom=guidedPhoto!=null && guidedPhoto.DarkroomGuidanceActive && !inspection;
            if(darkroom && guidedPhoto.PointerOverUI)return;
            string value=darkroom?guidedPhoto.DarkroomTooltip(view.ScreenPointToRay(point)):TextFor(hit);
            if(string.IsNullOrWhiteSpace(value))return;
            label.text=value;
            float width=Mathf.Min(320,Screen.width-32);float height=Mathf.Clamp(label.preferredHeight+24,42,180);
            bubble.sizeDelta=new Vector2(width,height);
            bubble.position=new Vector3(Mathf.Clamp(point.x+18,8,Screen.width-width-8),Mathf.Clamp(point.y-18,height+8,Screen.height-8),0);
            bubble.gameObject.SetActive(true);
        }
        void OnDisable(){if(guidedPhoto!=null)guidedPhoto.ClearDarkroomHighlight();if(bubble!=null)bubble.gameObject.SetActive(false);}
    }
}
