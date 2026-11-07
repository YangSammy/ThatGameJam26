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

    public class PaintingChapter:Chapter
    {
        public int Phase {get;private set;}
        public float Focus {get;private set;}
        public float Elapsed {get;private set;}
        public bool[] Painted {get;private set;}=new bool[5];
        public bool BookOpen {get;private set;}
        public Vector2 Actor=new Vector2(.7f,0),Velocity;
        public float SwingAngle=-.55f,SwingSpeed,FlowerAngle=-Mathf.PI/2,CrateX=2;
        public float BridgeGrowth,WhiteTime,WardDistance,WardRevealHeight;
        // Retained only to migrate the previous automatic-reveal save format.
        public const float WardRevealSeconds=12;
        bool wardDWasDown;
        public float WardTime {get;private set;}
        public float WardProgress=>Phase==4?1:Mathf.Clamp01(WardDistance/WardWalkDistance);
        public float NoseHeight=1.55f;
        public float WardWalkDistance=>Mathf.Max(.01f,NarrativeDefinition.Resolve(this)?.wardWalkDistance??8);
        public float WardWalkSpeed=>Mathf.Max(.01f,NarrativeDefinition.Resolve(this)?.wardWalkSpeed??.7f);
        internal void BindWardCutoff(float height)
        {
            NoseHeight=height;
            UpdateWardReveal();
        }
        void UpdateWardReveal()=>WardRevealHeight=NoseHeight*WardProgress;
        public bool KeyInserted,HighCheckpoint,MiddleCheckpoint,PendantHintPlayed;
        public int Riding=-1;
        public bool Grounded=true;
        internal int FallCount { get; private set; }
        internal bool OnDrawnBridge => Phase==0 && Grounded && BridgeGrowth>=1 && Actor.x>9 && Actor.x<12 &&
            Actor.x>=bridgeLeft && Actor.x<=bridgeRight;
        bool jumpWasDown;float remount,focusId=-1;
        public Vector2 CurrentTarget=>Vector2.zero;
        public bool CupContact=>false;
        public bool HandsContact=>false;
        public bool Ready=>true;
        // Positions are chapter configuration, never checkpoint data. The default geometry
        // also serves isolated model tests that do not load the winter scene.
        Matrix4x4 swingFrame=Matrix4x4.Translate(new Vector3(4.5f,0,0));
        Matrix4x4 wheelFrame=Matrix4x4.Translate(new Vector3(8,0,0));
        float plateX=4,doorX=14.7f,bridgeLeft=9,bridgeRight=12,bridgeY=.7f;
        internal void BindLayout(WinterAlbumWorkspace layout)
        {
            var toRoute=layout.routeOrigin.worldToLocalMatrix;
            var metres=Matrix4x4.Scale(Vector3.one*.1f);
            swingFrame=toRoute*layout.swing.transform.localToWorldMatrix*metres;
            wheelFrame=toRoute*layout.wheel.transform.localToWorldMatrix*metres;
            plateX=layout.routeOrigin.InverseTransformPoint(layout.counterweight.transform.position).x;
            CrateX=Mathf.Clamp(CrateX,1.3f,Mathf.Max(1.3f,plateX+.2f));
            doorX=layout.routeOrigin.InverseTransformPoint(layout.keySocket.position).x;
            var bridgeFrame=toRoute*layout.bridge.transform.localToWorldMatrix*metres;
            var left=bridgeFrame.MultiplyPoint3x4(Vector3.left*1.5f);
            var right=bridgeFrame.MultiplyPoint3x4(Vector3.right*1.5f);
            bridgeLeft=Mathf.Min(left.x,right.x);bridgeRight=Mathf.Max(left.x,right.x);bridgeY=(left.y+right.y)*.5f;
        }
        public bool AtDoor=>Phase==1 && Actor.x>=doorX-.7f && Actor.y>=3.1f;
        public bool PlatePressed=>Mathf.Abs(CrateX-plateX)<.36f && Painted[2] || Mathf.Abs(Actor.x-plateX)<.3f && Actor.y<.2f;
        float Speed=>NarrativeDefinition.Resolve(this)?.albumSpeed??2.5f;
        float Jump=>NarrativeDefinition.Resolve(this)?.jumpSpeed??5;
        float Gravity=>NarrativeDefinition.Resolve(this)?.gravity??12;
        public override string Help=>"Hold LMB  Focus / Lower edge  Raise album / Upper edge  Lower album\nA D  Move or swing / Space  Jump off / Drag pendant into the matching door recess";
        public string TaskHint=>Phase>=2?"D  Follow the line":!BookOpen?"Hold LMB  Focus / Mouse  Look / Lower edge  Raise album":
            AtDoor&&!KeyInserted?(Painted[4]?"Drag the pendant into the recess":"Upper edge  Look for the matching shape"):
            Riding==0?"A D  Swing / Space  Jump off":Riding>0?"Wait for the high ledge / D + Space  Jump off":
            Phase==1&&!HighCheckpoint?(PlatePressed?"D + Space  Jump over the suitcase":"D  Push the suitcase onto the plate"):
            "A D  Move / Space  Jump / Upper edge  Observe";
        public override string DebugState=>CaptureCheckpoint();
        [System.Serializable] sealed class Checkpoint
        {
            public int version=5,phase;public bool[] shapes;
            public bool middle,high,key,pendantHint;public float crate=2,ward,wardActor,white,wardTime;
        }
        public override string CaptureCheckpoint()=>JsonUtility.ToJson(new Checkpoint{phase=Phase,shapes=Painted,middle=MiddleCheckpoint,high=HighCheckpoint,key=KeyInserted,pendantHint=PendantHintPlayed,crate=CrateX,ward=WardDistance,wardActor=Phase>=3?Actor.x:0,white=WhiteTime,wardTime=WardTime});
        public override void RestoreCheckpoint(string json)
        {
            CancelInput();if(string.IsNullOrEmpty(json))return;
            var n=JsonUtility.FromJson<Checkpoint>(json);
            if(n==null || n.version<1 || n.version>5)throw new System.IO.InvalidDataException("Invalid album checkpoint.");
            if(n.version==1){Phase=0;return;}
            if(n.phase<0 || n.phase>4 || n.shapes==null || n.shapes.Length!=5 || !Finite(n.crate) || !Finite(n.ward) || !Finite(n.white) || n.ward<0)throw new System.IO.InvalidDataException("Invalid album route.");
            if(n.version>=3 && (!Finite(n.wardActor) || n.wardActor<0 || n.wardActor>n.ward))throw new System.IO.InvalidDataException("Invalid ward position.");
            if(n.version>=4 && (!Finite(n.wardTime) || n.wardTime<0))throw new System.IO.InvalidDataException("Invalid ward reveal time.");
            if(n.key && !n.shapes[4] || n.phase>=2 && !n.key)throw new System.IO.InvalidDataException("Invalid pendant ownership.");
            Phase=n.phase;Painted=n.shapes;MiddleCheckpoint=n.middle;HighCheckpoint=n.high;KeyInserted=n.key;
            PendantHintPlayed=n.pendantHint;
            CrateX=Mathf.Max(1.3f,n.crate);WardDistance=Mathf.Clamp(n.ward,0,WardWalkDistance);WhiteTime=Mathf.Max(0,n.white);BridgeGrowth=Painted[1]?1:0;
            // v4 could reveal while the actor stood still, or let it reach the line's
            // end before the picture. Preserve the picture, then rebase only an actor
            // ahead of that picture. New v5 saves preserve retreats exactly.
            if(n.version==4 && Phase==3)
                WardDistance=Mathf.Clamp(n.wardTime/WardRevealSeconds,0,.999f)*WardWalkDistance;
            if(Phase<3)WardDistance=0;
            WardTime=WardProgress*WardRevealSeconds;
            UpdateWardReveal();
            Respawn();
            // Old checkpoints recorded only the furthest distance and resumed there.
            if(Phase>=3 && n.version>=3)Actor=new Vector2(Mathf.Clamp(n.wardActor,0,WardDistance),0);
            BookOpen=Phase>=2;
        }
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        public void CancelInput(){Focus=0;focusId=-1;jumpWasDown=false;}
        public void SetBook(bool open){if(Phase>=2)return;BookOpen=open;Focus=0;jumpWasDown=false;}
        public bool Observe(int id,bool held,float dt)
        {
            if(BookOpen || Phase>1 || id<0 || id>=5 || (Phase==0 && id>1)){Focus=0;focusId=-1;return false;}
            if(focusId!=id){Focus=0;focusId=id;}
            Focus=held?Focus+Mathf.Max(0,dt):0;
            if(Painted[id] || Focus<(NarrativeDefinition.Resolve(this)?.shapeFocusSeconds??1.2f))return false;
            Painted[id]=true;Focus=0;return true;
        }
        public bool InsertPendant()
        {if(!BookOpen || !AtDoor || !Painted[4] || KeyInserted)return false;KeyInserted=true;return true;}
        public Vector2 SwingSeat=>swingFrame.MultiplyPoint3x4(new Vector3(Mathf.Sin(SwingAngle)*3,3.3f-Mathf.Cos(SwingAngle)*3));
        public Vector2 FlowerSeat(int petal)
        {float a=FlowerAngle+petal*Mathf.PI/3;return wheelFrame.MultiplyPoint3x4(new Vector3(Mathf.Cos(a)*2,2+Mathf.Sin(a)*2));}
        public void Respawn()
        {
            Riding=-1;Velocity=Vector2.zero;Grounded=true;remount=0;SwingSpeed=0;SwingAngle=-.55f;
            FlowerAngle=-Mathf.PI/2;
            Actor=Phase==0?(MiddleCheckpoint?new Vector2(6.8f,.7f):new Vector2(.7f,0)):
                Phase==1?(HighCheckpoint?new Vector2(10.8f,3.2f):new Vector2(.7f,0)):new Vector2(WardDistance,0);
        }
        public override void Tick(float dt,WBInput input)
        {
            if(Completed || !BookOpen)return;dt=Mathf.Max(0,dt);Elapsed+=dt;
            if(Phase==4){Finish();return;}
            if(Phase==2){WhiteTime+=dt;if(WhiteTime>=(NarrativeDefinition.Resolve(this)?.whiteTransitionSeconds??1.2f)){Phase=3;Actor=Vector2.zero;WardDistance=WardTime=0;UpdateWardReveal();}return;}
            if(Phase==3)
            {
                // Ward movement is deliberate, D-only forward travel.
                float axis=input.Keys.Contains(KeyCode.D)?1:0;
                float limit=WardWalkDistance;
                bool press=axis>0 && !wardDWasDown;wardDWasDown=axis>0;
                Actor.x=Mathf.Clamp(Actor.x+axis*Mathf.Min(WardWalkSpeed,.35f)*dt+(press?.06f:0),0,limit);
                WardDistance=Mathf.Max(WardDistance,Actor.x);
                WardTime=WardProgress*WardRevealSeconds;
                bool reachedCutoff=WardDistance>=limit;
                if(reachedCutoff)Phase=4;
                UpdateWardReveal();
                if(reachedCutoff)Finish();
                return;
            }
            float direction=(input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0);
            bool jump=input.Keys.Contains(KeyCode.Space),edge=jump&&!jumpWasDown;jumpWasDown=jump;
            float remaining=Mathf.Min(dt,.25f);while(remaining>0){float h=Mathf.Min(remaining,.01f);remaining-=h;Step(h,direction,edge);edge=false;if(!BookOpen || Phase>=2)break;}
        }
        void Step(float dt,float axis,bool jump)
        {
            remount=Mathf.Max(0,remount-dt);
            if(Painted[1])BridgeGrowth=Mathf.MoveTowards(BridgeGrowth,1,dt/ .65f);
            if(Phase==1 && Painted[3])FlowerAngle+=dt*(NarrativeDefinition.Resolve(this)?.flowerRadiansPerSecond??.65f);
            if(Riding==0)
            {
                SwingSpeed+=(-Mathf.Sin(SwingAngle)*3.2f+axis*3.5f-SwingSpeed*.25f)*dt;
                SwingAngle+=SwingSpeed*dt;float limit=(NarrativeDefinition.Resolve(this)?.swingMaxDegrees??65)*Mathf.Deg2Rad;
                if(Mathf.Abs(SwingAngle)>limit){SwingAngle=Mathf.Clamp(SwingAngle,-limit,limit);SwingSpeed=0;}
                Actor=SwingSeat;
                if(!jump)return;
                // Jump from the current seat position without inheriting swing momentum.
                Velocity=new Vector2(axis*Speed,Jump);Riding=-1;Grounded=false;remount=.45f;
            }
            else if(Riding>=1)
            {
                int petal=Riding-1;Actor=FlowerSeat(petal);
                if(!jump)return;
                Velocity=new Vector2(axis*Speed,Jump);Riding=-1;Grounded=false;remount=.45f;
            }
            else
            {
                Velocity.x=Grounded?axis*Speed:Mathf.MoveTowards(Velocity.x,axis*Speed,dt*2);
                if(jump && Grounded){Velocity.y=Jump;Grounded=false;remount=.2f;}
            }
            var before=Actor;var next=Actor+Velocity*dt;Velocity.y-=Gravity*dt;
            next.x=Mathf.Clamp(next.x,.15f,16.5f);
            if(Phase==1)
            {
                if(Painted[2] && !HighCheckpoint && Mathf.Abs(next.x-CrateX)<.62f && next.y<.5f)
                {
                    // Resolve on the side occupied before the step, never the input side.
                    float side=before.x<CrateX?-1:1;
                    if(Grounded && axis*side<0)CrateX=Mathf.Clamp(CrateX+axis*Speed*dt,1.3f,plateX+.2f);
                    next.x=CrateX+side*.62f;Velocity.x=0;
                }
                if(!PlatePressed && before.x<plateX+2.2f && next.x>=plateX+1.9f && next.y<2.5f)next.x=plateX+1.9f;
            }
            Grounded=false;
            float floor=Floor(next.x);
            if(Velocity.y<=0 && before.y>=floor-.03f && next.y<=floor && floor>-5){next.y=floor;Velocity.y=0;Grounded=true;}
            Actor=next;
            if(Actor.y< -3){FallCount++;Respawn();return;}
            if(Phase==0)
            {
                if(Grounded && Actor.x>=6 && Actor.x<=9)MiddleCheckpoint=true;
                if(Painted[0] && remount<=0 && !MiddleCheckpoint && CanBoard(SwingSeat,.8f)){Riding=0;Actor=SwingSeat;Velocity=Vector2.zero;}
                if(Actor.x>=15.3f && Grounded && Painted[0] && Painted[1]){Phase=1;MiddleCheckpoint=false;Respawn();SetBook(false);}
            }
            else
            {
                if(Grounded && Actor.x>=10.3f && Actor.y>=3.1f)HighCheckpoint=true;
                if(Painted[3] && remount<=0 && !HighCheckpoint)for(int i=0;i<6;i++)if(CanBoard(FlowerSeat(i),.52f)){Riding=i+1;Actor=FlowerSeat(i);Velocity=Vector2.zero;break;}
                if(Actor.x>=doorX+.8f && Grounded && KeyInserted){Phase=2;WhiteTime=0;Riding=-1;Velocity=Vector2.zero;}
                // Wait beside the missing piece, not directly in front of its silhouette.
                else if(!KeyInserted && Actor.x>doorX-.5f)Actor.x=doorX-.5f;
            }
        }
        bool CanBoard(Vector2 seat,float radius)
        {
            // Ground approaches remain usable; airborne boarding is a landing from above.
            return Velocity.y<=0 && (Grounded || Actor.y>=seat.y-.1f) && Vector2.Distance(Actor,seat)<radius;
        }
        float Floor(float x)
        {
            if(Phase==0){if(x<=3)return 0;if(x>=6 && x<=9 || x>=12)return .7f;if(x>=bridgeLeft && x<=bridgeRight && BridgeGrowth>=1)return bridgeY;return -10;}
            if(x<=6 || x<=8.2f && PlatePressed)return 0;
            if(x>=10.1f)return 3.2f;return -10;
        }
        public override void Draw(){}
    }

    public class EndingChapter:Chapter
    {
        public int Phase {get;private set;}
        public float SpringProgress {get;private set;}
        public bool DoorOpen=>Phase>=1;
        public bool Dragging=>false;
        public bool LeftHeld=>false;
        public Vector2 Point=>Vector2.zero;
        public Vector2 Target=>Vector2.zero;
        [System.Serializable] sealed class Checkpoint{public int version=2,phase;public float spring;}
        public override string CaptureCheckpoint()=>JsonUtility.ToJson(new Checkpoint{phase=Phase,spring=SpringProgress});
        public override void RestoreCheckpoint(string json)
        {
            if(string.IsNullOrEmpty(json))return;var n=JsonUtility.FromJson<Checkpoint>(json);
            if(n==null || n.version<1 || n.version>2 || n.phase<0 || n.phase>3 || float.IsNaN(n.spring)||float.IsInfinity(n.spring))throw new System.IO.InvalidDataException("Invalid departure checkpoint.");
            Phase=n.version==1?0:Mathf.Min(n.phase,2);SpringProgress=n.version==1?0:Mathf.Clamp01(n.spring);
        }
        public void OpenDoor(){if(!Completed)Phase=1;}
        public void CrossThreshold(){if(DoorOpen && !Completed){Phase=2;Finish();}}
        public override void Tick(float dt,WBInput input){SpringProgress=Mathf.Clamp01(SpringProgress+Mathf.Max(0,dt)/(NarrativeDefinition.Resolve(this)?.springSeconds??5));if(Phase==2&&!Completed)Finish();}
        public override string Help=>"LMB  Open door / Walk through the doorway";
        public override void Draw(){}
    }
}
