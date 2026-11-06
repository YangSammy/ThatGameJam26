using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        [Tooltip("Authored slot outlines. Shown only for an eligible cassette drop.")]
        public LineRenderer sourceDropPreview,targetDropPreview;
        readonly HoverOutline dropOutline=new HoverOutline();
        [Min(.01f)] public float tapeDropTolerance=.035f;
        readonly List<RaycastResult> tapeUIHits=new List<RaycastResult>();
        bool TapeUIAt(Vector2 point)
        {
            if(EventSystem.current==null)return false;
            var pointer=new PointerEventData(EventSystem.current){position=point};
            tapeUIHits.Clear();EventSystem.current.RaycastAll(pointer,tapeUIHits);
            foreach(var hit in tapeUIHits)
                if(hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>()!=null)return true;
            return false;
        }
        bool TapeDropAllowed(Collider slot,Ray ray,Transform item)
            => TapeDropGate(slot,ray,item)==null && TapeDropResult(slot,ray,item)==DragReceiverProbe.Result.Ready;
        string TapeDropGate(Collider slot,Ray ray,Transform item)
        {
            if(slot==null || item==null || Chapter==null)return "Slot unavailable";
            bool a=slot==sourceSlot;
            if(slot!=sourceSlot && slot!=slotHit)return "Use the recorder slot";
            if(DeckBusy(a?0:1))return "Wait for the tape and door to settle";
            if(a && Chapter.Completed)return "All copies complete";
            if(a?Chapter.SourceLoaded:(State!=Phase.Empty && State!=Phase.Dragging))return a?"Eject A before loading another tape":"Eject B before loading another tape";
            var command=dualDeck!=null?dualDeck.Command(PickWorkbenchTarget(ray)):null;
            if(command!=null && !command.EndsWith("_Door"))return "Aim at the cassette door, above the buttons";
            var point=view.WorldToScreenPoint(ray.GetPoint(1));
            return TapeUIAt(point)?"Move off the controls and onto the slot":null;
        }
        DragReceiverProbe.Result TapeDropResult(Collider slot,Ray ray,Transform item)
            => DragReceiverProbe.Evaluate(ray,slot.transform,item,tapeDropTolerance,3f,
                c=>c==radioHit || (dualDeck!=null && c.transform.IsChildOf(dualDeck.transform)),slot);
        string TapeDropHint(Collider slot,Ray ray,ObjectDragSession drag)
        {
            string gate=TapeDropGate(slot,ray,drag.Item);if(gate!=null)return gate;
            if(drag.Blocked)return "Path blocked. Move around the obstacle";
            switch(TapeDropResult(slot,ray,drag.Item))
            {
                case DragReceiverProbe.Result.Ready:return slot==sourceSlot?"Release LMB · Load A":"Release LMB · Load B";
                case DragReceiverProbe.Result.TooFar:return "Move closer to the recorder";
                case DragReceiverProbe.Result.Occluded:return "Slot blocked. Find a clear angle";
                case DragReceiverProbe.Result.Unavailable:return "Slot unavailable";
                default:return "Drag to the slot / Release on desk / RMB Cancel";
            }
        }
        void ShowTapeDropPreview(Transform receiver)
        {
            if(preparationDropPreview!=null)preparationDropPreview.enabled=receiver!=null && receiver==workMat.transform;
            // Legacy rectangular guides stay disabled; use the same physical door silhouette as hover.
            if(sourceDropPreview!=null)sourceDropPreview.enabled=false;
            if(targetDropPreview!=null)targetDropPreview.enabled=false;
            int side=receiver!=null && sourceSlot!=null && receiver==sourceSlot.transform?0:
                receiver!=null && slotHit!=null && receiver==slotHit.transform?1:-1;
            var surfaces=side>=0 && dualDeck!=null?dualDeck.DoorOutline(side):
                receiver!=null && workbenchVisuals!=null && workbenchVisuals.finishedHit!=null && receiver==workbenchVisuals.finishedHit.transform?
                dropOutline.Surfaces(receiver):null;
            dropOutline.Show(view,surfaces);
        }
    }
}
