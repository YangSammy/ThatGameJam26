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
        public static float ExposureError(float aperture,float seconds,int iso,float ev100)=>
            Mathf.Log(aperture*aperture/seconds,2)-Mathf.Log(iso/100f,2)-ev100;
        public static float ExposureGain(float error)=>Mathf.Pow(2,Mathf.Clamp(-error,-8,8));
        public static float ApertureScale(float aperture)=>Mathf.Pow(Mathf.Clamp(aperture,2.8f,16)/8,.85f);
        public static float ClearRange(float focus,float aperture)=>Mathf.Max(.09f,Mathf.Max(.2f,focus*.065f)*ApertureScale(aperture));
        public static bool IsSharp(float depth,float focus,float aperture)=>depth>0 && Mathf.Abs(depth-focus)<=ClearRange(focus,aperture);
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
        [System.NonSerialized] public float aperture=8,exposureError;
        Material effect;
        void OnEnable(){GetComponent<Camera>().depthTextureMode|=DepthTextureMode.Depth;if(template!=null)effect=new Material(template);}
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if((!activeFocus && raiseOcclusion<=0) || effect==null){Graphics.Blit(source,destination);return;}
            float focus=Mathf.Max(.1f,distance);
            effect.SetFloat("_FocusDistance",focus);
            effect.SetFloat("_SharpRange",ParkCameraResponse.ClearRange(focus,aperture));
            effect.SetFloat("_BlurTransition",Mathf.Max(.18f,focus*.15f)*ParkCameraResponse.ApertureScale(aperture));
            // Radius is authored at 1080 px height, so preview and captured film agree.
            effect.SetFloat("_MaxBlurPixels",activeFocus?Mathf.Lerp(12,4,Mathf.InverseLerp(2.8f,16,aperture)):0);
            effect.SetFloat("_ExposureGain",activeFocus?ParkCameraResponse.ExposureGain(exposureError):1);
            effect.SetFloat("_RaiseOcclusion",Mathf.Clamp01(raiseOcclusion));
            Graphics.Blit(source,destination,effect);
        }
        void OnDisable(){if(effect!=null)Destroy(effect);effect=null;}
    }
}
