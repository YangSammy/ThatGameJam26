using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    /// <summary>Embodied gaze into a volumetric still life. Pigment is world geometry on an easel.</summary>
    public sealed class PaintingWorld3D : WorldChapter3D
    {
        public readonly PaintingChapter State;
        public string GazeTarget { get; private set; }
        public string RequiredNode { get { return State.Phase == 0 ? "scarf-target" : State.Phase == 1 ? "cup-contact" : "hands-contact"; } }
        public Vector3 RequiredPoint { get { return Nodes[RequiredNode].transform.position; } }
        public bool[] Painted { get { return State.Painted; } }
        public PaintingDefocus Defocus { get; private set; }
        readonly Transform cupHand, leftHand, rightHand, brushHand;
        public override Transform[] PlayerHandTargets => new Transform[]{null,brushHand};
        public override bool PlayerHandsEngaged => true;
        readonly List<GameObject>[] pigment = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
        readonly Renderer scarfRenderer, cupContact, handsContact;
        readonly WBInput gazeInput = new WBInput();
        readonly Vector3 cupHandContact = new Vector3(.12f, 1.27f, 2.25f);
        readonly Vector3 leftContact = new Vector3(.70f, 1.06f, .95f), rightContact = new Vector3(.84f, 1.06f, .95f);
        Light windowLight;

        readonly Vector3 easelShift=new Vector3(3.95f,0,-1.08f);
        public Transform SharedScarf {get;private set;}

        public override string Help { get { return "Mouse  Look  /  Hold LMB  Focus\nScarf > Cup contact > Joined hands\nWait for contact; hold focus to complete each detail."; } }

        public PaintingWorld3D(WBMain host, PaintingChapter model) : base(host, model, 3)
        {
            State = model; WantsMouseLook = true;
            if(SharedRoom){cupHandContact=new Vector3(.42f,1.27f,1);leftContact=new Vector3(.85f,1.06f,1.3f);rightContact=new Vector3(1,1.06f,1.3f);}
            Room(8, 8);
            Eye = World(new Vector3(0, 1.65f, -2.7f)); Look = World(new Vector3(.20f, 1.17f, 1.75f)); Fov = 64;
            // Real easel, canvas edge, legs and a place for the painter's hand.
            Part(null, new Vector3(-1.95f, 1.55f, 1.08f), new Vector3(1.45f, 1.9f, .085f), "#8c7458");
            Part("canvas", new Vector3(-1.95f, 1.55f, 1.025f), new Vector3(1.30f, 1.76f, .04f), "#e8dcc0");
            for (int sign = -1; sign <= 1; sign += 2)
                Part(null, new Vector3(-1.95f + sign * .46f, .56f, 1.22f), new Vector3(.085f, 1.3f, .09f), "#715c46", PrimitiveType.Cube, new Vector3(0, 0, sign * -11));
            Part(null, new Vector3(-1.95f, .56f, 1.66f), new Vector3(.085f, 1.3f, .09f), "#715c46", PrimitiveType.Cube, new Vector3(-20, 0, 0));
            Part(null, new Vector3(-1.95f, .66f, .96f), new Vector3(1.65f, .09f, .19f), "#8c7458");
            if(SharedRoom){for(int i=0;i<6;i++)Root.transform.GetChild(i).localPosition+=easelShift;Eye=new Vector3(1.25f,1.65f,-.6f);Look=new Vector3(2,.9f,2.1f);}
            if(SharedRoom && Host.deskStudy)
            {
                for(int i=0;i<6;i++)Root.transform.GetChild(i).gameObject.SetActive(false);
                Part(null,new Vector3(2.15f,1.005f,.5f),new Vector3(.62f,.025f,.79f),"#e8dcc0");
                Eye=new Vector3(-.3f,1.65f,.95f);Look=new Vector3(-.5f,.85f,2.6f);
            }
            // Strong perspective: table recedes toward the window rather than a flat still-life panel.
            if(!SharedRoom){
            Part(null, new Vector3(.87f, .90f, 1.80f), new Vector3(3.25f, .13f, 2.35f), "#8b735b");
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Part(null, new Vector3(.87f + x * 1.43f, .43f, 1.8f + z * .97f), new Vector3(.12f, .86f, .12f), "#685a49");
            }
            int scarfStart=Root.transform.childCount;
            // Red scarf is a draped ribbon of spatial blocks, with a hanging end.
            scarfRenderer = Part("scarf-target", new Vector3(1.67f, 1.005f, 1.72f), new Vector3(.79f, .075f, .22f), "#a24f49", PrimitiveType.Cube, new Vector3(0, -18, 0)).GetComponent<Renderer>();
            Part("scarf-fold", new Vector3(1.98f, 1.015f, 1.34f), new Vector3(.22f, .09f, .88f), "#b56050", PrimitiveType.Cube, new Vector3(0, -8, 0));
            Part("scarf-tail", new Vector3(2.00f, .69f, .63f), new Vector3(.22f, .61f, .07f), "#984c48", PrimitiveType.Cube, new Vector3(-7, 0, 0));
            for (int i = 0; i < 6; i++)
                Part(null, new Vector3(1.91f + i * .036f, .32f, .655f), new Vector3(.013f, .12f, .018f), "#ac6656");
            if(SharedRoom)for(int i=scarfStart;i<Root.transform.childCount;i++){var g=Root.transform.GetChild(i);g.GetComponent<Renderer>().enabled=false;var collider=g.GetComponent<Collider>();if(collider!=null && g.name!="scarf-target")collider.enabled=false;}
            int cupStart=Root.transform.childCount;
            // A cup with a visible hollow rim, not a symbol floating on the screen.
            Part(null, new Vector3(-.18f, .987f, 2.25f), new Vector3(.62f, .025f, .52f), "#bfc1aa", PrimitiveType.Cylinder);
            Part("cup-body", new Vector3(-.18f, 1.13f, 2.25f), new Vector3(.29f, .14f, .29f), "#b9c2ae", PrimitiveType.Cylinder);
            Part(null, new Vector3(-.18f, 1.275f, 2.25f), new Vector3(.255f, .012f, .255f), "#5e6859", PrimitiveType.Cylinder);
            Circle("cup-rim", new Vector3(-.18f, 1.285f, 2.25f), .145f, "#d4d6bc", .025f, false);
            Circle("cup-handle", new Vector3(.02f, 1.13f, 2.25f), .09f, "#b9c2ae", .032f, true);
            cupContact = Part("cup-contact", new Vector3(-.045f, 1.29f, 2.25f), new Vector3(.12f, .075f, .15f), "#ceb091", PrimitiveType.Sphere).GetComponent<Renderer>();
            if(SharedRoom)for(int i=cupStart;i<Root.transform.childCount;i++)Root.transform.GetChild(i).localPosition+=new Vector3(.3f,0,-1.25f);
            cupHand = Hand("Hand resting on cup", cupHandContact, true, "#c9a486");
            leftHand = Hand("Left offered hand", leftContact, false, "#c9a486");
            rightHand = Hand("Right answering hand", rightContact, true, "#ac826c");
            handsContact = Part("hands-contact", new Vector3(.77f, 1.085f, .92f), new Vector3(.17f, .10f, .19f), "#c2a186", PrimitiveType.Sphere).GetComponent<Renderer>();
            if(SharedRoom)handsContact.transform.localPosition=new Vector3(.925f,1.085f,1.3f);
            // Chairs anchor the disconnected whitebox hands to places where people sat.
            if(!SharedRoom)for (int sign = -1; sign <= 1; sign += 2)
            {
                Part(null, new Vector3(.77f + sign * 1.5f, .56f, 1.00f), new Vector3(.70f, .12f, .72f), "#73796a");
                Part(null, new Vector3(.77f + sign * 1.75f, 1.09f, 1.00f), new Vector3(.09f, .95f, .72f), "#73796a");
            }
            DrawCanvasUnderpainting();
            brushHand = Group("Painter's first person hand", Vector3.zero);
            Child(brushHand, null, Vector3.zero, new Vector3(.17f, .1f, .22f), "#cfab8c", PrimitiveType.Sphere, new Vector3(0, 0, -20));
            Child(brushHand, null, new Vector3(.09f, -.11f, -.08f), new Vector3(.14f, .37f, .15f), "#8c9691", PrimitiveType.Capsule, new Vector3(0, 0, -25));
            Child(brushHand, null, new Vector3(-.03f, .14f, .035f), new Vector3(.018f, .45f, .018f), "#5d5b49", PrimitiveType.Cylinder, new Vector3(0, 0, -17));
            Child(brushHand, null, new Vector3(-.095f, .345f, .035f), new Vector3(.03f, .085f, .03f), "#b8aa85", PrimitiveType.Capsule, new Vector3(0, 0, -17));
            var lightObject = new GameObject("Still life window warmth"); lightObject.transform.SetParent(Root.transform, false); lightObject.transform.localPosition = new Vector3(2.5f, 2.9f, 3.4f);
            windowLight = lightObject.AddComponent<Light>(); windowLight.type = LightType.Point; windowLight.range = 8; windowLight.intensity = SharedRoom?0f:1.35f; windowLight.color = new Color(1, .86f, .66f);
            UpdateStillLife(.01f);if(SharedRoom)SetObservedHandsVisible(false);
        }
        void SetObservedHandsVisible(bool visible)
        {
            foreach(var hand in new[]{cupHand,leftHand,rightHand})foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true))renderer.enabled=visible;
            cupContact.enabled=handsContact.enabled=visible;
        }
        public void BindSharedScarf(EndingWorld3D ending)
        {
            SharedScarf=ending.Root.transform.Find("Scarf fabric mesh");
            scarfRenderer.transform.position=ending.World(new Vector3(0,.89f,.08f));scarfRenderer.transform.rotation=Quaternion.identity;scarfRenderer.transform.localScale=new Vector3(1.35f,.15f,.6f);
        }
        Transform Hand(string name, Vector3 at, bool reverse, string color)
        {
            Transform hand = Group(name, at); if (reverse) hand.localRotation = Quaternion.Euler(0, 180, 0);
            Child(hand, null, new Vector3(-.18f, 0, 0), new Vector3(.48f, .12f, .16f), color, PrimitiveType.Capsule, new Vector3(0, 0, 90));
            Child(hand, null, new Vector3(.025f, 0, 0), new Vector3(.22f, .10f, .22f), color, PrimitiveType.Sphere);
            for (int i = 0; i < 4; i++)
                Child(hand, null, new Vector3(.13f, 0, -.083f + i * .053f), new Vector3(.18f, .045f, .046f), color, PrimitiveType.Capsule, new Vector3(0, 0, 90));
            Child(hand, null, new Vector3(.07f, -.015f, -.12f), new Vector3(.15f, .05f, .05f), color, PrimitiveType.Capsule, new Vector3(0, -30, 90));
            if(SharedRoom)hand.localScale=Vector3.one*.5f;return hand;
        }
        LineRenderer Circle(string name, Vector3 center, float radius, string color, float width, bool vertical)
        {
            var points = new Vector3[49];
            for (int i = 0; i < points.Length; i++)
            {
                float a = i * Mathf.PI * 2 / (points.Length - 1);
                points[i] = center + (vertical ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) : new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))) * radius;
            }
            return Line(name, points, color, width);
        }
        Vector3 Canvas(float x, float y) { if(SharedRoom && Host.deskStudy)return new Vector3(2.15f+x*.4f,1.025f,.5f+y*.4f);return new Vector3(-1.95f + x, 1.55f + y, .996f)+(SharedRoom?easelShift:Vector3.zero); }
        void CanvasStroke(int group, string name, Vector2[] sketch, string color, float width, bool colored)
        {
            var positions = new Vector3[sketch.Length];
            for (int i = 0; i < sketch.Length; i++) positions[i] = Canvas(sketch[i].x, sketch[i].y) + (colored ? Vector3.back * .003f : Vector3.zero);
            var stroke = Line(name, positions, color, width);
            if (colored) { pigment[group].Add(stroke.gameObject); stroke.gameObject.SetActive(false); }
        }
        void DrawCanvasUnderpainting()
        {
            Vector2[] scarf = { new Vector2(-.46f, .56f), new Vector2(-.20f, .47f), new Vector2(.08f, .55f), new Vector2(.37f, .42f), new Vector2(.42f, .20f) };
            Vector2[] cup = { new Vector2(-.27f, .10f), new Vector2(-.23f, -.15f), new Vector2(.02f, -.15f), new Vector2(.07f, .10f), new Vector2(-.27f, .10f) };
            Vector2[] hand = { new Vector2(.47f, .14f), new Vector2(.22f, .14f), new Vector2(.05f, .09f), new Vector2(.18f, .03f), new Vector2(.36f, .03f) };
            Vector2[] hands = { new Vector2(-.52f, -.55f), new Vector2(-.13f, -.53f), new Vector2(.04f, -.47f), new Vector2(.18f, -.53f), new Vector2(.52f, -.55f) };
            for (int colored = 0; colored < 2; colored++)
            {
                bool c = colored == 1;
                CanvasStroke(0, "Scarf remembered in paint", scarf, c ? "#a9574e" : "#bcb095", c ? .055f : .008f, c);
                CanvasStroke(1, "Cup remembered in paint", cup, c ? "#84958b" : "#bcb095", c ? .032f : .008f, c);
                CanvasStroke(1, "Touch remembered in paint", hand, c ? "#bd9578" : "#bcb095", c ? .038f : .008f, c);
                CanvasStroke(2, "Hands remembered in paint", hands, c ? "#b89177" : "#bcb095", c ? .060f : .008f, c);
            }
        }
        public override void Activate()
        {
            base.Activate();if(SharedRoom)SetObservedHandsVisible(true);
            Defocus=View.GetComponent<PaintingDefocus>();if(Defocus==null)Defocus=View.gameObject.AddComponent<PaintingDefocus>();
            Defocus.Begin();
        }
        public override void Deactivate(){if(SharedRoom)SetObservedHandsVisible(false);if(Defocus!=null)Defocus.End();base.Deactivate();}
        public override void Dispose(){if(Defocus!=null)Defocus.End();base.Dispose();}
        public override void Tick(float dt, WBInput input)
        {
            if (View != null)
            {
                Ray gaze = View.ViewportPointToRay(new Vector3(.5f, .5f, 0));
                GazeTarget = Pick(gaze);
                if (GazeTarget == "scarf-fold" || GazeTarget == "scarf-tail") GazeTarget = "scarf-target";
            }
            gazeInput.Pointer = GazeTarget == RequiredNode ? State.CurrentTarget : new Vector2(-10000, -10000);
            gazeInput.Down = input.Down;
            Defocus?.SetFocusHeld(input.Down,dt);
            if(View!=null)View.fieldOfView=Mathf.Lerp(View.fieldOfView,input.Down?58:64,Mathf.Clamp01(dt*7));
            State.Tick(dt, gazeInput);
            UpdateStillLife(dt);
            for (int i = 0; i < 3; i++) foreach (var stroke in pigment[i]) stroke.SetActive(State.Painted[i]);
            if (View != null)
            {
                brushHand.position = View.transform.TransformPoint(new Vector3(.38f, -.36f + State.Focus * .025f, .67f));
                brushHand.rotation = View.transform.rotation * Quaternion.Euler(0, 0, Mathf.Sin(State.Elapsed * 1.7f) * 2);
            }
            windowLight.intensity = Mathf.Lerp(SharedRoom?0f:1.35f, SharedRoom?0f:2.15f, (State.Phase + State.Focus / 1.25f) / 3f);
        }
        void UpdateStillLife(float dt)
        {
            float cupAway = State.CupContact ? 0 : Mathf.Sin((State.Elapsed % 5 - 2.7f) / 2.3f * Mathf.PI) * .57f;
            cupHand.localPosition = cupHandContact + new Vector3(cupAway, cupAway * .35f, 0);
            cupContact.gameObject.SetActive(State.CupContact);
            float gap = State.HandsContact ? 0 : Mathf.Sin((State.Elapsed % 6 - 3.8f) / 2.2f * Mathf.PI) * .43f;
            leftHand.localPosition = leftContact + Vector3.left * gap;
            rightHand.localPosition = rightContact + Vector3.right * gap;
            handsContact.gameObject.SetActive(State.HandsContact);
            float light = State.Focus / 1.25f * .18f;
            scarfRenderer.sharedMaterial.color = new Color(.64f + (State.Phase == 0 ? light : 0), .31f + (State.Phase == 0 ? light : 0), .28f);
            cupContact.sharedMaterial.color = new Color(.80f + (State.Phase == 1 ? light : 0), .67f, .54f);
            handsContact.sharedMaterial.color = new Color(.75f + (State.Phase == 2 ? light : 0), .63f, .51f);
        }
    }
}





