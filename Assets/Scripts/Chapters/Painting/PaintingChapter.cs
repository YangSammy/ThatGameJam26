using System.Globalization;
using UnityEngine;

namespace MemoryWhitebox
{
    // Immediate-mode art shared only by these two chapters; no assets or packages.
    internal static class MemoryArt
    {
        internal static Vector2 V(float x, float y) { return new Vector2(x, y); }
        internal static void Stroke(Vector2 a, Vector2 b, Color color, float width)
        {
            G.Line(a, b, color, width); G.Circle(a, width * .5f, color); G.Circle(b, width * .5f, color);
        }
        internal static void Scarf(Vector2[] points, float width = 35, string color = "#a6504f")
        {
            Color main = G.C(color), weave = G.C("#d58d75");
            for (int i = 1; i < points.Length; i++)
            {
                Stroke(points[i - 1], points[i], main, width);
                G.Line(points[i - 1] + Vector2.up * 5, points[i] + Vector2.up * 5, weave, 1);
            }
            for (int i = -2; i <= 2; i++)
            {
                G.Line(points[0] + V(-5, i * 5), points[0] + V(-22, i * 5 + 5), main, 2);
                G.Line(points[points.Length - 1] + V(4, i * 5), points[points.Length - 1] + V(19, i * 5 + 3), main, 2);
            }
        }
        private static Vector2 Rot(Vector2 p, float angle)
        {
            float a = angle * Mathf.Deg2Rad;
            return V(p.x * Mathf.Cos(a) - p.y * Mathf.Sin(a), p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a));
        }
        internal static void Hand(Vector2 at, float angle, string color = "#d1a88c", float scale = 1)
        {
            Color c = G.C(color);
            Vector2[] a = { V(-73, 2), V(-8, -7), V(-2, 1), V(-3, 8), V(-20, 9) };
            Vector2[] b = { V(8, -2), V(32, -11), V(35, 0), V(29, 10), V(10, 24) };
            for (int i = 0; i < a.Length; i++)
                Stroke(at + Rot(a[i] * scale, angle), at + Rot(b[i] * scale, angle), c, (i == 0 ? 26 : 10) * scale);
        }
        internal static void Cup(Vector2 p, string color = "#c2c1a9", bool outline = false)
        {
            Color c = G.C(color);
            if (!outline) G.Box(new Rect(p.x - 22, p.y, 44, 46), c);
            G.Line(p + V(-25, 0), p + V(-20, 45), c, 3);
            G.Line(p + V(-20, 45), p + V(20, 45), c, 3);
            G.Line(p + V(20, 45), p + V(25, 0), c, 3);
            G.Ring(p + V(26, 22), 12, 1, c, 3);
            G.Line(p + V(-25, 0), p + V(25, 0), outline ? c : G.C("#7c8070"), 5);
        }
        internal static void Background(bool warm = false)
        {
            G.Box(new Rect(0, 90, 1280, 670), G.C(warm ? "#322e2d" : "#232d31"));
        }
        internal static void Guide(Vector2 point, Vector2 target, float time, float radius)
        {
            G.Ring(point, 14 + Mathf.Sin(time * 3) * 2, 1, G.C("#f3dab3"), 2);
            G.Ring(target, radius, 1, new Color(.95f, .85f, .7f, .28f), 2);
            for (int i = 0; i < 12; i++)
                G.Circle(Vector2.Lerp(point, target, i / 12f), 1.5f, new Color(.95f, .85f, .7f, .25f));
            G.Circle(Vector2.Lerp(point, target, (time * .45f) % 1), 3, G.C("#e8cda4"));
        }
    }

    public class PaintingChapter : Chapter
    {
        public int Phase { get; private set; }
        public float Focus { get; private set; }
        public float Elapsed { get; private set; }
        public bool[] Painted { get; private set; } = new bool[3];
        public Vector2 CurrentTarget { get { return Targets[Mathf.Min(Phase, 2)]; } }
        // Drawing and focus validation share the same contact state.
        public bool CupContact { get { return Elapsed % 5 < 2.7f; } }
        public bool HandsContact { get { return Elapsed % 6 < 3.8f; } }
        public bool Ready { get { return Phase == 0 || (Phase == 1 ? CupContact : Phase == 2 && HandsContact); } }
        private readonly Vector2[] Targets = { new Vector2(1030, 412), new Vector2(885, 327), new Vector2(935, 557) };
        private readonly float[] Radii = { 48, 35, 46 };
        private Vector2 pointer;
        private float finishTime;
        public override string Help
        {
            get { return "鼠标控制视线，按住左键才聚焦。参照底稿，依次看向红围巾、手停在杯口时的接触处、两手相接处，按住左键稳定注视约 1.25 秒。动作未对齐时等待；松开或移开会减少当前聚焦，已完成的笔触保留。"; }
        }
        public override string DebugState
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{{\"phase\":{0},\"focus\":{1:0.00},\"time\":{2:0.00},\"ready\":{3},\"completed\":{4}}}", Phase, Focus, Elapsed, Ready ? "true" : "false", Completed ? "true" : "false"); }
        }
        public override void Tick(float dt, WBInput input)
        {
            dt = Mathf.Clamp(dt, 0, .1f); Elapsed += dt; pointer = input.Pointer;
            if (Phase == 3)
            {
                finishTime += dt;
                if (finishTime > 2.4f && !Completed) Finish();
                return;
            }
            bool looking = input.Down && Vector2.Distance(pointer, CurrentTarget) < Radii[Phase];
            Focus = Mathf.Clamp(Focus + dt * (Ready && looking ? 1 : -.8f), 0, 1.25f);
            if (Focus >= 1.25f)
            {
                Painted[Phase] = true; WBMain.Tone(new float[] { 330, 392, 494 }[Phase], .35f);
                Phase++; Focus = 0;
            }
        }
        public override void PointerMove(Vector2 p) { pointer = p; }
        public override void PointerDown(Vector2 p, int button) { pointer = p; }
        public override void PointerUp(Vector2 p) { pointer = p; }
        public override void Draw()
        {
            MemoryArt.Background();
            G.Box(new Rect(52, 123, 553, 579), G.C("#a99378"));
            G.Box(new Rect(68, 137, 520, 546), G.C("#e4d8bc"));
            for (int y = 150; y < 680; y += 10)
                G.Line(new Vector2(75, y), new Vector2(581, y + 1), new Color(.4f, .3f, .2f, .04f), 1);
            string[] faint = { "#b5aa94", "#b5aa94", "#b5aa94" };
            faint[Mathf.Min(Phase, 2)] = "#948774";
            MemoryArt.Scarf(new[] { new Vector2(215, 228), new Vector2(290, 254), new Vector2(352, 227), new Vector2(420, 267) }, 25, Painted[0] ? "#a6504f" : faint[0]);
            MemoryArt.Cup(new Vector2(310, 360), Painted[1] ? "#8d9d98" : faint[1], !Painted[1]);
            MemoryArt.Hand(new Vector2(362, 363), 180, Painted[1] ? "#b78a70" : faint[1], .6f);
            MemoryArt.Hand(new Vector2(308, 535), 0, Painted[2] ? "#c29b80" : faint[2], .8f);
            MemoryArt.Hand(new Vector2(328, 533), 180, Painted[2] ? "#9d7665" : faint[2], .8f);
            G.Box(new Rect(654, 140, 566, 525), G.C("#4e5855"));
            G.Box(new Rect(1030, 145, 184, 203), G.C("#b5b89b"));
            G.Line(new Vector2(1115, 145), new Vector2(1115, 350), G.C("#ded4b7"), 5);
            G.Line(new Vector2(1030, 235), new Vector2(1214, 235), G.C("#ded4b7"), 5);
            G.Box(new Rect(685, 383, 510, 63), G.C("#85705b"));
            float move = CupContact ? 0 : Mathf.Sin((Elapsed % 5 - 2.7f) / 2.3f * Mathf.PI) * 115;
            MemoryArt.Cup(new Vector2(856, 329));
            MemoryArt.Hand(new Vector2(912 + move, 329 + move * .18f), 180, "#c79e83", .9f);
            G.Line(new Vector2(852, 308), new Vector2(848 + Mathf.Sin(Elapsed) * 4, 280), new Color(.87f, .84f, .72f, .4f), 2);
            MemoryArt.Scarf(new[] { new Vector2(987, 390), new Vector2(1030, 414), new Vector2(1095, 389), new Vector2(1123, 447) }, 30);
            float gap = HandsContact ? 0 : Mathf.Sin((Elapsed % 6 - 3.8f) / 2.2f * Mathf.PI) * 100;
            MemoryArt.Hand(new Vector2(924 - gap, 557), -2, "#c79e83", 1.1f);
            MemoryArt.Hand(new Vector2(950 + gap, 553), 182, "#ad826e", 1.1f);
            if (Phase < 3)
            {
                if (Vector2.Distance(pointer, CurrentTarget) < Radii[Phase] || Focus > 0)
                    G.Ring(CurrentTarget, Radii[Phase] + 12, Focus / 1.25f, G.C("#efcea0"), 3);
                if (pointer.x > 650 && pointer.y > 140) G.Ring(pointer, 6, 1, new Color(.94f, .90f, .82f, .5f), 1);
            }
            else
            {
                float lead = Mathf.Clamp01(finishTime / 2.1f);
                MemoryArt.Scarf(new[] { new Vector2(417, 267), new Vector2(490, 276), new Vector2(540 + 650 * lead, 650) }, 14);
                G.Box(new Rect(0, 90, 1280, 670), new Color(.91f, .76f, .54f, lead * .12f));
            }
        }
    }

    public class EndingChapter : Chapter
    {
        public int Phase { get; private set; }
        public bool Dragging { get; private set; }
        public bool LeftHeld { get; private set; }
        public Vector2 Point { get { return Phase == 0 ? corner : Phase == 1 ? tail : tight; } }
        public Vector2 Target { get { return Phase == 0 ? new Vector2(450, 442) : Phase == 1 ? new Vector2(580, 448) : new Vector2(660, 613); } }
        private Vector2 corner = new Vector2(365, 385), tail = new Vector2(920, 430), tight = new Vector2(660, 460), pointer;
        private float time, finishTime;
        public override string Help
        {
            get { return "左键按住亮圈，拖到淡色轮廓：先抚平左侧折角，再把右端交叠到中间。最后按住 W/A/S/D 任一键（或左右方向键）扶住左端，同时用鼠标把右端向下拉紧；无需松键。中途松手可以从当前位置继续。"; }
        }
        public override string DebugState
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{{\"phase\":{0},\"dragging\":{1},\"leftHeld\":{2},\"x\":{3:0},\"y\":{4:0},\"completed\":{5}}}", Phase, Dragging ? "true" : "false", LeftHeld ? "true" : "false", Point.x, Point.y, Completed ? "true" : "false"); }
        }
        private void Advance()
        {
            WBMain.Tone(new float[] { 293, 349, 440 }[Phase], .45f); Phase++; Dragging = false;
        }
        public override void Tick(float dt, WBInput input)
        {
            dt = Mathf.Clamp(dt, 0, .1f); time += dt;
            LeftHeld = input.Keys != null && (input.Keys.Contains(KeyCode.A) || input.Keys.Contains(KeyCode.D) || input.Keys.Contains(KeyCode.W) || input.Keys.Contains(KeyCode.S) || input.Keys.Contains(KeyCode.LeftArrow) || input.Keys.Contains(KeyCode.RightArrow));
            if (Dragging && Phase == 2 && LeftHeld && Vector2.Distance(tight, Target) < 65) Advance();
            if (Phase == 3) { finishTime += dt; if (finishTime > 2.8f && !Completed) Finish(); }
        }
        public override void PointerDown(Vector2 p, int button)
        {
            pointer = p;
            if (button == 0 && Phase < 3 && Vector2.Distance(p, Point) < (Phase == 0 ? 55 : 85)) Dragging = true;
        }
        public override void PointerMove(Vector2 p)
        {
            pointer = p;
            if (!Dragging || Phase >= 3) return;
            Vector2 next = new Vector2(Mathf.Clamp(p.x, 260, 1040), Mathf.Clamp(p.y, 300, 655));
            if (Phase == 0) corner = next; else if (Phase == 1) tail = next; else tight = next;
            if (Phase == 0 && Vector2.Distance(corner, Target) < 42) { corner = Target; Advance(); }
            else if (Phase == 1 && Vector2.Distance(tail, Target) < 90) { tail = Target; Advance(); }
            // Validate the final pull in Tick, after the current frame's keys
            // have been read. PointerMove arrives before Tick in WBMain.
        }
        public override void PointerUp(Vector2 p) { pointer = p; Dragging = false; }
        public override void Draw()
        {
            MemoryArt.Background(true);
            G.Box(new Rect(185, 284, 910, 354), G.C("#78614e"));
            G.Box(new Rect(205, 304, 870, 310), G.C("#806852"));
            G.Line(new Vector2(220, 493), new Vector2(1060, 493), new Color(.62f, .51f, .39f, .2f), 2);
            if (Phase < 2)
            {
                MemoryArt.Scarf(new[] { new Vector2(385, 427), new Vector2(540, 427), new Vector2(730, 430), tail }, 58);
                if (Phase == 0)
                {
                    MemoryArt.Stroke(corner, new Vector2(405, 450), G.C("#8c4242"), 36);
                    G.Line(new Vector2(402, 452), new Vector2(469, 403), G.C("#d58d75"), 2);
                    G.Line(new Vector2(410, 420), Target, G.C("#bba58a"), 2);
                }
                else
                {
                    G.Line(new Vector2(920, 431), new Vector2(840, 500), G.C("#baa084"), 3);
                    G.Line(new Vector2(840, 500), new Vector2(580, 448), G.C("#baa084"), 3);
                }
            }
            else if (Phase == 2)
            {
                MemoryArt.Scarf(new[] { new Vector2(390, 443), new Vector2(497, 443), new Vector2(580, 450), new Vector2(620, 418), new Vector2(663, 453), tight }, 54);
                MemoryArt.Scarf(new[] { new Vector2(580, 450), new Vector2(590, 505), new Vector2(545, 560) }, 46, "#8e4446");
                MemoryArt.Hand(new Vector2(495, 440), 0, LeftHeld ? "#d4ac90" : "#9d8874", 1.1f);
                MemoryArt.Hand(new Vector2(660, 615), -90, "#9d8874", 1.1f);
                Vector2[] keys = { new Vector2(410, 650), new Vector2(444, 650), new Vector2(478, 650), new Vector2(444, 616) };
                Vector2[] dirs = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
                for (int i = 0; i < keys.Length; i++)
                {
                    G.Box(new Rect(keys[i].x, keys[i].y, 27, 25), G.C(LeftHeld ? "#cdb18b" : "#8d7a67"));
                    Vector2 center = keys[i] + new Vector2(13, 12), d = dirs[i], n = new Vector2(-d.y, d.x);
                    G.Line(center - d * 3 + n * 4, center + d * 3, G.C("#392f2a"), 2);
                    G.Line(center - d * 3 - n * 4, center + d * 3, G.C("#392f2a"), 2);
                }
                G.Ring(new Vector2(497, 443), 31 + Mathf.Sin(time * 4) * 2, 1, G.C(LeftHeld ? "#e2c18b" : "#9d8874"), 2);
            }
            else
            {
                float settle = Mathf.Clamp01(finishTime / 1.5f);
                MemoryArt.Scarf(new[] { new Vector2(410, 430), new Vector2(550, 441), new Vector2(615, 422), new Vector2(660, 465), new Vector2(700, 570) }, 53);
                MemoryArt.Scarf(new[] { new Vector2(610, 447), new Vector2(580, 515), new Vector2(548, 565) }, 45, "#8e4446");
                MemoryArt.Hand(new Vector2(553 + settle * 28, 451), -5, "#d4ac90", 1.25f);
                MemoryArt.Hand(new Vector2(710 - settle * 25, 463), 185, "#b38870", 1.25f);
                G.Box(new Rect(0, 90, 1280, 670), new Color(.96f, .8f, .54f, .1f * settle));
            }
            if (Phase < 3)
            {
                MemoryArt.Guide(Point, Target, time, Phase == 1 ? 65 : 29);
                if (Dragging) MemoryArt.Hand(pointer + new Vector2(-12, 12), -32, "#d4ac90", .8f);
            }
        }
    }
}
