Shader "Park/Layered Afternoon Sky"
{
 Properties { _Top("Upper Sky",Color)=(.43,.52,.60,1) _Horizon("Horizon",Color)=(.72,.75,.73,1) _Cloud("Cloud",Color)=(.88,.84,.76,1) }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct a {float4 vertex:POSITION;};
   struct v {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
   float4 _Top,_Horizon,_Cloud;
   v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.dir=i.vertex.xyz;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   fixed4 frag(v i):SV_Target{
    float3 d=normalize(i.dir);float h=saturate(d.y);
    float3 col=lerp(_Horizon.rgb,_Top.rgb,pow(h,.48));
    float2 uv=d.xz/(max(.12,d.y)+.5)*4;
    float n=noise(uv)*.55+noise(uv*2.13+3.7)*.27+noise(uv*4.31)*.13+noise(uv*8.2)*.05;
    float cloud=smoothstep(.44,.67,n)*smoothstep(.015,.15,d.y);
    float3 shade=_Cloud.rgb*(.91+.09*smoothstep(.45,.7,n));
    return float4(lerp(col,shade,cloud*.85),1);
   }
   ENDCG
  }
 }
}
