using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        void CaptureWinterPointerPress()
        {
            var painting=CurrentWorld as PaintingWorld3D;
            if(painting==null || !InStation || !houseFocused || ShowHelp || awaitRelease ||
                !InputContext.Ready || (safeInspection!=null && safeInspection.BlocksInput))return;
            painting.CapturePointerPress(Event.current);
        }
    }
}
