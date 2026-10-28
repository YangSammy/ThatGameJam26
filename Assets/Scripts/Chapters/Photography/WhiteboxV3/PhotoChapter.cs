using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox.V3
{
    // The model contains no screen coordinates or scene objects: the same physical gates are
    // used by mouse interaction and the reproducible editor checks.
    public sealed class PhotoChapter : Chapter
    {
        public enum FilmPhase { Empty, LoadedOpen, Closed, Ready, Exposed, Rewound, DarkBag, Tank, Processing, Developed }
        public enum LoadStep { ClosedEmpty, BackOpen, Cartridge, Leader, Perforations, AdvanceChecked, ClosedLoaded, Ready }
        public enum DarkStep
        {
            None, Rewound, BagLoaded, BagSealed, ReelLoaded, TankSealed,
            Developer, DeveloperDrain, StopReady, Stop, StopDrain, FixReady, Fixer, FixerDrain,
            WashReady, Wash, WashDrain, NegativeReady, NegativeDrying, ContactReady, ContactExposing,
            PaperReady, PaperDeveloper, PaperStopReady, PaperStop, PaperFixReady, PaperFix,
            PaperWashReady, PaperWash, PaperDryReady, PaperDrying, Complete
        }
        [Serializable] public sealed class CameraSettings
        {
            public float X, Z, Yaw, Pitch, Focal = 50, Focus = 5, Aperture = 8, Shutter = 1f / 125;
            public int ISO = 100;
            public CameraSettings Copy() => (CameraSettings)MemberwiseClone();
        }
        [Serializable] public sealed class Exposure
        {
            public int ReferenceIndex;
            public CameraSettings Settings;
            public float GirlAction;
            public bool HandResponse;
        }
        public static readonly int[] FilmSpeeds = { 100, 200, 400, 800 };
        public static readonly float[] Apertures = { 2.8f, 4, 5.6f, 8, 11, 16 };
        public static readonly float[] Shutters = { 1f/4, 1f/8, 1f/15, 1f/30, 1f/60, 1f/125, 1f/250, 1f/500, 1f/1000 };
        public readonly CameraSettings Settings = new CameraSettings { X = -.8f, Z = -1 };
        public readonly CameraSettings[] Targets = {
            new CameraSettings { X = 0, Z = 0, Focus = 5, Aperture = 8, Shutter = 1f/250, ISO = 200 },
            new CameraSettings { X = 8, Z = .25f, Focus = 5.25f, Aperture = 5.6f, Shutter = 1f/250, ISO = 200 },
            new CameraSettings { X = 16, Z = -.2f, Focus = 4.8f, Aperture = 4, Shutter = 1f/250, ISO = 200 }
        };
        public readonly List<Exposure> Exposures = new List<Exposure>();
        public CameraSettings Target => Targets[Mathf.Clamp(Exposures.Count, 0, 2)];
        public CameraSettings Captured => Exposures.Count == 0 ? null : Exposures[Exposures.Count-1].Settings;
        public int CurrentFrame => Mathf.Min(Exposures.Count, 2);
        public FilmPhase Phase;
        public LoadStep Loading;
        public DarkStep Darkroom;
        public int Chemistry;
        public float GeometryError, ExposureError, FocusBlur, MotionBlur;
        public float ProcessingTime, ProcessingDuration, BagTurns, RewindTurns, GirlActionProgress;
        public bool FilmThreaded, Agitated, Fogged, InViewfinder, MeterVisible, HandResponse, CompositionQualified;
        public Action BeforeComplete;
        public bool SuppressCompletionNotification;
        public int Attempts;
        float qualificationAge;
        CameraSettings qualifiedSettings;
        public bool AllExposed => Exposures.Count == 3;
        public bool NeedsDrain => Darkroom == DarkStep.DeveloperDrain || Darkroom == DarkStep.StopDrain || Darkroom == DarkStep.FixerDrain || Darkroom == DarkStep.WashDrain;
        public bool TankFixed => Darkroom >= DarkStep.FixerDrain;
        public bool TimerRunning => ProcessingTime > 0;
        public float TimerProgress => ProcessingDuration <= 0 ? 0 : 1 - ProcessingTime / ProcessingDuration;
        public override string Help => "";
        public override void Draw() { }

        public static float EV(CameraSettings s) => Mathf.Log(s.Aperture*s.Aperture / s.Shutter, 2) - Mathf.Log(s.ISO/100f, 2);
        public static float BlurAt(float distance, CameraSettings s, float width = 1280)
        {
            float f = s.Focal/1000f, d = Mathf.Max(.2f, distance), focus = Mathf.Max(.2f, s.Focus);
            float v = f*d/(d-f), vf = f*focus/(focus-f);
            return Mathf.Abs(vf-v)/v*(f/s.Aperture)/.036f*width;
        }
        public static Vector2 Project(Vector3 p, CameraSettings s, Rect frame)
        {
            var q = Quaternion.Euler(s.Pitch*Mathf.Rad2Deg, s.Yaw*Mathf.Rad2Deg, 0);
            Vector3 local = Quaternion.Inverse(q) * (p - new Vector3(s.X, 1.6f, s.Z));
            float scale = frame.width*s.Focal/.036f/1000f / Mathf.Max(.15f, local.z);
            return frame.center + new Vector2(local.x, -local.y)*scale;
        }
        public Vector3[] Landmarks(int frame)
        {
            float x = frame*8;
            if(frame == 0) return new[] { new Vector3(x-1.25f,.7f,4), new Vector3(x+1.25f,2.8f,5), new Vector3(x+.5f,.2f,7) };
            if(frame == 1) return new[] { new Vector3(x-1.1f,.6f,3.4f), new Vector3(x+1.3f,1.2f,6.5f), new Vector3(x,2.7f,8) };
            return new[] { new Vector3(x-1.2f,.25f,4), new Vector3(x+1.5f,1.2f,6.5f), new Vector3(x+.3f,2.8f,8) };
        }
        public float SubjectDistance(CameraSettings s)
        {
            var subject = new Vector3(CurrentFrame*8, 1.4f, CurrentFrame == 0 ? 5 : CurrentFrame == 1 ? 5.5f : 4.6f);
            return Vector3.Dot(subject-new Vector3(s.X,1.6f,s.Z), Quaternion.Euler(s.Pitch*Mathf.Rad2Deg,s.Yaw*Mathf.Rad2Deg,0)*Vector3.forward);
        }
        public void Assess()
        {
            var points = Landmarks(CurrentFrame); var frame = new Rect(0,0,1280,760);
            GeometryError = 0;
            foreach(var p in points) GeometryError += Vector2.Distance(Project(p,Settings,frame),Project(p,Target,frame))/points.Length;
            ExposureError = Mathf.Abs(EV(Settings)-EV(Target));
            FocusBlur = BlurAt(SubjectDistance(Settings),Settings);
            MotionBlur = Mathf.Max(0,Settings.Shutter/(1f/125)-1)*2.5f;
        }
        static bool Same(CameraSettings a, CameraSettings b)
        {
            return a != null && b != null && Mathf.Abs(a.X-b.X)<.04f && Mathf.Abs(a.Z-b.Z)<.04f && Mathf.Abs(a.Yaw-b.Yaw)<.007f && Mathf.Abs(a.Pitch-b.Pitch)<.007f
                && Mathf.Abs(a.Focus-b.Focus)<.04f && a.Aperture==b.Aperture && a.Shutter==b.Shutter && a.ISO==b.ISO;
        }
        public override void Tick(float dt, WBInput input)
        {
            if(Completed) return;
            if(Phase == FilmPhase.Ready && !AllExposed)
            {
                Assess();
                bool valid = GeometryError<27 && ExposureError<.42f && FocusBlur<1.65f && MotionBlur<3.5f;
                if(!valid || (CompositionQualified && !Same(Settings,qualifiedSettings)))
                { CompositionQualified=false; GirlActionProgress=0; HandResponse=false; qualificationAge=0; }
                if(valid)
                {
                    if(!CompositionQualified) { CompositionQualified=true; qualifiedSettings=Settings.Copy(); }
                    if(InViewfinder)qualificationAge += dt;
                    GirlActionProgress = Mathf.Clamp01((qualificationAge-.35f)/1.7f);
                }
            }
            if(ProcessingTime<=0) return;
            // Tank development/fixing require one physical agitation gesture. All other baths run freely.
            if((Darkroom==DarkStep.Developer || Darkroom==DarkStep.Fixer) && !Agitated) return;
            ProcessingTime = Mathf.Max(0, ProcessingTime-dt);
            if(ProcessingTime>0) return;
            switch(Darkroom)
            {
                case DarkStep.Developer: Darkroom=DarkStep.DeveloperDrain; Phase=FilmPhase.Tank; break;
                case DarkStep.Stop: Darkroom=DarkStep.StopDrain; Phase=FilmPhase.Tank; break;
                case DarkStep.Fixer: Darkroom=DarkStep.FixerDrain; Phase=FilmPhase.Tank; break;
                case DarkStep.Wash: Darkroom=DarkStep.WashDrain; Phase=FilmPhase.Tank; break;
                case DarkStep.NegativeDrying: Darkroom=DarkStep.ContactReady; break;
                case DarkStep.ContactExposing: Darkroom=DarkStep.PaperReady; break;
                case DarkStep.PaperDeveloper: Darkroom=DarkStep.PaperStopReady; break;
                case DarkStep.PaperStop: Darkroom=DarkStep.PaperFixReady; break;
                case DarkStep.PaperFix: Darkroom=DarkStep.PaperWashReady; break;
                case DarkStep.PaperWash: Darkroom=DarkStep.PaperDryReady; break;
                case DarkStep.PaperDrying:
                    Darkroom=DarkStep.Complete; Phase=FilmPhase.Developed; BeforeComplete?.Invoke();
                    if(SuppressCompletionNotification)Completed=true;else Finish();
                    break;
            }
        }
        public bool OpenBack()
        {
            if(Loading!=LoadStep.ClosedEmpty || Phase!=FilmPhase.Empty) return false;
            Loading=LoadStep.BackOpen; return true;
        }
        public bool InsertFilm(int iso)
        {
            if(Loading!=LoadStep.BackOpen || Array.IndexOf(FilmSpeeds,iso)<0) return false;
            Settings.ISO=iso; Loading=LoadStep.Cartridge; Phase=FilmPhase.LoadedOpen; return true;
        }
        public bool PullLeader() { if(Loading!=LoadStep.Cartridge) return false; Loading=LoadStep.Leader; return true; }
        public bool SeatPerforations() { if(Loading!=LoadStep.Leader) return false; Loading=LoadStep.Perforations; FilmThreaded=true; return true; }
        public bool Advance()
        {
            if(Loading==LoadStep.Perforations) { Loading=LoadStep.AdvanceChecked; return true; }
            if(Loading==LoadStep.ClosedLoaded) { Loading=LoadStep.Ready; Phase=FilmPhase.Ready; return true; }
            if(Phase==FilmPhase.Exposed && !AllExposed) { Phase=FilmPhase.Ready; ResetQualification(); return true; }
            return false;
        }
        public bool CloseBack()
        {
            if(Loading!=LoadStep.AdvanceChecked) return false;
            Loading=LoadStep.ClosedLoaded; Phase=FilmPhase.Closed; return true;
        }
        public void AdjustFocus(float step) { Settings.Focus=Mathf.Clamp(Settings.Focus+step*.2f,.6f,20); ResetQualification(); }
        public void AdjustAperture(int step) { Settings.Aperture=Apertures[Mathf.Clamp(Closest(Apertures,Settings.Aperture)+step,0,Apertures.Length-1)]; ResetQualification(); }
        public void AdjustShutter(int step) { Settings.Shutter=Shutters[Mathf.Clamp(Closest(Shutters,Settings.Shutter)+step,0,Shutters.Length-1)]; ResetQualification(); }
        public bool RespondToHand() { if(CurrentFrame!=2 || GirlActionProgress<.6f) return false; HandResponse=true; return true; }
        public bool Expose()
        {
            Attempts++;
            if(Phase!=FilmPhase.Ready || AllExposed || !CompositionQualified || GirlActionProgress<.65f || (CurrentFrame==2 && !HandResponse)) return false;
            Exposures.Add(new Exposure { ReferenceIndex=CurrentFrame, Settings=Settings.Copy(), GirlAction=GirlActionProgress, HandResponse=HandResponse });
            Phase=FilmPhase.Exposed; InViewfinder=false; ResetQualification(); return true;
        }
        public bool Rewind(float turns)
        {
            if(!AllExposed || Phase!=FilmPhase.Exposed) return false;
            RewindTurns=Mathf.Clamp01(RewindTurns+Mathf.Abs(turns));
            if(RewindTurns<1) return true;
            Phase=FilmPhase.Rewound; Darkroom=DarkStep.Rewound; return true;
        }
        public bool PutInBag() { if(Darkroom!=DarkStep.Rewound) return false; Darkroom=DarkStep.BagLoaded; Phase=FilmPhase.DarkBag; return true; }
        public bool SealBag() { if(Darkroom!=DarkStep.BagLoaded) return false; Darkroom=DarkStep.BagSealed; return true; }
        public bool WindReel(float turns)
        {
            if(Darkroom!=DarkStep.BagSealed) return false;
            BagTurns=Mathf.Clamp01(BagTurns+Mathf.Abs(turns));
            if(BagTurns>=1) Darkroom=DarkStep.ReelLoaded;
            return true;
        }
        public bool SealTank() { if(Darkroom!=DarkStep.ReelLoaded) return false; Darkroom=DarkStep.TankSealed; Phase=FilmPhase.Tank; Chemistry=0; return true; }
        void StartTimer(DarkStep stage,float duration)
        { Darkroom=stage; ProcessingDuration=ProcessingTime=duration; Agitated=false; }
        public bool Pour(int liquid)
        {
            if(Darkroom==DarkStep.TankSealed && liquid==0) { StartTimer(DarkStep.Developer,3); Phase=FilmPhase.Processing; return true; }
            if(Darkroom==DarkStep.StopReady && liquid==1) { StartTimer(DarkStep.Stop,1); Phase=FilmPhase.Processing; return true; }
            if(Darkroom==DarkStep.FixReady && liquid==2) { StartTimer(DarkStep.Fixer,2.5f); Phase=FilmPhase.Processing; return true; }
            if(Darkroom==DarkStep.WashReady && liquid==3) { StartTimer(DarkStep.Wash,2); Phase=FilmPhase.Processing; return true; }
            return false;
        }
        public bool Drain()
        {
            if(Darkroom==DarkStep.DeveloperDrain) { Darkroom=DarkStep.StopReady; Chemistry=1; return true; }
            if(Darkroom==DarkStep.StopDrain) { Darkroom=DarkStep.FixReady; Chemistry=2; return true; }
            if(Darkroom==DarkStep.FixerDrain) { Darkroom=DarkStep.WashReady; Chemistry=3; return true; }
            if(Darkroom==DarkStep.WashDrain) { Darkroom=DarkStep.NegativeReady; Chemistry=4; return true; }
            return false;
        }
        public bool OpenTank() => Darkroom==DarkStep.NegativeReady && TankFixed;
        public bool HangNegative() { if(!OpenTank()) return false; StartTimer(DarkStep.NegativeDrying,2.5f); return true; }
        public bool BeginContact() { if(Darkroom!=DarkStep.ContactReady) return false; StartTimer(DarkStep.ContactExposing,1.5f); return true; }
        public void CancelContact()
        { if(Darkroom==DarkStep.ContactExposing) { Darkroom=DarkStep.ContactReady; ProcessingTime=0; } }
        public bool PaperBath(int liquid)
        {
            if(Darkroom==DarkStep.PaperReady && liquid==0) { StartTimer(DarkStep.PaperDeveloper,2.5f); return true; }
            if(Darkroom==DarkStep.PaperStopReady && liquid==1) { StartTimer(DarkStep.PaperStop,1); return true; }
            if(Darkroom==DarkStep.PaperFixReady && liquid==2) { StartTimer(DarkStep.PaperFix,2); return true; }
            if(Darkroom==DarkStep.PaperWashReady && liquid==3) { StartTimer(DarkStep.PaperWash,2); return true; }
            return false;
        }
        public bool HangPaper() { if(Darkroom!=DarkStep.PaperDryReady) return false; StartTimer(DarkStep.PaperDrying,3); return true; }
        public void ResetQualification() { CompositionQualified=false; qualificationAge=0; GirlActionProgress=0; HandResponse=false; qualifiedSettings=null; }
        public static int Closest(float[] values,float value)
        { int result=0; for(int i=1;i<values.Length;i++) if(Mathf.Abs(values[i]-value)<Mathf.Abs(values[result]-value)) result=i; return result; }
        public override string DebugState => "{\"phase\":\""+Phase+"\",\"loading\":\""+Loading+"\",\"darkroom\":\""+Darkroom+"\",\"frames\":"+Exposures.Count+",\"qualified\":"+CompositionQualified.ToString().ToLowerInvariant()+",\"completed\":"+Completed.ToString().ToLowerInvariant()+"}";
    }
}
