Shader "Memory/Album Observation"
{
    Properties { _MainTex ("Scene view", 2D) = "white" {} _PaperWhite ("Fade to paper", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Lighting Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half _PaperWhite;
            half4 frag(v2f_img i) : SV_Target
            {
                return lerp(tex2D(_MainTex,i.uv), half4(1,1,1,1), _PaperWhite);
            }
            ENDCG
        }
    }
}
