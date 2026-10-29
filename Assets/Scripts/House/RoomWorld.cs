using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public class RoomWorld
    {
        public Camera Camera;
        public Vector3[] StationPositions;
        public int AimedStation { get; private set; } = -1;
        public GameObject Root { get; private set; }
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Bounds> tables = new List<Bounds>();
        private float yaw, pitch;
        readonly List<BoxCollider> liveBlockers=new List<BoxCollider>();
        Vector3 velocity;
        public float WalkSpeed=1.8f,LookSensitivity=2.1f;
        public Vector3 Velocity => velocity;
        public void StopMotion(){velocity=Vector3.zero;}
        private bool looking;
        private bool shared;
        private bool study;
        private StudyCurtain curtain;
        private Material wall, wood, metal, paper, dark, red, glass;

        public void Create(Camera camera)
        {
            Destroy(); Camera = camera;
            Root = new GameObject("Memory Room - runtime geometry");
            StationPositions = new[] {
                new Vector3(-3, .75f, 1.2f), new Vector3(-1.3f, .75f, 3.3f),
                new Vector3(1.3f, .75f, 3.3f), new Vector3(3, .75f, 1.2f),
                new Vector3(3, .75f, -2)
            };
            wall = Mat("#d4d3c9"); wood = Mat("#aca794"); metal = Mat("#8b9492");
            paper = Mat("#ebe5d3"); dark = Mat("#485451"); red = Mat("#9f514b"); glass = Mat("#bacbd0");
            Part("Floor", Root.transform, new Vector3(0, -.12f, 0), new Vector3(10, .24f, 10), wood);
            Part("West wall", Root.transform, new Vector3(-5, 1.5f, 0), new Vector3(.18f, 3, 10), wall);
            Part("East wall", Root.transform, new Vector3(5, 1.5f, 0), new Vector3(.18f, 3, 10), wall);
            Part("Far wall", Root.transform, new Vector3(0, 1.5f, 5), new Vector3(10, 3, .18f), wall);
            Part("Near wall", Root.transform, new Vector3(0, 1.5f, -5), new Vector3(10, 3, .18f), wall);
            Part("Door", Root.transform, new Vector3(-1.7f, 1.05f, -4.89f), new Vector3(1.15f, 2.1f, .04f), wood);
            Part("Door handle", Root.transform, new Vector3(-1.25f, 1.05f, -4.84f), new Vector3(.16f, .04f, .06f), metal);
            for (int i = -4; i <= 4; i++)
                Part("Floor seam", Root.transform, new Vector3(i, .005f, 0), new Vector3(.012f, .008f, 10), metal);
            BuildWindow();
            for (int i = 0; i < StationPositions.Length; i++) BuildStation(i);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.44f, .46f, .44f);
            GameObject sun = new GameObject("Window daylight"); sun.transform.SetParent(Root.transform, false);
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            Light directional = sun.AddComponent<Light>(); directional.type = LightType.Directional;
            directional.intensity = .8f; directional.color = new Color(1f, .96f, .84f);
            directional.shadows = LightShadows.Soft; directional.shadowStrength = .65f;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 35;
            GameObject glow = new GameObject("Warm room lamp"); glow.transform.SetParent(Root.transform, false);
            glow.transform.position = new Vector3(0, 2.65f, .4f);
            Light lamp = glow.AddComponent<Light>(); lamp.type = LightType.Point;
            lamp.range = 10; lamp.intensity = 1.4f; lamp.color = new Color(1, .84f, .64f);
            Camera.transform.position = new Vector3(0, 1.65f, -3.4f);
            yaw = 0; pitch = 0; Camera.transform.rotation = Quaternion.identity;
            Camera.nearClipPlane = .05f; Camera.farClipPlane = 40; Camera.fieldOfView = 70;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(.67f, .69f, .65f);
            AimedStation = -1;
        }
        private Material Mat(string hex)
        {
            Material template = Resources.Load<Material>("WhiteboxMaterial");
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material m = template != null ? new Material(template) : new Material(shader); Color c; ColorUtility.TryParseHtmlString(hex, out c); m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .12f);
            materials.Add(m); return m;
        }
        private GameObject Part(string name, Transform parent, Vector3 position, Vector3 size, Material material,
            PrimitiveType shape = PrimitiveType.Cube, Vector3? rotation = null)
        {
            GameObject go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            if (rotation.HasValue) go.transform.localEulerAngles = rotation.Value;
            go.GetComponent<Renderer>().sharedMaterial = material;
            // Navigation uses explicit bounds; these proxies need no physics.
            Collider collider = go.GetComponent<Collider>(); if (collider != null) Release(collider);
            return go;
        }
        private void BuildWindow()
        {
            Part("Window", Root.transform, new Vector3(4.89f, 1.95f, 1.5f), new Vector3(.035f, 1.2f, 2), glass);
            Part("Window sill", Root.transform, new Vector3(4.73f, 1.33f, 1.5f), new Vector3(.5f, .1f, 2.2f), paper);
            Part("Window upright", Root.transform, new Vector3(4.84f, 1.95f, 1.5f), new Vector3(.06f, 1.3f, .05f), paper);
            Part("Window crossbar", Root.transform, new Vector3(4.84f, 1.95f, 1.5f), new Vector3(.06f, .05f, 2.1f), paper);
            Part("Left curtain", Root.transform, new Vector3(4.72f, 1.95f, .33f), new Vector3(.12f, 1.6f, .3f), wall);
            Part("Right curtain", Root.transform, new Vector3(4.72f, 1.95f, 2.67f), new Vector3(.12f, 1.6f, .3f), wall);
        }
        private void BuildStation(int index)
        {
            Vector3 at = StationPositions[index];
            GameObject station = new GameObject("Station " + index); station.transform.SetParent(Root.transform, false);
            station.transform.position = at;
            Vector3 inward = new Vector3(-at.x, 0, -at.z).normalized;
            station.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg + 180, 0);
            Transform t = station.transform;
            Part("Desk surface", t, Vector3.zero, new Vector3(1.45f, .12f, .85f), wood);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                Part("Desk leg", t, new Vector3(x * .62f, -.37f, z * .32f), new Vector3(.08f, .74f, .08f), metal);
            // Axis-aligned expanded world bounds are deliberately conservative.
            Renderer top = t.Find("Desk surface").GetComponent<Renderer>();
            Bounds bounds = top.bounds; bounds.Expand(new Vector3(.48f, 0, .48f)); tables.Add(bounds);
            Part("Side stool", t, new Vector3(-1.0f, -.28f, .1f), new Vector3(.4f, .12f, .4f), wood);
            Part("Stool leg", t, new Vector3(-1.0f, -.52f, .1f), new Vector3(.12f, .47f, .12f), metal);
            if (index == 0)
            {
                Part("Tape recorder", t, new Vector3(0, .24f, 0), new Vector3(.92f, .35f, .49f), metal);
                for (int x = -1; x <= 1; x += 2)
                {
                    Part("Tape reel", t, new Vector3(x * .23f, .31f, -.255f), new Vector3(.24f, .018f, .24f), dark, PrimitiveType.Cylinder, new Vector3(90, 0, 0));
                    Part("Tape hub", t, new Vector3(x * .23f, .31f, -.28f), new Vector3(.055f, .02f, .055f), paper, PrimitiveType.Cylinder, new Vector3(90, 0, 0));
                }
                Part("Record key", t, new Vector3(.28f, .13f, -.27f), new Vector3(.1f, .055f, .04f), red);
                Part("Play key", t, new Vector3(.12f, .13f, -.27f), new Vector3(.1f, .055f, .04f), paper);
            }
            else if (index == 1)
            {
                Part("Camera body", t, new Vector3(0, .23f, .02f), new Vector3(.56f, .31f, .23f), dark);
                Part("Camera top", t, new Vector3(0, .43f, .02f), new Vector3(.24f, .13f, .2f), metal);
                Part("Camera lens", t, new Vector3(0, .23f, -.17f), new Vector3(.22f, .12f, .22f), metal, PrimitiveType.Cylinder, new Vector3(90, 0, 0));
                Part("Lens glass", t, new Vector3(0, .23f, -.3f), new Vector3(.17f, .008f, .17f), glass, PrimitiveType.Cylinder, new Vector3(90, 0, 0));
                Part("Film cartridge", t, new Vector3(.48f, .17f, .05f), new Vector3(.12f, .1f, .12f), metal, PrimitiveType.Cylinder);
                Part("Reference photo", t, new Vector3(-.45f, .068f, -.05f), new Vector3(.26f, .008f, .34f), paper);
            }
            else if (index == 2)
            {
                Part("Unfinished letter", t, new Vector3(0, .069f, -.05f), new Vector3(.65f, .008f, .48f), paper);
                Part("Pen", t, new Vector3(.29f, .09f, -.04f), new Vector3(.025f, .025f, .37f), dark, PrimitiveType.Cube, new Vector3(0, -22, 0));
                Part("Ink bottle", t, new Vector3(-.45f, .13f, .2f), new Vector3(.11f, .13f, .11f), dark);
                Part("Paper ball", t, new Vector3(.48f, .15f, .16f), Vector3.one * .13f, paper, PrimitiveType.Sphere);
                Part("Wastebasket", t, new Vector3(.95f, -.5f, -.1f), new Vector3(.35f, .25f, .35f), metal, PrimitiveType.Cylinder);
            }
            else if (index == 3)
            {
                Part("Canvas frame", t, new Vector3(0, .52f, .08f), new Vector3(.74f, .75f, .07f), wood);
                Part("Canvas", t, new Vector3(0, .52f, .035f), new Vector3(.65f, .66f, .025f), paper);
                Part("Easel support", t, new Vector3(0, .2f, .12f), new Vector3(.07f, .4f, .07f), dark);
                Part("Brush", t, new Vector3(.5f, .1f, -.08f), new Vector3(.025f, .025f, .38f), dark);
                Part("Palette", t, new Vector3(-.49f, .1f, -.12f), new Vector3(.25f, .025f, .2f), metal, PrimitiveType.Cylinder);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    Part("Old scarf", t, new Vector3(-.42f + i * .27f, .1f + i * .003f, Mathf.Sin(i * 1.7f) * .1f), new Vector3(.37f, .05f, .22f), red, PrimitiveType.Cube, new Vector3(0, i % 2 == 0 ? -12 : 12, 0));
                Part("Folded cloth", t, new Vector3(0, .08f, .25f), new Vector3(.6f, .025f, .18f), paper);
            }
        }
        public void WalkTick(float dt)
        {
            var axes=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
            StepInput(axes,PlayerInputPolicy.LookDelta,dt);
        }
        // Same movement boundary for live input and Play Mode tests. Small steps prevent thin-obstacle tunnelling.
        public void StepInput(Vector2 axes,Vector2 look,float dt)
        {
            if(Camera==null)return;
            if(!looking){StopMotion();return;}
            yaw+=look.x*Mathf.Clamp(LookSensitivity,.1f,8);
            pitch=PlayerControlSettings.Current.ClampPitch(pitch-look.y*Mathf.Clamp(LookSensitivity,.1f,8));
            Camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            axes=Vector2.ClampMagnitude(axes,1);
            Vector3 wanted=Quaternion.Euler(0,yaw,0)*new Vector3(axes.x,0,axes.y)*Mathf.Max(.1f,WalkSpeed);
            float remaining=Mathf.Clamp(dt,0,.1f);
            while(remaining>0)
            {
                float stepTime=Mathf.Min(remaining,.01f);remaining-=stepTime;
                velocity=PlayerControlSettings.Current.ApproachVelocity(velocity,wanted,stepTime);
                Vector3 step=velocity*stepTime,pos=Camera.transform.position;
                var next=new Vector3(Mathf.Clamp(pos.x+step.x,shared?-3.45f:-4.6f,shared?3.45f:4.6f),pos.y,pos.z);
                if(Clear(next))pos=next;else velocity.x=0;
                next=new Vector3(pos.x,pos.y,Mathf.Clamp(pos.z+step.z,shared?-4.05f:-4.6f,shared?4.05f:4.6f));
                if(Clear(next))pos=next;else velocity.z=0;
                Camera.transform.position=pos;
            }
            UpdateAim();
        }
        public bool CanWalkAt(Vector3 position){return (!shared || (position.x>=-3.45f && position.x<=3.45f && position.z>=-4.05f && position.z<=4.05f)) && Clear(position);}
        private bool Clear(Vector3 position)
        {
            if(curtain!=null && curtain.Blocks(position))return false;
            foreach(var box in liveBlockers)
            {
                if(box==null || !box.enabled || !box.gameObject.activeInHierarchy)continue;
                var b=box.bounds;b.Expand(new Vector3(.36f,0,.36f));
                if(position.x>b.min.x && position.x<b.max.x && position.z>b.min.z && position.z<b.max.z)return false;
            }
            foreach (Bounds b in tables)
                if (position.x > b.min.x && position.x < b.max.x && position.z > b.min.z && position.z < b.max.z) return false;
            return true;
        }
        private void UpdateAim()
        {
            if(study){var entry=StudyEntry.Pick(Camera);AimedStation=entry!=null?entry.chapter:-1;return;}
            AimedStation = -1; float best = .9f;
            for (int i = 0; i < StationPositions.Length; i++)
            {
                Vector3 target = StationPositions[i] + Vector3.up * .35f;
                Vector3 delta = target - Camera.transform.position;
                if (delta.magnitude > 2.5f) continue;
                float dot = Vector3.Dot(Camera.transform.forward, delta.normalized);
                if (dot > best) { best = dot; AimedStation = i; }
            }
        }
        public void WarpTo(int index)
        {
            if (Camera == null || StationPositions == null) return;
            index = Mathf.Clamp(index, 0, StationPositions.Length - 1);
            Vector3 at = StationPositions[index], inward = new Vector3(-at.x, 0, -at.z).normalized;
            Camera.transform.position = shared?HouseLayout.Approaches[index]:new Vector3(at.x + inward.x * 1.7f, 1.65f, at.z + inward.z * 1.7f);
            Vector3 target=at+Vector3.up*.35f;
            if(study && Root!=null)foreach(var entry in Root.GetComponentsInChildren<StudyEntry>())if(entry.chapter==index && entry.GetComponent<Collider>()!=null){target=entry.GetComponent<Collider>().bounds.center;break;}
            Vector3 delta = target - Camera.transform.position;
            yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            Camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0); UpdateAim();
        }
        public void EnableLook(bool enable)
        {
            if(enable && Camera!=null){var e=Camera.transform.eulerAngles;yaw=e.y;pitch=e.x>180?e.x-360:e.x;}
            StopMotion();looking = enable;PlayerInputPolicy.SetCursor(!enable);
        }
        public void StepFreePointer(Vector2 axes,Vector2 look,float dt)
        {
            bool previous=looking;looking=true;
            StepInput(axes,look,dt);
            looking=previous;
        }
        public void CreateShared(Camera camera,GameObject savedRoom=null)
        {
            shared=true;Camera=camera;Root=savedRoom;
            study=WBMain.Instance!=null && WBMain.Instance.deskStudy;
            StationPositions=new Vector3[5];
            for(int i=0;i<5;i++)StationPositions[i]=HouseLayout.Targets[i]-Vector3.up*.35f;
            if(Root==null)Root=HouseRoomGeometry.Build();
            curtain=Root.GetComponentInChildren<StudyCurtain>();
            tables.Clear();liveBlockers.Clear();
            foreach(var box in Root.GetComponentsInChildren<BoxCollider>(true))
                if(box.name.StartsWith("Walk blocker"))liveBlockers.Add(box);
            Camera.nearClipPlane=.05f;Camera.farClipPlane=70;Camera.cullingMask &= ~(1<<26);
            var angles=Camera.transform.eulerAngles;yaw=angles.y;pitch=angles.x>180?angles.x-360:angles.x;
        }
        void AddObstacle(Vector3 center,Vector3 size){tables.Add(new Bounds(center,size));}
        public void Destroy()
        {
            if (Root != null) Release(Root);
            foreach (Material m in materials) if (m != null) Release(m);
            materials.Clear(); tables.Clear();liveBlockers.Clear();StopMotion(); AimedStation = -1;
        }
        private static void Release(Object item)
        {
            if (Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item);
        }
    }
}


