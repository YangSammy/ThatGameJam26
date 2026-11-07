Shader "MemoryWhitebox/WinterPaperWash"
{
    Properties { _Color ("Pigment",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos:SV_POSITION;float3 local:TEXCOORD0; };
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float wash=noise(i.local.xy*7)*.055+noise(i.local.xy*180)*.025;
                return fixed4(lerp(_Color.rgb,fixed3(.98,.96,.91),wash),1);
            }
            ENDCG
        }
    }
}
