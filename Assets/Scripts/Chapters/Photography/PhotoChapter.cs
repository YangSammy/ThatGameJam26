using System;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed class PhotoChapter : Chapter
    {
        public enum FilmPhase { Empty, LoadedOpen, Closed, Ready, Exposed, Rewound, DarkBag, Tank, Processing, Developed }
        [Serializable] public class CameraSettings
        {
            public float X=-.7f,Z=-.25f,Yaw,Focal=35,Focus=2,Aperture=8,Shutter=1f/60;
            public int ISO=100;
            public CameraSettings Copy()=>(CameraSettings)MemberwiseClone();
        }
        public readonly CameraSettings Settings=new CameraSettings();
        public readonly CameraSettings Target=new CameraSettings{X=.55f,Z=.4f,Yaw=0,Focal=50,Focus=4.6f,Aperture=8,Shutter=1f/250,ISO=200};
        public CameraSettings Captured;
        public Vector3[] CompositionLandmarks;
        public Vector3? FocusSubject;
        public float FocusDistance(CameraSettings s){return FocusSubject.HasValue?Mathf.Max(.2f,Vector3.Dot(FocusSubject.Value-new Vector3(s.X,1.5f,s.Z),new Vector3(Mathf.Sin(s.Yaw),0,Mathf.Cos(s.Yaw)))):Mathf.Max(.2f,5-s.Z);}
        public FilmPhase Phase;
        public int Chemistry;
        public float ProcessingTime,GeometryError,ExposureError,FocusBlur,MotionBlur;
        public bool Fogged,Agitated,FilmThreaded;
        public int Attempts;
        public bool InViewfinder,MeterVisible;
        public float BagTurns;
        public int WheelMode;
        int dragISO=100;
        int drag=-1;Vector2 dragPoint,last;bool aiming,shaking,bagHeld;
        readonly Rect view=new Rect(500,130,730,400),tank=new Rect(970,575,115,115);
        static readonly float[] apertures={2.8f,4,5.6f,8,11,16};
        static readonly float[] shutters={1f/4,1f/8,1f/15,1f/30,1f/60,1f/125,1f/250,1f/500,1f/1000};
        public override string Help=>"先WASD找拍摄位置。选择ISO100/200/400胶卷拖入后盖→拖片头到右侧收片轴→点后盖→点过片杆。测光表在左侧，读数后记忆换算。点右侧画面进取景器：中心滚轮调焦距，向右后滚轮调快门，向下后调光圈，向上离开; 回中心重选焦距，左键拍摄。拍完倒片回卷，卷拖黑袋，按住左键滚轮把胶片绕入片轴，再按1→2→3倒药，每次拖罐轻摇后计时，最后点水滴。Esc返回旧屋。";
        public static Vector2 Project(Vector3 p,CameraSettings s,Rect frame)
        {
            float dx=p.x-s.X,dz=p.z-s.Z,rx=dx*Mathf.Cos(s.Yaw)-dz*Mathf.Sin(s.Yaw),rz=dx*Mathf.Sin(s.Yaw)+dz*Mathf.Cos(s.Yaw);
            float f=frame.width*s.Focal/36f;
            return new Vector2(frame.center.x+rx/Mathf.Max(.3f,rz)*f,frame.y+frame.height*.57f+(1.5f-p.y)/Mathf.Max(.3f,rz)*f);
        }
        public static float BlurAt(float distance,CameraSettings s,float width=730)
        {
            float f=s.Focal/1000f,d=Mathf.Max(.1f,distance),focus=Mathf.Max(.1f,s.Focus);
            float v=f*d/(d-f),vf=f*focus/(focus-f);return Mathf.Abs(vf-v)/v*(f/s.Aperture)/.036f*width;
        }
        public static float EV(CameraSettings s)=>Mathf.Log(s.Aperture*s.Aperture/s.Shutter,2)-Mathf.Log(s.ISO/100f,2);
        public override void Tick(float dt,WBInput input)
        {
            if(Completed)return;
            if(!InViewfinder&&!MeterVisible&&Phase!=FilmPhase.Processing&&Phase!=FilmPhase.Developed){float dx=(input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0),dz=(input.Keys.Contains(KeyCode.W)?1:0)-(input.Keys.Contains(KeyCode.S)?1:0);Settings.X=Mathf.Clamp(Settings.X+dx*dt*.8f,-1.6f,1.6f);Settings.Z=Mathf.Clamp(Settings.Z+dz*dt*.65f,-.7f,1.4f);}
            Settings.Focus=5-Settings.Z;
            if(Phase==FilmPhase.Processing&&(Agitated||Chemistry==3)){ProcessingTime-=dt;if(ProcessingTime<=0){Chemistry++;if(Chemistry>=4){Phase=FilmPhase.Developed;Evaluate();}else Phase=FilmPhase.Tank;}}
        }
        public override void PointerDown(Vector2 p,int button)
        {
            if(Completed)return;last=p;
            if(MeterVisible){MeterVisible=false;return;}
            if(InViewfinder){if(button==0&&Phase==FilmPhase.Ready)Expose();return;}
            if(new Rect(365,370,85,62).Contains(p)){MeterVisible=true;return;}
            if(Phase==FilmPhase.DarkBag&&tank.Contains(p)&&button==0){bagHeld=true;return;}
            if(Phase==FilmPhase.LoadedOpen&&!FilmThreaded&&new Rect(145,484,55,45).Contains(p)){drag=4;dragPoint=p;return;}
            if(Phase==FilmPhase.Processing&&tank.Contains(p)){shaking=true;return;}
            if(Phase==FilmPhase.Empty)for(int i=0;i<4;i++)if(new Rect(65+i*95,590,82,90).Contains(p)){drag=0;dragISO=new[]{100,200,400,800}[i];dragPoint=p;return;}
            if(Phase==FilmPhase.Rewound&&new Rect(375,555,90,90).Contains(p)){drag=0;dragPoint=p;return;}
            if(Phase==FilmPhase.Tank){for(int i=0;i<3;i++)if(new Rect(1115,548+i*48,47,42).Contains(p)){drag=i+1;dragPoint=p;return;}if(Chemistry==3&&new Rect(1170,610,70,80).Contains(p)){ProcessingTime=1;Phase=FilmPhase.Processing;return;}}
            if(new Rect(80,445,280,125).Contains(p)){if(Phase==FilmPhase.LoadedOpen&&FilmThreaded)Phase=FilmPhase.Closed;else if(Phase==FilmPhase.Exposed){Fogged=true;Phase=FilmPhase.Rewound;}return;}
            if(Vector2.Distance(p,new Vector2(405,468))<30&&Phase==FilmPhase.Closed){Phase=FilmPhase.Ready;WBMain.Tone(160,.07f);return;}
            if(Vector2.Distance(p,new Vector2(345,423))<26&&Phase==FilmPhase.Ready){Expose();return;}
            if(Vector2.Distance(p,new Vector2(115,425))<29&&Phase==FilmPhase.Exposed){Phase=FilmPhase.Rewound;WBMain.Tone(180,.15f);return;}
            if(Phase==FilmPhase.Developed&&new Rect(385,647,75,40).Contains(p)){Phase=FilmPhase.Empty;Captured=null;Chemistry=0;Fogged=false;FilmThreaded=false;return;}
            if(view.Contains(p)&&Phase==FilmPhase.Ready){InViewfinder=true;WheelMode=0;return;}
            if(view.Contains(p)&&Phase!=FilmPhase.Developed)aiming=true;
        }
        public override void PointerMove(Vector2 p)
        {
            if(InViewfinder)
            {
                if(p.y<view.y+35){InViewfinder=false;return;}
                if(p.y>view.center.y+105)WheelMode=2;
                else if(p.x>view.center.x+150)WheelMode=1;
                else if(Mathf.Abs(p.x-view.center.x)<80&&Mathf.Abs(p.y-view.center.y)<70)WheelMode=0;
                return;
            }
            if(drag>=0)dragPoint=p;
            if(shaking&&Vector2.Distance(p,last)>15)Agitated=true;
            if(aiming)Settings.Yaw=Mathf.Clamp(Settings.Yaw+(p.x-last.x)*.0013f,-.25f,.25f);
            last=p;
        }
        public override void PointerUp(Vector2 p)
        {
            aiming=false;shaking=false;bagHeld=false;if(drag==4){if(new Rect(270,475,75,70).Contains(p))FilmThreaded=true;}
            else if(drag==0){if(Phase==FilmPhase.Empty&&new Rect(80,445,280,125).Contains(p)){Settings.ISO=dragISO;Phase=FilmPhase.LoadedOpen;}else if(Phase==FilmPhase.Rewound&&tank.Contains(p)){Phase=FilmPhase.DarkBag;BagTurns=0;Chemistry=0;}}
            else if(drag>0&&Phase==FilmPhase.Tank&&tank.Contains(p)){if(drag-1==Chemistry){ProcessingTime=Chemistry==0?3:Chemistry==1?.8f:2;Agitated=false;Phase=FilmPhase.Processing;WBMain.Tone(240,.08f);}else WBMain.Tone(100,.08f);}
            drag=-1;
        }
        public override void Wheel(float delta,Vector2 p)
        {
            if(Phase==FilmPhase.DarkBag&&bagHeld){BagTurns=Mathf.Clamp01(BagTurns+delta*.1f);if(BagTurns>=.999f){Phase=FilmPhase.Tank;bagHeld=false;WBMain.Tone(300,.12f);}return;}
            if(!InViewfinder||Mathf.Abs(delta)<.01f)return;int step=delta>0?1:-1;
            if(WheelMode==0)Settings.Focal=Mathf.Clamp(Settings.Focal+step*5,28,70);
            else if(WheelMode==1)Settings.Shutter=shutters[Mathf.Clamp(Closest(shutters,Settings.Shutter)+step,0,shutters.Length-1)];
            else Settings.Aperture=apertures[Mathf.Clamp(Closest(apertures,Settings.Aperture)+step,0,apertures.Length-1)];
            WBMain.Tone(120,.025f);
        }
        public void Expose(){Captured=Settings.Copy();Phase=FilmPhase.Exposed;InViewfinder=false;Attempts++;WBMain.Tone(95,.06f);}
        public void Evaluate()
        {
            Assess();
            if(Captured!=null && FailureReason=="")Finish();
        }
        public void Assess()
        {
            if(Captured==null)return;
            Vector3[] landmarks=CompositionLandmarks??new[]{new Vector3(0,1.6f,5),new Vector3(-1.1f,.9f,2.8f),new Vector3(1.7f,2,9),new Vector3(0,0,5)};
            GeometryError=0;foreach(Vector3 p in landmarks)GeometryError+=Vector2.Distance(Project(p,Captured,view),Project(p,Target,view))/landmarks.Length;
            ExposureError=Mathf.Abs(EV(Captured)-EV(Target));FocusBlur=BlurAt(FocusDistance(Captured),Captured);MotionBlur=Mathf.Max(0,Captured.Shutter/(1f/125)-1)*2.5f;
        }
        public string FailureReason {
            get {
                if(Captured==null)return "No exposure";
                var reasons=new System.Collections.Generic.List<string>();
                if(Fogged)reasons.Add("Film fogged: rewind before opening");
                if(GeometryError>=21)reasons.Add("Framing mismatch: adjust position and focal length");
                if(ExposureError>=.38f)reasons.Add(EV(Captured)>EV(Target)?"Underexposed: slower shutter / wider aperture":"Overexposed: faster shutter / smaller aperture");
                if(FocusBlur>=1.4f)reasons.Add("Out of focus: adjust distance / aperture");
                if(MotionBlur>=3.5f)reasons.Add("Motion blur: use faster shutter");
                return string.Join("; ",reasons.ToArray());
            }
        }
        public void RestoreCompleted(bool value){Completed=value;}
        // Explicit clipping keeps rotated IMGUI lines inside each photographic frame.
        static int OutCode(Vector2 p,Rect r)=> (p.x<r.xMin?1:p.x>r.xMax?2:0)|(p.y<r.yMin?4:p.y>r.yMax?8:0);
        static void ClipLine(Rect frame,Vector2 a,Vector2 b,Color color,float width=2)
        {
            Rect r=new Rect(frame.x+width/2,frame.y+width/2,frame.width-width,frame.height-width);
            for(int i=0;i<12;i++)
            {
                int ca=OutCode(a,r),cb=OutCode(b,r);if((ca|cb)==0){G.Line(a,b,color,width);return;}if((ca&cb)!=0)return;
                int code=ca!=0?ca:cb;Vector2 q;
                if((code&8)!=0)q=new Vector2(a.x+(b.x-a.x)*(r.yMax-a.y)/(b.y-a.y),r.yMax);
                else if((code&4)!=0)q=new Vector2(a.x+(b.x-a.x)*(r.yMin-a.y)/(b.y-a.y),r.yMin);
                else if((code&2)!=0)q=new Vector2(r.xMax,a.y+(b.y-a.y)*(r.xMax-a.x)/(b.x-a.x));
                else q=new Vector2(r.xMin,a.y+(b.y-a.y)*(r.xMin-a.x)/(b.x-a.x));
                if(code==ca)a=q;else b=q;
            }
        }
        void Scene(Rect frame,CameraSettings s,bool fog=false,bool live=false)
        {
            float exposure=live?1:Mathf.Pow(2,EV(Target)-EV(s)),light=Mathf.Clamp(exposure,.12f,2);
            G.Box(frame,Color.Lerp(Color.black,G.C("#dce3d8"),Mathf.Clamp01(light)));
            if(fog){G.Box(frame,G.C("#f7f6e8"));return;}
            Color ink=Color.Lerp(Color.black,G.C("#536551"),Mathf.Clamp01(light));
            Action<Vector3,Vector3,Color> segment=(a,b,col)=>{
                Vector2 p=Project(a,s,frame),q=Project(b,s,frame);float blur=Mathf.Min(10,BlurAt((a.z+b.z)/2-s.Z,s,frame.width));
                if(blur>.5f)for(int i=-1;i<=1;i+=2){Color ghost=col;ghost.a*=.2f;ClipLine(frame,p+Vector2.right*blur*i,q+Vector2.right*blur*i,ghost,2);}
                ClipLine(frame,p,q,col,2);
            };
            for(int z=3;z<=11;z+=2)segment(new Vector3(-3,0,z),new Vector3(3,0,z),new Color(ink.r,ink.g,ink.b,.25f));
            segment(new Vector3(1.15f,2.4f,9),new Vector3(2.25f,2.4f,9),ink);segment(new Vector3(2.25f,2.4f,9),new Vector3(2.25f,.8f,9),ink);segment(new Vector3(2.25f,.8f,9),new Vector3(1.15f,.8f,9),ink);segment(new Vector3(1.15f,.8f,9),new Vector3(1.15f,2.4f,9),ink);segment(new Vector3(1.7f,2.4f,9),new Vector3(1.7f,.8f,9),ink);
            Vector2 head=Project(new Vector3(0,1.55f,5),s,frame),foot=Project(new Vector3(0,0,5),s,frame);float height=foot.y-head.y,r=Mathf.Max(5,height*.075f),blurSubject=BlurAt(5-s.Z,s,frame.width);
            if(blurSubject>1)for(int i=0;i<4;i++){float off=(i-1.5f)*Mathf.Min(blurSubject,12);Color ghost=ink;ghost.a=.12f;ClipLine(frame,head+new Vector2(off,r),foot+new Vector2(off,-height*.3f),ghost,height*.13f+blurSubject);}
            if(new Rect(frame.x+r,frame.y+r,frame.width-r*2,frame.height-r*2).Contains(head))G.Circle(head,r,ink);
            ClipLine(frame,head+new Vector2(0,r),foot+new Vector2(0,-height*.3f),ink,height*.13f);ClipLine(frame,foot+new Vector2(0,-height*.3f),foot+new Vector2(-height*.1f,0),ink,height*.05f);ClipLine(frame,foot+new Vector2(0,-height*.3f),foot+new Vector2(height*.1f,0),ink,height*.05f);
            Vector2 scarf=Project(new Vector3(0,1.35f,5),s,frame);float trail=Mathf.Min(18,Mathf.Max(0,s.Shutter/(1f/125)-1)*2.5f);for(int i=1;i<=3&&trail>0;i++){Color ghost=G.C("#a55f4c");ghost.a=.12f;ClipLine(frame,scarf+new Vector2(r+trail*i/3,4),scarf+new Vector2(r+4+trail*i/3,height*.27f),ghost,7);}ClipLine(frame,scarf+new Vector2(-r,0),scarf+new Vector2(r,4),G.C("#a55f4c"),6);ClipLine(frame,scarf+new Vector2(r,4),scarf+new Vector2(r+4,height*.27f),G.C("#a55f4c"),7);
            segment(new Vector3(-1.1f,0,2.8f),new Vector3(-1.1f,1.2f,2.8f),ink);segment(new Vector3(-1.8f,.85f,2.8f),new Vector3(.9f,.85f,2.8f),ink);
            if(exposure>1)G.Box(frame,new Color(1,1,.95f,Mathf.Clamp01((exposure-1)*.38f)));G.Box(frame,Color.clear,G.C("#687a64"),2);
        }
        void Roll(Vector2 p,int iso=100){G.Box(new Rect(p.x-28,p.y-28,56,57),G.C("#333e36"),G.C("#89977c"),2);G.Box(new Rect(p.x-19,p.y-16,38,29),G.C("#d5d0b7"));G.Circle(p+new Vector2(0,-31),8,G.C("#59694e"));G.Text(iso.ToString(),new Rect(p.x-20,p.y-12,40,20),13);}
        float Slider(float v,float a,float b,Rect r,string prefix,string value)
        {
            G.Text(prefix,new Rect(r.x,r.y-5,35,24),14);G.Text(value,new Rect(r.x+259,r.y-5,89,24),14);return GUI.HorizontalSlider(new Rect(r.x+42,r.y+2,205,17),v,a,b);
        }
        public override void Draw()
        {
            Scene(new Rect(45,130,400,220),Target);G.Box(new Rect(37,122,416,236),Color.clear,G.C("#a5afa0"),4);
            var display=Phase==FilmPhase.Developed&&Captured!=null?Captured:Settings.Copy();if(Phase!=FilmPhase.Developed){if(!InViewfinder)display.Focal=35;if(!InViewfinder||WheelMode!=2)display.Aperture=2.8f;display.Shutter=1f/125;}
            Scene(view,display,Phase==FilmPhase.Developed&&Fogged,Phase!=FilmPhase.Developed);
            if(Phase!=FilmPhase.Developed){G.Line(new Vector2(view.center.x-15,view.center.y),new Vector2(view.center.x+15,view.center.y),G.C("#9b7661"),1);G.Line(new Vector2(view.center.x,view.center.y-15),new Vector2(view.center.x,view.center.y+15),G.C("#9b7661"),1);}
            Color body=G.C("#aab8a2"),edge=G.C("#61765b");G.Box(new Rect(80,445,280,125),body,edge,2);G.Ring(new Vector2(240,505),39,1,edge,4);G.Ring(new Vector2(240,505),25,1,edge,2);
            if(Phase==FilmPhase.Empty||Phase==FilmPhase.LoadedOpen){G.Box(new Rect(82,450,268,112),G.C("#d9dfcf"),edge,2);G.Box(new Rect(166,472,92,66),G.C("#8c9b80"));if(Phase==FilmPhase.LoadedOpen){Roll(new Vector2(122,507),Settings.ISO);G.Ring(new Vector2(307,507),20,1,edge,4);if(FilmThreaded)G.Box(new Rect(146,493,160,28),G.C("#817c59"));else{G.Box(new Rect(146,493,30,28),G.C("#817c59"));G.Arrow(new Vector2(181,507),new Vector2(282,507),G.C("#ac7352"),2);}}G.Line(new Vector2(80,450),new Vector2(46,430),edge,3);G.Line(new Vector2(46,430),new Vector2(46,554),edge,3);}
            G.Circle(new Vector2(115,425),17,edge);G.Arrow(new Vector2(100,425),new Vector2(115,410),G.C("#d8e1cd"),2);
            G.Circle(new Vector2(345,423),17,Phase==FilmPhase.Ready?G.C("#ad634f"):edge);G.Line(new Vector2(367,455),new Vector2(410,470),edge,8);G.Circle(new Vector2(405,468),10,edge);
            if(Phase==FilmPhase.Ready)G.Circle(new Vector2(346,423),5,G.C("#e8e9da"));
            if(Phase==FilmPhase.Empty)for(int i=0;i<4;i++)if(drag!=0||dragISO!=new[]{100,200,400,800}[i])Roll(new Vector2(105+i*95,635),new[]{100,200,400,800}[i]);if(Phase==FilmPhase.Rewound&&drag!=0)Roll(new Vector2(417,597),Settings.ISO);
            if(Phase==FilmPhase.Developed){G.Text("↻",new Rect(385,647,75,40),28);if(Completed)G.Check(new Vector2(350,625),22);else{G.Circle(new Vector2(350,625),18,G.C("#ba8063"));G.Text("!",new Rect(338,608,24,32),22,Color.white);}}
            G.Box(new Rect(365,370,85,62),G.C("#b4bda9"),edge,2);G.Circle(new Vector2(407,391),14,G.C("#59684f"));G.Line(new Vector2(407,391),new Vector2(418,380),G.C("#e2e7d5"),2);G.Text("EV",new Rect(375,406,65,18),12);
            if(InViewfinder){G.Box(new Rect(520,548,355,105),G.C("#dce1d1"),edge,2);string val=WheelMode==0?Settings.Focal.ToString("0")+" mm":WheelMode==1?"1/"+(1/Settings.Shutter).ToString("0"):"f/"+Settings.Aperture.ToString("0.0");G.Ring(new Vector2(575,599),31,1,edge,4);G.Line(new Vector2(575,574),new Vector2(575,583),edge,3);G.Text(val,new Rect(622,567,210,55),29);G.Text(WheelMode==0?"↔":WheelMode==1?"◷":"◉",new Rect(829,565,30,50),22);G.Text("ISO "+Settings.ISO,new Rect(665,622,150,23),13);}
            else {G.Icon(1,new Vector2(565,602),23,edge);G.Arrow(new Vector2(606,602),new Vector2(648,602),edge,2);G.Ring(new Vector2(687,602),22,1,edge,3);G.Text("M",new Rect(745,574,40,40),24);G.Text("ISO "+Settings.ISO,new Rect(790,584,100,35),16);}
            G.Box(tank,G.C("#343e35"),edge,3);G.Box(new Rect(983,558,90,17),G.C("#28352b"));G.Icon(1,new Vector2(1028,629),20,G.C("#bdcab0"));
            if(Phase==FilmPhase.DarkBag){G.Box(new Rect(953,565,148,127),G.C("#252e29"),edge,2);G.Ring(new Vector2(1028,629),30,BagTurns,G.C("#d4c69a"),4);G.Text("↻",new Rect(995,592,65,65),35,G.C("#d4c69a"));G.Circle(new Vector2(966,672),11,G.C("#65705c"));G.Circle(new Vector2(1087,672),11,G.C("#65705c"));}
            if(Phase==FilmPhase.Tank||Phase==FilmPhase.Processing||Phase==FilmPhase.Developed){for(int i=0;i<4;i++)G.Circle(new Vector2(995+i*22,679),4,i<Chemistry?G.C("#c4d7b3"):G.C("#626e58"));}
            for(int i=0;i<3;i++){G.Box(new Rect(1115,548+i*48,47,42),G.C(i<Chemistry?"#cbd2c1":"#e2e3d4"),edge);G.Box(new Rect(1125,540+i*48,27,8),edge);G.Text((i+1).ToString(),new Rect(1115,552+i*48,47,32),18);if(i==Chemistry&&Phase==FilmPhase.Tank)G.Ring(new Vector2(1138,569+i*48),24,1,G.C("#b37c58"),2);}
            G.Circle(new Vector2(1201,650),13,G.C("#738b89"));G.Line(new Vector2(1201,621),new Vector2(1191,642),G.C("#738b89"),5);G.Line(new Vector2(1201,621),new Vector2(1211,642),G.C("#738b89"),5);
            if(Phase==FilmPhase.Processing){G.Ring(new Vector2(1028,628),35,1-ProcessingTime/(Chemistry==0?3:Chemistry==1?.8f:Chemistry==2?2:1),G.C("#d4c69a"),4);if(!Agitated&&Chemistry<3){G.Arrow(new Vector2(995,612),new Vector2(1060,646),G.C("#d4c69a"),3);G.Arrow(new Vector2(1060,646),new Vector2(995,612),G.C("#d4c69a"),3);}}
            if(drag==4)G.Line(new Vector2(147,507),dragPoint,G.C("#817c59"),25);else if(drag==0)Roll(dragPoint,dragISO);else if(drag>0){G.Box(new Rect(dragPoint.x-20,dragPoint.y-20,40,40),G.C("#dddcca"),edge);G.Text(drag.ToString(),new Rect(dragPoint.x-20,dragPoint.y-20,40,40),18);}
            if(MeterVisible){G.Box(new Rect(465,200,380,290),G.C("#384536"),G.C("#88967d"),4);G.Box(new Rect(490,240,330,210),G.C("#c8d1b4"));G.Text("EV100",new Rect(510,252,120,34),22);G.Text("13",new Rect(650,247,100,55),42);G.Text("ISO 100",new Rect(510,320,230,35),24);G.Text("f/8     1/125",new Rect(510,370,280,45),28);}
        }
        static int Closest(float[] a,float v){int result=0;for(int i=1;i<a.Length;i++)if(Mathf.Abs(a[i]-v)<Mathf.Abs(a[result]-v))result=i;return result;}
        public override string DebugState=>"{\"phase\":\""+Phase+"\",\"chemistry\":"+Chemistry+",\"attempts\":"+Attempts+",\"fogged\":"+Fogged.ToString().ToLower()+",\"completed\":"+Completed.ToString().ToLower()+"}";
    }
}
