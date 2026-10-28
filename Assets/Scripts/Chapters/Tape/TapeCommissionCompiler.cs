using System;
using System.Linq;
using UnityEngine;
namespace MemoryWhitebox
{
    // The transport receives Samples only. CleanMap and rules are used exclusively at submission.
    public sealed class TapeCommissionAudio
    {
        public float[] Samples;
        public int[] CleanMap;
        public int CleanCount,Rate;
        public TapeAcceptanceRules Rules;
        public string Reject(int[] recording)
        {
            if(recording==null || recording.Length==0)return "No recording. Load A and a prepared B, then click REC.";
            var seen=new bool[CleanCount];int noise=0,silence=0,previous=-1,clean=0;
            bool repeated=false;
            foreach(int source in recording)
            {
                if(source<0 || source>=CleanMap.Length){silence++;continue;}
                int index=CleanMap[source];if(index<0){noise++;continue;}
                if(index<=previous)repeated=true;previous=index;
                if(!seen[index]){seen[index]=true;clean++;}
            }
            if(silence>Rules.allowedSilenceSeconds*Rate)return "Too much blank audio. Play A while recording; stop B during noise.";
            if(noise>Rules.allowedNoiseSeconds*Rate)return "Too much noise. Stop B before noise; resume after it ends.";
            if(repeated)return "Repeated or out-of-order content. Retry this commission.";
            foreach(var sound in Rules.requiredSounds)
            {
                int first=Mathf.RoundToInt(sound.start*Rate),last=Mathf.Min(CleanCount,Mathf.RoundToInt(sound.end*Rate)),count=0;
                for(int i=first;i<last;i++)if(seen[i])count++;
                if(count<(last-first)*sound.minimumCoverage)return "Missing required sound: "+sound.label;
            }
            if(clean<CleanCount*Rules.minimumCleanCoverage)return "Some clean content is missing. Keep the sections on both sides of the noise.";
            return "";
        }
    }
    public static class TapeCommissionCompiler
    {
        public static TapeCommissionAudio Build(TapeCommissionDefinition definition,int rate)
        {
            if(definition==null || definition.sourceAudio==null || definition.acceptance==null || rate<=0)
                throw new InvalidOperationException("Commission source or acceptance configuration is missing.");
            var clip=definition.sourceAudio;clip.LoadAudioData();
            if(clip.loadState!=AudioDataLoadState.Loaded)throw new InvalidOperationException("Source audio is still loading.");
            float seconds=definition.cleanDuration>0?definition.cleanDuration:clip.length;
            if(float.IsNaN(seconds) || seconds<=0 || seconds>clip.length+.001f || seconds>120)throw new InvalidOperationException("Invalid clean audio length.");
            int cleanCount=Mathf.RoundToInt(seconds*rate);
            var noise=(definition.noise??new TapeCommissionDefinition.NoiseInsertion[0]).OrderBy(n=>n.atCleanSecond).ToArray();
            int total=cleanCount;
            foreach(var n in noise)
            {
                if(float.IsNaN(n.atCleanSecond) || float.IsNaN(n.duration) || float.IsNaN(n.amplitude) || n.atCleanSecond<0 || n.atCleanSecond>seconds || n.duration<=0 || n.duration>120 || n.amplitude<0 || n.amplitude>1)
                    throw new InvalidOperationException("Invalid noise insertion.");
                total+=Mathf.RoundToInt(n.duration*rate);
            }
            if(total>rate*120)throw new InvalidOperationException("Source exceeds 120 seconds.");
            var rules=definition.acceptance;
            if(rules.minimumCleanCoverage<=0 || rules.minimumCleanCoverage>1 || float.IsNaN(rules.minimumCleanCoverage) || float.IsNaN(rules.allowedNoiseSeconds) || float.IsInfinity(rules.allowedNoiseSeconds) || float.IsNaN(rules.allowedSilenceSeconds) || float.IsInfinity(rules.allowedSilenceSeconds) || rules.allowedNoiseSeconds<0 || rules.allowedSilenceSeconds<0 || rules.requiredSounds==null)
                throw new InvalidOperationException("Invalid acceptance thresholds.");
            foreach(var sound in rules.requiredSounds)
                if(float.IsNaN(sound.start) || float.IsNaN(sound.end) || sound.start<0 || sound.end<=sound.start || sound.end>seconds || float.IsNaN(sound.minimumCoverage) || sound.minimumCoverage<=0 || sound.minimumCoverage>1 || Mathf.RoundToInt(sound.end*rate)<=Mathf.RoundToInt(sound.start*rate))
                    throw new InvalidOperationException("Invalid required sound range: "+sound.label);
            var pcm=new float[clip.samples*clip.channels];if(!clip.GetData(pcm,0))throw new InvalidOperationException("Source audio cannot be read.");
            var result=new TapeCommissionAudio{Samples=new float[total],CleanMap=new int[total],CleanCount=cleanCount,Rate=rate,Rules=rules};
            var random=new System.Random(definition.noiseSeed);int output=0,insertion=0;
            for(int clean=0;clean<=cleanCount;clean++)
            {
                while(insertion<noise.Length && Mathf.RoundToInt(noise[insertion].atCleanSecond*rate)==clean)
                {
                    var n=noise[insertion++];int count=Mathf.RoundToInt(n.duration*rate);
                    for(int j=0;j<count;j++){result.Samples[output]=(float)(random.NextDouble()*2-1)*n.amplitude;result.CleanMap[output++]=-1;}
                }
                if(clean==cleanCount)break;
                int frame=Mathf.Min(clip.samples-1,(int)((long)clean*clip.frequency/rate));
                for(int channel=0;channel<clip.channels;channel++)result.Samples[output]+=pcm[frame*clip.channels+channel]/clip.channels;
                result.CleanMap[output++]=clean;
            }
            return result;
        }
    }
}
