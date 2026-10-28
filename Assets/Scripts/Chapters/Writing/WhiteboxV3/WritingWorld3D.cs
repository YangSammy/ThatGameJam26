using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    // Both halves are perspective cameras rendering editable spatial geometry.
    public sealed class WritingWorld3D : WorldChapter3D
    {
        public readonly WritingChapter State;
        public readonly Camera MemoryCamera;
        public int InkStrokeCount => ink.Count;
        public bool PaperGrabbed => State.PaperDragging;
        public Vector3 PaperCenter => World(new Vector3(-.075f,1.015f,.12f));
        public Vector3 BinCenter => World(binAt+Vector3.up*.72f);
        public Action<string,WorldChapter3D> CaptureStage;
        const int MemoryLayer=26;
        readonly Vector3 memoryOffset=new Vector3(12,0,0), binAt=new Vector3(.70f,0,-.87f);
        readonly Transform memory, player, girl, penHand, paperBall, timerHand, memoryDoor, girlHair;
        readonly GameObject farewellHalo,farewellPath;
        readonly List<GameObject> nearWalls=new List<GameObject>();
        readonly Transform[] items=new Transform[4], carry=new Transform[4];
        readonly GameObject sheet;
        readonly Renderer doorGlow;
        readonly List<LineRenderer> ink=new List<LineRenderer>();
        readonly List<Vector3> stroke=new List<Vector3>();
        LineRenderer currentStroke;
        int mask,previousDraft,previousPage,previousRow=-1;
        float previousInk,clock; bool writingHeld;
        string phase;

        public WritingWorld3D(WBMain host,WritingChapter model):base(host,model,2)
        {
            State=model;previousDraft=State.Draft;phase=State.Phase;
            Room(5,6);Eye=World(new Vector3(.22f,2.25f,-1.85f));Look=World(new Vector3(.18f,.85f,-.06f));Fov=61;
            Part(null,new Vector3(0,.93f,.15f),new Vector3(2.65f,.12f,1.65f),"#87715a");
            for(int s=-1;s<=1;s+=2)Part(null,new Vector3(s*1.08f,.45f,.52f),new Vector3(.1f,.9f,.1f),"#685b4e");
            sheet=Part("paper",new Vector3(-.075f,1.001f,.12f),new Vector3(.95f,.012f,.72f),"#eee6cf");
            Part(null,new Vector3(-.075f,.994f,.12f),new Vector3(.97f,.012f,.74f),"#bdb29b");
            Part(null,new Vector3(-.82f,1.07f,.66f),new Vector3(.13f,.15f,.13f),"#293431");
            Part("bin",binAt+new Vector3(0,.35f,0),new Vector3(.48f,.05f,.44f),"#4d5855");
            for(int s=-1;s<=1;s+=2)
            {
                Part("bin-side-"+s,binAt+new Vector3(s*.24f,.54f,0),new Vector3(.027f,.38f,.46f),"#6e7770");
                Part("bin-edge-"+s,binAt+new Vector3(0,.54f,s*.22f),new Vector3(.48f,.38f,.027f),"#6e7770");
            }
            Part("bin-rim",binAt+new Vector3(0,.74f,.22f),new Vector3(.53f,.04f,.035f),"#b7bdab");
            paperBall=Group("Crumpled sheet in hand",Vector3.zero);
            Child(paperBall,null,Vector3.zero,new Vector3(.17f,.13f,.16f),"#e6ddc6",PrimitiveType.Sphere);
            for(int i=0;i<6;i++)Child(paperBall,null,new Vector3(Mathf.Sin(i*2.5f)*.05f,.02f,Mathf.Cos(i*2.5f)*.05f),new Vector3(.09f,.07f,.08f),i%2==0?"#c4b79d":"#eee6cf",PrimitiveType.Cube,new Vector3(i*23,i*41,i*17));
            paperBall.gameObject.SetActive(false);
            penHand=Group("Right hand continues writing",new Vector3(-.4f,1.025f,.35f));
            Child(penHand,null,new Vector3(.07f,.05f,-.1f),new Vector3(.18f,.08f,.13f),"#c9a587",PrimitiveType.Sphere);
            Child(penHand,null,new Vector3(.16f,.055f,-.23f),new Vector3(.13f,.095f,.29f),"#889394",PrimitiveType.Capsule,new Vector3(72,0,-28));
            Child(penHand,null,new Vector3(.025f,.059f,-.02f),new Vector3(.018f,.018f,.28f),"#303a3c",PrimitiveType.Cylinder,new Vector3(62,-22,0));
            var timer=Group("Physical sixty second kitchen timer",new Vector3(.77f,1.09f,.63f));
            Child(timer,null,Vector3.zero,new Vector3(.25f,.09f,.25f),"#c6baa3",PrimitiveType.Cylinder);
            Child(timer,null,new Vector3(0,.095f,0),new Vector3(.21f,.01f,.21f),"#e7dfca",PrimitiveType.Cylinder);
            for(int i=0;i<12;i++)Child(timer,null,new Vector3(Mathf.Sin(i*Mathf.PI/6)*.086f,.108f,Mathf.Cos(i*Mathf.PI/6)*.086f),new Vector3(.009f,.006f,.018f),"#5d635d",PrimitiveType.Cube,new Vector3(0,i*30,0));
            timerHand=new GameObject("Timer pointer").transform;timerHand.SetParent(timer,false);timerHand.localPosition=Vector3.up*.115f;
            Child(timerHand,null,new Vector3(0,0,.04f),new Vector3(.009f,.009f,.085f),"#ac5c49");
            memory=Group("Right side three dimensional recollection",memoryOffset);
            BuildMemory();
            player=Person("Memory self",memory,new Vector3(0,0,-1.9f),"#67989e",false);
            girl=Person("Backlit farewell silhouette",memory,new Vector3(0,0,-2.92f),"#3f4a4d",true);
            girlHair=girl.Find("Long hair silhouette");
            Shader silhouetteShader=Shader.Find("Unlit/Color");
            if(silhouetteShader==null)throw new InvalidOperationException("Writing silhouette requires retained Unlit/Color shader");
            foreach(var renderer in girl.GetComponentsInChildren<Renderer>(true))
            {renderer.sharedMaterial.shader=silhouetteShader;renderer.sharedMaterial.color=new Color(.07f,.13f,.15f);}
            girl.gameObject.SetActive(false);
            for(int i=0;i<4;i++)
            {
                Vector2 p=WritingChapter.ItemPositions[i];float itemY=i==3?.24f:i==1?1.01f:1.13f;
                items[i]=MakeItem(i,memory,new Vector3(p.x,itemY,p.y),"Collectible "+i);
                carry[i]=MakeItem(i,player,new Vector3(-.25f+i*.16f,.60f,-.22f),"Held "+i);carry[i].localScale=Vector3.one*.65f;
            }
            var doorway=Child(memory,null,new Vector3(0,.025f,-2.65f),new Vector3(1.1f,.05f,.65f),"#dfd3aa");doorGlow=doorway.GetComponent<Renderer>();
            for(int s=-1;s<=1;s+=2)Child(memory,null,new Vector3(s*.62f,1.05f,-2.85f),new Vector3(.08f,2.1f,.09f),"#c5b99a");
            memoryDoor=new GameObject("Hinged foyer door opens for the farewell").transform;memoryDoor.SetParent(memory,false);memoryDoor.localPosition=new Vector3(-.57f,0,-2.91f);
            Child(memoryDoor,null,new Vector3(.57f,1.01f,0),new Vector3(1.14f,2.02f,.065f),"#84775d");
            Child(memoryDoor,null,new Vector3(1.02f,1.01f,-.055f),new Vector3(.12f,.04f,.045f),"#c3a875");
            farewellPath=Child(memory,null,new Vector3(0,-.08f,-3.4f),new Vector3(1.5f,.16f,1.3f),"#c5bea1");
            farewellHalo=Child(memory,null,new Vector3(0,1.30f,-1.55f),new Vector3(1.45f,2.5f,1),"#fff2c8",PrimitiveType.Quad);
            farewellHalo.name="Blown-out backlight behind the featureless farewell silhouette";
            farewellHalo.GetComponent<Renderer>().sharedMaterial.shader=silhouetteShader;
            var halo=new GameObject("Farewell backlight outside the doorway");halo.transform.SetParent(memory,false);halo.transform.localPosition=new Vector3(0,1.6f,-3.25f);
            var rim=halo.AddComponent<Light>();rim.type=LightType.Point;rim.range=3;rim.intensity=2.2f;rim.color=new Color(1,.92f,.72f);rim.cullingMask=1<<MemoryLayer;
            SetLayer(memory,MemoryLayer);
            var cam=new GameObject("Right memory perspective camera");cam.transform.SetParent(Root.transform,false);MemoryCamera=cam.AddComponent<Camera>();
            MemoryCamera.transform.position=World(memoryOffset+new Vector3(0,8.3f,-4.7f));MemoryCamera.transform.LookAt(World(memoryOffset+new Vector3(0,.2f,0)));
            MemoryCamera.fieldOfView=47;MemoryCamera.nearClipPlane=.05f;MemoryCamera.farClipPlane=24;MemoryCamera.clearFlags=CameraClearFlags.SolidColor;MemoryCamera.backgroundColor=new Color(.13f,.17f,.17f);
            MemoryCamera.cullingMask=1<<MemoryLayer;MemoryCamera.depth=View!=null?View.depth+1:1;MemoryCamera.rect=new Rect(.5f,0,.5f,1);MemoryCamera.enabled=false;
            RestoreInk();Refresh(0);
        }
        void RestoreInk()
        {
            // Rebuild the current sheet when a chapter is reopened or inspected by validation.
            for(int row=0;row<=State.Row;row++)
            {
                float end=row<State.Row?425:State.LineExtent;if(end<.1f)continue;
                var points=new List<Vector3>();
                for(float x=0;x<=end;x+=6)points.Add(new Vector3(-.51f+x/425*.86f,1.016f,.405f-row*.062f+Mathf.Sin(x*.3f)*.005f));
                var l=Line("Restored handwritten marks",points.ToArray(),"#344442",.004f);ink.Add(l);
            }
        }
        void BuildMemory()
        {
            Child(memory,null,new Vector3(0,-.08f,0),new Vector3(5.7f,.16f,5.7f),"#8b8574");
            Child(memory,null,new Vector3(-2.85f,.65f,0),new Vector3(.12f,1.3f,5.7f),"#9c9e8e");
            Child(memory,null,new Vector3(2.85f,.65f,0),new Vector3(.12f,1.3f,5.7f),"#8b9488");
            Child(memory,null,new Vector3(0,1.05f,2.85f),new Vector3(5.7f,2.1f,.13f),"#b3ad96");
            for(int s=-1;s<=1;s+=2)nearWalls.Add(Child(memory,null,new Vector3(s*1.83f,.65f,-2.85f),new Vector3(2.15f,1.3f,.12f),"#9c9e8e"));
            Child(memory,null,new Vector3(-1.975f,.3f,1.725f),new Vector3(1.25f,.6f,1.65f),"#6b7b75");
            Child(memory,null,new Vector3(-1.975f,.64f,1.60f),new Vector3(1.2f,.15f,1.85f),"#b9b09a");
            Child(memory,null,new Vector3(-1.975f,.77f,2.13f),new Vector3(.95f,.15f,.4f),"#ded2b8");
            Child(memory,null,new Vector3(1.9f,.67f,1.925f),new Vector3(1.3f,.13f,1.15f),"#746551");
            Child(memory,null,new Vector3(0,.42f,.225f),new Vector3(1,.16f,.65f),"#79694f");
            Child(memory,null,new Vector3(-1.90f,.55f,-.35f),new Vector3(.62f,1.1f,.63f),"#7f765e");
            Child(memory,null,new Vector3(1.98f,.55f,.8f),new Vector3(.7f,1.1f,.6f),"#9c8c70");
            Child(memory,null,new Vector3(-.8f,.5f,2.15f),new Vector3(.7f,1,.55f),"#8c7b61");
            Child(memory,null,new Vector3(-.8f,1.65f,2.75f),new Vector3(1.35f,1.05f,.06f),"#d7d7ba");
            Child(memory,null,new Vector3(-.8f,1.65f,2.70f),new Vector3(.04f,1.05f,.05f),"#706c5b");
            Child(memory,null,new Vector3(1.80f,.05f,-1.65f),new Vector3(.9f,.1f,.7f),"#b5a588");
            var lamp=new GameObject("Recollection diffuse light");lamp.transform.SetParent(memory,false);lamp.transform.localPosition=new Vector3(0,4,0);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=2.6f;light.color=new Color(1,.86f,.66f);light.cullingMask=1<<MemoryLayer;
        }
        Transform Person(string name,Transform parent,Vector3 p,string color,bool female)
        {
            var group=new GameObject(name).transform;group.SetParent(parent,false);group.localPosition=p;
            Child(group,null,new Vector3(0,.61f,0),new Vector3(.32f,.55f,.22f),color,PrimitiveType.Capsule);
            Child(group,null,new Vector3(0,1.02f,0),new Vector3(.23f,.26f,.23f),female?"#303b3d":"#bfaa8b",PrimitiveType.Sphere);
            for(int s=-1;s<=1;s+=2)Child(group,null,new Vector3(s*.1f,.21f,0),new Vector3(.105f,.42f,.11f),"#46585b",PrimitiveType.Capsule);
            if(female)
            {
                var hair=new GameObject("Long hair silhouette").transform;hair.SetParent(group,false);hair.localPosition=new Vector3(0,1.04f,0);
                Child(hair,null,new Vector3(0,-.15f,-.055f),new Vector3(.31f,.52f,.25f),"#283437",PrimitiveType.Capsule);
                Child(hair,null,new Vector3(-.13f,-.19f,-.09f),new Vector3(.09f,.40f,.10f),"#283437",PrimitiveType.Capsule,new Vector3(0,0,-9));
                Child(group,null,new Vector3(0,.6f,-.13f),new Vector3(.3f,.24f,.18f),"#a28c6d");
                // No exposed facial geometry; a bright door halo carries the gesture.
            }
            return group;
        }
        Transform MakeItem(int i,Transform parent,Vector3 p,string name)
        {
            var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=p;
            if(i==0)
            {
                Child(g,null,Vector3.zero,new Vector3(.12f,.15f,.045f),"#bda46c",PrimitiveType.Sphere);
                Child(g,null,new Vector3(.015f,.13f,0),new Vector3(.018f,.18f,.018f),"#c4ac71",PrimitiveType.Cylinder);
                Child(g,null,new Vector3(-.025f,.025f,-.028f),new Vector3(.035f,.035f,.014f),"#598688",PrimitiveType.Sphere);
            }
            if(i==1)
            {
                Child(g,null,new Vector3(0,.1f,0),new Vector3(.025f,.40f,.025f),"#738361",PrimitiveType.Cylinder);
                for(int j=0;j<5;j++)Child(g,null,new Vector3(Mathf.Sin(j*1.256f)*.07f,.31f+Mathf.Cos(j*1.256f)*.07f,0),new Vector3(.105f,.10f,.04f),"#c49d86",PrimitiveType.Sphere);
            }
            if(i==2)
            {
                Child(g,null,Vector3.zero,new Vector3(.3f,.04f,.2f),"#e3d7b9");
                Child(g,null,new Vector3(0,.028f,0),new Vector3(.055f,.01f,.055f),"#a56852",PrimitiveType.Cylinder);
            }
            if(i==3)
            {
                Child(g,null,new Vector3(0,.02f,0),new Vector3(.42f,.32f,.23f),"#8b7155");
                for(int s=-1;s<=1;s+=2)Child(g,null,new Vector3(s*.13f,.025f,-.125f),new Vector3(.035f,.32f,.02f),"#d0b58b");
                Child(g,null,new Vector3(0,.21f,0),new Vector3(.18f,.055f,.06f),"#4d5248");
            }
            return g;
        }
        static void SetLayer(Transform root,int layer){foreach(Transform child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=layer;}
        public override void Activate()
        {
            base.Activate();if(View!=null){mask=View.cullingMask;View.cullingMask&=~(1<<MemoryLayer);View.rect=new Rect(0,0,.5f,1);}
            MemoryCamera.enabled=true;
        }
        public override void Deactivate(){MemoryCamera.enabled=false;if(View!=null)View.cullingMask=mask;base.Deactivate();}
        public override void SuspendInput()
        {
            State.SuspendInput();writingHeld=false;currentStroke=null;paperBall.gameObject.SetActive(false);sheet.SetActive(true);
            foreach(var l in ink)if(l!=null)l.gameObject.SetActive(true);
        }
        public override Ray PointerRay(Vector2 p)=>p.x>=Screen.width*.5f?MemoryCamera.ScreenPointToRay(p):View.ScreenPointToRay(p);
        Vector2 PaperCoordinates(Ray ray)
        {
            Vector3 p=Point(ray,1.014f)-Origin;
            return new Vector2(WritingChapter.Paper.xMin+(p.x+.55f)/.95f*WritingChapter.Paper.width,WritingChapter.Paper.yMin+(.48f-p.z)/.72f*WritingChapter.Paper.height);
        }
        public override void Press(string id,Ray ray,int button)
        {
            if(button!=0)return;
            if(id=="paper")
            {
                State.PointerDown(PaperCoordinates(ray),0);writingHeld=State.Phase!="crumple";
                if(State.PaperDragging){sheet.SetActive(false);foreach(var l in ink)l.gameObject.SetActive(false);paperBall.gameObject.SetActive(true);}
            }
        }
        public override void Move(Ray ray)
        {
            if(State.PaperDragging){paperBall.position=Point(ray,1.10f);return;}
            if(!writingHeld)return;
            Vector2 p=PaperCoordinates(ray);float before=State.WritingProgress;State.PointerMove(p);
            if(WritingChapter.Paper.Contains(p))penHand.position=Point(ray,1.025f);
            if(State.WritingProgress>before)
            {
                if(currentStroke==null||previousRow!=State.Row||previousPage!=State.PageSerial)NewStroke();
                float x=-.51f+State.LineExtent/425*.86f;
                float z=.405f-State.Row*.062f+Mathf.Sin(State.WritingProgress*.3f)*.005f;
                stroke.Add(new Vector3(x,1.016f,z));currentStroke.positionCount=stroke.Count;currentStroke.SetPositions(stroke.ToArray());
            }
            previousRow=State.Row;previousPage=State.PageSerial;
        }
        void NewStroke()
        {
            if(previousPage!=State.PageSerial)ClearInk();
            currentStroke=Line("Handwritten ink",new Vector3[0],"#344442",.004f);ink.Add(currentStroke);stroke.Clear();
            stroke.Add(new Vector3(-.51f,1.016f,.405f-State.Row*.062f));
        }
        void ClearInk(){foreach(var l in ink)if(l!=null)ReleaseObject(l.gameObject);ink.Clear();stroke.Clear();currentStroke=null;}
        public override void Release(string id,Ray ray)
        {
            bool was=State.PaperDragging;State.PointerUp(was&&id.StartsWith("bin")?WritingChapter.Bin.center:PaperCoordinates(ray));writingHeld=false;currentStroke=null;
            if(was){paperBall.gameObject.SetActive(false);sheet.SetActive(true);if(State.Phase=="crumple")foreach(var l in ink)l.gameObject.SetActive(true);}
        }
        public override void Tick(float dt,WBInput input)
        {
            State.Tick(dt,input);clock+=dt;Refresh(dt);
            if(phase!=State.Phase){phase=State.Phase;CaptureStage?.Invoke("writing-"+phase,this);}
        }
        void Refresh(float dt)
        {
            if(previousDraft!=State.Draft){previousDraft=State.Draft;ClearInk();sheet.SetActive(true);writingHeld=false;}
            player.localPosition=new Vector3(State.Player.x,0,State.Player.y);
            for(int i=0;i<4;i++)
            {
                items[i].gameObject.SetActive(!State.HasItem(i));carry[i].gameObject.SetActive(State.HasItem(i));
                items[i].localEulerAngles=new Vector3(0,Mathf.Sin(clock*.8f+i)*9,0);
            }
            bool farewell=State.Phase=="farewell"||State.Completed;girl.gameObject.SetActive(farewell);
            foreach(var wall in nearWalls)wall.SetActive(!farewell);
            farewellHalo.SetActive(farewell);farewellPath.SetActive(farewell);
            memoryDoor.localEulerAngles=new Vector3(0,farewell?-112:0,0);
            if(farewell)
            {
                float t=Mathf.Clamp01(State.FarewellProgress/1.2f);
                girl.localPosition=new Vector3(-.15f,Mathf.Sin(t*24)*.015f,-2.12f-t*1.48f);
                girl.localEulerAngles=new Vector3(0,Mathf.Sin(t*Mathf.PI)*28,Mathf.Sin(t*20)*1.5f);
                girlHair.localEulerAngles=new Vector3(0,-Mathf.Sin(t*Mathf.PI)*17,Mathf.Sin(t*18)*5);
            }
            if(MemoryCamera!=null)
            {
                Vector3 wantedEye=World(memoryOffset+(farewell?new Vector3(0,4.9f,-5.8f):new Vector3(0,8.3f,-4.7f)));
                Vector3 wantedLook=World(memoryOffset+(farewell?new Vector3(0,.85f,-2.25f):new Vector3(0,.2f,0)));
                float blend=dt<=0?1:1-Mathf.Exp(-dt*4);
                MemoryCamera.transform.position=Vector3.Lerp(MemoryCamera.transform.position,wantedEye,blend);
                MemoryCamera.transform.rotation=Quaternion.Slerp(MemoryCamera.transform.rotation,Quaternion.LookRotation(wantedLook-wantedEye),blend);
                MemoryCamera.fieldOfView=Mathf.Lerp(MemoryCamera.fieldOfView,farewell?44:47,blend);
            }
            timerHand.localEulerAngles=new Vector3(0,(1-State.Remaining/WritingChapter.Duration)*360,0);
            penHand.gameObject.SetActive(State.Phase!="crumple"&&!State.PaperDragging);
            Color c=State.AllCollected?new Color(1,.94f,.69f):new Color(.65f,.66f,.56f);doorGlow.sharedMaterial.color=c;
            if(State.Phase=="crumple")sheet.transform.localEulerAngles=new Vector3(0,0,Mathf.Sin(clock*2)*1.5f);
            else sheet.transform.localRotation=Quaternion.identity;
            previousInk=State.WritingProgress;
        }
        public override void DrawOverlay()
        {
            // A wordless seam keeps the two simultaneous spaces readable.
            Color previous=GUI.color;GUI.color=new Color(.09f,.12f,.12f);GUI.DrawTexture(new Rect(638,0,4,760),Texture2D.whiteTexture);GUI.color=previous;
        }
    }
}
