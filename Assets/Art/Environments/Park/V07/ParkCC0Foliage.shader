Shader "Park/CC0 Soft Foliage" {
 Properties {
  _MainTex("Leaf color and alpha",2D)="white"{}
  _Color("Tint",Color)=(.85,.88,.78,1)
  _Cutoff("Cutout",Range(0,1))=.4
  _Saturation("Color restraint",Range(0,1))=.7
  _Fill("Leaf scattered light",Range(0,.2))=.07
 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
  CGPROGRAM
  #pragma surface surf Lambert alphatest:_Cutoff fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;half _Saturation,_Fill;
  struct Input {float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutput o){
   fixed4 t=tex2D(_MainTex,IN.uv_MainTex);
   float lum=dot(t.rgb,float3(.2126,.7152,.0722));
   o.Albedo=lerp(lum.xxx,t.rgb,_Saturation)*_Color.rgb;
   o.Emission=o.Albedo*_Fill;o.Alpha=t.a;
  }
  ENDCG
 }
 Fallback "Transparent/Cutout/Diffuse"
}
