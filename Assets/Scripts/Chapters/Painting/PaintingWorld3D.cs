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
        public override bool HasPointerGesture=>focusHeld||keyDragging||DoorPerformanceActive;
        const int RouteLayer=27,ObservationLayer=28,PaperLayer=29;
        Transform observation,route,actor,key,hole,ward,paper;
        Camera albumCamera,pageCamera;RenderTexture albumTexture;Collider pageCollider;
        Vector3 fixedEye;Quaternion fixedRotation;float yaw,pitch;int originalMask;Rect originalRect;Color originalBackground;CameraClearFlags originalClearFlags;
        bool built,focusHeld,keyDragging,edgeArmed=true;
        Plane keyPlane;Vector3 keyHome,keyOffset;Vector2 pointer;

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
                BuildAlbumActor();
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

        void BuildRoute()
        {
            Block(route,"ground-a",new Vector3(1.5f,-.15f,0),new Vector3(3,.3f,1),"#939689");
            Block(route,"ground-mid",new Vector3(7.5f,.55f,0),new Vector3(3,.3f,1),"#939689");
            Block(route,"ground-end",new Vector3(14,.55f,0),new Vector3(4,.3f,1),"#939689");
            Block(route,"high-platform",new Vector3(13,3.05f,0),new Vector3(5.8f,.3f,1),"#939689");
            hole=new GameObject("key-recess").transform;hole.SetParent(route,false);
            key=new GameObject("album-key").transform;key.SetParent(route,false);
            key.localPosition=new Vector3(12,5,-.5f);keyHome=key.localPosition;
            var hit=key.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.55f,.65f,.16f);
            BindClue("album-key",hit);
            foreach(var r in route.GetComponentsInChildren<Renderer>())r.sharedMaterial.shader=Shader.Find("Unlit/Color");
            BuildAlbumActor();
        }
        public override void Activate()
        {
            base.Activate();originalMask=View.cullingMask;originalRect=View.rect;originalBackground=View.backgroundColor;originalClearFlags=View.clearFlags;View.clearFlags=CameraClearFlags.SolidColor;
            View.backgroundColor=new Color(.22f,.25f,.24f);
            BuildAlbum();RestoreWinterPerformance();RestoreDoorView();wardPoseCaptured=false;actTransition=0;SetObservationAct(State.Phase==0?0:1);
            View.cullingMask=1<<ObservationLayer;View.rect=new Rect(0,0,1,1);
            // Render the physical paper after the landscape has been defocused.
            // Both cameras have the same eye; picking still uses the original view.
            if(pageCamera==null)
            {
                var overlay=new GameObject("Album foreground camera");
                overlay.transform.SetParent(View.transform,false);
                pageCamera=overlay.AddComponent<Camera>();
            }
            pageCamera.CopyFrom(View);pageCamera.fieldOfView=Fov;
            pageCamera.clearFlags=CameraClearFlags.Depth;pageCamera.cullingMask=1<<PaperLayer;
            pageCamera.depth=View.depth+1;pageCamera.enabled=true;
            Defocus=View.GetComponent<PaintingDefocus>();if(Defocus==null)Defocus=View.gameObject.AddComponent<PaintingDefocus>();Defocus.Begin();
            albumCamera.enabled=true;edgeArmed=false;BeginFirstGap();RefreshAlbum();
            if(State.Phase>=2)RefreshWard();
        }
        public override void CancelPointerGesture(){StopWinterMotion();winterPressPending=false;focusHeld=false;ResetObservationPointer();if(keyDragging){keyDragging=false;key.localPosition=keyHome;}State.CancelInput();
#if UNITY_EDITOR
            editorWardWalk=false;
#endif
        }
        public override void Deactivate()
        {
            CancelPointerGesture();if(observedSeat!=null)observedSeat.SetSwingAngleRadians(0);
            EndWardImage();
            if(albumCamera!=null)albumCamera.enabled=false;
            if(wardDesk!=null)wardDesk.gameObject.SetActive(false);
            if(pageCamera!=null)pageCamera.enabled=false;if(Defocus!=null)Defocus.End();
            if(View!=null){View.cullingMask=originalMask;View.rect=originalRect;View.backgroundColor=originalBackground;View.clearFlags=originalClearFlags;}
            base.Deactivate();
        }
        public override void Dispose(){if(PagePendant)ReleaseObject(key.gameObject);StopWinterMotion();EndWardImage();if(wardImage!=null)ReleaseObject(wardImage);if(albumBacking!=null)ReleaseObject(albumBacking.gameObject);if(bridgeHover)ReleaseObject(bridgeHover.gameObject);if(pageCamera!=null)ReleaseObject(pageCamera.gameObject);if(Defocus!=null)Defocus.End();if(albumTexture!=null){albumTexture.Release();UnityEngine.Object.Destroy(albumTexture);}if(winterSheet!=null)winterSheet.SetPageTexture(null);base.Dispose();}
        bool PageRay(Ray ray,out Ray bookRay)
        {
            bookRay=ray;RaycastHit hit;
            if(pageCollider==null || !pageCollider.gameObject.activeInHierarchy || !pageCollider.Raycast(ray,out hit,10))return false;
            bookRay=albumCamera.ViewportPointToRay(hit.textureCoord);return true;
        }
        internal override Transform HoverVisual(Vector2 point,out Camera camera)
        {
            camera=View;
            if(State.Phase>=2 || actTransition>0 || DoorPerformanceActive)return null;
            string id=Pick(PointerRay(point));
            if(State.BookOpen)
            {
                if(keyDragging){camera=albumCamera;return PendantDropMatches(PointerRay(point))?layout.recessOutline:null;}
                camera=PagePendant?pageCamera:albumCamera;
                return id=="album-key" && State.AtDoor && State.Painted[4] && !State.KeyInserted && !keyDragging?key:null;
            }
            int clue=System.Array.IndexOf(new[]{"shape-swing","shape-bridge","shape-luggage","shape-flower","shape-pendant"},id);
            if(clue<0 || State.Painted[clue] || State.Phase==0 && clue>1)return null;
            switch(clue)
            {
                case 0:return layout.observedSwing;
                case 1:return layout.realtimeObservation?layout.observedBridge:bridgeHover;
                case 2:return layout.observedSuitcase;
                case 3:return layout.flowerFocus.transform.parent;
                case 4:return layout.observedPendant;
                default:return null;
            }
        }
        public override string Pick(Ray ray)
        {
            RaycastHit keyHit;
            if(State.BookOpen && PagePendant && key.gameObject.activeInHierarchy && !keyDragging && key.GetComponent<Collider>().Raycast(ray,out keyHit,10))return "album-key";
            Ray bookRay;if(PageRay(ray,out bookRay)){if(!State.BookOpen)return "";ray=bookRay;}
            RaycastHit hit;if(!Physics.Raycast(ray,out hit,50,State.BookOpen?1<<RouteLayer:1<<ObservationLayer))return "";
            var hotspot=hit.collider.GetComponent<WorldHotspot>();return hotspot!=null?hotspot.Id:"";
        }
        public override void Press(string id,Ray ray,int button)
        {
            if(State.Phase>=2 || actTransition>0 || DoorPerformanceActive)return;
            if(button!=0)return;
            if(!State.BookOpen){if(!focusHeld)WinterShot(37);focusHeld=true;return;}
            Ray bookRay;
            if(id=="album-key" && State.AtDoor && State.Painted[4] && !State.KeyInserted && PendantRay(ray,out bookRay))
            {if(!keyDragging)WinterShot(19);keyDragging=true;keyPlane=new Plane(PagePendant?paper.forward:albumCamera.transform.forward,key.position);float d;keyOffset=keyPlane.Raycast(bookRay,out d)?key.position-bookRay.GetPoint(d):Vector3.zero;}
        }
        public override void Move(Ray ray)
        {
            if(actTransition>0 || State.Phase>=2 || DoorPerformanceActive)return;
            pointer=View.WorldToViewportPoint(ray.GetPoint(2));
            MoveObservationPointer(pointer);
            if(keyDragging){Ray bookRay;float d;if(PendantRay(ray,out bookRay)&&keyPlane.Raycast(bookRay,out d))key.position=bookRay.GetPoint(d)+keyOffset;}
            GazeTarget=Pick(ray);
        }
        public override void Release(string id,Ray ray)
        {
            if(focusHeld && GazeTarget=="shape-swing" && State.Focus>0 && !State.Painted[0])shortPresses++;
            focusHeld=false;ResetObservationPointer();
            if(!keyDragging)return;
            bool matched=PendantDropMatches(ray);
            keyDragging=false;
            if(matched && State.InsertPendant())
            {
                key.gameObject.SetActive(false);
                BeginDoorPerformance();
                Host?.SaveStableProgress();
            }
            else key.localPosition=keyHome;
        }
        public override void Tick(float dt,WBInput input)
        {
            if(!built)return;
#if UNITY_EDITOR
            if(editorWardWalk)input=editorWardInput;
#endif
            if(actTransition>0){AdvanceActTransition(dt);return;}
            if(DoorPerformanceActive)
            {
                TickDoorPerformance(dt);RefreshAlbum();return;
            }
            TickObservationPointer(dt);
            View.transform.position=fixedEye;View.transform.rotation=fixedRotation*Quaternion.Euler(pitch,yaw,0);
            if(State.Phase<2)GazeTarget=Pick(View.ViewportPointToRay(pointer));
            if(pointer.y>.16f && pointer.y<.84f)edgeArmed=true;
            if(State.Phase<2 && edgeArmed && !HasPointerGesture && (State.BookOpen?pointer.y>.94f:pointer.y<.06f))
            {TurnPageForInput();edgeArmed=false;Host?.InputContext.Handoff();Host?.SyncInputContext();}
            string[] ids={"shape-swing","shape-bridge","shape-luggage","shape-flower","shape-pendant"};
            int id=Array.IndexOf(ids,GazeTarget);if(State.Observe(id,focusHeld,dt)){ShapeRecorded(id);Host?.SaveStableProgress();}
            TickFirstGap(dt);
            TickPendantHint(dt);
            Eye=fixedEye;Look=Eye+View.transform.forward;
            State.NoseHeight=layout.revealCutoff.position.y-layout.revealStart.position.y;
            var before=new WinterAudioFrame(State);
            if(!keyDragging)State.Tick(dt,input);
            SyncWinterActionSounds(before);
            TickAlbumActor(dt,before);
            if(!Active)return;
            if(before.Phase==0 && State.Phase==1)
            {
                CancelPointerGesture();actTransition=ActFadeDuration;Host?.SaveStableProgress();return;
            }
            float blurBefore=Defocus.Amount;
            Defocus.SetFocusHeld(State.BookOpen || focusHeld,dt);
            SyncWinterFocusSounds(id,blurBefore);
            TickDoorView(dt);RefreshAlbum();
            if(State.Phase>=2)RefreshWard();
        }
        void RefreshAlbum()
        {
            bool second=State.Phase==1;
            if(emptyHanger!=null)emptyHanger.gameObject.SetActive(State.Phase==0 && !State.Painted[0]);
            foreach(string id in new[]{"ground-a","ground-mid","ground-end"})Nodes[id].SetActive(State.Phase==0);
            if(second){Nodes["ground-a"].SetActive(true);Nodes["ground-a"].transform.localPosition=new Vector3(3,-.15f,0);Nodes["ground-a"].transform.localScale=new Vector3(6,.3f,1);}
            Nodes["high-platform"].SetActive(second);
            hole.gameObject.SetActive(second && !State.KeyInserted);
            key.gameObject.SetActive(second && State.Painted[4] && !State.KeyInserted);if(!keyDragging)key.localPosition=keyHome;
            // The page camera looks along +Z. Keep the act-two figure in front
            // of the door/frame (nearest authored face is at route Z -0.575).
            // Gameplay remains in State.Actor's XY plane.
            PlaceAlbumActor(second);
            paper.gameObject.SetActive(State.Phase==0 || State.BookOpen || State.Phase>=2);
            float y=State.Phase==0?sheetY:-.14f;
            paper.SetPositionAndRotation(fixedEye+View.transform.rotation*new Vector3(0,y,.85f),View.transform.rotation);
            observation.gameObject.SetActive(State.Phase<3);
            RefreshWinterVisuals();
        }
    }
}
