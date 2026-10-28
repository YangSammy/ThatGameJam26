using System;
using UnityEngine;

namespace MemoryWhitebox
{
    // The authored desk radio is the single player-facing tape inspection station.
    public sealed partial class HouseRadioDeck : MonoBehaviour
    {
        public enum Phase { Empty, Dragging, Loading, Playing, Paused, Returning }
        [Serializable] public sealed class Cassette
        {
            public Transform root, model, leftReel, rightReel;
            public Collider hit;
            public AudioClip audio;
            public string label;
            public Collider leftSpindleHit,rightSpindleHit;
            public GameObject damageMarker;
            public TapePreparationBindings preparation;
            [NonSerialized] public Transform homeParent;
            [NonSerialized] public Vector3 homePosition, homeScale;
            [NonSerialized] public Quaternion homeRotation, modelRotation;
            [NonSerialized] public Vector3 modelPosition, modelScale;
        }
        public Animator animator;
        public AudioSource speaker;
        public Transform carrier, proxyTape, leftDriver, rightDriver, startSocket;
        public Collider slotHit, radioHit;
        public Cassette[] tapes;
        public WBMain house;
        public Camera view;
        public SceneViewAnchor operationView;
        public Phase State { get; private set; }
        public int Selected { get; private set; } = -1;
        public int AudioStarts { get; private set; }
        public string Hint { get; private set; }
        bool initialized;
        float hiddenAudioPosition;
        bool restoreHiddenAudio;
        Renderer[] proxyRenderers;

        void Awake()
        {
            if(animator==null || speaker==null || carrier==null || proxyTape==null || tapes==null || tapes.Length!=3)
            { Debug.LogError("House radio is missing its authored references.",this);enabled=false;return; }
            proxyRenderers=proxyTape.GetComponentsInChildren<Renderer>(true);
            foreach(var r in proxyRenderers)r.enabled=false;
            foreach(var t in tapes)
            {
                t.homeParent=t.root.parent;t.homePosition=t.root.localPosition;t.homeRotation=t.root.localRotation;t.homeScale=t.root.localScale;
                t.modelPosition=t.model.localPosition;t.modelRotation=t.model.localRotation;t.modelScale=t.model.localScale;
            }
            animator.keepAnimatorStateOnDisable=true;
            initialized=true;Chapter=new RadioTapeChapter();ResetDeck();InitializeRecording();
        }
        void OnEnable()
        {
            if(!initialized)return;SetStationActive(false);
            if(restoreHiddenAudio && Selected>=0)
            {
                speaker.clip=tapes[Selected].audio;
                // A disabled AudioSource loses its playback cursor; seek requires an initialized voice.
                speaker.Play();speaker.time=Mathf.Clamp(hiddenAudioPosition,0,Mathf.Max(0,speaker.clip.length-.001f));speaker.Pause();restoreHiddenAudio=false;
            }
        }
        void OnDisable(){if(initialized)SetStationActive(false);}
        bool SelectForDrag(int index)
        {
            if(!initialized || State!=Phase.Empty || index<0 || index>=tapes.Length)return false;
            Selected=index;State=Phase.Dragging;tapes[index].hit.enabled=true;Notice="";
            Chapter.Tape.Selected=index;Chapter.Tape.SourceVerified=Chapter.Tape.Candidates[index].Intact;
            return true;
        }
        public bool InsertDragged()
        {
            if(State!=Phase.Dragging || Selected<0)return false;
            if(!Chapter.Tape.Candidates[Selected].Prepared){Notice="";WBMain.Tone(95,.15f);return false;}
            CancelTapeDrag();ReleaseSpindle();
            var t=tapes[Selected];t.root.SetParent(carrier,false);t.root.localPosition=Vector3.zero;t.root.localRotation=Quaternion.identity;t.root.localScale=Vector3.one;
            // The imported FBX wrapper holds axis conversion and centimeters. Under the
            // animated carrier those are already supplied by the radio's FBX root.
            t.model.localPosition=proxyTape.localPosition;t.model.localRotation=proxyTape.localRotation;t.model.localScale=proxyTape.localScale;
            animator.speed=1;animator.Play("LoadCassette",0,0);animator.Update(0);State=Phase.Loading;return true;
        }
        public void TogglePlayback(){OpenRecording();}
        public void PrepareForHide(){StopRecordingInput();SetStationActive(false);restoreHiddenAudio=false;}
        public void ResetDeck()
        {
            if(!initialized)return;
            CancelTapeDrag();ReleaseSpindle();
            transport?.StopAll();speaker.Stop();animator.speed=1;
            foreach(var t in tapes)
            {
                t.root.SetParent(t.homeParent,false);t.root.localPosition=t.homePosition;t.root.localRotation=t.homeRotation;t.root.localScale=t.homeScale;
                t.model.localPosition=t.modelPosition;t.model.localRotation=t.modelRotation;t.model.localScale=t.modelScale;
                t.hit.enabled=true;
                if(t.leftReel!=null)t.leftReel.localRotation=Quaternion.identity;
                if(t.rightReel!=null)t.rightReel.localRotation=Quaternion.identity;
            }
            Selected=-1;State=Phase.Empty;Chapter.Tape.Selected=-1;
            if(animator.isActiveAndEnabled){animator.Play("Ready_Open",0,0);animator.Update(0);}
        }
        void Update()
        {
            if(!initialized)return;
            if(!StationActive || waitingForRelease || !focused || (house!=null && house.ShowHelp) || (house!=null && house.safeInspection!=null && house.safeInspection.BlocksInput))return;
            if(State==Phase.Loading && animator.GetCurrentAnimatorStateInfo(0).IsName("PlaybackLoop"))
            {
                speaker.Stop();animator.speed=0;State=Phase.Paused;
                tapes[Selected].hit.enabled=true;
                Chapter.VerifyPlayback();
            }
            if(State==Phase.Playing && !speaker.isPlaying && speaker.clip!=null){animator.speed=0;State=Phase.Paused;}
        }
        void LateUpdate()
        {
            if(RecordingMode)return;
            if(!initialized || Selected<0)return;
            if(house!=null && house.safeInspection!=null && house.safeInspection.BlocksInput)return;
            if(!focused)return;
            var t=tapes[Selected];
            if(State==Phase.Returning){AdvanceReturn(Time.unscaledDeltaTime);return;}
            if(State==Phase.Dragging && view!=null)
            {
                PoseDraggedTape(Time.unscaledDeltaTime);
            }
            else if(State==Phase.Loading || State==Phase.Playing || State==Phase.Paused)
            {
                if(t.leftReel!=null)t.leftReel.localRotation=leftDriver.localRotation;
                if(t.rightReel!=null)t.rightReel.localRotation=rightDriver.localRotation;
            }
        }
        Collider Pick(Ray ray)
        {
            return StudyEntry.PickVisible(ray);
        }
        public string GetHint(Ray ray)
        {
            var hit=Pick(ray);
            if(!StationActive)return IsEntryHit(hit)?"LMB  Interact   /   E  Inspect":null;
            if(State==Phase.Dragging)return draggingTape?(hit==slotHit?"Release LMB to insert   /   RMB  Return":"LMB drag to slot. Release to insert."):"LMB drag shell to slot   /   RMB  Return";
            if(State==Phase.Loading)return "Loading tape...";
            if(State==Phase.Returning)return string.IsNullOrEmpty(Notice)?"Returning tape...":Notice;
            if(hit==radioHit || hit==slotHit)
            {
                if(State==Phase.Playing)return "LMB  Pause   /   RMB  Eject";
                if(State==Phase.Paused)return "LMB  Play   /   RMB  Eject";
                return "Drag tape to slot   /   E  Inspect radio";
            }
            for(int i=0;i<tapes.Length;i++)if(hit==tapes[i].hit)return State==Phase.Empty?"LMB drag  Tape "+(i+1)+"  /  E  Inspect":"Eject the loaded tape first";
            return null;
        }
        // Called by the existing house input owner; no second competing input loop.
        public bool HandleInput(Ray ray,bool left,bool right,bool inspect)
        {
            if(!StationActive)return false;
            bool consumed=State!=Phase.Empty || IsEntryHit(Pick(ray));
            TickStation(Time.unscaledDeltaTime,ray,left,right,0,inspect,!left && !right && !inspect);
            Hint=GetHint(ray);return consumed;
        }
    }
}
