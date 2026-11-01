using System;
using System.Collections.Generic;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PaintingWorld3D:WorldChapter3D
    {
        public readonly PaintingChapter State;
        public PaintingDefocus Defocus {get;private set;}
        public string GazeTarget {get;private set;}
        public string RequiredNode=>State.Phase==0?(State.Painted[0]?"shape-bridge":"shape-swing"):!State.Painted[2]?"shape-luggage":!State.Painted[3]?"shape-flower":"shape-pendant";
        public Vector3 RequiredPoint=>Nodes.ContainsKey(RequiredNode)?Nodes[RequiredNode].transform.position:Eye;
        public bool[] Painted=>State.Painted;
        public Transform SharedScarf {get;private set;}
        public override string Help=>State.Help;
        public override bool HasPointerGesture=>focusHeld||keyDragging||looking;
        const int RouteLayer=27,ObservationLayer=28,PaperLayer=29;
        Transform observation,route,actor,swingSeat,swingRope,bridge,crate,gate,linkage,flower,key,hole,ward,nose,paper;
        readonly Transform[] petals=new Transform[6];
        Camera albumCamera;RenderTexture albumTexture;Collider pageCollider;
        Vector3 fixedEye;Quaternion fixedRotation;float yaw,pitch;int originalMask;Rect originalRect;Color originalBackground;CameraClearFlags originalClearFlags;
        bool built,focusHeld,looking,keyDragging,edgeArmed=true;
        Plane keyPlane;Vector3 keyHome,keyOffset;Vector2 lastLook,pointer;
        readonly List<Material> wardMaterials=new List<Material>();
        public PaintingWorld3D(WBMain host,PaintingChapter model):base(host,model,3)
        {State=model;Eye=SharedRoom?new Vector3(-.3f,1.65f,.95f):World(new Vector3(0,1.65f,-3));Look=Eye+Vector3.forward*3;Fov=64;WantsMouseLook=false;}
        public void BindSharedScarf(EndingWorld3D ending){}
        static void Layer(Transform root,int layer){root.gameObject.layer=layer;foreach(Transform child in root)Layer(child,layer);}
        Transform Block(Transform parent,string id,Vector3 p,Vector3 size,string color,PrimitiveType type=PrimitiveType.Cube)
        {return Child(parent,id,p,size,color,type).transform;}
        void BuildAlbum()
        {
            if(built)return;built=true;
            fixedEye=View.transform.position;fixedRotation=View.transform.rotation;
            observation=Group("Winter fixed observation",Vector3.zero);observation.SetPositionAndRotation(fixedEye+fixedRotation*new Vector3(0,-1.65f,3.8f),fixedRotation);
            BuildObservation();Layer(observation,ObservationLayer);
            route=Group("Album route - physical geometry",new Vector3(0,-30,0));BuildRoute();Layer(route,RouteLayer);
            var camera=Group("Album side camera",Vector3.zero);albumCamera=camera.gameObject.AddComponent<Camera>();
            albumCamera.transform.position=route.TransformPoint(new Vector3(8,2,-16));albumCamera.transform.rotation=Quaternion.identity;
            albumCamera.orthographic=true;albumCamera.orthographicSize=5.2f;albumCamera.aspect=1.8f;albumCamera.cullingMask=1<<RouteLayer;
            albumCamera.clearFlags=CameraClearFlags.SolidColor;albumCamera.backgroundColor=new Color(.89f,.87f,.8f);albumCamera.nearClipPlane=.05f;albumCamera.farClipPlane=40;
            albumTexture=new RenderTexture(1280,720,24);albumTexture.name="Physical album page";albumTexture.Create();albumCamera.targetTexture=albumTexture;
            paper=Group("Persistent album and final paper",Vector3.zero);
            Block(paper,"album-backing",Vector3.zero,new Vector3(1.25f,.72f,.015f),"#e5dfc9");
            var page=Block(paper,"album-page",new Vector3(0,0,-.012f),new Vector3(1.2f,.675f,1),"#ffffff",PrimitiveType.Quad);
            var material=new Material(Shader.Find("Unlit/Texture"));material.mainTexture=albumTexture;page.GetComponent<Renderer>().sharedMaterial=material;pageMaterial=material;
            pageCollider=page.GetComponent<Collider>();Layer(paper,PaperLayer);
            BuildWard();
        }
        Material pageMaterial;
        void BuildObservation()
        {
            Block(observation,"",new Vector3(0,-.08f,0),new Vector3(11,.15f,7),"#c4cbc7");
            var swing=Block(observation,"shape-swing",new Vector3(-1.5f,1,0),new Vector3(.8f,.08f,.4f),"#8a7154");
            for(int sign=-1;sign<=1;sign+=2){Block(observation,"",new Vector3(-1.5f+sign*.6f,1.5f,.1f),new Vector3(.06f,3,.06f),"#6c776b");Block(swing,"shape-swing",new Vector3(sign*.4f,8,0),new Vector3(.025f,16,.04f),"#9b8f71");}
            Block(observation,"shape-bridge",new Vector3(1.6f,.6f,.4f),new Vector3(1.8f,.18f,.7f),"#9b9078");
            for(int sign=-1;sign<=1;sign+=2)Block(observation,"shape-bridge",new Vector3(1.6f,1,.4f+sign*.3f),new Vector3(1.8f,.08f,.05f),"#81775f");
            Block(observation,"shape-luggage",new Vector3(-1.3f,.55f,-.45f),new Vector3(.7f,1,.35f),"#866b55");
            Block(observation,"shape-flower",new Vector3(1.2f,.9f,-.5f),new Vector3(.08f,1.4f,.08f),"#778465");
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Block(observation,"shape-flower",new Vector3(1.2f+Mathf.Cos(a)*.22f,1.5f+Mathf.Sin(a)*.22f,-.5f),new Vector3(.2f,.2f,.06f),"#e7dfbd",PrimitiveType.Sphere);}
            var pendant=CreatePendant(observation,"shape-pendant",new Vector3(-.75f,.38f,-.7f),.35f,"#a69a73");
            Block(observation,"",new Vector3(-.75f,.53f,-.69f),new Vector3(.016f,.2f,.02f),"#afa37a");
        }
        // Both the collectible and the carved recess use this same asymmetric shape recipe.
        Transform CreatePendant(Transform parent,string id,Vector3 at,float scale,string color)
        {
            var root=new GameObject(id).transform;root.SetParent(parent,false);root.localPosition=at;root.localScale=Vector3.one*scale;
            var body=Block(root,id,Vector3.zero,new Vector3(.38f,.5f,.09f),color);body.localRotation=Quaternion.Euler(0,0,35);
            Block(root,id,new Vector3(.18f,-.12f,0),new Vector3(.15f,.12f,.09f),color);
            Block(root,id,new Vector3(0,.3f,0),new Vector3(.12f,.12f,.08f),color,PrimitiveType.Sphere);
            return root;
        }
        void BuildRoute()
        {
            Block(route,"ground-a",new Vector3(1.5f,-.15f,0),new Vector3(3,.3f,1),"#939689");
            Block(route,"ground-mid",new Vector3(7.5f,.55f,0),new Vector3(3,.3f,1),"#939689");
            Block(route,"ground-end",new Vector3(14,.55f,0),new Vector3(4,.3f,1),"#939689");
            bridge=Block(route,"route-bridge",new Vector3(10.5f,.6f,0),new Vector3(3,.2f,1),"#aa9a7b");
            swingSeat=Block(route,"swing-seat",Vector3.zero,new Vector3(.65f,.1f,.6f),"#957553");
            swingRope=Block(route,"swing-rope",Vector3.zero,new Vector3(.035f,3,.035f),"#777057");
            Block(route,"swing-pivot",new Vector3(4.5f,3.3f,0),new Vector3(.25f,.2f,.3f),"#6b7766");
            actor=Block(route,"album-person",new Vector3(.7f,.25f,0),new Vector3(.28f,.5f,.28f),"#4e665f",PrimitiveType.Capsule);
            crate=Block(route,"route-luggage",new Vector3(2,.35f,0),new Vector3(.85f,.7f,.65f),"#95795b");
            Block(route,"pressure-plate",new Vector3(4,.03f,0),new Vector3(.9f,.06f,.8f),"#b8ab7a");
            gate=Block(route,"pressure-gate",new Vector3(6,1,0),new Vector3(.18f,2,1),"#798575");
            Block(route,"gate-walkway",new Vector3(7.1f,-.15f,0),new Vector3(2.2f,.3f,1),"#939689");
            linkage=Block(route,"visible-linkage",new Vector3(5,.15f,.3f),new Vector3(2,.055f,.055f),"#b8ab7a");
            flower=Block(route,"flower-hub",new Vector3(8,2,0),new Vector3(.45f,.45f,.45f),"#b9a369",PrimitiveType.Sphere);
            for(int i=0;i<6;i++)
            {
                petals[i]=Block(route,"flower-seat-"+i,Vector3.zero,new Vector3(.72f,.12f,.6f),"#ddd4ae");
                Block(flower,"petal-arm-"+i,Vector3.zero,new Vector3(.12f,2,.12f),"#c5bd92");
            }
            Block(route,"high-platform",new Vector3(13,3.05f,0),new Vector3(5.8f,.3f,1),"#939689");
            Block(route,"key-door",new Vector3(15.3f,4.1f,.15f),new Vector3(.3f,1.8f,1),"#9f9a84");
            hole=CreatePendant(route,"key-recess",new Vector3(14.7f,4,-.48f),1,"#494e47");
            key=CreatePendant(route,"album-key",new Vector3(12,5,-.5f),1,"#b6a16a");keyHome=key.localPosition;
            foreach(var r in route.GetComponentsInChildren<Renderer>())r.sharedMaterial.shader=Shader.Find("Unlit/Color");
        }
        public override void Activate()
        {
            base.Activate();originalMask=View.cullingMask;originalRect=View.rect;originalBackground=View.backgroundColor;originalClearFlags=View.clearFlags;View.clearFlags=CameraClearFlags.SolidColor;
            View.backgroundColor=new Color(.22f,.25f,.24f);
            BuildAlbum();fixedEye=View.transform.position;fixedRotation=View.transform.rotation;yaw=pitch=0;
            View.cullingMask=(1<<ObservationLayer)|(1<<PaperLayer);View.rect=new Rect(0,0,1,1);
            Defocus=View.GetComponent<PaintingDefocus>();if(Defocus==null)Defocus=View.gameObject.AddComponent<PaintingDefocus>();Defocus.Begin();
            albumCamera.enabled=true;edgeArmed=false;RefreshAlbum();
        }
        public override void CancelPointerGesture(){focusHeld=looking=false;if(keyDragging){keyDragging=false;key.localPosition=keyHome;}State.CancelInput();}
        public override void Deactivate()
        {
            CancelPointerGesture();if(albumCamera!=null)albumCamera.enabled=false;if(Defocus!=null)Defocus.End();
            if(View!=null){View.cullingMask=originalMask;View.rect=originalRect;View.backgroundColor=originalBackground;View.clearFlags=originalClearFlags;}
            base.Deactivate();
        }
        public override void Dispose(){if(Defocus!=null)Defocus.End();if(paperRetained && paper!=null)UnityEngine.Object.Destroy(paper.gameObject);foreach(var m in wardMaterials)if(m!=null)UnityEngine.Object.Destroy(m);if(albumTexture!=null){albumTexture.Release();UnityEngine.Object.Destroy(albumTexture);}if(pageMaterial!=null)UnityEngine.Object.Destroy(pageMaterial);base.Dispose();}
        bool PageRay(Ray ray,out Ray bookRay)
        {
            bookRay=ray;RaycastHit hit;
            if(pageCollider==null || !pageCollider.gameObject.activeInHierarchy || !pageCollider.Raycast(ray,out hit,10))return false;
            bookRay=albumCamera.ViewportPointToRay(hit.textureCoord);return true;
        }
        public override string Pick(Ray ray)
        {
            Ray bookRay;if(State.BookOpen && PageRay(ray,out bookRay))ray=bookRay;
            RaycastHit hit;if(!Physics.Raycast(ray,out hit,50,State.BookOpen?1<<RouteLayer:1<<ObservationLayer))return "";
            var hotspot=hit.collider.GetComponent<WorldHotspot>();return hotspot!=null?hotspot.Id:"";
        }
        public override void Press(string id,Ray ray,int button)
        {
            if(State.Phase>=2)return;
            if(button==1 && !State.BookOpen && !focusHeld){looking=true;lastLook=Input.mousePosition;return;}
            if(button!=0 || looking)return;
            if(!State.BookOpen){focusHeld=true;return;}
            Ray bookRay;
            if(id=="album-key" && State.AtDoor && State.Painted[4] && !State.KeyInserted && PageRay(ray,out bookRay))
            {keyDragging=true;keyPlane=new Plane(Vector3.forward,key.position);float d;keyOffset=keyPlane.Raycast(bookRay,out d)?key.position-bookRay.GetPoint(d):Vector3.zero;}
        }
        public override void Move(Ray ray)
        {
            pointer=View.WorldToViewportPoint(ray.GetPoint(2));
            if(looking){Vector2 now=Input.mousePosition,delta=now-lastLook;lastLook=now;yaw=Mathf.Clamp(yaw+delta.x*.12f,-65,65);pitch=Mathf.Clamp(pitch-delta.y*.12f,-40,40);}
            if(keyDragging){Ray bookRay;float d;if(PageRay(ray,out bookRay)&&keyPlane.Raycast(bookRay,out d))key.position=bookRay.GetPoint(d)+keyOffset;}
            GazeTarget=Pick(ray);
        }
        public override void Release(string id,Ray ray)
        {
            looking=focusHeld=false;
            if(!keyDragging)return;
            Ray bookRay;bool matched=false;
            if(PageRay(ray,out bookRay))
            {float d;var plane=new Plane(Vector3.forward,hole.position);matched=plane.Raycast(bookRay,out d)&&Vector3.Distance(bookRay.GetPoint(d),hole.position)<.55f;}
            keyDragging=false;if(matched && State.InsertPendant()){key.gameObject.SetActive(false);Host?.SaveStableProgress();}else key.localPosition=keyHome;
        }
        public override void Tick(float dt,WBInput input)
        {
            if(!built)return;View.transform.position=fixedEye;View.transform.rotation=fixedRotation*Quaternion.Euler(pitch,yaw,0);
            if(pointer.y>.16f && pointer.y<.84f)edgeArmed=true;
            if(State.Phase<2 && edgeArmed && !HasPointerGesture && (State.BookOpen?pointer.y>.94f:pointer.y<.06f))
            {State.SetBook(!State.BookOpen);edgeArmed=false;Host?.InputContext.Handoff();Host?.SyncInputContext();}
            string[] ids={"shape-swing","shape-bridge","shape-luggage","shape-flower","shape-pendant"};
            int id=Array.IndexOf(ids,GazeTarget);if(State.Observe(id,focusHeld,dt))Host?.SaveStableProgress();
            Eye=fixedEye;Look=Eye+View.transform.forward;
            State.NoseHeight=nose.position.y-ward.position.y;
            State.Tick(dt,input);if(!Active)return;
            Defocus.SetFocusHeld(State.BookOpen || focusHeld,dt);RefreshAlbum();
            if(State.Phase>=2)RefreshWard();
        }
        void RefreshAlbum()
        {
            bool second=State.Phase==1;
            foreach(string id in new[]{"ground-a","ground-mid","ground-end","swing-pivot"})Nodes[id].SetActive(State.Phase==0);
            if(second){Nodes["ground-a"].SetActive(true);Nodes["ground-a"].transform.localPosition=new Vector3(3,-.15f,0);Nodes["ground-a"].transform.localScale=new Vector3(6,.3f,1);}
            foreach(string id in new[]{"pressure-plate","pressure-gate","visible-linkage","high-platform","key-door"})Nodes[id].SetActive(second);
            Nodes["gate-walkway"].SetActive(second && State.PlatePressed);
            bridge.gameObject.SetActive(State.Phase==0 && State.Painted[1]);bridge.localScale=new Vector3(3*State.BridgeGrowth,.2f,1);
            swingSeat.gameObject.SetActive(State.Phase==0 && State.Painted[0]);swingRope.gameObject.SetActive(swingSeat.gameObject.activeSelf);
            var seat=State.SwingSeat;swingSeat.localPosition=new Vector3(seat.x,seat.y,0);
            swingRope.localPosition=(swingSeat.localPosition+new Vector3(4.5f,3.3f,0))*.5f;swingRope.localRotation=Quaternion.Euler(0,0,State.SwingAngle*Mathf.Rad2Deg);
            crate.gameObject.SetActive(second && State.Painted[2]);crate.localPosition=new Vector3(State.CrateX,.35f,0);
            gate.localPosition=new Vector3(6,State.PlatePressed?3.2f:1,0);linkage.localRotation=Quaternion.Euler(0,0,State.PlatePressed?8:0);
            flower.gameObject.SetActive(second && State.Painted[3]);flower.localRotation=Quaternion.Euler(0,0,State.FlowerAngle*Mathf.Rad2Deg);
            for(int i=0;i<6;i++)
            {
                var p=State.FlowerSeat(i);petals[i].gameObject.SetActive(second && State.Painted[3]);petals[i].localPosition=new Vector3(p.x,p.y,0);petals[i].localRotation=Quaternion.identity;
                var arm=flower.GetChild(i);arm.localScale=new Vector3(.12f,2,.12f)/.45f;float a=i*Mathf.PI/3;arm.localPosition=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)/.45f;arm.localRotation=Quaternion.Euler(0,0,i*60-90);
            }
            hole.gameObject.SetActive(second && !State.KeyInserted);Nodes["key-door"].SetActive(second && !State.KeyInserted);
            key.gameObject.SetActive(second && State.Painted[4] && !State.KeyInserted);if(!keyDragging)key.localPosition=keyHome;
            actor.localPosition=new Vector3(State.Actor.x,State.Actor.y+.3f,-.05f);actor.localRotation=Quaternion.identity;
            paper.gameObject.SetActive(State.BookOpen || State.Phase>=2);paper.SetPositionAndRotation(fixedEye+View.transform.rotation*new Vector3(0,-.14f,.85f),View.transform.rotation);
            observation.gameObject.SetActive(State.Phase<3);
        }
    }
}
