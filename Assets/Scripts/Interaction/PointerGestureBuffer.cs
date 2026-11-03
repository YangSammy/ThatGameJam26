using UnityEngine;
namespace MemoryWhitebox
{
    // Native events own gesture edges; polling is only used to wait for Unity's
    // legacy button state to catch up before a release is dispatched.
    public sealed class PointerGestureBuffer
    {
        bool pressPending, releasePending, held;
        Ray pressRay, releaseRay;
        public bool Held => held;
        public void Capture(bool pressed, Ray ray)
        {
            if (pressed)
            {
                if (held || pressPending || releasePending) return;
                pressRay = ray; pressPending = true;
            }
            else if (held || pressPending)
            { releaseRay = ray; releasePending = true; }
        }
        public bool TryBegin(out Ray ray)
        {
            ray = pressRay;
            if (!pressPending) return false;
            pressPending = false; held = true; return true;
        }
        public bool TryEnd(bool buttonStillHeld, out Ray ray)
        {
            ray = releaseRay;
            if (!held || !releasePending || buttonStillHeld) return false;
            held = false; releasePending = false; return true;
        }
        public void Clear() { pressPending = releasePending = held = false; }
    }
}
