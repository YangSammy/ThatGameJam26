using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        public PhotoWorld3D DarkroomWorld=>worlds[1] as PhotoWorld3D;
        public bool DirectDarkroomActive=>!InStation && DarkroomWorld!=null && DarkroomWorld.DirectDarkroom;
        Transform darkroomReachTarget;
        bool darkroomWasLeft;
        bool darkroomPressPending;
        Ray darkroomPressRay;
        // Input.mousePosition contains the latest position, not necessarily the
        // position where this frame's press began. Preserve the native event ray.
        void CaptureDarkroomPointerPress()
        {
            if(!DirectDarkroomActive || !houseFocused || !InputContext.Ready || ShowHelp ||
                (safeInspection!=null && safeInspection.BlocksInput))
            {darkroomPressPending=false;return;}
            var e=Event.current;
            if(e.type!=EventType.MouseDown || e.button!=0 || darkroomWasLeft || DarkroomWorld.PointerOverUI)return;
            darkroomPressRay=Room.Camera.ScreenPointToRay(new Vector3(e.mousePosition.x,Screen.height-e.mousePosition.y,0));
            darkroomPressPending=true;
        }
        public void PrepareContinuousDarkroom()
        {
            var photo=DarkroomWorld;if(photo==null || !photo.DirectDarkroomAvailable)return;
            if(InStation && ChapterIndex==1)
            {
                photo.CancelInteraction();if(!photo.SaveProgress())return;
                photo.Deactivate();InStation=false;returningToRoom=false;transition=0;awaitRelease=false;ShowHelp=false;
                Room.EnableLook(true);SetStudyPresentation(-1);SuspendHouseInput();
            }
            photo.PresentDirectDarkroom();
            // The procedural camera controls remain usable for rewind/loading; hide only
            // their duplicate desk entry model, not the other study activities.
            if(StudyEntries!=null)foreach(var entry in StudyEntries.GetComponentsInChildren<StudyEntry>(true))
                if(entry.chapter==1)entry.gameObject.SetActive(false);
        }
        bool TickContinuousDarkroom(float dt)
        {
            var photo=DarkroomWorld;
            if(photo==null || !photo.DirectDarkroomAvailable || DirectTapeHandling)return false;
            photo.PresentDirectDarkroom();
            if(!DirectDarkroomActive)
            {
                if(!photo.ReachableDarkroomObject(Room.Camera.ViewportPointToRay(new Vector3(.5f,.5f)),out darkroomReachTarget))return false;
                photo.SetDirectDarkroom(true);Room.EnableLook(false);SyncInputContext();InputContext.Handoff();
                darkroomWasLeft=false;return true;
            }
            if(!photo.HasActiveGesture && !photo.ParkGalleryOpen && (darkroomReachTarget==null || Vector3.Distance(Room.Camera.transform.position,darkroomReachTarget.position)>2.7f))
            {
                photo.SetDirectDarkroom(false);Room.EnableLook(true);tapePointerWasFree=false;SyncInputContext();InputContext.Handoff();return true;
            }
            if(!InputContext.Ready){Room.StopMotion();return true;}
            if(photo.ParkGalleryOpen){Room.StopMotion();photo.TickDirectDarkroom(dt);return true;}
            bool down=Input.GetMouseButton(0),right=Input.GetMouseButton(1);
            var axis=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
            if(photo.HasActiveGesture && !photo.CarryingExposedFilm)Room.StopMotion();
            else Room.StepFreePointer(axis,right?PlayerInputPolicy.LookDelta:Vector2.zero,dt);
            Ray ray=Room.Camera.ScreenPointToRay(Input.mousePosition);string id=photo.Pick(ray);
            if(!photo.PointerOverUI)
            {
                if(darkroomPressPending && !darkroomWasLeft)
                {photo.Press(photo.Pick(darkroomPressRay),darkroomPressRay,0);darkroomWasLeft=true;}
                if(Mathf.Abs(Input.mouseScrollDelta.y)>.01f)photo.Scroll(id,Input.mouseScrollDelta.y,ray);
            }
            darkroomPressPending=false;
            photo.Move(ray);
            if(!down && darkroomWasLeft)photo.Release(id,ray);
            photo.TickDirectDarkroom(dt);
            if(!down)darkroomWasLeft=false;
            return true;
        }
    }
}
