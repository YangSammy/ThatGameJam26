using UnityEngine;

namespace MemoryWhitebox
{
    // One temporary commission made from the existing clip. Noise is inserted,
    // never laid over story content. Only submission sees the reference ranges.
    public static class TapeCommissionSource
    {
        public static float[] Build(AudioClip clip,int rate)
        {
            if(clip==null)throw new System.InvalidOperationException("Source tape has no audio clip.");
            var pcm=new float[clip.samples*clip.channels];
            if(!clip.GetData(pcm,0))throw new System.InvalidOperationException("Source clip must support PCM GetData.");
            var samples=new float[rate*14];var random=new System.Random(861);
            for(int i=0;i<samples.Length;i++)
            {
                if(i>=rate*6 && i<rate*8){samples[i]=(float)(random.NextDouble()*2-1)*.22f;continue;}
                int clean=i<rate*6?i:i-rate*2;
                int frame=(int)((long)clean*clip.frequency/rate)%clip.samples;
                for(int channel=0;channel<clip.channels;channel++)samples[i]+=pcm[frame*clip.channels+channel]/clip.channels;
            }
            return samples;
        }
        public static bool Accept(int[] map,int rate)
        {
            if(map.Length<rate*11 || map.Length>rate*13)return false;
            var seen=new bool[rate*14];int clean=0,bad=0,previous=-1;
            foreach(int sample in map)
            {
                if(sample<0 || sample>=seen.Length || (sample>=rate*6 && sample<rate*8)){bad++;continue;}
                if(sample<=previous)return false;previous=sample;
                if(!seen[sample]){seen[sample]=true;clean++;}
            }
            return clean>=rate*11.4f && bad<=rate*.25f;
        }
    }
}
