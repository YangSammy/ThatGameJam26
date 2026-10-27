using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class WorldHotspot : MonoBehaviour
    {
        public WorldChapter3D Owner;
        public string Id;
    }

    // Each chapter owns spatial meshes and colliders. UI is never a puzzle renderer.
    public abstract class WorldChapter3D
    {
        public readonly WBMain Host;
        public readonly Chapter Model;
        public readonly GameObject Root;
        public readonly Camera View;
        public readonly Vector3 Origin;
        public readonly Dictionary<string,GameObject> Nodes = new Dictionary<string,GameObject>();
        public Vector3 Eye, Look;
        public float Fov = 55;
        public bool WantsMouseLook;
        public string HoverId = "";
        public virtual string Help => Model.Help;
        public bool Active { get; private set; }
        protected bool SharedRoom => Host!=null && Host.sharedHouse;
        public virtual Transform[] PlayerHandTargets => new Transform[0];
        public virtual Transform[] PlayerArmTargets => new Transform[0];
        public virtual bool PlayerHandsEngaged => false;
        readonly List<Material> materials = new List<Material>();
        readonly bool ownsRoot = true;

        // A scene-backed chapter borrows its objects; disposal must never delete them.
        protected WorldChapter3D(Chapter model, GameObject root, Camera view)
        {
            Model=model;Root=root;View=view;Origin=root.transform.position;ownsRoot=false;
        }

        protected WorldChapter3D(WBMain host,Chapter model,int index)
        {
            Host=host;Model=model;View=host!=null?host.Room.Camera:Camera.main;
            Origin=host!=null && host.sharedHouse?HouseLayout.Origins[index]:new Vector3(40+index*28,0,0);
            Root=new GameObject("3D Memory "+index+" "+GetType().Name);
            Root.transform.position=Origin;
            Eye=World(new Vector3(0,1.6f,-2));Look=World(new Vector3(0,1,1));
        }
        public virtual void Activate(){Active=true;Root.SetActive(true);}
        public virtual void Deactivate(){Active=false;Root.SetActive(Host!=null && Host.sharedHouse);if(View!=null)View.rect=new Rect(0,0,1,1);}
        public virtual void Tick(float dt,WBInput input){Model.Tick(dt,input);}
        public virtual void Press(string id,Ray ray,int button){}
        public virtual void Move(Ray ray){}
        public virtual void Release(string overId,Ray ray){}
        public virtual void Scroll(string id,float delta,Ray ray){}
        public virtual Ray PointerRay(Vector2 nativeScreenBottomLeft){return View.ScreenPointToRay(nativeScreenBottomLeft);}
        public virtual string Pick(Ray ray)
        {
            RaycastHit[] hits=Physics.RaycastAll(ray,60,1<<8);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){var node=hit.collider.GetComponent<WorldHotspot>();if(node!=null&&node.Owner==this)return node.Id;}
            return "";
        }
        public Vector3 Point(Ray ray,float localY)
        {
            Plane plane=new Plane(Vector3.up,World(new Vector3(0,localY,0)));
            return plane.Raycast(ray,out float d)?ray.GetPoint(d):World(Vector3.zero);
        }
        public Vector3 World(Vector3 local){return Origin+local;}
        protected Transform Group(string name,Vector3 localPosition)
        {
            var go=new GameObject(name);go.transform.SetParent(Root.transform,false);go.transform.localPosition=localPosition;return go.transform;
        }
        protected Material Material(string hex)
        {
            Material template=Resources.Load<Material>("WhiteboxMaterial");
            var m=template!=null?new Material(template):new Material(Shader.Find("Standard"));
            ColorUtility.TryParseHtmlString(hex,out Color c);m.color=c;
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.16f);
            materials.Add(m);return m;
        }
        protected GameObject Part(string id,Vector3 localPosition,Vector3 scale,string hex,PrimitiveType shape=PrimitiveType.Cube,Vector3? euler=null)
        {return Child(Root.transform,id,localPosition,scale,hex,shape,euler);}
        protected GameObject Child(Transform parent,string id,Vector3 localPosition,Vector3 scale,string hex,PrimitiveType shape=PrimitiveType.Cube,Vector3? euler=null)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=string.IsNullOrEmpty(id)?"Scenery":id;
            go.transform.SetParent(parent,false);go.transform.localPosition=localPosition;go.transform.localScale=scale;
            if(euler.HasValue)go.transform.localEulerAngles=euler.Value;
            go.GetComponent<Renderer>().sharedMaterial=Material(hex);go.layer=8;
            if(!string.IsNullOrEmpty(id))
            {
                var target=go.AddComponent<WorldHotspot>();target.Owner=this;target.Id=id;Nodes[id]=go;
            }
            else {var col=go.GetComponent<Collider>();if(col!=null)ReleaseObject(col);}
            return go;
        }
        protected LineRenderer Line(string name,Vector3[] localPoints,string hex,float width=.012f)
        {
            var go=new GameObject(name);go.transform.SetParent(Root.transform,false);var line=go.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.positionCount=localPoints.Length;line.SetPositions(localPoints);line.startWidth=line.endWidth=width;
            line.numCapVertices=4;line.numCornerVertices=3;line.sharedMaterial=Material(hex);return line;
        }
        protected TextMesh Label(string text,Vector3 localPosition,float size=.05f,string hex="#dcd7c9",Vector3? euler=null)
        {
            var go=new GameObject("Device marking "+text);go.transform.SetParent(Root.transform,false);go.transform.localPosition=localPosition;
            if(euler.HasValue)go.transform.localEulerAngles=euler.Value;
            var tm=go.AddComponent<TextMesh>();tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;tm.text=text;tm.characterSize=size*.25f;tm.fontSize=64;tm.anchor=TextAnchor.MiddleCenter;
            ColorUtility.TryParseHtmlString(hex,out Color c);tm.color=c;return tm;
        }
        protected void Room(float width=8,float depth=9)
        {
            if(Host!=null && Host.sharedHouse)return;
            Part(null,new Vector3(0,-.12f,1.5f),new Vector3(width,.24f,depth),"#9d9581");
            Part(null,new Vector3(-width/2,1.6f,1.5f),new Vector3(.14f,3.2f,depth),"#aaa79a");
            Part(null,new Vector3(width/2,1.6f,1.5f),new Vector3(.14f,3.2f,depth),"#aaa79a");
            Part(null,new Vector3(0,1.6f,1.5f+depth/2),new Vector3(width,3.2f,.14f),"#bbb5a2");
            Part(null,new Vector3(width*.24f,1.9f,1.4f+depth/2),new Vector3(width*.26f,1.5f,.07f),"#c9d2cd");
            Part(null,new Vector3(width*.24f,1.9f,1.34f+depth/2),new Vector3(.05f,1.5f,.06f),"#e0d5b8");
            for(int i=0;i<9;i++)Part(null,new Vector3(-width/2+i*width/8,.006f,1.5f),new Vector3(.008f,.006f,depth),"#807d72");
            var lamp=new GameObject("Memory desk warm light");lamp.transform.SetParent(Root.transform,false);lamp.transform.localPosition=new Vector3(-1,2.7f,-.4f);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=1.5f;light.color=new Color(1,.86f,.66f);
        }
        public void ApplyPose()
        {if(View==null)return;View.transform.position=Eye;View.transform.rotation=Quaternion.LookRotation(Look-Eye);View.fieldOfView=Fov;}
        public virtual void Dispose()
        {
            if(ownsRoot&&Root!=null)ReleaseObject(Root);foreach(var m in materials)if(m!=null)ReleaseObject(m);materials.Clear();
        }
        protected static void ReleaseObject(UnityEngine.Object obj){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
    }
}
