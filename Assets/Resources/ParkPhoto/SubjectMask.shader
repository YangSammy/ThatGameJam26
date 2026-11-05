Shader "Hidden/ParkPhoto/SubjectMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Back ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 position:SV_POSITION; };
            v2f vert(appdata_base input){v2f o;o.position=UnityObjectToClipPos(input.vertex);return o;}
            fixed4 frag(v2f input):SV_Target{return fixed4(1,1,1,1);}
            ENDCG
        }
    }
}
