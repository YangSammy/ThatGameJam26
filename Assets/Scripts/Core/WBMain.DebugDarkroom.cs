#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        void DebugEnterDarkroom()
        {
            if(!deskStudy || !sharedHouse)return;
            if((InStation && ChapterIndex!=1) || DirectTapeHandling || radioDeck.RecordingMode || radioDeck.WorkbenchMode)
            {cameraRouteNotice="Exit the current interaction, then press 2 for darkroom.";cameraRouteNoticeUntil=Time.unscaledTime+5;return;}
            if(!CanEnterDay(1))return;
            var photo=worlds[1] as PhotoWorld3D;if(photo==null)return;
            if(!photo.DebugPrepareDarkroom(out string message))
            {
                cameraRouteNotice=message;cameraRouteNoticeUntil=Time.unscaledTime+7;
                if(InStation)photo.DarkroomNotice(message);
                SuspendHouseInput();return;
            }
            ShowHelp=false;
            if(Curtain!=null && !Curtain.IsOpen){Curtain.Toggle();Curtain.Advance(1);}
            PrepareContinuousDarkroom();
            photo.DarkroomNotice(message);
            cameraRouteNotice=message;cameraRouteNoticeUntil=Time.unscaledTime+6;
            SuspendHouseInput();
        }
    }
}
#endif
