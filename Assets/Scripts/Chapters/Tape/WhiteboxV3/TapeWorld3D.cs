using System;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    public sealed class TapeWorld3D : WorldChapter3D
    {
        public readonly TapeChapter Tape;
        readonly Transform[] cassettes = new Transform[3];
        readonly Transform[,] reels = new Transform[3, 2];
        readonly Transform[,] rolls = new Transform[3, 2];
        readonly GameObject[] breaks = new GameObject[3];
        readonly GameObject[] intact = new GameObject[3];
        readonly Transform[] keys = new Transform[5];
        readonly Renderer[] deliveries = new Renderer[3];
        readonly Renderer[] inspectionEnds = new Renderer[2];
        readonly Vector3[] homes = new Vector3[3];
        readonly Transform sourceCassette, pullStrip, looseStrip;
        readonly Transform[] sourceReels = new Transform[2];
        readonly TextMesh[] counters = new TextMesh[2];
        readonly Transform[] meters = new Transform[2];
        readonly Renderer submitLamp;
        readonly AudioSource sourceAudio, destinationAudio;
        AudioClip sourceClip, destinationClip;
        int audioCommission = -1, audioRevision = -1;
        bool pointerHeld, rotating;
        int heldReel = -1, dragCandidate = -1;
        string dragging = "";
        Vector3 dragPoint, pressPoint;
        readonly Vector3 inspection = new Vector3(0, .89f, -.18f);
        readonly Vector3 sourceHome = new Vector3(-1.0f, .88f, .14f);
        readonly Vector3 deckA = new Vector3(-.48f, .982f, .8f);
        readonly Vector3 deckB = new Vector3(.48f, .982f, .8f);
        readonly Vector3 tapeRoll = new Vector3(1.0f, .89f, -.05f);
        readonly Vector3 delivery = new Vector3(1.0f, .88f, -.58f);
        float rotationYaw, rejectionTimer;
        int seenFailures;

        public TapeWorld3D(WBMain host, TapeChapter model) : base(host, model, 0)
        {
            Tape = model; WantsMouseLook = false;
            Eye = World(new Vector3(0, 2.02f, -1.61f));
            Look = World(new Vector3(0, .85f, .31f)); Fov = 55;
            Room(6, 6);
            Part(null, new Vector3(0, .73f, .18f), new Vector3(2.65f, .14f, 2.13f), "#7f7262");
            Part(null, new Vector3(-1.15f, .35f, .7f), new Vector3(.09f, .7f, .09f), "#574d43");
            Part(null, new Vector3(1.15f, .35f, .7f), new Vector3(.09f, .7f, .09f), "#574d43");
            Part("tape.tray", inspection + Vector3.down * .052f, new Vector3(.63f, .025f, .46f), "#bab6a6");
            for (int side = -1; side <= 1; side += 2)
                Part(null, inspection + new Vector3(side * .33f, -.03f, 0), new Vector3(.016f, .06f, .47f), "#d4c8ad");
            for (int i = 0; i < 2; i++)
                inspectionEnds[i] = Part(null, inspection + new Vector3(i == 0 ? -.29f : .29f, -.02f, -.20f), new Vector3(.035f, .015f, .035f), "#675e44", PrimitiveType.Cylinder).GetComponent<Renderer>();
            // A physically open cassette storage box, with three visibly different blank candidates.
            Part(null, new Vector3(-.29f, .817f, -.62f), new Vector3(1.46f, .024f, .34f), "#5e6358");
            for (int side = -1; side <= 1; side += 2)
                Part(null, new Vector3(-.29f + side * .745f, .86f, -.62f), new Vector3(.025f, .105f, .37f), "#697162");
            for (int i = 0; i < 3; i++)
            {
                homes[i] = new Vector3(-.77f + i * .48f, .87f, -.62f);
                cassettes[i] = MakeCassette(i, homes[i]);
            }
            MakeDeck();
            sourceCassette = MakeSource();
            // Roll, cardboard core and serrated cutter are separate three-dimensional pieces.
            Part("tape.adhesive", tapeRoll, new Vector3(.19f, .07f, .19f), "#d8c99f", PrimitiveType.Cylinder);
            Part(null, tapeRoll + Vector3.up * .04f, new Vector3(.10f, .012f, .10f), "#746650", PrimitiveType.Cylinder);
            Part(null, tapeRoll + new Vector3(0, -.04f, .12f), new Vector3(.23f, .04f, .035f), "#63716b");
            for (int i = 0; i < 9; i++)
                Part(null, tapeRoll + new Vector3((i - 4) * .022f, -.014f, .12f), new Vector3(.01f, .017f, .018f), "#c2c6bc");
            pullStrip = Group("Adhesive pulled from roll", tapeRoll + Vector3.up * .06f);
            Child(pullStrip, null, Vector3.zero, new Vector3(.02f, .008f, .055f), "#dfcf91");
            looseStrip = Group("Torn adhesive cursor prop", tapeRoll);
            Child(looseStrip, null, Vector3.zero, new Vector3(.10f, .008f, .055f), "#dfcf91");
            Part("tape.deliver", delivery + Vector3.down * .035f, new Vector3(.40f, .025f, .34f), "#a9b8a0");
            for (int i = 0; i < 3; i++)
                deliveries[i] = Part(null, new Vector3(.77f + i * .11f, .85f, -.82f), new Vector3(.055f, .015f, .055f), "#565c4f", PrimitiveType.Cylinder).GetComponent<Renderer>();
            submitLamp = Part(null, delivery + new Vector3(.24f, -.005f, 0), new Vector3(.04f, .022f, .04f), "#59634d", PrimitiveType.Cylinder).GetComponent<Renderer>();
            Part("tape.retry", new Vector3(-1.05f, .85f, -.36f), new Vector3(.19f, .03f, .14f), "#adb2a1");
            // Wordless retry glyph: circular route and a short arrow.
            Vector3[] ring = new Vector3[15];
            for (int i = 0; i < ring.Length; i++) { float a = i / 14f * Mathf.PI * 1.8f; ring[i] = new Vector3(-1.05f + Mathf.Cos(a) * .055f, .874f, -.36f + Mathf.Sin(a) * .045f); }
            Line("Retry circular arrow", ring, "#465448", .009f);
            Part(null, new Vector3(-1.0f, .88f, -.38f), new Vector3(.025f, .008f, .025f), "#465448", PrimitiveType.Cube, new Vector3(0, 45, 0));
            var aSpeaker = new GameObject("Source A actual PCM audio"); aSpeaker.transform.SetParent(Root.transform, false);
            sourceAudio = aSpeaker.AddComponent<AudioSource>(); sourceAudio.playOnAwake = false; sourceAudio.volume = .40f;
            var bSpeaker = new GameObject("Destination B actual recorded PCM audio"); bSpeaker.transform.SetParent(Root.transform, false);
            destinationAudio = bSpeaker.AddComponent<AudioSource>(); destinationAudio.playOnAwake = false; destinationAudio.volume = .40f;
            Refresh();
        }

        Transform MakeCassette(int index, Vector3 position)
        {
            Transform g = Group("Blank cassette " + index, position);
            string[] colors = { "#c4bda4", "#afc0b8", "#cab18e" };
            Child(g, "tape.cassette." + index, Vector3.zero, new Vector3(.37f, .06f, .245f), colors[index]);
            Child(g, null, new Vector3(0, .034f, .063f), new Vector3(.31f, .009f, .043f), "#e6dfcb");
            for (int r = 0; r < 2; r++)
            {
                Transform axle = new GameObject("Actual reel " + r).transform; axle.SetParent(g, false);
                axle.localPosition = new Vector3(r == 0 ? -.092f : .092f, .04f, -.007f); reels[index, r] = axle;
                var roll = Child(axle, "tape.spindle." + index + "." + r, Vector3.zero, new Vector3(.105f, .014f, .105f), "#493326", PrimitiveType.Cylinder);
                rolls[index, r] = roll.transform;
                Child(axle, null, new Vector3(0, .014f, 0), new Vector3(.035f, .018f, .035f), "#e1d7ba", PrimitiveType.Cylinder);
                Child(axle, null, new Vector3(0, .026f, 0), new Vector3(.067f, .009f, .012f), "#e1d7ba");
                Child(axle, null, new Vector3(0, .026f, 0), new Vector3(.012f, .009f, .067f), "#e1d7ba");
            }
            intact[index] = Child(g, null, new Vector3(0, .035f, -.096f), new Vector3(.25f, .009f, .012f), "#493326");
            var tear = new GameObject("Visible red tape break").transform; tear.SetParent(g, false);
            Child(tear, null, new Vector3(-.048f, .049f, -.118f), new Vector3(.095f, .01f, .015f), "#df352a", PrimitiveType.Cube, new Vector3(0, -22, 0)).name = "Red break left end";
            Child(tear, null, new Vector3(.05f, .064f, -.13f), new Vector3(.09f, .01f, .015f), "#df352a", PrimitiveType.Cube, new Vector3(13, 24, 17)).name = "Red break right end";
            breaks[index] = tear.gameObject;
            for (int h = 0; h < 2; h++)
            {
                Child(g, "tape.hole." + index + "." + h, new Vector3(h == 0 ? -.13f : .13f, .034f, .107f), new Vector3(.04f, .008f, .025f), "#272d28");
                GameObject patch = Child(g, null, new Vector3(h == 0 ? -.13f : .13f, .041f, .107f), new Vector3(.055f, .007f, .04f), "#ddc989");
                patch.name = "Record protect adhesive " + h;
            }
            return g;
        }

        Transform MakeSource()
        {
            Transform g = Group("Current commission source cassette", sourceHome);
            Child(g, "tape.source", Vector3.zero, new Vector3(.37f, .06f, .245f), "#879b91");
            Child(g, null, new Vector3(0, .034f, .065f), new Vector3(.31f, .008f, .05f), "#e5ddc6");
            for (int side = 0; side < 2; side++)
            {
                Transform reel = new GameObject("Source reel " + side).transform; reel.SetParent(g, false);
                reel.localPosition = new Vector3(side == 0 ? -.092f : .092f, .045f, -.008f);
                Child(reel, null, Vector3.zero, new Vector3(.09f, .012f, .09f), "#40342c", PrimitiveType.Cylinder);
                Child(reel, null, Vector3.up * .014f, new Vector3(.066f, .008f, .012f), "#d6c9a9"); sourceReels[side] = reel;
            }
            // Three physical commission dots, never a task list or caption.
            for (int i = 0; i < 3; i++) Child(g, null, new Vector3((i - 1) * .06f, .043f, .064f), new Vector3(.018f, .007f, .018f), "#9a7656", PrimitiveType.Cylinder);
            return g;
        }

        void MakeDeck()
        {
            Part(null, new Vector3(0, .855f, .8f), new Vector3(1.53f, .15f, .76f), "#4b5750");
            Part(null, new Vector3(0, .944f, .8f), new Vector3(1.45f, .027f, .69f), "#abb8a8");
            for (int d = 0; d < 2; d++)
            {
                float x = d == 0 ? -.48f : .48f;
                Part(d == 0 ? "tape.deck.A" : "tape.deck.B", new Vector3(x, .965f, .8f), new Vector3(.43f, .017f, .30f), "#293e35");
                Part(null, new Vector3(x, 1.005f, 1.02f), new Vector3(.42f, .035f, .035f), "#798b7b");
                counters[d] = Label("00.0", new Vector3(x, .982f, 1.125f), .042f, "#283c31", new Vector3(90, 0, 0));
                Transform meter = Group("Actual level meter " + d, new Vector3(d == 0 ? -.18f : .18f, .985f, .94f));
                Child(meter, null, new Vector3(0, 0, .032f), new Vector3(.009f, .008f, .07f), "#a26940"); meters[d] = meter;
            }
            for (int k = 0; k < 5; k++)
            {
                float x = .12f + k * .14f;
                Transform key = Group("B held transport key " + k, new Vector3(x, .993f, .51f)); keys[k] = key;
                Child(key, "tape.key.B." + k, Vector3.zero, new Vector3(.12f, .045f, .105f), k == 0 ? "#b48b6c" : "#d5d3bd");
                Glyph(key, k, new Vector3(0, .027f, 0), .032f);
            }
            for (int k = 0; k < 4; k++)
            {
                float x = -.68f + k * .14f;
                Transform key = Group("A mouse transport key " + k, new Vector3(x, .995f, .51f));
                Child(key, "tape.key.A." + k, Vector3.zero, new Vector3(.12f, .045f, .105f), "#bbc7b4");
                Glyph(key, k == 0 ? 2 : k == 1 ? 1 : k == 2 ? 4 : 3, new Vector3(0, .027f, 0), .032f);
            }
        }
        void Glyph(Transform parent, int type, Vector3 p, float size)
        {
            if (type == 0) Child(parent, null, p, new Vector3(size, .007f, size), "#b53e30", PrimitiveType.Cylinder);
            else if (type == 4) Child(parent, null, p, new Vector3(size, .007f, size), "#3a4a3e");
            else
            {
                int n = type == 1 ? 1 : 2;
                for (int i = 0; i < n; i++)
                {
                    float direction = type == 2 ? -1 : 1;
                    Vector3 q = p + Vector3.right * (i - (n - 1) * .5f) * size;
                    Vector3[] vertices = { q + new Vector3(-direction * size * .45f, 0, -size * .55f), q + new Vector3(-direction * size * .45f, 0, size * .55f), q + new Vector3(direction * size * .55f, 0, 0) };
                    var go = new GameObject("Transport pictogram"); go.transform.SetParent(parent, false);
                    var mesh = new Mesh(); mesh.name = "Wordless transport triangle"; mesh.vertices = vertices; mesh.triangles = new[] { 0, 1, 2, 2, 1, 0 }; mesh.RecalculateNormals();
                    go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = Material("#3a4a3e");
                }
            }
        }

        static int LastNumber(string id) { int n; int dot = id.LastIndexOf('.'); return dot >= 0 && int.TryParse(id.Substring(dot + 1), out n) ? n : -1; }
        int CandidateFrom(string id)
        {
            if (id.StartsWith("tape.cassette.")) return LastNumber(id);
            if (id.StartsWith("tape.spindle.") || id.StartsWith("tape.hole.")) { string[] parts = id.Split('.'); int n; return parts.Length > 2 && int.TryParse(parts[2], out n) ? n : -1; }
            return -1;
        }
        public override void Press(string id, Ray ray, int button)
        {
            id = id ?? ""; pointerHeld = true; pressPoint = Point(ray, .93f) - Origin;
            if (Tape.Completed) return;
            if (id.StartsWith("tape.key.A."))
            {
                int n = LastNumber(id); Tape.SetSourceTransport(n == 0 ? -1 : n == 1 ? 1 : n == 2 ? 0 : 2); return;
            }
            if (button != 0 && button != 1) return;
            if (id == "tape.retry") { Tape.ResetCurrentRecording(); WBMain.Tone(180, .06f); return; }
            if (id == "tape.adhesive") { Tape.BeginTapePull(); return; }
            int index = CandidateFrom(id);
            if (id.StartsWith("tape.spindle.") && index == Tape.Selected && !Tape.B.Loaded)
            { heldReel = LastNumber(id); return; }
            if (id.StartsWith("tape.hole.") && index == Tape.Selected)
            { if (Tape.PatchProtectHole(LastNumber(id))) WBMain.Tone(350, .035f); Refresh(); return; }
            if (id == "tape.source" && !Tape.A.Loaded) { dragging = "source"; dragPoint = sourceHome; return; }
            if (id.StartsWith("tape.cassette.") && index >= 0)
            {
                if (Tape.B.Loaded && index == Tape.Selected && !Tape.B.Play && Tape.B.Seek == 0)
                { dragging = "deliver"; dragCandidate = index; dragPoint = deckB; return; }
                if (!Tape.B.Loaded && (index != Tape.Selected || Tape.TargetPrepared))
                { dragging = "candidate"; dragCandidate = index; dragPoint = cassettes[index].localPosition; return; }
                if (!Tape.B.Loaded && index == Tape.Selected) rotating = true;
            }
        }
        public override void Move(Ray ray)
        {
            Vector3 p = Point(ray, .93f) - Origin;
            p.x = Mathf.Clamp(p.x, -1.25f, 1.25f); p.z = Mathf.Clamp(p.z, -.87f, 1.08f);
            if (dragging != "") dragPoint = p;
            if (rotating && pointerHeld) rotationYaw = (p.x - pressPoint.x) * 180;
            if (Tape.PullingTape) Tape.MoveTapePull(Vector3.Distance(p, tapeRoll));
            looseStrip.localPosition = p + Vector3.up * .05f;
            Refresh();
        }
        public override void Release(string overId, Ray ray)
        {
            Vector3 p = Point(ray, .93f) - Origin;
            if (Tape.PullingTape) { Tape.MoveTapePull(Vector3.Distance(p, tapeRoll)); Tape.CompleteTapePull(); WBMain.Tone(510, .035f); }
            if (dragging == "source" && Near(p, deckA, .30f)) { Tape.LoadSource(); WBMain.Tone(250, .04f); }
            if (dragging == "candidate")
            {
                if (Near(p, inspection, .38f)) { Tape.SelectCandidate(dragCandidate); rotationYaw = 0; }
                else if (dragCandidate == Tape.Selected && Near(p, deckB, .30f) && Tape.LoadTarget()) WBMain.Tone(250, .04f);
            }
            if (dragging == "deliver" && Near(p, delivery, .28f)) Tape.Submit();
            pointerHeld = rotating = false; heldReel = -1; dragging = ""; dragCandidate = -1;
            Tape.ReleaseSourceSeek(); Refresh();
        }
        static bool Near(Vector3 p, Vector3 target, float range) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(target.x, target.z)) < range;
        public override void Scroll(string id, float delta, Ray ray)
        {
            if (pointerHeld && heldReel >= 0 && dragging == "")
            {
                bool wasVerified = Tape.SourceVerified;
                bool wasBroken = Tape.Selected >= 0 && Tape.Candidates[Tape.Selected].Broken;
                Tape.InspectReel(heldReel, delta);
                if (!wasVerified && Tape.SourceVerified) WBMain.Tone(580, .10f);
                if (!wasBroken && Tape.Selected >= 0 && Tape.Candidates[Tape.Selected].Broken) WBMain.Tone(95, .09f);
                Refresh();
            }
        }
        public override void Tick(float dt, WBInput input)
        {
            Tape.Tick(dt, input); rejectionTimer = Mathf.Max(0, rejectionTimer - dt);
            if (Tape.SubmissionFailures != seenFailures) { seenFailures = Tape.SubmissionFailures; rejectionTimer = 1; WBMain.Tone(120, .12f); }
            for (int i = 0; i < keys.Length; i++)
            {
                Vector3 p = keys[i].localPosition; p.y = Mathf.Lerp(p.y, Tape.HeldKeys[i] ? .973f : .993f, Mathf.Clamp01(dt * 25)); keys[i].localPosition = p;
            }
            Refresh(); SyncAudio();
        }
        void Refresh()
        {
            for (int i = 0; i < 3; i++)
            {
                bool selected = i == Tape.Selected;
                cassettes[i].localPosition = dragCandidate == i && dragging != "" ? dragPoint : selected ? (Tape.B.Loaded ? deckB : inspection) : homes[i];
                cassettes[i].localScale = selected && !Tape.B.Loaded && dragging == "" ? Vector3.one * 1.40f : Vector3.one;
                cassettes[i].localRotation = Quaternion.Euler(0, selected && !Tape.B.Loaded && dragging == "" ? rotationYaw : 0, 0);
                breaks[i].SetActive(Tape.Candidates[i].Broken); intact[i].SetActive(!Tape.Candidates[i].Broken);
                float position = selected && Tape.B.Loaded ? Tape.B.Position : Tape.Candidates[i].Position;
                for (int r = 0; r < 2; r++)
                {
                    reels[i, r].localRotation = Quaternion.Euler(0, position * 230 * (r == 0 ? 1 : -1), 0);
                    float amount = r == 0 ? 1 - position / TapeChapter.Duration : position / TapeChapter.Duration;
                    float radius = Mathf.Lerp(.044f, .12f, amount);
                    rolls[i, r].localScale = new Vector3(radius, .014f, radius);
                }
                Transform leftPatch = cassettes[i].Find("Record protect adhesive 0"), rightPatch = cassettes[i].Find("Record protect adhesive 1");
                if (leftPatch != null) leftPatch.gameObject.SetActive(selected && Tape.Patched[0]);
                if (rightPatch != null) rightPatch.gameObject.SetActive(selected && Tape.Patched[1]);
            }
            sourceCassette.localPosition = dragging == "source" ? dragPoint : Tape.A.Loaded ? deckA : sourceHome;
            for (int i = 0; i < inspectionEnds.Length; i++)
            {
                bool reached = Tape.Selected >= 0 && (i == 0 ? Tape.Candidates[Tape.Selected].ReachedStart : Tape.Candidates[Tape.Selected].ReachedEnd);
                inspectionEnds[i].sharedMaterial.color = reached ? new Color(.49f, .67f, .38f) : new Color(.4f, .37f, .27f);
            }
            for (int i = 0; i < sourceReels.Length; i++) sourceReels[i].localRotation = Quaternion.Euler(0, Tape.A.Position * 230 * (i == 0 ? 1 : -1), 0);
            pullStrip.gameObject.SetActive(Tape.PullingTape);
            pullStrip.localScale = new Vector3(Mathf.Max(1, Tape.PullLength / .02f), 1, 1);
            looseStrip.gameObject.SetActive(Tape.TapePieceReady);
            for (int i = 0; i < deliveries.Length; i++) deliveries[i].sharedMaterial.color = i < Tape.DeliveredCount ? new Color(.50f, .69f, .43f) : new Color(.29f, .33f, .27f);
            submitLamp.sharedMaterial.color = rejectionTimer > 0 ? new Color(.83f, .20f, .13f) : Tape.Completed ? new Color(.4f, .8f, .38f) : new Color(.36f, .42f, .30f);
            counters[0].text = Tape.A.Position.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
            counters[1].text = Tape.B.Position.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
            for (int d = 0; d < 2; d++)
            {
                int frame = d == 0 ? Tape.A.Frame : Tape.B.Frame;
                bool running = d == 0 ? Tape.A.Play : Tape.B.Play;
                float value = d == 0 ? Tape.Programme.Source[Mathf.Clamp(frame, 0, Tape.Programme.Source.Length - 1)] : Tape.RecordedPcm[Mathf.Clamp(frame, 0, Tape.RecordedPcm.Length - 1)];
                meters[d].localRotation = Quaternion.Euler(0, running ? -35 + Mathf.Abs(value) * 160 : -40, 0);
            }
        }
        void SyncAudio()
        {
            if (audioCommission != Tape.CommissionIndex)
            {
                sourceAudio.Stop(); destinationAudio.Stop();
                if (sourceClip != null) ReleaseObject(sourceClip);
                sourceClip = AudioClip.Create("Commission source PCM " + Tape.CommissionIndex, Tape.Programme.Source.Length, 1, TapePcm.SampleRate, false);
                sourceClip.SetData(Tape.Programme.Source, 0); sourceAudio.clip = sourceClip; audioCommission = Tape.CommissionIndex;
            }
            PlayAt(sourceAudio, Tape.A.Play && Active, Tape.A.Frame);
            bool monitor = Tape.B.Play && !Tape.B.Armed && Tape.RecordedFrames > 0 && Tape.B.Frame < Tape.RecordedFrames && Active;
            if (monitor && audioRevision != Tape.Revision)
            {
                destinationAudio.Stop(); if (destinationClip != null) ReleaseObject(destinationClip);
                float[] recorded = new float[Tape.RecordedFrames]; Array.Copy(Tape.RecordedPcm, recorded, recorded.Length);
                destinationClip = AudioClip.Create("Player destination PCM", recorded.Length, 1, TapePcm.SampleRate, false);
                destinationClip.SetData(recorded, 0); destinationAudio.clip = destinationClip; audioRevision = Tape.Revision;
            }
            PlayAt(destinationAudio, monitor, Tape.B.Frame);
        }
        static void PlayAt(AudioSource source, bool play, int frame)
        {
            if (!play || source.clip == null) { if (source.isPlaying) source.Stop(); return; }
            frame = Mathf.Clamp(frame, 0, source.clip.samples - 1);
            if (!source.isPlaying) { source.timeSamples = frame; source.Play(); }
            else if (Mathf.Abs(source.timeSamples - frame) > TapePcm.SampleRate / 5) source.timeSamples = frame;
        }
        public override void SuspendInput()
        {
            // Releasing focus cancels a hand gesture; it never drops or submits a cassette.
            pointerHeld = rotating = false; dragging = ""; heldReel = -1; dragCandidate = -1;
            Tape.PullingTape = false; Tape.PullLength = 0;
            Tape.ReleaseSourceSeek(); Tape.Tick(0, new WBInput());
            Array.Clear(Tape.HeldKeys, 0, Tape.HeldKeys.Length);
            sourceAudio.Stop(); destinationAudio.Stop(); Refresh();
            // A's latched transport and both heads stay where they were. The next active
            // Tick resumes A from its PCM head; B requires newly held physical keys.
        }
        public override void Deactivate()
        {
            pointerHeld = rotating = false; dragging = ""; heldReel = -1;
            Tape.ReleaseSourceSeek(); Tape.SetSourceTransport(0); sourceAudio.Stop(); destinationAudio.Stop(); base.Deactivate();
        }
        public override void Dispose()
        {
            if (sourceClip != null) ReleaseObject(sourceClip);
            if (destinationClip != null) ReleaseObject(destinationClip);
            if (Root != null) foreach (var f in Root.GetComponentsInChildren<MeshFilter>()) if (f.sharedMesh != null && f.sharedMesh.name == "Wordless transport triangle") ReleaseObject(f.sharedMesh);
            base.Dispose();
        }
        public override string Help => Tape.Help;
    }
}
