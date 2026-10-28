using UnityEngine;

namespace MemoryWhitebox
{
    // A tangible strip with thickness, changing folds and two embodied hands.
    public sealed class EndingWorld3D : WorldChapter3D
    {
        public readonly EndingChapter Ending;
        public Mesh ScarfMesh { get; private set; }
        readonly Transform handle, destination, leftHand, rightHand;
        public override Transform[] PlayerHandTargets => new[]{leftHand,rightHand};
        public override bool PlayerHandsEngaged => Ending.Dragging || Ending.LeftHeld || Ending.Phase>=3;
        readonly Renderer leftSkin;
        readonly Vector3[] centerline = new Vector3[15];
        readonly Vector3[] vertices = new Vector3[15 * 4];
        readonly int[] triangles = new int[(15 - 1) * 24 + 12];
        readonly Vector3[] weavePoints = new Vector3[15];
        LineRenderer weave;
        float elapsed;
        const float ClothY = .87f;
        const float Scale = .0022f;

        public EndingWorld3D(WBMain host, EndingChapter model) : base(host, model, 4)
        {
            Ending = model; Fov = 50;
            Eye = SharedRoom?(Host.deskStudy?new Vector3(-.8f,1.7f,1.2f):new Vector3(2.65f,1.7f,1.4f)):World(new Vector3(0, 1.8f, -1.48f)); Look = World(new Vector3(0, .86f, .04f));
            Room(6, 6);
            if(!SharedRoom){
            Part(null, new Vector3(0, .73f, .05f), new Vector3(2.25f, .19f, 1.5f), "#8a7259");
            Part(null, new Vector3(0, .832f, .05f), new Vector3(2.22f, .014f, 1.47f), "#9b8062");
            // The same small domestic cup from the painted memory remains nearby.
            Part(null, new Vector3(.82f, .94f, .51f), new Vector3(.12f, .20f, .12f), "#c7c4a5", PrimitiveType.Cylinder);
            }
            GameObject cloth = new GameObject("Scarf fabric mesh"); cloth.transform.SetParent(Root.transform, false);
            MeshFilter mf = cloth.AddComponent<MeshFilter>(); MeshRenderer mr = cloth.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material("#943836");
            ScarfMesh = new Mesh(); ScarfMesh.name = "Procedural scarf with thickness"; ScarfMesh.MarkDynamic(); mf.sharedMesh = ScarfMesh;
            BuildTriangles();
            weave = Line("Scarf weave", weavePoints, "#c88465", .008f);
            handle = Part("ending.scarf", Vector3.zero, new Vector3(.13f, .043f, .12f), "#b66f55", PrimitiveType.Sphere).transform;
            destination = Group("Remembered fold indentation", Vector3.zero);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Child(destination, null, new Vector3(Mathf.Cos(a) * .081f, 0, Mathf.Sin(a) * .081f), new Vector3(.024f, .006f, .01f), "#d2bd8d", PrimitiveType.Cube, new Vector3(0, -a * Mathf.Rad2Deg, 0));
            }
            leftHand = MakeHand("Left stabilizing hand", new Vector3(-.5f, .90f, -.48f), "#d2aa86", false);
            rightHand = MakeHand("Right manipulating hand", new Vector3(.48f, .93f, -.5f), "#c69c7e", true);
            leftSkin = leftHand.GetChild(0).GetComponent<Renderer>();
            UpdateGeometry();
        }
        Transform MakeHand(string name, Vector3 p, string color, bool right)
        {
            Transform g = Group(name, p);
            Child(g, null, Vector3.zero, new Vector3(.14f, .07f, .17f), color, PrimitiveType.Sphere);
            Child(g, null, new Vector3(0, -.015f, -.18f), new Vector3(.11f, .08f, .27f), "#797565", PrimitiveType.Capsule, new Vector3(90, 0, 0));
            for (int i = 0; i < 4; i++) Child(g, null, new Vector3((i - 1.5f) * .033f, 0, .10f), new Vector3(.028f, .028f, .13f), color, PrimitiveType.Capsule, new Vector3(90, 0, 0));
            Child(g, null, new Vector3(right ? -.09f : .09f, 0, .015f), new Vector3(.04f, .035f, .10f), color, PrimitiveType.Capsule, new Vector3(90, 0, right ? -35 : 35));
            return g;
        }
        Vector3 Cloth(Vector2 p) => new Vector3((p.x - 640) * Scale, ClothY, (480 - p.y) * Scale);
        Vector2 Paper(Vector3 local) => new Vector2(local.x / Scale + 640, 480 - local.z / Scale);
        public override void Press(string id, Ray ray, int button)
        {
            if (button != 0 || Ending.Completed || Ending.Phase >= 3 || id != "ending.scarf") return;
            Ending.PointerDown(Ending.Point, button); Move(ray);
        }
        public override void Move(Ray ray)
        {
            if (!Ending.Dragging) return;
            Ending.PointerMove(Paper(Point(ray, ClothY) - Origin)); UpdateGeometry();
        }
        public override void Release(string overId, Ray ray) { Ending.PointerUp(Paper(Point(ray, ClothY) - Origin)); UpdateGeometry(); }
        public override void Tick(float dt, WBInput input)
        {
            elapsed += dt; Ending.Tick(dt, input); UpdateGeometry();
        }
        void BuildTriangles()
        {
            int n = 0;
            for (int i = 0; i < 14; i++)
            {
                int a = i * 4, b = a + 4;
                AddQuad(ref n, a, b, b + 1, a + 1); // top
                AddQuad(ref n, a + 2, a + 3, b + 3, b + 2);
                AddQuad(ref n, a, a + 2, b + 2, b);
                AddQuad(ref n, a + 1, b + 1, b + 3, a + 3);
            }
            AddQuad(ref n, 0, 1, 3, 2); int last = 14 * 4; AddQuad(ref n, last, last + 2, last + 3, last + 1);
        }
        void AddQuad(ref int at, int a, int b, int c, int d)
        {
            triangles[at++] = a; triangles[at++] = b; triangles[at++] = c;
            triangles[at++] = a; triangles[at++] = c; triangles[at++] = d;
        }
        void UpdateGeometry()
        {
            Vector3 start = Cloth(new Vector2(365, 430)), end = Cloth(new Vector2(920, 430));
            Vector3 point = Cloth(Ending.Point);
            for (int i = 0; i < centerline.Length; i++)
            {
                float t = i / (float)(centerline.Length - 1);
                Vector3 p;
                if (Ending.Phase == 0)
                {
                    p = Vector3.Lerp(start, end, t);
                    p += (point - start) * Mathf.Pow(1 - t, 4);
                    p.y += Mathf.Sin(t * Mathf.PI * 4) * .018f * (1 - t);
                }
                else if (Ending.Phase == 1)
                {
                    p = Vector3.Lerp(start, point, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * .045f;
                }
                else
                {
                    Vector3 knot = Cloth(new Vector2(615, 442));
                    Vector3 tail = Ending.Phase == 2 ? point : Cloth(new Vector2(700, 570));
                    if (t < .48f) p = Vector3.Lerp(start, knot, t / .48f);
                    else
                    {
                        float u = (t - .48f) / .52f;
                        p = Vector3.Lerp(knot, tail, u);
                        p.x += Mathf.Sin(u * Mathf.PI * 2) * .075f * (1 - u);
                        p.y += Mathf.Sin(u * Mathf.PI) * .055f;
                    }
                }
                p.y += .008f;
                if(SharedRoom && !Host.deskStudy)p.y-=ChairDrape(p.x);
                centerline[i] = p;
            }
            for (int i = 0; i < centerline.Length; i++)
            {
                Vector3 tangent = centerline[Mathf.Min(i + 1, 14)] - centerline[Mathf.Max(i - 1, 0)];
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized * .057f;
                if (side.sqrMagnitude < .0001f) side = Vector3.right * .057f;
                int k = i * 4;
                vertices[k] = centerline[i] - side; vertices[k + 1] = centerline[i] + side;
                vertices[k + 2] = vertices[k] - Vector3.up * .012f; vertices[k + 3] = vertices[k + 1] - Vector3.up * .012f;
                weavePoints[i] = centerline[i] + side * .5f + Vector3.up * .008f;
            }
            ScarfMesh.vertices = vertices; ScarfMesh.triangles = triangles; ScarfMesh.RecalculateNormals(); ScarfMesh.RecalculateBounds();
            weave.positionCount = weavePoints.Length;
            for (int i = 0; i < weavePoints.Length; i++) weave.SetPosition(i, weavePoints[i]);
            handle.gameObject.SetActive(Ending.Phase < 3);
            handle.localPosition = point + Vector3.up * .025f;
            destination.gameObject.SetActive(Ending.Phase < 3); destination.localPosition = Cloth(Ending.Target) + Vector3.up * .003f;
            if(SharedRoom && !Host.deskStudy){handle.localPosition-=Vector3.up*ChairDrape(point.x);var at=destination.localPosition;at.y-=ChairDrape(at.x);destination.localPosition=at;}
            if (Ending.Phase == 2)
            {
                leftHand.localPosition = Cloth(new Vector2(490, 443)) + new Vector3(0, Ending.LeftHeld ? .04f : .14f, -.04f);
                leftSkin.sharedMaterial.color = Ending.LeftHeld ? new Color(.83f, .67f, .52f) : new Color(.61f, .55f, .45f);
            }
            else if (Ending.Phase >= 3)
                leftHand.localPosition = Vector3.Lerp(leftHand.localPosition, Cloth(new Vector2(575, 450)) + Vector3.up * .04f, .05f);
            rightHand.localPosition = Ending.Phase >= 3 ? Cloth(new Vector2(690, 465)) + Vector3.up * .06f : Ending.Dragging ? point + new Vector3(.04f, .055f, -.05f) : point + new Vector3(.13f, .14f, -.15f);
        }
        public override void Deactivate() { Ending.PointerUp(Ending.Point); base.Deactivate(); }
        // The scarf rests on the cushion in the centre and rises over the chair arms.
        static float ChairDrape(float x)=>.15f*(1-Mathf.SmoothStep(0,1,Mathf.Abs(x)/.48f));
        public override void Dispose() { if(ScarfMesh!=null)ReleaseObject(ScarfMesh);base.Dispose(); }
        public override string Help => "LMB drag  Smooth corner > Fold end\nHold WASD + LMB drag  Tighten";
    }
}


