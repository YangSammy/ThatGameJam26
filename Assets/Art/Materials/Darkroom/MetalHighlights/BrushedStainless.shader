Shader "Memory/Darkroom Brushed Stainless"
{
 Properties {
  _Color("Steel reflectance",Color)=(.52,.52,.52,1)
  _Smoothness("Base smoothness",Range(0,1))=.78
  _BrushStrength("Brushing",Range(0,.3))=.12
  _Wear("Use marks",Range(0,1))=.35
 }
 SubShader {
  Tags { "RenderType"="Opaque" } LOD 300
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  fixed4 _Color; half _Smoothness,_BrushStrength,_Wear;
  struct Input { float3 worldPos; };
  float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
  void surf(Input IN,inout SurfaceOutputStandard o) {
   // World-scale machining lines: consistent density on differently sized props.
   float3 p=IN.worldPos;
   float phase=p.x*2800+p.y*3100;
   float resolved=saturate(1-fwidth(phase)*.3);
   float grain=sin(phase+sin(p.z*31)*.3)*resolved;
   float2 cell=float2((p.x+p.y)*650,p.z*18);
   float seed=hash(floor(cell));
   float width=max(fwidth(cell.x),.035);
   float scratch=(1-smoothstep(.02,.02+width,abs(frac(cell.x)-.5)))*step(.94,seed);
   float wear=scratch*_Wear;
   float broad=sin(p.x*47+p.z*33)*sin(p.z*19+p.y*23);
   o.Albedo=_Color.rgb*(.96+grain*.035-wear*.18);
   o.Metallic=.9;
   o.Smoothness=saturate(_Smoothness+grain*_BrushStrength*.55-wear*.24+broad*.025);
   o.Normal=normalize(float3(grain*_BrushStrength*.24,wear*.035,1));
   o.Occlusion=1;
  }
  ENDCG
 }
 FallBack "Standard"
}
