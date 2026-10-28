using System;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    public sealed class PaintingChapter : Chapter
    {
        public const float FocusSeconds=1.25f, RunSpeed=3.4f, JumpSpeed=4.8f;
        public readonly bool[] Painted=new bool[5]; // swing, bridge, luggage, flower, pendant
        public int Act { get; private set; }
        public string Stage { get; private set; }="park";
        public bool BoardOpen { get; private set; }
        public bool PendantDragging { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool OnSwing { get; private set; }
        public bool OnFlower { get; private set; }
        public bool Grounded { get; private set; }=true;
        public bool GateOpen => Painted[2]&&LuggageX>=4.62f&&LuggageX<=5.35f;
        public bool NoseThresholdReached { get; private set; }
        public bool MemoryAbruptCut { get; private set; }
        public Vector2 Position { get; private set; }=new Vector2(.7f,0);
        public Vector2 Velocity { get; private set; }
        public float SwingAngle { get; private set; }=-.56f;
        public float SwingSpeed { get; private set; }
        public float FlowerAngle { get; private set; }=-Mathf.PI/2;
        public float LuggageX { get; private set; }=3.2f;
        public float FocusProgress { get; private set; }
        public int GazeIndex { get; private set; }=-1;
        public float WhiteAmount { get; private set; }
        public float LineProgress { get; private set; }
        public Vector2 SwingSeat => new Vector2(4.9f+Mathf.Sin(SwingAngle)*3,3.6f-Mathf.Cos(SwingAngle)*3);
        public Vector2 FlowerSeat => new Vector2(9.15f+Mathf.Cos(FlowerAngle)*1.65f,1.9f+Mathf.Sin(FlowerAngle)*1.65f);
        public Vector2 Checkpoint { get; private set; }=new Vector2(.7f,0);
        public override string Help => "现实中固定站位，鼠标转头；按住左键使虚焦变清晰，凝视物件约1.25秒把它画入书中。鼠标到下边缘举书，上边缘放书；书内 A/D 移动、Space 跳。公园坐上秋千后 A/D 荡过缺口，桥是普通桥。空房推动行李压住踏板，乘花轮升上高台，寻找实景隐藏吊坠，再把书缘吊坠拖入门上的同形凹槽。放书会暂停画中运动。最后沿纸上的直线向右走。";
        public override string DebugState => JsonUtility.ToJson(new Snapshot(this));
        [Serializable] sealed class Snapshot
        {
            public int act;public string stage;public bool book,key,gate,onSwing,onFlower,cut,complete;public Vector2 player;public float focus,line;
            public Snapshot(PaintingChapter s){act=s.Act;stage=s.Stage;book=s.BoardOpen;key=s.KeyInserted;gate=s.GateOpen;onSwing=s.OnSwing;onFlower=s.OnFlower;cut=s.MemoryAbruptCut;complete=s.Completed;player=s.Position;focus=s.FocusProgress;line=s.LineProgress;}
        }
        bool focusHeld,jumpHeld;float transition,rideCooldown; int pendingAct;
        public void SetBoardOpen(bool value)
        {
            if(Stage=="line")value=true;
            if(Stage=="park"||Stage=="room"||Stage=="line")BoardOpen=value;
            if(value){GazeIndex=-1;FocusProgress=0;focusHeld=false;}
        }
        public void SetGaze(int index,bool held)
        {
            bool legal=Act==0?index>=0&&index<2:index>=2&&index<5;
            if(BoardOpen||!legal||Stage!="park"&&Stage!="room")index=-1;
            if(index!=GazeIndex)FocusProgress=0;
            GazeIndex=index;focusHeld=held;
        }
        public bool BeginPendantDrag()
        {
            if(Act!=1||!BoardOpen||!Painted[4]||KeyInserted||Completed)return false;
            PendantDragging=true;return true;
        }
        public bool EndPendantDrag(bool hitSocket)
        {
            if(!PendantDragging)return false;
            PendantDragging=false;
            if(hitSocket&&Position.x>=12.2f&&Position.y>=2.55f)
            {KeyInserted=true;WBMain.Tone(660,.18f);return true;}
            WBMain.Tone(170,.08f);return false;
        }
        public void SuspendInput()
        {focusHeld=false;GazeIndex=-1;FocusProgress=0;PendantDragging=false;jumpHeld=true;}
        public override void Tick(float dt,WBInput input)
        {
            dt=Mathf.Clamp(dt,0,.05f);if(Completed)return;
            bool jump=input.Keys.Contains(KeyCode.Space),newJump=jump&&!jumpHeld;jumpHeld=jump;
            if(Stage=="fade-out")
            {
                transition+=dt;WhiteAmount=Mathf.Clamp01(transition/.65f);
                if(WhiteAmount>=1)
                {
                    Act=pendingAct;transition=0;Stage="fade-in";ResetRunner();BoardOpen=false;
                }
                return;
            }
            if(Stage=="fade-in")
            {transition+=dt;WhiteAmount=1-Mathf.Clamp01(transition/.65f);if(transition>=.65f){Stage="room";WhiteAmount=0;}return;}
            if(Stage=="white")
            {
                transition+=dt;WhiteAmount=Mathf.Clamp01(transition/.8f);
                if(transition>=.8f){Act=2;Stage="line";BoardOpen=true;Position=new Vector2(0,0);Velocity=Vector2.zero;WhiteAmount=1;}
                return;
            }
            float axis=(input.Keys.Contains(KeyCode.D)||input.Keys.Contains(KeyCode.RightArrow)?1:0)-(input.Keys.Contains(KeyCode.A)||input.Keys.Contains(KeyCode.LeftArrow)?1:0);
            if(Stage=="line")
            {
                // This remembered line can only unfold forward; there is no hospital puzzle.
                LineProgress=Mathf.Clamp01(LineProgress+Mathf.Max(0,axis)*dt*.085f);Position=new Vector2(LineProgress*18,0);
                return; // world compares its actual nose marker against the reveal edge
            }
            if(!BoardOpen)
            {
                if(focusHeld&&GazeIndex>=0&&!Painted[GazeIndex])
                {
                    FocusProgress+=dt;
                    if(FocusProgress>=FocusSeconds){Painted[GazeIndex]=true;FocusProgress=0;WBMain.Tone(440+GazeIndex*55,.12f);}
                }
                else FocusProgress=0;
                return;
            }
            if(PendantDragging)return;
            rideCooldown=Mathf.Max(0,rideCooldown-dt);
            if(Act==0)Swing(dt,axis);
            if(Act==1&&Painted[3])FlowerAngle+=dt*.62f;
            if(OnSwing)
            {
                Position=SwingSeat;Velocity=Vector2.zero;Grounded=false;
                if(newJump)
                {
                    OnSwing=false;rideCooldown=.55f;Velocity=new Vector2(Mathf.Cos(SwingAngle)*3*SwingSpeed+axis*2.8f,Mathf.Sin(SwingAngle)*3*SwingSpeed+4.7f);
                }
                else return;
            }
            if(OnFlower)
            {
                Position=FlowerSeat;Velocity=Vector2.zero;Grounded=false;
                if(newJump){OnFlower=false;rideCooldown=.55f;Velocity=new Vector2(axis*RunSpeed,JumpSpeed);}
                else return;
            }
            if(Act==1&&Painted[2]&&Grounded&&Mathf.Abs(Position.x-LuggageX)<.9f&&Position.y<.2f&&axis!=0)
            {
                if(axis>0&&Position.x<LuggageX||axis<0&&Position.x>LuggageX)LuggageX=Mathf.Clamp(LuggageX+axis*RunSpeed*dt,1.2f,5.05f);
            }
            if(Grounded&&newJump){Velocity=new Vector2(Velocity.x,JumpSpeed);Grounded=false;}
            Vector2 old=Position;
            float horizontal=axis*RunSpeed;
            // Carry the swing's release momentum briefly instead of forcing a zero-input stop.
            if(!Grounded&&Mathf.Abs(Velocity.x)>RunSpeed)horizontal=Mathf.Lerp(Velocity.x,horizontal,dt*2);
            Velocity=new Vector2(horizontal,Velocity.y-9.8f*dt);
            Vector2 next=Position+Velocity*dt;next.x=Mathf.Clamp(next.x,.2f,18.8f);
            if(Act==1)
            {
                if(Painted[2]&&next.y<.82f)
                {
                    if(old.x<=LuggageX-.55f&&next.x>LuggageX-.55f)next.x=LuggageX-.55f;
                    if(old.x>=LuggageX+.55f&&next.x<LuggageX+.55f)next.x=LuggageX+.55f;
                }
                if(!GateOpen&&old.x<6.08f&&next.x>=6.08f&&next.y<2.45f)next.x=6.07f;
                if(!GateOpen&&old.x>6.32f&&next.x<=6.32f&&next.y<2.45f)next.x=6.33f;
                if(!KeyInserted&&old.x<14.25f&&next.x>=14.25f&&next.y<5.2f)next.x=14.24f;
            }
            Grounded=false;
            if(Velocity.y<=0)
            {
                if(Act==0)
                {
                    Land(ref next,old,0,3.25f,0);Land(ref next,old,7.2f,10.8f,0);Land(ref next,old,15,19,0);
                    if(Painted[1])Land(ref next,old,10.75f,15.1f,0);
                }
                else
                {
                    Land(ref next,old,0,10.15f,0);Land(ref next,old,10.3f,19,2.8f);
                    if(Painted[2])Land(ref next,old,LuggageX-.48f,LuggageX+.48f,.85f);
                }
            }
            Position=next;
            if(Act==0&&Painted[0]&&!newJump&&rideCooldown<=0&&Vector2.Distance(Position,SwingSeat)<.5f)
            {OnSwing=true;Grounded=false;Position=SwingSeat;}
            if(Act==1&&Painted[3]&&!newJump&&rideCooldown<=0&&Vector2.Distance(Position,FlowerSeat)<.46f)
            {OnFlower=true;Grounded=false;Position=FlowerSeat;}
            if(Position.y< -3){Position=Checkpoint;Velocity=Vector2.zero;Grounded=true;OnSwing=OnFlower=false;WBMain.Tone(200,.07f);}
            if(Act==0&&Position.x>7.4f&&Position.x<10.3f&&Grounded)Checkpoint=new Vector2(7.7f,0);
            if(Act==1&&Position.x>7&&Position.x<9&&Grounded)Checkpoint=new Vector2(7.5f,0);
            if(Act==1&&Position.x>10.7f&&Position.y>=2.79f&&Grounded)Checkpoint=new Vector2(11,2.8f);
            if(Act==0&&Position.x>=18.25f&&Grounded&&Painted[0]&&Painted[1])
            {Stage="fade-out";transition=0;pendingAct=1;BoardOpen=false;}
            if(Act==1&&Position.x>=17.9f&&Position.y>=2.79f&&KeyInserted)
            {Stage="white";transition=0;BoardOpen=true;}
        }
        void Swing(float dt,float axis)
        {
            if(!Painted[0])return;
            SwingSpeed+=(-3.27f*Mathf.Sin(SwingAngle)-SwingSpeed*.28f+(OnSwing?axis*3.7f:0))*dt;
            SwingAngle+=SwingSpeed*dt;
            if(Mathf.Abs(SwingAngle)>1.12f){SwingAngle=Mathf.Sign(SwingAngle)*1.12f;SwingSpeed*=-.2f;}
        }
        void Land(ref Vector2 next,Vector2 previous,float left,float right,float top)
        {
            if(next.x<left||next.x>right||previous.y<top-.02f||next.y>top)return;
            next.y=top;Velocity=new Vector2(Velocity.x,0);Grounded=true;
        }
        void ResetRunner()
        {Position=Checkpoint=new Vector2(.7f,0);Velocity=Vector2.zero;Grounded=true;OnSwing=OnFlower=false;jumpHeld=false;}
        // Called when the reveal edge physically reaches the seated girl's nose marker.
        public void ObserveHospitalReveal(bool noseReached)
        {
            if(Stage!="line"||!noseReached||Completed)return;
            NoseThresholdReached=true;MemoryAbruptCut=true;Stage="cut";WhiteAmount=1;Finish();
        }
        public override void Draw(){}
    }
}
