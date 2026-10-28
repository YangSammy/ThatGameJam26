Shader "Hidden/Memory/InspectionBackdrop"
{
    Properties { _Opacity ("Opacity", Range(0,1)) = 0.72 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Opacity;
            fixed4 frag(v2f_img input) : SV_Target { return fixed4(0,0,0,_Opacity); }
            ENDCG
        }
    }
}
