using UnityEngine;

namespace MemoryWhitebox
{
    [DefaultExecutionOrder(-200)]
    public sealed class SafeInspection : MonoBehaviour
    {
        public const int DisplayLayer=29;
        public Camera view,displayCamera;
        public Light inspectionLight;
        public AudioSource feedback;
        public SafeInspectionSubject[] subjects;
        [Header("统一检视：距离与缩放")]
        [Min(.1f)] public float defaultDistance=.85f;
        [Min(.1f)] public float minimumDistance=.55f;
        [Min(.1f)] public float maximumDistance=1.15f;
        [Min(.001f)] public float zoomStep=.055f;
        [Header("固定设备：靠近观察")]
        [Min(.1f)] public float fixedDeviceDistanceScale=1.05f;
        [Min(.1f)] public float fixedDeviceMinimumDistance=.7f;
        [Min(.1f)] public float fixedDeviceMaximumDistance=1.8f;
        [Header("统一检视：构图与动作")]
        [Min(.01f)] public float displaySize=.38f;
        [Range(20,100)] public float inspectionFieldOfView=55;
        public Vector2 displayOffset=new Vector2(0,-.02f);
        [Min(0)] public float rotationSensitivity=.32f;
        [Min(.01f)] public float approachDuration=.35f;
        [Min(.01f)] public float resetDuration=.22f;
        [Min(0)] public float openPartClearance=.65f;
        [Header("实际手持：镜头局部空间")]
        public Vector3 heldTapePosition=new Vector3(.09f,-.035f,.35f);
        public Vector3 heldTapeEulerAngles=Vector3.zero;
        [Min(.01f)] public float heldTapeMoveSpeed=1.6f;
        [Min(1)] public float heldTapeRotateSpeed=540;
        float NearDistance=>Mathf.Max(.1f,minimumDistance);
        float FarDistance=>Mathf.Max(NearDistance,maximumDistance);
        float DefaultDistance=>Mathf.Clamp(defaultDistance,NearDistance,FarDistance);
        public SafeInspectionSubject Current {get;private set;}
        public bool Active => Current!=null;
        public bool BlocksInput => Active || releaseGate;
        public float Distance {get;private set;}
        public SafeInspectionPart HeldPart {get;private set;}
        bool releaseGate,focused=true;
        readonly PlayerInputReleaseGate handoff=new PlayerInputReleaseGate();
        bool entryGate;
        int closeFrame;
        Vector3 eye,targetEye;
        Quaternion rotation,targetRotation,spin;
        float fov,elapsed;
        Rect viewport;
        int mask;
        CursorLockMode cursor;
        bool cursorVisible;
        enum Gesture { None, PendingPart, Part, Rotate, WaitRelease }
        Gesture gesture;
        bool leftHeld,rightHeld;
        SafeInspectionPart pendingPart;
        Vector2 pendingDelta;
        float pendingSeconds;
        const float ChordDelay=.08f;
        float resetSeconds=-1,resetDistance;
        Quaternion resetSpin;
        public SafeInspectionPart HoveredPart => hovered;
        public bool Resetting => resetSeconds>=0;
        public Rect ResetButtonRect => new Rect(20,Screen.height*.5f-20,148,40);
        SafeInspectionPart hovered;
        Font font;

        public SafeInspectionSubject ForSource(Transform source)
        {foreach(var s in subjects)if(s.source==source)return s;return null;}
        public SafeInspectionSubject ForEntry(StudyEntry entry)
        {foreach(var s in subjects)if(s.entry==entry && entry!=null)return s;return null;}
        public SafeInspectionSubject ForHit(Collider hit)
        {
            if(hit==null)return null;
            var entry=hit.GetComponentInParent<InspectionEntry>();
            return entry!=null?entry.subject:null;
        }
        public bool Open(SafeInspectionSubject subject)
        {
            if(!isActiveAndEnabled || BlocksInput || subject==null || view==null || displayCamera==null || !focused)return false;
            Current=subject;HeldPart=hovered=null;spin=Quaternion.identity;Distance=DefaultDistance;elapsed=0;resetSeconds=-1;
            eye=view.transform.position;rotation=view.transform.rotation;fov=view.fieldOfView;viewport=view.rect;mask=view.cullingMask;
            cursor=Cursor.lockState;cursorVisible=Cursor.visible;
            targetEye=eye;targetRotation=rotation;
            subject.RefreshFraming();
            if(subject.fixedDevice)
            {
                Vector3 center=subject.source.position+subject.source.rotation*subject.localCenter;
                Vector3 direction=(eye-center).normalized;
                float near=Mathf.Max(.1f,fixedDeviceMinimumDistance);
                targetEye=center+direction*Mathf.Clamp(subject.diameter*Mathf.Max(.1f,fixedDeviceDistanceScale),near,Mathf.Max(near,fixedDeviceMaximumDistance));
                targetRotation=Quaternion.LookRotation(center-targetEye);
            }
            subject.Begin();view.cullingMask&=~((1<<DisplayLayer)|(1<<FirstPersonHands.HandLayer));
            displayCamera.enabled=true;inspectionLight.enabled=true;
            PlayerInputPolicy.ApplyCursor(PlayerControlState.Inspection);entryGate=true;handoff.Arm();
            leftHeld=rightHeld=false;gesture=Gesture.None;pendingPart=null;pendingDelta=Vector2.zero;Advance(0);return true;
        }
        public void Close()
        {
            if(!Active)return;
            ReleasePart();SetHover(null);leftHeld=rightHeld=false;gesture=Gesture.None;pendingPart=null;resetSeconds=-1;Current.End();Current=null;
            if(view!=null){view.transform.SetPositionAndRotation(eye,rotation);view.fieldOfView=fov;view.rect=viewport;view.cullingMask=mask;}
            if(displayCamera!=null)displayCamera.enabled=false;
            if(inspectionLight!=null)inspectionLight.enabled=false;
            if(feedback!=null)feedback.Stop();
            PlayerInputPolicy.SetCursor(cursor!=CursorLockMode.Locked);Cursor.visible=cursorVisible;
            releaseGate=true;entryGate=false;handoff.Arm();closeFrame=Time.frameCount;
        }
        public void Rotate(Vector2 delta)
        {if(Active && HeldPart==null && !Resetting && !Current.fixedDevice)spin=Quaternion.AngleAxis(-delta.x*Mathf.Max(0,rotationSensitivity),Vector3.up)*Quaternion.AngleAxis(delta.y*Mathf.Max(0,rotationSensitivity),Vector3.right)*spin;}
        public void Flip(){if(Active && gesture==Gesture.None && !leftHeld && !rightHeld && !Resetting && !Current.fixedDevice)spin=Quaternion.AngleAxis(180,Vector3.up)*spin;}
        public void Zoom(float wheel){if(Active && gesture==Gesture.None && !leftHeld && !rightHeld && !Resetting)Distance=Mathf.Clamp(Distance-wheel*Mathf.Max(.001f,zoomStep),NearDistance,FarDistance);}
        // Capture() already stored each part's entry pose. Never re-read or reset the gameplay source here.
        public void ResetView()
        {
            if(!Active)return;
            ReleasePart();SetHover(null);pendingPart=null;pendingDelta=Vector2.zero;
            gesture=leftHeld || rightHeld?Gesture.WaitRelease:Gesture.None;
            resetSpin=spin;resetDistance=Distance;resetSeconds=0;
            foreach(var part in Current.parts)part.ResetToBaseline();
            if(feedback!=null)feedback.Stop();
        }
        public bool PressPart(Ray ray)
        {
            if(!Active || Resetting)return false;
            return HoldPart(Current.Pick(ray));
        }
        bool HoldPart(SafeInspectionPart part)
        {
            HeldPart=part;if(HeldPart==null)return false;
            HeldPart.Press();Sound(HeldPart);return true;
        }
        public void DragPart(Vector2 delta){if(HeldPart!=null && HeldPart.Drag(delta.x-delta.y))Sound(HeldPart);}
        public void ReleasePart(){if(HeldPart==null)return;var part=HeldPart;part.Release();HeldPart=null;Sound(part);}
        void Sound(SafeInspectionPart part){if(feedback!=null && part.contact!=null)feedback.PlayOneShot(part.contact,.35f);}
        public void SetFocus(bool focus){focused=focus;if(!focus)Close();}
        public void ReleaseInputGate(bool allReleased)
        {if(!Active && allReleased && Time.frameCount>closeFrame+1)releaseGate=false;}
        void Update()
        {
            if(!focused)return;
            if(!Active)
            {
                ReleaseInputGate(handoff.Ready);return;
            }
            if(entryGate){if(handoff.Ready)entryGate=false;return;}
            if(Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)){Close();return;}
            if(Input.GetKeyDown(KeyCode.F))Flip();
            if(Input.GetKeyDown(KeyCode.R))ResetView();
            UpdateHover(Input.mousePosition);
        }
        void SetHover(SafeInspectionPart part)
        {
            if(hovered==part)return;
            if(hovered!=null)hovered.SetHighlighted(false);
            hovered=part;if(hovered!=null)hovered.SetHighlighted(true);
        }
        public bool PointerOverUI(Vector2 point)
        {
            var gui=new Vector2(point.x,Screen.height-point.y);
            return gui.y<65 || gui.y>Screen.height-112 || ResetButtonRect.Contains(gui);
        }
        public void UpdateHover(Vector2 point)
        {
            if(!Active || Resetting || PointerOverUI(point) || gesture==Gesture.Rotate || gesture==Gesture.WaitRelease){SetHover(null);return;}
            SetHover(HeldPart!=null?HeldPart:Current.Pick(displayCamera.ScreenPointToRay(point)));
        }
        void CommitPart()
        {
            if(gesture!=Gesture.PendingPart)return;
            gesture=Gesture.Part;HoldPart(pendingPart);pendingPart=null;DragPart(pendingDelta);pendingDelta=Vector2.zero;
        }
        // Own one complete gesture. A chord can claim a pending press, never an already committed part drag.
        // Event positions also preserve fast down/drag/up sequences arriving within one frame.
        public void PointerEvent(EventType type,int button,Vector2 screenPoint,Vector2 delta,float wheel=0)
        {
            if(!Active)return;
            bool overUI=PointerOverUI(screenPoint);
            if(type==EventType.ScrollWheel)
            {
                if(overUI || Resetting)return;
                // A held dial owns the wheel; a pending chord buffers it without changing zoom or the source.
                if(gesture==Gesture.PendingPart && pendingPart!=null && pendingPart.motion==SafeInspectionPart.Motion.Dial && leftHeld && !rightHeld)pendingDelta.x+=wheel*18;
                else if(gesture==Gesture.Part && HeldPart!=null && HeldPart.motion==SafeInspectionPart.Motion.Dial && leftHeld && !rightHeld)DragPart(new Vector2(wheel*18,0));
                else Zoom(wheel);
                return;
            }
            if(button<0 || button>1)return;
            if(type==EventType.MouseDown)
            {
                if(button==0)leftHeld=true;else rightHeld=true;
                if(gesture==Gesture.WaitRelease)return;
                if(gesture==Gesture.Part || gesture==Gesture.Rotate)return;
                if(overUI || Resetting){gesture=Gesture.WaitRelease;pendingPart=null;SetHover(null);return;}
                if(leftHeld && rightHeld)
                {gesture=Gesture.Rotate;pendingPart=null;pendingDelta=Vector2.zero;SetHover(null);return;}
                if(button==0)
                {gesture=Gesture.PendingPart;pendingSeconds=0;pendingDelta=Vector2.zero;pendingPart=Current.Pick(displayCamera.ScreenPointToRay(screenPoint));}
            }
            else if(type==EventType.MouseDrag)
            {
                if(gesture==Gesture.Rotate && leftHeld && rightHeld)Rotate(delta);
                else if(gesture==Gesture.Part && leftHeld && button==0)DragPart(delta);
                else if(gesture==Gesture.PendingPart && leftHeld && button==0)pendingDelta+=delta;
            }
            else if(type==EventType.MouseUp)
            {
                if(button==0)
                {
                    if(gesture==Gesture.PendingPart)CommitPart();
                    ReleasePart();leftHeld=false;
                    gesture=rightHeld?Gesture.WaitRelease:Gesture.None;
                }
                else
                {
                    rightHeld=false;
                    if(gesture==Gesture.Rotate)gesture=leftHeld?Gesture.WaitRelease:Gesture.None;
                    else if(gesture==Gesture.WaitRelease && !leftHeld)gesture=Gesture.None;
                }
            }
        }
        void LateUpdate(){if(Active)Advance(Time.unscaledDeltaTime);}
        public void Advance(float dt)
        {
            if(!Active)return;
            dt=Mathf.Max(0,dt);
            if(gesture==Gesture.PendingPart){pendingSeconds+=dt;if(pendingSeconds>=ChordDelay)CommitPart();}
            if(Resetting)
            {
                resetSeconds+=dt;float resetT=Mathf.SmoothStep(0,1,Mathf.Clamp01(resetSeconds/Mathf.Max(.01f,resetDuration)));
                spin=Quaternion.Slerp(resetSpin,Quaternion.identity,resetT);Distance=Mathf.Lerp(resetDistance,DefaultDistance,resetT);
                if(resetSeconds>=Mathf.Max(.01f,resetDuration))resetSeconds=-1;
            }
            foreach(var part in Current.parts)part.Advance(dt);
            elapsed+=dt;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.01f,approachDuration)));
            // Save the entry FOV in Open and restore it in Close; station anchors stay untouched.
            view.fieldOfView=Mathf.Lerp(fov,Mathf.Clamp(inspectionFieldOfView,20,100),t);
            if(Current.fixedDevice)
            {
                Vector3 center=Current.source.position+Current.source.rotation*Current.localCenter;
                Vector3 zoomEye=center+(targetEye-center)*(Distance/DefaultDistance);
                view.transform.SetPositionAndRotation(Vector3.Lerp(eye,zoomEye,t),Quaternion.Slerp(rotation,targetRotation,t));
                Current.displayRoot.SetPositionAndRotation(Current.source.position,Current.source.rotation);Current.displayRoot.localScale=Vector3.one;
            }
            else
            {
                float scale=Mathf.Max(.01f,displaySize)/Mathf.Max(.01f,Current.diameter);
                var pose=rotation*spin*Current.InitialRotation();
                Current.displayRoot.localScale=Vector3.one*Mathf.Lerp(1,scale,t);
                float opened=0;foreach(var part in Current.parts)if(part.motion==SafeInspectionPart.Motion.Hinge)opened=Mathf.Max(opened,part.Value);
                // Give an open cover/page clearance as it swings toward the viewer.
                var target=eye+rotation*new Vector3(displayOffset.x,displayOffset.y,Distance+opened*Mathf.Max(0,openPartClearance))-pose*(Current.localCenter*scale);
                Current.displayRoot.SetPositionAndRotation(Vector3.Lerp(Current.source.position,target,t),Quaternion.Slerp(Current.source.rotation,pose,t));
            }
            displayCamera.transform.SetPositionAndRotation(view.transform.position,view.transform.rotation);
            displayCamera.fieldOfView=view.fieldOfView;displayCamera.rect=view.rect;displayCamera.aspect=view.aspect;
        }
        void OnApplicationFocus(bool focus){SetFocus(focus);}
        void OnDisable(){Close();releaseGate=false;}
        void OnDestroy(){if(font!=null)Destroy(font);}
        void OnGUI()
        {
            if(!Active || entryGate)return;
            var e=Event.current;
            if(!entryGate && (e.type==EventType.MouseDown || e.type==EventType.MouseUp || e.type==EventType.MouseDrag || e.type==EventType.ScrollWheel))
            {
                PointerEvent(e.type,e.button,new Vector2(e.mousePosition.x,Screen.height-e.mousePosition.y),new Vector2(e.delta.x,-e.delta.y),-e.delta.y/3f);
                if(!Active)return;
            }
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};
            var button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
            GUI.Box(new Rect(12,12,Mathf.Min(760,Screen.width-24),48),"");
            GUI.Label(new Rect(24,22,Screen.width-48,35),Current.title+" · 安全检视 · 放回后保持原状",style);
            float y=Screen.height-104;
            GUI.Box(new Rect(12,y,Screen.width-24,92),"");
            if(GUI.Button(ResetButtonRect,"重置检视 [R]",button))ResetView();
            if(GUI.Button(new Rect(Screen.width-160,y+10,132,30),"放回 [E / Esc]",button)){Close();return;}
            string hint=hovered!=null?hovered.label+"："+(hovered.motion==SafeInspectionPart.Motion.Button?"按住左键，松开回弹":hovered.motion==SafeInspectionPart.Motion.Dial?"左键拖动 / 按住左键滚轮转动":"左键拖动操作"):
                "指向部件查看操作提示";
            GUI.Label(new Rect(24,y+10,Mathf.Max(100,Screen.width-210),32),hint,style);
            string controls=Current.fixedDevice?"左键操作部件 · 滚轮远近":"左键操作部件 · 左右键同时按住拖动旋转 · F 翻面 · 滚轮远近";
            GUI.Label(new Rect(24,y+49,Screen.width-48,37),controls+" · E / Esc 放回",style);
        }
    }
}
