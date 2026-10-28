using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        public enum PhotographyMode { Exploration, Meter, Body, Viewfinder, Aperture, Shutter }
        [Header("Photography V3 input and metering")]
        [Min(.1f)] public float edgeDwell=.25f;
        [Min(1)] public float virtualCursorSpeed=20;
        [Range(0,20)] public float sceneEv100=13;
        public PhotographyMode PhotoMode=>photoMode;
        public bool HandPresentationBlocked=>inputGate || !focused || changingScene || Paused;
        public Transform HandContactTarget=>dragPart!=null && instrumentParts.ContainsKey(dragPart)?instrumentParts[dragPart]:null;
        public int FilmIso=>roll!=null && roll.cameraLoading!=null?roll.cameraLoading.iso:100;
        public float Aperture=>apertures[apertureIndex];
        public float ShutterSeconds=>speeds[speedIndex];
        public float ExposureError=>Mathf.Log(Aperture*Aperture/ShutterSeconds,2)-Mathf.Log(FilmIso/100f,2)-sceneEv100;
        static readonly int[] filmIsos={100,200,400,800};
        static readonly float[] apertures={2.8f,4,5.6f,8,11,16};
        static readonly float[] speeds={1f/4,1f/8,1f/15,1f/30,1f/60,1f/125,1f/250,1f/500,1f/1000};
        int apertureIndex=3,speedIndex=5,meterIsoIndex,loadingStep;
        PhotographyMode photoMode;
        Vector2 virtualPointer=new Vector2(.5f,.5f);
        int edgeCandidate=-1;
        float dwell,cooldown;
        bool edgeArmed=true;
        string dragPart;
        Vector3 dragStart;
        Transform instruments,bodyRoot,meterRoot;
        readonly Dictionary<string,Transform> instrumentParts=new Dictionary<string,Transform>();
        readonly Dictionary<string,Vector3> partHomes=new Dictionary<string,Vector3>();
        readonly List<TextMesh> instrumentLabels=new List<TextMesh>();
        Material instrumentMaterial;
        TextMesh meterReadout,cameraReadout;

        void InitializePhotographyModes()
        {
            var saved=roll!=null?roll.cameraLoading:null;
            loadingStep=saved==null?0:saved.phase==PhotoChapter.FilmPhase.Ready?7:
                saved.mechanicalStep>0?saved.mechanicalStep:saved.phase==PhotoChapter.FilmPhase.Closed?6:saved.threaded?4:saved.phase==PhotoChapter.FilmPhase.LoadedOpen?2:0;
            meterIsoIndex=Mathf.Max(0,Array.IndexOf(filmIsos,FilmIso));
            if(roll!=null && roll.frames.Count>0)
            {
                var last=roll.frames[roll.frames.Count-1];
                int aperture=Array.IndexOf(apertures,last.aperture),speed=Array.IndexOf(speeds,last.shutterSeconds);
                if(aperture>=0)apertureIndex=aperture;if(speed>=0)speedIndex=speed;
            }
            instruments=new GameObject("Photography hand instruments").transform;instruments.SetParent(view.transform,false);
            bodyRoot=new GameObject("Camera operation model").transform;bodyRoot.SetParent(instruments,false);
            meterRoot=new GameObject("Incident light meter").transform;meterRoot.SetParent(instruments,false);
            instrumentMaterial=new Material(Shader.Find("Standard")){color=new Color(.27f,.29f,.25f)};
            Instrument("body",bodyRoot,new Vector3(0,-.05f,.65f),new Vector3(.36f,.22f,.075f));
            Instrument("back",bodyRoot,new Vector3(0,-.05f,.59f),new Vector3(.34f,.2f,.018f));
            Instrument("rewind",bodyRoot,new Vector3(-.13f,.095f,.6f),new Vector3(.045f,.04f,.04f));
            Instrument("chamber",bodyRoot,new Vector3(-.11f,-.05f,.595f),new Vector3(.07f,.14f,.025f));
            Instrument("spool",bodyRoot,new Vector3(.12f,-.05f,.59f),new Vector3(.04f,.13f,.035f));
            Instrument("leader",bodyRoot,new Vector3(-.06f,-.05f,.56f),new Vector3(.06f,.065f,.008f));
            Instrument("advance",bodyRoot,new Vector3(.14f,.095f,.59f),new Vector3(.09f,.02f,.035f));
            Instrument("eyepiece",bodyRoot,new Vector3(0,.085f,.59f),new Vector3(.055f,.035f,.025f));
            Instrument("aperture",bodyRoot,new Vector3(-.18f,-.06f,.56f),new Vector3(.04f,.09f,.045f));
            Instrument("speed",bodyRoot,new Vector3(.1f,.11f,.64f),new Vector3(.065f,.025f,.065f));
            for(int i=0;i<4;i++)
            {
                var film=Instrument("film-"+filmIsos[i],bodyRoot,new Vector3(-.18f+i*.12f,-.23f,.6f),new Vector3(.065f,.09f,.045f));
                Numeric(film,filmIsos[i].ToString(),new Vector3(0,0,-.55f),.18f);
            }
            var meter=Instrument("meter",meterRoot,new Vector3(0,-.02f,.55f),new Vector3(.24f,.32f,.05f));
            meterReadout=Numeric(meter,"",new Vector3(0,0,-.55f),.12f);
            cameraReadout=Numeric(bodyRoot,"",new Vector3(0,.16f,.58f),.012f);
            SyncInstruments();SetPhotographyMode(PhotographyMode.Exploration);
        }
        Transform Instrument(string id,Transform parent,Vector3 position,Vector3 scale)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=id;go.layer=FirstPersonHands.HandLayer;
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=instrumentMaterial;instrumentParts[id]=go.transform;partHomes[id]=position;return go.transform;
        }
        TextMesh Numeric(Transform parent,string text,Vector3 position,float size)
        {
            var go=new GameObject("Instrument scale");go.layer=FirstPersonHands.HandLayer;go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var label=go.AddComponent<TextMesh>();label.text=text;label.characterSize=size;label.fontSize=48;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.95f,.9f,.7f);instrumentLabels.Add(label);return label;
        }
        void SetPhotographyMode(PhotographyMode mode)
        {
            CancelInstrumentDrag();photoMode=mode;Aiming=mode==PhotographyMode.Viewfinder;
            if(mode!=PhotographyMode.Aperture && mode!=PhotographyMode.Shutter)virtualPointer=new Vector2(.5f,.5f);
            dwell=0;cooldown=.2f;edgeCandidate=-1;edgeArmed=false;
            moveVelocity=Vector3.zero;inputGate=true;handoff.Arm();
            if(instruments!=null)SyncInstruments();
        }
        void CancelInstrumentDrag()
        {
            if(dragPart!=null && instrumentParts.ContainsKey(dragPart))instrumentParts[dragPart].localPosition=partHomes[dragPart];
            dragPart=null;
        }
        bool TickPhotographyModes(float dt)
        {
            Vector2 motion=PlayerInputPolicy.LookDelta*virtualCursorSpeed;
            virtualPointer+=new Vector2(motion.x/Mathf.Max(1,Screen.width),motion.y/Mathf.Max(1,Screen.height));
            virtualPointer=new Vector2(Mathf.Clamp01(virtualPointer.x),Mathf.Clamp01(virtualPointer.y));
            cooldown=Mathf.Max(0,cooldown-dt);
            bool centered=virtualPointer.x>.25f && virtualPointer.x<.75f && virtualPointer.y>.25f && virtualPointer.y<.75f;
            if(centered && cooldown<=0)edgeArmed=true;
            int edge=-1;
            if(dragPart==null && !Input.GetMouseButton(0))
            {
                if(photoMode!=PhotographyMode.Exploration && virtualPointer.y>.94f)edge=0;
                else if(photoMode==PhotographyMode.Exploration && virtualPointer.y<.2f)edge=virtualPointer.x<.18f?1:virtualPointer.x>.82f?2:-1;
                else if(photoMode==PhotographyMode.Viewfinder)edge=virtualPointer.x<.08f?3:virtualPointer.x>.92f?4:-1;
            }
            if(edgeArmed && cooldown<=0 && edge>=0)
            {
                if(edgeCandidate!=edge){edgeCandidate=edge;dwell=0;}dwell+=dt;
                if(dwell>=edgeDwell)
                {
                    SetPhotographyMode(edge==0?PhotographyMode.Exploration:edge==1?PhotographyMode.Meter:edge==2?PhotographyMode.Body:edge==3?PhotographyMode.Aperture:PhotographyMode.Shutter);return true;
                }
            }
            else {edgeCandidate=-1;dwell=0;}
            if((photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter) && centered && cooldown<=0)
            {SetPhotographyMode(PhotographyMode.Viewfinder);return true;}
            float scroll=Input.mouseScrollDelta.y;
            if(Mathf.Abs(scroll)>.01f)
            {
                int direction=scroll>0?1:-1;
                if(photoMode==PhotographyMode.Meter)meterIsoIndex=Mathf.Clamp(meterIsoIndex+direction,0,3);
                if(photoMode==PhotographyMode.Aperture)apertureIndex=Mathf.Clamp(apertureIndex+direction,0,apertures.Length-1);
                if(photoMode==PhotographyMode.Shutter)speedIndex=Mathf.Clamp(speedIndex+direction,0,speeds.Length-1);
            }
            if(photoMode==PhotographyMode.Body)HandleBody();
            SyncInstruments();
            return photoMode!=PhotographyMode.Exploration && photoMode!=PhotographyMode.Viewfinder;
        }
        string PickInstrument(Ray ray)
        {
            string result="";float nearest=float.MaxValue;RaycastHit hit;
            foreach(var pair in instrumentParts)
                if(pair.Value.gameObject.activeInHierarchy && pair.Value.GetComponent<Collider>().Raycast(ray,out hit,2) && hit.distance<nearest)
                {nearest=hit.distance;result=pair.Key;}
            return result;
        }
        void HandleBody()
        {
            var ray=view.ViewportPointToRay(virtualPointer);string hit=PickInstrument(ray);
            if(Input.GetMouseButtonDown(0))
            {
                if((hit=="rewind" && loadingStep==0) || (hit.StartsWith("film-") && loadingStep==1) || (hit=="leader" && loadingStep==2))
                {dragPart=hit;dragStart=ray.GetPoint(.55f);}
                else if(hit=="advance" && (loadingStep==4 || loadingStep==6))SaveLoadingStep(loadingStep+1,FilmIso);
                else if(hit=="back" && loadingStep==5)SaveLoadingStep(6,FilmIso);
                else if(hit=="eyepiece" && roll!=null && roll.ReadyForPark){SetPhotographyMode(PhotographyMode.Viewfinder);return;}
            }
            if(dragPart!=null && Input.GetMouseButton(0))
            {
                if(dragPart=="rewind")
                {if(Vector3.Dot(ray.GetPoint(.55f)-dragStart,view.transform.up)>.025f){SaveLoadingStep(1,FilmIso);CancelInstrumentDrag();}}
                else instrumentParts[dragPart].position=ray.GetPoint(.54f);
            }
            if(Input.GetMouseButtonUp(0) && dragPart!=null)
            {
                string dragged=dragPart;
                // Ignore the held object when finding its drop destination.
                instrumentParts[dragged].GetComponent<Collider>().enabled=false;hit=PickInstrument(ray);instrumentParts[dragged].GetComponent<Collider>().enabled=true;
                if(dragged.StartsWith("film-") && hit=="chamber")SaveLoadingStep(2,int.Parse(dragged.Substring(5)));
                else if(dragged=="leader" && hit=="spool")SaveLoadingStep(4,FilmIso);
                CancelInstrumentDrag();
            }
        }
        void SaveLoadingStep(int step,int iso)
        {
            if(roll==null || roll.frames.Count>0)return;
            var progress=new ParkCameraLoading{mechanicalStep=step,iso=iso,threaded=step>=4,
                phase=step>=7?PhotoChapter.FilmPhase.Ready:step>=6?PhotoChapter.FilmPhase.Closed:step>=2?PhotoChapter.FilmPhase.LoadedOpen:PhotoChapter.FilmPhase.Empty};
            try{roll=ParkFilmStore.SaveCameraLoading(roll.id,progress);loadingStep=step;}
            catch(Exception e){Debug.LogWarning("Camera step not saved; previous state retained: "+e.Message);}
        }
        void SyncInstruments()
        {
            if(instruments==null)return;
            meterRoot.gameObject.SetActive(photoMode==PhotographyMode.Meter);
            bodyRoot.gameObject.SetActive(photoMode==PhotographyMode.Body || photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter);
            foreach(int iso in filmIsos)instrumentParts["film-"+iso].gameObject.SetActive(loadingStep<2);
            instrumentParts["back"].localPosition=partHomes["back"]+(loadingStep>0 && loadingStep<6?Vector3.left*.3f:Vector3.zero);
            instrumentParts["leader"].gameObject.SetActive(loadingStep>=2 && loadingStep<6);
            if(loadingStep>=4){instrumentParts["leader"].localPosition=new Vector3(.025f,-.05f,.56f);instrumentParts["leader"].localScale=new Vector3(.19f,.065f,.008f);}
            instrumentParts["speed"].localRotation=Quaternion.Euler(0,speedIndex*24,0);
            instrumentParts["aperture"].localRotation=Quaternion.Euler(0,0,apertureIndex*24);
            float suggested=64/Mathf.Pow(2,sceneEv100+Mathf.Log(filmIsos[meterIsoIndex]/100f,2));
            meterReadout.text=filmIsos[meterIsoIndex]+"\nf/8\n1/"+Mathf.RoundToInt(1/suggested);
            cameraReadout.text=photoMode==PhotographyMode.Aperture?"f/"+Aperture:photoMode==PhotographyMode.Shutter?"1/"+Mathf.RoundToInt(1/ShutterSeconds):FilmIso+"   "+ExposedFrames+"/"+(roll!=null?roll.Capacity:3);
        }
        void DrawPhotographyModes()
        {
            if(photoMode==PhotographyMode.Viewfinder)
                GUI.Label(new Rect(Screen.width*.5f-140,Screen.height-60,280,30),"f/"+Aperture+"   1/"+Mathf.RoundToInt(1/ShutterSeconds)+"   "+FilmIso+"   "+focusDistance.ToString("0.0")+"m");
            if(photoMode!=PhotographyMode.Exploration || edgeCandidate>=0)
                GUI.Label(new Rect(virtualPointer.x*Screen.width-5,(1-virtualPointer.y)*Screen.height-10,20,20),"·");
        }
        void DisposePhotographyModes(){if(instruments!=null)Destroy(instruments.gameObject);if(instrumentMaterial!=null)Destroy(instrumentMaterial);}
    }
}
