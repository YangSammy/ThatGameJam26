Shader "Hidden/ParkExtras/GrassTransitionBake" {
 Properties { _MainTex("Grass",2D)="white"{} _MossTex("Existing ground",2D)="white"{} _GrassWeight("Grass weight",Float)=0.5 _GrassRepeat("Grass repeat",Float)=5 }
 SubShader { Cull Off ZWrite Off ZTest Always Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_MossTex;float _GrassWeight,_GrassRepeat;
 fixed4 frag(v2f_img i):SV_Target {float3 grass=tex2D(_MainTex,i.uv*_GrassRepeat).rgb;float3 moss=tex2D(_MossTex,i.uv).rgb;return float4(lerp(moss,grass,_GrassWeight),1);}
 ENDCG } } Fallback Off
}
