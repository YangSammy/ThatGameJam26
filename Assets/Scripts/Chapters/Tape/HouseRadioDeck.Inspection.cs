using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        public RadioTapeChapter Chapter {get;private set;}
        public bool StationActive {get;private set;}
        public bool WaitingForRelease=>waitingForRelease;
        public int HeldSpindle=>heldSpindle;
        public string Notice {get;private set;}="";
        public Light stationLight;
        // Retained only to migrate existing scene/prefab values without losing artist edits.
        [SerializeField,HideInInspector] Vector3 heldTapePosition=new Vector3(.09f,-.035f,.35f);
        [SerializeField,HideInInspector] Vector3 heldTapeEulerAngles=Vector3.zero;
        [SerializeField,HideInInspector] float heldTapeMoveSpeed=1.6f;
        [SerializeField,HideInInspector] float heldTapeRotateSpeed=540;
        [SerializeField,HideInInspector] bool inspectionPoseMigrated;
        [HideInInspector] public SafeInspection inspectionSettings;
        public bool MigrateInspectionSettings()
        {
            if(inspectionPoseMigrated && inspectionSettings!=null)return false;
            if(inspectionSettings==null && house!=null)inspectionSettings=house.safeInspection;
            if(inspectionSettings==null && view!=null)inspectionSettings=view.GetComponent<SafeInspection>();
            if(inspectionSettings==null)return false;
            if(!inspectionPoseMigrated)
            {
                inspectionSettings.heldTapePosition=heldTapePosition;
                inspectionSettings.heldTapeEulerAngles=heldTapeEulerAngles;
                inspectionSettings.heldTapeMoveSpeed=heldTapeMoveSpeed;
                inspectionSettings.heldTapeRotateSpeed=heldTapeRotateSpeed;
                inspectionPoseMigrated=true;
            }
            return true;
        }
        public bool DraggingTape=>draggingTape;
        bool draggingTape,dropReady;
        Ray dragRay;
        bool waitingForRelease,leftWasDown,rightWasDown,focused=true;
        float statusHeight=60;
        int heldSpindle=-1;
        readonly float[] visualTapePositions=new float[3];
        public bool IsEntryHit(Collider hit)
        {
            if(hit==null)return false;if(hit==radioHit || hit==slotHit)return true;
            foreach(var t in tapes)if(hit==t.hit || hit==t.leftSpindleHit || hit==t.rightSpindleHit)return true;
            return false;
        }
        public Transform InspectionSource(Ray ray)
        {
            var hit=Pick(ray);if(hit==radioHit || hit==slotHit)return transform;
            foreach(var t in tapes)if(hit==t.hit || hit==t.leftSpindleHit || hit==t.rightSpindleHit)return t.root;
            return null;
        }
        public bool TryInspect(Ray ray)
        {
            if(house==null || house.safeInspection==null || waitingForRelease || draggingTape || leftWasDown || rightWasDown || heldSpindle>=0 || State==Phase.Loading || State==Phase.Playing)return false;
            var source=InspectionSource(ray);if(source==null && State==Phase.Holding)source=tapes[Selected].root;
            if(source==null)return false;
            house.safeInspection.feedback.mute=house.Muted;
            return house.safeInspection.Open(house.safeInspection.ForSource(source));
        }
        public void SetStationActive(bool active)
        {
            if(!initialized)return;
            CancelTapeDrag();ReleaseSpindle();StationActive=active;waitingForRelease=active;leftWasDown=rightWasDown=false;
            if(stationLight!=null)stationLight.enabled=active;
            if(active && house!=null)speaker.mute=house.Muted;
            if(!active)
            {
                if(State==Phase.Holding || State==Phase.Returning)ResetDeck();
                if(State==Phase.Playing)TogglePlayback();
                animator.speed=0;
            }
        }
        public void ReleaseInputGate(bool released)
        {if(waitingForRelease && released && focused && StationActive && (house==null || (!house.ShowHelp && !house.safeInspection.BlocksInput))){waitingForRelease=false;leftWasDown=rightWasDown=false;if(State==Phase.Loading)animator.speed=1;}}
        public void SuspendInput()
        {
            CancelTapeDrag();ReleaseSpindle();waitingForRelease=true;leftWasDown=rightWasDown=false;
            if(State==Phase.Playing)TogglePlayback();animator.speed=0;
        }
        void OnApplicationFocus(bool value){focused=value;if(!value && initialized)SuspendInput();}
        public void ReleaseSpindle(){heldSpindle=-1;Chapter?.Tape.EndSpindle();}
        void SetHeldHits(bool enabled)
        {
            if(Selected<0)return;var t=tapes[Selected];t.hit.enabled=enabled;
            if(t.leftSpindleHit!=null)t.leftSpindleHit.enabled=enabled;
            if(t.rightSpindleHit!=null)t.rightSpindleHit.enabled=enabled;
        }
        void BeginTapeDrag(Ray ray)
        {
            if(State!=Phase.Holding)return;
            ReleaseSpindle();draggingTape=true;dropReady=false;dragRay=ray;
            // The held cassette must not intercept the cursor ray aimed at the slot.
            SetHeldHits(false);
        }
        void CancelTapeDrag(){if(draggingTape)SetHeldHits(true);draggingTape=dropReady=false;}
        void EndTapeDrag(bool overSlot)
        {
            CancelTapeDrag();
            if(overSlot){InsertHeld();return;}
            Notice="磁带留在手中：可检查轮轴，或按住外壳重新拖向卡槽；右键放回。";
        }
        public void PoseHeldTape(float dt)
        {
            if(Selected<0 || State!=Phase.Holding || view==null || inspectionSettings==null)return;
            var t=tapes[Selected];
            var offset=inspectionSettings.heldTapePosition;offset.z=Mathf.Max(view.nearClipPlane+.05f,offset.z);
            var target=view.transform.TransformPoint(offset);
            if(draggingTape)
            {
                // Keep dragging on a camera-parallel plane. Slot hover only signals
                // acceptance; the authored load animation owns depth after release.
                var plane=new Plane(view.transform.forward,view.transform.position+view.transform.forward*offset.z);
                float distance;
                if(plane.Raycast(dragRay,out distance))target=dragRay.GetPoint(distance);
            }
            var rotation=view.transform.rotation*Quaternion.Euler(inspectionSettings.heldTapeEulerAngles);
            t.root.position=Vector3.MoveTowards(t.root.position,target,Mathf.Max(0,dt)*Mathf.Max(.01f,inspectionSettings.heldTapeMoveSpeed));
            t.root.rotation=Quaternion.RotateTowards(t.root.rotation,rotation,Mathf.Max(0,dt)*Mathf.Max(1,inspectionSettings.heldTapeRotateSpeed));
            t.root.localScale=Vector3.one;
        }
        public void TickStation(float dt,Ray ray,bool left,bool right,float wheel,bool inspect,bool allReleased)
        {
            if(!StationActive || !focused)return;
            if(house!=null && (house.ShowHelp || house.safeInspection.BlocksInput))return;
            if(waitingForRelease){ReleaseInputGate(allReleased);return;}
            var point=view.WorldToScreenPoint(ray.GetPoint(1));
            if(PointerOverUI(point))
            {
                if(draggingTape){dragRay=ray;dropReady=false;if(!left)EndTapeDrag(false);}
                if(!left)ReleaseSpindle();leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);return;
            }
            if(inspect && !left && !right && TryInspect(ray))return;
            var hit=Pick(ray);
            if(draggingTape)
            {
                dragRay=ray;dropReady=hit==slotHit && !Chapter.Tape.Candidates[Selected].Broken;
                if(right && !rightWasDown){CancelTapeDrag();PutBack();waitingForRelease=true;}
                else if(!left)EndTapeDrag(hit==slotHit);
                leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);return;
            }
            if(left && !leftWasDown)
            {
                if(State==Phase.Holding)
                {
                    var t=tapes[Selected];
                    if(hit==t.leftSpindleHit || hit==t.rightSpindleHit)heldSpindle=hit==t.leftSpindleHit?0:1;
                    else if(hit==t.hit)BeginTapeDrag(ray);
                }
                else if(State!=Phase.Loading && State!=Phase.Returning)
                {
                    if(hit==radioHit || hit==slotHit)TogglePlayback();
                    for(int i=0;i<tapes.Length;i++)if(hit==tapes[i].hit || hit==tapes[i].leftSpindleHit || hit==tapes[i].rightSpindleHit)
                    {if(i==Selected){if(State==Phase.Playing)TogglePlayback();else{TakeOut();BeginTapeDrag(ray);}}else if(State==Phase.Empty && PickUp(i))BeginTapeDrag(ray);break;}
                }
            }
            if(right && !rightWasDown){if(State==Phase.Holding)PutBack();else ResetDeck();waitingForRelease=true;}
            if(!left)ReleaseSpindle();
            if(left && heldSpindle>=0 && State==Phase.Holding && Mathf.Abs(wheel)>.001f)
            {
                var tape=Chapter.Tape;tape.BeginSpindle(heldSpindle);tape.Turn(wheel);
                if(tape.Candidates[Selected].Broken){Notice="发现原有断头：轮轴已停止。右键放回，再拿另一盒。";ReleaseSpindle();}
                else if(tape.SourceVerified)Notice="整段磁带已检查完整，可以装入收音机播放。";
            }
            leftWasDown=left;rightWasDown=right;AdvanceInspection(dt);
        }
        public void AdvanceInspection(float dt)
        {
            if(Chapter==null)return;
            for(int i=0;i<tapes.Length;i++)
            {
                var t=tapes[i];var data=Chapter.Tape.Candidates[i];
                if(t.damageMarker!=null)t.damageMarker.SetActive(data.Broken);
                if(i==Selected && (State==Phase.Loading || State==Phase.Playing || State==Phase.Paused))continue;
                visualTapePositions[i]=Mathf.Lerp(visualTapePositions[i],data.Position,1-Mathf.Exp(-Mathf.Max(0,dt)/.055f));
                // Imported PlaybackLoop rotates the spindle around local Y (not the shell's forward axis).
                var turn=Quaternion.AngleAxis(-visualTapePositions[i]*420,Vector3.up);
                t.leftReel.localRotation=turn;t.rightReel.localRotation=turn;
            }
        }
        public bool PointerOverUI(Vector2 point)
        {
            var gui=new Vector2(point.x,Screen.height-point.y);
            // The footer is passive help text, not a control. It must not make
            // authored cassettes near the lower edge of the view unpickable.
            return new Rect(12,12,Mathf.Min(740,Screen.width-250),statusHeight).Contains(gui)
                || ((State==Phase.Playing || State==Phase.Paused) && new Rect(20,132,230,32).Contains(gui));
        }
        public void TakeOut()
        {
            if(Selected<0 || State==Phase.Loading)return;
            int i=Selected;ResetDeck();PickUp(i);
        }
        public void PutBack(){if(State==Phase.Holding){CancelTapeDrag();ReleaseSpindle();State=Phase.Returning;}}
        public void AdvanceReturn(float dt)
        {
            if(State!=Phase.Returning || Selected<0)return;
            var t=tapes[Selected];var position=t.homeParent.TransformPoint(t.homePosition);var rotation=t.homeParent.rotation*t.homeRotation;
            t.root.position=Vector3.MoveTowards(t.root.position,position,Mathf.Max(0,dt)*1.8f);
            t.root.rotation=Quaternion.RotateTowards(t.root.rotation,rotation,Mathf.Max(0,dt)*540);
            if(Vector3.Distance(t.root.position,position)<.001f && Quaternion.Angle(t.root.rotation,rotation)<.1f)ResetDeck();
        }
        public void DrawStation(GUIStyle style)
        {
            if(!StationActive)return;
            string status=draggingTape?(dropReady?"松开左键装入磁带 · 右键取消":"按住左键拖向卡槽，松手装入；槽外松手留在手中。"):
                State==Phase.Holding?"左键按住轮轴 + 滚轮检查；按住外壳拖向卡槽，松手装入；右键放回。":
                Chapter.Completed?"已找到完整磁带，声音恢复播放。":"按住左键拿起磁带，拖向卡槽后松手装入。";
            if(State==Phase.Loading)status="正在装入磁带…";if(State==Phase.Returning)status="正在放回磁带…";
            if(Selected>=0)status+="\n磁带 "+(Selected+1)+" · 已检查 "+Mathf.RoundToInt(Chapter.Tape.InspectionProgress*100)+"%";
            if(Notice!="")status+="\n"+Notice;
            float textWidth=Mathf.Min(716,Screen.width-274);statusHeight=Mathf.Clamp(style.CalcHeight(new GUIContent(status),textWidth)+16,46,108);
            GUI.Box(new Rect(12,12,Mathf.Min(740,Screen.width-250),statusHeight),"");GUI.Label(new Rect(24,20,textWidth,statusHeight-16),status,style);
            GUI.Box(new Rect(12,Screen.height-56,Screen.width-24,44),"");GUI.Label(new Rect(24,Screen.height-50,Screen.width-48,34),"E 安全检视 · H 操作说明 · Esc 返回书房 · 右键放回 / 取出",style);
            if(waitingForRelease || house.ShowHelp)return;
            if(State==Phase.Playing || State==Phase.Paused)
            {
                if(GUI.Button(new Rect(20,132,110,32),State==Phase.Playing?"暂停播放":"继续播放"))TogglePlayback();
                if(GUI.Button(new Rect(140,132,110,32),"取出磁带")){TakeOut();waitingForRelease=true;}
            }
        }
    }
}
