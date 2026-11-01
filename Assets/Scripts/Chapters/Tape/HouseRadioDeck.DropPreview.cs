using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Tooltip("Authored slot outlines. Shown only for an eligible cassette drop.")]
        public LineRenderer sourceDropPreview,targetDropPreview;
        [Min(.01f)] public float tapeDropTolerance=.035f;
        readonly List<RaycastResult> tapeUIHits=new List<RaycastResult>();
        bool TapeUIAt(Vector2 point)
        {
            if(radioFocused && RadioBackRect.Contains(new Vector2(point.x,Screen.height-point.y)))return true;
            if(preparationFocused && PreparationBackRect.Contains(new Vector2(point.x,Screen.height-point.y)))return true;
            if(PreparationButtonVisible && PreparationButtonRect.Contains(new Vector2(point.x,Screen.height-point.y)))return true;
            if(EventSystem.current==null)return false;
            var pointer=new PointerEventData(EventSystem.current){position=point};
            tapeUIHits.Clear();EventSystem.current.RaycastAll(pointer,tapeUIHits);
            foreach(var hit in tapeUIHits)
                if(hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>()!=null)return true;
            return false;
        }
        bool TapeDropAllowed(Collider slot,Ray ray,Transform item)
        {
            if(slot==null || item==null || Chapter==null)return false;
            bool a=slot==sourceSlot;
            if(slot!=sourceSlot && slot!=slotHit)return false;
            if(a?(Chapter.SourceLoaded || Chapter.Completed || DeckBusy(0)):(State!=Phase.Empty && State!=Phase.Dragging || DeckBusy(1)))return false;
            var point=view.WorldToScreenPoint(ray.GetPoint(1));
            if(TapeUIAt(point))return false;
            return DragReceiverProbe.Accepts(ray,slot.transform,item,tapeDropTolerance,3f,
                c=>c==radioHit || (dualDeck!=null && c.transform.IsChildOf(dualDeck.transform)),slot);
        }
        void ShowTapeDropPreview(Transform receiver)
        {
            if(preparationDropPreview!=null)preparationDropPreview.enabled=receiver!=null && receiver==workMat.transform;
            if(sourceDropPreview!=null)sourceDropPreview.enabled=receiver!=null && receiver==sourceSlot.transform;
            if(targetDropPreview!=null)targetDropPreview.enabled=receiver!=null && receiver==slotHit.transform;
        }
    }
}
