using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    public class WritingChapter:Chapter
    {
        public string Phase="tutorial";
        public Vector2 Player=new Vector2(160,505),Npc=new Vector2(315,570);
        public float WritingProgress,Remaining=60;
        public int Draft=1,Row,Collected;
        public bool Started,AwaitingReturn;
        public bool MissingItemHintUsed { get; private set; }
        public bool TimedOut => Draft>=2 && Phase=="crumple" && Remaining<=0;
        public bool Carried=>Collected!=0;
        public bool DoorHeld=>Phase=="farewell";
        public bool DoorOpen=>true;
        public bool InkFresh=>pressed && clock-lastInk<=.18f;
        public float FarewellProgress=>handover;
        public readonly Vector2[] ItemPositions={new Vector2(175,275),new Vector2(460,260),new Vector2(460,510),new Vector2(245,490)};
        public static readonly string[] ItemIds={"pendant","flower","luggage","letter"};
        public bool HasItem(int i)=>(Collected&(1<<i))!=0;
        readonly Rect paper=new Rect(685,165,535,445);
        readonly List<float> rows=new List<float>{0};
        float clock,lastInk=-100,handover,farewellTime;
        bool pressed,havePointer,dragging;
        Vector2 pointer;
        public override string Help=>"LMB drag  Write / WASD  Move / Hold E while writing  Pick up\nCrumple the stalled draft into the bin. The formal draft clock keeps running when you stop writing.";
        public IReadOnlyList<float> InkRows=>rows;
        public bool InPickupRange(int i)=>!HasItem(i) && Vector2.Distance(Player,ItemPositions[i])<46;
        public bool NearPickup=>InPickupRange(0)||InPickupRange(1)||InPickupRange(2)||InPickupRange(3);
        string TutorialHint=>NearPickup?"Keep writing + hold E":WritingProgress<=0?"Drag right across the letter":"WASD  Approach an object";
        public string TaskHint=>Phase=="crumple"?"Drag paper to the bin":Phase=="tutorial"?TutorialHint:Phase=="arrival"?"Keep writing + hold E to say goodbye":Phase=="farewell"?"": "Write + hold E to collect / Return to the entrance";
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
            MissingItemHintUsed=n.version>=3 && n.missingItemHintUsed;
            if(n.version==1){Phase=n.crumple?"crumple":Draft>=2?"collect":"tutorial";return;}
            if(Array.IndexOf(new[]{"tutorial","crumple","collect","arrival","farewell","complete"},n.phase)<0 || n.collected<0 || n.collected>15 || !Finite(n.remaining) || n.remaining<0 || !Finite(n.player.x) || !Finite(n.player.y))throw new System.IO.InvalidDataException("Invalid autumn state.");
            if((n.phase=="arrival" || n.phase=="farewell" || n.phase=="complete") && n.collected!=15)throw new System.IO.InvalidDataException("Missing autumn delivery.");
            Phase=n.phase=="farewell"?"arrival":n.phase;Collected=n.collected;Started=n.started;Remaining=n.remaining;
            Player=new Vector2(Mathf.Clamp(n.player.x,82,577),Mathf.Clamp(n.player.y,202,610));
            rows.Clear();if(n.rows!=null && n.rows.Length>0 && n.rows.Length<=9)foreach(var amount in n.rows){if(!Finite(amount))throw new System.IO.InvalidDataException("Invalid ink.");rows.Add(Mathf.Clamp(amount,0,425));}else rows.Add(0);
            Row=Mathf.Clamp(n.row,0,rows.Count-1);WritingProgress=Mathf.Max(0,n.writingProgress);AwaitingReturn=n.awaitingReturn;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public void CancelInput(){pressed=havePointer=dragging=false;lastInk=-100;handover=0;}
#if UNITY_EDITOR
        public void EditorBeginFarewell()
        {
            if(Completed || Phase=="farewell")return;
            CancelInput();Draft=Mathf.Max(2,Draft);Collected=15;
            Player=new Vector2(300,565);Phase="farewell";farewellTime=0;
        }
#endif
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
            rows[Row]+=amount;WritingProgress+=amount;lastInk=clock;AwaitingReturn=rows[Row]>=425;
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
            Player=new Vector2(160,505);rows.Clear();rows.Add(0);Row=0;WritingProgress=0;AwaitingReturn=false;CancelInput();
        }
        public override void Tick(float dt,WBInput input)
        {
            if(Completed)return;dt=Mathf.Max(0,dt);clock+=dt;
            if(Phase=="complete"){Finish();return;}
            if(Phase=="crumple")return;
            if(Phase=="farewell"){farewellTime+=dt;if(farewellTime>=2){Phase="complete";Finish();}return;}
            if(Phase=="arrival")
            {
                handover=InkFresh && input.Keys.Contains(KeyCode.E)?handover+dt:0;
                if(handover>=(NarrativeDefinition.Resolve(this)?.farewellHold??1.2f)){Phase="farewell";farewellTime=0;}
                return;
            }
            float step=Phase=="collect" && Started?Mathf.Min(dt,Remaining):dt;
            var axes=new Vector2((input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0),(input.Keys.Contains(KeyCode.S)?1:0)-(input.Keys.Contains(KeyCode.W)?1:0));
            var move=Vector2.ClampMagnitude(axes,1)*165*step;var next=Player+move;
            next.x=Mathf.Clamp(next.x,82,577);next.y=Mathf.Clamp(next.y,202,610);
            bool passage=next.y>353 && next.y<436;
            if(!(next.x>332 && next.x<368 && !passage))Player.x=next.x;
            if(!(Player.x>332 && Player.x<368 && !passage))Player.y=next.y;
            if(InkFresh && input.Keys.Contains(KeyCode.E))for(int i=0;i<4;i++)
            {
                if(InPickupRange(i))
                {Collected|=1<<i;if(Phase=="tutorial"){Phase="crumple";CancelInput();return;}}
            }
            if(Phase!="collect" || !Started)return;
            // Only a return with exactly one missing possession merits the girl's one cue.
            if(!MissingItemHintUsed && Player.y>=555 && Player.x>=260 && Player.x<=410 &&
                (Collected==7 || Collected==11 || Collected==13 || Collected==14))MissingItemHintUsed=true;
            // Arrival wins an exact deadline tie; movement is limited to the remaining time.
            if(Collected==15 && Player.y>=555 && Player.x>=260 && Player.x<=410){Remaining=Mathf.Max(0,Remaining-step);Phase="arrival";CancelInput();return;}
            Remaining=Mathf.Max(0,Remaining-dt);
            if(Remaining<=0){Phase="crumple";CancelInput();}
        }
        public override void Draw(){}
    }
}
