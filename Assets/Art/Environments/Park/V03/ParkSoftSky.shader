Shader "Park/Soft Afternoon Sky"
{
 Properties {
  _Top ("Upper sky", Color) = (.40,.49,.57,1)
  _Horizon ("Horizon", Color) = (.76,.75,.68,1)
  _Cloud ("Cloud", Color) = (.86,.83,.75,1)
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
   float4 _Top,_Horizon,_Cloud;
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   fixed4 frag(v2f i):SV_Target {
    float3 d=normalize(i.dir);float h=saturate(d.y);
    float3 col=lerp(_Horizon.rgb,_Top.rgb,pow(h,.55));
    float2 uv=d.xz/(max(.18,d.y)+.4)*3.5;
    float n=noise(uv)*.6+noise(uv*2.1)*.27+noise(uv*4.2)*.13;
    float cloud=smoothstep(.43,.70,n)*smoothstep(.01,.13,d.y);
    col=lerp(col,_Cloud.rgb,cloud*.88);
    return float4(col,1);
   }
   ENDCG
  }
 }
}
