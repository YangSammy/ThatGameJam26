using UnityEngine;
namespace MemoryWhitebox
{
    // Presentation only. No Update, input, inventory ownership or save access.
    public sealed class PhotographyCameraVisual : MonoBehaviour
    {
        public Transform body,lens,back,lift,rewindKnob,takeUp,advanceLever,shutter,dial,focusRing,apertureRing;
        public Transform insert,eyepiece,grip,support;
        public Transform[] blades;
        public GameObject[] cartridges;
        public Transform LeaderTip {get;private set;}
        PhotographyVisualAssets assets;
        int lastIso=-1,lastFilmStage=-1;
        float lastFilmProgress=-1,door=1,transition=1;
        int targetStage=-1;
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
        public void Apply(bool open,bool hasFilm,int iso,int filmStage,float progress,float focus,float aperture,float shutterSeconds,float shutterPress,float dt,bool instant=false)
        {
            // Film stages: 0 ready, 1 pull, 2 seated, 3 advanced, 4 rewind, 5 retracted.
            if(assets==null)assets=PhotographyVisualAssets.Load();
            if(LeaderTip==null && insert!=null)LeaderTip=insert.Find("Leader interaction tip");
            if(targetStage!=filmStage){transition=targetStage<0 || instant?1:0;targetStage=filmStage;}
            transition=instant?1:Mathf.MoveTowards(transition,1,Mathf.Max(0,dt)*4);
            float sampleProgress=(filmStage==2 || filmStage==3)?transition:progress;
            door=instant?(open?1:0):Mathf.MoveTowards(door,open?1:0,Mathf.Max(0,dt)*3);
            back.localRotation=Quaternion.Euler(0,110*door,0);lift.localPosition=new Vector3(lift.localPosition.x,.071f+.004f*door,lift.localPosition.z);
            int index=iso==200?1:iso==400?2:iso==800?3:0;
            for(int i=0;i<4;i++)cartridges[i].SetActive(hasFilm && i==index);
            if(hasFilm && (lastIso!=iso || lastFilmStage!=filmStage || !Mathf.Approximately(lastFilmProgress,sampleProgress)))
            {
                SampleFilm(assets,cartridges[index],filmStage,sampleProgress);
                var skin=cartridges[index].GetComponentInChildren<SkinnedMeshRenderer>();
                if(tipIndex<0){var uv=skin.sharedMesh.uv;float score=float.MinValue;for(int j=0;j<uv.Length;j++){float v=uv[j].x*100-Mathf.Abs(uv[j].y-.5f);if(v>score){score=v;tipIndex=j;}}}
                if(tipMesh==null)tipMesh=new Mesh();skin.BakeMesh(tipMesh);tipMesh.GetVertices(tipVertices);
                LeaderTip.position=skin.transform.TransformPoint(tipVertices[tipIndex]);
                lastIso=iso;lastFilmStage=filmStage;lastFilmProgress=sampleProgress;
            }
            if(hasFilm)
            {
                var film=cartridges[index].transform;
                takeUp.localRotation=film.Find("Visual/TakeUpDrive").localRotation;
                rewindKnob.localRotation=film.Find("Visual/CartridgeAssembly/CartridgeSpool").localRotation;
            }
            else {lastIso=-1;takeUp.localRotation=rewindKnob.localRotation=Quaternion.identity;}
            focusRing.localRotation=Quaternion.Euler(0,0,160*Mathf.Clamp01(.45f/Mathf.Max(.45f,focus)));
            int a=0;for(int i=1;i<stops.Length;i++)if(Mathf.Abs(stops[i]-aperture)<Mathf.Abs(stops[a]-aperture))a=i;
            apertureRing.localRotation=Quaternion.Euler(0,0,a*15);foreach(var blade in blades)blade.localRotation=Quaternion.Euler(0,0,angles[a]);
            dial.localRotation=Quaternion.Euler(0,Mathf.Log(1/Mathf.Max(.001f,shutterSeconds),2)*30,0);
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
