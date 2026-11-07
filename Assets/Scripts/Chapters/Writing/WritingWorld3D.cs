using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    /// <summary>
    /// Two simultaneous perspective cameras, both rendering spatial geometry.
    /// The left viewport shows writing; the right viewport shows the memory.
    /// WritingChapter is only the tested rule model. Its Draw method is never used.
    /// </summary>
    public sealed partial class WritingWorld3D : WorldChapter3D
    {
        public readonly WritingChapter State;
        public readonly Camera MemoryCamera;
        public string LastHit { get; private set; }
        public int InkStrokeCount { get { return ink.Count; } }
        public bool PaperGrabbed { get { return draggingPaper; } }
        public Vector3 PaperCenter { get { return desk.paper.bounds.center + desk.paper.transform.up*.004f; } }
        public Vector3 BinCenter { get { return desk.basketMouth.position; } }
        AutumnWritingDesk desk;

        const int MemoryLayer = 26;
        readonly Transform memory, player, npc, npcBox, scarf, door, doorHand;
        Transform penHand,paperBall;
        GameObject sheet;
        readonly Renderer memoryWindow, handle;
        readonly List<LineRenderer> ink = new List<LineRenderer>();
        readonly List<Vector3> activeStroke = new List<Vector3>();
        readonly List<int> strokeRows = new List<int>();
        readonly Vector3 memoryOffset = new Vector3(12, 0, 0);
        int originalMask, previousDraft, previousRow = -1;
        float time, lineExtent, penLift, crumple;
        bool writingHeld, draggingPaper, newSheetSoundPending;
        public override Transform[] PlayerHandTargets => new Transform[]{null,writingHandContact != null ? writingHandContact : penHand};
        public override bool PlayerHandsEngaged => writingHeld || draggingPaper;
        public override bool HasPointerGesture => writingHeld || draggingPaper;
        public override void CancelPointerGesture()
        {
            // Cancel is never a successful drop in the bin. Keep already written ink.
            State.CancelInput();
            CancelAutumnPresentation();
            StorySfx.Stop(32);
            newSheetSoundPending=false;
            writingHeld=draggingPaper=false;currentStroke=null;activeStroke.Clear();
            if(desk!=null){sheet.SetActive(true);paperBall.gameObject.SetActive(false);}
            foreach(var stroke in ink)stroke.gameObject.SetActive(true);
        }
        Vector3 penPoint, lastMemoryPlayer;
        Rect originalRect;
        string previousPhase;
        LineRenderer currentStroke;
        Light memoryLight;

        public override Vector3 World(Vector3 local)=>Root.transform.TransformPoint(local);
        bool fitted;
        Vector3 originalEye,originalLook;

        public void FitTeaTable(AutumnWritingDesk layout)
        {
            if(fitted)return;
            fitted=true;originalEye=Eye;originalLook=Look;
            // The memory and its own camera remain at their authored world poses.
            memory.SetParent(null,true);MemoryCamera.transform.SetParent(null,true);
            var root=Root.transform;
            root.SetPositionAndRotation(layout.transform.position,layout.transform.rotation);
            root.localScale=layout.transform.lossyScale;
            AttachDesk(layout);
            binFraming=State.Phase=="crumple"?1:0;
            FrameLetter();
            memory.SetParent(root,true);MemoryCamera.transform.SetParent(root,true);
        }

        const float FirstInkRow=.175f;
        const float InkRowSpacing=.055f;
        float binFraming;

        void FrameLetter()
        {
            Fov=48;
            CalculateLetterFrame(Mathf.Max(.15f,.5f*Screen.width/Mathf.Max(1,Screen.height)),binFraming,out Eye,out Look);
        }


        public override Quaternion ViewRotation=>desk!=null
            ? Quaternion.LookRotation(-desk.paper.transform.up,desk.paper.transform.forward)
            : base.ViewRotation;

        public void ReleaseTeaTable()
        {
            if(!fitted)return;
            fitted=false;desk.Present(false);
            memory.SetParent(null,true);MemoryCamera.transform.SetParent(null,true);
            Root.transform.SetPositionAndRotation(Origin,Quaternion.identity);Root.transform.localScale=Vector3.one;
            memory.SetParent(Root.transform,true);MemoryCamera.transform.SetParent(Root.transform,true);
            Eye=originalEye;Look=originalLook;
        }
        public override string Help=>State.Help;

        public WritingWorld3D(WBMain host, WritingChapter model) : base(host, model, 2)
        {
            State = model; previousDraft = State.Draft; previousPhase = State.Phase;
            Room(5, 6);
            Eye = World(new Vector3(.30f, 2.40f, -1.90f));
            Look = World(new Vector3(.30f, .85f, -.05f)); Fov = 64;
            memory = Group("A room held in memory - full geometry", memoryOffset);
            BuildMemoryRoom();
            player = Person("memory-player", "#72a9ad", true);
            doorHand = Child(player, "memory-player-holding-door", new Vector3(.30f, .88f, -.08f), new Vector3(.26f, .08f, .09f), "#d5b795", PrimitiveType.Capsule, new Vector3(0, 0, 90)).transform;
            doorHand.gameObject.SetActive(false);
            npc = Person("memory-person", "#c2ad91", false);
            npcBox = Child(npc, "carried-box", new Vector3(0, .47f, -.20f), new Vector3(.36f, .3f, .27f), "#957957").transform;
            scarf = Child(memory, "memory-scarf", new Vector3(1.3f, .61f, 1.50f), new Vector3(.55f, .045f, .14f), "#b66054").transform;
            Child(scarf, null, new Vector3(.19f, -.18f, 0), new Vector3(.11f, .37f, .13f), "#ad5149");
            door = new GameObject("hinged memory door").transform; door.SetParent(memory, false); door.localPosition = new Vector3(2.45f, 0, .10f);
            Child(door, "memory-door", new Vector3(0, 1.04f, -.48f), new Vector3(.075f, 2.08f, .94f), "#8b775f");
            handle = Child(door, "memory-handle", new Vector3(-.075f, 1.0f, -.78f), new Vector3(.16f, .045f, .04f), "#d5bc87").GetComponent<Renderer>();
            memoryWindow = Child(memory, "memory-window", new Vector3(3f, 1.65f, 5.20f), new Vector3(1.6f, 1.55f, .04f), "#b6cac6").GetComponent<Renderer>();
            Child(memory, null, new Vector3(3f, 1.65f, 5.14f), new Vector3(.07f, 1.65f, .08f), "#ddd5ba");
            Child(memory, null, new Vector3(3f, 1.65f, 5.14f), new Vector3(1.65f, .06f, .08f), "#ddd5ba");
            var lamp = new GameObject("Memory warmth follows the pen"); lamp.transform.SetParent(memory, false); lamp.transform.localPosition = new Vector3(3.6f, 3.9f, 1.5f);
            memoryLight = lamp.AddComponent<Light>(); memoryLight.type = LightType.Directional; lamp.transform.localRotation=Quaternion.Euler(48,155,0); memoryLight.shadows=LightShadows.Soft; memoryLight.shadowCustomResolution=1024; memoryLight.shadowStrength=.72f; memoryLight.shadowBias=.035f; memoryLight.shadowNormalBias=.12f; memoryLight.range = 11; memoryLight.intensity = 1.2f; memoryLight.color = new Color(1, .93f, .81f); memoryLight.cullingMask = 1 << MemoryLayer;
            memoryWindow.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(memoryWindow.sharedMaterial.HasProperty("_EmissionColor"))
            {
                memoryWindow.sharedMaterial.EnableKeyword("_EMISSION");
                memoryWindow.sharedMaterial.SetColor("_EmissionColor",new Color(.32f,.39f,.43f));
            }
            var fillObject=new GameObject("Memory soft sky fill");fillObject.transform.SetParent(memory,false);
            fillObject.transform.localRotation=Quaternion.Euler(65,-25,0);
            var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;
            fill.color=new Color(.78f,.86f,1);fill.shadows=LightShadows.None;fill.cullingMask=1<<MemoryLayer;
            SetLayer(memory, MemoryLayer);
            var cameraObject = new GameObject("Right memory perspective camera"); cameraObject.transform.SetParent(Root.transform, false);
            MemoryCamera = cameraObject.AddComponent<Camera>(); MemoryCamera.orthographic = false;
            MemoryCamera.transform.position = World(memoryOffset + new Vector3(3.0f, 9.5f, -5.8f));
            MemoryCamera.transform.LookAt(World(memoryOffset + new Vector3(3f, .55f, 2.6f)));
            MemoryCamera.fieldOfView = 2*Mathf.Atan(Mathf.Tan(23*Mathf.Deg2Rad)/1.2f)*Mathf.Rad2Deg; MemoryCamera.nearClipPlane = .05f; MemoryCamera.farClipPlane = 25;
            MemoryCamera.clearFlags = CameraClearFlags.SolidColor; MemoryCamera.backgroundColor = new Color(.12f, .17f, .18f); MemoryCamera.cullingMask = 1 << MemoryLayer;
            MemoryCamera.depth = View != null ? View.depth + 1 : 1; MemoryCamera.rect = new Rect(.5f, 0, .5f, 1); MemoryCamera.enabled = false;
            BuildAutumnProps();BuildWritingVisuals();UpdateActors(.02f);
        }

        void BuildMemoryRoom()
        {
            visualAssets=WritingVisualAssets.Load();
            if(visualAssets==null || visualAssets.memoryBedroom==null)
                throw new System.InvalidOperationException("Memory bedroom prefab is not bound.");
            CreateVisual(visualAssets.memoryBedroom,memory);
        }

        Transform Person(string id, string coat, bool small)
        {
            Transform person = new GameObject(id).transform; person.SetParent(memory, false);
            float s = small ? .85f : 1;
            Child(person, id + "-body", new Vector3(0, .60f * s, 0), new Vector3(.38f, .70f, .27f) * s, coat, PrimitiveType.Capsule);
            Child(person, null, new Vector3(0, 1.13f * s, 0), Vector3.one * .29f * s, "#d5b795", PrimitiveType.Sphere);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Child(person, null, new Vector3(sign * .11f * s, .20f * s, 0), new Vector3(.11f, .42f, .13f) * s, "#525e5b", PrimitiveType.Capsule);
                Child(person, null, new Vector3(sign * .25f * s, .69f * s, -.035f), new Vector3(.10f, .45f, .11f) * s, coat, PrimitiveType.Capsule, new Vector3(15, 0, sign * 12));
            }
            return person;
        }
        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayer(child, layer);
        }
        static Vector3 MemoryPosition(Vector2 p) { return new Vector3((p.x - 80) * .012f, 0, (610 - p.y) * .012f); }
        Vector2 PaperCoordinates(Ray ray)
        {
            var hit=desk.paper;
            var plane=new Plane(hit.transform.up,PaperCenter);
            if(!plane.Raycast(ray,out float distance))return new Vector2(-1,-1);
            Vector3 p=hit.transform.InverseTransformPoint(ray.GetPoint(distance))-hit.center;
            // Chapter-local paper coordinates, independent of the screen half.
            return new Vector2(685+(p.x/hit.size.x+.5f)*535,165+(.5f-p.z/hit.size.z)*445);
        }
        public override void Activate()
        {
            base.Activate();
            RestoreInk();
            UpdateActors(0);UpdateWritingVisuals(0, true);desk.ShowDrafts(State.Draft);
            if(State.WritingProgress==0)
            {
                foreach(var stroke in ink)ReleaseObject(stroke.gameObject);
                ink.Clear();strokeRows.Clear();activeStroke.Clear();currentStroke=null;
                lineExtent=0;previousRow=-1;previousDraft=State.Draft;previousPhase=State.Phase;
                writingHeld=draggingPaper=false;sheet.SetActive(true);paperBall.gameObject.SetActive(false);
                penPoint=desk.InkPoint(BodyRowOrigin,0);
                if(!State.Carried)
                {
                    scarf.SetParent(memory,false);scarf.localPosition=new Vector3(1.3f,.61f,1.50f);
                    scarf.localRotation=Quaternion.identity;
                }
            }
            if (View != null) { originalRect = View.rect; originalMask = View.cullingMask; View.rect = new Rect(0, 0, .5f, 1); View.cullingMask = originalMask & ~(1 << MemoryLayer); }
            MemoryCamera.enabled = true;
        }
        public override void Deactivate()
        {
            CancelPointerGesture();
            MemoryCamera.enabled = false;
            if (View != null) { View.cullingMask = originalMask == 0 ? -1 : originalMask; View.rect = originalRect.width > 0 ? originalRect : new Rect(0, 0, 1, 1); }
            writingHeld = false; State.PointerUp(Vector2.zero); draggingPaper = false;
            sheet.SetActive(true); paperBall.gameObject.SetActive(false);
            base.Deactivate();
        }
        internal override Transform HoverVisual(Vector2 point,out Camera camera)
        {
            camera=View;
            if(State.Completed || !sheet || !View.pixelRect.Contains(point))return null;
            return Pick(PointerRay(point))=="paper" && !HasPointerGesture?sheet.transform:null;
        }
        public override Ray PointerRay(Vector2 nativeScreenBottomLeft)
        {
            return MemoryCamera.pixelRect.Contains(nativeScreenBottomLeft)
                ? MemoryCamera.ScreenPointToRay(nativeScreenBottomLeft)
                : View.ScreenPointToRay(nativeScreenBottomLeft);
        }
        public override void Press(string id, Ray ray, int button)
        {
            LastHit = id;
            if (button != 0 || State.Completed || HasPointerGesture) return;
            if (State.Phase == "crumple" && id == "paper")
            {
                draggingPaper = true; crumple = 0;crumpleVisual.SetCrumpleProgress(0);
                State.PointerDown(new Vector2(1195, 590), 0);
                sheet.SetActive(false); foreach (LineRenderer stroke in ink) stroke.gameObject.SetActive(false);
                paperBall.gameObject.SetActive(true); MoveFoldedPaper(ray);
                StorySfx.Shot(33,paperBall.position);
                return;
            }
            if (id != "paper") return;
            writingHeld = true; activeStroke.Clear(); currentStroke = null;
            int oldRow = State.Row; bool wasAwaitingReturn = State.AwaitingReturn;
            State.PointerDown(PaperCoordinates(ray), 0);
            AcceptNewRow(oldRow, wasAwaitingReturn);
            Move(ray);
        }
        void MoveFoldedPaper(Ray ray)
        {
            var plane=new Plane(sheet.transform.up,PaperCenter+sheet.transform.up*.025f);
            if(plane.Raycast(ray,out float distance))paperBall.position=ray.GetPoint(distance);
        }
        public override void Move(Ray ray)
        {
            if (draggingPaper)
            {
                MoveFoldedPaper(ray);
                State.PointerMove(new Vector2(1195, 590)); return;
            }
            if (!writingHeld || State.Phase == "crumple") return;
            Vector2 p = PaperCoordinates(ray);
            int oldRow = State.Row; bool wasAwaitingReturn = State.AwaitingReturn; float before = State.WritingProgress;
            bool wasStarted=State.Started;
            State.PointerMove(p);
            AcceptNewRow(oldRow, wasAwaitingReturn);
            float amount = State.WritingProgress - before;
            if (amount <= 0) return;
            if (previousRow != State.Row) { lineExtent = 0; previousRow = State.Row; currentStroke = null; activeStroke.Clear(); }
            AppendInk(amount);
            StorySfx.Motion(32,sheet.transform.TransformPoint(penPoint));
            if(!wasStarted && State.Started)StorySfx.Shot(35,TimerSoundPosition);
        }
        void AcceptNewRow(int oldRow, bool wasAwaitingReturn)
        {
            if (State.Row != oldRow || wasAwaitingReturn && !State.AwaitingReturn)
            {
                // Once nine rows are visible the model rolls the oldest row off the page.
                // Keep that physical ink layout in sync instead of overdrawing the last row.
                if (State.Row == oldRow && oldRow == 8)
                {
                    for (int i = ink.Count - 1; i >= 0; i--)
                    {
                        strokeRows[i]--;
                        if (strokeRows[i] < 0) { ReleaseObject(ink[i].gameObject); ink.RemoveAt(i); strokeRows.RemoveAt(i); }
                        else ink[i].transform.localPosition += Vector3.forward * (InkRowSpacing*desk.paper.size.z);
                    }
                }
                lineExtent = 0; currentStroke = null; activeStroke.Clear();
            }
        }
        void AppendInk(float amount)
        {
            if (currentStroke == null)
            {
                currentStroke = desk.CreateInkStroke("Ink physically laid on paper - draft " + State.Draft + " row " + State.Row, sheet.transform);
                ink.Add(currentStroke); strokeRows.Add(State.Row);
            }
            float end = Mathf.Min(425, lineExtent + amount);
            desk.ExtendInk(currentStroke,activeStroke,BodyRowOrigin,lineExtent,end);
            lineExtent=end;
            penPoint=desk.InkPoint(BodyRowOrigin,end);
            penLift=0;
        }
        Vector2 BodyRowOrigin=>new Vector2(AutumnWritingDesk.LeftMargin,FirstInkRow-State.Row*InkRowSpacing);
        void RestoreInk()
        {
            foreach(var stroke in ink)ReleaseObject(stroke.gameObject);
            ink.Clear();strokeRows.Clear();activeStroke.Clear();currentStroke=null;
            int savedRow=State.Row;
            for(int row=0;row<State.InkRows.Count;row++)
            {
                if(State.InkRows[row]<=0)continue;
                State.Row=row;lineExtent=0;currentStroke=null;activeStroke.Clear();AppendInk(State.InkRows[row]);
            }
            State.Row=savedRow;lineExtent=State.InkRows[savedRow];previousRow=savedRow;previousDraft=State.Draft;previousPhase=State.Phase;
            currentStroke=null;activeStroke.Clear();
        }
        public override void Release(string overId, Ray ray)
        {
            StorySfx.Stop(32);
            if (draggingPaper)
            {
                bool inBin = !string.IsNullOrEmpty(overId) && overId.StartsWith("bin");
                int draft=State.Draft;
                State.PointerUp(inBin ? new Vector2(1185, 635) : new Vector2(950, 450));
                if(State.Draft!=draft)
                {
                    StorySfx.Shot(233,BinCenter);
                    newSheetSoundPending=true;
                }
                draggingPaper = false; sheet.SetActive(true); paperBall.gameObject.SetActive(false);
                foreach (LineRenderer stroke in ink) stroke.gameObject.SetActive(true);
            }
            else State.PointerUp(PaperCoordinates(ray));
            writingHeld = false; currentStroke = null; activeStroke.Clear();
        }
        public override void Tick(float dt, WBInput input)
        {
            float elapsed=Mathf.Max(0,dt);dt = Mathf.Min(.1f, dt); time += dt;
            int collected=State.Collected;
            string phase=State.Phase;
            bool clockRunning=State.Started && phase=="collect";
            float remaining=State.Remaining;
            bool fullRowBeforeTick=State.AwaitingReturn;
            State.Tick(elapsed, input);
            if(phase!="arrival" && State.Phase=="arrival" && fullRowBeforeTick && !State.AwaitingReturn)
                RestoreInk();
            pickupIntent=input.Keys.Contains(KeyCode.E) || State.PickupPrepared;
            PlayWritingStateEdges(collected,phase,clockRunning,remaining);
            bool reachBlocked=input.Keys.Contains(KeyCode.E) && State.NearPickup && !State.InkFresh
                && (State.Phase=="tutorial" || State.Phase=="collect");
            if (State.Draft != previousDraft)
            {
                foreach (var stroke in ink) ReleaseObject(stroke.gameObject);
                ink.Clear(); strokeRows.Clear(); activeStroke.Clear(); currentStroke = null; lineExtent = 0; previousRow = -1;
                previousDraft = State.Draft; penPoint=desk.InkPoint(BodyRowOrigin,0);
                desk.ShowDrafts(State.Draft);
                if(newSheetSoundPending)
                {
                    StorySfx.Shot(133,PaperCenter);
                    newSheetSoundPending=false;
                }
            }
            if (State.Phase != previousPhase && State.Phase == "crumple") writingHeld = false;
            previousPhase = State.Phase;
            penLift = Mathf.MoveTowards(penLift, State.Phase == "crumple" || !writingHeld ? .065f : 0, dt * .3f);
            if(writingHeld)
            {
                var target=sheet.transform.TransformPoint(penPoint+Vector3.up*penLift);
                penHand.position+=(target-penTip.position)*Mathf.Clamp01(dt*24);
            }
            else penHand.localPosition=Vector3.Lerp(penHand.localPosition,Vector3.zero,Mathf.Clamp01(dt*24));
            if (State.Phase == "crumple") penHand.localEulerAngles = new Vector3(0, Mathf.Sin(time * 5) * 5, 0);
            else penHand.localEulerAngles = reachBlocked?new Vector3(0,0,3*Mathf.Sin(time*12)):Vector3.zero;
            if (draggingPaper) { crumple = Mathf.Min(1, crumple + dt * 4); crumpleVisual.SetCrumpleProgress(crumple); }
            binFraming=Mathf.MoveTowards(binFraming,State.Phase=="crumple"?1:0,dt*4);
            FrameLetter();UpdateActors(dt);UpdateWritingVisuals(dt);
        }
        public override void Dispose()
        {
            StorySfx.Stop(32);
            foreach(var stroke in ink)if(stroke!=null)ReleaseObject(stroke.gameObject);
            ink.Clear();
            StorySfx.Stop(234);
            base.Dispose();
        }
        void UpdateActors(float dt){UpdateAutumnActors(dt);}
    }
}
