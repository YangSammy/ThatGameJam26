using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public class WritingChapter : Chapter
    {
        public string Phase = "door";
        public Vector2 Player = new Vector2(160, 505);
        public Vector2 Npc = new Vector2(185, 395);
        public float WritingProgress;
        public bool Carried;
        public bool DoorHeld;
        public bool DoorOpen;
        public int Draft = 1;
        public bool InkFresh { get { return clock - lastInk < 2; } }
        public bool AwaitingReturn;
        public int Row;
        public override string Help { get { return "右手：在纸上按住鼠标向右写，行满移回左端继续；只有真实书写会让回忆人物行动。\n左手：WASD 移动，第一稿在门把手附近按住 E 扶门，同时继续书写。\n卡稿：把纸拖到右下纸篓。第二稿靠近座位拾围巾，经门洞走到窗边的人面前，同时书写交付。"; } }
        public override string DebugState { get { return JsonUtility.ToJson(new State(this)); } }
        readonly Rect paper = new Rect(685, 165, 535, 445);
        readonly List<float> rows = new List<float> { 0 };
        float clock, lastInk = -100, handover, flash;
        bool pressed, havePointer, dragging;
        Vector2 pointer, dragPoint;

        [System.Serializable]
        class State
        {
            public string phase;
            public int draft, row;
            public float writingProgress;
            public bool inkFresh, carried, completed, doorHeld, doorOpen, awaitingReturn;
            public Vector2 player, npc;
            public State(WritingChapter s)
            {
                phase = s.Phase; draft = s.Draft; row = s.Row;
                writingProgress = s.WritingProgress; inkFresh = s.InkFresh;
                carried = s.Carried; completed = s.Completed; player = s.Player; npc = s.Npc;
                doorHeld = s.DoorHeld; doorOpen = s.DoorOpen; awaitingReturn = s.AwaitingReturn;
            }
        }
        static Color C(string hex) { return G.C(hex); }
        static Color A(string hex, float alpha) { Color c = C(hex); c.a = alpha; return c; }
        static void L(float x, float y, float xx, float yy, Color color, float width = 2)
        { G.Line(new Vector2(x, y), new Vector2(xx, yy), color, width); }
        static void B(float x, float y, float w, float h, string color)
        { G.Box(new Rect(x, y, w, h), C(color)); }
        static void Dot(float x, float y, float r, Color color)
        { G.Circle(new Vector2(x, y), r, color); }
        static bool Has(WBInput input, KeyCode key) { return input.Keys != null && input.Keys.Contains(key); }

        public override void PointerDown(Vector2 p, int button)
        {
            if (button != 0) return;
            pressed = true; pointer = p; havePointer = true;
            if (Phase == "crumple" && paper.Contains(p)) { dragging = true; dragPoint = p; }
            else if (AwaitingReturn && paper.Contains(p) && p.x < 820) NextRow();
        }
        void NextRow()
        {
            Row++; rows.Add(0); AwaitingReturn = false;
            if (rows.Count > 9) { rows.RemoveAt(0); Row--; }
        }
        public override void PointerMove(Vector2 p)
        {
            if (dragging) { dragPoint = p; return; }
            Vector2 previous = pointer; bool had = havePointer;
            pointer = p; havePointer = true;
            if (!pressed || !had || !paper.Contains(p) || Phase == "crumple" || Completed) return;
            if (AwaitingReturn) { if (p.x < 820) NextRow(); return; }
            float dx = p.x - previous.x;
            if (dx < 0.08f) return;
            float amount = Mathf.Min(dx, 425 - rows[Row]);
            if (amount <= 0) return;
            rows[Row] += amount; WritingProgress += amount; lastInk = clock;
            if (rows[Row] >= 425) AwaitingReturn = true;
        }
        public override void PointerUp(Vector2 p)
        {
            pressed = false;
            if (!dragging) return;
            if (p.x > 1060 && p.y > 555 && p.x < 1260 && p.y < 705) SecondDraft();
            dragging = false;
        }
        void SecondDraft()
        {
            Phase = "return"; Draft = 2; rows.Clear(); rows.Add(0); Row = 0;
            AwaitingReturn = false; lastInk = -100; DoorOpen = true;
            Player = new Vector2(145, 510); Npc = new Vector2(472, 395);
            WBMain.Tone(260, 0.15f); flash = 0.7f;
        }
        public override void Tick(float dt, WBInput input)
        {
            dt = Mathf.Clamp(dt, 0, 0.1f); clock += dt; flash = Mathf.Max(0, flash - dt);
            float dx = (Has(input, KeyCode.D) || Has(input, KeyCode.RightArrow) ? 1 : 0) - (Has(input, KeyCode.A) || Has(input, KeyCode.LeftArrow) ? 1 : 0);
            float dy = (Has(input, KeyCode.S) || Has(input, KeyCode.DownArrow) ? 1 : 0) - (Has(input, KeyCode.W) || Has(input, KeyCode.UpArrow) ? 1 : 0);
            if (Phase == "crumple" || Completed) { dx = 0; dy = 0; }
            DoorHeld = Phase == "door" && Vector2.Distance(Player, new Vector2(326, 431)) < 70 && (Has(input, KeyCode.E) || Has(input, KeyCode.D));
            if (DoorHeld && dx > 0) dx = 0;
            Vector2 movement = new Vector2(dx, dy).normalized * (165 * dt);
            float nx = Mathf.Clamp(Player.x + movement.x, 82, 577);
            float ny = Mathf.Clamp(Player.y + movement.y, 202, 610);
            bool passage = ny > 353 && ny < 436 && (DoorOpen || (InkFresh && DoorHeld));
            if (!(nx > 332 && nx < 368 && !passage)) Player.x = nx;
            if (!(Player.x > 332 && Player.x < 368 && !passage)) Player.y = ny;
            if (Phase == "door" && InkFresh)
            {
                if (Npc.x < 302 || DoorHeld || Npc.x > 385) Npc.x += dt * 46;
                if (Npc.x >= 470)
                {
                    Npc.x = 470; DoorOpen = true; Phase = "crumple";
                    while (rows.Count > 5) rows.RemoveAt(0);
                    Row = rows.Count - 1; rows[Row] = Mathf.Min(rows[Row], 220);
                    WBMain.Tone(160, 0.15f); flash = 0.7f;
                }
            }
            if (Phase != "return") return;
            if (!Carried && Vector2.Distance(Player, new Vector2(190, 485)) < 43)
            { Carried = true; WBMain.Tone(450, 0.1f); }
            if (InkFresh) Npc.y = Mathf.Max(274, Npc.y - dt * 40);
            if (Carried && InkFresh && Npc.y < 292 && Vector2.Distance(Player, Npc) < 62)
            {
                handover += dt;
                if (handover >= 0.7f && !Completed)
                { Phase = "complete"; WBMain.Tone(660, 0.3f); Finish(); }
            }
        }
        static void Person(Vector2 p, Color color, bool small)
        {
            float s = small ? 0.85f : 1;
            Dot(p.x, p.y - 15 * s, 12 * s, color);
            G.Box(new Rect(p.x - 13 * s, p.y - s, 26 * s, 25 * s), color);
            L(p.x - 7 * s, p.y + 20 * s, p.x - 9 * s, p.y + 35 * s, color, 6);
            L(p.x + 7 * s, p.y + 20 * s, p.x + 9 * s, p.y + 35 * s, color, 6);
        }
        static void Ink(float x, float y, float extent)
        {
            Vector2 previous = new Vector2(x, y);
            for (float i = 3; i <= extent; i += 3)
            {
                float wave = Mathf.Sin(i * 0.26f) * (3 + Mathf.Sin(i * 0.063f) * 2) + Mathf.Sin(i * 0.67f) * 2;
                Vector2 next = new Vector2(x + i, y + wave);
                G.Line(previous, next, C("#423d3e"), 1.8f); previous = next;
            }
        }
        public override void Draw()
        {
            B(45, 140, 575, 520, "#171e27"); B(67, 190, 530, 438, "#303b44");
            B(75, 195, 265, 425, "#756f62"); B(361, 195, 228, 425, "#615f58");
            for (int y = 213; y < 620; y += 35) L(77, y, 588, y, A("#8e8573", 0.25f), 1);
            B(338, 195, 24, 154, "#b8ae97"); B(338, 442, 24, 180, "#b8ae97");
            if (!DoorOpen)
            {
                if (DoorHeld && InkFresh) L(350, 350, 395, 340, C("#dcc8a0"), 9);
                else L(350, 350, 350, 442, C("#a59070"), 9);
            }
            B(423, 205, 110, 57, "#343b42"); B(430, 210, 96, 45, "#b6cad0");
            L(478, 211, 478, 254, C("#666e72"), 4); L(431, 233, 525, 233, C("#666e72"), 3);
            B(150, 456, 82, 54, "#4f5053");
            L(157, 508, 157, 528, C("#30363c"), 5); L(225, 508, 225, 528, C("#30363c"), 5);
            if (Phase == "return" && !Carried)
            {
                Dot(190, 479, 28 + Mathf.Sin(clock * 4) * 3, A("#eea474", 0.15f));
                L(169, 476, 206, 476, C("#dd765c"), 9); L(203, 476, 203, 499, C("#dd765c"), 8);
            }
            if (Phase == "door")
            {
                B(Npc.x - 10, Npc.y + 18, 30, 28, "#a18b69");
                L(Npc.x - 4, Npc.y + 18, Npc.x + 10, Npc.y + 18, C("#c8b48c"), 3);
                Dot(324, 430, 19 + Mathf.Sin(clock * 4) * 3, A("#dfc68b", DoorHeld ? 0.44f : 0.16f));
                L(322, 418, 322, 438, C("#f0dbaa"), 4);
            }
            Person(Npc, C("#d6bea0"), false); Person(Player, C("#8ac4c3"), true);
            if (Carried)
            {
                Vector2 p = Completed ? Npc : Player;
                L(p.x - 11, p.y - 6, p.x + 12, p.y - 6, C("#e57d64"), 7);
                L(p.x + 9, p.y - 5, p.x + 9, p.y + 15, C("#e57d64"), 6);
            }
            if (Phase == "return" && Carried)
            {
                G.Ring(new Vector2(478, 233), 47, 1, A("#edbc80", 0.35f + Mathf.Sin(clock * 3) * 0.1f), 2);
                L(Npc.x - 12, Npc.y + 6, Npc.x - 33, Npc.y + 2, C("#d6bea0"), 6);
                Dot(Npc.x - 34, Npc.y + 2, 5, C("#e3c9a7"));
            }
            if (Completed) L(Npc.x - 12, Npc.y + 4, Npc.x - 30, Npc.y - 15, C("#d6bea0"), 6);
            if (!InkFresh && !Completed) G.Box(new Rect(67, 190, 530, 438), A("#171e27", 0.25f));
            B(655, 140, 580, 520, "#282c32");
            if (!dragging)
            {
                B(paper.x + 6, paper.y + 7, paper.width, paper.height, "#c7bdac");
                G.Box(paper, C("#eee5cf"));
                for (int i = 0; i < rows.Count; i++) Ink(732, 215 + i * 38, rows[i]);
                float end = 732 + rows[Row], yy = 215 + Row * 38;
                L(end + 2, yy, end + 36, yy - 53, C("#3e4650"), 7);
                Dot(end + 29, yy - 33, 14, C("#c6a98d"));
                if (Phase == "crumple")
                {
                    Dot(1196, 590, 26 + Mathf.Sin(clock * 4) * 3, A("#da9876", 0.25f));
                    L(1180, 610, 1220, 570, C("#b6a688"), 3);
                    L(end - 8, yy - 10, end + 7, yy + 9, C("#9b5a50"), 2);
                    L(1194, 598, 1186, 616, C("#99694f"), 3);
                    L(1180, 608, 1186, 616, C("#99694f"), 3);
                    L(1186, 616, 1194, 612, C("#99694f"), 3);
                }
                else if (!Completed)
                {
                    L(757, 631, 1120, 631, A("#ded2b4", 0.45f), 2);
                    float head = AwaitingReturn ? 757 : 1120;
                    float tail = AwaitingReturn ? 775 : 1104;
                    L(tail, 623, head, 631, C("#ded2b4")); L(head, 631, tail, 639, C("#ded2b4"));
                    Dot(710, 631, 9, C(InkFresh ? "#ddb77c" : "#5c6266"));
                }
            }
            else
            {
                Dot(dragPoint.x, dragPoint.y, 27, C("#eee5cf"));
                L(dragPoint.x - 14, dragPoint.y - 13, dragPoint.x + 12, dragPoint.y + 17, C("#b8aa90"));
                L(dragPoint.x + 15, dragPoint.y - 12, dragPoint.x - 13, dragPoint.y + 9, C("#b8aa90"));
            }
            Color bin = C(Phase == "crumple" ? "#e2b779" : "#797c7c");
            G.Box(new Rect(1163, 622, 45, 34), Color.clear, bin, 3); L(1159, 620, 1212, 620, bin, 4);
            for (int x = 1173; x < 1206; x += 12) L(x, 628, x, 650, C("#797c7c"));
            if (Completed) { L(939, 557, 955, 571, C("#99cdb6"), 5); L(955, 571, 984, 540, C("#99cdb6"), 5); }
            if (flash > 0) G.Box(new Rect(45, 140, 1188, 520), A("#f4ddb6", flash * 0.25f));
        }
    }
}
