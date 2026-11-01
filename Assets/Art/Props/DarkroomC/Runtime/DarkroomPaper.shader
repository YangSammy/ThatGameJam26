Shader "ThatGamejam/Art/DarkroomPaper"
{
    Properties
    {
        _MainTex ("Runtime photograph", 2D) = "white" {}
        _PaperTex ("Paper fibre", 2D) = "white" {}
        _BumpMap ("Paper normal", 2D) = "bump" {}
        _Reveal ("Development", Range(0,1)) = 0
        _Wetness ("Wetness", Range(0,1)) = 0
        _PaperColor ("Paper tint", Color) = (1,1,1,1)
        _GuideTint ("Interaction tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _PaperTex, _BumpMap;
        half _Reveal, _Wetness;
        fixed4 _PaperColor, _GuideTint;
        struct Input { float2 uv_MainTex; float2 uv_PaperTex; float2 uv_BumpMap; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 paper=tex2D(_PaperTex, IN.uv_PaperTex).rgb * _PaperColor.rgb;
            fixed3 picture=tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Albedo=lerp(paper,picture,smoothstep(0,1,saturate(_Reveal)))*_GuideTint.rgb;
            o.Normal=UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            o.Metallic=0; o.Smoothness=lerp(.25,.72,saturate(_Wetness));o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
