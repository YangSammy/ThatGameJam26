Shader "ThatGamejam/Art/AlbumPage" {
 Properties { _MainTex("Runtime page content",2D)="white"{} _PaperTex("Paper",2D)="white"{} _ContentAmount("Content enabled",Range(0,1))=0 _ContentRect("UV scale/offset",Vector)=(1,1,0,0) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Lambert
 sampler2D _MainTex,_PaperTex;float _ContentAmount;float4 _ContentRect;
 struct Input{float2 uv_MainTex;};
 void surf(Input IN,inout SurfaceOutput o){float3 paper=tex2D(_PaperTex,IN.uv_MainTex).rgb;float4 picture=tex2D(_MainTex,IN.uv_MainTex*_ContentRect.xy+_ContentRect.zw);float ink=_ContentAmount*picture.a;o.Albedo=paper*(1-ink);o.Emission=picture.rgb*ink;o.Alpha=1;}
 ENDCG
 } Fallback "Diffuse"
}
