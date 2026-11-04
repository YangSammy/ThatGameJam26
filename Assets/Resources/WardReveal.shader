Shader "Memory/WardReveal"
{
 Properties { _Color("Color", Color)=(1,1,1,1) _Height("World reveal height", Float)=-1000 }
 SubShader { Tags { "RenderType"="Opaque" } Pass { Cull Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float _Height;
 struct v2f {float4 pos:SV_POSITION;float worldY:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.worldY=mul(unity_ObjectToWorld,v.vertex).y;return o;}
 fixed4 frag(v2f i):SV_Target {clip(_Height-i.worldY);return _Color;}
 ENDCG
 } }
}
