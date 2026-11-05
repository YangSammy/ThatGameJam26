Shader "Memory/WardReveal"
{
 Properties
 {
  _Color("Color", Color)=(1,1,1,1)
  _MainTex("Albedo", 2D)="white" {}
  _Height("World reveal height", Float)=-1000
  _WardBacklight("Ward backlight", Range(0,1))=0
 }
 SubShader { Tags { "RenderType"="Opaque" } Pass { Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 _MainTex_ST;
 fixed4 _Color;
 float _Height, _WardBacklight;
 struct v2f
 {
  float4 pos:SV_POSITION;
  float worldY:TEXCOORD0;
  float2 uv:TEXCOORD1;
  float3 normal:TEXCOORD2;
 };
 v2f vert(appdata_base v)
 {
  v2f o;
  o.pos=UnityObjectToClipPos(v.vertex);
  o.worldY=mul(unity_ObjectToWorld,v.vertex).y;
  o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);
  o.normal=UnityObjectToWorldNormal(v.normal);
  return o;
 }
 fixed4 frag(v2f i):SV_Target
 {
  clip(_Height-i.worldY);
  fixed4 color=tex2D(_MainTex,i.uv)*_Color;
  float rim=saturate(dot(normalize(i.normal),normalize(float3(.28,.62,1))));
  color.rgb*=lerp(1,.38+.66*rim,_WardBacklight);
  return color;
 }
 ENDCG
 } }
}
