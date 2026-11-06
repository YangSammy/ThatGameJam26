Shader "Hidden/ParkPhoto/ManualFocus"
{
    Properties
    {
        _MainTex("Image",2D)="white"{}
        _ExposureGain("Exposure gain",Float)=1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _OriginalTex;
        float4 _MainTex_TexelSize, _OriginalTex_TexelSize;
        float _FocusBlur, _MaxBlurPixels;
        float _ExposureGain, _RaiseOcclusion, _DetailBackdrop;
        float4 Gaussian(float2 uv,float2 axis)
        {
            // One subject-based blur for the entire image, including sky and
            // transparent surfaces. No depth-edge masks or split-image optics.
            float radius=(_DetailBackdrop>.5?1:saturate(_FocusBlur))*_MaxBlurPixels/1080;
            float2 stepUV=axis*radius/12;
            float4 sum=0;float total=0;
            // Dense contiguous taps, three-sigma radius. No distant copies.
            [unroll] for(int tap=-12;tap<=12;tap++)
            {
                float2 sampleUV=saturate(uv+stepUV*tap);
                float weight=exp(-.5*(tap*tap)/16.0);
                sum+=tex2Dlod(_MainTex,float4(sampleUV,0,0))*weight;total+=weight;
            }
            return sum/max(total,.00001);
        }
        float4 Horizontal(v2f_img i):SV_Target
        {
            return Gaussian(i.uv,float2(abs(_MainTex_TexelSize.x/_MainTex_TexelSize.y),0));
        }
        float4 Vertical(v2f_img i):SV_Target {return Gaussian(i.uv,float2(0,1));}
        float3 Expose(float3 color)
        {
            color=max(0,color);
            #if defined(UNITY_COLORSPACE_GAMMA)
            color=GammaToLinearSpace(color);
            #endif
            color=color*_ExposureGain/(1+color*max(0,_ExposureGain-1));
            #if defined(UNITY_COLORSPACE_GAMMA)
            color=LinearToGammaSpace(color);
            #endif
            return color;
        }
        float4 Composite(v2f_img i):SV_Target
        {
            float4 result=tex2D(_MainTex,i.uv);
            result.rgb=Expose(result.rgb);
            result.rgb*=1-saturate(_RaiseOcclusion);
            return result;
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Horizontal
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Vertical
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
}
