using UnityEngine;
namespace MemoryWhitebox
{
    // Shared by rendering and task acceptance. Gameplay DOF, not a thin-lens solver.
    public static class ParkCameraResponse
    {
        internal static readonly float[] ShutterSpeeds={1f/4,1f/8,1f/15,1f/30,1f/60,1f/125,1f/250,1f/500,1f/1000};
        // Meter recommendations must be selectable on the same mechanical dial.
        public static float SuggestedShutter(float aperture,int iso,float ev100)
        {
            float result=ShutterSpeeds[0],best=float.MaxValue;
            foreach(float seconds in ShutterSpeeds)
            {
                float error=Mathf.Abs(ExposureError(aperture,seconds,iso,ev100));
                if(error<best){best=error;result=seconds;}
            }
            return result;
        }
        // Advice only: keep the chosen aperture unless the shutter dial cannot expose it.
        public static float SuggestedAperture(float preferred,int iso,float ev100)
        {
            float speed=SuggestedShutter(preferred,iso,ev100);
            if(Mathf.Abs(ExposureError(preferred,speed,iso,ev100))<=.55f)return preferred;
            float best=preferred,bestDistance=float.MaxValue;
            foreach(float aperture in new[]{2.8f,4,5.6f,8,11,16})
            {
                float error=Mathf.Abs(ExposureError(aperture,SuggestedShutter(aperture,iso,ev100),iso,ev100));
                float distance=Mathf.Abs(Mathf.Log(aperture/preferred,2));
                if(error<=.55f && distance<bestDistance){best=aperture;bestDistance=distance;}
            }
            return best;
        }
        public static float ExposureError(float aperture,float seconds,int iso,float ev100)=>
            Mathf.Log(aperture*aperture/seconds,2)-Mathf.Log(iso/100f,2)-ev100;
        public static float ExposureGain(float error)=>Mathf.Pow(2,Mathf.Clamp(-error,-8,8));
        public static float ApertureScale(float aperture)=>Mathf.Pow(Mathf.Clamp(aperture,2.8f,16)/8,.85f);
        public static float ClearRange(float focus,float aperture)=>Mathf.Max(.09f,Mathf.Max(.2f,focus*.065f)*ApertureScale(aperture));
        public static bool IsSharp(float depth,float focus,float aperture)=>depth>0 && Mathf.Abs(depth-focus)<=ClearRange(focus,aperture);
        public static float FocusBlur(float depth,float focus,float aperture)
        {
            if(depth<=0)return 1;
            float excess=Mathf.Abs(depth-focus)-ClearRange(focus,aperture);
            float transition=Mathf.Max(.35f,depth*.18f)*ApertureScale(aperture);
            return Mathf.SmoothStep(0,1,Mathf.Clamp01(excess/transition));
        }
        public static float ScrollFocus(float focus,float wheel,float nominalStep,float minimum,float maximum)=>
            Mathf.Clamp(focus*Mathf.Exp(Mathf.Clamp(wheel*nominalStep/4,-4,4)),minimum,Mathf.Max(minimum,maximum));
    }

    [RequireComponent(typeof(Camera))]
    public sealed class ParkPhotoFocus : MonoBehaviour
    {
        public Material template;
        [HideInInspector] public bool activeFocus;
        [System.NonSerialized] public float raiseOcclusion;
        [HideInInspector] public float distance=4;
        [System.NonSerialized] public float subjectDepth=4;
        [System.NonSerialized] public float aperture=8,exposureError;
        [System.NonSerialized] public bool showFocusAid, suppressFocusAid;
        [System.NonSerialized] public bool detailBackdrop;
        Material effect;
        void OnEnable(){EnsureMaterial();}
        void EnsureMaterial(){if(effect==null && template!=null)effect=new Material(template){hideFlags=HideFlags.HideAndDontSave};}
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            // Also recover when the material reference is assigned after AddComponent or a reload.
            EnsureMaterial();
            if((!activeFocus && raiseOcclusion<=0) || effect==null){Graphics.Blit(source,destination);return;}
            float focus=Mathf.Max(.1f,distance);
            effect.SetFloat("_FocusBlur",ParkCameraResponse.FocusBlur(subjectDepth,focus,aperture));
            // Radius is authored at 1080 px height, so preview and captured film agree.
            bool backdrop=detailBackdrop && !suppressFocusAid;
            effect.SetFloat("_DetailBackdrop",backdrop?1:0);
            effect.SetFloat("_MaxBlurPixels",backdrop?28:activeFocus?28:0);
            effect.SetFloat("_ExposureGain",activeFocus?ParkCameraResponse.ExposureGain(exposureError):1);
            effect.SetFloat("_RaiseOcclusion",Mathf.Clamp01(raiseOcclusion));
            effect.SetTexture("_OriginalTex",source);
            // Two dense separable passes avoid the multiple distinct silhouettes of sparse taps.
            // Preserve source precision and resolve MSAA before sampling. Pooled buffers live for this frame only.
            var descriptor=source.descriptor;descriptor.depthBufferBits=0;descriptor.msaaSamples=1;
            descriptor.useMipMap=false;descriptor.autoGenerateMips=false;
            RenderTexture horizontal=null,vertical=null;
            try
            {
                horizontal=RenderTexture.GetTemporary(descriptor);vertical=RenderTexture.GetTemporary(descriptor);
                horizontal.filterMode=vertical.filterMode=FilterMode.Bilinear;
                horizontal.wrapMode=vertical.wrapMode=TextureWrapMode.Clamp;
                Graphics.Blit(source,horizontal,effect,0);
                Graphics.Blit(horizontal,vertical,effect,1);
                Graphics.Blit(vertical,destination,effect,2);
            }
            finally
            {
                effect.SetTexture("_OriginalTex",null);
                if(horizontal!=null)RenderTexture.ReleaseTemporary(horizontal);
                if(vertical!=null)RenderTexture.ReleaseTemporary(vertical);
            }
        }
        void OnDisable(){if(effect!=null){if(Application.isPlaying)Destroy(effect);else DestroyImmediate(effect);}effect=null;showFocusAid=false;suppressFocusAid=false;}
    }
}
