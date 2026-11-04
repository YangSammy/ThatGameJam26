Shader "ParkExtras/Natural Review"
{
 Properties
 {
  _Color ("Tint", Color) = (1,1,1,1)
  _MainTex ("Albedo Alpha", 2D) = "white" {}
  _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
  _Brightness ("Brightness", Range(0.5,3)) = 1.15
  _FillLight ("Shadow Fill", Range(0,1)) = 0.5
 }
 SubShader
 {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Cull Off
  Pass
  {
   Tags { "LightMode"="ForwardBase" }
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex;
   float4 _MainTex_ST;
   fixed4 _Color;
   half _Brightness, _Cutoff, _FillLight;
   struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
   struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 worldPos : TEXCOORD2; UNITY_FOG_COORDS(3) };
   v2f vert(appdata v)
   {
    v2f o;
    o.pos=UnityObjectToClipPos(v.vertex);
    o.uv=TRANSFORM_TEX(v.uv,_MainTex);
    o.normal=UnityObjectToWorldNormal(v.normal);
    o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
   }
   fixed4 frag(v2f i) : SV_Target
   {
    fixed4 tex=tex2D(_MainTex,i.uv)*_Color;
    clip(tex.a-_Cutoff);
    float3 n=i.normal*rsqrt(max(dot(i.normal,i.normal),0.0001));
    float3 l=normalize(UnityWorldSpaceLightDir(i.worldPos));
    half diffuse=abs(dot(n,l));
    half3 ambient=max(half3(0.45,0.45,0.45),half3(_FillLight,_FillLight,_FillLight));
    half3 illumination=ambient+_LightColor0.rgb*diffuse*0.65;
    fixed4 col=fixed4(tex.rgb*_Brightness*illumination,1);
    UNITY_APPLY_FOG(i.fogCoord,col);
    return col;
   }
   ENDCG
  }
 }
 Fallback Off
}
