Shader "QuietRoom/Double sided wool"
{
    Properties
    {
        _Color("Wool tint",Color)=(.25,.31,.29,1)
        _MainTex("Woven wool",2D)="white"{}
        _BumpMap("Fibre normal",2D)="bump"{}
        _BumpScale("Fibre strength",Range(0,2))=.24
        _MetallicGlossMap("Smoothness",2D)="white"{}
        _GlossMapScale("Smoothness scale",Range(0,1))=.3
        _Glossiness("Smoothness",Range(0,1))=.15
        _Metallic("Metallic",Range(0,1))=0
        _OcclusionMap("Fibre occlusion",2D)="white"{}
        _OcclusionStrength("Occlusion",Range(0,1))=.72
        _Transmission("Fibre back scatter",Range(0,1))=.14
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 300
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _METALLICGLOSSMAP
        #include "UnityCG.cginc"
        #include "UnityStandardUtils.cginc"
        #include "Lighting.cginc"
        sampler2D _MainTex,_BumpMap,_MetallicGlossMap,_OcclusionMap;
        half4 _Color;half _BumpScale,_GlossMapScale,_Glossiness,_Metallic,_Transmission,_OcclusionStrength;
        struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 worldNormal;float facing:VFACE;INTERNAL_DATA};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            half3 fibre=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
            o.Albedo=fibre;o.Alpha=1;o.Metallic=0;o.Smoothness=_Glossiness;
            o.Occlusion=lerp(1,tex2D(_OcclusionMap,IN.uv_MainTex).g,_OcclusionStrength);
            #if defined(_NORMALMAP)
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
            #endif
            o.Normal*=IN.facing>0?1:-1;
            #if defined(_METALLICGLOSSMAP)
            o.Smoothness=tex2D(_MetallicGlossMap,IN.uv_MainTex).a*_GlossMapScale;
            #endif
            float3 n=WorldNormalVector(IN,o.Normal);n*=rsqrt(max(dot(n,n),.0001));
            float3 lightDir=_WorldSpaceLightPos0.xyz;lightDir*=rsqrt(max(dot(lightDir,lightDir),.0001));
            o.Emission=fibre*_LightColor0.rgb*_Transmission*saturate(dot(-n,lightDir));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
