using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    /// <summary>
    /// Two simultaneous perspective cameras, both rendering spatial geometry.
    /// The right hand writes on a desk; the left hand inhabits a small memory.
    /// WritingChapter is only the tested rule model. Its Draw method is never used.
    /// </summary>
    public sealed class WritingWorld3D : WorldChapter3D
    {
        public readonly WritingChapter State;
        public readonly Camera MemoryCamera;
        public string LastHit { get; private set; }
        public int InkStrokeCount { get { return ink.Count; } }
        public bool PaperGrabbed { get { return draggingPaper; } }
        public Vector3 PaperCenter { get { return World(new Vector3(-.07f, 1.015f, .12f)); } }
        public Vector3 BinCenter { get { return World(binAt + Vector3.up * .74f); } }
        public readonly Renderer Desktop;

        const int MemoryLayer = 26;
        readonly Transform memory, player, npc, npcBox, scarf, door, penHand, paperBall, doorHand;
        readonly GameObject sheet;
        readonly Renderer memoryWindow, handle, binLip;
        readonly List<LineRenderer> ink = new List<LineRenderer>();
        readonly List<Vector3> activeStroke = new List<Vector3>();
        readonly List<int> strokeRows = new List<int>();
        readonly Vector3 memoryOffset = new Vector3(12, 0, 0);
        readonly Vector3 binAt = new Vector3(.92f, 0, -.97f);
        int originalMask, previousDraft, previousRow = -1;
        float time, lineExtent, penLift, crumple;
        bool writingHeld, draggingPaper;
        public override Transform[] PlayerHandTargets => new Transform[]{null,penHand};
        public override bool PlayerHandsEngaged => writingHeld || draggingPaper;
        Vector3 penPoint, lastMemoryPlayer;
        Rect originalRect;
        string previousPhase;
        LineRenderer currentStroke;
        Light memoryLight;

        public override string Help { get { return "LMB drag right  Write  /  Return pen left for next line\nWASD  Move memory  /  E  Hold door\nDiscard draft in bin; carry scarf to the person by the window."; } }

        public WritingWorld3D(WBMain host, WritingChapter model) : base(host, model, 2)
        {
            State = model; previousDraft = State.Draft; previousPhase = State.Phase;
            if(SharedRoom)binAt+=Vector3.down*.13f;
            Room(5, 6);
            Eye = World(new Vector3(.30f, 2.40f, -1.90f));
            Look = World(new Vector3(.30f, .85f, -.05f)); Fov = 64;
            Desktop = Part(null, new Vector3(0, .93f, .15f), new Vector3(2.65f, .12f, 1.65f), "#87715a").GetComponent<Renderer>();
            if(SharedRoom)Desktop.gameObject.SetActive(false);
            if(!SharedRoom)for (int x = -1; x <= 1; x += 2)
                Part(null, new Vector3(x * 1.08f, .45f, .52f), new Vector3(.10f, .9f, .1f), "#685b4e");
            sheet = Part("paper", new Vector3(-.075f, 1.001f, .12f), new Vector3(.95f, .012f, .72f), "#eee6cf");
            Part(null, new Vector3(-.075f, .994f, .12f), new Vector3(.97f, .012f, .74f), "#bdb29b");
            Part(null, new Vector3(-.82f, 1.07f, .66f), new Vector3(.13f, .15f, .13f), "#293431");
            Part(null, new Vector3(-.82f, 1.156f, .66f), new Vector3(.085f, .025f, .085f), "#151e20", PrimitiveType.Cylinder);
            Part(null, new Vector3(.75f, 1.035f, .7f), new Vector3(.36f, .045f, .34f), "#c7baa1");
            // A genuinely open box: the bottom and rim are pickable, not an image.
            // The basket is in front of the desk, beyond its opaque front edge.
            // Keeping it beneath the writing surface makes a valid ray target invisible.
            Part("bin", binAt + new Vector3(0, .35f, 0), new Vector3(.51f, .06f, .47f), "#4d5855");
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Part("bin-side-" + sign, binAt + new Vector3(sign * .255f, .54f, 0), new Vector3(.026f, .38f, .49f), "#6e7770");
                Part("bin-edge-" + sign, binAt + new Vector3(0, .54f, sign * .235f), new Vector3(.51f, .38f, .026f), "#6e7770");
            }
            binLip = Part("bin-rim", binAt + new Vector3(0, .75f, .24f), new Vector3(.56f, .04f, .035f), "#a4aa96").GetComponent<Renderer>();
            paperBall = Group("Crumpled paper held by the right hand", new Vector3(0, 1.15f, 0));
            Child(paperBall, null, Vector3.zero, new Vector3(.17f, .13f, .16f), "#e6ddc6", PrimitiveType.Sphere);
            for (int i = 0; i < 5; i++)
                Child(paperBall, null, new Vector3(Mathf.Sin(i * 2.5f) * .045f, .02f, Mathf.Cos(i * 2.5f) * .045f), new Vector3(.09f, .07f, .08f), i % 2 == 0 ? "#c4b79d" : "#eee6cf", PrimitiveType.Cube, new Vector3(i * 23, i * 41, i * 17));
            paperBall.gameObject.SetActive(false);
            penHand = Group("Right hand and pen", Vector3.zero);
            Child(penHand, null, new Vector3(.07f, .05f, -.10f), new Vector3(.18f, .08f, .13f), "#c9a587", PrimitiveType.Sphere, new Vector3(0, 25, 0));
            Child(penHand, null, new Vector3(.16f, .055f, -.23f), new Vector3(.13f, .095f, .29f), "#889394", PrimitiveType.Capsule, new Vector3(72, 0, -28));
            Child(penHand, null, new Vector3(.025f, .059f, -.02f), new Vector3(.018f, .018f, .28f), "#303a3c", PrimitiveType.Cylinder, new Vector3(62, -22, 0));
            Child(penHand, null, new Vector3(.025f, .006f, .032f), new Vector3(.018f, .023f, .018f), "#1f2624", PrimitiveType.Sphere);
            penPoint = new Vector3(-.44f, 1.02f, .38f); penHand.localPosition = penPoint;

            memory = Group("A room held in memory - full geometry", memoryOffset);
            BuildMemoryRoom();
            player = Person("memory-player", "#72a9ad", true);
            doorHand = Child(player, "memory-player-holding-door", new Vector3(.30f, .88f, -.08f), new Vector3(.26f, .08f, .09f), "#d5b795", PrimitiveType.Capsule, new Vector3(0, 0, 90)).transform;
            doorHand.gameObject.SetActive(false);
            npc = Person("memory-person", "#c2ad91", false);
            npcBox = Child(npc, "carried-box", new Vector3(0, .47f, -.20f), new Vector3(.36f, .3f, .27f), "#957957").transform;
            scarf = Child(memory, "memory-scarf", new Vector3(1.3f, .61f, 1.50f), new Vector3(.55f, .045f, .14f), "#b66054").transform;
            Child(scarf, null, new Vector3(.19f, -.18f, 0), new Vector3(.11f, .37f, .13f), "#ad5149");
            door = new GameObject("hinged memory door").transform; door.SetParent(memory, false); door.localPosition = new Vector3(3.12f, 0, 3.09f);
            Child(door, "memory-door", new Vector3(0, 1.04f, -.48f), new Vector3(.075f, 2.08f, .94f), "#8b775f");
            handle = Child(door, "memory-handle", new Vector3(-.075f, 1.0f, -.78f), new Vector3(.16f, .045f, .04f), "#d5bc87").GetComponent<Renderer>();
            memoryWindow = Child(memory, "memory-window", new Vector3(4.80f, 1.5f, 5.1f), new Vector3(1.6f, 1.55f, .04f), "#b6cac6").GetComponent<Renderer>();
            Child(memory, null, new Vector3(4.8f, 1.5f, 5.04f), new Vector3(.07f, 1.65f, .08f), "#ddd5ba");
            Child(memory, null, new Vector3(4.8f, 1.5f, 5.04f), new Vector3(1.65f, .06f, .08f), "#ddd5ba");
            var lamp = new GameObject("Memory warmth follows the pen"); lamp.transform.SetParent(memory, false); lamp.transform.localPosition = new Vector3(3.6f, 3.9f, 1.5f);
            memoryLight = lamp.AddComponent<Light>(); memoryLight.type = LightType.Point; memoryLight.range = 11; memoryLight.intensity = 1.6f; memoryLight.color = new Color(1, .85f, .64f); memoryLight.cullingMask = 1 << MemoryLayer;
            SetLayer(memory, MemoryLayer);
            var cameraObject = new GameObject("Left memory perspective camera"); cameraObject.transform.SetParent(Root.transform, false);
            MemoryCamera = cameraObject.AddComponent<Camera>(); MemoryCamera.orthographic = false;
            MemoryCamera.transform.position = World(memoryOffset + new Vector3(3.10f, 6.4f, -5.1f));
            MemoryCamera.transform.LookAt(World(memoryOffset + new Vector3(3.05f, .45f, 2.5f)));
            MemoryCamera.fieldOfView = 49; MemoryCamera.nearClipPlane = .05f; MemoryCamera.farClipPlane = 25;
            MemoryCamera.clearFlags = CameraClearFlags.SolidColor; MemoryCamera.backgroundColor = new Color(.12f, .17f, .18f); MemoryCamera.cullingMask = 1 << MemoryLayer;
            MemoryCamera.depth = View != null ? View.depth + 1 : 1; MemoryCamera.rect = new Rect(0, 0, .5f, 1); MemoryCamera.enabled = false;
            UpdateActors(.02f);
        }

        void BuildMemoryRoom()
        {
            Child(memory, null, new Vector3(3, -.08f, 2.6f), new Vector3(6.15f, .16f, 5.3f), "#837e6d");
            Child(memory, null, new Vector3(0, 1.2f, 2.65f), new Vector3(.13f, 2.4f, 5.3f), "#8e9687");
            Child(memory, null, new Vector3(6.0f, 1.2f, 2.65f), new Vector3(.13f, 2.4f, 5.3f), "#778278");
            Child(memory, null, new Vector3(3, 1.2f, 5.25f), new Vector3(6.2f, 2.4f, .13f), "#8c9486");
            Child(memory, null, new Vector3(3.12f, .94f, .99f), new Vector3(.14f, 1.88f, 2.12f), "#a5a28b");
            Child(memory, null, new Vector3(3.12f, .94f, 4.26f), new Vector3(.14f, 1.88f, 2.35f), "#a5a28b");
            Child(memory, null, new Vector3(3.12f, 2.20f, 2.56f), new Vector3(.14f, .24f, 1.06f), "#a5a28b");
            // The wall stops above the doorway. The player must actually pass its opening.
            for (int i = 1; i < 7; i++)
                Child(memory, null, new Vector3(i * .85f, .008f, 2.6f), new Vector3(.012f, .008f, 5.2f), "#645f52");
            Child(memory, null, new Vector3(1.3f, .50f, 1.50f), new Vector3(.72f, .12f, .58f), "#5a645c");
            Child(memory, null, new Vector3(1.3f, .94f, 1.78f), new Vector3(.72f, .78f, .08f), "#5a645c");
            for (int sign = -1; sign <= 1; sign += 2)
                Child(memory, null, new Vector3(1.3f + sign * .29f, .25f, 1.50f), new Vector3(.07f, .5f, .48f), "#4b5550");
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
            Vector3 p = Point(ray, 1.016f) - Origin;
            return new Vector2(685 + (p.x + .55f) / .95f * 535, 165 + (.48f - p.z) / .72f * 445);
        }
        public override void Activate()
        {
            base.Activate();
            if(State.WritingProgress==0)
            {
                foreach(var stroke in ink)ReleaseObject(stroke.gameObject);
                ink.Clear();strokeRows.Clear();activeStroke.Clear();currentStroke=null;
                lineExtent=0;previousRow=-1;previousDraft=State.Draft;previousPhase=State.Phase;
                writingHeld=draggingPaper=false;sheet.SetActive(true);paperBall.gameObject.SetActive(false);
                penPoint=new Vector3(-.44f,1.02f,.38f);
                if(!State.Carried)
                {
                    scarf.SetParent(memory,false);scarf.localPosition=new Vector3(1.3f,.61f,1.50f);
                    scarf.localRotation=Quaternion.identity;
                }
            }
            if (View != null) { originalRect = View.rect; originalMask = View.cullingMask; View.rect = new Rect(.5f, 0, .5f, 1); View.cullingMask = originalMask & ~(1 << MemoryLayer); }
            MemoryCamera.enabled = true;
        }
        public override void Deactivate()
        {
            if(Host!=null && Host.sharedHouse)Release("",new Ray(Vector3.zero,Vector3.up));
            MemoryCamera.enabled = false;
            if (View != null) { View.cullingMask = originalMask == 0 ? -1 : originalMask; View.rect = originalRect.width > 0 ? originalRect : new Rect(0, 0, 1, 1); }
            writingHeld = false; State.PointerUp(Vector2.zero); draggingPaper = false;
            sheet.SetActive(true); paperBall.gameObject.SetActive(false);
            base.Deactivate();
        }
        public override Ray PointerRay(Vector2 nativeScreenBottomLeft)
        {
            return nativeScreenBottomLeft.x < Screen.width * .5f ? MemoryCamera.ScreenPointToRay(nativeScreenBottomLeft) : View.ScreenPointToRay(nativeScreenBottomLeft);
        }
        public override void Press(string id, Ray ray, int button)
        {
            LastHit = id;
            if (button != 0 || State.Completed) return;
            if (State.Phase == "crumple" && id == "paper")
            {
                draggingPaper = true; crumple = 0;
                State.PointerDown(new Vector2(1195, 590), 0);
                sheet.SetActive(false); foreach (LineRenderer stroke in ink) stroke.gameObject.SetActive(false);
                paperBall.gameObject.SetActive(true); paperBall.position = Point(ray, 1.07f);
                return;
            }
            if (id != "paper") return;
            writingHeld = true; activeStroke.Clear(); currentStroke = null;
            int oldRow = State.Row; bool wasAwaitingReturn = State.AwaitingReturn;
            State.PointerDown(PaperCoordinates(ray), 0);
            AcceptNewRow(oldRow, wasAwaitingReturn);
            Move(ray);
        }
        public override void Move(Ray ray)
        {
            if (draggingPaper)
            {
                paperBall.position = Point(ray, 1.07f);
                State.PointerMove(new Vector2(1195, 590)); return;
            }
            if (!writingHeld || State.Phase == "crumple") return;
            Vector2 p = PaperCoordinates(ray);
            int oldRow = State.Row; bool wasAwaitingReturn = State.AwaitingReturn; float before = State.WritingProgress;
            State.PointerMove(p);
            AcceptNewRow(oldRow, wasAwaitingReturn);
            float amount = State.WritingProgress - before;
            if (amount <= 0) return;
            if (previousRow != State.Row) { lineExtent = 0; previousRow = State.Row; currentStroke = null; activeStroke.Clear(); }
            AppendInk(amount);
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
                        else ink[i].transform.localPosition += Vector3.forward * .0615f;
                    }
                }
                lineExtent = 0; currentStroke = null; activeStroke.Clear();
            }
        }
        void AppendInk(float amount)
        {
            if (currentStroke == null)
            {
                currentStroke = Line("Ink physically laid on paper - draft " + State.Draft + " row " + State.Row,
                    new Vector3[0], "#3e3e35", .0025f);
                ink.Add(currentStroke); strokeRows.Add(State.Row);
            }
            float end = Mathf.Min(425, lineExtent + amount);
            for (float a = lineExtent; a < end; a += 2.5f)
            {
                float wave = Mathf.Sin(a * .26f) * (3 + Mathf.Sin(a * .063f) * 2) + Mathf.Sin(a * .67f) * 2;
                activeStroke.Add(new Vector3(-.467f + a / 535f * .95f, 1.009f, .399f - State.Row * .0615f + wave * .0016f));
            }
            lineExtent = end;
            currentStroke.positionCount = activeStroke.Count; currentStroke.SetPositions(activeStroke.ToArray());
            float v = Mathf.Sin(end * .26f) * (3 + Mathf.Sin(end * .063f) * 2) + Mathf.Sin(end * .67f) * 2;
            penPoint = new Vector3(-.467f + end / 535f * .95f, 1.022f, .399f - State.Row * .0615f + v * .0016f);
            penLift = 0;
        }
        public override void Release(string overId, Ray ray)
        {
            if (draggingPaper)
            {
                bool inBin = !string.IsNullOrEmpty(overId) && overId.StartsWith("bin");
                State.PointerUp(inBin ? new Vector2(1185, 635) : new Vector2(950, 450));
                draggingPaper = false; sheet.SetActive(true); paperBall.gameObject.SetActive(false);
                foreach (LineRenderer stroke in ink) stroke.gameObject.SetActive(true);
            }
            else State.PointerUp(PaperCoordinates(ray));
            writingHeld = false; currentStroke = null; activeStroke.Clear();
        }
        public override void Tick(float dt, WBInput input)
        {
            dt = Mathf.Min(.1f, dt); time += dt;
            State.Tick(dt, input);
            if (State.Draft != previousDraft)
            {
                foreach (var stroke in ink) ReleaseObject(stroke.gameObject);
                ink.Clear(); strokeRows.Clear(); activeStroke.Clear(); currentStroke = null; lineExtent = 0; previousRow = -1;
                previousDraft = State.Draft; penPoint = new Vector3(-.44f, 1.02f, .38f);
                Part(null, binAt + new Vector3(0, .43f, 0), Vector3.one * .15f, "#d3c7ad", PrimitiveType.Sphere);
            }
            if (State.Phase != previousPhase && State.Phase == "crumple") writingHeld = false;
            previousPhase = State.Phase;
            penLift = Mathf.MoveTowards(penLift, State.Phase == "crumple" || !writingHeld ? .065f : 0, dt * .3f);
            penHand.localPosition = Vector3.Lerp(penHand.localPosition, penPoint + Vector3.up * penLift, Mathf.Clamp01(dt * 24));
            if (State.Phase == "crumple") penHand.localEulerAngles = new Vector3(0, Mathf.Sin(time * 5) * 5, 0);
            else penHand.localEulerAngles = Vector3.zero;
            if (draggingPaper) { crumple = Mathf.Min(1, crumple + dt * 4); paperBall.localScale = Vector3.one * Mathf.Lerp(1.7f, 1, crumple); }
            binLip.sharedMaterial.color = Color.Lerp(new Color(.55f, .59f, .52f), new Color(.95f, .75f, .40f), State.Phase == "crumple" ? .5f + Mathf.Sin(time * 3) * .25f : 0);
            UpdateActors(dt);
        }
        void UpdateActors(float dt)
        {
            Vector3 next = MemoryPosition(State.Player), npcPosition = MemoryPosition(State.Npc);
            Vector3 direction = next - lastMemoryPlayer;
            if (direction.sqrMagnitude > .00001f) player.localRotation = Quaternion.LookRotation(direction);
            player.localPosition = next; npc.localPosition = npcPosition;
            npc.localRotation = State.Phase == "door" ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, 180, 0);
            lastMemoryPlayer = next;
            npcBox.gameObject.SetActive(State.Phase == "door" || State.Phase == "crumple");
            if (State.Carried)
            {
                scarf.SetParent(State.Completed ? npc : player, false); scarf.localPosition = new Vector3(0, .86f, -.11f); scarf.localRotation = Quaternion.identity;
            }
            else scarf.gameObject.SetActive(State.Phase == "return");
            bool open = State.DoorOpen || State.DoorHeld && State.InkFresh;
            door.localRotation = Quaternion.Slerp(door.localRotation, Quaternion.Euler(0, open ? -100 : 0, 0), Mathf.Clamp01(dt * 10));
            handle.sharedMaterial.color = State.Phase == "door" ? Color.Lerp(new Color(.65f, .52f, .30f), new Color(1, .85f, .48f), .5f + Mathf.Sin(time * 4) * .35f) : new Color(.65f, .58f, .42f);
            float warmth = State.Completed ? 1 : State.InkFresh ? .85f : .22f;
            memoryLight.intensity = Mathf.Lerp(memoryLight.intensity, .65f + warmth * 1.7f, Mathf.Clamp01(dt * 3));
            memoryWindow.sharedMaterial.color = Color.Lerp(new Color(.37f, .46f, .49f), new Color(.9f, .85f, .65f), warmth);
            // The player can see the remembered hand physically support the doorway.
            doorHand.gameObject.SetActive(State.DoorHeld);
        }
    }
}

