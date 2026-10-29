Shader "Hidden/Park/Forest Bake Mask"
{
 Properties { _MainTex("Texture",2D)="white"{} _Cutoff("Cutoff",Float)=0.3 }
 SubShader
 {
  Tags { "RenderType"="TreeLeaf" }
  Cull Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "UnityBuiltin3xTreeLibrary.cginc"
   sampler2D _MainTex;
   float _Cutoff;
   struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float3 viewDir:TEXCOORD1; };
   v2f vert(appdata_full v)
   {
    TreeVertLeaf(v);
    v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;
    TANGENT_SPACE_ROTATION;
    o.viewDir=mul(rotation,ObjSpaceViewDir(v.vertex));return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float alpha=tex2D(_MainTex,i.uv).a;
    alpha*=1-pow(normalize(i.viewDir).z,12);
    clip(alpha-_Cutoff);return fixed4(1,1,1,1);
   }
   ENDCG
  }
 }
 SubShader
 {
  Tags { "RenderType"="TreeBark" }
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "UnityBuiltin3xTreeLibrary.cginc"
   struct v2f { float4 position:SV_POSITION; };
   v2f vert(appdata_full v){TreeVertBark(v);v2f o;o.position=UnityObjectToClipPos(v.vertex);return o;}
   fixed4 frag(v2f i):SV_Target {return fixed4(1,1,1,1);}
   ENDCG
  }
 }
}
