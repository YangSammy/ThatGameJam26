Shader "Park/Soft Ground Blend"
{
 Properties {
  _Grass("Grass",Color)=(.40,.46,.22,1)
  _DryGrass("Dry grass",Color)=(.46,.48,.26,1)
  _Soil("Soil edge",Color)=(.36,.30,.21,1)
  _Gravel("Fine gravel path",Color)=(.58,.50,.36,1)
 }
 SubShader {
  Tags { "RenderType"="Opaque" } LOD 200
  CGPROGRAM
  #pragma surface surf Lambert fullforwardshadows
  #pragma target 3.0
  fixed4 _Grass,_DryGrass,_Soil,_Gravel;
  struct Input { float4 color:COLOR; float3 worldPos; };
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
  void surf(Input IN,inout SurfaceOutput o){
   float2 p=IN.worldPos.xz;
   float broad=noise(p*.22)*.6+noise(p*.7)*.4;
   float fine=noise(p*9)-.5;
   float3 grass=lerp(_Grass.rgb,_DryGrass.rgb,saturate(IN.color.b*.7+broad*.3));
   float gravel=smoothstep(.22,.78,IN.color.g);
   float soilWeight=saturate(IN.color.r)*(1-gravel);
   float3 soil=lerp(grass,_Soil.rgb,soilWeight*.68);
   float3 ground=lerp(soil,_Gravel.rgb,gravel);
   o.Albedo=ground*(.94+broad*.12+fine*.035);o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
