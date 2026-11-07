using UnityEngine;

namespace MemoryWhitebox
{
    // Runs after ParkPhotoFocus. Capture suppression is set by RenderPhotographicFrame
    // for every shutter/background sample, so no lens overlay or temporal history is baked in.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera), typeof(AmplifyBloom.AmplifyBloom))]
    public sealed class ParkCinematicBloom : MonoBehaviour
    {
        public ParkPhotoFocus focus;
        public Light sun;
        [Tooltip("Lens effect fraction with camera lowered. Keep at 0 for natural walking colors.")]
        [Range(0,1)] public float walkingLensBlend;
        [Range(0,3)] public float walkingIntensity=.65f, viewfinderIntensity=1.05f;
        [Range(0,1)] public float temporalWeight=.35f;
        [Tooltip("Maximum lens strengths when aiming toward visible sunlight. Solar visibility fades these automatically.")]
        [Range(0,3)] public float lensDirt=.65f, lensFlare=.4f, lensGlare=.3f;
        [Header("Golden viewfinder at bridge and meadow")]
        public ParkCompanion companion;
        public Material goldenBacklight;
        [Range(0,3)] public float goldenIntensity=1.35f;
        [Min(.05f)] public float goldenFadeSeconds=.45f;
        AmplifyBloom.AmplifyBloom bloom;
        Camera view;
        Material goldenEffect;
        RenderTexture solarVisibility;
        float goldenBlend;
        bool solarHistory;
        int visibilityFrame=-1;
        static readonly int SunPosition=Shader.PropertyToID("_SunPosition");
        static readonly int GoldenParams=Shader.PropertyToID("_GoldenParams");
        static readonly int PreviousVisibility=Shader.PropertyToID("_PreviousVisibility");
        static readonly int SolarVisibility=Shader.PropertyToID("_SolarVisibility");
        static readonly int VisibilityBlend=Shader.PropertyToID("_VisibilityBlend");
        Vector3 previousPosition;
        Quaternion previousRotation;
        float previousFov, lensBlend;
        bool hasHistory;

        void OnEnable()
        {
            bloom=GetComponent<AmplifyBloom.AmplifyBloom>();
            view=GetComponent<Camera>();
            if(focus==null)focus=GetComponent<ParkPhotoFocus>();
            goldenBlend=0;lensBlend=0;
            ResetHistory();
        }
        void LateUpdate()
        {
            float target=focus!=null && focus.activeFocus && !focus.detailBackdrop?1:0;
            lensBlend=Application.isPlaying?Mathf.MoveTowards(lensBlend,target,Time.unscaledDeltaTime*5):target;
            float goldenTarget=target*GoldenSiteWeight();
            goldenBlend=Application.isPlaying?Mathf.MoveTowards(goldenBlend,goldenTarget,
                Time.unscaledDeltaTime/Mathf.Max(.05f,goldenFadeSeconds)):goldenTarget;
        }
        public void ResetHistory()
        {
            hasHistory=false;
            solarHistory=false;visibilityFrame=-1;
            if(bloom!=null)bloom.TemporalFilteringActive=false;
        }
        void OnDisable()
        {
            ResetHistory();goldenBlend=0;
            if(bloom!=null)bloom.LensVisibilityTexture=null;
            if(solarVisibility!=null){solarVisibility.Release();DisposeEffectObject(solarVisibility);solarVisibility=null;}
            if(goldenEffect!=null){DisposeEffectObject(goldenEffect);goldenEffect=null;}
        }
        static void DisposeEffectObject(Object value)
        {if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnRenderImage(RenderTexture source,RenderTexture destination){RenderImage(source,destination);}

        public void RenderImage(RenderTexture source,RenderTexture destination)
        {
            if(bloom==null || !bloom.enabled){Graphics.Blit(source,destination);return;}
            bool capture=focus!=null && focus.suppressFocusAid;
            bool occluded=focus!=null && focus.raiseOcclusion>.001f;
            bool aiming=focus!=null && focus.activeFocus && !focus.detailBackdrop;
            float aimingBlend=Application.isPlaying?lensBlend:aiming?1:0;
            bool detail=focus!=null && focus.detailBackdrop;
            float blend=capture || occluded || detail?0:Mathf.Lerp(walkingLensBlend,1,aimingBlend)*SunViewportWeight();
            bool goldenProfile=goldenBacklight!=null;
            float goldenWeight=capture || occluded || (focus!=null && focus.detailBackdrop)?0:
                (Application.isPlaying?goldenBlend:(aiming?GoldenSiteWeight():0))*SunViewportWeight();
            bool cut=!hasHistory || Vector3.Distance(previousPosition,transform.position)>1 ||
                Quaternion.Angle(previousRotation,transform.rotation)>8 || Mathf.Abs(previousFov-view.fieldOfView)>2;
            if(cut || capture || occluded)ResetHistory();
            // Share the pre-bloom HDR solar visibility with Amplify's lens-only
            // composition. Never attenuate ordinary bloom or the scene itself.
            bool visibilityReady=sun!=null && sun.isActiveAndEnabled && sun.intensity>.001f &&
                goldenProfile && goldenBacklight.shader!=null && goldenBacklight.shader.isSupported;
            if(visibilityReady && (blend>.001f || goldenWeight>.001f))
                PrepareGoldenVisibility(source,cut);
            else if(!capture && !occluded){solarHistory=false;visibilityFrame=-1;}
            // A solar profile without a usable visibility source fails closed.
            bloom.LensVisibilityTexture=sun==null?null:
                (visibilityReady && solarHistory?(Texture)solarVisibility:Texture2D.blackTexture);
            bloom.TemporalFilteringActive=!capture && !occluded;
            bloom.TemporalFilteringValue=temporalWeight;
            float bloomBlend=goldenProfile?goldenWeight:aimingBlend;
            bloom.OverallIntensity=Mathf.Lerp(walkingIntensity,viewfinderIntensity,capture || occluded?0:bloomBlend);
            bloom.ApplyLensDirt=blend>.001f && lensDirt>0;
            bloom.ApplyLensFlare=blend>.001f && lensFlare>0;
            bloom.ApplyLensGlare=blend>.001f && lensGlare>0;
            bloom.LensDirtStrength=lensDirt*blend;
            bloom.LensFlare.OverallIntensity=lensFlare*blend;
            float glareIntensity=lensGlare*blend;
            if(!Mathf.Approximately(bloom.LensGlare.Intensity,glareIntensity))
                bloom.LensGlare.Intensity=glareIntensity;
            RenderTexture illuminated=null;
            try
            {
                if(visibilityReady && goldenWeight>.001f)
                {
                    var descriptor=source.descriptor;descriptor.depthBufferBits=0;descriptor.msaaSamples=1;
                    descriptor.useMipMap=false;descriptor.autoGenerateMips=false;
                    illuminated=RenderTexture.GetTemporary(descriptor);
                    illuminated.filterMode=FilterMode.Bilinear;
                    Graphics.SetRenderTarget(illuminated);bloom.RenderImage(source,illuminated);
                    goldenEffect.SetTexture(SolarVisibility,solarVisibility);
                    goldenEffect.SetVector(GoldenParams,new Vector4(goldenWeight*goldenIntensity,
                        source.width/(float)source.height,.42f,.13f));
                    Graphics.Blit(illuminated,destination,goldenEffect,1);
                }
                else
                {
                    Graphics.SetRenderTarget(destination);bloom.RenderImage(source,destination);
                }
            }
            finally{if(illuminated!=null)RenderTexture.ReleaseTemporary(illuminated);}
            previousPosition=transform.position;previousRotation=transform.rotation;previousFov=view.fieldOfView;
            hasHistory=!capture && !occluded;
        }

        float GoldenSiteWeight()
        {
            if(goldenBacklight==null || sun==null || !sun.isActiveAndEnabled || sun.intensity<=.001f ||
                companion==null || !companion.AtPhotoNode)return 0;
            var node=companion.photoHoldNode;
            if(node==null || (node.photoTask!=ParkPhotoWaypoint.PhotoTask.Bridge &&
                node.photoTask!=ParkPhotoWaypoint.PhotoTask.Meadow))return 0;
            float distance=Vector3.Distance(transform.position,companion.transform.position);
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,9,distance));
        }

        void PrepareGoldenVisibility(RenderTexture source,bool cut)
        {
            if(goldenEffect==null)goldenEffect=new Material(goldenBacklight){hideFlags=HideFlags.HideAndDontSave};
            var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)?
                RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32;
            if(solarVisibility==null)
            {
                solarVisibility=new RenderTexture(1,1,0,format,RenderTextureReadWrite.Linear)
                    {hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                solarVisibility.Create();
                var active=RenderTexture.active;
                try{RenderTexture.active=solarVisibility;GL.Clear(false,true,Color.black);}
                finally{RenderTexture.active=active;}
                solarHistory=false;
            }
            Vector3 position=view.WorldToViewportPoint(transform.position-sun.transform.forward*100);
            float radius=Mathf.Tan(.35f*Mathf.Deg2Rad)/(2*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad));
            goldenEffect.SetVector(SunPosition,new Vector4(position.x,position.y,
                radius*source.height/source.width,radius));
            goldenEffect.SetTexture(PreviousVisibility,solarVisibility);
            // Sample the original HDR image, before bloom/veil. Foreground trees,
            // character silhouettes and background cards naturally occlude the sun.
            float dt=visibilityFrame==Time.frameCount?0:Time.unscaledDeltaTime;
            float blend=!Application.isPlaying || !solarHistory || cut?1:1-Mathf.Exp(-dt*10);
            goldenEffect.SetFloat(VisibilityBlend,blend);
            var current=RenderTexture.GetTemporary(1,1,0,format,RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(source,current,goldenEffect,0);
                Graphics.Blit(current,solarVisibility);
            }
            finally{RenderTexture.ReleaseTemporary(current);}
            visibilityFrame=Time.frameCount;solarHistory=true;
        }

        public float SunViewportWeight()
        {
            if(sun==null)return 1; // Existing non-solar profiles keep their behavior.
            if(!sun.isActiveAndEnabled || sun.intensity<=.001f)return 0;
            Vector3 p=view.WorldToViewportPoint(view.transform.position-sun.transform.forward*100);
            if(p.z<=0)return 0;
            float edge=Mathf.Max(Mathf.Abs(p.x-.5f),Mathf.Abs(p.y-.5f));
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.56f,edge));
        }
    }
}
