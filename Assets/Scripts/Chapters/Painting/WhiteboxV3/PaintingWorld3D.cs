using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox.V3
{
    // The landscape, hands, paper and playable relief are native 3D objects.
    public sealed class PaintingWorld3D : WorldChapter3D
    {
        public readonly PaintingChapter State;
        public PaintingDefocus Defocus { get; private set; }
        public Action<string,WorldChapter3D> CaptureStage;
        public string GazeTarget { get; private set; }="";
        public bool BoardOpen=>State.BoardOpen;
        public float BoardBlend { get; private set; }
        public bool[] Painted=>State.Painted;
        public Transform HospitalNoseMarker=>nose;
        public float HospitalRevealEdge { get; private set; }
        public float NoseViewportY { get; private set; }
        public int NoseProjectionSamples { get; private set; }
        public bool HospitalVisible=>hospital.gameObject.activeSelf;
        public bool PersistentPaperVisible=>board.gameObject.activeSelf;
        public bool HasGirlInFirstTwoActs=>false;
        public Vector3 ObserverAnchor=>World(State.Act==0?new Vector3(0,1.65f,-3):State.Act==1?new Vector3(0,1.65f,-2.7f):new Vector3(0,1.62f,-3.5f));
        readonly Transform park,room,hospital,board,runner,swingPivot,swingSeat,flowerRotor,flowerSeat,luggage,plateGate,keyDoor,token,nose,whiteMask,insertedPendant;
        readonly GameObject[] parkCourse=new GameObject[3],roomCourse=new GameObject[6];
        readonly Transform parkRelief,roomRelief,lineRelief;
        readonly GameObject binding;
        readonly Renderer whiteRenderer,paperRenderer;
        readonly string[] ids={"swing-target","bridge-target","luggage-target","flower-target","pendant-target"};
        readonly Renderer[] sources=new Renderer[5];
        readonly Dictionary<int,List<Renderer>> marks=new Dictionary<int,List<Renderer>>();
        readonly Vector3[] pendantShape={new Vector3(-.11f,.10f,0),new Vector3(.025f,.145f,0),new Vector3(.12f,.055f,0),new Vector3(.065f,-.11f,0),new Vector3(-.065f,-.14f,0),new Vector3(-.13f,-.025f,0),new Vector3(-.11f,.10f,0)};
        readonly WBInput routed=new WBInput();
        Quaternion viewRotation;float yaw,pitch,clock;int lastAct=-1;string previousStage;
        bool dragging,edgeLatch;Vector2 pointer=new Vector2(640,380);

        public PaintingWorld3D(WBMain host,PaintingChapter model):base(host,model,3)
        {
            State=model;WantsMouseLook=false;Fov=58;
            park=Group("Park recollection - no person present",Vector3.zero);BuildPark();
            room=Group("Empty room recollection - no person present",Vector3.zero);BuildEmptyRoom();room.gameObject.SetActive(false);
            hospital=Group("Hospital fragment revealed from floor upward",Vector3.zero);nose=BuildHospital();hospital.gameObject.SetActive(false);
            board=Group("Persistent foreground drawing book and paper",Vector3.zero);
            Child(board,"board",Vector3.zero,new Vector3(2.68f,1.35f,.07f),"#816a51");
            paperRenderer=Child(board,null,new Vector3(0,0,-.045f),new Vector3(2.57f,1.24f,.015f),"#e8dfc7").GetComponent<Renderer>();
            binding=Child(board,null,new Vector3(0,0,-.061f),new Vector3(.025f,1.23f,.018f),"#b2a287");
            Child(board,null,new Vector3(0,-.69f,-.03f),new Vector3(.4f,.08f,.1f),"#53615b");
            BoardLine(board,"Wordless raise-book grip",new[]{new Vector3(-.07f,-.70f,-.09f),new Vector3(0,-.65f,-.09f),new Vector3(.07f,-.70f,-.09f)},"#e7ddbf",.012f);
            Hand(new Vector3(-1.32f,-.35f,-.04f));Hand(new Vector3(1.32f,-.35f,-.04f));
            parkRelief=NewChild(board,"Park playable relief");roomRelief=NewChild(board,"Room playable relief");lineRelief=NewChild(board,"Unbroken line on the persistent sheet");
            CourseBlock(parkRelief,0,3.25f,0,"#838b79");CourseBlock(parkRelief,7.2f,10.8f,0,"#838b79");CourseBlock(parkRelief,15,19,0,"#838b79");
            parkCourse[1]=CourseBlock(parkRelief,10.75f,15.1f,0,"#aa9270");
            for(float plank=10.9f;plank<15;plank+=.32f)BoardLine(parkCourse[1].transform,"Bridge plank",new[]{new Vector3((plank-12.925f)*.125f,.036f,-.026f),new Vector3((plank-12.925f)*.125f,-.025f,-.026f)},"#706b54",.003f);
            OutlineGap(parkRelief,3.25f,7.2f);OutlineGap(parkRelief,10.8f,15);
            swingPivot=NewChild(parkRelief,"Imported swing pivots under real gravity");swingPivot.localPosition=BoardPoint(4.9f,3.6f);
            BoardLine(swingPivot,"Two swing ropes",new[]{Vector3.zero,new Vector3(0,-3*.15f,0)},"#665f4c",.008f);
            swingSeat=Child(swingPivot,null,new Vector3(0,-3*.15f,0),new Vector3(.65f*.125f,.09f*.15f,.05f),"#a9795a").transform;
            parkCourse[0]=swingPivot.gameObject;
            for(int s=-1;s<=1;s+=2)Child(parkRelief,null,BoardPoint(18.2f+s*.45f,1),new Vector3(.015f,.3f,.035f),"#a39b80");
            CourseBlock(roomRelief,0,10.15f,0,"#939385");CourseBlock(roomRelief,10.3f,19,2.8f,"#939385");
            luggage=NewChild(roomRelief,"Pushable imported luggage");luggage.localPosition=BoardPoint(3.2f,.425f);
            Child(luggage,"book-luggage",Vector3.zero,new Vector3(.95f*.125f,.85f*.15f,.065f),"#a47c58");
            Child(luggage,null,new Vector3(0,.075f,0),new Vector3(.035f,.018f,.04f),"#685a45");roomCourse[0]=luggage.gameObject;
            Child(roomRelief,null,BoardPoint(4.98f,.01f),new Vector3(.9f*.125f,.025f,.06f),"#a07c4c");
            for(int s=-1;s<=1;s+=2)Child(roomRelief,null,BoardPoint(4.98f+s*.36f,.1f),new Vector3(.006f,.013f,.065f),"#e0c285");
            plateGate=NewChild(roomRelief,"Gate lifted by the luggage plate");plateGate.localPosition=BoardPoint(6.18f,1.2f);
            Child(plateGate,null,Vector3.zero,new Vector3(.14f*.125f,2.4f*.15f,.055f),"#877052");
            for(int i=0;i<5;i++)Child(plateGate,null,new Vector3(0,-.14f+i*.07f,-.02f),new Vector3(.05f,.008f,.025f),"#d8c49b");
            BoardLine(roomRelief,"Visible plate to gate linkage",new[]{BoardPoint(4.98f,.12f,-.13f),BoardPoint(5.60f,.12f,-.13f),BoardPoint(5.60f,2.45f,-.13f),BoardPoint(6.18f,2.45f,-.13f)},"#c0a476",.007f);
            var wheelFrame=NewChild(roomRelief,"Wheel circle mapped to the paper plane");wheelFrame.localPosition=BoardPoint(9.15f,1.9f);wheelFrame.localScale=new Vector3(.125f,.15f,1);
            flowerRotor=NewChild(wheelFrame,"Flower wheel spokes");
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;Vector3 v=new Vector3(Mathf.Cos(a)*1.65f,Mathf.Sin(a)*1.65f,0);
                BoardLine(flowerRotor,"Flower stem "+i,new[]{Vector3.zero,v},"#788f71",.075f);
                Child(flowerRotor,null,v,new Vector3(.72f,.48f,.045f),"#bb9c7b",PrimitiveType.Sphere,new Vector3(0,0,i*60));
            }
            Child(flowerRotor,null,Vector3.zero,new Vector3(.6f,.6f,.05f),"#d0b682",PrimitiveType.Cylinder,new Vector3(90,0,0));
            flowerSeat=Child(roomRelief,null,BoardPoint(9.15f,.25f),new Vector3(.75f*.125f,.06f*.15f,.07f),"#dec3a0").transform;
            roomCourse[1]=flowerRotor.gameObject;roomCourse[2]=flowerSeat.gameObject;
            keyDoor=NewChild(roomRelief,"Final door with the matching pendant recess");keyDoor.localPosition=BoardPoint(14.35f,3.95f);
            Child(keyDoor,"pendant-socket",Vector3.zero,new Vector3(.75f*.125f,2.3f*.15f,.065f),"#a18a69");
            var socket=NewChild(keyDoor,"Asymmetric pendant recess");socket.localPosition=new Vector3(0,.015f,-.04f);socket.localScale=Vector3.one*.25f;
            BoardLine(socket,"Pendant negative outline",pendantShape,"#4a524b",.026f);Child(socket,null,Vector3.zero,new Vector3(.11f,.14f,.008f),"#60685a",PrimitiveType.Sphere);
            insertedPendant=NewChild(keyDoor,"Pendant seated in the matching socket");insertedPendant.localPosition=new Vector3(0,.015f,-.05f);insertedPendant.localScale=Vector3.one*.25f;MakePendant(insertedPendant,true);
            token=NewChild(board,"Pendant waits at book edge for manual drag");MakePendant(token,true);token.localScale=Vector3.one*.45f;
            var tokenHit=Child(token,"pendant-token",Vector3.zero,new Vector3(.27f,.3f,.055f),"#bd9c61");tokenHit.GetComponent<Renderer>().enabled=false;
            BoardLine(lineRelief,"One straight line",new[]{BoardPoint(0,0),BoardPoint(19,0)},"#69766f",.012f);
            runner=NewChild(board,"Painted platform actor");
            Child(runner,null,new Vector3(0,.045f,0),new Vector3(.04f,.065f,.035f),"#506e78",PrimitiveType.Capsule);
            Child(runner,null,new Vector3(0,.09f,0),new Vector3(.035f,.035f,.035f),"#4c6570",PrimitiveType.Sphere);
            // White is behind the real foreground paper, so the paper survives every dissolve.
            whiteMask=Group("Camera space white dissolve and bottom-up reveal",Vector3.zero);
            whiteRenderer=Child(whiteMask,null,Vector3.zero,new Vector3(30,30,1),"#ffffff",PrimitiveType.Quad).GetComponent<Renderer>();
            // Runtime Standard keyword changes can lose their variants in a player build.
            // These two retained shaders have fixed, predictable state and need no lighting.
            Shader whiteShader=Shader.Find("Sprites/Default"),paperShader=Shader.Find("Unlit/Color");
            if(whiteShader==null||paperShader==null)throw new InvalidOperationException("Painting requires retained Sprites/Default and Unlit/Color shaders");
            var wm=whiteRenderer.sharedMaterial;wm.shader=whiteShader;wm.mainTexture=Texture2D.whiteTexture;wm.color=Color.white;wm.renderQueue=3000;
            paperRenderer.sharedMaterial.shader=paperShader;
            whiteMask.gameObject.SetActive(false);UpdateScene(0);UpdatePose(0,null);
        }
        Transform NewChild(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        void Hand(Vector3 p){Child(board,null,p,new Vector3(.16f,.11f,.19f),"#c7a58a",PrimitiveType.Sphere);}
        Vector3 BoardPoint(float x,float y,float z=-.09f)=>new Vector3((x-9.5f)*.125f,-.34f+y*.15f,z);
        GameObject CourseBlock(Transform parent,float left,float right,float top,string color)
        {return Child(parent,null,BoardPoint((left+right)/2,top-.22f),new Vector3((right-left)*.125f,.44f*.15f,.045f),color);}
        void BoardLine(Transform parent,string name,Vector3[] points,string color,float width)
        {
            var t=NewChild(parent,name);var l=t.gameObject.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=l.endWidth=width;l.numCapVertices=4;l.numCornerVertices=4;l.sharedMaterial=Material(color);
        }
        void OutlineGap(Transform parent,float left,float right)
        {for(float x=left+.12f;x<right;x+=.5f)BoardLine(parent,"Unfinished pencil trace",new[]{BoardPoint(x,0),BoardPoint(Mathf.Min(x+.19f,right),0)},"#b6af98",.004f);}
        void BuildPark()
        {
            Child(park,null,new Vector3(0,-.12f,4),new Vector3(18,.24f,20),"#8d9c80");
            Child(park,null,new Vector3(0,3.8f,10),new Vector3(22,9,.1f),"#c0cbc4");
            Child(park,null,new Vector3(0,.01f,2),new Vector3(1.4f,.025f,10),"#c0b397");
            for(int i=0;i<7;i++)
            {
                float x=-7+i*2.3f;
                Child(park,null,new Vector3(x,1.3f,7.7f),new Vector3(.18f,2.6f,.2f),"#78765b",PrimitiveType.Cylinder);
                Child(park,null,new Vector3(x,3.1f,7.8f),new Vector3(3,2.5f,2.5f),i%2==0?"#8b9f7e":"#a2ac8a",PrimitiveType.Sphere);
            }
            // Recognisable physical swing and footbridge, no human figure.
            for(int s=-1;s<=1;s+=2)Child(park,null,new Vector3(-2.1f+s*.85f,1.5f,2),new Vector3(.09f,3,.09f),"#857256",PrimitiveType.Cylinder,new Vector3(0,0,s*9));
            Child(park,null,new Vector3(-2.1f,2.98f,2),new Vector3(2.25f,.12f,.12f),"#8c785c");
            for(int s=-1;s<=1;s+=2)Child(park,"swing-target",new Vector3(-2.1f+s*.33f,1.9f,2),new Vector3(.025f,2.1f,.025f),"#b5aa8d",PrimitiveType.Cylinder);
            sources[0]=Child(park,"swing-target",new Vector3(-2.1f,.85f,2),new Vector3(.9f,.10f,.48f),"#ae785d").GetComponent<Renderer>();
            var bridge=NewChild(park,"Real park footbridge");bridge.localPosition=new Vector3(2.35f,.5f,3.7f);
            sources[1]=Child(bridge,"bridge-target",Vector3.zero,new Vector3(2.2f,.16f,1.1f),"#b29a74").GetComponent<Renderer>();
            for(int i=0;i<9;i++)Child(bridge,"bridge-target",new Vector3(-1+i*.25f,.10f,0),new Vector3(.02f,.02f,1.08f),"#756c53");
            for(int s=-1;s<=1;s+=2)
            {
                Child(bridge,"bridge-target",new Vector3(0,.6f,s*.48f),new Vector3(2.3f,.07f,.07f),"#978266");
                for(int k=-1;k<=1;k++)Child(bridge,null,new Vector3(k*.95f,.27f,s*.48f),new Vector3(.06f,.62f,.06f),"#978266");
            }
            Child(park,null,new Vector3(2.35f,.015f,3.7f),new Vector3(3,.02f,5),"#829f9b");
            LightAt(park,new Vector3(-1,4,-1),"Park warm ambient",1.8f);
        }
        void BuildEmptyRoom()
        {
            Child(room,null,new Vector3(0,-.1f,3),new Vector3(8,.2f,10),"#98937f");
            Child(room,null,new Vector3(0,1.8f,6.7f),new Vector3(8,3.6f,.12f),"#aca998");
            for(int s=-1;s<=1;s+=2)Child(room,null,new Vector3(s*4,1.8f,3),new Vector3(.12f,3.6f,7.5f),"#969c8c");
            Child(room,null,new Vector3(-1.7f,1.9f,6.60f),new Vector3(1.7f,1.7f,.05f),"#d7d7bc");
            Child(room,null,new Vector3(-1.7f,1.9f,6.53f),new Vector3(.055f,1.75f,.06f),"#827b64");
            var box=NewChild(room,"Real luggage remaining in the empty room");box.localPosition=new Vector3(-1.75f,.35f,1.3f);
            sources[2]=Child(box,"luggage-target",Vector3.zero,new Vector3(1,.7f,.65f),"#9b7855").GetComponent<Renderer>();
            for(int s=-1;s<=1;s+=2)Child(box,"luggage-target",new Vector3(s*.3f,0,-.34f),new Vector3(.06f,.7f,.02f),"#cfaf7d");
            Child(box,"luggage-target",new Vector3(0,.4f,0),new Vector3(.35f,.1f,.12f),"#5c5d4c");
            Child(room,null,new Vector3(.3f,.57f,2.5f),new Vector3(.7f,1.14f,.65f),"#87775b");
            Child(room,null,new Vector3(.3f,1.17f,2.5f),new Vector3(.22f,.22f,.22f),"#9e9276",PrimitiveType.Cylinder);
            Child(room,"flower-target",new Vector3(.3f,1.57f,2.5f),new Vector3(.03f,.7f,.03f),"#718669",PrimitiveType.Cylinder);
            for(int i=0;i<6;i++)sources[3]=Child(room,"flower-target",new Vector3(.3f+Mathf.Cos(i*Mathf.PI/3)*.19f,1.97f+Mathf.Sin(i*Mathf.PI/3)*.19f,2.47f),new Vector3(.28f,.28f,.075f),"#c2a081",PrimitiveType.Sphere).GetComponent<Renderer>();
            Child(room,null,new Vector3(2.15f,.8f,3.4f),new Vector3(1.4f,.12f,.65f),"#8b795e");
            Child(room,null,new Vector3(2.15f,1.05f,3.55f),new Vector3(.75f,.38f,.16f),"#aea48e",PrimitiveType.Cube,new Vector3(0,0,-10));
            var hidden=NewChild(room,"Pendant partly concealed behind the old frame");hidden.localPosition=new Vector3(2.56f,1.08f,3.25f);hidden.localScale=Vector3.one*1.1f;MakePendant(hidden,false);
            sources[4]=Child(hidden,"pendant-target",Vector3.zero,new Vector3(.3f,.32f,.10f),"#baa16a").GetComponent<Renderer>();sources[4].enabled=false;
            Child(room,null,new Vector3(2.8f,.35f,5.9f),new Vector3(1.4f,.7f,.8f),"#a79a80");
            Child(room,null,new Vector3(2.8f,.75f,5.9f),new Vector3(1.5f,.10f,.87f),"#d2c6aa");
            LightAt(room,new Vector3(-1,3.2f,1),"Empty room slanting light",1.7f);
        }
        void MakePendant(Transform parent,bool tokenShape)
        {
            BoardLine(parent,"Asymmetric brass pendant",pendantShape,"#c6a565",.025f);
            Child(parent,null,new Vector3(-.035f,0,-.013f),new Vector3(.08f,.10f,.035f),"#5f8a89",PrimitiveType.Sphere);
            BoardLine(parent,"Pendant short chain",new[]{new Vector3(.025f,.14f,0),new Vector3(.05f,.24f,0),new Vector3(-.02f,.31f,0),new Vector3(-.10f,.24f,0)},"#c6a565",.009f);
        }
        Transform BuildHospital()
        {
            Child(hospital,null,new Vector3(0,-.13f,3.5f),new Vector3(10,.25f,12),"#b9c4bd");
            Child(hospital,null,new Vector3(0,2.2f,7),new Vector3(10,4.4f,.1f),"#dce0d4");
            for(int s=-1;s<=1;s+=2)Child(hospital,null,new Vector3(s*4.5f,2.2f,3),new Vector3(.1f,4.4f,8),"#c4cfc6");
            var bed=NewChild(hospital,"Hospital bed");bed.localPosition=new Vector3(.45f,0,2.9f);
            Child(bed,null,new Vector3(0,.64f,0),new Vector3(1.6f,.22f,2.5f),"#a5b7b0");
            Child(bed,null,new Vector3(0,.82f,0),new Vector3(1.5f,.2f,2.4f),"#e0dfcf");
            Child(bed,null,new Vector3(0,1.24f,.9f),new Vector3(1.5f,.95f,.13f),"#c8cdc0");
            for(int s=-1;s<=1;s+=2)Child(bed,null,new Vector3(s*.76f,.42f,0),new Vector3(.07f,.8f,2.4f),"#7f9792");
            var girl=NewChild(hospital,"Seated girl - no full face is ever revealed");girl.localPosition=new Vector3(.45f,.91f,3.15f);
            Child(girl,null,new Vector3(0,.43f,0),new Vector3(.50f,.85f,.33f),"#bcc5bb",PrimitiveType.Capsule);
            Child(girl,null,new Vector3(0,.0f,-.6f),new Vector3(.85f,.22f,1.3f),"#d6d7c7",PrimitiveType.Capsule,new Vector3(90,0,0));
            Child(girl,null,new Vector3(0,.98f,0),new Vector3(.34f,.39f,.30f),"#737f77",PrimitiveType.Sphere);
            // Hair conceals facial features even during frame-by-frame inspection.
            Child(girl,null,new Vector3(-.08f,.93f,-.08f),new Vector3(.40f,.54f,.31f),"#566560",PrimitiveType.Capsule,new Vector3(0,0,-7));
            Child(girl,null,new Vector3(.25f,.42f,-.10f),new Vector3(.11f,.65f,.12f),"#bdc5b9",PrimitiveType.Capsule,new Vector3(0,0,15));
            var marker=NewChild(girl,"NoseThreshold marker - intentionally not visible");marker.localPosition=new Vector3(0,.98f,-.18f);
            Child(hospital,null,new Vector3(-1.7f,1.6f,3.8f),new Vector3(.055f,2.7f,.055f),"#899d94",PrimitiveType.Cylinder);
            Child(hospital,null,new Vector3(-1.7f,2.55f,3.8f),new Vector3(.32f,.5f,.10f),"#d0d9c7");
            Child(hospital,null,new Vector3(.45f,2.05f,6.8f),new Vector3(3.5f,3,.06f),"#f4efd8");
            LightAt(hospital,new Vector3(.45f,2.7f,5.5f),"Backlight behind the seated silhouette",3.3f);return marker;
        }
        void LightAt(Transform parent,Vector3 p,string name,float intensity)
        {var t=NewChild(parent,name);t.localPosition=p;var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=15;l.intensity=intensity;l.color=new Color(1,.92f,.76f);}
        public override void Activate()
        {
            base.Activate();Defocus=View.GetComponent<PaintingDefocus>();if(Defocus==null)Defocus=View.gameObject.AddComponent<PaintingDefocus>();Defocus.Begin();
        }
        public override void Deactivate(){if(Defocus!=null)Defocus.End();base.Deactivate();}
        public override void SuspendInput(){State.SuspendInput();dragging=false;GazeTarget="";UpdateScene(0);}
        public void PrepareQAFrame()
        {
            UpdateScene(0);UpdatePose(.5f,null);ApplyPose();UpdatePose(0,null);ApplyPose();
            if(Defocus!=null)Defocus.SetFocusHeld(State.BoardOpen||State.Act>=2,.5f);
        }
        public override void Tick(float dt,WBInput input)
        {
            clock+=dt;pointer=input.Pointer;
            if(State.Act<2&&(State.Stage=="park"||State.Stage=="room")&&!dragging)
            {
                if(input.Pointer.y>724&&!edgeLatch){State.SetBoardOpen(true);edgeLatch=true;}
                if(input.Pointer.y<35&&!edgeLatch){State.SetBoardOpen(false);edgeLatch=true;}
                if(input.Pointer.y>90&&input.Pointer.y<675)edgeLatch=false;
            }
            UpdatePose(dt,input);
            int gaze=-1;
            if(!State.BoardOpen&&State.Act<2)
            {
                Vector2 native=new Vector2(input.Pointer.x/1280*Screen.width,(1-input.Pointer.y/760)*Screen.height);
                GazeTarget=Pick(View.ScreenPointToRay(native));for(int i=0;i<ids.Length;i++)if(GazeTarget==ids[i])gaze=i;
            }
            else GazeTarget="";
            State.SetGaze(gaze,input.Down);State.Tick(dt,input);UpdateScene(dt);
            if(Defocus!=null)Defocus.SetFocusHeld(input.Down||State.BoardOpen||State.Act>=2,dt);
            UpdatePose(dt,null);
            if(previousStage!=State.Stage){previousStage=State.Stage;CaptureStage?.Invoke("painting-"+previousStage,this);}
        }
        void UpdatePose(float dt,WBInput input)
        {
            if(State.Act!=lastAct){lastAct=State.Act;yaw=pitch=0;}
            if(input!=null&&State.Act<2&&!State.BoardOpen&&!input.Down)
            {
                yaw=Mathf.Clamp(yaw+Mathf.Clamp(input.PointerDelta.x,-24,24)*.075f,-36,36);
                pitch=Mathf.Clamp(pitch+Mathf.Clamp(input.PointerDelta.y,-24,24)*.06f,-18,18);
            }
            Eye=ObserverAnchor;viewRotation=Quaternion.Euler(pitch,yaw,0);Look=Eye+viewRotation*Vector3.forward*6;
            BoardBlend=Mathf.MoveTowards(BoardBlend,State.BoardOpen?1:0,dt/ .35f);
            float boardY=State.Act>=2?-.72f:Mathf.Lerp(-1.29f,-.02f,BoardBlend);
            board.localScale=Vector3.one*(State.Act>=2?.8f:1);
            // The book becomes a low, slanted tabletop sheet; it must not cover the bed.
            board.position=Eye+viewRotation*new Vector3(0,boardY,State.Act>=2?1.5f:1.6f);
            board.rotation=viewRotation*(State.Act>=2?Quaternion.Euler(45,0,0):Quaternion.identity);
            if(State.Act>=2)
            {
                float aspect=View!=null?View.aspect:1.684f;
                float halfH=2.7f*Mathf.Tan(Fov*.5f*Mathf.Deg2Rad);
                whiteMask.rotation=viewRotation;
                if(State.MemoryAbruptCut||State.Stage!="line")
                {whiteMask.position=Eye+viewRotation*new Vector3(0,0,2.7f);whiteRenderer.transform.localScale=new Vector3(halfH*aspect*2.1f,halfH*2.1f,.02f);}
                else
                {
                    // reveal edge starts at the floor and reaches the actual nose marker once.
                    // Update the real camera before sampling, including on the first hospital frame.
                    View.transform.SetPositionAndRotation(Eye,viewRotation);View.fieldOfView=Fov;
                    Vector3 projected=View.WorldToViewportPoint(nose.position);float target=Mathf.Clamp(projected.y,.05f,.95f);
                    NoseViewportY=target;NoseProjectionSamples++;
                    HospitalRevealEdge=Mathf.Lerp(0,target,State.LineProgress);
                    float lower=(HospitalRevealEdge-.5f)*2*halfH;
                    float top=halfH*1.05f;float height=top-lower;
                    whiteMask.position=Eye+viewRotation*new Vector3(0,(top+lower)/2,2.7f);
                    whiteRenderer.transform.localScale=new Vector3(halfH*aspect*2.1f,height,.02f);
                    if(State.LineProgress>=.9999f){State.ObserveHospitalReveal(HospitalRevealEdge>=target-.001f);hospital.gameObject.SetActive(false);UpdatePose(0,null);}
                }
            }
            else {whiteMask.rotation=viewRotation;whiteMask.position=Eye+viewRotation*new Vector3(0,0,2.7f);whiteRenderer.transform.localScale=new Vector3(30,30,.02f);}
        }
        void UpdateScene(float dt)
        {
            park.gameObject.SetActive(State.Act==0);room.gameObject.SetActive(State.Act==1);hospital.gameObject.SetActive(State.Act==2&&!State.MemoryAbruptCut);
            parkRelief.gameObject.SetActive(State.Act==0);roomRelief.gameObject.SetActive(State.Act==1);lineRelief.gameObject.SetActive(State.Act>=2);
            binding.SetActive(State.Act<2);paperRenderer.sharedMaterial.color=State.Act>=2?new Color(.96f,.95f,.89f):new Color(.91f,.87f,.77f);
            lineRelief.localPosition=State.Act>=2?Vector3.up*.6f:Vector3.zero;
            parkCourse[0].SetActive(State.Painted[0]);parkCourse[1].SetActive(State.Painted[1]);
            roomCourse[0].SetActive(State.Painted[2]);roomCourse[1].SetActive(State.Painted[3]);roomCourse[2].SetActive(State.Painted[3]);
            swingPivot.localEulerAngles=new Vector3(0,0,State.SwingAngle*Mathf.Rad2Deg);
            // With unequal x/y book scale the seat is positioned independently so it exactly matches collision math.
            swingSeat.position=parkRelief.TransformPoint(BoardPoint(State.SwingSeat.x,State.SwingSeat.y));
            flowerRotor.localEulerAngles=new Vector3(0,0,State.FlowerAngle*Mathf.Rad2Deg);
            flowerSeat.localPosition=BoardPoint(State.FlowerSeat.x,State.FlowerSeat.y);flowerSeat.localRotation=Quaternion.identity;
            luggage.localPosition=BoardPoint(State.LuggageX,.425f);
            plateGate.localPosition=BoardPoint(6.18f,State.GateOpen?3.8f:1.2f);
            keyDoor.localEulerAngles=new Vector3(0,State.KeyInserted?-78:0,0);
            insertedPendant.gameObject.SetActive(State.KeyInserted);
            token.gameObject.SetActive(State.Act==1&&State.Painted[4]&&!State.KeyInserted);
            if(!dragging)token.localPosition=new Vector3(1.10f,.42f,-.15f);
            runner.localPosition=BoardPoint(State.Position.x,State.Position.y,-.14f)+(State.Act>=2?Vector3.up*.6f:Vector3.zero);
            float alpha=State.Act>=2?1:State.WhiteAmount;whiteMask.gameObject.SetActive(alpha>.001f);
            whiteRenderer.sharedMaterial.color=new Color(1,1,1,alpha);
            if(State.MemoryAbruptCut){whiteMask.gameObject.SetActive(true);whiteRenderer.sharedMaterial.color=Color.white;}
        }
        public override void Press(string id,Ray ray,int button)
        {
            if(button==0&&id=="pendant-token"&&State.BeginPendantDrag()){dragging=true;Drag(ray);}
        }
        void Drag(Ray ray)
        {
            Plane p=new Plane(board.forward,board.TransformPoint(new Vector3(0,0,-.18f)));
            if(p.Raycast(ray,out float d))token.position=ray.GetPoint(d);
        }
        public override void Move(Ray ray){if(dragging)Drag(ray);}
        public override void Release(string id,Ray ray)
        {
            if(!dragging)return;
            Drag(ray);Vector3 p=keyDoor.InverseTransformPoint(token.position);
            bool over=id=="pendant-socket"||Mathf.Abs(p.x)<.065f&&Mathf.Abs(p.y-.015f)<.065f;
            State.EndPendantDrag(over);dragging=false;UpdateScene(0);
        }
        public override void DrawOverlay()
        {
            if(State.BoardOpen||State.Act>=2||State.FocusProgress<=0)return;
            // Pencil-colored focus arc; no object labels, subtitles or task text.
            Color old=GUI.color;GUI.color=new Color(.94f,.86f,.66f,.7f);
            int n=Mathf.FloorToInt(State.FocusProgress/PaintingChapter.FocusSeconds*28);
            for(int i=0;i<n;i++){float a=i/28f*Mathf.PI*2;GUI.DrawTexture(new Rect(pointer.x+Mathf.Sin(a)*19,pointer.y-Mathf.Cos(a)*19,3,3),Texture2D.whiteTexture);}GUI.color=old;
        }
    }
}
