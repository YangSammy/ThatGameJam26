Shader "MemoryWhitebox/ParkGirlPrivacy"
{
    Properties
    {
        _Color("Tint",Color)=(1,1,1,1)
        _MainTex("Base Color",2D)="white"{}
        _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Normal Strength",Float)=1
        _MetallicGlossMap("Metallic Smoothness",2D)="black"{}
        _GlossMapScale("Smoothness",Range(0,1))=1
        _PrivacyColor("Featureless Shadow",Color)=(.18,.105,.065,1)
    }
    SubShader
    {
        Tags{"RenderType"="Opaque"}
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex,_BumpMap,_MetallicGlossMap;
        fixed4 _Color,_PrivacyColor;half _BumpScale,_GlossMapScale;
        struct Input{float2 uv_MainTex;float4 color:COLOR;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            half mask=saturate(IN.color.a);
            fixed4 base=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            fixed4 metal=tex2D(_MetallicGlossMap,IN.uv_MainTex);
            o.Albedo=base.rgb*(1-mask);
            o.Emission=_PrivacyColor.rgb*mask;
            half3 normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));normal.xy*=_BumpScale;
            o.Normal=normalize(lerp(normal,half3(0,0,1),mask));
            o.Metallic=metal.r*(1-mask);o.Smoothness=metal.a*_GlossMapScale*(1-mask);o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
