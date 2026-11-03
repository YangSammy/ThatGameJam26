using System;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class PointerGestureRegression
    {
        public static string Run()
        {
            var buffer = new PointerGestureBuffer();
            var start = new Ray(Vector3.zero, Vector3.forward);
            var end = new Ray(Vector3.right, Vector3.down);
            Ray actual;
            buffer.Capture(true, start);
            Require(buffer.TryBegin(out actual) && actual.origin == start.origin, "Press origin");
            Require(!buffer.TryEnd(false, out actual) && buffer.Held, "No synthetic early release between native events");
            buffer.Capture(false, end);
            Require(!buffer.TryEnd(true, out actual), "Wait for legacy button release");
            Require(buffer.TryEnd(false, out actual) && actual.direction == end.direction && actual.origin == end.origin, "Actual release ray");
            Require(!buffer.TryEnd(false, out actual), "Release once");
            buffer.Capture(true, start); buffer.Capture(false, end);
            Require(buffer.TryBegin(out actual) && buffer.TryEnd(false, out actual), "Same-frame click/drag retained");
            buffer.Capture(true, start); buffer.Clear();
            Require(!buffer.TryBegin(out actual) && !buffer.Held, "Cancel clears queued press");
            buffer.Capture(true, start); buffer.TryBegin(out actual); buffer.Clear(); buffer.Capture(false, end);
            Require(!buffer.TryEnd(false, out actual), "Late release after context change ignored");
            return "PASS 8 native pointer edge checks";
        }
        static void Require(bool value, string label) { if (!value) throw new Exception(label); }
    }
}
