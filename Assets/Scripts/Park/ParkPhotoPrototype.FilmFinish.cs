using System;
using System.Collections;
using UnityEngine;
namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype
    {
        bool shutterSampling;
        Coroutine shutterRoutine;
        // The real exposure keeps DOF/exposure but never includes viewfinder optics.
        // Use this for the initial shutter AND every temporal/background sample.
        void RenderPhotographicFrame()
        {
            bool previous=focusEffect!=null && focusEffect.suppressFocusAid;
            try{if(focusEffect!=null)focusEffect.suppressFocusAid=true;view.Render();}
            finally{if(focusEffect!=null)focusEffect.suppressFocusAid=previous;}
        }
        Texture2D ReadExposureFrame(bool hideSubject)
        {
            var oldTarget=view.targetTexture;var oldActive=RenderTexture.active;
            var renderers=companion.GetComponentsInChildren<Renderer>(true);
            var hidden=new bool[renderers.Length];
            RenderTexture target=null;Texture2D image=null;
            try
            {
                if(hideSubject)for(int i=0;i<renderers.Length;i++){hidden[i]=renderers[i].forceRenderingOff;renderers[i].forceRenderingOff=true;}
                target=RenderTexture.GetTemporary(photoWidth,Mathf.Max(1,Mathf.RoundToInt(photoWidth/view.aspect)),24,RenderTextureFormat.ARGB32);
                view.targetTexture=target;RenderPhotographicFrame();RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image;
            }
            catch{if(image!=null)Destroy(image);throw;}
            finally
            {
                if(hideSubject)for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].forceRenderingOff=hidden[i];
                view.targetTexture=oldTarget;RenderTexture.active=oldActive;if(target!=null)RenderTexture.ReleaseTemporary(target);
            }
        }
        Texture2D TryReadExposureFrame(bool hideSubject)
        {
            try{return ReadExposureFrame(hideSubject);}
            catch(Exception ex){Debug.LogWarning("Bridge exposure sampling unavailable; retaining captured frame: "+ex.Message);return null;}
        }
        void BeginBridgeExposure(ParkPhotoRecord record,Texture2D first)
        {
            shutterSampling=true;shutterRoutine=StartCoroutine(SampleBridgeExposure(record,first));
        }
        IEnumerator SampleBridgeExposure(ParkPhotoRecord record,Texture2D first)
        {
            Texture2D backgroundImage=null,sample=null;
            try
            {
                // Freeze camera/input, not the companion animation. Background remains one fixed exposure.
                backgroundImage=TryReadExposureFrame(true);
                if(backgroundImage==null){shutterSampling=false;QueueFinishedExposure(record,first);yield break;}
                var background=backgroundImage.GetPixels();var sum=new Color[background.Length];var touched=new bool[background.Length];
                ParkFilmFinish.AccumulateSubject(sum,first.GetPixels(),background,touched);
                float elapsed=0,last=0;int count=1;
                float duration=Mathf.Clamp(record.shutterSeconds,0,.25f);
                while(elapsed<duration && count<6)
                {
                    yield return null;
                    if(GamePauseMenu.BlocksGameplay || Paused || !focused || applicationPaused)continue;
                    companion.Paused=false;
                    elapsed+=Time.deltaTime;
                    if(elapsed-last<duration/5 && elapsed<duration)continue;
                    last=elapsed;sample=TryReadExposureFrame(false);
                    if(sample==null)break;
                    ParkFilmFinish.AccumulateSubject(sum,sample.GetPixels(),background,touched);count++;
                    Destroy(sample);sample=null;
                }
                first.SetPixels(ParkFilmFinish.Resolve(sum,background,touched,count));first.Apply();
                shutterSampling=false;
                QueueFinishedExposure(record,first);
            }
            finally
            {
                shutterSampling=false;shutterRoutine=null;
                if(first!=null)Destroy(first);if(sample!=null)Destroy(sample);if(backgroundImage!=null)Destroy(backgroundImage);
            }
        }
        void QueueFinishedExposure(ParkPhotoRecord record,Texture2D image)
        {
            try
            {
                var rgb=image.GetRawTextureData<byte>().ToArray();
                ParkFilmFinish.Grain(rgb,record.iso,record.id);
                BeginExposureEncoding(record,rgb,image.width,image.height);
            }
            catch(Exception ex){Notify("Shot not saved. Frame kept; retry.",6);Debug.LogWarning("Film treatment: "+ex.Message);}
        }
        void CancelShutterSampling()
        {
            if(shutterRoutine!=null)StopCoroutine(shutterRoutine);
            shutterRoutine=null;shutterSampling=false;
        }
    }
}
