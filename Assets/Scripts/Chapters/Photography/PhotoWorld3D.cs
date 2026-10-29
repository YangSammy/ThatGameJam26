using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    // Every addressable control below is a collider on a physical prop.  There
    // are deliberately no GUI rectangles, ray-to-panel coordinates or Draw calls.
    public sealed partial class PhotoWorld3D : WorldChapter3D
    {
        public readonly PhotoChapter Photo;
        public bool CameraHeld { get; private set; }
        public bool Rewinding { get; private set; }
        public bool RewoundBackOpen { get; private set; }
        public float RewindTurns { get; private set; }
        public string DraggedId => printDrag??dragged;
        public int NativeActionCount { get; private set; }
        public int NativeCaptureCount { get; private set; }
        public bool NativePhotoReady => referenceTexture != null;
        public CameraSettingsSnapshot LastShot;
        [Serializable] public struct CameraSettingsSnapshot { public Vector3 eye; public float yaw, focal, aperture, shutter; public int iso; }

        Transform cameraBody, cameraBack, lens, focalRing, shutterDial, apertureRing, advanceLever;
        Transform meterFace, clockHand, bagGrip, tankBody, tankLid, printClip;
        GameObject referencePrint, developedPrint, loadedFilm, threadedFilm, looseFilm, bag, leader;
        readonly Dictionary<string, Vector3> homes = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Quaternion> homeRotations = new Dictionary<string, Quaternion>();
        readonly List<GameObject> rolls = new List<GameObject>();
        readonly List<GameObject> chemistry = new List<GameObject>();
        Texture2D referenceTexture, exposedTexture;
        Camera photoCamera;
        RenderTexture photoRT;
        LineRenderer filmStrip, pour;
        WorldTooltipLabel focalNumber, shutterNumber, apertureNumber, isoNumber;
        Transform opticalFinder, handLeft, handRight, armLeft, armRight;
        public override Transform[] PlayerHandTargets => new[]{handLeft,handRight};
        public override Transform[] PlayerArmTargets => new[]{armLeft,armRight};
        public override bool PlayerHandsEngaged => CameraHeld || dragged!=null || printDrag!=null || printHeld || bagHeld || tankHeld || Rewinding;
        WorldTooltipLabel finderNumbers;
        string dragged;
        Vector3 dragOrigin, lastDrag;
        Ray lastRay;
        bool bagHeld, tankHeld, looking;
        Vector2 lastLookViewport;
        Vector3 inspectionEye, inspectionLook;
        float yaw, pitch = 27, clockDuration, liftTime, feedbackTime, agitationTravel;
        Color feedbackColor;
        Vector3 walkPosition = new Vector3(1.0f, 2.35f, -5.8f);
        Vector3 liftFrom, liftTo;
        Quaternion lookRotation;
        readonly Vector3 cameraDock = new Vector3(0, 1.18f, -3.05f);
        readonly Vector3 bagAt = new Vector3(2.0f, 1.04f, -3.25f);
        readonly Vector3 tankAt = new Vector3(2.0f, 1.06f, -2.27f);

        public PhotoWorld3D(WBMain host, PhotoChapter model) : base(host, model, 1)
        {
            Photo = model;
            FindPrintObjects();
            if(SharedRoom)
            {
                walkPosition=new Vector3(0,1.65f,-5);
                bagAt=Processing(bagAt);tankAt=Processing(tankAt);
                bagAt=ProcessingAnchor("film-bag",bagAt);tankAt=ProcessingAnchor("film-tank",tankAt);
                Photo.Target.X=.15f-Origin.x;Photo.Target.Z=-2.6f;Photo.Target.Yaw=37.15f*Mathf.Deg2Rad;
                Photo.FocusSubject=new Vector3(2.65f,.78f,2.61f)-Origin;
                Photo.CompositionLandmarks=new[]{new Vector3(2.65f,1.25f,3.13f)-Origin,new Vector3(1.12f,.75f,1.8f)-Origin,new Vector3(3.68f,2.1f,2.7f)-Origin,new Vector3(2.65f,.78f,2.61f)-Origin};
                if(Host.deskStudy){Photo.Target.X=0;Photo.Target.Z=.2f-Origin.z;Photo.Target.Yaw=0;Photo.FocusSubject=new Vector3(-.8f,.9f,2.6f)-Origin;Photo.CompositionLandmarks=new[]{new Vector3(-2.6f,2,4.25f)-Origin,new Vector3(.9f,2,4.25f)-Origin,new Vector3(-.8f,.8f,2.6f)-Origin,new Vector3(-2.8f,1.3f,.8f)-Origin};}
                Photo.Target.Focus=Photo.FocusDistance(Photo.Target);
            }
            Room(9, 24);
            BuildStillLife();
            BuildBenches();
            BuildCamera();
            BuildBody();
            int processingStart=Root.transform.childCount;BuildProcessing();CaptureProcessingPose();
            if(SharedRoom && Host.deskStudy && !sceneProcessing)
            {
                var supplies=new GameObject("Darkroom supplies").transform;supplies.SetParent(Host.houseShell.transform,false);supplies.position=Origin;
                // Preserve the original local frame so dragging/restoring bottles still uses the same coordinates.
                for(int i=Root.transform.childCount-1;i>=processingStart;i--){var part=Root.transform.GetChild(i);if(part.position.x>1.8f && part.position.z>.7f)part.SetParent(supplies,true);}
            }
            BuildPrints();
            BindPrintObjects();
            Eye = World(walkPosition); Look = World(UsesProgress?cameraDock:new Vector3(1, 1.05f, -3)); Fov = 62;
            yaw = Mathf.Atan2((Look - Eye).x, (Look - Eye).z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin((Look - Eye).normalized.y) * Mathf.Rad2Deg;
            LoadProgress();
            SyncProps();
        }

        public override string Help => parkLoading?LoadingHelp:ParkMode?ParkHelp:"LMB drag  Load / Scroll  Adjust / LMB  Shoot / Hold rewind + Scroll / Develop in tank";

        void BuildBenches()
        {
            if(SharedRoom)return; // Furniture is authored once in HouseScene.
            Part("", new Vector3(.05f, .9f, -3.1f), new Vector3(2.55f, .14f, 1.5f), "#a39a86");
            Part("", new Vector3(2.1f, .9f, -2.8f), new Vector3(1.55f, .14f, 2.25f), "#8d9690");
            foreach (float x in new[] { -1f, 1.05f, 1.6f, 2.65f })
                Part("", new Vector3(x, .43f, x < 1.4f ? -3.1f : -2.8f), new Vector3(.09f, .86f, .8f), "#777c70");
            Part("", new Vector3(-1.2f, 1.4f, -2.42f), new Vector3(.07f, .85f, .07f), "#605e52");
        }

        void BuildStillLife()
        {
            if(SharedRoom)return; // Photograph the real window seating, not a duplicate set.
            // These are the same foreground / subject / background landmarks used
            // by the exposure/composition model, but now really occupy space.
            Part("", new Vector3(-.45f, .82f, 2.8f), new Vector3(2.7f, .11f, .5f), "#998e78");
            Part("", new Vector3(-1.1f, .6f, 2.8f), new Vector3(.08f, 1.2f, .08f), "#55594d");
            Part("", new Vector3(-1.1f, 1.15f, 2.8f), new Vector3(.48f, .04f, .12f), "#55594d");
            Part("", new Vector3(0, .84f, 5), new Vector3(.36f, .68f, .25f), "#6a756d", PrimitiveType.Capsule);
            Part("", new Vector3(0, 1.57f, 5), Vector3.one * .27f, "#b4b6a7", PrimitiveType.Sphere);
            Part("", new Vector3(-.13f, .35f, 5), new Vector3(.13f, .7f, .13f), "#626d65", PrimitiveType.Capsule);
            Part("", new Vector3(.13f, .35f, 5), new Vector3(.13f, .7f, .13f), "#626d65", PrimitiveType.Capsule);
            Part("", new Vector3(0, 1.35f, 4.83f), new Vector3(.41f, .09f, .07f), "#a14f43");
            Part("", new Vector3(.14f, 1.06f, 4.82f), new Vector3(.1f, .55f, .04f), "#a14f43", PrimitiveType.Cube, new Vector3(0, 0, -8));
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? 1.15f : 2.25f;
                Part("", new Vector3(x, 1.6f, 9), new Vector3(.06f, 1.66f, .1f), "#dad6c7");
            }
            Part("", new Vector3(1.7f, 2.4f, 9), new Vector3(1.16f, .06f, .1f), "#dad6c7");
            Part("", new Vector3(1.7f, .8f, 9), new Vector3(1.16f, .06f, .1f), "#dad6c7");
            Part("", new Vector3(1.7f, 1.6f, 9.05f), new Vector3(1.04f, 1.5f, .03f), "#b6cad0");
            Part("", new Vector3(1.7f, 1.6f, 8.94f), new Vector3(.04f, 1.6f, .1f), "#e1ddcf");
            Part("", new Vector3(-2.5f, .45f, 6.3f), new Vector3(1, .9f, .8f), "#b3ab96");
        }

        void BuildCamera()
        {
            cameraBody = Group("Mechanical 35 mm camera", cameraDock);
            Child(cameraBody, "camera-body", Vector3.zero, new Vector3(.78f, .42f, .32f), "#323a37");
            Child(cameraBody, "", new Vector3(0, .27f, 0), new Vector3(.28f, .15f, .27f), "#a8ada3");
            Child(cameraBody, "finder", new Vector3(0, .27f, -.16f), new Vector3(.16f, .105f, .045f), "#4f655f");
            Child(cameraBody, "chamber", new Vector3(-.23f, 0, -.177f), new Vector3(.22f, .32f, .035f), "#181e1c");
            loadedFilm = Child(cameraBody, "", new Vector3(-.23f, 0, -.205f), new Vector3(.16f, .155f, .16f), "#d0bb7a", PrimitiveType.Cylinder);
            Child(cameraBody, "spool", new Vector3(.255f, 0, -.187f), new Vector3(.11f, .155f, .11f), "#929d90", PrimitiveType.Cylinder);
            leader = Child(cameraBody, "leader", new Vector3(-.1f, 0, -.21f), new Vector3(.13f, .115f, .025f), "#756442");
            threadedFilm = Child(cameraBody, "", new Vector3(.025f, 0, -.22f), new Vector3(.48f, .12f, .013f), "#756442");
            cameraBack = new GameObject("Hinged camera back").transform; cameraBack.SetParent(cameraBody, false);
            cameraBack.localPosition = new Vector3(-.395f, 0, -.18f);
            Child(cameraBack, "back", new Vector3(.39f, 0, -.012f), new Vector3(.78f, .43f, .035f), "#747e73");
            lens = Child(cameraBody, "", new Vector3(0, .015f, .265f), new Vector3(.32f, .13f, .32f), "#3e4942", PrimitiveType.Cylinder, new Vector3(90, 0, 0)).transform;
            focalRing = Child(cameraBody, "focal-ring", new Vector3(0, .015f, .305f), new Vector3(.335f, .025f, .335f), "#727a70", PrimitiveType.Cylinder, new Vector3(90, 0, 0)).transform;
            apertureRing = Child(cameraBody, "aperture-ring", new Vector3(0, .015f, .205f), new Vector3(.35f, .024f, .35f), "#b0aa91", PrimitiveType.Cylinder, new Vector3(90, 0, 0)).transform;
            Child(cameraBody, "", new Vector3(0, .015f, .402f), new Vector3(.24f, .01f, .24f), "#4c6663", PrimitiveType.Cylinder, new Vector3(90, 0, 0));
            shutterDial = Child(cameraBody, "shutter-dial", new Vector3(.23f, .24f, .02f), new Vector3(.18f, .025f, .18f), "#adb1a4", PrimitiveType.Cylinder).transform;
            Child(cameraBody, "shutter", new Vector3(.3f, .28f, -.095f), new Vector3(.07f, .025f, .07f), "#a15c49", PrimitiveType.Cylinder);
            Child(cameraBody, "rewind", new Vector3(-.27f, .25f, .015f), new Vector3(.13f, .03f, .13f), "#bdc0ad", PrimitiveType.Cylinder);
            advanceLever = Child(cameraBody, "advance", new Vector3(.4f, .23f, .09f), new Vector3(.29f, .035f, .055f), "#bec0ac", PrimitiveType.Cube, new Vector3(0, -28, 0)).transform;
            for (int i = 0; i < 4; i++)
            {
                int iso = new[] { 100, 200, 400, 800 }[i];
                Vector3 at = new Vector3(-1.01f + i * .25f, 1.075f, -3.48f);
                var roll = Part("film-" + iso, at, new Vector3(.14f, .12f, .14f), i == 0 ? "#b5bba4" : i == 1 ? "#d0bb7a" : "#a69685", PrimitiveType.Cylinder);
                rolls.Add(roll); Remember("film-" + iso);
                var number = Label(iso.ToString(), at + new Vector3(0, .01f, -.073f), .027f, "#252d29"); number.transform.SetParent(roll.transform, true);
            }
            looseFilm = Part("exposed-film", new Vector3(.56f, 1.075f, -3.48f), new Vector3(.14f, .12f, .14f), "#d0bb7a", PrimitiveType.Cylinder);
            Remember("exposed-film");
            filmStrip = Line("Dragged real film leader", new[] { cameraDock, cameraDock }, "#82704b", .075f);
            filmStrip.useWorldSpace = true;
            filmStrip.gameObject.SetActive(false);
            // Dynamic equipment values are presented by the shared hover UI.
            focalNumber = Label("35 mm", new Vector3(-.16f, 1.46f, -3.225f), .026f, "#ece9da");
            shutterNumber = Label("60", new Vector3(.22f, 1.46f, -3.17f), .025f, "#e8e4d5");
            apertureNumber = Label("8", new Vector3(.34f, 1.17f, -3.23f), .026f, "#ece9da");
            isoNumber = Label("100", new Vector3(-.3f, 1.17f, -3.23f), .026f, "#ece9da");
            foreach (WorldTooltipLabel text in new[] { focalNumber, shutterNumber, apertureNumber, isoNumber }) text.transform.SetParent(cameraBody, true);
            focalNumber.target=focalRing;focalNumber.prefix="Focal length: ";
            shutterNumber.target=shutterDial;shutterNumber.prefix="Shutter: 1/";
            apertureNumber.target=apertureRing;apertureNumber.prefix="Aperture: f/";
            isoNumber.target=cameraBody;isoNumber.prefix="ISO: ";
        }

        void BuildProcessing()
        {
            if(SharedRoom && Host.deskStudy)BindSceneProcessing();
            else BuildGeneratedProcessing();
            pour = Line("Liquid stream", new[] { tankAt, tankAt }, "#cadad0", .02f); pour.useWorldSpace = true; pour.gameObject.SetActive(false);
            BuildLightMeter();
        }

        void BuildGeneratedProcessing()
        {
            bool compact=printObjects!=null && printObjects.Placement("film-bag")!=null;
            bag = Part("bag", bagAt, compact?new Vector3(.5f,.16f,.32f):new Vector3(.69f, .16f, .48f), "#222b29");
            Part("", bagAt + new Vector3(compact?-.22f:-.35f, .02f, -.08f), new Vector3(.18f, .15f, .16f), "#434e46", PrimitiveType.Capsule, new Vector3(0, 0, 90));
            bagGrip = Part("", bagAt + new Vector3(compact?.22f:.39f, .025f, -.05f), new Vector3(.16f, .12f, .16f), "#a6ac9b", PrimitiveType.Sphere).transform;
            tankBody = Part("tank", tankAt, new Vector3(.38f, .19f, .38f), "#343f38", PrimitiveType.Cylinder).transform;
            Remember("tank");
            tankLid=Part("tank-lid", tankAt + Vector3.up * .2f, new Vector3(.4f, .025f, .4f), "#1e2a25", PrimitiveType.Cylinder).transform;
            tankLid.SetParent(tankBody,true);
            for (int i = 0; i < 3; i++)
            {
                Vector3 at = ProcessingAnchor("film-chemical-"+i,Processing(new Vector3(2.68f, 1.16f, -3.53f + i * .43f)));
                var bottle = Part("chemical-" + i, at, new Vector3(.2f, .2f, .2f), i == 0 ? "#c2b88c" : i == 1 ? "#b5bdad" : "#8ca6a7", PrimitiveType.Cylinder);
                chemistry.Add(bottle); Remember("chemical-" + i);
                var number = Label((i + 1).ToString(), at + new Vector3(0, 0, -.108f), .053f, "#27352e"); number.transform.SetParent(bottle.transform, true);
                var cap=Part("", at + Vector3.up * .24f, new Vector3(.12f, .04f, .12f), "#646e61", PrimitiveType.Cylinder);cap.transform.SetParent(bottle.transform,true);
            }
            Vector3 tap=ProcessingAnchor("film-tap",Processing(new Vector3(2.32f,1.47f,-1.85f)));
            Vector3 sink=ProcessingAnchor("film-sink",Processing(new Vector3(2.14f,.99f,-1.81f)));
            Vector3 clock=ProcessingAnchor("film-clock",Processing(new Vector3(2,1.69f,-2.15f)));
            Part("",tap+new Vector3(.13f,-.23f,0), new Vector3(.065f,.48f,.065f),"#aab8b1");
            Part("water",tap,new Vector3(.28f,.065f,.065f),"#bac8bf");
            Part("",sink,compact?new Vector3(.4f,.055f,.28f):new Vector3(.75f,.055f,.42f),"#6c7e79");
            Part("",clock,new Vector3(.31f,.025f,.31f),"#dad7c6",PrimitiveType.Cylinder,new Vector3(90,0,0));
            clockHand=Part("",clock+new Vector3(0,0,-.023f),new Vector3(.012f,.115f,.009f),"#56604f").transform;
            if(SharedRoom)Part("",clock+Vector3.down*.44f,new Vector3(.035f,.57f,.035f),"#56604f");
        }

        void BuildLightMeter()
        {
            meterFace = Group("Physical light meter", new Vector3(-1, 1.14f, -2.93f));
            Child(meterFace, "meter", Vector3.zero, new Vector3(.3f, .35f, .13f), "#657362");
            Child(meterFace, "", new Vector3(0, .1f, -.075f), new Vector3(.24f, .11f, .025f), "#d4d4b5");
            var meterText = Label("EV 13\n100  8  125", new Vector3(-1, 1.23f, -3.02f), .024f, "#29372b");
            meterText.transform.SetParent(meterFace, true);
        }

        void BuildBody()
        {
            handLeft = Part("", Vector3.zero, new Vector3(.125f, .07f, .17f), "#c4ae90", PrimitiveType.Sphere).transform;
            handRight = Part("", Vector3.zero, new Vector3(.125f, .07f, .17f), "#c4ae90", PrimitiveType.Sphere).transform;
            armLeft = Part("", Vector3.zero, new Vector3(.13f, .4f, .13f), "#7b8175", PrimitiveType.Capsule).transform;
            armRight = Part("", Vector3.zero, new Vector3(.13f, .4f, .13f), "#7b8175", PrimitiveType.Capsule).transform;
            opticalFinder = Group("Optical eyecup with engraved settings", Vector3.zero);
            Child(opticalFinder, "", new Vector3(-.205f, 0, 0), new Vector3(.02f, .25f, .03f), "#242e29");
            Child(opticalFinder, "", new Vector3(.205f, 0, 0), new Vector3(.02f, .25f, .03f), "#242e29");
            Child(opticalFinder, "", new Vector3(0, .12f, 0), new Vector3(.43f, .016f, .03f), "#242e29");
            Child(opticalFinder, "", new Vector3(0, -.12f, 0), new Vector3(.43f, .025f, .03f), "#242e29");
            finderNumbers = Label("", new Vector3(0, -.117f, -.019f), .012f, "#c5cc9b");
            finderNumbers.transform.SetParent(opticalFinder, false);
            var readoutHit=opticalFinder.gameObject.AddComponent<BoxCollider>();
            readoutHit.center=new Vector3(0,-.12f,0);readoutHit.size=new Vector3(.43f,.025f,.03f);readoutHit.isTrigger=true;
            finderNumbers.target=opticalFinder;
            finderNumbers.transform.localPosition = new Vector3(0, -.117f, -.019f);
        }

        Vector3 Processing(Vector3 p){return SharedRoom?p+(Host.deskStudy?new Vector3(.7f,0,5.7f):new Vector3(-5.1f,0,5.5f))-Origin:p;}

        void BuildPrints()
        {
            int first=Root.transform.childCount;
            if(SharedRoom)foreach(float x in new[]{-.72f,.56f})Part("",new Vector3(x,1.07f,-2.43f),new Vector3(.055f,.25f,.055f),"#605e52");
            Part("", new Vector3(-.72f, 1.4f, -2.43f), new Vector3(.84f, .51f, .055f), "#b5ab92");
            referencePrint = Part("reference-photo", new Vector3(-.72f, 1.4f, -2.466f), new Vector3(.75f, .422f, .012f), "#e7e1cf");
            Part("", new Vector3(.56f, 1.4f, -2.43f), new Vector3(.84f, .51f, .055f), "#b5ab92");
            developedPrint = Part("developed-photo", new Vector3(.56f, 1.4f, -2.466f), new Vector3(.75f, .422f, .012f), "#d6d4bd");
            printClip = Part("retry", new Vector3(.56f, 1.68f, -2.46f), new Vector3(.13f, .09f, .06f), "#68796c").transform;
            developedPrint.SetActive(false);
            if(UsesProgress)
            {
                // The park roll uses authored darkroom paper and its gallery. Keep legacy
                // references for old paths, but never show the old desk display in HouseScene.
                var legacy=Group("Legacy photo display (inactive)",Vector3.zero);
                for(int i=Root.transform.childCount-2;i>=first;i--)Root.transform.GetChild(i).SetParent(legacy,true);
                legacy.gameObject.SetActive(false);
            }
        }

        public override void Activate()
        {
            base.Activate();
            if(ParkMode){if(resumedDarkroom)DarkroomNotice("Progress restored. Continue the highlighted step.");resumedDarkroom=true;}
            if(UsesProgress && (InDarkroomPhase || ParkMode) && Host.Curtain!=null){if(!Host.Curtain.IsOpen)Host.Curtain.Toggle();Host.Curtain.Advance(1);}
            if (!ParkMode && !parkLoading && !parkFilmUnavailable && referenceTexture == null) RenderReference();
            SyncProps();
        }

        public override void Deactivate()
        {
            ClearDarkroomHighlight();
            parkGallery=false;
            if(Host!=null && Host.sharedHouse)
            {
                CancelInteraction();ReturnToCheckpoint();Photo.MeterVisible=false;SaveProgress();
                Eye=World(walkPosition);Look=Eye+Quaternion.Euler(pitch,yaw,0)*Vector3.forward;Fov=62;SyncProps();
            }
            else Release("", lastRay);
            looking = false; Photo.InViewfinder = false; Photo.MeterVisible = false;
            View.ResetAspect();
            base.Deactivate();
        }
        public bool HasActiveGesture=>dragged!=null || printDrag!=null || Rewinding || bagHeld || tankHeld || printHeld;
        public void CancelInteraction()
        {
            CancelPrintExposure();
            CancelPhysicalPrint();
            looking=Rewinding=bagHeld=tankHeld=false;
            if(dragged!=null && Nodes.ContainsKey(dragged) && homes.ContainsKey(dragged))
            {Nodes[dragged].transform.localPosition=homes[dragged];Nodes[dragged].transform.localRotation=homeRotations[dragged];SetColliders(Nodes[dragged],true);}
            dragged=null;filmStrip.gameObject.SetActive(false);pour.gameObject.SetActive(false);SyncProps();
        }

        public override void Tick(float dt, WBInput input)
        {
            SaveProgress(false);
            if(parkFilmUnavailable)return;
            if(parkGallery)
            {
                if(Input.GetKeyDown(KeyCode.LeftArrow))TurnParkFrame(-1);
                if(Input.GetKeyDown(KeyCode.RightArrow))TurnParkFrame(1);
                return;
            }
            var previousPhase=Photo.Phase;
            if (Photo.Phase == PhotoChapter.FilmPhase.Processing)
                AdvancePhotoChemistry(dt);
            if(ParkMode && darkroomWorkflow>=2 && previousPhase==PhotoChapter.FilmPhase.Processing && Photo.Phase==PhotoChapter.FilmPhase.Developed)BeginPrintWorkflow();
            TickPrintWorkflow(dt);
            if(ManualTank && !Printing && previousPhase==PhotoChapter.FilmPhase.Processing && Photo.Phase==PhotoChapter.FilmPhase.Tank)needsDrain=true;
            if(ParkMode && Photo.Phase!=previousPhase)SaveProgress();
            if (feedbackTime > 0) feedbackTime -= dt;
            if (liftTime > 0)
            {
                liftTime = Mathf.Max(0, liftTime - dt);
                float t = Mathf.SmoothStep(0, 1, 1 - liftTime);
                walkPosition = Vector3.Lerp(liftFrom, liftTo, t);
            }
            else if (!Photo.InViewfinder && !Photo.MeterVisible && printDrag==null && !printHeld && (dragged == null || (SharedRoom && dragged == "exposed-film")) && !bagHeld && !tankHeld && !Rewinding)
            {
                Vector2 axis = new Vector2((input.Keys.Contains(KeyCode.D) ? 1 : 0) - (input.Keys.Contains(KeyCode.A) ? 1 : 0), (input.Keys.Contains(KeyCode.W) ? 1 : 0) - (input.Keys.Contains(KeyCode.S) ? 1 : 0));
                axis = Vector2.ClampMagnitude(axis, 1);
                Vector3 step = Quaternion.Euler(0, yaw, 0) * new Vector3(axis.x, 0, axis.y) * dt * 1.5f;
                Vector3 next = walkPosition + step;
                next.x = Mathf.Clamp(next.x, SharedRoom?-3.45f-Origin.x:-3.7f, SharedRoom?3.45f-Origin.x:3.7f); next.z = Mathf.Clamp(next.z, SharedRoom?-4.05f-Origin.z:-6, SharedRoom?4.05f-Origin.z:1.5f);
                Vector3 nextX = new Vector3(next.x, walkPosition.y, walkPosition.z);
                if (!InsideTable(nextX)) walkPosition = nextX;
                Vector3 nextZ = new Vector3(walkPosition.x, walkPosition.y, next.z);
                if (!InsideTable(nextZ)) walkPosition = nextZ;
            }
            Photo.Settings.X = walkPosition.x; Photo.Settings.Z = walkPosition.z; Photo.Settings.Yaw = yaw * Mathf.Deg2Rad; Photo.Settings.Focus = Photo.FocusDistance(Photo.Settings);
            lookRotation = Quaternion.Euler(Photo.InViewfinder ? 0 : pitch, yaw, 0);
            Eye = Photo.MeterVisible ? inspectionEye : World(walkPosition); Look = Photo.MeterVisible ? inspectionLook : Eye + lookRotation * Vector3.forward * 5;
            UpdateFinderViewport();
            Fov = Photo.InViewfinder ? 2 * Mathf.Atan(20f / (2 * Photo.Settings.Focal)) * Mathf.Rad2Deg : 62;
            if (Photo.MeterVisible) Fov = 52;
            if (CameraHeld)
            {
                cameraBody.position = Eye + lookRotation * new Vector3(.28f, Photo.MeterVisible ? -.9f : Photo.InViewfinder ? -.49f : -.27f, .62f);
                cameraBody.rotation = lookRotation * Quaternion.Euler(-7, 0, -4);
                cameraBody.localScale = Vector3.one * .65f;
            }
            else
            {
                cameraBody.localPosition = cameraDock; cameraBody.localRotation = Quaternion.identity; cameraBody.localScale = Vector3.one;
            }
            if (tankHeld && Photo.Phase == PhotoChapter.FilmPhase.Processing) tankBody.localRotation = homeRotations["tank"] * Quaternion.Euler(Vector3.Scale(sceneProcessing?printObjects.tankAgitationEuler:new Vector3(9,0,8),new Vector3(Mathf.Sin(Time.unscaledTime*14),Mathf.Sin(Time.unscaledTime*12),Mathf.Cos(Time.unscaledTime*11))));
            else if(dragged!="tank")tankBody.localRotation = homeRotations["tank"];
            UpdateBody();
            if (Photo.Phase == PhotoChapter.FilmPhase.Processing && clockDuration > 0) clockHand.localRotation = clockHomeRotation * Quaternion.Euler(0, 0, -(1 - Photo.ProcessingTime / clockDuration) * 360);
            if (Photo.Phase == PhotoChapter.FilmPhase.Processing && Photo.Chemistry == 3)
            {
                pour.gameObject.SetActive(true); pour.SetPosition(0, Nodes["water"].transform.position+new Vector3(-.13f,-.02f,0)); pour.SetPosition(1, tankLid.position);
            }
            else if (dragged == null) pour.gameObject.SetActive(false);
            SyncProps();
        }

        void UpdateBody()
        {
            bool left = CameraHeld || bagHeld || Rewinding;
            bool right = left || dragged != null || printDrag!=null || printHeld || tankHeld;
            handLeft.gameObject.SetActive(left); armLeft.gameObject.SetActive(left);
            handRight.gameObject.SetActive(right); armRight.gameObject.SetActive(right);
            Vector3 leftAt = Eye + lookRotation * new Vector3(-.05f, Photo.InViewfinder ? -.5f : -.33f, .64f);
            Vector3 rightAt = Eye + lookRotation * new Vector3(.48f, Photo.InViewfinder ? -.44f : -.22f, .54f);
            if (bagHeld) { leftAt = bag.transform.position + new Vector3(-.22f,.12f,-.1f); rightAt = bagGrip.position; }
            if (Rewinding) { leftAt = cameraBody.TransformPoint(new Vector3(-.3f, -.1f, -.2f)); rightAt = Nodes["rewind"].transform.position + Vector3.up * .04f; }
            if (dragged == "leader") rightAt = filmStrip.GetPosition(1) + Vector3.up * .03f;
            else if (dragged != null && Nodes.ContainsKey(dragged)) rightAt = Nodes[dragged].transform.position + new Vector3(.06f, .055f, 0);
            if (tankHeld) rightAt = tankBody.position + new Vector3(.17f, .1f, 0);
            if(printDrag!=null)rightAt=Nodes[printDrag].transform.position+Vector3.up*.03f;
            else if(printHeld && printObjects!=null)rightAt=Nodes["print-exposure"].transform.position;
            handLeft.position = leftAt; handRight.position = rightAt;
            handLeft.rotation = handRight.rotation = lookRotation;
            PoseArm(armLeft, Eye + lookRotation * new Vector3(-.28f, -.55f, -.04f), leftAt);
            PoseArm(armRight, Eye + lookRotation * new Vector3(.34f, -.55f, -.04f), rightAt);
            opticalFinder.gameObject.SetActive(Photo.InViewfinder);
            if (Photo.InViewfinder)
            {
                opticalFinder.position = Eye + lookRotation * Vector3.forward * .65f; opticalFinder.rotation = lookRotation;
                float width = 2 * .65f * Mathf.Tan(Fov * .5f * Mathf.Deg2Rad) * (View != null ? View.aspect : 1.684f);
                opticalFinder.localScale = Vector3.one * Mathf.Clamp(width / .43f * .9f, .45f, 2.5f);
                string[] values = { Photo.Settings.Focal.ToString("0") + "mm", "1/" + (1 / Photo.Settings.Shutter).ToString("0"), "f/" + Photo.Settings.Aperture.ToString("0.0") };
                finderNumbers.text = (Photo.WheelMode == 0 ? "[" + values[0] + "]" : values[0]) + "    " + (Photo.WheelMode == 1 ? "[" + values[1] + "]" : values[1]) + "    " + (Photo.WheelMode == 2 ? "[" + values[2] + "]" : values[2]);
            }
        }

        static void PoseArm(Transform arm, Vector3 from, Vector3 to)
        {
            arm.position = (from + to) * .5f; arm.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            arm.localScale = new Vector3(.115f, Vector3.Distance(from, to) * .5f, .115f);
        }

        public override void Press(string id, Ray ray, int button)
        {
            if(parkGallery || parkFilmUnavailable)return;
            id = id ?? "";
            if(id=="tank-lid" && (!ManualTank || tankSealed))id="tank";
            lastRay = ray;
            if(printDrag!=null || printHeld)return;
            if (button == 1) { if (!Photo.InViewfinder && !HasActiveGesture) { looking = true; lastLookViewport = View.WorldToViewportPoint(ray.GetPoint(2)); } return; }
            if (button != 0) return;
            looking=false;
            if(PressPrintWorkflow(id,ray))return;
            if(parkLoading && !id.StartsWith("film-") && id!="leader" && id!="back" && id!="advance")return;
            if(ParkMode)
            {
                if(id=="print-paper" && ParkPrintsReady){OpenParkGallery();return;}
                if(id=="developed-photo"){OpenParkGallery();return;}
                if(id!="bag" && id!="tank" && id!="tank-lid" && id!="water" && id!="rewind" && id!="back" && id!="exposed-film" && !id.StartsWith("chemical-"))return;
                if(ManualTank)
                {
                    // The camera stays light-tight until all exposed film is rewound.
                    if(id=="back" && Photo.Phase==PhotoChapter.FilmPhase.Exposed){WBMain.Tone(100,.06f);return;}
                    if((id=="tank-lid" || id=="tank") && Photo.Phase==PhotoChapter.FilmPhase.Tank && !tankSealed)
                    {tankSealed=true;SyncProps();SaveProgress();WBMain.Tone(220,.06f);return;}
                    if(id=="tank" && needsDrain){BeginDrag(id,ray);return;}
                    if((id.StartsWith("chemical-") || id=="water") && (!tankSealed || needsDrain)){DarkroomNotice(!tankSealed?"Seal the tank lid first.":"Drain the tank into the basin first.");return;}
                    if(id.StartsWith("chemical-") && !CanPourChemical(id)){DarkroomNotice(Photo.Phase==PhotoChapter.FilmPhase.Processing?"Processing — please wait.":"Use the highlighted chemical bottle.");return;}
                }
            }
            NativeActionCount++;
            if (Photo.InViewfinder) { ExposeNative(); return; }
            if (Photo.MeterVisible) { Photo.MeterVisible = false; return; }
            if (Photo.Completed && id != "reference-photo" && id != "developed-photo" && id != "meter") return;
            if (id == "meter") { Photo.MeterVisible = true; inspectionEye = World(new Vector3(-1, 1.38f, -3.72f)); inspectionLook = World(new Vector3(-1, 1.23f, -2.98f)); return; }
            if (id.StartsWith("film-") && Photo.Phase == PhotoChapter.FilmPhase.Empty) { BeginDrag(id, ray); return; }
            if (id == "leader" && Photo.Phase == PhotoChapter.FilmPhase.LoadedOpen && !Photo.FilmThreaded) { dragged = "leader"; filmStrip.gameObject.SetActive(true); SetFilmLine(Point(ray, 1.18f)); return; }
            if (id == "back")
            {
                if (Photo.Phase == PhotoChapter.FilmPhase.LoadedOpen && Photo.FilmThreaded) Photo.Phase = PhotoChapter.FilmPhase.Closed;
                else if (Photo.Phase == PhotoChapter.FilmPhase.Exposed) { Photo.Fogged = true; Photo.Phase = PhotoChapter.FilmPhase.Rewound; RewoundBackOpen = true; ReturnToBench(); }
                else if (Photo.Phase == PhotoChapter.FilmPhase.Rewound) RewoundBackOpen = true;
            }
            else if (id == "advance" && Photo.Phase == PhotoChapter.FilmPhase.Closed) { Photo.Phase = PhotoChapter.FilmPhase.Ready; WBMain.Tone(160, .07f); }
            else if (id == "finder" && Photo.Phase == PhotoChapter.FilmPhase.Ready)
            {
                if (!CameraHeld) { CameraHeld = true; liftFrom = walkPosition; liftTo = SharedRoom?new Vector3(1.2f,1.5f,-4.8f):new Vector3(-.7f, 1.5f, -1.8f); liftTime = 1; yaw = SharedRoom?30:0; pitch = 0; }
                else { Photo.InViewfinder = true; Photo.WheelMode = 0; }
            }
            else if (id == "shutter" && Photo.Phase == PhotoChapter.FilmPhase.Ready) ExposeNative();
            else if (id == "rewind" && Photo.Phase == PhotoChapter.FilmPhase.Exposed) { Rewinding = true; }
            else if (id == "exposed-film" && Photo.Phase == PhotoChapter.FilmPhase.Rewound) BeginDrag(id, ray);
            else if (id == "bag" && Photo.Phase == PhotoChapter.FilmPhase.DarkBag) bagHeld = true;
            else if (id == "tank" && Photo.Phase == PhotoChapter.FilmPhase.Processing) { tankHeld = true; agitationTravel = 0; lastDrag = Point(ray, 1.2f); }
            else if (id.StartsWith("chemical-") && Photo.Phase == PhotoChapter.FilmPhase.Tank) BeginDrag(id, ray);
            else if (id == "water" && Photo.Phase == PhotoChapter.FilmPhase.Tank && Photo.Chemistry == 3) { Photo.ProcessingTime = clockDuration = 1; Photo.Phase = PhotoChapter.FilmPhase.Processing; }
            else if (id == "retry" && Photo.Phase == PhotoChapter.FilmPhase.Developed && !Photo.Completed) ResetFilm();
            else if (id == "reference-photo") { ViewPrint(false); }
            else if (id == "developed-photo" && Photo.Phase == PhotoChapter.FilmPhase.Developed) ViewPrint(true);
            SyncProps();Photo.Assess();SaveProgress();
        }

        public override void Move(Ray ray)
        {
            if(parkGallery || parkFilmUnavailable)return;
            lastRay = ray;
            if(MovePhysicalPrint(ray))return;
            if (Photo.InViewfinder)
            {
                Vector3 screen = View.WorldToViewportPoint(ray.GetPoint(2));
                if (screen.y > .91f) { Photo.InViewfinder = false; return; }
                if (screen.y < .28f) Photo.WheelMode = 2;
                else if (screen.x > .72f) Photo.WheelMode = 1;
                else if (Mathf.Abs(screen.x - .5f) < .16f && Mathf.Abs(screen.y - .5f) < .18f) Photo.WheelMode = 0;
                return;
            }
            if (looking && !DirectDarkroom)
            {
                Vector2 at = View.WorldToViewportPoint(ray.GetPoint(2));
                Vector2 delta = at - lastLookViewport;
                yaw += delta.x * 110;
                pitch = Mathf.Clamp(pitch - delta.y * 85, -65, 65);
                lastLookViewport = at;
            }
            if (dragged != null)
            {
                    Vector3 point = ProcessingDragPoint(ray)+dragGrabOffset;
                if (dragged == "leader") SetFilmLine(point);
                else if (Nodes.ContainsKey(dragged))
                {
                    Nodes[dragged].transform.position = point;
                    if(dragged=="tank")
                    {
                        Vector3 sink=Nodes[DrainTarget].transform.position;
                        bool over=CanDropDarkroom(ray,DrainTarget);
                        tankBody.localRotation=homeRotations["tank"]*Quaternion.Euler(over?(sceneProcessing?printObjects.tankDrainEuler:new Vector3(0,0,100)):Vector3.zero);
                        if(over){point=ToolDropPreview(DrainTarget,Nodes[dragged]);tankBody.position=point;}
                        pour.gameObject.SetActive(over);
                        if(over){pour.SetPosition(0,point);pour.SetPosition(1,new Vector3(sink.x,point.y-.3f,sink.z));}
                    }
                    else if (dragged.StartsWith("chemical-"))
                    {
                        Vector3 receiver=Printing?developedPrint.transform.position:tankBody.position;
                        bool over = CanPourChemical(dragged) && CanDropDarkroom(ray,"tank");
                        Nodes[dragged].transform.localRotation = homeRotations[dragged]*Quaternion.Euler(over?(sceneProcessing?printObjects.bottlePourEuler:new Vector3(60,0,-35)):Vector3.zero);
                        if(over){point=ToolDropPreview("tank",Nodes[dragged]);Nodes[dragged].transform.position=point;}
                        pour.gameObject.SetActive(over); if (over) { pour.SetPosition(0, point); pour.SetPosition(1, receiver+Vector3.up*.03f); }
                    }
                }
            }
            if (tankHeld && Photo.Phase == PhotoChapter.FilmPhase.Processing)
            {
                Vector3 current = Point(ray, 1.2f);
                float distance = Vector3.Distance(current, lastDrag);
                // Accumulate travel rather than requiring a large single-frame jump.
                // Keep the anchor for sub-millimetre motion to filter pointer jitter.
                if (distance >= .001f) { agitationTravel += distance; lastDrag = current; }
                if (agitationTravel >= .065f) Photo.Agitated = true;
            }
        }

        public override void Release(string overId, Ray ray)
        {
            if(parkGallery || parkFilmUnavailable)return;
            if(!Input.GetMouseButton(1))looking=false;
            if(HasActiveGesture && Input.GetMouseButton(0))return;
            Rewinding = false; bagHeld = false; tankHeld = false;
            bool wasExposing=printHeld;
            CancelPrintExposure();
            if(ReleasePhysicalPrint(ray))return;
            if(wasExposing){SyncProps();SaveProgress();return;}
            if (dragged == null) return;
            NativeActionCount++;
            string target = Pick(ray);
            if(target=="tank-lid")target="tank";
            string item = dragged;
            if(item=="tank" && ManualTank && needsDrain)
            {
                if(CanDropDarkroom(ray,DrainTarget))
                {needsDrain=false;WBMain.Tone(160,.1f);}
                else DarkroomNotice("Release the tank over the drain basin.");
            }
            else if (item == "leader")
            {
                if (target == "spool") Photo.FilmThreaded = true;
                filmStrip.gameObject.SetActive(false);
            }
            else if (item.StartsWith("film-") && Photo.Phase == PhotoChapter.FilmPhase.Empty && target == "chamber")
            {
                Photo.Settings.ISO = int.Parse(item.Substring(5)); Photo.Phase = PhotoChapter.FilmPhase.LoadedOpen;
            }
            else if (item == "exposed-film" && Photo.Phase == PhotoChapter.FilmPhase.Rewound && CanDropDarkroom(ray,"bag"))
            {
                Photo.Phase = PhotoChapter.FilmPhase.DarkBag; Photo.BagTurns = 0; Photo.Chemistry = 0;
            }
            else if(Printing && item.StartsWith("chemical-") && target=="developed-photo")
                PourPrintChemical(int.Parse(item.Substring(9)));
            else if (!Printing && item.StartsWith("chemical-"))
            {
                int index = int.Parse(item.Substring(9));
                if (CanPourChemical(item) && CanDropDarkroom(ray,"tank")) { Photo.ProcessingTime = clockDuration = index == 0 ? 3 : index == 1 ? .8f : 2; Photo.Agitated = false; Photo.Phase = PhotoChapter.FilmPhase.Processing; WBMain.Tone(240, .08f); }
                else { DarkroomNotice(CanPourChemical(item)?"Release the bottle over the tank.":"Use the highlighted bottle after sealing and draining.");WBMain.Tone(100,.08f); }
            }
            else if(item=="exposed-film")DarkroomNotice("Release the film over the dark bag.");
            if (Nodes.ContainsKey(item) && homes.ContainsKey(item))
            {
                Nodes[item].transform.localPosition = homes[item]; Nodes[item].transform.localRotation = homeRotations[item];
                SetColliders(Nodes[item], true);
            }
            dragged = null; pour.gameObject.SetActive(false); SyncProps();SaveProgress();
        }

        public override void Scroll(string id, float delta, Ray ray)
        {
            if(parkGallery || parkFilmUnavailable)return;
            if (Mathf.Abs(delta) < .01f) return;
            NativeActionCount++;
            if (Rewinding && Photo.Phase == PhotoChapter.FilmPhase.Exposed)
            {
                RewindTurns = Mathf.Clamp01(RewindTurns + delta / 8);
                Nodes["rewind"].transform.localRotation = Quaternion.Euler(0, RewindTurns * 720, 0);
                if (RewindTurns >= .999f) { Rewinding = false; Photo.Phase = PhotoChapter.FilmPhase.Rewound; Photo.FilmThreaded = false; RewoundBackOpen = false; ReturnToBench(); WBMain.Tone(180, .15f); }
            }
            else if (bagHeld && Photo.Phase == PhotoChapter.FilmPhase.DarkBag)
            {
                Photo.BagTurns = Mathf.Clamp01(Photo.BagTurns + delta * .1f);
                bagGrip.localRotation = BagGripRotation;
                if (Photo.BagTurns >= .999f) { Photo.Phase = PhotoChapter.FilmPhase.Tank; bagHeld = false; WBMain.Tone(300, .12f); }
            }
            else if (Photo.InViewfinder) Photo.Wheel(delta, Vector2.zero);
            else if (Photo.Phase == PhotoChapter.FilmPhase.Ready && (id == "focal-ring" || id == "shutter-dial" || id == "aperture-ring"))
            {
                bool was = Photo.InViewfinder; Photo.InViewfinder = true; Photo.WheelMode = id == "focal-ring" ? 0 : id == "shutter-dial" ? 1 : 2; Photo.Wheel(delta, Vector2.zero); Photo.InViewfinder = was;
            }
            SyncProps();SaveProgress();
        }

        void BeginDrag(string id, Ray ray)
        {
            dragged = id; dragOrigin = Nodes[id].transform.position; lastDrag = dragOrigin;
            dragGrabOffset=(id=="tank" || id.StartsWith("chemical-"))?dragOrigin-ProcessingDragPoint(ray):Vector3.zero;
            SetColliders(Nodes[id], false); Move(ray);
        }

        bool InsideTable(Vector3 position)
        {
            if(SharedRoom)return !Host.Room.CanWalkAt(World(position));
            return position.z > -3.95f && position.z < -2.25f && position.x > -1.48f && position.x < 1.45f
                || position.x > 1.2f && position.x < 3.15f && position.z > -4.2f && position.z < -1.35f;
        }

        void SetFilmLine(Vector3 end)
        {
            filmStrip.SetPosition(0, leader.transform.position); filmStrip.SetPosition(1, end);
        }

        void ExposeNative()
        {
            Photo.Settings.X = walkPosition.x; Photo.Settings.Z = walkPosition.z; Photo.Settings.Yaw = yaw * Mathf.Deg2Rad; Photo.Settings.Focus = Photo.FocusDistance(Photo.Settings);
            LastShot = new CameraSettingsSnapshot { eye = World(new Vector3(walkPosition.x, 1.5f, walkPosition.z)), yaw = yaw, focal = Photo.Settings.Focal, aperture = Photo.Settings.Aperture, shutter = Photo.Settings.Shutter, iso = Photo.Settings.ISO };
            Photo.Expose(); NativeCaptureCount++;
            RewindTurns=0;Photo.Assess();
            if (exposedTexture != null) ReleaseObject(exposedTexture);
            exposedTexture = RenderPhotograph(Photo.Captured, true);
            savedImage=exposedTexture!=null?Convert.ToBase64String(exposedTexture.EncodeToPNG()):null;SaveProgress();
        }

        void ReturnToBench()
        {
            CameraHeld = false; Photo.InViewfinder = false;
            walkPosition = SharedRoom?new Vector3(0,1.65f,-5):new Vector3(1, 2.35f, -5.8f); yaw = 0; pitch = 27; liftTime = 0;
        }

        void ViewPrint(bool developed)
        {
            Photo.MeterVisible = true;
            Vector3 at = developed ? new Vector3(.56f, 1.4f, -2.466f) : new Vector3(-.72f, 1.4f, -2.466f);
            inspectionEye = World(at + new Vector3(0, 0, -.75f)); inspectionLook = World(at); Eye = inspectionEye; Look = inspectionLook; Fov = 52;
        }

        void ResetFilm()
        {
            if(ParkMode || parkFilmUnavailable)return;
            Photo.Phase = PhotoChapter.FilmPhase.Empty; Photo.Captured = null; Photo.Chemistry = 0; Photo.Fogged = false; Photo.FilmThreaded = false; Photo.BagTurns = 0;
            RewoundBackOpen = false;
            RewindTurns=0;Photo.ProcessingTime=0;Photo.Agitated=false;savedImage=null;
            if(exposedTexture!=null){ReleaseObject(exposedTexture);exposedTexture=null;}
            ReturnToBench();
            SaveProgress();
        }

        void SyncProps()
        {
            bool open = Photo.Phase == PhotoChapter.FilmPhase.Empty || Photo.Phase == PhotoChapter.FilmPhase.LoadedOpen || Photo.Phase == PhotoChapter.FilmPhase.Rewound && RewoundBackOpen;
            cameraBack.localRotation = Quaternion.Euler(0, open ? -112 : 0, 0);
            loadedFilm.SetActive(Photo.Phase == PhotoChapter.FilmPhase.LoadedOpen);
            threadedFilm.SetActive(open && Photo.FilmThreaded); leader.SetActive(open && Photo.Phase == PhotoChapter.FilmPhase.LoadedOpen && !Photo.FilmThreaded);
            Nodes["chamber"].SetActive(open); Nodes["spool"].SetActive(open);
            foreach (GameObject go in rolls) go.SetActive(Photo.Phase == PhotoChapter.FilmPhase.Empty);
            looseFilm.SetActive(Photo.Phase == PhotoChapter.FilmPhase.Rewound && RewoundBackOpen);
            if (looseFilm.activeSelf && dragged != "exposed-film") looseFilm.transform.position = cameraBody.TransformPoint(new Vector3(-.23f, 0, -.205f));
            referencePrint.SetActive(!ParkMode && !parkLoading && !parkFilmUnavailable);
            developedPrint.SetActive(!PhysicalPrinting && ((Printing && printStage>=ParkPrintStage.ContactReady) || (Photo.Phase == PhotoChapter.FilmPhase.Developed && (!ParkMode || (ParkPrintsReady && exposedTexture!=null)))));
            if(Printing)
            {
                var paper=developedPrint.GetComponent<Renderer>().sharedMaterial;paper.mainTexture=Texture2D.whiteTexture;
                paper.color=printStage>=ParkPrintStage.PaperFixer?new Color(.65f,.65f,.62f):new Color(.93f,.9f,.82f);
            }
            if (developedPrint.activeSelf && exposedTexture != null)
            {
                var material = developedPrint.GetComponent<Renderer>().sharedMaterial; material.mainTexture = exposedTexture; material.color = Color.white;
                if (Photo.Fogged) material.mainTexture = Texture2D.whiteTexture;
            }
            bag.transform.localScale = Vector3.Scale(bagHomeScale,Photo.Phase==PhotoChapter.FilmPhase.DarkBag?(sceneProcessing?printObjects.bagWorkingScale:new Vector3(1,1.4375f,1)):Vector3.one);
            bagGrip.localRotation = BagGripRotation;
            // Lift the closed, light-tight lid inside the bag until the player seals it.
            if(tankLid!=null)tankLid.localPosition=tankLidHomePosition+((PhysicalPrinting && printObjectsState!=null && printObjectsState.negativeRemoved) || (ManualTank && Photo.Phase==PhotoChapter.FilmPhase.Tank && !tankSealed)?(sceneProcessing?printObjects.tankLidOpenOffset:Vector3.up*(.16f/Mathf.Max(.001f,tankLid.parent.lossyScale.y))):Vector3.zero);
            focalRing.localRotation = Quaternion.Euler(90, Photo.Settings.Focal * 4, 0);
            apertureRing.localRotation = Quaternion.Euler(90, Photo.Settings.Aperture * 11, 0);
            shutterDial.localRotation = Quaternion.Euler(0, Mathf.Log(1 / Photo.Settings.Shutter, 2) * 28, 0);
            advanceLever.localRotation = Quaternion.Euler(0, Photo.Phase == PhotoChapter.FilmPhase.Ready ? -65 : -28, 0);
            focalNumber.text = Photo.Settings.Focal.ToString("0") + " mm";
            shutterNumber.text = (1 / Photo.Settings.Shutter).ToString("0"); apertureNumber.text = Photo.Settings.Aperture.ToString("0.0"); isoNumber.text = Photo.Settings.ISO.ToString();
            printClip.localRotation = Quaternion.Euler(0, 0, Photo.Completed ? -28 : 0);
            printClip.GetComponent<Renderer>().sharedMaterial.color = feedbackTime > 0 ? feedbackColor : Photo.Completed ? new Color(.38f, .54f, .38f) : new Color(.4f, .47f, .42f);
            SyncPhysicalPrint();
        }

        void RenderReference()
        {
            referenceTexture = RenderPhotograph(Photo.Target, false);
            if (referenceTexture == null) return;
            referencePrint.GetComponent<Renderer>().sharedMaterial.mainTexture = referenceTexture;
            referencePrint.GetComponent<Renderer>().sharedMaterial.color = Color.white;
        }

        Texture2D RenderPhotograph(PhotoChapter.CameraSettings settings, bool expose)
        {
            // Batchmode logical QA has no graphics device; actual captures are
            // additionally verified in the standalone player with D3D11.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return null;
            if (photoCamera == null)
            {
                GameObject go = new GameObject("Physical photographic exposure camera"); go.transform.SetParent(Root.transform, false);
                photoCamera = go.AddComponent<Camera>(); photoCamera.enabled = false; photoCamera.nearClipPlane = .05f; photoCamera.farClipPlane = 30;
                photoCamera.cullingMask &= ~((1<<FirstPersonHands.HandLayer)|(1<<26));
                photoCamera.clearFlags = CameraClearFlags.SolidColor; photoCamera.backgroundColor = new Color(.68f, .72f, .7f);
                photoRT = new RenderTexture(720, 400, 24, RenderTextureFormat.ARGB32); photoRT.Create();
                photoCamera.targetTexture = photoRT;
            }
            // The body in the hands is never baked into its own photograph.
            GameObject[] bodyObjects = { cameraBody.gameObject, handLeft.gameObject, handRight.gameObject, armLeft.gameObject, armRight.gameObject, opticalFinder.gameObject };
            bool[] bodyVisibility = new bool[bodyObjects.Length];
            for (int i = 0; i < bodyObjects.Length; i++) { bodyVisibility[i] = bodyObjects[i].activeSelf; bodyObjects[i].SetActive(false); }
            photoCamera.transform.position = World(new Vector3(settings.X, 1.5f, settings.Z)); photoCamera.transform.rotation = Quaternion.Euler(0, settings.Yaw * Mathf.Rad2Deg, 0);
            photoCamera.aspect = 1.8f; photoCamera.fieldOfView = 2 * Mathf.Atan(20f / (2 * settings.Focal)) * Mathf.Rad2Deg;
            photoCamera.Render();
            RenderTexture old = RenderTexture.active; RenderTexture.active = photoRT;
            var result = new Texture2D(720, 400, TextureFormat.RGB24, false); result.name = expose ? "Actual immutable 3D exposure" : "3D target exposure";
            result.ReadPixels(new Rect(0, 0, 720, 400), 0, 0); result.Apply(); RenderTexture.active = old;
            for (int i = 0; i < bodyObjects.Length; i++) bodyObjects[i].SetActive(bodyVisibility[i]);
            if (expose)
            {
                float brightness = Mathf.Pow(2, PhotoChapter.EV(Photo.Target) - PhotoChapter.EV(settings));
                Color[] pixels = result.GetPixels();
                for (int i = 0; i < pixels.Length; i++) { Color c = pixels[i]; c.r = Mathf.Clamp01(c.r * brightness); c.g = Mathf.Clamp01(c.g * brightness); c.b = Mathf.Clamp01(c.b * brightness); pixels[i] = c; }
                result.SetPixels(pixels); result.Apply();
                ApplyPhotoBlur(result,Photo.FocusBlur,Photo.MotionBlur);
            }
            return result;
        }

        void Remember(string id) { homes[id] = Nodes[id].transform.localPosition; homeRotations[id] = Nodes[id].transform.localRotation; }
        static void SetColliders(GameObject obj, bool value) { foreach (Collider collider in obj.GetComponentsInChildren<Collider>()) collider.enabled = value; }

        // QA changes the actor pose, then still uses the identical physical nodes,
        // Press/Move/Release/Scroll route and exposure camera used by the player.
        public void QASetActor(Vector3 localEye, float yawDegrees = 0) { walkPosition = localEye; yaw = yawDegrees; pitch = 0; liftTime = 0; }
        public Ray QARayTo(string id) { Physics.SyncTransforms(); Vector3 target = Nodes[id].transform.position; return new Ray(target + new Vector3(0, .025f, -1), (target - (target + new Vector3(0, .025f, -1))).normalized); }
        public bool QANodeRaycasts(string id) { return Pick(QARayTo(id)) == id; }

        public override void Dispose()
        {
            ClearDarkroomHighlight();
            SaveProgress();
            RestoreSceneProcessing();
            RestorePrintPoses();
            if(contactTexture!=null)ReleaseObject(contactTexture);
            if(parkReferenceTexture!=null)ReleaseObject(parkReferenceTexture);
            if (referenceTexture != null) ReleaseObject(referenceTexture);
            if (exposedTexture != null) ReleaseObject(exposedTexture);
            if (photoRT != null) { photoRT.Release(); ReleaseObject(photoRT); }
            base.Dispose();
        }
    }
}







