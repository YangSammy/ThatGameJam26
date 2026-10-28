Shader "Park/CC0 Quiet Surface"
{
 Properties {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Base color - no lighting",2D)="white"{}
  _BumpMap("Restrained normal",2D)="bump"{}
  _MetallicGlossMap("Metallic and smoothness",2D)="black"{}
  _BumpScale("Normal strength",Range(0,.5))=.12
  _Meters("Texture repeat meters",Float)=2
  _Kind("0 wood 1 painted siding 2 stone 3 roof 4 bark",Float)=0
  _GrainDirection("World grain direction",Vector)=(1,0,0,0)
  _Contrast("Texture contrast",Range(0,1))=.6
  _Wear("Restrained wear",Range(0,.15))=.035
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 250
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap,_MetallicGlossMap;
  fixed4 _Color;float _Meters,_Kind,_BumpScale,_Wear,_Contrast;float4 _GrainDirection;
  struct Input {float3 worldPos;float3 worldNormal;INTERNAL_DATA};
  void surf(Input IN,inout SurfaceOutputStandard o){
   float3 p=IN.worldPos;float3 nw=normalize(WorldNormalVector(IN,float3(0,0,1)));float3 weight=pow(abs(nw),4);weight/=max(.001,weight.x+weight.y+weight.z);
   float meters=max(.1,_Meters);float2 uv;
   if(_Kind<.5 || _Kind>3.5){
    float3 axis=normalize(_GrainDirection.xyz);float3 crossAxis=abs(axis.y)<.8?float3(0,1,0):float3(1,0,0);
    float across=dot(p,crossAxis)*.8+dot(p,cross(axis,crossAxis))*.6;
    uv=float2(dot(p,axis),across)/meters;
   }else if(_Kind<1.5){uv=float2(p.x*.71+p.z*.71,p.y)/meters;}
   else {uv=(p.yz*weight.x+p.xz*weight.y+p.xy*weight.z)/meters;}
   float3 texColor=tex2D(_MainTex,uv).rgb;float3 base=lerp(float3(.40,.36,.29),texColor,_Contrast)*_Color.rgb;
   float smooth=tex2D(_MetallicGlossMap,uv).a;
   if(_Kind>.5&&_Kind<1.5){
    float board=frac(p.y/.23);float seam=1-smoothstep(.0,.022,min(board,1-board));
    base*=1-seam*.045;smooth*=1-seam*.12;
   }
   float damp=(1-smoothstep(-.1,.65,p.y))*_Wear;
   base*=1-damp;
   o.Albedo=base;o.Metallic=0;o.Smoothness=smooth;
   // Very small normal variation; broad material changes do most of the work.
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,uv),_BumpScale);
   o.Occlusion=1;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
