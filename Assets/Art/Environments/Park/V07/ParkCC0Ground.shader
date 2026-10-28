Shader "Park/CC0 Ground Blend" {
 Properties {
 _Grass("Lawn",Color)=(.31,.36,.19,1)
 _DryGrass("Dry variation",Color)=(.39,.40,.24,1)
 _Soil("Soil",Color)=(.30,.26,.19,1)
 _Gravel("Path",Color)=(.52,.46,.35,1)
 _GrassTex("CC0 grass",2D)="white"{}
 _SoilTex("CC0 soil",2D)="white"{}
 _PathTex("CC0 path",2D)="white"{}
 _Detail("Texture contrast",Range(0,1))=.45
 }
 SubShader {
 Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _GrassTex,_SoilTex,_PathTex;fixed4 _Grass,_DryGrass,_Soil,_Gravel;half _Detail;
 struct Input {float4 color:COLOR;float3 worldPos;};
 float3 detail(sampler2D tex,float2 uv){float3 t=tex2D(tex,uv).rgb;float l=dot(t,float3(.2126,.7152,.0722));return lerp(float3(1,1,1),clamp(lerp(l.xxx,t,.25)*2,.5,1.5),_Detail);}
 void surf(Input IN,inout SurfaceOutputStandard o){
  float2 p=IN.worldPos.xz;
  float broad=tex2D(_GrassTex,p*.037).g;
  float3 grass=lerp(_Grass.rgb,_DryGrass.rgb,saturate(IN.color.b*.6+broad*.3))*detail(_GrassTex,p/3.7);
  float gravel=smoothstep(.22,.78,IN.color.g);
  float soil=saturate(IN.color.r)*(1-gravel);
  float3 c=lerp(grass,_Soil.rgb*detail(_SoilTex,p/2.6),soil*.8);
  o.Albedo=lerp(c,_Gravel.rgb*detail(_PathTex,p/3),gravel);
  o.Metallic=0;o.Smoothness=.08;o.Occlusion=1;o.Alpha=1;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
