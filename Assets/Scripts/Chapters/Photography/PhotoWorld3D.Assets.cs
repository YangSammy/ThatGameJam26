using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        PhotographyCameraVisual cameraVisual;
        PhotographyCameraVisual entryVisual;
        PhotographyMeterDisplay meterVisual;
        GameObject[] extractedVisuals;
        float leaderPull,meterHeldProgress;
        ParkPhotoFocus houseFocus;
        GameObject Proxy(Transform parent,string id,Vector3 position,Vector3 scale)
        {
            var go=Child(parent,id,position,scale,"#303735");go.GetComponent<Renderer>().enabled=false;go.AddComponent<PhotographyInteractionHit>();return go;
        }
        internal override Transform HoverVisual(Vector2 point,out Camera camera)
        {
            camera=View;
            if(PointerOverUI || HasActiveGesture)return null;
            string id=Pick(PointerRay(point));
            if(id=="meter" && meterVisual)return meterVisual.transform;
            var part=cameraVisual?cameraVisual.HoverPart(id):null;
            if(part)return part;
            return Nodes.TryGetValue(id,out var node)?node.transform:null;
        }
        void BuildAssetCamera()
        {
            cameraBody=Group("Mechanical 35 mm camera",cameraDock);
            cameraVisual=PhotographyCameraVisual.Create(cameraBody,new Vector3(0,-.17f,-.075f),5,8);
            if(Host!=null && Host.houseShell!=null)entryVisual=Host.houseShell.GetComponentInChildren<PhotographyCameraVisual>(true);
            Proxy(cameraBody,"camera-body",Vector3.zero,new Vector3(.7f,.42f,.3f));
            Proxy(cameraBody,"finder",Vector3.zero,new Vector3(.09f,.07f,.055f));
            // Receivers retain their existing authored gameplay positions.
            Proxy(cameraBody,"chamber",new Vector3(-.23f,0,-.177f),new Vector3(.22f,.32f,.035f));
            Proxy(cameraBody,"spool",new Vector3(.255f,0,-.187f),new Vector3(.11f,.31f,.11f));
            leader=Proxy(cameraBody,"leader",Vector3.zero,new Vector3(.09f,.12f,.025f));
            loadedFilm=new GameObject("Loaded film state proxy");loadedFilm.transform.SetParent(cameraBody,false);
            threadedFilm=new GameObject("Threaded film state proxy");threadedFilm.transform.SetParent(cameraBody,false);
            cameraBack=new GameObject("Hinged camera back").transform;cameraBack.SetParent(cameraBody,false);
            Proxy(cameraBack,"back",new Vector3(.33f,0,0),new Vector3(.65f,.27f,.025f));
            lens=cameraVisual.lens;
            focalRing=Proxy(cameraBody,"focal-ring",Vector3.zero,new Vector3(.32f,.32f,.04f)).transform;
            apertureRing=Proxy(cameraBody,"aperture-ring",Vector3.zero,new Vector3(.32f,.32f,.04f)).transform;
            shutterDial=Proxy(cameraBody,"shutter-dial",Vector3.zero,new Vector3(.09f,.045f,.09f)).transform;
            Proxy(cameraBody,"shutter",Vector3.zero,new Vector3(.06f,.035f,.06f));
            Proxy(cameraBody,"rewind",Vector3.zero,new Vector3(.11f,.06f,.11f));
            advanceLever=Proxy(cameraBody,"advance",Vector3.zero,new Vector3(.15f,.04f,.07f)).transform;
            var assets=PhotographyVisualAssets.Load();
            for(int i=0;i<4;i++)
            {
                int iso=new[]{100,200,400,800}[i];string id="film-"+iso;
                var roll=Proxy(Root.transform,id,new Vector3(-1.01f+i*.25f,1.075f,-3.48f),new Vector3(.14f,.24f,.14f));
                AddCartridge(roll.transform,assets.Film(iso),5,0);rolls.Add(roll);Remember(id);
            }
            looseFilm=Proxy(Root.transform,"exposed-film",new Vector3(.56f,1.075f,-3.48f),new Vector3(.14f,.24f,.14f));Remember("exposed-film");
            extractedVisuals=new GameObject[4];for(int i=0;i<4;i++)extractedVisuals[i]=AddCartridge(looseFilm.transform,assets.films[i],5,5);
            filmStrip=Line("Leader drag state",new[]{cameraDock,cameraDock},"#82704b",.01f);filmStrip.enabled=false;filmStrip.gameObject.SetActive(false);
            focalNumber=Label("",Vector3.zero,.02f,"#ffffff");shutterNumber=Label("",Vector3.zero,.02f,"#ffffff");apertureNumber=Label("",Vector3.zero,.02f,"#ffffff");isoNumber=Label("",Vector3.zero,.02f,"#ffffff");
            focalNumber.target=focalRing;focalNumber.prefix="Focus: ";shutterNumber.target=shutterDial;apertureNumber.target=apertureRing;apertureNumber.prefix="Aperture: f/";shutterNumber.prefix="Shutter: 1/";isoNumber.target=Nodes["chamber"].transform;isoNumber.prefix="ISO ";
            Photo.Settings.Focal=50;
        }
        static GameObject AddCartridge(Transform proxy,GameObject prefab,float scale,int stage)
        {
            var go=Object.Instantiate(prefab,proxy);go.transform.localScale=new Vector3(scale/proxy.localScale.x,scale/proxy.localScale.y,scale/proxy.localScale.z);
            PhotographyCameraVisual.PrepareVisual(go,proxy.gameObject.layer);PhotographyCameraVisual.SampleFilm(PhotographyVisualAssets.Load(),go,stage,0);return go;
        }
        void BuildAssetMeter()
        {
            meterFace=Group("Physical light meter",new Vector3(-1,1.14f,-2.93f));
            Proxy(meterFace,"meter",Vector3.zero,new Vector3(.3f,.35f,.13f));
            meterVisual=PhotographyMeterDisplay.Create(meterFace,new Vector3(0,-.17f,0),3,8);meterVisual.transform.localRotation=Quaternion.Euler(0,180,0);
        }
        void SyncAssetCamera(bool open)
        {
            if(cameraVisual==null)return;
            // Reuse the scene camera's authored pose after returning from the park.
            // Only the transient operation rig moves; scene anchors and entry stay intact.
            if(DirectDarkroomAvailable && entryVisual!=null)
            {
                float scale=entryVisual.transform.lossyScale.x/cameraVisual.transform.localScale.x;
                cameraBody.localScale=Vector3.one*scale;
                cameraBody.rotation=entryVisual.transform.rotation*Quaternion.Inverse(cameraVisual.transform.localRotation);
                cameraBody.position=entryVisual.transform.position-cameraBody.TransformVector(cameraVisual.transform.localPosition);
                cameraBody.gameObject.SetActive(true);
                if(meterFace!=null)meterFace.gameObject.SetActive(false);
            }
            bool inCamera=Photo.Phase>=PhotoChapter.FilmPhase.LoadedOpen && Photo.Phase<=PhotoChapter.FilmPhase.Rewound && !(Photo.Phase==PhotoChapter.FilmPhase.Rewound && RewoundBackOpen);
            int stage=Photo.Phase==PhotoChapter.FilmPhase.Rewound?5:Photo.Phase==PhotoChapter.FilmPhase.Exposed?4:Photo.Phase>=PhotoChapter.FilmPhase.Ready?3:Photo.FilmThreaded?2:dragged=="leader"?1:0;
            cameraVisual.Apply(open,inCamera,Photo.Settings.ISO,stage,stage==4?RewindTurns:leaderPull,Photo.Settings.Focus,Photo.Settings.Aperture,Photo.Settings.Shutter,0,Time.deltaTime,!Active);
            if(entryVisual!=null)entryVisual.Apply(open,inCamera,Photo.Settings.ISO,stage==1?0:stage,stage==4?RewindTurns:0,Photo.Settings.Focus,Photo.Settings.Aperture,Photo.Settings.Shutter,0,0,true);
            cameraBack.SetPositionAndRotation(cameraVisual.back.position,cameraVisual.back.rotation);
            Nodes["finder"].transform.position=cameraVisual.eyepiece.position;
            Nodes["rewind"].transform.SetPositionAndRotation(cameraVisual.rewindKnob.position,cameraVisual.rewindKnob.rotation);
            Nodes["shutter"].transform.position=cameraVisual.shutter.position;
            shutterDial.position=cameraVisual.dial.position;advanceLever.SetPositionAndRotation(cameraVisual.advanceLever.position,cameraVisual.advanceLever.rotation);
            ((BoxCollider)advanceLever.GetComponent<Collider>()).size=new Vector3(1.25f,2f,1.5f);
            // Loading does not use exposure controls: do not let them steal the lever hit.
            foreach(var id in new[]{"shutter-dial","shutter","focal-ring","aperture-ring"})
                Nodes[id].GetComponent<Collider>().enabled=!parkLoading && !ParkMode;
            Nodes["advance"].GetComponent<Collider>().enabled=!ParkMode;
            Nodes["finder"].GetComponent<Collider>().enabled=!ParkMode;
            focalRing.SetPositionAndRotation(cameraVisual.focusRing.position,cameraBody.rotation);apertureRing.SetPositionAndRotation(cameraVisual.apertureRing.position,cameraBody.rotation);
            leader.transform.position=cameraVisual.LeaderTip.position;
            Nodes["camera-body"].GetComponent<Collider>().enabled=false;
            Nodes["chamber"].GetComponent<Collider>().enabled=Photo.Phase==PhotoChapter.FilmPhase.Empty;
            Nodes["spool"].GetComponent<Collider>().enabled=Photo.Phase==PhotoChapter.FilmPhase.LoadedOpen && !Photo.FilmThreaded;
            Nodes["back"].GetComponent<Collider>().enabled=!ParkMode && Photo.Phase==PhotoChapter.FilmPhase.LoadedOpen && Photo.FilmThreaded;
            Nodes["rewind"].GetComponent<Collider>().enabled=Photo.Phase==PhotoChapter.FilmPhase.Exposed ||
                (Photo.Phase==PhotoChapter.FilmPhase.Rewound && !RewoundBackOpen);
            if(looseFilm.activeSelf && dragged!="exposed-film" && !OperationArea.RestorePose(looseFilm.transform))
                looseFilm.transform.SetPositionAndRotation(cameraVisual.insert.position,cameraVisual.insert.rotation);
            if(looseFilm.activeSelf && dragged!="exposed-film")SetColliders(looseFilm,cameraVisual.BackOpen);
            for(int i=0;i<4;i++)extractedVisuals[i].SetActive(i==(Photo.Settings.ISO==200?1:Photo.Settings.ISO==400?2:Photo.Settings.ISO==800?3:0));
            focalNumber.text=Photo.Settings.Focus.ToString("0.00")+" m (50 mm)";
            meterHeldProgress=Mathf.MoveTowards(meterHeldProgress,Photo.MeterVisible?1:0,Time.deltaTime*4);
            if(meterVisual!=null)meterVisual.Apply(Photo.Settings.ISO,PhotoChapter.EV(Photo.Target),Active && Photo.MeterVisible,meterHeldProgress);
            if(houseFocus==null && View!=null){houseFocus=View.GetComponent<ParkPhotoFocus>();if(houseFocus==null){houseFocus=View.gameObject.AddComponent<ParkPhotoFocus>();houseFocus.enabled=false;houseFocus.template=PhotographyVisualAssets.Load().focusEffect;houseFocus.enabled=true;}}
            if(houseFocus!=null){houseFocus.activeFocus=Active && Photo.InViewfinder;houseFocus.distance=Photo.Settings.Focus;}
        }
        void PullAssetLeader(Vector3 point)
        {
            if(cameraVisual==null)return;
            var start=cameraVisual.insert.TransformPoint(new Vector3(.039f,.0015f,-.0134f));var end=Nodes["spool"].transform.position;
            leaderPull=Mathf.Clamp01(Vector3.Dot(point-start,end-start)/Mathf.Max(.001f,(end-start).sqrMagnitude));
            SyncAssetCamera(true);
        }
        Vector3 LeaderDragPoint(Ray ray)
        {
            if(cameraVisual==null)return ProcessingDragPoint(ray);
            var plane=new Plane(cameraBody.forward,cameraVisual.insert.position);float distance;
            return plane.Raycast(ray,out distance)?ray.GetPoint(distance):cameraVisual.LeaderTip.position;
        }
    }
}
