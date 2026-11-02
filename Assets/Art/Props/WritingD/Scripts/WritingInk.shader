Shader "ThatGamejam/Art/WritingInk"
{
 Properties {
  _PaperTex("Paper base",2D)="white"{}
  _MainTex("Runtime ink RGBA",2D)="white"{}
  _BumpMap("Paper normal",2D)="bump"{}
  _InkAmount("Ink visibility",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _PaperTex,_MainTex,_BumpMap; half _InkAmount;
  struct Input {float2 uv_PaperTex;float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   fixed4 ink=tex2D(_MainTex,IN.uv_MainTex);
   o.Albedo=lerp(tex2D(_PaperTex,IN.uv_PaperTex).rgb,ink.rgb,saturate(ink.a*_InkAmount));
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_PaperTex));o.Metallic=0;o.Smoothness=.22;o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
