Shader "Park/Soft Contact Shade"
{
 Properties { _Color("Shade",Color)=(.15,.18,.12,.22) }
 SubShader {
  Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Offset -1,-1
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
   v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;UNITY_TRANSFER_FOG(o,o.pos);return o;}
   fixed4 frag(v i):SV_Target{float r=length(i.uv*2-1);fixed4 c=_Color;c.a*=pow(saturate(1-r),1.5);UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(unity_FogColor.rgb,c.a));return c;}
   ENDCG
  }
 }
}
