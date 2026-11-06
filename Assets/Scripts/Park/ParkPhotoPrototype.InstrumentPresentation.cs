using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        [Header("Camera loading presentation")]
        [Tooltip("Camera-local offset for the complete loading kit, including film choices and hit targets. Does not change the optical view or model scale.")]
        public Vector3 cameraOperationOffset=new Vector3(0,.05f,-.12f);
        public int MeterIso=>filmIsos[meterIsoIndex];
        public bool MeterIsoMatchesFilm=>MeterIso==FilmIso;
        public Transform InstrumentLeftHandTarget=>HeldInstrumentTarget(true);
        public Transform InstrumentRightHandTarget=>HeldInstrumentTarget(false);
        Transform apertureHandContact,meterHandContact;
        Quaternion presentationStart=Quaternion.identity,presentationGoal=Quaternion.identity;
        float presentationElapsed=.2f;
        int presentationFrame=-1;
        AudioSource instrumentClickSource;
        AudioClip instrumentClick;

        static Quaternion InstrumentPresentationRotation(PhotographyMode mode)
        {
            if(mode==PhotographyMode.Aperture)return Quaternion.Euler(0,100,0);
            if(mode==PhotographyMode.Shutter)return Quaternion.Euler(18,-8,0);
            return Quaternion.identity;
        }
        void SyncInstrumentPresentation()
        {
            var target=InstrumentPresentationRotation(photoMode);
            if(Quaternion.Angle(target,presentationGoal)>.01f)
            {
                presentationStart=instrumentCamera.transform.localRotation;
                presentationGoal=target;presentationElapsed=0;
            }
            // SyncInstruments can run more than once per Update during a handoff.
            if(presentationFrame!=Time.frameCount)
            {presentationElapsed=Mathf.Min(.2f,presentationElapsed+Mathf.Max(0,Time.deltaTime));presentationFrame=Time.frameCount;}
            float u=Mathf.SmoothStep(0,1,presentationElapsed/.2f);
            instrumentCamera.transform.localRotation=Quaternion.Slerp(presentationStart,presentationGoal,u);
        }
        Transform HeldInstrumentTarget(bool left)
        {
            if(photoMode==PhotographyMode.Meter && left && instrumentMeter!=null && instrumentMeter.grip!=null)
            {
                if(meterHandContact==null)
                {
                    meterHandContact=new GameObject("Meter hand contact").transform;
                    meterHandContact.SetParent(instrumentMeter.grip,false);
                }
                meterHandContact.position=instrumentMeter.grip.position;
                meterHandContact.rotation=view.transform.rotation;
                return meterHandContact;
            }
            if(instrumentCamera==null || !bodyRoot.gameObject.activeInHierarchy)return null;
            if(photoMode!=PhotographyMode.Body && photoMode!=PhotographyMode.Aperture && photoMode!=PhotographyMode.Shutter)return null;
            if(CameraRaising && cameraRaiseElapsed>=CameraRaiseSeconds*.6f)return null;
            if(left && photoMode==PhotographyMode.Aperture)return ApertureHandContact();
            if(!left && photoMode==PhotographyMode.Shutter)return instrumentCamera.dial;
            return left?instrumentCamera.support:instrumentCamera.grip;
        }
        Transform ApertureHandContact()
        {
            if(apertureHandContact==null)
            {
                // The ring origin is the lens axis, occluded behind the camera back.
                // Bring the wrist just outside the visible left edge; fingers wrap forward
                // toward the ring without driving the forearm through the back plate.
                apertureHandContact=new GameObject("Aperture visible side hand contact").transform;
                apertureHandContact.SetParent(instrumentCamera.transform,false);
                apertureHandContact.gameObject.layer=FirstPersonHands.HandLayer;
                apertureHandContact.localPosition=new Vector3(-.082f,.023f,-.025f);
            }
            // Keep hand orientation independent of aperture detent rotation.
            apertureHandContact.rotation=instrumentCamera.support.rotation;
            return apertureHandContact;
        }
        bool StepInstrumentControl(int direction)
        {
            if(photoMode==PhotographyMode.Meter)
            {if(MeterLockedToFilm)return false;int next=Mathf.Clamp(meterIsoIndex+direction,0,filmIsos.Length-1);bool changed=next!=meterIsoIndex;meterIsoIndex=next;return changed;}
            if(photoMode==PhotographyMode.Aperture)
            {int next=Mathf.Clamp(apertureIndex+direction,0,apertures.Length-1);bool changed=next!=apertureIndex;apertureIndex=next;return changed;}
            if(photoMode==PhotographyMode.Shutter)
            {int next=Mathf.Clamp(speedIndex+direction,0,speeds.Length-1);bool changed=next!=speedIndex;speedIndex=next;return changed;}
            return false;
        }
        void SyncMeterAfterLoadingStep(int step,int iso)
        {
            if(step==2)meterIsoIndex=Mathf.Max(0,Array.IndexOf(filmIsos,iso));
        }
        void PlayInstrumentClick()
        {
            if(!CanPlayPhotographySfx || PlayPhotographyCue(26))return;
            if(instrumentClick==null)
            {
                const int rate=22050;var samples=new float[441];
                // A short, damped mechanical tick, distinct from the exposure-ready tone.
                uint noise=17;
                for(int i=0;i<samples.Length;i++)
                {noise=noise*1664525u+1013904223u;float t=i/(float)rate;samples[i]=(((noise>>8)/(float)0xffffff)*2-1)*Mathf.Exp(-t*290)*.45f+Mathf.Sin(t*9800)*Mathf.Exp(-t*430)*.18f;}
                instrumentClick=AudioClip.Create("Camera mechanical detent",samples.Length,1,rate,false);instrumentClick.SetData(samples,0);
            }
            if(instrumentClickSource==null)
            {instrumentClickSource=gameObject.AddComponent<AudioSource>();instrumentClickSource.playOnAwake=false;instrumentClickSource.spatialBlend=0;}
            instrumentClickSource.PlayOneShot(instrumentClick,.24f);
        }
        void DisposeInstrumentClick()
        {
            if(instrumentClickSource!=null)Destroy(instrumentClickSource);
            if(instrumentClick!=null)Destroy(instrumentClick);
        }
    }
}
