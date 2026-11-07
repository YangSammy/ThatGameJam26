using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class WBMain
    {
        public PhotoWorld3D DarkroomWorld=>worlds[1] as PhotoWorld3D;
        public bool DirectDarkroomActive=>!InStation && DarkroomWorld!=null && DarkroomWorld.DirectDarkroom;
        Transform darkroomReachTarget;
        bool enterReturnedDarkroom;
        void EnterReturnedDarkroom()
        {
            if(!enterReturnedDarkroom)return;
            if(summerPhotoRoom==null || summerPhotoRoom.darkroomView==null || DarkroomWorld==null || !DarkroomWorld.DirectDarkroomAvailable)
                throw new System.InvalidOperationException("Returned film has no available darkroom entry.");
            PrepareContinuousDarkroom();
            Room.Camera.transform.SetPositionAndRotation(summerPhotoRoom.darkroomView.position,summerPhotoRoom.darkroomView.rotation);
            Room.EnableLook(true); // Adopt the new heading before free-pointer control.
            darkroomReachTarget=DarkroomWorld.Nodes["bag"].transform;
            DarkroomWorld.SetDirectDarkroom(true);Room.EnableLook(false);
            enterReturnedDarkroom=false;ParkFilmRoute.EnterDarkroomOnLoad=false;
            SuspendHouseInput();SyncInputContext();
        }
        readonly PointerGestureBuffer photoPointer = new PointerGestureBuffer();
        // Input.mousePosition contains the latest position, not necessarily the
        // position where this frame's press began. Preserve the native event ray.
        void CaptureDarkroomPointerPress()
        {
            var world=DirectDarkroomActive?DarkroomWorld:InStation?CurrentWorld:null;
            var photo=world as PhotoWorld3D;
            if(world==null || !houseFocused || !InputContext.Ready || ShowHelp || transition>0 || awaitRelease ||
                (safeInspection!=null && safeInspection.BlocksInput))
            {photoPointer.Clear();return;}
            var e=Event.current;
            if(e==null || e.button!=0 || (e.type!=EventType.MouseDown && e.type!=EventType.MouseUp))return;
            if(e.type==EventType.MouseDown && contextReturnButton!=null && contextReturnButton.ContainsScreenPoint(new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y)))return;
            if(e.type==EventType.MouseDown && photo!=null && photo.PointerOverUIAt(e.mousePosition))return;
            var ray=world.PointerRay(new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y));
            photoPointer.Capture(e.type==EventType.MouseDown,ray);
        }
        public void PrepareContinuousDarkroom()
        {
            var photo=DarkroomWorld;if(DepartureRoom!=null || photo==null || !photo.DirectDarkroomAvailable)return;
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
                photoPointer.Clear();return true;
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
            // UI eligibility was checked at the native press position. Consume
            // that press even if the pointer has since moved over a UI control.
            Ray pressRay;
            if(photoPointer.TryBegin(out pressRay))
            {photo.Press(photo.Pick(pressRay),pressRay,0);}
            if(!photo.PointerOverUI && Mathf.Abs(Input.mouseScrollDelta.y)>.01f)photo.Scroll(id,Input.mouseScrollDelta.y,ray);
            photo.Move(ray);
            Ray releaseRay;
            if(photoPointer.TryEnd(down,out releaseRay))
            {photo.Move(releaseRay);photo.Release(photo.Pick(releaseRay),releaseRay);}
            photo.TickDirectDarkroom(dt);

            return true;
        }
    }
}
