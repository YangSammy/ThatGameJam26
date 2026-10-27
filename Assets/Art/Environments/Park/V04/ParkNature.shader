Shader "Park/Rounded Foliage"
{
 Properties { _Color("Leaf color",Color)=(.38,.44,.22,1) _AmbientLift("Soft leaf fill",Range(0,.15))=.035 }
 SubShader {
  Tags { "RenderType"="Opaque" } LOD 200 Cull Off
  CGPROGRAM
  #pragma surface surf Lambert fullforwardshadows addshadow
  #pragma target 3.0
  fixed4 _Color; half _AmbientLift;
  struct Input { float4 color:COLOR; };
  void surf(Input IN,inout SurfaceOutput o){o.Albedo=_Color.rgb*IN.color.rgb;o.Emission=o.Albedo*_AmbientLift;o.Alpha=1;}
  ENDCG
 }
 Fallback "Diffuse"
}
