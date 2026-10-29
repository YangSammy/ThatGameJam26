Shader "Memory/ContactPrintReveal"
{
    Properties
    {
        _MainTex ("Contact sheet", 2D) = "white" {}
        _Reveal ("Development", Range(0,1)) = 0
        _PaperColor ("Paper", Color) = (0.93,0.91,0.84,1)
        _GuideTint ("Interaction tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;float4 _MainTex_ST;float _Reveal;fixed4 _PaperColor;fixed4 _GuideTint;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
            fixed4 frag(v2f i):SV_Target{return lerp(_PaperColor,tex2D(_MainTex,i.uv),smoothstep(0,1,_Reveal))*_GuideTint;}
            ENDCG
        }
    }
}
