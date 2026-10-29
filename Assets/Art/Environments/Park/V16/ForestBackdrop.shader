Shader "Park/Forest Backdrop Cutout Fog"
{
    Properties
    {
        _MainTex("Forest canopy RGBA", 2D) = "white" {}
        _Color("Distance tint", Color) = (1,1,1,1)
        _Cutoff("Canopy alpha cutoff", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Cutoff;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v)
            {
                v2f o;
                o.position=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                UNITY_TRANSFER_FOG(o,o.position);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                clip(c.a-_Cutoff);
                c.rgb*=_Color.rgb;
                c.a=1;
                UNITY_APPLY_FOG(i.fogCoord,c);
                return c;
            }
            ENDCG
        }
    }
}
