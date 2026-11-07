using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    public class WritingChapter:Chapter
    {
        public string Phase="tutorial";
        public Vector2 Player=new Vector2(330,450),Npc=new Vector2(315,570);
        public float WritingProgress,Remaining=60;
        public int Draft=1,Row,Collected;
        public bool Started,AwaitingReturn;
        public bool MissingItemHintUsed { get; private set; }
        public bool TimedOut => Draft>=2 && Phase=="crumple" && Remaining<=0;
        public bool Carried=>Collected!=0;
        public bool DoorHeld=>Phase=="farewell";
        public bool DoorOpen=>true;
        public bool InkFresh=>pressed && inkThisTick;
        public const float WalkSpeed=33f;
        public const float PickupBufferSeconds=.22f;
        bool inkPending,inkThisTick,eWasHeld;
        int bufferedItem=-1;
        float pickupExpires=-1;
        public bool PickupPrepared=>bufferedItem>=0 && clock<=pickupExpires && InPickupRange(bufferedItem);
        public float FarewellProgress=>handover;
        public float FarewellFill=>Mathf.Clamp01(handover/Mathf.Max(.001f,NarrativeDefinition.Resolve(this)?.farewellHold??1.2f));
        public int PromptItem
        {
            get
            {
                if(Phase!="tutorial" && Phase!="collect")return -1;
                for(int i=0;i<ItemPositions.Length;i++)if(InPickupRange(i))return i;
                return -1;
            }
        }
        public const float WaveSeconds=2.3f, TurnSeconds=.65f, WalkOutSeconds=2f;
        public const float FarewellDuration=WaveSeconds+TurnSeconds+WalkOutSeconds;
        public float FarewellElapsed=>farewellTime;
        public float DepartureTurn=>Mathf.Clamp01((farewellTime-WaveSeconds)/TurnSeconds);
        public float DepartureWalk=>farewellTime>=FarewellDuration?1:Mathf.Clamp01((farewellTime-WaveSeconds-TurnSeconds)/WalkOutSeconds);
        public readonly Vector2[] ItemPositions={new Vector2(200,320),new Vector2(330,240),new Vector2(485,505),new Vector2(485,360)};
        public static readonly string[] ItemIds={"pendant","flower","luggage","letter"};
        public bool HasItem(int i)=>(Collected&(1<<i))!=0;
        readonly Rect paper=new Rect(685,165,535,445);
        readonly List<float> rows=new List<float>{0};
        float clock,handover,farewellTime;
        bool pressed,havePointer,dragging;
        Vector2 pointer;
        public override string Help=>"LMB drag  Write / WASD  Move / E while writing  Pick up\nCrumple the stalled draft into the bin. The formal draft clock keeps running when you stop writing.";
        public IReadOnlyList<float> InkRows=>rows;
        public bool InPickupRange(int i)=>!HasItem(i) && Vector2.Distance(Player,ItemPositions[i])<46;
        public bool NearPickup=>InPickupRange(0)||InPickupRange(1)||InPickupRange(2)||InPickupRange(3);
        string TutorialHint=>NearPickup?"Keep writing on the left paper":WritingProgress<=0?"Drag right across the letter":"WASD  Approach an object";
        public string TaskHint=>Phase=="crumple"?"Drag paper to the bin":Phase=="tutorial"?TutorialHint:Phase=="arrival"?FarewellHint:Phase=="farewell"?"":Collected==15?"Return to the entrance":"Write + E to collect";
        string FarewellHint=>AwaitingReturn?"Return to the left edge of the paper":"Keep writing on the left paper";
        public override string DebugState=>CaptureCheckpoint();
        [Serializable] sealed class Checkpoint
        {
            public int version=3,draft=1,collected,row;public string phase;
            public bool missingItemHintUsed;
            public bool crumple,started,awaitingReturn;
            public float remaining=60,writingProgress;public Vector2 player;public float[] rows;
        }
        public override string CaptureCheckpoint()=>JsonUtility.ToJson(new Checkpoint{draft=Draft,phase=Phase,collected=Collected,started=Started,remaining=Remaining,player=Player,row=Row,rows=rows.ToArray(),writingProgress=WritingProgress,awaitingReturn=AwaitingReturn,missingItemHintUsed=MissingItemHintUsed});
        public override void RestoreCheckpoint(string json)
        {
            CancelInput();if(string.IsNullOrEmpty(json))return;
            var n=JsonUtility.FromJson<Checkpoint>(json);
            if(n==null || n.version<1 || n.version>3 || n.draft<1)throw new System.IO.InvalidDataException("Invalid writing checkpoint.");
            Draft=n.draft;
            MissingItemHintUsed=false; // Legacy field retained for checkpoint compatibility; cue retired.
            if(n.version==1){Phase=n.crumple?"crumple":Draft>=2?"collect":"tutorial";return;}
            if(Array.IndexOf(new[]{"tutorial","crumple","collect","arrival","farewell","complete"},n.phase)<0 || n.collected<0 || n.collected>15 || !Finite(n.remaining) || n.remaining<0 || !Finite(n.player.x) || !Finite(n.player.y))throw new System.IO.InvalidDataException("Invalid autumn state.");
            if((n.phase=="arrival" || n.phase=="farewell" || n.phase=="complete") && n.collected!=15)throw new System.IO.InvalidDataException("Missing autumn delivery.");
            Phase=n.phase=="farewell"?"arrival":n.phase;Collected=n.collected;Started=n.started;Remaining=n.remaining;
            Player=new Vector2(Mathf.Clamp(n.player.x,82,577),Mathf.Clamp(n.player.y,202,610));
            if(!CanStand(Player))Player=new Vector2(330,450); // Older room checkpoints may now lie inside furniture.
            rows.Clear();if(n.rows!=null && n.rows.Length>0 && n.rows.Length<=9)foreach(var amount in n.rows){if(!Finite(amount))throw new System.IO.InvalidDataException("Invalid ink.");rows.Add(Mathf.Clamp(amount,0,425));}else rows.Add(0);
            Row=Mathf.Clamp(n.row,0,rows.Count-1);WritingProgress=Mathf.Max(0,n.writingProgress);AwaitingReturn=n.awaitingReturn;
            if(Phase=="arrival" && AwaitingReturn)NextRow();
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public void CancelInput(){pressed=havePointer=dragging=false;inkPending=inkThisTick=eWasHeld=false;bufferedItem=-1;pickupExpires=-1;handover=0;}
        public void EditorBeginFarewell()
        {
            if(Completed || Phase=="farewell")return;
            CancelInput();Draft=Mathf.Max(2,Draft);Collected=15;
            Player=new Vector2(300,565);Phase="farewell";farewellTime=0;
        }
        public override void PointerDown(Vector2 p,int button)
        {
            if(button!=0 || Completed)return;pressed=true;pointer=p;havePointer=true;
            if(Phase=="crumple" && paper.Contains(p))dragging=true;
            else if(AwaitingReturn && paper.Contains(p) && p.x<820)NextRow();
        }
        void NextRow(){rows.Add(0);if(rows.Count>9)rows.RemoveAt(0);Row=rows.Count-1;AwaitingReturn=false;}
        public override void PointerMove(Vector2 p)
        {
            var old=pointer;pointer=p;
            if(dragging || !pressed || !havePointer || !paper.Contains(p) || Phase=="crumple" || Phase=="farewell" || Completed)return;
            if(AwaitingReturn){if(p.x<820)NextRow();return;}
            if(!paper.Contains(old))return;
            float amount=Mathf.Min(Mathf.Max(0,p.x-old.x),425-rows[Row]);if(amount<.08f)return;
            rows[Row]+=amount;WritingProgress+=amount;inkPending=true;AwaitingReturn=rows[Row]>=425;
            if(Draft>=2 && Phase=="collect")Started=true;
        }
        public override void PointerUp(Vector2 p)
        {
            pressed=false;
            if(dragging && p.x>1060 && p.x<1260 && p.y>555 && p.y<705)RestartDraft();
            dragging=havePointer=false;
        }
        void RestartDraft()
        {
            Draft=Mathf.Max(2,Draft+1);Phase="collect";Collected=0;Started=false;Remaining=NarrativeDefinition.Resolve(this)?.writingSeconds??60;
            MissingItemHintUsed=false;
            Player=new Vector2(330,450);rows.Clear();rows.Add(0);Row=0;WritingProgress=0;AwaitingReturn=false;CancelInput();
        }
        static bool CanStand(Vector2 p)
        {
            float x=(p.x-80)*.012f,z=(610-p.y)*.012f;
            // Bed, bedside cabinet, north flower stand, east desk, southeast wardrobe.
            return !(x<1.25f && z>1.85f && z<4.95f)
                && !(x>1.15f && x<1.78f && z>3.15f && z<3.85f)
                && !(x>2.55f && x<3.45f && z>4.13f)
                && !(x>4.65f && z>2.35f && z<3.95f)
                && !(x>5.05f && z<1.75f);
        }
        public override void Tick(float dt,WBInput input)
        {
            if(Completed)return;dt=Mathf.Max(0,dt);clock+=dt;
            inkThisTick=inkPending;inkPending=false;
            bool eHeld=input.Keys.Contains(KeyCode.E);
            if(eHeld && !eWasHeld && (Phase=="tutorial" || Phase=="collect"))
            {
                bufferedItem=-1;
                for(int i=0;i<4;i++)if(InPickupRange(i)){bufferedItem=i;pickupExpires=clock+PickupBufferSeconds;break;}
            }
            eWasHeld=eHeld;
            if(bufferedItem>=0 && (clock>pickupExpires || !InPickupRange(bufferedItem)))bufferedItem=-1;
            if(Phase=="complete"){Finish();return;}
            if(Phase=="crumple")return;
            if(Phase=="farewell"){farewellTime=Mathf.Min(FarewellDuration,farewellTime+dt);if(farewellTime>=FarewellDuration){Phase="complete";Finish();}return;}
            if(Phase=="arrival")
            {
                // Holding E preserves progress during pen lifts and row returns.
                // Only fresh ink advances the hold; releasing E resets it.
                if(!eHeld){handover=0;}
                else if(InkFresh){handover+=dt;}

                if(handover>=(NarrativeDefinition.Resolve(this)?.farewellHold??1.2f)){Phase="farewell";farewellTime=0;}
                return;
            }
            float step=Phase=="collect" && Started?Mathf.Min(dt,Remaining):dt;
            var axes=new Vector2((input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0),(input.Keys.Contains(KeyCode.S)?1:0)-(input.Keys.Contains(KeyCode.W)?1:0));
            var move=Vector2.ClampMagnitude(axes,1)*WalkSpeed*step;var next=Player+move;
            next.x=Mathf.Clamp(next.x,82,577);next.y=Mathf.Clamp(next.y,202,610);
            // Axis-separated movement slides along the simplified furniture footprints.
            if(CanStand(new Vector2(next.x,Player.y)))Player.x=next.x;
            if(CanStand(new Vector2(Player.x,next.y)))Player.y=next.y;
            if(InkFresh)for(int i=0;i<4;i++)
            {
                if(InPickupRange(i) && (eHeld || (bufferedItem==i && clock<=pickupExpires)))
                {bufferedItem=-1;Collected|=1<<i;if(Phase=="tutorial"){Phase="crumple";CancelInput();return;}}
            }
            if(Phase!="collect" || !Started)return;
            // Arrival wins an exact deadline tie; movement is limited to the remaining time.
            if(Collected==15 && Player.y>=555 && Player.x>=260 && Player.x<=410){Remaining=Mathf.Max(0,Remaining-step);Phase="arrival";
                // Preserve pressed/pointer so an ongoing drag continues into farewell.
                bufferedItem=-1;pickupExpires=-1;handover=0;
                if(AwaitingReturn)NextRow();
                inkPending=inkThisTick=false;return;}
            Remaining=Mathf.Max(0,Remaining-dt);
            if(Remaining<=0){Phase="crumple";CancelInput();}
        }
        public override void Draw(){}
    }
}
