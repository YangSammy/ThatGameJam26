using System;
using System.Collections.Generic;
using MemoryWhitebox.Art;
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
        Transform observation,route,actor,key,hole,ward,paper;
        Camera albumCamera;RenderTexture albumTexture;Collider pageCollider;
        Vector3 fixedEye;Quaternion fixedRotation;float yaw,pitch;int originalMask;Rect originalRect;Color originalBackground;CameraClearFlags originalClearFlags;
        bool built,focusHeld,looking,keyDragging,edgeArmed=true;
        Plane keyPlane;Vector3 keyHome,keyOffset;Vector2 lastLook,pointer;

        public PaintingWorld3D(WBMain host,PaintingChapter model):base(host,model,3)
        {State=model;Eye=SharedRoom?new Vector3(-.3f,1.65f,.95f):World(new Vector3(0,1.65f,-3));Look=Eye+Vector3.forward*3;Fov=64;WantsMouseLook=false;}
        public void BindSharedScarf(EndingWorld3D ending){}
        static void Layer(Transform root,int layer){root.gameObject.layer=layer;foreach(Transform child in root)Layer(child,layer);}
        Transform Block(Transform parent,string id,Vector3 p,Vector3 size,string color,PrimitiveType type=PrimitiveType.Cube)
        {return Child(parent,id,p,size,color,type).transform;}
        void BuildAlbum()
        {
            if(built)return;
            layout=Host!=null?Host.WinterAlbum:null;
            if(layout==null && !State.Completed)throw new InvalidOperationException("Winter album workspace is missing from the active bedroom.");
            built=true;
            fixedEye=View.transform.position;fixedRotation=View.transform.rotation;
            observation=layout!=null?layout.observationCarrier:null;
            route=Group("Album route - physical geometry",new Vector3(0,-30,0));if(layout!=null)
            {
                route.SetPositionAndRotation(layout.routeOrigin.position,layout.routeOrigin.rotation);
                route.localScale=layout.routeOrigin.lossyScale;
                BuildRoute();
            }
            else
            {
                actor=Block(route,"album-person",Vector3.zero,new Vector3(.28f,.5f,.28f),"#4e665f",PrimitiveType.Capsule);
                actor.GetComponent<Renderer>().sharedMaterial.shader=Shader.Find("Unlit/Color");
            }
            Layer(route,RouteLayer);
            var camera=Group("Album side camera",Vector3.zero);albumCamera=camera.gameObject.AddComponent<Camera>();
            albumCamera.transform.position=route.TransformPoint(new Vector3(8,2,-16));albumCamera.transform.rotation=route.rotation;
            albumCamera.orthographic=true;albumCamera.orthographicSize=5.2f;albumCamera.aspect=1.8f;albumCamera.cullingMask=1<<RouteLayer;
            albumCamera.clearFlags=CameraClearFlags.SolidColor;albumCamera.backgroundColor=new Color(.89f,.87f,.8f);albumCamera.nearClipPlane=.05f;albumCamera.farClipPlane=40;
            albumTexture=new RenderTexture(1280,720,24);albumTexture.name="Physical album page";albumTexture.Create();albumCamera.targetTexture=albumTexture;
            if(layout!=null)
            {
                paper=layout.paperCarrier;
                pageCollider=layout.pageHit;
                Nodes["album-page"]=pageCollider.gameObject;
                winterSheet=layout.sheet;
                BindWinterVisuals();
            }
            if(winterSheet!=null)winterSheet.SetPageTexture(albumTexture);
            BindWard();
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
            actor=Block(route,"album-person",new Vector3(.7f,.25f,0),new Vector3(.28f,.5f,.28f),"#4e665f",PrimitiveType.Capsule);
            Block(route,"high-platform",new Vector3(13,3.05f,0),new Vector3(5.8f,.3f,1),"#939689");
            hole=CreatePendant(route,"key-recess",new Vector3(14.7f,4,-.48f),1,"#494e47");
            key=CreatePendant(route,"album-key",new Vector3(12,5,-.5f),1,"#b6a16a");keyHome=key.localPosition;
            foreach(var r in route.GetComponentsInChildren<Renderer>())r.sharedMaterial.shader=Shader.Find("Unlit/Color");
        }
        public override void Activate()
        {
            base.Activate();originalMask=View.cullingMask;originalRect=View.rect;originalBackground=View.backgroundColor;originalClearFlags=View.clearFlags;View.clearFlags=CameraClearFlags.SolidColor;
            View.backgroundColor=new Color(.22f,.25f,.24f);
            BuildAlbum();actTransition=0;SetObservationAct(State.Phase==0?0:1);
            View.cullingMask=(1<<ObservationLayer)|(1<<PaperLayer);View.rect=new Rect(0,0,1,1);
            Defocus=View.GetComponent<PaintingDefocus>();if(Defocus==null)Defocus=View.gameObject.AddComponent<PaintingDefocus>();Defocus.Begin();
            albumCamera.enabled=true;edgeArmed=false;RefreshAlbum();
            if(State.Phase>=2)RefreshWard();
        }
        public override void CancelPointerGesture(){winterPressPending=false;focusHeld=looking=false;if(keyDragging){keyDragging=false;key.localPosition=keyHome;}State.CancelInput();}
        public override void Deactivate()
        {
            CancelPointerGesture();if(albumCamera!=null)albumCamera.enabled=false;if(Defocus!=null)Defocus.End();
            if(View!=null){View.cullingMask=originalMask;View.rect=originalRect;View.backgroundColor=originalBackground;View.clearFlags=originalClearFlags;}
            base.Deactivate();
        }
        public override void Dispose(){if(Defocus!=null)Defocus.End();if(albumTexture!=null){albumTexture.Release();UnityEngine.Object.Destroy(albumTexture);}if(winterSheet!=null)winterSheet.SetPageTexture(null);base.Dispose();}
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
            if(State.Phase>=2 || actTransition>0)return;
            if(button==1 && !State.BookOpen && !focusHeld){looking=true;lastLook=Input.mousePosition;return;}
            if(button!=0 || looking)return;
            if(!State.BookOpen){focusHeld=true;return;}
            Ray bookRay;
            if(id=="album-key" && State.AtDoor && State.Painted[4] && !State.KeyInserted && PageRay(ray,out bookRay))
            {keyDragging=true;keyPlane=new Plane(Vector3.forward,key.position);float d;keyOffset=keyPlane.Raycast(bookRay,out d)?key.position-bookRay.GetPoint(d):Vector3.zero;}
        }
        public override void Move(Ray ray)
        {
            if(actTransition>0)return;
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
            if(!built)return;
            if(actTransition>0){AdvanceActTransition(dt);return;}
            View.transform.position=fixedEye;View.transform.rotation=fixedRotation*Quaternion.Euler(pitch,yaw,0);
            if(pointer.y>.16f && pointer.y<.84f)edgeArmed=true;
            if(State.Phase<2 && edgeArmed && !HasPointerGesture && (State.BookOpen?pointer.y>.94f:pointer.y<.06f))
            {State.SetBook(!State.BookOpen);edgeArmed=false;Host?.InputContext.Handoff();Host?.SyncInputContext();}
            string[] ids={"shape-swing","shape-bridge","shape-luggage","shape-flower","shape-pendant"};
            int id=Array.IndexOf(ids,GazeTarget);if(State.Observe(id,focusHeld,dt))Host?.SaveStableProgress();
            Eye=fixedEye;Look=Eye+View.transform.forward;
            State.NoseHeight=layout.revealCutoff.position.y-layout.revealStart.position.y;
            int phase=State.Phase;
            if(!keyDragging)State.Tick(dt,input);
            if(!Active)return;
            if(phase==0 && State.Phase==1)
            {
                CancelPointerGesture();actTransition=ActFadeDuration;Host?.SaveStableProgress();return;
            }
            Defocus.SetFocusHeld(State.BookOpen || focusHeld,dt);RefreshAlbum();
            if(State.Phase>=2)RefreshWard();
        }
        void RefreshAlbum()
        {
            bool second=State.Phase==1;
            foreach(string id in new[]{"ground-a","ground-mid","ground-end"})Nodes[id].SetActive(State.Phase==0);
            if(second){Nodes["ground-a"].SetActive(true);Nodes["ground-a"].transform.localPosition=new Vector3(3,-.15f,0);Nodes["ground-a"].transform.localScale=new Vector3(6,.3f,1);}
            Nodes["high-platform"].SetActive(second);
            hole.gameObject.SetActive(second && !State.KeyInserted);
            key.gameObject.SetActive(second && State.Painted[4] && !State.KeyInserted);if(!keyDragging)key.localPosition=keyHome;
            actor.localPosition=new Vector3(State.Actor.x,State.Actor.y+.3f,-.05f);actor.localRotation=Quaternion.identity;
            paper.gameObject.SetActive(State.BookOpen || State.Phase>=2);paper.SetPositionAndRotation(fixedEye+View.transform.rotation*new Vector3(0,-.14f,.85f),View.transform.rotation);
            observation.gameObject.SetActive(State.Phase<3);
            RefreshWinterVisuals();
        }
    }
}
