using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation only. No Update, input, inventory ownership or save access.
    public sealed class PhotographyCameraVisual : MonoBehaviour
    {
        public Transform body,lens,back,lift,rewindKnob,takeUp,advanceLever,shutter,dial,focusRing,apertureRing;
        public Transform insert,eyepiece,grip,support;
        Transform eyepieceOutline,chamberOutline;
        public Transform[] blades;
        public GameObject[] cartridges;
        public Transform LeaderTip {get;private set;}
        PhotographyVisualAssets assets;
        int lastIso=-1,lastFilmStage=-1;
        float lastFilmProgress=-1,door=1,transition=1;
        int targetStage=-1;
        float returningLeader;
        float seatingPullStart=1;
        float displayedAperture=-1,displayedShutter;
        public bool BackOpen=>door>=.98f;
        public bool MechanicalTransition=>Inserting || transition<1 || returningLeader>0;
        void Awake(){assets=PhotographyVisualAssets.Load();if(insert!=null)LeaderTip=insert.Find("Leader interaction tip");}
        Mesh tipMesh;
        readonly System.Collections.Generic.List<Vector3> tipVertices=new System.Collections.Generic.List<Vector3>();
        int tipIndex=-1;
        static readonly float[] stops={2,2.8f,4,5.6f,8,11,16};
        static readonly float[] angles={0,-15.41476f,-25.23975f,-31.36030f,-35.80569f,-38.588f,-40.88564f};
        public static PhotographyCameraVisual Create(Transform parent,Vector3 localPosition,float scale,int layer)
        {
            var go=new GameObject("Photography Visual Adapter");go.transform.SetParent(parent,false);
            go.transform.localPosition=localPosition;go.transform.localScale=Vector3.one*scale;
            var v=go.AddComponent<PhotographyCameraVisual>();v.Build(layer);return v;
        }
        void Build(int layer)
        {
            assets=PhotographyVisualAssets.Load();
            if(assets==null)throw new System.InvalidOperationException("PhotographyVisualAssets is missing");
            body=Instantiate(assets.cameraBody,transform).transform;
            back=body.Find("Visual/BackDoor");lift=body.Find("Visual/RewindLift");rewindKnob=lift.Find("RewindKnob");
            takeUp=body.Find("Visual/TakeUpSpool");advanceLever=body.Find("Visual/AdvanceLever");shutter=body.Find("Visual/ShutterButton");dial=body.Find("Visual/ShutterDial");
            insert=body.Find("Visual/InsertAnchor");eyepiece=body.Find("Visual/EyepieceAnchor");grip=body.Find("Visual/GripAnchor");support=body.Find("Visual/SupportAnchor");
            lens=Instantiate(assets.lens,body.Find("Visual/LensMountAnchor")).transform;
            focusRing=lens.Find("Visual/FocusRing");apertureRing=lens.Find("Visual/ApertureRing");
            blades=new Transform[7];for(int i=0;i<7;i++)blades[i]=lens.Find("Visual/Iris/Blade_"+(i+1).ToString("00"));
            cartridges=new GameObject[4];for(int i=0;i<4;i++){cartridges[i]=Instantiate(assets.films[i],insert);cartridges[i].SetActive(false);}
            LeaderTip=new GameObject("Leader interaction tip").transform;LeaderTip.SetParent(insert,false);
            PrepareVisual(gameObject,layer);
        }
        float insertTime=1;
        Vector3 insertStart;
        Quaternion insertRotation;
        public bool Inserting=>insertTime<1;
        public void BeginInsertion(Transform carried)
        {
            insertStart=insert.InverseTransformPoint(carried.position);
            insertRotation=Quaternion.Inverse(insert.rotation)*carried.rotation;
            insertTime=0;
        }
        // Keep the complete carried mesh on the viewer's side of the body and open door.
        public Vector3 ClearCarryPosition(Camera camera,Transform item,Ray ray,Vector3 desired)
        {
            var forward=camera.transform.forward;var eye=camera.transform.position;
            float nearest=float.PositiveInfinity,extent=0;
            foreach(var r in body.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled || r.forceRenderingOff || r.transform.IsChildOf(insert))continue;
                var b=r.bounds;var e=b.extents;
                float radius=Mathf.Abs(forward.x)*e.x+Mathf.Abs(forward.y)*e.y+Mathf.Abs(forward.z)*e.z;
                nearest=Mathf.Min(nearest,Vector3.Dot(b.center-eye,forward)-radius);
            }
            foreach(var r in item.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled || r.forceRenderingOff)continue;
                var b=r.bounds;var e=b.extents;
                extent=Mathf.Max(extent,Vector3.Dot(b.center-item.position,forward)+Mathf.Abs(forward.x)*e.x+Mathf.Abs(forward.y)*e.y+Mathf.Abs(forward.z)*e.z);
            }
            float depth=Mathf.Min(Vector3.Dot(desired-eye,forward),nearest-extent-.008f);
            var plane=new Plane(forward,eye+forward*depth);float distance;
            return plane.Raycast(ray,out distance)?ray.GetPoint(distance):desired;
        }
        void ApplyInsertion(GameObject film,float dt,bool instant)
        {
            if(instant)insertTime=1;
            else insertTime=Mathf.MoveTowards(insertTime,1,Mathf.Max(0,dt)/.55f);
            var t=film.transform;
            // First align behind the open chamber, then approach along its insertion axis.
            var approach=new Vector3(0,0,Mathf.Min(-.075f,insertStart.z));
            float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(insertTime/.6f));
            t.localPosition=insertTime<.6f?Vector3.Lerp(insertStart,approach,u):Vector3.Lerp(approach,Vector3.zero,Mathf.SmoothStep(0,1,(insertTime-.6f)/.4f));
            t.localRotation=Quaternion.Slerp(insertRotation,Quaternion.identity,u);
            if(insertTime>=1){t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;}
        }
        public static void PrepareVisual(GameObject go,int layer)
        {
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
            foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var a in go.GetComponentsInChildren<Animator>(true))a.enabled=false;
        }
        internal Transform HoverPart(string id)
        {
            switch(id)
            {
                case "eyepiece":
                    return OutlineRegion(ref eyepieceOutline,eyepiece,"Eyepiece hover outline",new Vector3(.022f,.016f,.004f));
                case "chamber":
                    return OutlineRegion(ref chamberOutline,insert,"Film chamber hover outline",new Vector3(.023f,.044f,.012f));
                case "rewind": return rewindKnob;
                case "back": return back;
                case "spool": return takeUp;
                case "advance": return advanceLever;
                case "aperture": return apertureRing;
                case "shutter-dial":
                case "speed": return dial;
                case "shutter": return shutter;
                case "focal-ring": return focusRing;
                case "aperture-ring": return apertureRing;
                case "leader":
                    foreach(var cartridge in cartridges)
                        if(cartridge && cartridge.activeSelf)return cartridge.GetComponentInChildren<SkinnedMeshRenderer>()?.transform;
                    break;
            }
            return null;
        }
        Transform OutlineRegion(ref Transform cached,Transform anchor,string name,Vector3 size)
        {
            if(cached!=null)return cached;
            // These details are combined into Body_Mesh; outline their local region,
            // never the whole body. No visible mesh, collider or gameplay identity.
            var region=GameObject.CreatePrimitive(PrimitiveType.Cube);region.name=name;
            cached=region.transform;cached.SetParent(anchor,false);cached.localScale=size;
            region.layer=anchor.gameObject.layer;
            var collider=region.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
            region.GetComponent<Renderer>().enabled=false;region.AddComponent<HoverOutlineProxy>();
            return cached;
        }
        public void Apply(bool open,bool hasFilm,int iso,int filmStage,float progress,float focus,float aperture,float shutterSeconds,float shutterPress,float dt,bool instant=false,bool smoothDials=false)
        {
            // Film stages: 0 ready, 1 pull, 2 seated, 3 advanced, 4 rewind, 5 retracted.
            if(assets==null)assets=PhotographyVisualAssets.Load();
            if(LeaderTip==null && insert!=null)LeaderTip=insert.Find("Leader interaction tip");
            if(targetStage==1 && filmStage==0 && !instant)returningLeader=Mathf.Max(0,lastFilmProgress);
            if(targetStage!=filmStage)
            {
                if(filmStage==2)seatingPullStart=targetStage==1?Mathf.Clamp01(lastFilmProgress):1;
                transition=targetStage<0 || instant?1:0;targetStage=filmStage;
            }
            transition=instant?1:Mathf.MoveTowards(transition,1,Mathf.Max(0,dt)*4);
            float sampleProgress=(filmStage==2 || filmStage==3)?transition:progress;
            int sampledStage=filmStage;
            if(filmStage==2 && seatingPullStart<.999f && transition<1)
            {
                // Complete the remaining pull before wrapping the take-up spool.
                // Forgiving drop acceptance must not teleport an unextended leader.
                sampledStage=transition<.5f?1:2;
                sampleProgress=transition<.5f?Mathf.Lerp(seatingPullStart,1,Mathf.SmoothStep(0,1,transition*2)):Mathf.SmoothStep(0,1,(transition-.5f)*2);
            }
            if(instant || filmStage!=0)returningLeader=0;
            else if(returningLeader>0){returningLeader=Mathf.MoveTowards(returningLeader,0,Mathf.Max(0,dt)*4);sampledStage=1;sampleProgress=returningLeader;}
            door=instant?(open?1:0):Mathf.MoveTowards(door,open?1:0,Mathf.Max(0,dt)*3);
            back.localRotation=Quaternion.Euler(0,110*door,0);lift.localPosition=new Vector3(lift.localPosition.x,.071f+.004f*door,lift.localPosition.z);
            int index=iso==200?1:iso==400?2:iso==800?3:0;
            for(int i=0;i<4;i++)cartridges[i].SetActive(hasFilm && i==index);
            if(hasFilm && (lastIso!=iso || lastFilmStage!=sampledStage || !Mathf.Approximately(lastFilmProgress,sampleProgress)))
            {
                SampleFilm(assets,cartridges[index],sampledStage,sampleProgress);
                var skin=cartridges[index].GetComponentInChildren<SkinnedMeshRenderer>();
                if(tipIndex<0){var uv=skin.sharedMesh.uv;float score=float.MinValue;for(int j=0;j<uv.Length;j++){float v=uv[j].x*100-Mathf.Abs(uv[j].y-.5f);if(v>score){score=v;tipIndex=j;}}}
                if(tipMesh==null)tipMesh=new Mesh();skin.BakeMesh(tipMesh);tipMesh.GetVertices(tipVertices);
                LeaderTip.position=skin.transform.TransformPoint(tipVertices[tipIndex]);
                lastIso=iso;lastFilmStage=sampledStage;lastFilmProgress=sampleProgress;
            }
            if(hasFilm)
            {
                ApplyInsertion(cartridges[index],dt,instant || filmStage!=0);
                var film=cartridges[index].transform;
                takeUp.localRotation=film.Find("Visual/TakeUpDrive").localRotation;
                rewindKnob.localRotation=film.Find("Visual/CartridgeAssembly/CartridgeSpool").localRotation;
            }
            else {insertTime=1;lastIso=-1;takeUp.localRotation=rewindKnob.localRotation=Quaternion.identity;}
            focusRing.localRotation=Quaternion.Euler(0,0,160*Mathf.Clamp01(.45f/Mathf.Max(.45f,focus)));
            int a=0;for(int i=1;i<stops.Length;i++)if(Mathf.Abs(stops[i]-aperture)<Mathf.Abs(stops[a]-aperture))a=i;
            float speedAngle=Mathf.Log(1/Mathf.Max(.001f,shutterSeconds),2)*30;
            if(instant || !smoothDials || displayedAperture<0)
            {displayedAperture=a;displayedShutter=speedAngle;}
            else
            {
                // Only the mechanical presentation interpolates; exposure and saves use the selected detent immediately.
                displayedAperture=Mathf.MoveTowards(displayedAperture,a,Mathf.Max(0,dt)*6);
                displayedShutter=Mathf.MoveTowards(displayedShutter,speedAngle,Mathf.Max(0,dt)*180);
            }
            int low=Mathf.Clamp(Mathf.FloorToInt(displayedAperture),0,angles.Length-1),high=Mathf.Min(low+1,angles.Length-1);
            float bladeAngle=Mathf.Lerp(angles[low],angles[high],displayedAperture-low);
            apertureRing.localRotation=Quaternion.Euler(0,0,displayedAperture*15);foreach(var blade in blades)blade.localRotation=Quaternion.Euler(0,0,bladeAngle);
            dial.localRotation=Quaternion.Euler(0,displayedShutter,0);
            shutter.localPosition=new Vector3(shutter.localPosition.x,.073f-.0012f*Mathf.Clamp01(shutterPress),shutter.localPosition.z);
            advanceLever.localRotation=Quaternion.Euler(0,filmStage==3?-65*Mathf.Sin(transition*Mathf.PI):0,0);
        }
        public static void SampleFilm(PhotographyVisualAssets a,GameObject film,int stage,float u)
        {
            // Each sample begins with a complete stable pose, preventing stale blendshape channels.
            a.leaderReady.SamplePropAnimation(film,0);
            if(stage==1)a.pullLeader.SamplePropAnimation(film,Mathf.Clamp01(u));
            else if(stage==2){a.pullLeader.SamplePropAnimation(film,1);a.seatLeader.SamplePropAnimation(film,Mathf.Clamp01(u));}
            else if(stage==3){if(u>=1)a.advanced.SamplePropAnimation(film,0);else{a.pullLeader.SamplePropAnimation(film,1);a.seatLeader.SamplePropAnimation(film,1);a.advance.SamplePropAnimation(film,Mathf.Clamp01(u));}}
            else if(stage==4){a.advanced.SamplePropAnimation(film,0);a.rewind.SamplePropAnimation(film,Mathf.Clamp01(u));}
            else if(stage==5)a.retracted.SamplePropAnimation(film,0);
        }
        void OnDestroy(){if(tipMesh!=null){if(Application.isPlaying)Destroy(tipMesh);else DestroyImmediate(tipMesh);}}
    }
}
