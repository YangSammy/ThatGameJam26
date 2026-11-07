using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        [Header("Target metering (regional light approximation)")]
        public ParkRegionalMetering regionalMetering;
        public float MeterEv100 {get;private set;}=13;
        bool introductoryMeterRead;
        bool meterReadingValid;
        string meteredTask;
        bool NeedsMeterGuidance=>!meterReadingValid || meteredTask!=CurrentTaskId;
        bool MeterLockedToFilm=>roll!=null && (loadingStep>=2 || roll.frames.Count>0);
        float MeterSuggestedAperture=>ParkCameraResponse.SuggestedAperture(Aperture,MeterIso,MeterEv100);
        string MeterNumbers=>meterReadingValid?
            "ISO "+MeterIso+"   EV "+MeterEv100.ToString("0.0")+"   f/"+MeterSuggestedAperture+"   1/"+
            Mathf.RoundToInt(1/ParkCameraResponse.SuggestedShutter(MeterSuggestedAperture,MeterIso,MeterEv100))+" s   Held":
            "ISO "+MeterIso+"   Aim at her / LMB: measure";
        string MeterInstruction=>!meterReadingValid?"Aim at your companion. LMB: measure.":
            MeterLockedToFilm?"Scroll: compare ISO. Film ISO "+FilmIso+". LMB: remeasure.":
            "Reading held. Scroll: compare ISO. E: lower meter, then E: load film.";
        void UpdateRegionalMetering()
        {
            if(MeterLockedToFilm && photoMode!=PhotographyMode.Meter)meterIsoIndex=System.Array.IndexOf(filmIsos,FilmIso);
            // Live subject light remains the exposure authority. The meter holds a separate sample.
            if(regionalMetering!=null)
                sceneEv100=regionalMetering.Sample(companion!=null && companion.chest!=null?companion.chest.position:transform.position);
        }
        void TickTargetMeter()
        {
            var controls=PlayerControlSettings.Current;
            var look=PlayerInputPolicy.LookDelta;
            yaw+=look.x*controls.lookSensitivity;
            pitch=controls.ClampPitch(pitch-look.y*controls.lookSensitivity);
            transform.rotation=Quaternion.Euler(0,yaw,0);
            view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            virtualPointer=new Vector2(.5f,.5f);
            if(!Input.GetMouseButtonDown(0))return;
            if(companion==null || companion.chest==null)return;
            var target=companion.chest.position;
            if(!InView(target) || Vector3.Angle(view.transform.forward,target-view.transform.position)>12 || !Visible(target))
            {Notify("Aim at your companion; keep her visible.",3);return;}
            UpdateRegionalMetering();
            MeterEv100=sceneEv100;meterReadingValid=true;introductoryMeterRead=true;meteredTask=CurrentTaskId;
            PlayPhotographyCue(51);Notify("Reading held. R: lower meter.",3);
        }
        string OpeningPhotoInstruction()
        {
            if(roll==null)return "Film unavailable. Leave and retry; saved progress is kept.";
            if(roll.ReadyForPark)return "WASD: walk. E: inspect camera; click eyepiece to compose.";
            if(loadingStep>=2)return "Continue loading your film. E: camera.";
            return introductoryMeterRead?"Choose an ISO, then E: load film. Your companion will wait.":
                "R: raise meter. Aim at her and LMB: measure before choosing film.";
        }
    }
}
