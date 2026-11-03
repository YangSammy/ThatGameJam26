Shader "QuietRoom/Thin linen transmission"
{
    Properties
    {
        _Color("Linen tint",Color)=(1,1,1,.78)
        _MainTex("Woven linen",2D)="white"{}
        _BumpMap("Fibre normal",2D)="bump"{}
        _BumpScale("Fibre strength",Range(0,2))=.24
        _MetallicGlossMap("Smoothness",2D)="white"{}
        _GlossMapScale("Smoothness scale",Range(0,1))=.3
        _Glossiness("Smoothness",Range(0,1))=.18
        _Metallic("Metallic",Range(0,1))=0
        _OcclusionMap("Fibre occlusion",2D)="white"{}
        _OcclusionStrength("Occlusion",Range(0,1))=.72
        _Transmission("Backlit fibre transmission",Range(0,1))=.10
        _DirectSpecularScale("Direct dielectric specular scale",Range(0,1))=1
        _IndirectDiffuseScale("Local baked diffuse response",Range(0,1))=1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 300
        Cull Off
        CGPROGRAM
        #pragma surface surf Linen fullforwardshadows alpha:premul exclude_path:deferred
        #pragma target 3.0
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _METALLICGLOSSMAP
        #include "UnityCG.cginc"
        #include "UnityStandardUtils.cginc"
        #include "UnityPBSLighting.cginc"
        sampler2D _MainTex,_BumpMap,_MetallicGlossMap,_OcclusionMap;
        half4 _Color;half _BumpScale,_GlossMapScale,_Glossiness,_Metallic,_Transmission,_OcclusionStrength,_DirectSpecularScale,_IndirectDiffuseScale;
        struct Input {float2 uv_MainTex;float2 uv_BumpMap;float facing:VFACE;};
        inline half4 LightingLinen(SurfaceOutputStandard s,float3 viewDir,UnityGI gi)
        {
            half3 fibre=s.Albedo;
            s.Albedo*=1-_Transmission;
            // Appearance calibration for the single baked aperture probe on thin
            // cloth. Direct light, opacity and indirect specular remain unchanged.
            // This is not a new GI solver or an illuminance correction.
            gi.indirect.diffuse*=_IndirectDiffuseScale;
            SurfaceOutputStandardSpecular linen;
            UNITY_INITIALIZE_OUTPUT(SurfaceOutputStandardSpecular,linen);
            linen.Albedo=s.Albedo;linen.Specular=.04*_DirectSpecularScale;linen.Normal=s.Normal;
            linen.Emission=s.Emission;linen.Smoothness=s.Smoothness;linen.Occlusion=s.Occlusion;linen.Alpha=s.Alpha;
            UnityGI directGI=gi;directGI.indirect.diffuse=0;directGI.indirect.specular=0;
            UnityGI indirectGI=gi;indirectGI.light.color=0;
            half4 lit=LightingStandard(s,viewDir,gi);
            if(_DirectSpecularScale<.999h)
                lit.rgb=LightingStandardSpecular(linen,viewDir,directGI).rgb+LightingStandard(s,viewDir,indirectGI).rgb;
            // UnityGI supplies the light's realtime/mixed shadow attenuation.
            // The transmitted diffuse term also obeys premultiplied opacity.
            half back=saturate(dot(-normalize(s.Normal),gi.light.dir));
            lit.rgb+=fibre*gi.light.color*(_Transmission*back*s.Alpha);
            return lit;
        }
        inline void LightingLinen_GI(SurfaceOutputStandard s,UnityGIInput data,inout UnityGI gi)
        {
            LightingStandard_GI(s,data,gi);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            half4 fibre=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            o.Albedo=fibre.rgb;o.Alpha=fibre.a;o.Metallic=0;o.Smoothness=_Glossiness;
            o.Occlusion=lerp(1,tex2D(_OcclusionMap,IN.uv_MainTex).g,_OcclusionStrength);
            o.Normal=half3(0,0,1);
            #if defined(_NORMALMAP)
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
            #endif
            o.Normal*=IN.facing>=0?1:-1;
            #if defined(_METALLICGLOSSMAP)
            o.Smoothness=tex2D(_MetallicGlossMap,IN.uv_MainTex).a*_GlossMapScale;
            #endif
            o.Emission=0;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
