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
        public bool HandPresentationBlocked=>(inputGate && !raiseReleasePresentation) || !focused || changingScene || Paused;
        // The automatic full-roll panel may finish an already visible release gesture.
        // Manual pause, focus loss and scene changes still suspend/hide it normally.
        public bool PhotoHandReleaseAllowed=>CurrentPauseReason==PauseReason.FilmComplete && focused && !applicationPaused && !changingScene && !GamePauseMenu.BlocksGameplay;
        public Transform HandContactTarget=>dragPart!=null && instrumentParts.ContainsKey(dragPart)?instrumentParts[dragPart]:null;
        public Transform CameraRaiseGrip=>CameraRaising && cameraRaiseElapsed<CameraRaiseSeconds*.6f && instrumentCamera!=null?instrumentCamera.grip:null;
        public Transform CameraRaiseSupport=>CameraRaising && cameraRaiseElapsed<CameraRaiseSeconds*.6f && instrumentCamera!=null?instrumentCamera.support:null;
        public int FilmIso=>roll?.FilmIso??100;
        public float Aperture=>apertures[apertureIndex];
        public float ShutterSeconds=>speeds[speedIndex];
        public float ExposureError=>ParkCameraResponse.ExposureError(Aperture,ShutterSeconds,FilmIso,sceneEv100);
        static readonly int[] filmIsos={100,200,400,800};
        static readonly float[] apertures={2.8f,4,5.6f,8,11,16};
        static readonly float[] speeds=ParkCameraResponse.ShutterSpeeds;
        int apertureIndex=3,speedIndex=5,meterIsoIndex,loadingStep;
        PhotographyMode photoMode;
        bool CameraDetailMode=>photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter;
        bool PreciseInstrumentPointer=>photoMode==PhotographyMode.Body || photoMode==PhotographyMode.Viewfinder || CameraDetailMode;
        Vector2 virtualPointer=new Vector2(.5f,.5f);
        int edgeCandidate=-1;
        float dwell,cooldown;
        bool edgeArmed=true;
        string dragPart;
        readonly PointerGestureBuffer bodyPointer=new PointerGestureBuffer();
        Vector3 dragStart;
        Transform instruments,bodyRoot,meterRoot;
        readonly Dictionary<string,Transform> instrumentParts=new Dictionary<string,Transform>();
        readonly Dictionary<string,Vector3> partHomes=new Dictionary<string,Vector3>();
        readonly List<WorldTooltipLabel> instrumentLabels=new List<WorldTooltipLabel>();
        Material instrumentMaterial;
        WorldTooltipLabel meterReadout,cameraReadout;
        const float CameraRaiseSeconds=.5f;
        float cameraRaiseElapsed=-1,raiseStartFov;
        bool raiseReleasePresentation;
        Vector3 raiseHome,raiseGoal;
        bool CameraRaising=>cameraRaiseElapsed>=0;

        void BeginCameraRaise()
        {
            if(CameraRaising || photoMode!=PhotographyMode.Body || roll==null || !roll.ReadyForPark || bodyRoot==null)return;
            CancelInstrumentDrag();
            raiseHome=bodyRoot.localPosition;raiseStartFov=view.fieldOfView;
            var eye=instrumentParts["eyepiece"];
            // Keep the solid eyepiece and back door in front of the near plane.
            var destination=view.transform.TransformPoint(new Vector3(0,0,Mathf.Max(.28f,view.nearClipPlane+.20f)));
            raiseGoal=raiseHome+bodyRoot.parent.InverseTransformVector(destination-eye.position);
            cameraRaiseElapsed=0;moveVelocity=Vector3.zero;verticalSpeed=0;PlayPhotographyCue(21);
            dwell=0;edgeCandidate=-1;bodyPointer.Clear();
            SyncParkInput();
            InputContext.Handoff();
        }
        bool TickCameraRaise(float dt)
        {
            if(!inputGate)raiseReleasePresentation=false;
            if(!CameraRaising)return false;
            if(Input.GetKeyDown(KeyCode.E)){SetPhotographyMode(PhotographyMode.Exploration);return true;}
            if(Input.GetKeyDown(KeyCode.R)){SetPhotographyMode(PhotographyMode.Meter);return true;}
            cameraRaiseElapsed=Mathf.Min(CameraRaiseSeconds,cameraRaiseElapsed+Mathf.Max(0,dt));
            float u=cameraRaiseElapsed/CameraRaiseSeconds;
            float smooth=u*u*(3-2*u);
            bodyRoot.localPosition=Vector3.Lerp(raiseHome,raiseGoal,smooth)+Vector3.up*(Mathf.Sin(Mathf.PI*u)*.07f);
            view.fieldOfView=Mathf.Lerp(raiseStartFov,PlayerControlSettings.Current.parkAimingFov,smooth);
            SyncInstruments();
            // Transfer to the optical view while fully occluded, never through solid geometry.
            if(focusEffect!=null)
            {
                focusEffect.raiseOcclusion=u<.6f?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.6f,u)):
                    1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,u));
                if(u>=.6f)SyncRaiseOpticalPreview();
            }
            if(u>=.6f && focusEffect!=null)bodyRoot.gameObject.SetActive(false);
            if(u>=1)
            {
                cameraRaiseElapsed=-1;bodyRoot.localPosition=raiseHome;
                aimBlend=1;SetPhotographyMode(PhotographyMode.Viewfinder);raiseReleasePresentation=true;
            }
            return true;
        }
        void CancelCameraRaise()
        {
            raiseReleasePresentation=false;
            if(focusEffect!=null)focusEffect.raiseOcclusion=0;
            if(!CameraRaising)return;
            cameraRaiseElapsed=-1;
            if(bodyRoot!=null)bodyRoot.localPosition=raiseHome;
            if(view!=null)view.fieldOfView=raiseStartFov;
        }
        void SyncRaiseOpticalPreview()
        {
            focusEffect.activeFocus=true;focusEffect.distance=focusDistance;
            focusEffect.aperture=Aperture;focusEffect.exposureError=ExposureError;
        }

        void InitializePhotographyModes()
        {
            var saved=roll!=null?roll.cameraLoading:null;
            loadingStep=saved!=null?saved.StableStep:0;
            meterIsoIndex=Mathf.Max(0,Array.IndexOf(filmIsos,FilmIso));
            if(roll!=null && roll.frames.Count>0)
            {
                var last=roll.frames[roll.frames.Count-1];
                int aperture=Array.IndexOf(apertures,last.aperture),speed=Array.IndexOf(speeds,last.shutterSeconds);
                if(aperture>=0)apertureIndex=aperture;if(speed>=0)speedIndex=speed;
            }
            if(PhotographyVisualAssets.Load()!=null){BuildAssetInstruments();return;}
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
        WorldTooltipLabel Numeric(Transform parent,string text,Vector3 position,float size)
        {
            var go=new GameObject("Instrument scale");go.layer=FirstPersonHands.HandLayer;go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var label=go.AddComponent<WorldTooltipLabel>();label.text=text;label.target=parent;instrumentLabels.Add(label);return label;
        }
        void SetPhotographyMode(PhotographyMode mode)
        {
            var previousMode=photoMode;
            CancelCameraRaise();
            handJoinTransition=0;
            CancelInstrumentDrag();photoMode=mode;Aiming=mode==PhotographyMode.Viewfinder;
            PhotographyModeSfx(previousMode,mode);
            // Back and edge transitions can return before Update's normal render sync.
            if(focusEffect!=null)SyncCameraPreview();
            if(mode!=PhotographyMode.Aperture && mode!=PhotographyMode.Shutter)virtualPointer=new Vector2(.5f,.5f);
            dwell=0;cooldown=.2f;edgeCandidate=-1;edgeArmed=false;
            moveVelocity=Vector3.zero;inputGate=true;handoff.Arm();
            InputContext.Handoff();SyncParkInput();
            if(instruments!=null)SyncInstruments();
            if(mode==PhotographyMode.Exploration)ReleaseGuidedDeparture();
            SyncCompanionControl();
        }
        void CancelInstrumentDrag()
        {
            StopPhotographyLoops();
            bodyPointer.Clear();
            if(dragPart!=null && instrumentParts.ContainsKey(dragPart))instrumentParts[dragPart].localPosition=partHomes[dragPart];
            dragPart=null;
        }
        bool TickPhotographyModes(float dt)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {ToggleCamera();return true;}
            if(Input.GetKeyDown(KeyCode.R))
            {SetPhotographyMode(photoMode==PhotographyMode.Meter?PhotographyMode.Exploration:PhotographyMode.Meter);return true;}
            if(PreciseInstrumentPointer)
                virtualPointer=new Vector2(Input.mousePosition.x/Mathf.Max(1,Screen.width),Input.mousePosition.y/Mathf.Max(1,Screen.height));
            else
            {
                Vector2 motion=PlayerInputPolicy.LookDelta*virtualCursorSpeed;
                virtualPointer+=new Vector2(motion.x/Mathf.Max(1,Screen.width),motion.y/Mathf.Max(1,Screen.height));
            }
            // Preserve out-of-window coordinates for a free cursor. Clamping them
            // would turn a pointer outside Game view into a valid edge gesture.
            if(!PreciseInstrumentPointer)virtualPointer=new Vector2(Mathf.Clamp01(virtualPointer.x),Mathf.Clamp01(virtualPointer.y));
            bool returningPointer=Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButton(2);
            if(TickCameraPointerEdge(dt,dragPart!=null || Input.GetMouseButton(0) || returningPointer || handJoinTransition>0 || (PhotoHandPointerActive && PointerOnHand)))return true;
            float scroll=Input.mouseScrollDelta.y;
            if(Mathf.Abs(scroll)>.01f && !returningPointer && (!PreciseInstrumentPointer || PointerInsideViewport(virtualPointer)))
            {
                int direction=scroll>0?1:-1;
                if(StepInstrumentControl(direction))PlayInstrumentClick();
            }
            if(photoMode==PhotographyMode.Meter)TickTargetMeter();
            if(photoMode==PhotographyMode.Body)HandleBody();
            SyncInstruments();
            return photoMode==PhotographyMode.Body || photoMode==PhotographyMode.Meter;
        }
        bool TickCameraPointerEdge(float dt,bool gestureBusy)
        {
            if(!PointerInsideViewport(virtualPointer)){ResetCameraEdgeGesture();return false;}
            if(ReturnCameraDetailAtCentre(virtualPointer))return true;
            cooldown=Mathf.Max(0,cooldown-dt);
            bool centered=virtualPointer.x>.25f && virtualPointer.x<.75f && virtualPointer.y>.25f && virtualPointer.y<.75f;
            if(centered && cooldown<=0 && !gestureBusy)edgeArmed=true;
            int edge=-1;
            if(!gestureBusy)
            {
                // Only an explicit viewfinder pointer gesture opens a dial. Walking,
                // loading, metering and vertical edges cannot accidentally select it.
                if(photoMode==PhotographyMode.Viewfinder)
                    edge=virtualPointer.x<.06f?1:virtualPointer.x>.94f?2:-1;
            }
            if(edgeArmed && cooldown<=0 && edge>=0)
            {
                if(edgeCandidate!=edge){edgeCandidate=edge;dwell=0;}dwell+=dt;
                if(dwell>=edgeDwell)
                {
                    SetPhotographyMode(edge==1?PhotographyMode.Aperture:PhotographyMode.Shutter);return true;
                }
            }
            else {edgeCandidate=-1;dwell=0;}
            return false;
        }
        bool ReturnCameraDetailAtCentre(Vector2 pointer)
        {
            if(!CameraDetailMode || !PointerInsideViewport(pointer) || pointer.x<=.25f || pointer.x>=.75f || pointer.y<=.25f || pointer.y>=.75f)return false;
            // Navigation is immediate, even during a held click. SetPhotographyMode
            // arms the release gates, so that click cannot become a shutter/hand click.
            SetPhotographyMode(PhotographyMode.Viewfinder);return true;
        }
        void ResetCameraEdgeGesture(){dwell=0;cooldown=.2f;edgeCandidate=-1;edgeArmed=false;}
        internal static bool PointerInsideViewport(Vector2 pointer)=>pointer.x>=0 && pointer.x<=1 && pointer.y>=0 && pointer.y<=1;
        internal static bool FinderClickAllowed(Vector2 pointer,bool palmHit)=>PointerInsideViewport(pointer) &&
            (palmHit || (pointer.x>=.06f && pointer.x<=.94f));
        void StepFinderWheel(float scroll)
        {
            if(photoMode!=PhotographyMode.Viewfinder || Mathf.Abs(scroll)<=.01f || !FinderClickAllowed(virtualPointer,false))return;
            StepFocusControl(scroll);
        }
        void DrawFinderShortcuts()
        {
            if(photoMode!=PhotographyMode.Viewfinder || CameraRaising || changingScene)return;
            var saved=GUI.color;
            var style=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleLeft};
            float x=Screen.width-204,y=Screen.height-116;
            string[] labels={"Scroll  Focus","Left edge  Aperture","Right edge  Shutter"};
            for(int i=0;i<3;i++)
            {
                GUI.color=i==0?new Color(1,.78f,.35f,1):new Color(1,1,1,.8f);
                var c=new Vector2(x+10,y+12+i*29);
                IconCircle(c,8);
                if(i==0)IconCircle(c,3);
                else if(i==1)
                {
                    for(int n=0;n<6;n++)
                    {float a=n*Mathf.PI/3;GUI.DrawTexture(new Rect(c.x+Mathf.Cos(a)*4-1,c.y+Mathf.Sin(a)*4-1,2,2),Texture2D.whiteTexture);}
                }
                else {GUI.DrawTexture(new Rect(c.x-1,c.y-5,2,6),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(c.x,c.y-1,5,2),Texture2D.whiteTexture);}
                GUI.Label(new Rect(x+26,y+i*29,176,25),labels[i],style);
            }
            GUI.color=saved;
        }
        void ToggleCamera()
        {
            SetPhotographyMode(photoMode==PhotographyMode.Exploration?
                PhotographyMode.Body:PhotographyMode.Exploration);
        }
        public Vector2 TooltipPoint=>new Vector2(virtualPointer.x*Screen.width,virtualPointer.y*Screen.height);
        public Collider TooltipTarget
        {
            get
            {
                if(CameraDetailMode || (PreciseInstrumentPointer && !PointerInsideViewport(virtualPointer)))return null;
                var ray=view.ViewportPointToRay(virtualPointer);
                if(dragPart!=null)
                {
                    // Use the same receiver ray as release, ignoring the held cartridge/leader.
                    string receiver=dragPart.StartsWith("film-")?"chamber":dragPart=="leader"?"spool":"";
                    if(receiver!="" && instrumentParts.TryGetValue(receiver,out var target))
                    {
                        var col=target.GetComponent<Collider>();
                        return InstrumentDropHit(receiver,ray,dragPart=="leader")?col:null;
                    }
                    return null;
                }
                var id=PickInstrument(ray);
                return instrumentParts.ContainsKey(id)?instrumentParts[id].GetComponent<Collider>():null;
            }
        }
        string PickInstrument(Ray ray)
        {
            string result="";float nearest=float.MaxValue;RaycastHit hit;
            foreach(var pair in instrumentParts)
                if(pair.Value.gameObject.activeInHierarchy && pair.Value.GetComponent<Collider>().enabled && pair.Value.GetComponent<Collider>().Raycast(ray,out hit,2) && hit.distance<nearest)
                {nearest=hit.distance;result=pair.Key;}
            return result;
        }
        void HandleBody()
        {
            var ray=view.ViewportPointToRay(virtualPointer);
            Ray captured;
            if(bodyPointer.TryBegin(out captured))BeginBodyPointer(captured);
            if(dragPart!=null && Input.GetMouseButton(0))MoveBodyPointer(ray);
            if(bodyPointer.TryEnd(Input.GetMouseButton(0),out captured) && dragPart!=null)
            {MoveBodyPointer(captured);EndBodyPointer(captured);}
        }
        void CaptureBodyPointer(Event e)
        {
            if(photoMode!=PhotographyMode.Body || HandPresentationBlocked || !InputContext.Allows(InputContextId.ParkBody))
            {bodyPointer.Clear();return;}
            if(e==null || e.button!=0 || (e.type!=EventType.MouseDown && e.type!=EventType.MouseUp))return;
            // Preserve the press origin even if a whole drag occurs between Updates.
            bodyPointer.Capture(e.type==EventType.MouseDown,view.ScreenPointToRay(new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y)));
        }
        // The native input path and regression tests share the same ray/collider routing.
        void BeginBodyPointer(Ray ray)
        {
            string hit=PickInstrument(ray);
            if(instrumentCamera!=null && instrumentCamera.MechanicalTransition)return;
            if(dragPart==null)
            {
                if((hit=="rewind" && loadingStep==0) || (hit.StartsWith("film-") && loadingStep==1) || (hit=="leader" && loadingStep==2))
                {dragPart=hit;dragStart=ray.GetPoint(.55f);}
                else if(hit=="advance" && (loadingStep==4 || loadingStep==6))SaveLoadingStep(loadingStep+1,FilmIso);
                else if(hit=="back" && loadingStep==5)SaveLoadingStep(6,FilmIso);
                else if(hit=="eyepiece" && roll!=null && roll.ReadyForPark){BeginCameraRaise();return;}
            }
        }
        void MoveBodyPointer(Ray ray)
        {
            if(dragPart!=null)
            {
                if(dragPart=="rewind")
                {if(Vector3.Dot(ray.GetPoint(.55f)-dragStart,view.transform.up)>.025f){SaveLoadingStep(1,FilmIso);CancelInstrumentDrag();}}
                else
                {
                    // Use a view-aligned plane, so lateral cursor movement never changes drag depth.
                    var plane=new Plane(view.transform.forward,view.transform.position+view.transform.forward*.54f);
                    if(plane.Raycast(ray,out float distance))
                    {
                        var before=instrumentParts[dragPart].position;
                        var point=ray.GetPoint(distance);
                        if(dragPart.StartsWith("film-") && instrumentCamera!=null)point=instrumentCamera.ClearCarryPosition(view,instrumentParts[dragPart],ray,point);
                        instrumentParts[dragPart].position=point;
                        LeaderMovementSfx(before,instrumentParts[dragPart].position);
                    }
                }
            }
        }
        // Forgiving screen-space receiver; only the held leader gets this margin.
        bool InstrumentDropHit(string id,Ray ray,bool forgivingLeader)
        {
            Transform target;
            if(!instrumentParts.TryGetValue(id,out target) || !target.gameObject.activeInHierarchy)return false;
            var col=target.GetComponent<Collider>();
            if(col==null || !col.enabled)return false;
            RaycastHit hit;if(col.Raycast(ray,out hit,2))return true;
            if(!forgivingLeader)return false;
            var bounds=col.bounds;
            var screen=view.WorldToScreenPoint(bounds.center);
            if(screen.z<=0 || screen.z>2)return false;
            var lo=new Vector2(float.MaxValue,float.MaxValue);var hi=new Vector2(float.MinValue,float.MinValue);
            for(int i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=view.WorldToScreenPoint(corner);if(p.z<=0)return false;
                lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);
            }
            var plane=new Plane(view.transform.forward,bounds.center);float distance;
            if(!plane.Raycast(ray,out distance) || distance>2)return false;
            var pointer=view.WorldToScreenPoint(ray.GetPoint(distance));
            float margin=Mathf.Min(view.pixelWidth,view.pixelHeight)*.035f;
            return pointer.x>=lo.x-margin && pointer.x<=hi.x+margin && pointer.y>=lo.y-margin && pointer.y<=hi.y+margin;
        }
        void EndBodyPointer(Ray ray)
        {
            if(dragPart!=null)
            {
                string dragged=dragPart;
                // Ignore the held object when finding its drop destination.
                string destination=dragged.StartsWith("film-")?"chamber":dragged=="leader"?"spool":"";
                Physics.SyncTransforms();
                string hit=destination!="" && InstrumentDropHit(destination,ray,dragged=="leader")?destination:"";
                if(dragged.StartsWith("film-") && hit=="chamber")
                {
                    SaveLoadingStep(2,int.Parse(dragged.Substring(5)));
                    if(loadingStep==2 && instrumentCamera!=null)instrumentCamera.BeginInsertion(instrumentParts[dragged].GetChild(0));
                }
                else if(dragged=="leader" && hit=="spool")SaveLoadingStep(4,FilmIso);
                CancelInstrumentDrag();
            }
        }
        string LoadingNextAction()
        {
            switch(loadingStep)
            {
                case 0:return "Pull the rewind knob up to open the back.";
                case 1:return "Drag one film cartridge into the left chamber.";
                case 2:case 3:return "Pull the film leader onto the right take-up spool.";
                case 4:return "Click the advance lever to check film tension.";
                case 5:return "Close the camera back.";
                case 6:return "Click the advance lever to prepare the first frame.";
                default:return "Click the eyepiece to compose.";
            }
        }
        void SaveLoadingStep(int step,int iso)
        {
            if(roll==null || roll.frames.Count>0 || !ParkCameraLoading.CanAdvance(loadingStep,step))return;
            var progress=ParkCameraLoading.AtStep(step,iso);
            try{roll=ParkFilmStore.SaveCameraLoading(roll.id,progress);loadingStep=step;SyncMeterAfterLoadingStep(step,iso);PlayLoadingSfx(step);}
            catch(Exception e){Notify("Camera step not saved. Release and retry.",6);Debug.LogWarning("Camera step not saved; previous state retained: "+e.Message);}
        }
        void ShowInstrumentTask(string id)
        {
            var target=instrumentCamera!=null?instrumentCamera.HoverPart(id):null;
            if(target==null && instrumentParts.ContainsKey(id))target=instrumentParts[id];
            if(target && target.gameObject.activeInHierarchy)
                TaskOutlineCue.Show(target,view,target.GetComponentsInChildren<Renderer>(true));
        }
        void SyncInstruments()
        {
            if(photoMode==PhotographyMode.Body && !HandPresentationBlocked && instrumentCamera!=null)
            {
                if(loadingStep==1 && dragPart==null)
                {
                    foreach(int iso in filmIsos)ShowInstrumentTask("film-"+iso);
                }
                else
                {
                    string next=dragPart=="leader"?"spool":
                        dragPart!=null && dragPart.StartsWith("film-")?"chamber":
                        loadingStep==0?"rewind":loadingStep==2 || loadingStep==3?"leader":
                        loadingStep==4 || loadingStep==6?"advance":loadingStep==5?"back":"eyepiece";
                    ShowInstrumentTask(next);
                }
            }

            if(!HandPresentationBlocked)
            {
                Transform control=photoMode==PhotographyMode.Aperture && instrumentCamera!=null?instrumentCamera.apertureRing:
                    photoMode==PhotographyMode.Shutter && instrumentCamera!=null?instrumentCamera.dial:
                    photoMode==PhotographyMode.Meter && instrumentMeter!=null?instrumentMeter.transform:null;
                if(control && control.gameObject.activeInHierarchy)
                    TaskOutlineCue.Show(control,view,control.GetComponentsInChildren<Renderer>(true));
            }
            if(instruments==null)return;
            // Move the complete kit together so film drag and mechanical hit targets
            // stay aligned. Eyepiece raising owns this transform during its transition.
            if(bodyRoot!=null && !CameraRaising)
            {bodyRoot.localPosition=cameraOperationOffset+(CameraDetailMode?Vector3.right*20:Vector3.zero);bodyRoot.localRotation=Quaternion.identity;}
            if(instrumentCamera!=null){SyncAssetInstruments();RenderCameraDetail();return;}
            meterRoot.gameObject.SetActive(photoMode==PhotographyMode.Meter);
            bodyRoot.gameObject.SetActive(photoMode==PhotographyMode.Body || photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter);
            foreach(int iso in filmIsos)instrumentParts["film-"+iso].gameObject.SetActive(loadingStep<2);
            instrumentParts["back"].localPosition=partHomes["back"]+(loadingStep>0 && loadingStep<6?Vector3.left*.3f:Vector3.zero);
            instrumentParts["leader"].gameObject.SetActive(loadingStep>=2 && loadingStep<6);
            if(loadingStep>=4){instrumentParts["leader"].localPosition=new Vector3(.025f,-.05f,.56f);instrumentParts["leader"].localScale=new Vector3(.19f,.065f,.008f);}
            instrumentParts["speed"].localRotation=Quaternion.Euler(0,speedIndex*24,0);
            instrumentParts["aperture"].localRotation=Quaternion.Euler(0,0,apertureIndex*24);
            float suggested=ParkCameraResponse.SuggestedShutter(8,filmIsos[meterIsoIndex],MeterEv100);
            meterReadout.text=MeterNumbers;
            cameraReadout.text=photoMode==PhotographyMode.Aperture?"f/"+Aperture:photoMode==PhotographyMode.Shutter?"1/"+Mathf.RoundToInt(1/ShutterSeconds):FilmIso+"   "+ExposedFrames+"/"+(roll!=null?roll.Capacity:3);
            RenderCameraDetail();
        }
        // Lightweight IMGUI icons share the existing photography HUD lifecycle.
        void DrawInstrumentShortcuts()
        {
            if(changingScene || CameraRaising)return;
            var saved=GUI.color;
            var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=15};
            float y=Screen.height-72;
            for(int i=0;i<2;i++)
            {
                float x=26+i*62;
                bool selected=i==0?photoMode!=PhotographyMode.Exploration && photoMode!=PhotographyMode.Meter:photoMode==PhotographyMode.Meter;
                bool suggested=photoMode==PhotographyMode.Exploration && loadingStep<2 && (i==1?!introductoryMeterRead:introductoryMeterRead);
                GUI.color=selected || suggested?new Color(1,.78f,.35f,1):new Color(1,1,1,.8f);
                if(i==0)
                {
                    IconBox(new Rect(x,y+8,38,25));IconBox(new Rect(x+5,y+3,12,6));
                    IconCircle(new Vector2(x+22,y+20),8);
                }
                else
                {
                    IconBox(new Rect(x+9,y+8,22,28));IconCircle(new Vector2(x+20,y+6),5);
                    IconBox(new Rect(x+13,y+13,14,9));
                    GUI.DrawTexture(new Rect(x+19,y+14,2,7),Texture2D.whiteTexture);
                }
                GUI.Label(new Rect(x,y+38,40,22),i==0?"E":"R",style);
            }
            GUI.color=saved;
        }
        static void IconBox(Rect r)
        {
            GUI.DrawTexture(new Rect(r.x,r.y,r.width,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.yMax-2,r.width,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,r.y,2,r.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax-2,r.y,2,r.height),Texture2D.whiteTexture);
        }
        static void IconCircle(Vector2 centre,float radius)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16;
                GUI.DrawTexture(new Rect(centre.x+Mathf.Cos(a)*radius-1,centre.y+Mathf.Sin(a)*radius-1,2,2),Texture2D.whiteTexture);
            }
        }
        void DrawPhotographyModes()
        {
            DrawInstrumentShortcuts();DrawFinderShortcuts();
            DrawCameraDetail();
            if(photoMode==PhotographyMode.Aperture || photoMode==PhotographyMode.Shutter)
            {
                bool aperture=photoMode==PhotographyMode.Aperture;
                var detail=CameraDetailRect();
                int valueSize=CameraDetailValueSize;
                float panelWidth=Mathf.Min(Screen.width-24,560);
                float valueHeight=valueSize+8;
                float hintHeight=CameraDetailHintSize*2+6;
                var panel=new Rect((Screen.width-panelWidth)*.5f,detail.yMax+4,panelWidth,valueHeight+hintHeight);
                var label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=valueSize,fontStyle=FontStyle.Bold};
                GUI.Label(new Rect(panel.x,panel.y,panel.width,valueHeight),aperture?"Aperture  f/"+Aperture:"Shutter  1/"+Mathf.RoundToInt(1/ShutterSeconds)+" s",label);
                label.fontSize=CameraDetailHintSize;label.fontStyle=FontStyle.Normal;
                GUI.Label(new Rect(panel.x,panel.y+valueHeight,panel.width,hintHeight),"Scroll: change one stop\nCentre / RMB: viewfinder   Esc: pause",label);
            }
            if(photoMode==PhotographyMode.Meter)
            {
                // Readable instrument values at small Game-view resolutions; no task box.
                float seconds=ParkCameraResponse.SuggestedShutter(8,MeterIso,MeterEv100);
                var numbers=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=16,wordWrap=true};
                GUI.Label(new Rect(160,Screen.height-60,Mathf.Max(1,Screen.width-320),48),
                    MeterNumbers,numbers);
                IconCircle(new Vector2(Screen.width*.5f,Screen.height*.5f),10);
            }
            if(photoMode==PhotographyMode.Viewfinder)
                GUI.Label(new Rect(Screen.width*.5f-140,Screen.height-60,280,30),"f/"+Aperture+"   1/"+Mathf.RoundToInt(1/ShutterSeconds)+"   "+FilmIso+"   "+focusDistance.ToString("0.0")+"m");
            if(!PreciseInstrumentPointer && (photoMode!=PhotographyMode.Exploration || edgeCandidate>=0))
                GUI.Label(new Rect(virtualPointer.x*Screen.width-5,(1-virtualPointer.y)*Screen.height-10,20,20),"Â·");
        }
        void DisposePhotographyModes(){DisposeCameraDetail();DisposeInstrumentClick();if(instruments!=null)Destroy(instruments.gameObject);if(instrumentMaterial!=null)Destroy(instrumentMaterial);}
    }
}
