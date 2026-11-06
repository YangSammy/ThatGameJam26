using System;
using UnityEngine;
namespace MemoryWhitebox
{
    // Output-only treatment. Never changes exposure eligibility or stored settings.
    public static class ParkFilmFinish
    {
        public static void Grain(byte[] rgb,int iso,string exposureId)
        {
            uint seed=2166136261;foreach(char c in exposureId??"")seed=(seed^c)*16777619;
            float strength=iso>=800?7:iso>=400?4.5f:iso>=200?2.8f:1.6f;
            for(int i=0;i<rgb.Length;i+=3)
            {
                seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;
                float noise=((seed&65535)/65535f-.5f)*2;
                float luminance=(rgb[i]*.2126f+rgb[i+1]*.7152f+rgb[i+2]*.0722f)/255f;
                float amount=noise*strength*(.6f+.4f*(1-luminance));
                for(int c=0;c<3;c++)rgb[i+c]=(byte)Mathf.Clamp(Mathf.RoundToInt(rgb[i+c]+amount),0,255);
            }
        }
        public static void AccumulateSubject(Color[] sum,Color[] sample,Color[] background,bool[] touched)
        {
            for(int i=0;i<sum.Length;i++)
            {
                var a=sample[i];var b=background[i];
                bool subject=Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Max(Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b)))>.025f;
                touched[i]|=subject;
                sum[i]+=(subject?a:b).linear;
            }
        }
        public static Color[] Resolve(Color[] sum,Color[] background,bool[] touched,int count)
        {
            var result=new Color[sum.Length];
            for(int i=0;i<result.Length;i++)result[i]=touched[i]?(sum[i]/count).gamma:background[i];
            return result;
        }
    }
}
