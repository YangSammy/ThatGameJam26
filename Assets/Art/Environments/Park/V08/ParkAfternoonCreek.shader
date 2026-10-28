Shader "Park/Afternoon Probe Creek" {
 Properties {
  _Color("Water tone", Color)=(.20,.28,.23,1)
  _DepthScale("Depth opacity", Range(.1,4))=1.1
  _ReflectionStrength("Probe reflection",Range(0,1))=.38
  _RippleStrength("Ripple strength",Range(0,.15))=.035
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
   float4 _Color; float _DepthScale,_ReflectionStrength,_RippleStrength;
   struct appdata {float4 vertex:POSITION;};
   struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float4 screen:TEXCOORD1;float eye:TEXCOORD2;UNITY_FOG_COORDS(3)};
   v2f vert(appdata i){v2f o;o.pos=UnityObjectToClipPos(i.vertex);o.world=mul(unity_ObjectToWorld,i.vertex).xyz;o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(i.vertex).z;UNITY_TRANSFER_FOG(o,o.pos);return o;}
   fixed4 frag(v2f i):SV_Target {
    float t=_Time.y;
    float3 n=normalize(float3(sin(i.world.z*4.1+t*.65)*_RippleStrength,1,cos(i.world.x*3.2+i.world.z*1.8-t*.4)*_RippleStrength));
    float3 view=normalize(_WorldSpaceCameraPos-i.world);
    half4 probe=UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,reflect(-view,n),3);
    half3 reflection=DecodeHDR(probe,unity_SpecCube0_HDR);
    float fresnel=.08+.65*pow(1-saturate(dot(view,n)),3);
    float sceneDepth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
    float depth=max(0,sceneDepth-i.eye);
    float alpha=saturate(depth*_DepthScale)*.72+fresnel*.18;
    float3 col=lerp(_Color.rgb,reflection,saturate(fresnel*_ReflectionStrength)); col += .012*sin(i.world.z*5.5+sin(i.world.x*2.1)-t*.65);
    fixed4 result=float4(col,alpha);UNITY_APPLY_FOG(i.fogCoord,result);return result;
   }
   ENDCG
  }
 }
}

