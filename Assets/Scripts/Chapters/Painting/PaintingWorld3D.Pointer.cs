using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D
    {
        bool observationPointerReady;
        Vector2 observationPointerLast;
        void ResetObservationPointer()
        {
            observationPointerReady=false;
            edgeArmed=false;
            pointer=new Vector2(.5f,.5f);
            GazeTarget="";
        }
        void MoveObservationPointer(Vector2 now)
        {
            if(State.BookOpen || focusHeld || DoorPerformanceActive ||
                now.x<0 || now.x>1 || now.y<0 || now.y>1)
            {observationPointerReady=false;return;}
            if(observationPointerReady)
            {
                Vector2 delta=now-observationPointerLast;
                // Large discontinuities are cursor re-entry/warps, not head motion.
                if(delta.sqrMagnitude<.09f && now.y>.12f && now.y<.88f)
                {
                    yaw=Mathf.Clamp(yaw+delta.x*110,-65,65);
                    pitch=Mathf.Clamp(pitch-delta.y*120,-40,40);
                }
            }
            observationPointerLast=now;observationPointerReady=true;
        }
        void TickObservationPointer(float dt)
        {
            // Free cursor remains available for edge gestures. Holding a horizontal
            // margin continues turning; LMB freezes the camera for precise aiming.
            if(!observationPointerReady || State.BookOpen || focusHeld ||
                pointer.y<.12f || pointer.y>.88f)return;
            float turn=pointer.x<.08f?-1:pointer.x>.92f?1:0;
            yaw=Mathf.Clamp(yaw+turn*32*Mathf.Max(0,dt),-65,65);
        }
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
