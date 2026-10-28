using System;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    // Rules are separate from the two 3D views so keyboard/mouse and recovery can be checked.
    public sealed class WritingChapter : Chapter
    {
        public static readonly Vector2[] ItemPositions =
        {
            new Vector2(-1.9f,-.35f), new Vector2(-.8f,2.15f),
            new Vector2(1.95f,.8f), new Vector2(1.8f,-1.65f)
        };
        public static readonly Vector2 Foyer = new Vector2(0,-2.3f);
        public static readonly Rect Paper = new Rect(65,150,535,445);
        public static readonly Rect Bin = new Rect(480,605,130,125);
        public const float Duration = 60, Speed = 1.95f;
        public string Phase { get; private set; } = "tutorial";
        public Vector2 Player { get; private set; } = new Vector2(0,-1.9f);
        public int Draft { get; private set; } = 1;
        public int CollectedMask { get; private set; }
        public int Row { get; private set; }
        public int PageSerial { get; private set; }
        public float LineExtent { get; private set; }
        public float WritingProgress { get; private set; }
        public float Remaining { get; private set; } = Duration;
        public float FarewellProgress { get; private set; }
        public bool TimerStarted { get; private set; }
        public bool SuccessLatched { get; private set; }
        public bool AwaitingReturn { get; private set; }
        public bool PaperDragging { get; private set; }
        public bool InkFresh => clock-lastInk<=.22f || clock<lineGraceUntil;
        public bool AllCollected => CollectedMask==15;
        public bool Tutorial => Phase=="tutorial";
        public bool HasItem(int index) => (CollectedMask & (1<<index))!=0;
        public override string Help => "左侧：按住鼠标在纸上向右运笔，写满一行后回到左边继续。右侧：WASD 行走，持续书写时靠近物品按 E 拾取。第一稿拾起吊坠后揉纸扔入纸篓；正式稿首次落笔开始60秒，收吊坠、花、信和行李到玄关，再边写边按住 E 送别。停笔不会暂停时钟，超时揉纸重试。";
        public override string DebugState => JsonUtility.ToJson(new Snapshot(this));
        [Serializable] sealed class Snapshot
        {
            public string phase; public int draft, collected; public float seconds, ink;
            public bool started, latched, complete; public Vector2 player;
            public Snapshot(WritingChapter s)
            { phase=s.Phase;draft=s.Draft;collected=s.CollectedMask;seconds=s.Remaining;ink=s.WritingProgress;started=s.TimerStarted;latched=s.SuccessLatched;complete=s.Completed;player=s.Player; }
        }
        float clock,lastInk=-100,lineGraceUntil=-100;
        bool pressed,havePointer,eHeld; Vector2 pointer;
        static readonly Rect[] Obstacles =
        {
            new Rect(-2.6f,.9f,1.25f,1.65f), new Rect(1.25f,1.35f,1.3f,1.15f),
            new Rect(-.50f,-.1f,1,.65f)
        };
        public override void PointerDown(Vector2 p,int button)
        {
            if(button!=0||Completed)return;
            if(Phase=="crumple" && Paper.Contains(p)) { PaperDragging=true;return; }
            if(!Paper.Contains(p))return;
            pressed=true;pointer=p;havePointer=true;
            if(AwaitingReturn&&p.x<180)NextRow();
        }
        void NextRow()
        {
            Row++; if(Row>=9){Row=0;PageSerial++;}
            LineExtent=0;AwaitingReturn=false;lineGraceUntil=clock+.4f;
        }
        public override void PointerMove(Vector2 p)
        {
            if(!havePointer){pointer=p;havePointer=true;return;}
            Vector2 previous=pointer;pointer=p;
            if(!pressed||Phase=="crumple"||Completed||!Paper.Contains(p))return;
            if(AwaitingReturn){if(p.x<180)NextRow();return;}
            float amount=Mathf.Clamp(p.x-previous.x,0,425-LineExtent);
            if(amount<.08f)return;
            LineExtent+=amount;WritingProgress+=amount;lastInk=clock;
            if(Phase=="collecting")TimerStarted=true;
            if(LineExtent>=424.9f){AwaitingReturn=true;lineGraceUntil=clock+.4f;}
        }
        public override void PointerUp(Vector2 p)
        {
            pressed=false;
            if(PaperDragging && Bin.Contains(p))BeginFormalDraft();
            PaperDragging=false;
        }
        public void SuspendInput()
        {pressed=false;PaperDragging=false;lastInk=-100;lineGraceUntil=-100;eHeld=false;}
        void BeginFormalDraft()
        {
            Draft++;Phase="collecting";Player=new Vector2(0,-1.9f);CollectedMask=0;
            Remaining=Duration;TimerStarted=false;SuccessLatched=false;FarewellProgress=0;
            Row=0;LineExtent=0;PageSerial++;AwaitingReturn=false;lastInk=-100;lineGraceUntil=-100;
            pressed=false;eHeld=false;
            WBMain.Tone(230,.12f);
        }
        bool Clear(Vector2 p)
        {
            foreach(Rect rect in Obstacles)
                if(new Rect(rect.xMin-.17f,rect.yMin-.17f,rect.width+.34f,rect.height+.34f).Contains(p))return false;
            return true;
        }
        public override void Tick(float dt,WBInput input)
        {
            dt=Mathf.Clamp(dt,0,.1f);clock+=dt;
            bool e=input.Keys.Contains(KeyCode.E), ePressed=e&&!eHeld;eHeld=e;
            if(Completed||Phase=="crumple")return;
            if(Phase=="farewell")
            {
                if(InkFresh&&e)FarewellProgress+=dt;
                else FarewellProgress=Mathf.Max(0,FarewellProgress-dt*.5f);
                if(FarewellProgress>=1.2f){Phase="complete";Finish();}
                return;
            }
            Vector2 direction=new Vector2(
                (input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0),
                (input.Keys.Contains(KeyCode.W)?1:0)-(input.Keys.Contains(KeyCode.S)?1:0)).normalized;
            Vector2 delta=direction*Speed*dt;
            Vector2 next=new Vector2(Mathf.Clamp(Player.x+delta.x,-2.5f,2.5f),Player.y);
            if(Clear(next))Player=next;
            next=new Vector2(Player.x,Mathf.Clamp(Player.y+delta.y,-2.5f,2.5f));
            if(Clear(next))Player=next;
            if(Phase=="collecting"&&TimerStarted)
            {
                Remaining=Mathf.Max(0,Remaining-dt);
                if(Remaining<=0){Phase="crumple";pressed=false;lastInk=-100;return;}
            }
            if(ePressed&&InkFresh)
            {
                for(int i=0;i<4;i++)
                {
                    if(Tutorial&&i!=0||HasItem(i)||Vector2.Distance(Player,ItemPositions[i])>.62f)continue;
                    CollectedMask|=1<<i;WBMain.Tone(330+i*55,.09f);
                    if(Tutorial){Phase="crumple";pressed=false;lastInk=-100;}
                    break;
                }
            }
            if(Phase=="collecting"&&TimerStarted&&AllCollected&&Vector2.Distance(Player,Foyer)<.66f)
            {SuccessLatched=true;Phase="farewell";WBMain.Tone(440,.2f);}
        }
        public override void Draw() {}
    }
}
