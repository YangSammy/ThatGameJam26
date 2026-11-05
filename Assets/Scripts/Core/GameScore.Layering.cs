using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class GameScore
    {
        GameScoreLayers layerSettings;
        GameScoreLayers.Arrangement arrangement;
        AudioSource[] layerVoices;
        double scheduledStart;
        bool restartScheduled;

        void InitializeLayers()
        {
            layerSettings=Resources.Load<GameScoreLayers>("GameScoreLayers");
            int count=0;
            if(layerSettings!=null && layerSettings.arrangements!=null)
                foreach(var item in layerSettings.arrangements)
                    if(item!=null && item.layers!=null)count=Mathf.Max(count,item.layers.Length);
            layerVoices=new AudioSource[count];
            for(int i=0;i<count;i++)layerVoices[i]=Voice("Supplemental score layer "+i);
        }
        void ScheduleArrangement(AudioClip clip,bool loop)
        {
            StopLayers();
            arrangement=layerSettings!=null?layerSettings.Find(clip):null;
            scheduledStart=AudioSettings.dspTime+.15;
            if(clip==null)return;
            beds[active].PlayScheduled(scheduledStart);
            if(arrangement==null || arrangement.layers==null)return;
            for(int i=0;i<arrangement.layers.Length;i++)
            {
                var layer=arrangement.layers[i];var voice=layerVoices[i];
                if(layer==null || layer.clip==null)continue;
                // Distance-driven ward has no fixed duration; supplements sustain until its live cut.
                voice.clip=layer.clip;voice.loop=loop || clip==score.ward;voice.volume=0;
                // Silent layers run from the same sample origin; gain changes never restart them.
                voice.PlayScheduled(scheduledStart);
            }
        }
        void StopLayers()
        {
            if(layerVoices!=null)foreach(var voice in layerVoices)
                if(voice!=null){voice.Stop();voice.clip=null;voice.volume=0;}
            arrangement=null;
        }
        void PauseLayers(bool value)
        {
            if(layerVoices==null)return;
            if(value && AudioSettings.dspTime<scheduledStart && beds[active].clip!=null)
            {
                restartScheduled=true;beds[active].Stop();
                foreach(var voice in layerVoices)voice.Stop();
                return;
            }
            if(!value && restartScheduled)
            {
                restartScheduled=false;
                ScheduleArrangement(beds[active].clip,beds[active].loop);
                return;
            }
            foreach(var voice in layerVoices){if(value)voice.Pause();else voice.UnPause();}
        }
        public static float AutumnLayerProgress(WritingChapter chapter)
        {
            return chapter!=null && chapter.Phase=="collect" && chapter.Started
                ?Mathf.Clamp01((15-chapter.Remaining)/15):0;
        }
        public static float LayerWeight(float progress,float enterAt)
        {
            return enterAt<=0?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(enterAt,Mathf.Min(1,enterAt+.18f),progress));
        }
        void UpdateLayers(float dt)
        {
            if(arrangement==null)return;
            float progress=0;
            if(house!=null && house.Current is WritingChapter writing)progress=AutumnLayerProgress(writing);
            else if(house!=null && house.Current is PaintingChapter painting)
                progress=painting.Phase==3?painting.WardProgress:painting.BookOpen?.3f:.1f;
            for(int i=0;i<arrangement.layers.Length;i++)
            {
                var layer=arrangement.layers[i];if(layer==null)continue;
                var voice=layerVoices[i];
                float master=layer.environment?layerSettings.environmentGain:score.gain*gainScale;
                float target=master*layer.gain*LayerWeight(progress,layer.enterAt);
                voice.volume=master<=0?0:Mathf.MoveTowards(voice.volume,target,dt*master/.35f);
            }
        }
        void StopWardVoices()
        {
            bool wardActive=score!=null && arrangement!=null && arrangement.sourceMix==score.ward;
            if(score!=null && beds!=null)foreach(var bed in beds)
                if(bed!=null && bed.clip==score.ward){wardActive=true;bed.Stop();bed.clip=null;bed.volume=0;}
            if(wardActive){restartScheduled=false;StopLayers();}
        }
    }
}
