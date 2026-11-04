Shader "QuietRoom/Double sided leaf"
{
    Properties
    {
        _Color("Leaf tint",Color)=(1,1,1,1)
        _MainTex("Leaf color and optional alpha",2D)="white"{}
        _AlphaMap("Independent leaf opacity",2D)="white"{}
        _AlphaSource("Opacity: 0 solid, 1 color alpha, 2 independent",Float)=0
        _AlphaChannel("Independent opacity channel",Vector)=(1,0,0,0)
        _Cutoff("Leaf edge cutoff",Range(0,1))=.45
        _Cull("Face culling",Float)=0
        _BumpMap("Leaf veins normal",2D)="bump"{}
        _BumpScale("Vein strength",Range(0,2))=.35
        _MetallicGlossMap("Metallic and smoothness",2D)="white"{}
        _GlossMapScale("Smoothness scale",Range(0,1))=1
        _Glossiness("Smoothness",Range(0,1))=.22
        _Metallic("Metallic",Range(0,1))=0
        _OcclusionMap("Leaf occlusion",2D)="white"{}
        _OcclusionStrength("Occlusion strength",Range(0,1))=.72
        _Transmission("Thin leaf transmission",Range(0,1))=.30
        _DirectSpecularScale("Direct leaf dielectric response",Range(0,1))=1
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True"}
        LOD 300
        Cull [_Cull]
        ZWrite On
        AlphaToMask On
        CGPROGRAM
        #pragma surface surf Leaf fullforwardshadows addshadow alphatest:_Cutoff keepalpha exclude_path:deferred
        #pragma target 3.0
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _METALLICGLOSSMAP
        #pragma multi_compile_local __ _SPECULARHIGHLIGHTS_OFF
        #pragma multi_compile_local __ _GLOSSYREFLECTIONS_OFF
        #include "UnityCG.cginc"
        #include "UnityStandardUtils.cginc"
        #include "UnityPBSLighting.cginc"
        sampler2D _MainTex,_AlphaMap,_BumpMap,_MetallicGlossMap,_OcclusionMap;
        half4 _Color,_AlphaChannel;
        half _AlphaSource,_BumpScale,_GlossMapScale,_Glossiness,_Metallic,_Transmission,_OcclusionStrength,_DirectSpecularScale;
        struct Input {float2 uv_MainTex;float2 uv_AlphaMap;float2 uv_BumpMap;float facing:VFACE;};
        inline half4 LightingLeaf(SurfaceOutputStandard s,float3 viewDir,UnityGI gi)
        {
            // Keep the original indirect response, while controlling the direct
            // dielectric lobe separately. Runtime isolation showed direct GGX,
            // not the shared back-face photograph, caused the silver leaf planes.
            UnityGI indirectGI=gi;indirectGI.light.color=0;
            UnityGI directGI=gi;directGI.indirect.diffuse=0;directGI.indirect.specular=0;
            SurfaceOutputStandardSpecular leaf;
            leaf.Albedo=s.Albedo;leaf.Normal=s.Normal;leaf.Emission=s.Emission;
            leaf.Specular=unity_ColorSpaceDielectricSpec.rgb*_DirectSpecularScale;
            leaf.Smoothness=s.Smoothness;leaf.Occlusion=s.Occlusion;leaf.Alpha=s.Alpha;
            half4 lit=LightingStandardSpecular(leaf,viewDir,directGI)+LightingStandard(s,viewDir,indirectGI);
            lit.a=s.Alpha;
            // gi.light.color already includes realtime/mixed shadow attenuation.
            // Light transmission therefore disappears behind walls and dense branches.
            half back=saturate(dot(-normalize(s.Normal),gi.light.dir));
            lit.rgb+=s.Albedo*gi.light.color*(_Transmission*back);
            return lit;
        }
        inline void LightingLeaf_GI(SurfaceOutputStandard s,UnityGIInput data,inout UnityGI gi)
        {
            LightingStandard_GI(s,data,gi);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            half4 color=tex2D(_MainTex,IN.uv_MainTex);
            half opacity=1;
            if(_AlphaSource>.5)opacity=color.a;
            if(_AlphaSource>1.5)opacity=dot(tex2D(_AlphaMap,IN.uv_AlphaMap),_AlphaChannel);
            o.Albedo=color.rgb*_Color.rgb;o.Alpha=opacity*_Color.a;
            o.Metallic=_Metallic;o.Smoothness=_Glossiness;
            o.Occlusion=lerp(1,tex2D(_OcclusionMap,IN.uv_MainTex).g,_OcclusionStrength);
            o.Normal=half3(0,0,1);
            #if defined(_NORMALMAP)
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
            #endif
            // Reverse the tangent-space normal on the back face, preserving veins.
            o.Normal*=IN.facing>=0?1:-1;
            #if defined(_METALLICGLOSSMAP)
            half4 packed=tex2D(_MetallicGlossMap,IN.uv_MainTex);
            o.Metallic=packed.r;o.Smoothness=packed.a*_GlossMapScale;
            #endif
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
