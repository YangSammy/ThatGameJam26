Shader "Park/Quiet Creek"
{
 Properties {
  _Color("Water tone",Color)=(.19,.29,.26,1)
  _Reflection("Soft sky reflection",Color)=(.58,.62,.56,1)
 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 150
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   float4 _Color,_Reflection;
   struct a {float4 vertex:POSITION;};
   struct v {float4 p:SV_POSITION;float3 w:TEXCOORD0;UNITY_FOG_COORDS(1)};
   v vert(a i){v o;o.p=UnityObjectToClipPos(i.vertex);o.w=mul(unity_ObjectToWorld,i.vertex).xyz;UNITY_TRANSFER_FOG(o,o.p);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   fixed4 frag(v i):SV_Target {
    float2 p=i.w.xz;float t=_Time.y*.08;
    float n=noise(float2(p.x*1.2,p.y*5.0+t));
    float broad=noise(float2(p.x*.7,p.y*1.4-t));
    float facing=1-saturate(dot(normalize(_WorldSpaceCameraPos-i.w),float3(0,1,0)));
    float reflectance=saturate(.12+pow(facing,2)*.40+broad*.28);
    float3 col=lerp(_Color.rgb,_Reflection.rgb,reflectance);
    col+=smoothstep(.65,.78,n)*.07;
    fixed4 result=float4(col,1);UNITY_APPLY_FOG(i.fogCoord,result);return result;
   }
   ENDCG
  }
 }
}
