Shader "ThatGamejam/Art/AlbumReveal" {
 Properties { _MainTex("Albedo",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=.55 _MetallicGlossMap("Metallic R Smoothness A",2D)="black"{} _OcclusionMap("AO",2D)="white"{} _Reveal("Appearance",Range(0,1))=1 _RevealRange("Local X range",Vector)=(-.158,.158,0,0) }
 SubShader { Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex,_BumpMap,_MetallicGlossMap,_OcclusionMap;float _BumpScale,_Reveal;float4 _RevealRange;
 struct Input {float2 uv_MainTex;float3 worldPos;};
 void surf(Input IN,inout SurfaceOutputStandard o){float x=mul(unity_WorldToObject,float4(IN.worldPos,1)).x;clip(_Reveal-0.00001);clip(lerp(_RevealRange.x,_RevealRange.y,_Reveal)-x);o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_MainTex),_BumpScale);float4 m=tex2D(_MetallicGlossMap,IN.uv_MainTex);o.Metallic=m.r;o.Smoothness=m.a;o.Occlusion=tex2D(_OcclusionMap,IN.uv_MainTex).g;o.Alpha=1;}
 ENDCG
 } Fallback "Diffuse"
}
