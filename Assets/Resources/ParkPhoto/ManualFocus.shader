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
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _MainTex_TexelSize;
            float _FocusDistance, _SharpRange, _BlurTransition, _MaxBlurPixels, _ExposureGain;
            float4 frag(v2f_img i):SV_Target
            {
                float2 depthUV=i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0)depthUV.y=1-depthUV.y;
                #endif
                float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,depthUV));
                float blur=smoothstep(_SharpRange,_SharpRange+_BlurTransition,abs(depth-_FocusDistance));
                // Keep nearby hands readable; do not exempt them from focus entirely.
                if(depth<_FocusDistance)blur*=.65;
                float2 radius=float2(abs(_MainTex_TexelSize.x/_MainTex_TexelSize.y),1)*blur*_MaxBlurPixels/1080;
                float4 sum=tex2D(_MainTex,i.uv)*.2;
                sum+=tex2D(_MainTex,i.uv+radius*float2(.45,0))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-.45,0))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,.45))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,-.45))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(1,0))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-1,0))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,1))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,-1))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(.707,.707))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-.707,.707))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(.707,-.707))*.05;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-.707,-.707))*.05;
                float3 color=max(0,sum.rgb);
                #if defined(UNITY_COLORSPACE_GAMMA)
                color=GammaToLinearSpace(color);
                #endif
                // Neutral is identity; positive exposure rolls highlights off smoothly.
                color=color*_ExposureGain/(1+color*max(0,_ExposureGain-1));
                #if defined(UNITY_COLORSPACE_GAMMA)
                color=LinearToGammaSpace(color);
                #endif
                sum.rgb=color;
                return sum;
            }
            ENDCG
        }
    }
}
