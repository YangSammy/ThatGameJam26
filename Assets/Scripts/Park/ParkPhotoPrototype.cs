using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    [Serializable] public sealed class ParkPhotoRecord
    {
        public string id,filmId,capturedUtc,negativeFile,moment,feedback;
        public int frameNumber;
        public Vector3 cameraPosition,cameraEuler,subjectPosition;
        public float focusDistance,subjectDepth,fieldOfView,subjectScreenHeight;
        public bool sharp,visible,framed,accepted;
    }
    [RequireComponent(typeof(CharacterController))]
    public sealed class ParkPhotoPrototype : MonoBehaviour
    {
        [Header("场景引用")]
        public Camera view;
        public Transform spawn;
        public ParkCompanion companion;
        public ParkPhotoFocus focusEffect;
        public AudioSource sound;
        public AudioClip shutter,callSound;
        // Serialized legacy fields retained for compatibility; shared tuning now
        // comes from Resources/PlayerControlSettings.asset in both scenes.
        [HideInInspector]
        [Min(.1f)] public float walkSpeed=2.7f;
        [HideInInspector,Min(.1f)] public float aimWalkSpeed=1.1f;
        [HideInInspector,Min(.1f)] public float lookSensitivity=1.7f;
        [Header("取景与手动调焦")]
        [HideInInspector,Range(30,90)] public float walkingFov=65;
        [HideInInspector,Range(25,70)] public float viewfinderFov=48;
        [Min(.1f)] public float minimumFocus=1.2f,maximumFocus=18,focusStep=.25f;
        [Tooltip("沿用上一张的焦距，玩家需要根据人物距离亲手重新调焦。") ] public float focusDistance=4;
        [Header("胶卷记录：每卷固定 8 张，曝光后自动保存")]
        [Range(640,1920)] public int photoWidth=960;
        public event Action<ParkPhotoRecord> PhotoTaken;
        public event Action<string> FilmFilled;
        public string CurrentFilmId=>roll!=null?roll.id:"";
        public int ExposedFrames=>roll!=null?roll.frames.Count:0;
        public int RemainingFrames=>roll!=null?roll.Remaining:0;
        public bool CanExpose=>roll!=null && filmError=="" && roll.state==ParkFilmState.Loaded && roll.Remaining>0;
        public bool CanReturnToDarkroom=>roll!=null && filmError=="" && roll.Remaining==0 && roll.state!=ParkFilmState.Loaded;
        public ParkFilmState FilmState=>roll!=null?roll.state:ParkFilmState.Loaded;
        public IReadOnlyList<ParkPhotoRecord> Records=>roll!=null?(IReadOnlyList<ParkPhotoRecord>)roll.frames.AsReadOnly():Array.Empty<ParkPhotoRecord>();
        public bool Aiming {get;private set;}
        public bool Paused {get;private set;}
        public float MovementSpeed=>motor!=null?new Vector2(motor.velocity.x,motor.velocity.z).magnitude:0;
        public float ShutterPulse=>flash;
        ParkFilmRoll roll;
        CharacterController motor;Font font;
        float yaw,pitch,verticalSpeed,shotCooldown,flash,noticeUntil;
        string notice="",filmError="";
        bool inputGate=true,focused=true;
        bool returnUiGate,changingScene;
        readonly PlayerInputReleaseGate handoff=new PlayerInputReleaseGate();
        Vector3 moveVelocity;
        float aimBlend;
        public PlayerControlState ControlState=>!focused?PlayerControlState.Unfocused:changingScene?
            PlayerControlState.Transition:Paused?PlayerControlState.Paused:Aiming?PlayerControlState.Operation:PlayerControlState.Exploration;
        void Start()
        {
            motor=GetComponent<CharacterController>();
            if(view==null || spawn==null || companion==null || focusEffect==null){Debug.LogError("Park photo prototype is missing scene references.",this);enabled=false;return;}
            motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);motor.enabled=true;
            var controls=PlayerControlSettings.Current;
            yaw=spawn.eulerAngles.y;pitch=controls.ClampPitch(Mathf.DeltaAngle(0,spawn.eulerAngles.x));
            transform.rotation=Quaternion.Euler(0,yaw,0);
            view.transform.localPosition=new Vector3(0,controls.eyeHeight,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            view.fieldOfView=controls.parkWalkingFov;handoff.Arm();
            try{roll=ParkFilmStore.LoadOrCreate();if(roll.frames.Count>0)focusDistance=Mathf.Clamp(roll.frames[roll.frames.Count-1].focusDistance,minimumFocus,maximumFocus);}
            catch(Exception ex){filmError="胶卷记录暂时无法读取，已停止曝光以保留原数据。";Debug.LogWarning("Park film load: "+ex.Message);}
            SetCursor(false);Notify(roll!=null && roll.state!=ParkFilmState.Loaded?"这卷胶片已封存，等待暗室处理。": "一卷 8 张。按右键取景、滚轮调焦；成片需要显影后才能看到。",7);
            if(CanReturnToDarkroom)OpenFilmReturnPanel();
        }
        void SetCursor(bool free){PlayerInputPolicy.SetCursor(free);}
        void Notify(string text,float seconds=4){notice=text;noticeUntil=Time.unscaledTime+seconds;}
        public void Resume(){Paused=false;inputGate=true;moveVelocity=Vector3.zero;handoff.Arm();SetCursor(false);}
        void PauseControls()
        {
            Paused=true;Aiming=false;moveVelocity=Vector3.zero;handoff.Arm();
            if(companion!=null)companion.Paused=true;
            if(focusEffect!=null)focusEffect.activeFocus=false;SetCursor(true);
        }
        void OpenFilmReturnPanel()
        {
            PauseControls();returnUiGate=true;inputGate=true;
        }
        public void ReturnToDarkroom()
        {
            if(!CanReturnToDarkroom || changingScene || returnUiGate)return;
            try
            {
                // Reload the committed roll before leaving; never pass a transient capture.
                roll=ParkFilmStore.Load(roll.id);
                if(!CanReturnToDarkroom)return;
                if(!Application.CanStreamedLevelBeLoaded(ParkFilmRoute.HouseScenePath))throw new InvalidOperationException("HouseScene is not in Build Settings.");
                changingScene=true;
                ParkFilmRoute.EnterDarkroomOnLoad=true;
                UnityEngine.SceneManagement.SceneManager.LoadScene(ParkFilmRoute.HouseScenePath);
            }
            catch(Exception ex)
            {
                ParkFilmRoute.EnterDarkroomOnLoad=false;
                changingScene=false;
                Notify("暂时无法返回书房，胶卷仍已保留。",6);Debug.LogWarning("Park return: "+ex.Message);
            }
        }
        void OnApplicationFocus(bool value)
        {focused=value;if(!value)PauseControls();else handoff.Arm();}
        void Update()
        {
            if(!focused || changingScene)return;
            // The final shutter press must be released before it can activate UI.
            if(returnUiGate && handoff.Ready)returnUiGate=false;
            var controls=PlayerControlSettings.Current;
            float dt=Mathf.Min(Time.deltaTime,.1f);shotCooldown=Mathf.Max(0,shotCooldown-dt);flash=Mathf.Max(0,flash-dt*4);
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(Paused)Resume();
                else if(Aiming){Aiming=false;inputGate=true;moveVelocity=Vector3.zero;handoff.Arm();}
                else PauseControls();
            }
            companion.Paused=Paused;
            focusEffect.activeFocus=Aiming && !Paused;focusEffect.distance=focusDistance;
            if(Paused)return;
            PlayerInputPolicy.ApplyCursor(ControlState,true);
            if(inputGate){if(handoff.Ready)inputGate=false;return;}
            if(Input.GetMouseButtonDown(1))Aiming=!Aiming;
            if(Input.GetKeyDown(KeyCode.R))
            {companion.RestartWalk();motor.enabled=false;transform.SetPositionAndRotation(spawn.position,spawn.rotation);motor.enabled=true;yaw=spawn.eulerAngles.y;pitch=controls.ClampPitch(Mathf.DeltaAngle(0,spawn.eulerAngles.x));verticalSpeed=0;moveVelocity=Vector3.zero;Aiming=false;inputGate=true;handoff.Arm();Notify("重新同行，仍使用原胶卷；已曝光张数不会重置。");return;}
            if(Input.GetKeyDown(KeyCode.E))
            {if(companion.Call()){if(sound!=null && callSound!=null)sound.PlayOneShot(callSound,.3f);}else Notify("走近一些，等对方回应后再喊。");}
            aimBlend=Mathf.MoveTowards(aimBlend,Aiming?1:0,dt/Mathf.Max(.01f,controls.aimTransitionSeconds));
            var look=PlayerInputPolicy.LookDelta;float sensitivity=controls.lookSensitivity*Mathf.Lerp(1,controls.aimingLookMultiplier,aimBlend);
            yaw+=look.x*sensitivity;pitch=controls.ClampPitch(pitch-look.y*sensitivity);
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var move=Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0)),1);
            float speed=controls.walkSpeed*Mathf.Lerp(controls.parkSpeedMultiplier,controls.aimingSpeedMultiplier,aimBlend);
            moveVelocity=controls.ApproachVelocity(moveVelocity,(transform.right*move.x+transform.forward*move.y)*speed,dt);
            // Bounded moves keep gravity/ground contact stable over bridge seams
            // and on slower frames. Ground geometry remains authored in ParkScene.
            float moveRemaining=dt;
            while(moveRemaining>0)
            {
                float step=Mathf.Min(.02f,moveRemaining);moveRemaining-=step;
                if(motor.isGrounded && verticalSpeed<0)verticalSpeed=-2;
                verticalSpeed-=18*step;
                var collisions=motor.Move((moveVelocity+Vector3.up*verticalSpeed)*step);
                if((collisions & CollisionFlags.Above)!=0 && verticalSpeed>0)verticalSpeed=0;
            }
            if(transform.position.y< -3){motor.enabled=false;transform.position=spawn.position;motor.enabled=true;verticalSpeed=0;Notify("回到小路起点，胶卷记录保留。");}
            view.fieldOfView=Mathf.Lerp(controls.parkWalkingFov,controls.parkAimingFov,Mathf.SmoothStep(0,1,aimBlend));
            if(Aiming)
            {
                focusDistance=Mathf.Clamp(focusDistance+Input.mouseScrollDelta.y*focusStep,minimumFocus,Mathf.Max(minimumFocus,maximumFocus));
                focusEffect.activeFocus=true;focusEffect.distance=focusDistance;
                if(Input.GetMouseButtonDown(0) && shotCooldown<=0)Capture();
            }
            else focusEffect.activeFocus=false;
        }
        bool Visible(Vector3 point)
        {
            RaycastHit hit;var delta=point-view.transform.position;
            if(!Physics.Raycast(view.transform.position,delta.normalized,out hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore))return true;
            return hit.transform.IsChildOf(companion.transform);
        }
        ParkPhotoRecord Assess()
        {
            var head=view.WorldToViewportPoint(companion.head.position);var hips=view.WorldToViewportPoint(companion.hips.position);
            var feet=view.WorldToViewportPoint(companion.feet.position);var chest=view.WorldToViewportPoint(companion.chest.position);
            bool inFrame=head.z>0 && hips.z>0 && head.x>.06f && head.x<.94f && head.y>.08f && head.y<.92f && hips.x>.06f && hips.x<.94f && hips.y>.07f && hips.y<.93f;
            float height=Mathf.Abs(head.y-feet.y);
            var photo=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),capturedUtc=DateTime.UtcNow.ToString("o"),cameraPosition=view.transform.position,cameraEuler=view.transform.eulerAngles,subjectPosition=companion.transform.position,
                focusDistance=focusDistance,subjectDepth=chest.z,fieldOfView=view.fieldOfView,subjectScreenHeight=height,
                sharp=Mathf.Abs(chest.z-focusDistance)<Mathf.Max(.2f,chest.z*.065f),visible=Visible(companion.head.position)&&Visible(companion.chest.position),framed=inFrame && height>.18f && height<.95f};
            var direction=view.transform.position-companion.transform.position;direction.y=0;float facing=Vector3.Dot(companion.transform.forward,direction.normalized);
            int moment=-1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Walking && facing<-.2f)moment=0;
            if(companion.CurrentMoment==ParkCompanion.Moment.Waterside && Mathf.Abs(facing)<.8f)moment=1;
            if(companion.CurrentMoment==ParkCompanion.Moment.Response && facing>.65f)moment=2;
            photo.moment=moment>=0?MomentName(moment):"散步随拍";
            photo.accepted=photo.sharp && photo.visible && photo.framed && moment>=0;
            photo.feedback=!photo.framed?"调整距离和构图，让人物头部与身体留在画面里。":!photo.visible?"人物被遮挡了，横移几步找空隙。":!photo.sharp?"焦点没有落在人物身上，滚轮调整后再拍。":moment<0?"这张很清楚。也可以留意背影、桥边侧影和回头回应。":"留住了「"+photo.moment+"」。";
            return photo;
        }
        static string MomentName(int i){return i==0?"同行的背影":i==1?"桥边望水":"回头的回应";}
        void Capture()
        {
            if(roll==null || filmError!=""){Notify(filmError!=""?filmError:"没有可用的胶卷。");return;}
            if(roll.state!=ParkFilmState.Loaded || roll.Remaining<=0){Notify("这一卷已经拍完，等待暗室显影后查看。",5);return;}
            shotCooldown=.7f;var record=Assess();Texture2D image=null;
            var previousTarget=view.targetTexture;var previousActive=RenderTexture.active;int previousMask=view.cullingMask;
            RenderTexture target=null;
            try
            {
                target=RenderTexture.GetTemporary(photoWidth,Mathf.Max(1,Mathf.RoundToInt(photoWidth/view.aspect)),24,RenderTextureFormat.ARGB32);
                view.cullingMask&=~(1<<FirstPersonHands.HandLayer);
                view.targetTexture=target;view.Render();RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            }
            catch(Exception ex){if(image!=null)Destroy(image);Notify("快门未完成记录，本次不扣张数，请再试一次。");Debug.LogWarning("Park capture: "+ex.Message);return;}
            finally{view.targetTexture=previousTarget;view.cullingMask=previousMask;RenderTexture.active=previousActive;if(target!=null)RenderTexture.ReleaseTemporary(target);}
            try
            {
                roll=ParkFilmStore.SaveExposure(roll,record,image.EncodeToPNG());
            }
            catch(Exception ex){filmError="胶卷暂时无法保存，已停止继续曝光；原有记录保留。";Notify(filmError,7);Debug.LogWarning("Park film save: "+ex.Message);return;}
            finally{if(image!=null)Destroy(image);}
            flash=1;Notify(roll.Remaining==0?"第 8 张已曝光。这卷已拍完，等待暗室显影。":"第 "+record.frameNumber+" 张已曝光 · 剩余 "+roll.Remaining+" 张",4);
            if(sound!=null && shutter!=null)sound.PlayOneShot(shutter,.6f);
            if(roll.Remaining==0)OpenFilmReturnPanel();
            PhotoTaken?.Invoke(record);
            if(roll.Remaining==0)FilmFilled?.Invoke(roll.id);
        }
        void OnDisable(){if(companion!=null)companion.Paused=true;if(focusEffect!=null)focusEffect.activeFocus=false;SetCursor(true);}
        void OnDestroy(){if(font!=null)Destroy(font);}
        void OnGUI()
        {
            if(view==null || companion==null)return;
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},18);
            var text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};var small=new GUIStyle(text){fontSize=15};var button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
            if(Paused)
            {
                GUI.Box(new Rect(Screen.width*.5f-175,Screen.height*.5f-70,350,CanReturnToDarkroom?208:150),"");
                GUI.Label(new Rect(Screen.width*.5f-150,Screen.height*.5f-50,300,50),CanReturnToDarkroom?"胶卷已拍满 · 8 / 8 张\n回暗室冲洗后才能查看成片。":"散步已暂停，人物会等你。",text);
                bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled && !returnUiGate && !changingScene;
                if(GUI.Button(new Rect(Screen.width*.5f-100,Screen.height*.5f+10,200,40),"继续散步 [Esc]",button))Resume();
                if(CanReturnToDarkroom && GUI.Button(new Rect(Screen.width*.5f-145,Screen.height*.5f+66,290,40),"带胶卷回书房暗室",button))ReturnToDarkroom();
                GUI.enabled=previousEnabled;
                if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(24,Screen.height-75,Screen.width-48,50),notice,text);
                return;
            }
            GUI.Box(new Rect(16,16,350,105),"");
            GUI.Label(new Rect(28,23,325,28),roll==null?"胶卷暂不可用":"胶卷 · 已曝光 "+ExposedFrames+" / 8 · 剩余 "+RemainingFrames+" 张",text);
            GUI.Label(new Rect(28,56,325,52),filmError!=""?filmError:CanReturnToDarkroom?"胶卷已拍满，Esc 打开回程菜单。\n点击按钮返回书房暗室。":"公园只看取景器，显影后才能看成片。",small);
            if(Aiming)
            {
                float cx=Screen.width*.5f,cy=Screen.height*.5f;
                GUI.Box(new Rect(cx-25,cy-25,50,50),"");
                GUI.Label(new Rect(cx-170,Screen.height-118,340,35),"手动焦点 "+focusDistance.ToString("0.00")+" 米 · 滚轮调节",text);
            }
            string line=Time.unscaledTime<noticeUntil?notice:companion.Waiting?"对方在等你，沿小路跟上。":companion.Line;
            if(!string.IsNullOrEmpty(line))GUI.Label(new Rect(Screen.width*.5f-310,Screen.height-166,620,48),line,text);
            GUI.Box(new Rect(12,Screen.height-65,Screen.width-24,53),"");
            GUI.Label(new Rect(24,Screen.height-59,Screen.width-48,46),"WASD 行走 · 鼠标环视 · 右键举起／放下相机 · 滚轮手动调焦 · 左键拍照\nE 喊一声 · R 重新同行（不重置胶卷） · Esc 放下相机／暂停",small);
            if(flash>0){var color=GUI.color;GUI.color=new Color(1,1,1,flash*.55f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;}
        }
    }
}
