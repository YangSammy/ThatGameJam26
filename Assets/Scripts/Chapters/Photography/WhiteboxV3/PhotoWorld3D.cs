using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox.V3
{
    // A deliberately plain, spatial prototype. Cameras, cartridges, controls, hands,
    // chemistry, negatives and final photographs are independent 3D objects.
    public sealed class PhotoWorld3D : WorldChapter3D
    {
        public enum Inspection { Walk, Meter, Body, Finder, Aperture, Shutter, Darkroom }
        readonly PhotoChapter p;
        public Inspection Mode { get; private set; }
        public readonly Texture2D[] ReferenceImages = new Texture2D[3];
        public readonly Texture2D[] ShotTextures = new Texture2D[3];
        readonly Texture2D[] finalPrints = new Texture2D[3];
        public override Texture2D[] CompletionImages => p.Completed ? finalPrints : null;
        public int NativeActionCount { get; private set; }
        public int NativeCaptureCount { get; private set; }
        public bool NativePhotoReady => p.Completed && finalPrints[0] != null && finalPrints[1] != null && finalPrints[2] != null;
        public PhotoChapter.CameraSettings LastShot => p.Captured;
        public Vector2 VirtualPointer => cursor;
        readonly Transform[] girls = new Transform[3], heads = new Transform[3], arms = new Transform[3];
        readonly Transform[] printBoards = new Transform[3];
        readonly Renderer[] printFaces = new Renderer[3];
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        readonly List<GameObject> cameraInternals = new List<GameObject>();
        readonly Dictionary<string, Vector3> homes = new Dictionary<string, Vector3>();
        readonly Vector3 cameraAt = new Vector3(0,1.25f,-4.3f);
        readonly Vector3 tankAt = new Vector3(-4.15f,1.22f,-4.55f);
        Transform cameraRig, backHinge, meterRig, darkroomRig, hand, foregroundHand;
        TextMesh apertureMark, shutterMark, focusMark, frameMark, meterMark;
        GameObject leaderBand, loadedCartridge, negativeStrip, contactPaper, tankLid, pouring;
        Camera photoCamera;
        RenderTexture captureTarget;
        Material liquidMaterial;
        Vector2 cursor = new Vector2(640,380);
        Vector2 lastPointer;
        string dragging = "", held = "";
        Vector3 dragOffset;
        Plane dragPlane;
        bool havePointer, referencesReady, contactPlaced, contactHeld, tankOpened;
        bool edgeArmed = true;
        int pendingEdge;
        float edgeTime, edgeCooldown, flash, rejectFlash, mouseStill, stableYaw, stablePitch;
        PhotoChapter.DarkStep previousDark;
        PhotoChapter.FilmPhase previousPhase;

        public PhotoWorld3D(WBMain host, PhotoChapter model) : base(host,model,1)
        {
            p=model; p.BeforeComplete=RevealPrints; BuildPark(); BuildCamera(); BuildMeter(); BuildDarkroom();
            var cameraObject=new GameObject("V3 physical film exposure camera");
            cameraObject.transform.SetParent(Root.transform,false); photoCamera=cameraObject.AddComponent<Camera>();
            photoCamera.enabled=false; photoCamera.clearFlags=CameraClearFlags.SolidColor;
            photoCamera.backgroundColor=new Color(.64f,.72f,.71f); photoCamera.nearClipPlane=.08f; photoCamera.farClipPlane=45;
            photoCamera.allowHDR=false; photoCamera.allowMSAA=false;
            // A single logical mouse pointer works consistently in both FPS and inspection views.
            // Keeping the native cursor locked also prevents the desktop boundary stopping a turn.
            WantsMouseLook=true; SyncObjects(); UpdatePose();
        }
        public override void Activate()
        {
            base.Activate(); ResetCursor();
            if(!referencesReady) GenerateReferences();
            UpdatePose();
        }
        // Unity's built-in cylinders and capsules have height 2. All dimensions in this
        // chapter are authored as full physical dimensions, including the visible collider.
        new GameObject Child(Transform parent,string id,Vector3 localPosition,Vector3 scale,string hex,PrimitiveType shape=PrimitiveType.Cube,Vector3? euler=null)
        {
            if(shape==PrimitiveType.Cylinder || shape==PrimitiveType.Capsule)scale.y*=.5f;
            var go=base.Child(parent,id,localPosition,scale,hex,shape,euler);
            if(shape==PrimitiveType.Cylinder && !string.IsNullOrEmpty(id))
            {
                // Unity supplies a CapsuleCollider for its cylinder primitive. A thin lid
                // consequently gets a sphere-sized collider that hides the pouring neck.
                // This bounded box follows the authored full dimensions even for short disks.
                var capsule=go.GetComponent<CapsuleCollider>();
                if(capsule!=null){capsule.enabled=false;ReleaseObject(capsule);}
                var box=go.AddComponent<BoxCollider>();box.size=new Vector3(1,2,1);
            }
            return go;
        }
        new GameObject Part(string id,Vector3 localPosition,Vector3 scale,string hex,PrimitiveType shape=PrimitiveType.Cube,Vector3? euler=null)
        { return Child(Root.transform,id,localPosition,scale,hex,shape,euler); }
        void BuildPark()
        {
            Part(null,new Vector3(8,-.15f,7),new Vector3(30,.25f,25),"#a0ac8c");
            Part(null,new Vector3(8,.001f,.1f),new Vector3(26,.035f,1.9f),"#c1b69d");
            Part(null,new Vector3(8,.02f,10.8f),new Vector3(27,.05f,1.4f),"#918b77");
            for(int i=0;i<12;i++)
            {
                float x=-3+i*2;
                Part(null,new Vector3(x,1,11.3f),new Vector3(.12f,2,.12f),"#857866");
                Part(null,new Vector3(x+.8f,.7f,11.3f),new Vector3(1.65f,.08f,.08f),"#8d8270");
            }
            // A lived-in boundary rather than a single isolated tree.
            for(int i=0;i<4;i++)
            {
                float x=-3+i*7.5f;
                Part(null,new Vector3(x,1.65f,16),new Vector3(4.8f,3.3f,3),i%2==0?"#b9b29d":"#abb8b2");
                Part(null,new Vector3(x,3.42f,16),new Vector3(5.2f,.3f,3.4f),"#827b70");
                for(int k=-1;k<=1;k+=2)
                { Part(null,new Vector3(x+k,2.1f,14.47f),new Vector3(.7f,1,.04f),"#60767a"); Part(null,new Vector3(x+k,2.1f,14.43f),new Vector3(.035f,1,.04f),"#d4c8a9"); }
                Part(null,new Vector3(x,.85f,14.45f),new Vector3(.8f,1.7f,.08f),"#807363");
            }
            for(int i=0;i<9;i++) Tree(-3+i*3,8+(i%3)*1.3f,2.7f+(i%2)*.4f);
            // Scene 1: the horizontal seat, tall swing frame and stone are composition landmarks.
            Bench(new Vector3(-1.25f,0,4));
            Part(null,new Vector3(1.25f,1.4f,5),new Vector3(.11f,2.8f,.11f),"#897a64");
            Part(null,new Vector3(2.5f,1.4f,5),new Vector3(.11f,2.8f,.11f),"#897a64");
            Part(null,new Vector3(1.87f,2.8f,5),new Vector3(1.7f,.13f,.13f),"#897a64");
            for(int i=0;i<2;i++)Part(null,new Vector3(1.58f+i*.6f,1.77f,5),new Vector3(.018f,2,.018f),"#655e53");
            Part(null,new Vector3(1.88f,.76f,5),new Vector3(.83f,.08f,.36f),"#a58c68");
            Part(null,new Vector3(.5f,.2f,7),new Vector3(.7f,.4f,.5f),"#a4a394",PrimitiveType.Sphere);
            // Scene 2: a real bridge deck, rails and a little water channel.
            Part(null,new Vector3(8,-.01f,5.6f),new Vector3(4.3f,.02f,6.1f),"#7c9c9b");
            Part(null,new Vector3(8,.17f,5),new Vector3(2.4f,.25f,4.9f),"#9c896d");
            for(int i=0;i<10;i++)Part(null,new Vector3(8,.31f,2.85f+i*.47f),new Vector3(2.5f,.025f,.022f),"#776c5d");
            for(int i=0;i<5;i++)for(int side=-1;side<=1;side+=2)
            {
                Part(null,new Vector3(8+side*1.2f,.78f,3.1f+i*1.05f),new Vector3(.09f,1.2f,.09f),"#807460");
                Part(null,new Vector3(8+side*1.2f,1.24f,5.2f),new Vector3(.08f,.08f,4.8f),"#9c8a6d");
            }
            Part(null,new Vector3(8,2.7f,8),new Vector3(.7f,.25f,.6f),"#d6d0b5",PrimitiveType.Sphere);
            // Scene 3: picnic cloth, fence post, and tall pear tree.
            Part(null,new Vector3(14.8f,.04f,4),new Vector3(1.7f,.04f,1.2f),"#c6b59b");
            for(int i=0;i<3;i++)Part(null,new Vector3(14.2f+i*.5f,.065f,4),new Vector3(.09f,.012f,1.2f),"#9f8075");
            Part(null,new Vector3(17.5f,.6f,6.5f),new Vector3(.16f,1.2f,.16f),"#857c68");
            Tree(16.3f,8,2.8f);
            for(int i=0;i<18;i++)
            {
                float x=13.7f+(i%6)*.8f,z=4.5f+(i/6)*.7f;
                Part(null,new Vector3(x,.19f,z),new Vector3(.018f,.38f,.018f),"#72876c");
                Part(null,new Vector3(x,.38f,z),new Vector3(.12f,.08f,.12f),i%2==0?"#dfd7b2":"#ba9e97",PrimitiveType.Sphere);
            }
            BuildGirl(0,new Vector3(0,0,5)); BuildGirl(1,new Vector3(8,.31f,5.5f)); BuildGirl(2,new Vector3(16,0,4.6f));
            foregroundHand=Group("Player answers the offered hand",new Vector3(16.2f,1.46f,.88f));
            Child(foregroundHand,null,Vector3.zero,new Vector3(.12f,.09f,.27f),"#c4b39c",PrimitiveType.Capsule,new Vector3(90,0,-15));
            Child(foregroundHand,null,new Vector3(-.02f,0,.14f),new Vector3(.13f,.075f,.13f),"#cebea7",PrimitiveType.Sphere);
            var sunlight=new GameObject("Summer soft sunlight");sunlight.transform.SetParent(Root.transform,false);
            var light=sunlight.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.0f;light.color=new Color(1,.93f,.79f);sunlight.transform.localEulerAngles=new Vector3(48,-32,0);
            var fill=new GameObject("Park ambient fill");fill.transform.SetParent(Root.transform,false);fill.transform.localPosition=new Vector3(8,5,2);
            var f=fill.AddComponent<Light>();f.type=LightType.Point;f.range=28;f.intensity=1.1f;f.color=new Color(.81f,.9f,1);
        }
        void Tree(float x,float z,float height)
        {
            Part(null,new Vector3(x,height*.45f,z),new Vector3(.17f,height*.9f,.17f),"#857b68",PrimitiveType.Cylinder);
            for(int i=0;i<3;i++)Part(null,new Vector3(x+(i-1)*.47f,height+(i%2)*.3f,z+(i%2)*.35f),new Vector3(1.75f,1.45f,1.7f),i==1?"#84967a":"#92a184",PrimitiveType.Sphere);
        }
        void Bench(Vector3 at)
        {
            Part(null,at+new Vector3(0,.7f,0),new Vector3(2.3f,.12f,.58f),"#9e8c70");
            Part(null,at+new Vector3(0,1.04f,.23f),new Vector3(2.3f,.52f,.09f),"#9e8c70");
            for(int i=-1;i<=1;i+=2)Part(null,at+new Vector3(i*.8f,.34f,0),new Vector3(.09f,.68f,.44f),"#6e7469");
        }
        void BuildGirl(int index,Vector3 at)
        {
            var g=Group("Backlit memory silhouette "+index,at); girls[index]=g;
            Child(g,null,new Vector3(0,1.02f,0),new Vector3(.39f,.7f,.24f),"#8d948b",PrimitiveType.Capsule);
            Child(g,null,new Vector3(0,.67f,.01f),new Vector3(.5f,.48f,.32f),"#91998c",PrimitiveType.Cylinder);
            for(int side=-1;side<=1;side+=2)Child(g,null,new Vector3(side*.1f,.3f,0),new Vector3(.1f,.55f,.1f),"#bbb5a2",PrimitiveType.Capsule);
            heads[index]=new GameObject("Hair silhouette; no facial geometry").transform;heads[index].SetParent(g,false);heads[index].localPosition=new Vector3(0,1.58f,0);
            Child(heads[index],null,Vector3.zero,new Vector3(.25f,.31f,.25f),"#5e5c56",PrimitiveType.Sphere);
            Child(heads[index],null,new Vector3(0,-.13f,.055f),new Vector3(.29f,.32f,.18f),"#5e5c56",PrimitiveType.Capsule);
            // The blown-out face is geometry, not a UI censor: it is present in every exposure.
            var face=Child(heads[index],null,new Vector3(0,.015f,-.126f),new Vector3(.245f,.24f,.025f),"#fff7df",PrimitiveType.Sphere);
            var material=face.GetComponent<Renderer>().sharedMaterial;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(1.9f,1.8f,1.55f));
            var glow=new GameObject("Face obscuring backlight");glow.transform.SetParent(g,false);glow.transform.localPosition=new Vector3(0,1.8f,.7f);
            var l=glow.AddComponent<Light>();l.type=LightType.Point;l.range=2;l.intensity=1.8f;l.color=new Color(1,.94f,.81f);
            arms[index]=new GameObject("Reaching shoulder").transform;arms[index].SetParent(g,false);arms[index].localPosition=new Vector3(.24f,1.29f,0);
            Child(arms[index],null,new Vector3(.02f,-.22f,0),new Vector3(.095f,.48f,.095f),"#a5ac9c",PrimitiveType.Capsule);
            Child(arms[index],null,new Vector3(.02f,-.46f,0),new Vector3(.105f,.12f,.08f),"#cbbda6",PrimitiveType.Sphere);
            Child(g,null,new Vector3(-.25f,1.03f,0),new Vector3(.095f,.5f,.095f),"#a5ac9c",PrimitiveType.Capsule);
        }
        void BuildCamera()
        {
            cameraRig=Group("Editable 35mm SLR",cameraAt);
            Child(cameraRig,"camera-body",Vector3.zero,new Vector3(.78f,.45f,.3f),"#4a4f4c");
            Child(cameraRig,null,new Vector3(0,.23f,.02f),new Vector3(.75f,.075f,.28f),"#c3c4b7");
            Child(cameraRig,null,new Vector3(0,.05f,.23f),new Vector3(.3f,.26f,.35f),"#373d3b",PrimitiveType.Cylinder,new Vector3(90,0,0));
            Child(cameraRig,"focus-ring",new Vector3(0,.05f,.35f),new Vector3(.32f,.085f,.32f),"#636b61",PrimitiveType.Cylinder,new Vector3(90,0,0));
            Child(cameraRig,"finder",new Vector3(0,.29f,-.16f),new Vector3(.2f,.14f,.075f),"#243a38");
            Child(cameraRig,"back-latch",new Vector3(-.405f,.06f,-.09f),new Vector3(.065f,.12f,.11f),"#d0cbb8");
            cameraInternals.Add(Child(cameraRig,"chamber",new Vector3(-.25f,-.01f,-.17f),new Vector3(.19f,.36f,.05f),"#171f1f"));
            cameraInternals.Add(Child(cameraRig,"spool",new Vector3(.25f,-.01f,-.2f),new Vector3(.11f,.32f,.11f),"#b7baa9",PrimitiveType.Cylinder));
            cameraInternals.Add(Child(cameraRig,"perforations",new Vector3(.16f,-.025f,-.245f),new Vector3(.06f,.29f,.035f),"#d2cfb5"));
            Child(cameraRig,"leader",new Vector3(-.12f,0,-.23f),new Vector3(.14f,.22f,.025f),"#9b8262");
            loadedCartridge=Child(cameraRig,null,new Vector3(-.25f,-.01f,-.205f),new Vector3(.13f,.3f,.13f),"#b7b197",PrimitiveType.Cylinder);
            leaderBand=Child(cameraRig,null,new Vector3(.055f,0,-.23f),new Vector3(.41f,.22f,.018f),"#9b8262");
            for(int i=0;i<7;i++)cameraInternals.Add(Child(cameraRig,null,new Vector3(-.12f+i*.05f,.085f,-.245f),new Vector3(.018f,.018f,.004f),"#dfd6b4"));
            backHinge=new GameObject("Independent hinged camera back").transform;backHinge.SetParent(cameraRig,false);backHinge.localPosition=new Vector3(-.4f,0,-.2f);
            Child(backHinge,"back",new Vector3(.4f,0,0),new Vector3(.8f,.43f,.035f),"#555d55");
            Child(cameraRig,"advance",new Vector3(.41f,.28f,-.03f),new Vector3(.28f,.04f,.075f),"#c6c4b0",PrimitiveType.Cube,new Vector3(0,0,12));
            Child(cameraRig,"shutter",new Vector3(.28f,.31f,-.045f),new Vector3(.055f,.07f,.055f),"#dfd4b7",PrimitiveType.Cylinder);
            Child(cameraRig,"shutter-dial",new Vector3(.245f,.29f,.07f),new Vector3(.2f,.06f,.2f),"#81877a",PrimitiveType.Cylinder);
            Child(cameraRig,"rewind",new Vector3(-.28f,.29f,.03f),new Vector3(.15f,.045f,.15f),"#c6c4b0",PrimitiveType.Cylinder);
            Child(cameraRig,"aperture-window",new Vector3(-.06f,.22f,-.205f),new Vector3(.17f,.07f,.022f),"#dad5bd");
            for(int i=0;i<4;i++)
            {
                int iso=PhotoChapter.FilmSpeeds[i]; var roll=Child(cameraRig,"film-"+iso,new Vector3(-.5f+i*.33f,-.43f,-.25f),new Vector3(.13f,.23f,.13f),i%2==0?"#b4b49a":"#c5b88d",PrimitiveType.Cylinder);
                var label=DeviceLabel(iso.ToString(),roll.transform,new Vector3(0,0,-.52f),.27f); label.transform.localScale=Vector3.one;
            }
            Child(cameraRig,"exposed-film",new Vector3(-.53f,-.36f,-.25f),new Vector3(.14f,.23f,.14f),"#b6b399",PrimitiveType.Cylinder);
            apertureMark=DeviceLabel("8",cameraRig,new Vector3(-.06f,.22f,-.221f),.03f);
            shutterMark=DeviceLabel("125",cameraRig,new Vector3(.245f,.334f,.07f),.036f,new Vector3(90,0,0));
            focusMark=DeviceLabel("5.0",cameraRig,new Vector3(.05f,.15f,-.173f),.03f);
            frameMark=DeviceLabel("0",cameraRig,new Vector3(.35f,.2f,-.173f),.03f);
            hand=Group("Physical inspection hands",cameraAt+new Vector3(0,-.2f,-.05f));
            for(int side=-1;side<=1;side+=2)Child(hand,null,new Vector3(side*.41f,-.05f,0),new Vector3(.13f,.12f,.29f),"#c4b39c",PrimitiveType.Capsule,new Vector3(55,0,side*15));
            foreach(var pair in Nodes) if(pair.Key.StartsWith("film-") || pair.Key=="leader") homes[pair.Key]=pair.Value.transform.localPosition;
        }
        TextMesh DeviceLabel(string text,Transform parent,Vector3 local,float size,Vector3? euler=null)
        {
            var tm=Label(text,Vector3.zero,size,"#e8e0c8",euler); tm.transform.SetParent(parent,false);tm.transform.localPosition=local; return tm;
        }
        void BuildMeter()
        {
            meterRig=Group("Incident light meter",new Vector3(-.7f,1.25f,-4.45f));
            Child(meterRig,null,Vector3.zero,new Vector3(.37f,.56f,.1f),"#56605a");
            Child(meterRig,null,new Vector3(0,.27f,0),new Vector3(.22f,.22f,.15f),"#e3dfcc",PrimitiveType.Sphere);
            Child(meterRig,null,new Vector3(0,.04f,-.06f),new Vector3(.31f,.27f,.025f),"#b8c0a4");
            meterMark=DeviceLabel("100\n8  125",meterRig,new Vector3(0,.045f,-.08f),.055f);
            // The physical needle and native numbers convey exposure without instructional labels.
            Child(meterRig,"meter-needle",new Vector3(0,-.15f,-.07f),new Vector3(.015f,.1f,.015f),"#a96e59");
        }
        void BuildDarkroom()
        {
            darkroomRig=Group("Compact black and white darkroom",Vector3.zero);
            Child(darkroomRig,null,new Vector3(-4.5f,.9f,-4.75f),new Vector3(5.25f,.18f,2.4f),"#8b8c7d");
            Child(darkroomRig,null,new Vector3(-4.5f,1.6f,-3.55f),new Vector3(5.25f,1.4f,.1f),"#8d948a");
            Child(darkroomRig,"bag",new Vector3(-6.05f,1.03f,-4.9f),new Vector3(.93f,.15f,.75f),"#303c38");
            Child(darkroomRig,"bag-zip",new Vector3(-6.05f,1.13f,-4.68f),new Vector3(.65f,.055f,.07f),"#bdbca5");
            Child(darkroomRig,"bag-wheel",new Vector3(-6.05f,1.16f,-4.96f),new Vector3(.28f,.07f,.28f),"#747c70",PrimitiveType.Cylinder);
            var rewoundRoll=Child(darkroomRig,"darkroom-film",new Vector3(-6.63f,1.17f,-5.35f),new Vector3(.17f,.3f,.17f),"#b7b197",PrimitiveType.Cylinder); homes["darkroom-film"]=rewoundRoll.transform.localPosition;
            Child(darkroomRig,"tank",tankAt,new Vector3(.45f,.48f,.45f),"#4c5752",PrimitiveType.Cylinder);
            tankLid=Child(darkroomRig,"tank-lid",tankAt+new Vector3(0,.26f,0),new Vector3(.49f,.055f,.49f),"#2d3937",PrimitiveType.Cylinder);
            var port=Child(darkroomRig,"pour-port",tankAt+new Vector3(-.14f,.32f,-.07f),new Vector3(.15f,.065f,.15f),"#b9bca7",PrimitiveType.Cylinder);
            port.transform.SetParent(tankLid.transform,true);
            Child(darkroomRig,"drain",new Vector3(-3.31f,1.02f,-4.66f),new Vector3(.57f,.07f,.6f),"#596c69");
            for(int i=0;i<3;i++)
            {
                var bottle=Child(darkroomRig,"chemical-"+i,new Vector3(-6.1f+i*.5f,1.25f,-3.93f),new Vector3(.24f,.49f,.24f),i==0?"#b2b5a0":i==1?"#bdaf85":"#8fa8a7",PrimitiveType.Cylinder);
                Child(bottle.transform,null,new Vector3(0,1.08f,0),new Vector3(.6f,.16f,.6f),"#5c695f",PrimitiveType.Cylinder);
                // Repeated dot counts, not a numbered task list.
                for(int j=0;j<=i;j++)Child(bottle.transform,null,new Vector3(-.22f+j*.22f,0,-.53f),new Vector3(.1f,.045f,.025f),"#52615b");
                homes["chemical-"+i]=bottle.transform.localPosition;
            }
            Child(darkroomRig,"water",new Vector3(-3.3f,1.4f,-3.86f),new Vector3(.45f,.55f,.1f),"#91aaa6");
            for(int i=0;i<4;i++)
            {
                Child(darkroomRig,"tray-"+i,new Vector3(-5.7f+i*.79f,1.025f,-5.42f),new Vector3(.7f,.075f,.5f),i==0?"#a6ab92":i==1?"#b3a47b":i==2?"#7e9b99":"#a4bbb2");
                for(int j=0;j<=i;j++)Child(darkroomRig,null,new Vector3(-5.9f+i*.79f+j*.1f,1.072f,-5.2f),new Vector3(.04f,.015f,.04f),"#4d625a",PrimitiveType.Sphere);
            }
            Child(darkroomRig,"contact-frame",new Vector3(-3.15f,1.07f,-3.94f),new Vector3(.66f,.09f,.61f),"#4c5850");
            Child(darkroomRig,"contact-lamp",new Vector3(-3.15f,1.85f,-3.93f),new Vector3(.39f,.1f,.39f),"#a5aa92",PrimitiveType.Cylinder);
            negativeStrip=Child(darkroomRig,"negative",new Vector3(-4.15f,1.49f,-4.54f),new Vector3(.09f,.035f,.46f),"#8f816b"); homes["negative"]=negativeStrip.transform.localPosition;
            contactPaper=Child(darkroomRig,"paper",new Vector3(-3.15f,1.13f,-3.94f),new Vector3(.55f,.025f,.49f),"#e0ddce"); homes["paper"]=contactPaper.transform.localPosition;
            Child(darkroomRig,"drying-line",new Vector3(-4.7f,2.13f,-3.65f),new Vector3(3.2f,.035f,.035f),"#bfc4b0");
            Line("Negative hanging line",new[]{new Vector3(-6.3f,2.13f,-3.65f),new Vector3(-3.1f,2.13f,-3.65f)},"#c6c9b7");
            for(int i=0;i<3;i++)
            {
                printBoards[i]=new GameObject("Independent developed print "+i).transform;printBoards[i].SetParent(darkroomRig,false);printBoards[i].localPosition=new Vector3(-5.75f+i*.7f,1.74f,-3.66f);
                Child(printBoards[i],null,Vector3.zero,new Vector3(.6f,.38f,.012f),"#eee8d5");
                printFaces[i]=PhotographSurface(printBoards[i]);
                Child(printBoards[i],null,new Vector3(0,.21f,0),new Vector3(.05f,.1f,.06f),"#7c806e");
            }
            pouring=Child(darkroomRig,null,tankAt+new Vector3(0,.4f,0),new Vector3(.025f,.35f,.025f),"#c9cbb3",PrimitiveType.Cylinder);liquidMaterial=pouring.GetComponent<Renderer>().sharedMaterial;
            var safe=new GameObject("Darkroom safelight");safe.transform.SetParent(darkroomRig,false);safe.transform.localPosition=new Vector3(-6.8f,2,-4);
            var light=safe.AddComponent<Light>();light.type=LightType.Point;light.range=4;light.intensity=.8f;light.color=new Color(1,.37f,.22f);
        }
        Renderer PhotographSurface(Transform parent)
        {
            // Cube primitive front-face UVs rotate the print. Use an explicitly upright plane.
            var go=new GameObject("Upright photograph surface");go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(0,0,-.011f);go.transform.localScale=new Vector3(.54f,.32f,1);
            var mesh=new Mesh { name="Upright photo UV" };
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
            mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material("#c2c1b2");return renderer;
        }
        void ResetCursor() { cursor=new Vector2(640,380); havePointer=false; edgeArmed=false; edgeTime=0; pendingEdge=0; edgeCooldown=.3f; }
        void Enter(Inspection next)
        {
            // Moving a pointer to an instrument is not a new photographic aim. Preserve the
            // last settled framing while entering edge views, then return to that same pose.
            if((Mode==Inspection.Walk && (next==Inspection.Meter || next==Inspection.Body)) || (Mode==Inspection.Finder && (next==Inspection.Aperture || next==Inspection.Shutter)))
            {p.Settings.Yaw=stableYaw;p.Settings.Pitch=stablePitch;}
            bool opticalChange=(next==Inspection.Aperture || next==Inspection.Shutter || next==Inspection.Finder) && (Mode==Inspection.Aperture || Mode==Inspection.Shutter || Mode==Inspection.Finder);
            Mode=next; p.MeterVisible=next==Inspection.Meter; p.InViewfinder=next==Inspection.Finder;
            edgeCooldown=.35f; edgeArmed=false; pendingEdge=0; edgeTime=0;if(!opticalChange)ResetCursor(); SyncObjects(); UpdatePose(); NativeActionCount++;
        }
        void UpdateCursor(WBInput input)
        {
            Vector2 delta=input.PointerDelta;
            bool locked=Cursor.lockState==CursorLockMode.Locked;
            if(locked) cursor+=delta;
            else cursor=input.Pointer;
            cursor.x=Mathf.Clamp(cursor.x,0,1280);cursor.y=Mathf.Clamp(cursor.y,0,760);
            if(!havePointer) { lastPointer=cursor; havePointer=true; }
        }
        public override Ray PointerRay(Vector2 nativeScreenBottomLeft)
        {
            if(View==null) return new Ray(Eye,(Look-Eye).normalized);
            // Host supplies a native ray, but the logical pointer is authoritative while locked.
            if(Cursor.lockState==CursorLockMode.Locked) return View.ViewportPointToRay(new Vector3(cursor.x/1280,1-cursor.y/760,0));
            return base.PointerRay(nativeScreenBottomLeft);
        }
        public override void Tick(float dt,WBInput input)
        {
            UpdateCursor(input); edgeCooldown=Mathf.Max(0,edgeCooldown-dt);flash=Mathf.Max(0,flash-dt);rejectFlash=Mathf.Max(0,rejectFlash-dt);
            Vector2 delta=input.PointerDelta;
            if(delta.sqrMagnitude<.04f){mouseStill+=dt;if(mouseStill>.14f){stableYaw=p.Settings.Yaw;stablePitch=p.Settings.Pitch;}}else mouseStill=0;
            if(Mode==Inspection.Walk)
            {
                p.Settings.Yaw+=delta.x*.0018f;p.Settings.Pitch=Mathf.Clamp(p.Settings.Pitch+delta.y*.0014f,-.32f,.32f);
                if(input.RightDown)cursor=new Vector2(640,380);
                float x=(input.Keys.Contains(KeyCode.D)?1:0)-(input.Keys.Contains(KeyCode.A)?1:0),z=(input.Keys.Contains(KeyCode.W)?1:0)-(input.Keys.Contains(KeyCode.S)?1:0);
                var movement=Quaternion.Euler(0,p.Settings.Yaw*Mathf.Rad2Deg,0)*new Vector3(x,0,z).normalized*dt*1.6f;
                p.Settings.X=Mathf.Clamp(p.Settings.X+movement.x,-2.5f,19);p.Settings.Z=Mathf.Clamp(p.Settings.Z+movement.z,-2.2f,2.7f);
            }
            else if(Mode==Inspection.Finder)
            {
                if(cursor.x>220 && cursor.x<1060 && cursor.y>100 && cursor.y<620)
                { p.Settings.Yaw+=delta.x*.00055f;p.Settings.Pitch=Mathf.Clamp(p.Settings.Pitch+delta.y*.00045f,-.32f,.32f); }
            }
            if(held=="tank" && input.Down && (delta.sqrMagnitude>2 || (cursor-lastPointer).sqrMagnitude>4))p.Agitated=true;
            if(held=="rewind" && input.Down && Mathf.Abs(delta.x)>.1f)p.Rewind(Mathf.Abs(delta.x)*.002f);
            p.Tick(dt,input);
            if(contactHeld && !input.Down) { contactHeld=false;p.CancelContact(); }
            if(p.Phase==PhotoChapter.FilmPhase.Rewound && previousPhase!=p.Phase)Enter(Inspection.Darkroom);
            previousPhase=p.Phase;previousDark=p.Darkroom;
            EdgeGestures(dt);PoseGirls();SyncObjects();UpdatePose();lastPointer=cursor;
        }
        void EdgeGestures(float dt)
        {
            bool centre=cursor.x>280 && cursor.x<1000 && cursor.y>180 && cursor.y<570;
            if(centre && edgeCooldown<=0)edgeArmed=true;
            int desired=0;
            if(cursor.y<30 && Mode!=Inspection.Walk && Mode!=Inspection.Darkroom)desired=1;
            else if(Mode==Inspection.Walk && cursor.x<125 && cursor.y>635)desired=2;
            else if(Mode==Inspection.Walk && cursor.x>1155 && cursor.y>635)desired=3;
            else if((Mode==Inspection.Finder || Mode==Inspection.Shutter) && cursor.x<45)desired=4;
            else if((Mode==Inspection.Finder || Mode==Inspection.Aperture) && cursor.x>1235)desired=5;
            else if((Mode==Inspection.Aperture || Mode==Inspection.Shutter) && centre)desired=6;
            if(desired==0 || !edgeArmed || edgeCooldown>0 || dragging!="") { pendingEdge=0;edgeTime=0;return; }
            if(pendingEdge!=desired){pendingEdge=desired;edgeTime=0;}
            edgeTime+=dt;if(edgeTime<.24f)return;
            if(desired==1)Enter(Inspection.Walk);
            if(desired==2)Enter(Inspection.Meter);
            if(desired==3)Enter(p.AllExposed && p.Phase>=PhotoChapter.FilmPhase.Rewound?Inspection.Darkroom:Inspection.Body);
            if(desired==4)Enter(Inspection.Aperture);
            if(desired==5)Enter(Inspection.Shutter);
            if(desired==6)Enter(Inspection.Finder);
        }
        void UpdatePose()
        {
            Fov=55;var s=p.Settings;
            if(Mode==Inspection.Walk || Mode==Inspection.Finder)
            {
                Eye=World(new Vector3(s.X,1.6f,s.Z)); Look=Eye+Quaternion.Euler(s.Pitch*Mathf.Rad2Deg,s.Yaw*Mathf.Rad2Deg,0)*Vector3.forward;
                if(Mode==Inspection.Finder)Fov=PhotoFov(s.Focal);
            }
            else if(Mode==Inspection.Meter) { Eye=World(new Vector3(-.7f,1.42f,-5.32f));Look=World(new Vector3(-.7f,1.35f,-4.45f));Fov=37; }
            else if(Mode==Inspection.Body) { Eye=World(new Vector3(.03f,1.87f,-5.55f));Look=World(cameraAt+new Vector3(0,-.1f,0));Fov=47; }
            else if(Mode==Inspection.Aperture) { Eye=World(cameraAt+new Vector3(-.09f,.34f,-.58f));Look=World(cameraAt+new Vector3(-.05f,.22f,-.17f));Fov=31; }
            else if(Mode==Inspection.Shutter) { Eye=World(cameraAt+new Vector3(.65f,.79f,-.22f));Look=World(cameraAt+new Vector3(.25f,.25f,.06f));Fov=31; }
            else { Eye=World(new Vector3(-4.55f,2.7f,-7.1f));Look=World(new Vector3(-4.55f,1.15f,-4.65f));Fov=64; }
        }
        static float PhotoFov(float focal) => 2*Mathf.Atan((36f*760/1280)/(2*focal))*Mathf.Rad2Deg;
        void PoseGirls()
        {
            for(int i=0;i<3;i++)
            {
                float action=0;bool response=false;
                if(i<p.Exposures.Count){action=p.Exposures[i].GirlAction;response=p.Exposures[i].HandResponse;}
                else if(i==p.CurrentFrame){action=p.GirlActionProgress;response=p.HandResponse;}
                PoseGirl(i,action,response);
            }
            foregroundHand.gameObject.SetActive(p.CurrentFrame==2 && p.GirlActionProgress>.55f && Mode==Inspection.Finder);
            foregroundHand.localPosition=HandPosition(p.Settings,p.HandResponse);
            foregroundHand.localEulerAngles=new Vector3(p.HandResponse?-20:0,p.Settings.Yaw*Mathf.Rad2Deg,0);
        }
        void PoseGirl(int i,float action,bool response)
        {
            heads[i].localRotation=Quaternion.Euler(0,(i==0?-33:i==1?48:8)*action, i==0?-9*action:0);
            girls[i].localRotation=Quaternion.Euler(0,i==1?18*action:0,0);
            arms[i].localRotation=Quaternion.Euler(i==2?80*action:0,0,i==2?-18*action:0);
        }
        static Vector3 HandPosition(PhotoChapter.CameraSettings settings,bool response)
        {
            var rotation=Quaternion.Euler(settings.Pitch*Mathf.Rad2Deg,settings.Yaw*Mathf.Rad2Deg,0);
            return new Vector3(settings.X,1.6f,settings.Z)+rotation*new Vector3(.2f,response?-.14f:-.19f,response?.88f:.75f);
        }
        void SyncObjects()
        {
            bool inspect=Mode==Inspection.Body || Mode==Inspection.Aperture || Mode==Inspection.Shutter;
            cameraRig.gameObject.SetActive(inspect);hand.gameObject.SetActive(inspect);meterRig.gameObject.SetActive(Mode==Inspection.Meter);
            darkroomRig.gameObject.SetActive(Mode==Inspection.Darkroom);
            bool open=p.Loading>=PhotoChapter.LoadStep.BackOpen && p.Loading<=PhotoChapter.LoadStep.AdvanceChecked;
            backHinge.localRotation=Quaternion.Euler(0,open?-110:0,0);
            foreach(var part in cameraInternals)part.SetActive(open);
            SetInteractive("chamber",open && p.Loading==PhotoChapter.LoadStep.BackOpen);
            SetInteractive("spool",open && p.Loading==PhotoChapter.LoadStep.Cartridge);
            SetInteractive("perforations",open && p.Loading==PhotoChapter.LoadStep.Leader);
            Nodes["leader"].SetActive(open && p.Loading==PhotoChapter.LoadStep.Cartridge);
            leaderBand.SetActive(open && p.Loading>=PhotoChapter.LoadStep.Leader);
            loadedCartridge.SetActive(open && p.Loading>=PhotoChapter.LoadStep.Cartridge);
            for(int i=0;i<4;i++)Nodes["film-"+PhotoChapter.FilmSpeeds[i]].SetActive(p.Loading<=PhotoChapter.LoadStep.BackOpen);
            Nodes["exposed-film"].SetActive(p.Phase==PhotoChapter.FilmPhase.Rewound);
            Nodes["darkroom-film"].SetActive(p.Darkroom==PhotoChapter.DarkStep.Rewound);
            apertureMark.text=p.Settings.Aperture.ToString("0.#");shutterMark.text=(1/p.Settings.Shutter).ToString("0");focusMark.text=p.Settings.Focus.ToString("0.0");frameMark.text=p.Exposures.Count.ToString();
            int iso=p.Settings.ISO;float shutter=1f/125;float meterAp=Mathf.Sqrt(Mathf.Pow(2,PhotoChapter.EV(p.Target))*shutter*(iso/100f));
            meterMark.text=iso+"\n"+meterAp.ToString("0.#")+"  125";
            negativeStrip.SetActive(p.Darkroom>=PhotoChapter.DarkStep.NegativeReady && p.Darkroom<=PhotoChapter.DarkStep.ContactReady);
            contactPaper.SetActive(p.Darkroom>=PhotoChapter.DarkStep.ContactReady && p.Darkroom<PhotoChapter.DarkStep.Complete);
            if(dragging!="negative")
            {
                bool hanging=p.Darkroom==PhotoChapter.DarkStep.NegativeDrying || (p.Darkroom==PhotoChapter.DarkStep.ContactReady && !contactPlaced);
                negativeStrip.transform.localPosition=hanging?new Vector3(-5.72f,1.85f,-3.68f):contactPlaced?new Vector3(-3.15f,1.16f,-3.94f):homes["negative"];
                negativeStrip.transform.localEulerAngles=hanging?new Vector3(90,0,0):Vector3.zero;
            }
            if(dragging!="paper")
            {
                int bath=-1;
                if(p.Darkroom==PhotoChapter.DarkStep.PaperDeveloper || p.Darkroom==PhotoChapter.DarkStep.PaperStopReady)bath=0;
                if(p.Darkroom==PhotoChapter.DarkStep.PaperStop || p.Darkroom==PhotoChapter.DarkStep.PaperFixReady)bath=1;
                if(p.Darkroom==PhotoChapter.DarkStep.PaperFix || p.Darkroom==PhotoChapter.DarkStep.PaperWashReady)bath=2;
                if(p.Darkroom==PhotoChapter.DarkStep.PaperWash || p.Darkroom==PhotoChapter.DarkStep.PaperDryReady)bath=3;
                contactPaper.transform.localPosition=bath<0?homes["paper"]:new Vector3(-5.7f+bath*.79f,1.085f,-5.42f);
            }
            tankLid.transform.localPosition=tankAt+new Vector3(tankOpened?.35f:0,.26f,0);
            pouring.SetActive(p.TimerRunning && p.Darkroom<=PhotoChapter.DarkStep.Wash);
            liquidMaterial.color=p.Darkroom==PhotoChapter.DarkStep.Stop?new Color(.65f,.59f,.37f):p.Darkroom==PhotoChapter.DarkStep.Fixer?new Color(.48f,.65f,.65f):new Color(.73f,.76f,.64f);
            for(int i=0;i<3;i++)printBoards[i].gameObject.SetActive(p.Darkroom==PhotoChapter.DarkStep.PaperDrying || p.Completed);
            // Only the physically meaningful next component receives a subtle, nonverbal pulse.
            if(Mode==Inspection.Darkroom)SyncDarkroomCue();
        }
        void SetInteractive(string id,bool enabled)
        { if(Nodes.TryGetValue(id,out var node))foreach(var c in node.GetComponentsInChildren<Collider>())c.enabled=enabled && dragging!=id; }
        void SyncDarkroomCue()
        {
            string next="";
            switch(p.Darkroom)
            {
                case PhotoChapter.DarkStep.Rewound: next="bag"; break;
                case PhotoChapter.DarkStep.BagLoaded: next="bag-zip"; break;
                case PhotoChapter.DarkStep.BagSealed: next="bag-wheel"; break;
                case PhotoChapter.DarkStep.ReelLoaded: next="tank-lid"; break;
                case PhotoChapter.DarkStep.TankSealed: next="chemical-0"; break;
                case PhotoChapter.DarkStep.StopReady: next="chemical-1"; break;
                case PhotoChapter.DarkStep.FixReady: next="chemical-2"; break;
                case PhotoChapter.DarkStep.WashReady: next="water"; break;
                case PhotoChapter.DarkStep.NegativeReady: next=tankOpened?"negative":"tank-lid"; break;
                case PhotoChapter.DarkStep.ContactReady: next=contactPlaced?"contact-lamp":"negative"; break;
                case PhotoChapter.DarkStep.PaperReady: next="tray-0"; break;
                case PhotoChapter.DarkStep.PaperStopReady: next="tray-1"; break;
                case PhotoChapter.DarkStep.PaperFixReady: next="tray-2"; break;
                case PhotoChapter.DarkStep.PaperWashReady: next="tray-3"; break;
                case PhotoChapter.DarkStep.PaperDryReady: next="drying-line"; break;
            }
            if(p.NeedsDrain)next="drain";
            foreach(var pair in Nodes)
            {
                var renderer=pair.Value.GetComponent<Renderer>();if(renderer==null)continue;
                var m=renderer.sharedMaterial;
                if(pair.Key==next){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.16f,.16f,.1f)*(1+Mathf.Sin(Time.realtimeSinceStartup*3)*.35f));}
                else if(m.IsKeywordEnabled("_EMISSION")){m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");}
            }
        }
        void Reject(){rejectFlash=.2f;WBMain.Tone(110,.06f);}
        void Accepted(){NativeActionCount++;WBMain.Tone(240,.045f);SyncObjects();}
        void BeginDrag(string id,Ray ray)
        {
            if(!Nodes.ContainsKey(id))return;dragging=id;
            var item=Nodes[id];if(!homes.ContainsKey(id))homes[id]=item.transform.localPosition;
            dragPlane=new Plane(View!=null?-View.transform.forward:Vector3.back,item.transform.position);
            Vector3 point=dragPlane.Raycast(ray,out float distance)?ray.GetPoint(distance):item.transform.position;
            dragOffset=item.transform.position-point;
            foreach(var col in item.GetComponentsInChildren<Collider>())col.enabled=false;
        }
        public override void Press(string id,Ray ray,int button)
        {
            if(button!=0 || p.Completed)return;held=id;
            if(Mode==Inspection.Finder)
            {
                if(p.CurrentFrame==2 && p.GirlActionProgress>.6f && !p.HandResponse){p.RespondToHand();Accepted();return;}
                Shoot();return;
            }
            if(Mode==Inspection.Body || Mode==Inspection.Aperture || Mode==Inspection.Shutter)
            {
                if(id=="back-latch"){if(p.OpenBack())Accepted();else Reject();return;}
                if(id=="back"){if(p.CloseBack())Accepted();else if(p.Loading==PhotoChapter.LoadStep.ClosedEmpty && p.OpenBack())Accepted();else Reject();return;}
                if(id.StartsWith("film-") && p.Loading==PhotoChapter.LoadStep.BackOpen){BeginDrag(id,ray);return;}
                if(id=="leader" && p.Loading==PhotoChapter.LoadStep.Cartridge){BeginDrag(id,ray);return;}
                if(id=="perforations"){if(p.SeatPerforations())Accepted();else Reject();return;}
                if(id=="advance"){if(p.Advance())Accepted();else Reject();return;}
                if(id=="finder"){if(p.Phase==PhotoChapter.FilmPhase.Ready)Enter(Inspection.Finder);else Reject();return;}
                if(id=="shutter"){Shoot();return;}
                if(id=="rewind"){if(!p.AllExposed)Reject();return;}
            }
            if(Mode!=Inspection.Darkroom)return;
            if(p.Darkroom==PhotoChapter.DarkStep.Rewound && id=="darkroom-film")
            {
                BeginDrag(id,ray);return;
            }
            if(id=="bag-zip"){if(p.SealBag())Accepted();else Reject();return;}
            if(id=="tank-lid" || id=="pour-port")
            {
                if(p.Darkroom==PhotoChapter.DarkStep.ReelLoaded){if(p.SealTank())Accepted();return;}
                if(p.OpenTank()){tankOpened=true;Accepted();return;}Reject();return;
            }
            if(id=="drain"){if(p.Drain())Accepted();else Reject();return;}
            if(id=="water" && p.Darkroom==PhotoChapter.DarkStep.WashReady){if(p.Pour(3))Accepted();return;}
            if(id.StartsWith("chemical-") && !p.TimerRunning){BeginDrag(id,ray);return;}
            if(id=="negative" && (tankOpened || p.Darkroom==PhotoChapter.DarkStep.ContactReady)){BeginDrag(id,ray);return;}
            if(id=="contact-lamp" && p.Darkroom==PhotoChapter.DarkStep.ContactReady && contactPlaced)
            { if(p.BeginContact()){contactHeld=true;Accepted();}return; }
            if(id=="paper" && !p.TimerRunning){BeginDrag(id,ray);return;}
            if(id.StartsWith("tray-") && p.Darkroom>=PhotoChapter.DarkStep.PaperReady)
            { if(int.TryParse(id.Substring(5),out int bath) && p.PaperBath(bath))Accepted();else Reject();return; }
        }
        void Shoot()
        {
            if(!p.Expose()){Reject();return;}
            var exposure=p.Exposures[p.Exposures.Count-1];PoseGirl(exposure.ReferenceIndex,exposure.GirlAction,exposure.HandResponse);
            bool showHand=exposure.ReferenceIndex==2 && exposure.HandResponse;foregroundHand.gameObject.SetActive(showHand);
            if(showHand)foregroundHand.localPosition=HandPosition(exposure.Settings,true);
            ShotTextures[exposure.ReferenceIndex]=CaptureScene(exposure.Settings,true);
            NativeCaptureCount++;flash=.11f;Accepted();Enter(Inspection.Body);
        }
        public override void Move(Ray ray)
        {
            if(dragging=="")return;
            if(dragPlane.Raycast(ray,out float distance))Nodes[dragging].transform.position=ray.GetPoint(distance)+dragOffset;
        }
        public override void Release(string overId,Ray ray)
        {
            if(contactHeld){contactHeld=false;p.CancelContact();}
            held="";
            if(dragging=="")return;
            string item=dragging;dragging="";bool success=false;
            if(item.StartsWith("film-") && overId=="chamber" && int.TryParse(item.Substring(5),out int iso))success=p.InsertFilm(iso);
            else if(item=="leader" && overId=="spool")success=p.PullLeader();
            else if(item=="darkroom-film" && (overId=="bag" || overId=="bag-wheel" || overId=="bag-zip"))success=p.PutInBag();
            else if(item.StartsWith("chemical-") && (overId=="pour-port" || overId=="tank") && int.TryParse(item.Substring(9),out int chemical))success=p.Pour(chemical);
            else if(item=="negative" && overId=="drying-line" && p.Darkroom==PhotoChapter.DarkStep.NegativeReady)success=p.HangNegative();
            else if(item=="negative" && (overId=="contact-frame" || overId=="paper") && p.Darkroom==PhotoChapter.DarkStep.ContactReady){contactPlaced=true;success=true;}
            else if(item=="paper" && overId.StartsWith("tray-") && int.TryParse(overId.Substring(5),out int bath))success=p.PaperBath(bath);
            else if(item=="paper" && overId=="drying-line")success=p.HangPaper();
            var go=Nodes[item];if(homes.TryGetValue(item,out var home))go.transform.localPosition=home;
            foreach(var col in go.GetComponentsInChildren<Collider>())col.enabled=true;
            if(success)Accepted();else Reject();SyncObjects();
        }
        public override void Scroll(string id,float delta,Ray ray)
        {
            if(Mathf.Abs(delta)<.001f || p.Completed)return;
            if(held=="rewind" && p.AllExposed){if(p.Rewind(Mathf.Abs(delta)*.13f))Accepted();return;}
            if(held=="bag-wheel" && p.Darkroom==PhotoChapter.DarkStep.BagSealed){if(p.WindReel(Mathf.Abs(delta)*.13f))Accepted();return;}
            int step=delta>0?1:-1;
            if(Mode==Inspection.Finder)p.AdjustFocus(step);
            else if(Mode==Inspection.Aperture)p.AdjustAperture(step);
            else if(Mode==Inspection.Shutter)p.AdjustShutter(step);
            else return;
            Accepted();
        }
        void GenerateReferences()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){referencesReady=true;return;}
            bool wasDark=darkroomRig.gameObject.activeSelf;darkroomRig.gameObject.SetActive(false);cameraRig.gameObject.SetActive(false);hand.gameObject.SetActive(false);meterRig.gameObject.SetActive(false);foregroundHand.gameObject.SetActive(false);
            for(int i=0;i<3;i++){PoseGirl(i,0,false);ReferenceImages[i]=CaptureScene(p.Targets[i],false);}
            referencesReady=true;darkroomRig.gameObject.SetActive(wasDark);PoseGirls();SyncObjects();
        }
        Texture2D CaptureScene(PhotoChapter.CameraSettings settings,bool expose)
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null || photoCamera==null)return null;
            if(captureTarget==null){captureTarget=new RenderTexture(768,456,24,RenderTextureFormat.ARGB32);captureTarget.Create();}
            photoCamera.transform.position=World(new Vector3(settings.X,1.6f,settings.Z));
            photoCamera.transform.rotation=Quaternion.Euler(settings.Pitch*Mathf.Rad2Deg,settings.Yaw*Mathf.Rad2Deg,0);
            photoCamera.fieldOfView=PhotoFov(settings.Focal);photoCamera.aspect=1280f/760;photoCamera.targetTexture=captureTarget;
            var previous=RenderTexture.active;
            try
            {
                Physics.SyncTransforms();photoCamera.Render();RenderTexture.active=captureTarget;
                var texture=new Texture2D(768,456,TextureFormat.RGB24,false);texture.name=expose?"Immutable shutter exposure "+p.Exposures.Count:"3D reference photograph";
                texture.ReadPixels(new Rect(0,0,768,456),0,0);texture.Apply();
                if(expose)
                {
                    float gain=Mathf.Pow(2,PhotoChapter.EV(p.Targets[p.Exposures.Count-1])-PhotoChapter.EV(settings));
                    var pixels=texture.GetPixels();for(int j=0;j<pixels.Length;j++){Color c=pixels[j]*gain;c.a=1;pixels[j]=c;}texture.SetPixels(pixels);texture.Apply();
                }
                return texture;
            }
            finally{RenderTexture.active=previous;photoCamera.targetTexture=null;}
        }
        void RevealPrints()
        {
            for(int i=0;i<3;i++)
            {
                if(ShotTextures[i]==null)continue;
                var original=ShotTextures[i];var result=new Texture2D(original.width,original.height,TextureFormat.RGB24,false);result.name="Developed B&W photo "+i;
                Color[] pixels=original.GetPixels();for(int j=0;j<pixels.Length;j++){float grey=pixels[j].grayscale;pixels[j]=new Color(grey*.96f+.025f,grey*.95f+.025f,grey*.91f+.025f);}
                result.SetPixels(pixels);result.Apply();finalPrints[i]=result;printFaces[i].sharedMaterial.mainTexture=result;printFaces[i].sharedMaterial.color=Color.white;
            }
        }
        public override void DrawOverlay()
        {
            if(Mode==Inspection.Walk || Mode==Inspection.Finder || Mode==Inspection.Aperture || Mode==Inspection.Shutter)
            {
                var r=new Rect(23,25,250,148.4f);G.Box(new Rect(r.x-7,r.y-7,r.width+14,r.height+30),G.C("#e8dfc7"));
                if(ReferenceImages[p.CurrentFrame]!=null)GUI.DrawTexture(r,ReferenceImages[p.CurrentFrame],ScaleMode.ScaleToFit);
                for(int i=0;i<3;i++)G.Circle(new Vector2(108+i*27,185),4,i<p.Exposures.Count?G.C("#707f69"):G.C("#bebaa8"));
            }
            if(Mode==Inspection.Walk)
            {
                G.Icon(1,new Vector2(1205,703),24,new Color(.93f,.9f,.8f,.6f));
                G.Box(new Rect(42,679,34,43),Color.clear,new Color(.93f,.9f,.8f,.6f),2);G.Circle(new Vector2(59,673),10,new Color(.93f,.9f,.8f,.6f));
            }
            if(Mode==Inspection.Finder)
            {
                G.Box(new Rect(0,0,1280,22),Color.black);G.Box(new Rect(0,738,1280,22),Color.black);
                G.Ring(new Vector2(640,380),37,1,new Color(.85f,.88f,.8f,.55f),1);
                float split=Mathf.Clamp(p.FocusBlur,0,18);G.Line(new Vector2(610-split,380),new Vector2(670-split,380),new Color(.9f,.9f,.82f,.7f),1);
                G.Line(new Vector2(640,351+split),new Vector2(640,409+split),new Color(.9f,.9f,.82f,.7f),1);
                G.Circle(new Vector2(1100,700),5,p.CompositionQualified?new Color(.53f,.73f,.48f):new Color(.72f,.48f,.37f));
                // These are native viewfinder markings, explicitly allowed by the design.
                G.Text(p.Settings.Aperture.ToString("0.#"),new Rect(38,688,65,32),20,G.C("#dedcc3"));
                G.Text((1/p.Settings.Shutter).ToString("0"),new Rect(1160,688,75,32),20,G.C("#dedcc3"));
            }
            if(p.TimerRunning && Mode==Inspection.Darkroom)G.Ring(new Vector2(640,654),27,p.TimerProgress,G.C("#e3dcb9"),3);
            if(pendingEdge!=0)G.Ring(cursor,15,edgeTime/.24f,G.C("#e8dfc7"),2);
            G.Circle(cursor,2,new Color(1,.97f,.86f,.85f));
            if(flash>0)G.Box(new Rect(0,0,1280,760),new Color(1,1,.93f,flash/.11f*.75f));
            if(rejectFlash>0)G.Ring(cursor,18,1,new Color(.7f,.35f,.28f,rejectFlash/.2f),3);
        }
        public void CancelInteraction()
        {
            if(dragging!=""){var go=Nodes[dragging];if(homes.TryGetValue(dragging,out var home))go.transform.localPosition=home;foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=true;dragging="";}
            held="";contactHeld=false;p.CancelContact();SyncObjects();
        }
        public override void SuspendInput()
        {
            // Losing focus is cancellation, never a synthetic drop on the last hovered object.
            CancelInteraction();
            pendingEdge=0;edgeTime=0;edgeArmed=false;edgeCooldown=.3f;havePointer=false;mouseStill=0;
        }
        public void QASetActor(Vector3 localEye,float yaw=0)
        {p.Settings.X=localEye.x;p.Settings.Z=localEye.z;p.Settings.Yaw=yaw;p.Settings.Pitch=0;UpdatePose();}
        public void QAEnter(Inspection mode){Enter(mode);}
        public bool QAShoot() { int count=NativeCaptureCount;Shoot();return NativeCaptureCount==count+1; }
        public void QAEdge(Vector2 point)
        {cursor=point;edgeArmed=true;edgeCooldown=0;pendingEdge=0;EdgeGestures(.3f);UpdatePose();}
        public Ray QARayTo(string id)
        {Vector3 at=Nodes[id].transform.position;return new Ray(at+new Vector3(0,.06f,-1),(at-(at+new Vector3(0,.06f,-1))).normalized);}
        public bool QANodeRaycasts(string id)
        {Physics.SyncTransforms();return Nodes.ContainsKey(id) && Pick(QARayTo(id))==id;}
        public string QAPickFromView(string id)
        {
            UpdatePose();ApplyPose();Physics.SyncTransforms();
            Vector3 target=Nodes[id].transform.position;return Pick(new Ray(Eye,(target-Eye).normalized));
        }
        public bool QAInView(string id)
        {
            UpdatePose();ApplyPose();if(View==null)return false;
            Vector3 v=View.WorldToViewportPoint(Nodes[id].transform.position);
            return v.z>View.nearClipPlane && v.x>.01f && v.x<.99f && v.y>.01f && v.y<.99f;
        }
        public override void Dispose()
        {
            p.BeforeComplete=null;
            if(captureTarget!=null){captureTarget.Release();ReleaseObject(captureTarget);}
            foreach(var texture in ReferenceImages)if(texture!=null)ReleaseObject(texture);
            foreach(var texture in ShotTextures)if(texture!=null)ReleaseObject(texture);
            foreach(var texture in finalPrints)if(texture!=null)ReleaseObject(texture);
            foreach(var mesh in ownedMeshes)if(mesh!=null)ReleaseObject(mesh);
            base.Dispose();
        }
    }
}
