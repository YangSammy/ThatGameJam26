using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox.V3
{
    public sealed class HubHotspot : MonoBehaviour { public int Chapter; }

    public sealed class RoomWorld
    {
        public Camera Camera;
        public GameObject Root { get; private set; }
        public int AimedStation { get; private set; } = -1;
        public Vector3[] StationPositions;
        public int Season { get; private set; }
        public bool DoorOpen { get; private set; }
        public float ThawProgress { get; private set; } = 1;
        public bool ThroughDoor => DoorOpen && Camera.transform.position.z < -3.35f;
        readonly List<Material> materials = new List<Material>();
        readonly List<Bounds> furniture = new List<Bounds>();
        readonly List<Renderer> leaves = new List<Renderer>();
        readonly List<Renderer> snow = new List<Renderer>();
        readonly List<GameObject> blossoms = new List<GameObject>();
        readonly List<GameObject> autumnLeaves = new List<GameObject>();
        readonly Renderer[] photoSurfaces = new Renderer[3];
        readonly List<Renderer> targets = new List<Renderer>();
        GameObject lightingRoot;
        Transform doorPivot, duvet, clothing;
        Light sun, doorLight;
        Renderer ground;
        bool looking;
        float yaw, pitch, clock;
        int currentDay;

        public void Create(Camera camera)
        {
            Camera = camera;
            Root = new GameObject("Environment_BedroomAndYard");
            var environment = GameObject.Find("Environment");
            if (environment) Root.transform.SetParent(environment.transform, false);
            lightingRoot = new GameObject("Lighting_BedroomV3");
            var lighting = GameObject.Find("Lighting");
            if (lighting) lightingRoot.transform.SetParent(lighting.transform, false);
            StationPositions = new[] { new Vector3(1.54f,.98f,1.92f), new Vector3(-2.16f,1.7f,1.15f), new Vector3(0,.78f,.15f), new Vector3(-1.91f,1.35f,-.65f), new Vector3(0,1.02f,-2.86f) };
            Box("Floor_Bedroom", new Vector3(0,-.10f,0), new Vector3(4.6f,.2f,5.8f), "#9b8a70");
            Box("Wall_West",new Vector3(-2.36f,1.425f,0),new Vector3(.12f,2.85f,5.8f),"#c5bba2");
            Box("Wall_East",new Vector3(2.36f,1.425f,0),new Vector3(.12f,2.85f,5.8f),"#c5bba2");
            Box("Ceiling_Bedroom",new Vector3(0,2.91f,0),new Vector3(4.8f,.12f,6),"#d9d1bc");
            Box("Wall_SouthWest",new Vector3(-1.45f,1.425f,-2.96f),new Vector3(1.8f,2.85f,.12f),"#c5bba2");
            Box("Wall_SouthEast",new Vector3(1.45f,1.425f,-2.96f),new Vector3(1.8f,2.85f,.12f),"#c5bba2");
            Box("Wall_DoorLintel",new Vector3(0,2.55f,-2.96f),new Vector3(1.15f,.6f,.12f),"#c5bba2");
            Box("Wall_NorthLower",new Vector3(0,.54f,2.96f),new Vector3(4.8f,1.08f,.12f),"#c5bba2");
            Box("Wall_NorthUpper",new Vector3(0,2.69f,2.96f),new Vector3(4.8f,.45f,.12f),"#c5bba2");
            Box("Wall_NorthLeft",new Vector3(-1.87f,1.76f,2.96f),new Vector3(1.06f,1.42f,.12f),"#c5bba2");
            Box("Wall_NorthRight",new Vector3(1.87f,1.76f,2.96f),new Vector3(1.06f,1.42f,.12f),"#c5bba2");
            Box("Window_Sill",new Vector3(0,1.08f,2.82f),new Vector3(2.82f,.1f,.46f),"#e3d8bf");
            Box("Window_Mullion",new Vector3(0,1.77f,2.91f),new Vector3(.07f,1.42f,.07f),"#e3d8bf");
            for(int side=-1;side<=1;side+=2) Box("Curtain_"+side,new Vector3(side*1.29f,1.76f,2.78f),new Vector3(.26f,1.6f,.1f),"#b59b76");
            Table("Desk",new Vector3(1.52f,.84f,1.93f),new Vector3(1.28f,.1f,.76f));
            Blocked("Chair_Seat",new Vector3(1.53f,.45f,1.08f),new Vector3(.43f,.08f,.43f),"#8d7a62");
            Box("Chair_Back",new Vector3(1.53f,.73f,.88f),new Vector3(.43f,.55f,.06f),"#8d7a62");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Chair_Leg_"+x+"_"+z,new Vector3(1.53f+x*.17f,.22f,1.08f+z*.17f),new Vector3(.05f,.44f,.05f),"#74634f");
            Blocked("Bed",new Vector3(-1.51f,.33f,1.66f),new Vector3(1.34f,.66f,2.12f),"#725f4c");
            Box("Mattress",new Vector3(-1.51f,.68f,1.66f),new Vector3(1.29f,.18f,2.05f),"#d8cbb0");
            duvet=Box("Duvet_Seasonal",new Vector3(-1.51f,.82f,1.4f),new Vector3(1.3f,.16f,1.48f),"#7f9389").transform;
            Box("Pillow",new Vector3(-1.51f,.84f,2.42f),new Vector3(.84f,.17f,.37f),"#e4dbc6");
            Blocked("Wardrobe",new Vector3(1.77f,1.05f,-1.88f),new Vector3(.9f,2.1f,1.5f),"#77644f");
            Box("Wardrobe_Handle",new Vector3(1.29f,1.1f,-1.89f),new Vector3(.06f,.2f,.035f),"#b1a58b");
            Table("TeaTable",new Vector3(0,.69f,.15f),new Vector3(.98f,.1f,.66f));
            Box("Letter_Entry",StationPositions[2],new Vector3(.55f,.014f,.39f),"#e9dfc6",2);
            Box("Pen_Entry",new Vector3(.27f,.8f,.11f),new Vector3(.023f,.025f,.28f),"#3d4f50");
            Box("Rack_Stand",new Vector3(-1.78f,.9f,-2.16f),new Vector3(.07f,1.8f,.07f),"#695b4b");
            Box("Rack_Crossbar",new Vector3(-1.78f,1.68f,-2.16f),new Vector3(.65f,.06f,.07f),"#695b4b");
            clothing=Box("Coat_Seasonal",new Vector3(-1.78f,1.32f,-2.16f),new Vector3(.53f,.7f,.14f),"#879486").transform;
            Box("Easel_Board",StationPositions[3],new Vector3(.13f,.76f,.65f),"#ded5bd",3);
            for(int s=-1;s<=1;s+=2) Box("Easel_Leg",new Vector3(-1.91f,.55f,-.65f+s*.25f),new Vector3(.06f,1.1f,.06f),"#77644f");
            for(int i=0;i<3;i++)
            {
                Vector3 p=new Vector3(-2.255f,1.7f, .48f+i*.5f);
                Box("Frame_"+i,p,new Vector3(.07f,.48f,.42f),"#77644f",1);
                photoSurfaces[i]=Box("Photo_"+i,p+Vector3.right*.045f,new Vector3(.016f,.40f,.34f),"#e8ddc5",1).GetComponent<Renderer>();
            }
            Box("Radio_Entry",StationPositions[0],new Vector3(.65f,.27f,.29f),"#65716d",0);
            for(int i=0;i<2;i++) Box("Radio_Slot_"+i,StationPositions[0]+new Vector3(-.16f+i*.32f,.01f,-.154f),new Vector3(.24f,.15f,.02f),"#2b3937",0);
            Box("CassetteBox",new Vector3(.93f,.96f,1.88f),new Vector3(.25f,.15f,.3f),"#ab8c63",0);
            for(int i=0;i<5;i++) Box("Cassette_Stored_"+i,new Vector3(.87f+i*.027f,1.06f,1.86f),new Vector3(.02f,.10f,.16f),i%2==0?"#66796e":"#c6bfa7",0);
            for(int i=0;i<4;i++)
            {
                Box("ArchiveBox_"+i,new Vector3(1.79f,.19f+i*.32f,-.42f),new Vector3(.66f,.3f,.48f),"#a88f69");
                Box("Shelf_Books_"+i,new Vector3(-2.17f,2.35f,1.82f-i*.35f),new Vector3(.15f,.32f,.12f),i%2==0?"#7d8b7c":"#9d6e5d");
            }
            doorPivot=new GameObject("Door_Hinge_South").transform;
            doorPivot.SetParent(Root.transform,false);doorPivot.localPosition=new Vector3(-.54f,0,-2.9f);
            var door=Box("Door_Exit",Vector3.zero,new Vector3(1.08f,2.2f,.08f),"#87735b",4);
            door.transform.SetParent(doorPivot,false);door.transform.localPosition=new Vector3(.54f,1.1f,0);
            var handle=Box("Door_Handle",Vector3.zero,new Vector3(.16f,.045f,.07f),"#c3ac79",4);
            handle.transform.SetParent(doorPivot,false);handle.transform.localPosition=new Vector3(.92f,1.02f,.08f);
            var lightGo=new GameObject("Light_DoorMemory");lightGo.transform.SetParent(lightingRoot.transform,false);lightGo.transform.position=new Vector3(0,1.4f,-3.65f);
            doorLight=lightGo.AddComponent<Light>();doorLight.type=LightType.Point;doorLight.range=6;doorLight.color=new Color(1,.88f,.64f);
            Box("Threshold_Path",new Vector3(0,-.09f,-4.6f),new Vector3(1.3f,.18f,3.6f),"#ddd5bd");
            BuildYard();
            var sunObject=new GameObject("Light_WindowSun");sunObject.transform.SetParent(lightingRoot.transform,false);sunObject.transform.rotation=Quaternion.Euler(40,-24,0);
            sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.6f;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.49f,.5f,.46f);
            Camera.nearClipPlane=.035f;Camera.farClipPlane=100;Camera.fieldOfView=65;
            Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=new Color(.70f,.76f,.75f);
            Camera.transform.position=new Vector3(.35f,1.62f,-2.28f);yaw=0;pitch=3;Camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            ApplySeason(0);
        }

        GameObject Box(string name,Vector3 position,Vector3 size,string color,int target=-1,PrimitiveType shape=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(Root.transform,false);g.transform.localPosition=position;g.transform.localScale=size;g.layer=9;
            var m=new Material(Shader.Find("Standard"));ColorUtility.TryParseHtmlString(color,out Color c);m.color=c;m.SetFloat("_Glossiness",.13f);materials.Add(m);g.GetComponent<Renderer>().sharedMaterial=m;
            if(target>=0){g.AddComponent<HubHotspot>().Chapter=target;targets.Add(g.GetComponent<Renderer>());}
            return g;
        }
        void Blocked(string name,Vector3 p,Vector3 size,string c){var g=Box(name,p,size,c);Bounds b=g.GetComponent<Renderer>().bounds;b.Expand(new Vector3(.42f,0,.42f));furniture.Add(b);}
        void Table(string name,Vector3 p,Vector3 size)
        {
            Blocked(name+"_Top",p,size,"#8e795e");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(name+"_Leg",new Vector3(p.x+x*(size.x*.5f-.08f),p.y*.5f,p.z+z*(size.z*.5f-.08f)),new Vector3(.06f,p.y,.06f),"#77644f");
        }
        void BuildYard()
        {
            ground=Box("Ground_Yard",new Vector3(0,-.18f,11),new Vector3(32,.16f,16),"#799071").GetComponent<Renderer>();
            Box("Road_Neighborhood",new Vector3(0,-.085f,13),new Vector3(32,.03f,3.1f),"#777d79");
            Box("Path_Yard",new Vector3(0,-.05f,7),new Vector3(1.1f,.04f,7),"#b5ad94");
            for(int i=-6;i<=6;i++)Box("Fence_Post_"+(i+6),new Vector3(i*.95f,.44f,10.6f),new Vector3(.07f,.9f,.07f),"#b4a389");
            Box("Fence_Rail",new Vector3(0,.6f,10.6f),new Vector3(12,.07f,.05f),"#b4a389");
            for(int i=0;i<3;i++)
            {
                float x=-7+i*7;
                Box("Neighbor_House_"+i,new Vector3(x,1.4f,18.2f),new Vector3(4.5f,2.8f,3.7f),i==1?"#b4b5a4":"#b8a790");
                Box("Neighbor_Roof_"+i,new Vector3(x,3.02f,18.2f),new Vector3(4.9f,.45f,4.1f),"#736c64");
                for(int w=-1;w<=1;w+=2)Box("Neighbor_Window_"+i+"_"+w,new Vector3(x+w*1.15f,1.8f,16.32f),new Vector3(.72f,.8f,.035f),"#c9d3ce");
            }
            for(int i=0;i<4;i++)
            {
                float x=i%2==0?-3.5f-i*.35f:3.5f+i*.3f,z=5.5f+i;
                Box("Tree_Trunk_"+i,new Vector3(x,1.4f,z),new Vector3(.24f,1.4f,.24f),"#75664d",-1,PrimitiveType.Cylinder);
                for(int j=0;j<3;j++)
                {
                    var branch=Box("Tree_Branch_"+i+"_"+j,new Vector3(x+(j-1)*.43f,2.4f+j*.15f,z),new Vector3(.10f,1.45f,.10f),"#75664d");branch.transform.localEulerAngles=new Vector3(0,0,(j-1)*36);
                    leaves.Add(Box("Tree_Leaves_"+i+"_"+j,new Vector3(x+(j-1)*.66f,3+j*.22f,z),new Vector3(1.7f,1.45f,1.6f),"#7d9871",-1,PrimitiveType.Sphere).GetComponent<Renderer>());
                    snow.Add(Box("Snow_Branch_"+i+"_"+j,new Vector3(x+(j-1)*.5f,2.92f+j*.2f,z),new Vector3(.9f,.12f,.35f),"#e9ece6").GetComponent<Renderer>());
                }
            }
            Box("Flower_Pot",new Vector3(.62f,1.22f,2.8f),new Vector3(.2f,.21f,.2f),"#a9785c",-1,PrimitiveType.Cylinder);
            for(int i=0;i<3;i++)
            {
                Box("Flower_Stem_"+i,new Vector3(.58f+i*.04f,1.43f,2.8f),new Vector3(.018f,.3f,.018f),"#65816c");
                blossoms.Add(Box("Flower_Blossom_"+i,new Vector3(.58f+i*.04f,1.58f,2.8f),Vector3.one*.1f,"#d0ad9d",-1,PrimitiveType.Sphere));
            }
            for(int i=0;i<15;i++)autumnLeaves.Add(Box("Leaf_Autumn_"+i,new Vector3(-.8f+(i%5)*.23f,.018f,2.1f+(i/5)*.16f),new Vector3(.11f,.012f,.06f),"#b48751"));
        }
        public void SetDay(int day)
        {
            currentDay=day;if(day<4){DoorOpen=false;doorPivot.localRotation=Quaternion.identity;}
            ApplySeason(Mathf.Min(day,3));if(day==4)ApplySeason(0);
            ThawProgress=day==4?0:1;if(day==4)ApplyThaw();doorLight.intensity=day==4?2.7f:0;
        }
        void ApplyThaw()
        {
            float t=ThawProgress;
            ground.sharedMaterial.color=Color.Lerp(new Color(.83f,.87f,.86f),new Color(.47f,.56f,.42f),t);
            foreach(var r in snow){r.enabled=t<.99f;Vector3 s=r.transform.localScale;s.y=.12f*(1-t);r.transform.localScale=s;}
            foreach(var r in leaves){r.enabled=t>.48f;r.transform.localScale=Vector3.Lerp(new Vector3(.05f,.05f,.05f),new Vector3(1.7f,1.45f,1.6f),Mathf.InverseLerp(.4f,1,t));}
            foreach(var g in blossoms){g.SetActive(t>.12f);g.transform.localScale=Vector3.one*Mathf.Lerp(.008f,.1f,t);}
        }
        public void ApplySeason(int season)
        {
            Season=season;bool winter=season==3;
            ground.sharedMaterial.color=winter?new Color(.83f,.87f,.86f):season==2?new Color(.56f,.48f,.32f):new Color(.47f,.56f,.42f);
            foreach(var r in leaves){r.enabled=!winter;r.sharedMaterial.color=season==2?new Color(.67f,.43f,.23f):season==1?new Color(.38f,.55f,.34f):new Color(.55f,.66f,.44f);}
            foreach(var r in snow)r.enabled=winter;
            foreach(var r in snow)r.transform.localScale=new Vector3(.9f,.12f,.35f);
            foreach(var r in leaves)r.transform.localScale=new Vector3(1.7f,1.45f,1.6f);
            foreach(var g in blossoms)g.SetActive(season==0);
            foreach(var g in blossoms)g.transform.localScale=Vector3.one*.1f;
            foreach(var g in autumnLeaves)g.SetActive(season==2);
            duvet.localScale=new Vector3(1.3f,season==1?.055f:winter?.25f:.14f,1.48f);
            duvet.GetComponent<Renderer>().sharedMaterial.color=winter?new Color(.58f,.61f,.63f):season==1?new Color(.74f,.76f,.65f):new Color(.48f,.59f,.55f);
            clothing.localScale=new Vector3(.53f,winter?.95f:season==1?.52f:.7f,winter?.23f:.13f);
            sun.intensity=season==1?1.35f:winter?.65f:.95f;sun.color=winter?new Color(.78f,.86f,1):new Color(1,.91f,.74f);
        }
        public void SetPhotos(Texture2D[] images)
        {
            for(int i=0;i<3;i++){photoSurfaces[i].sharedMaterial.color=Color.white;if(images!=null&&i<images.Length&&images[i]!=null)photoSurfaces[i].sharedMaterial.mainTexture=images[i];else photoSurfaces[i].sharedMaterial.color=new Color(.65f,.72f,.64f);}
        }
        public void OpenDoor(){if(currentDay!=4)return;DoorOpen=true;}
        public void SetVisible(bool visible){Root.SetActive(visible);lightingRoot.SetActive(visible);}
        public void EnableLook(bool enabled){looking=enabled;Cursor.lockState=enabled?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!enabled;Vector3 e=Camera.transform.eulerAngles;yaw=e.y;pitch=e.x>180?e.x-360:e.x;}
        public void WalkTick(float dt)
        {
            clock+=dt;
            if(currentDay==4&&ThawProgress<1){ThawProgress=Mathf.Min(1,ThawProgress+dt/5);ApplyThaw();}
            if(looking)
            {
                yaw+=Input.GetAxisRaw("Mouse X")*1.8f;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.8f,-65,70);Camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
                var axis=Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)),1);
                Vector3 step=Quaternion.Euler(0,yaw,0)*new Vector3(axis.x,0,axis.y)*1.75f*dt,pos=Camera.transform.position;
                Vector3 x=pos+Vector3.right*step.x,z=pos+Vector3.forward*step.z;
                if(Clear(x))pos=x;z=new Vector3(pos.x,pos.y,pos.z+step.z);if(Clear(z))pos=z;Camera.transform.position=pos;
            }
            if(DoorOpen)doorPivot.localRotation=Quaternion.Slerp(doorPivot.localRotation,Quaternion.Euler(0,-100,0),dt*3);
            foreach(var r in targets)
            {
                int chapter=r.GetComponent<HubHotspot>().Chapter;float emission=chapter==currentDay?.04f+(.5f+.5f*Mathf.Sin(clock*2))*.07f:0;
                r.sharedMaterial.EnableKeyword("_EMISSION");r.sharedMaterial.SetColor("_EmissionColor",new Color(1,.8f,.45f)*emission);
            }
            UpdateAim();
        }
        bool Clear(Vector3 p)
        {
            if(p.x< -2.08f||p.x>2.08f||p.z>2.65f)return false;
            if(p.z< -2.65f&&(!DoorOpen||Mathf.Abs(p.x)>.43f))return false;
            if(p.z< -5.8f)return false;
            foreach(var b in furniture)if(p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z)return false;
            return true;
        }
        public bool CanStand(Vector3 position){return Clear(position);}
        void UpdateAim()
        {
            AimedStation=-1;if(!Physics.Raycast(Camera.transform.position,Camera.transform.forward,out RaycastHit hit,2.4f,1<<9))return;
            var hs=hit.collider.GetComponent<HubHotspot>();if(hs!=null&&hs.Chapter==currentDay)AimedStation=hs.Chapter;
        }
        public void WarpTo(int chapter)
        {
            Vector3 at=StationPositions[Mathf.Clamp(chapter,0,4)];Vector3 from=at+new Vector3(0,.7f,chapter==4?1.2f:-1.1f);from.y=1.62f;
            Camera.transform.position=from;Camera.transform.LookAt(at);EnableLook(false);AimedStation=chapter;
        }
        static void Release(UnityEngine.Object o){if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
        public void Destroy(){if(Root)Release(Root);if(lightingRoot)Release(lightingRoot);foreach(var m in materials)if(m)Release(m);materials.Clear();}
    }
}
