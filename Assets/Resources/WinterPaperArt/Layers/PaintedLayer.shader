Shader "MemoryWhitebox/WinterPaintedLayer"
{
    Properties
    {
        _MainTex ("Artwork",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _Reveal ("Bridge reveal",Range(0,1))=1
        _Ghost ("Missing shape",Range(0,1))=0
        _UseReveal ("Reveal enabled",Float)=0
        _Erode ("Edge thinning in pixels",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_ST,_MainTex_TexelSize;fixed4 _Color;float _Reveal,_Ghost,_UseReveal,_Erode;
            struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*_Color;
                float2 e=_MainTex_TexelSize.xy*_Erode;
                c.a=min(c.a,min(min(tex2D(_MainTex,i.uv+float2(e.x,0)).a,tex2D(_MainTex,i.uv-float2(e.x,0)).a),
                    min(tex2D(_MainTex,i.uv+float2(0,e.y)).a,tex2D(_MainTex,i.uv-float2(0,e.y)).a)));
                c.a*=lerp(1,saturate((_Reveal-(i.uv.x-.495)/.38)*180),_UseReveal);
                c.a*=lerp(1,.16+step(.5,frac((i.uv.x+i.uv.y)*90))*.12,_Ghost);
                return c;
            }
            ENDCG
        }
    }
}
