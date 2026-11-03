Shader "Hidden/QuietRoom/ColorResponse"
{
    Properties { _MainTex("Scene HDR",2D)="white"{} _Exposure("Exposure",Float)=1.15 _Saturation("Saturation",Float)=.98 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Exposure,_Saturation;
            float4 frag(v2f_img i):SV_Target
            {
                float3 radiance=max(0,tex2D(_MainTex,i.uv).rgb*_Exposure);
                float3 display=saturate((radiance*(2.51*radiance+.03))/(radiance*(2.43*radiance+.59)+.14));
                float grey=dot(display,float3(.2126,.7152,.0722));
                return float4(lerp(grey.xxx,display,_Saturation),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
