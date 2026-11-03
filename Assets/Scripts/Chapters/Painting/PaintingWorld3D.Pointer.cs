using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        bool winterPressPending;
        Ray winterPressRay;
        // Preserve the native press position: Input.mousePosition can already be at
        // the destination by the next Update, especially during a fast short drag.
        public void CapturePointerPress(Event e)
        {
            if(!Active || !State.BookOpen || !State.AtDoor || !State.Painted[4] || State.KeyInserted ||
                HasPointerGesture || winterPressPending || e==null || e.type!=EventType.MouseDown || e.button!=0)return;
            var ray=PointerRay(new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y));
            if(Pick(ray)!="album-key")return;
            winterPressRay=ray;winterPressPending=true;
        }
        public bool BeginCapturedPointerPress()
        {
            if(!winterPressPending)return false;
            winterPressPending=false;
            if(!Active || HasPointerGesture || !State.BookOpen || !State.AtDoor || State.KeyInserted)return false;
            Press(Pick(winterPressRay),winterPressRay,0);
            return keyDragging;
        }
    }
}
