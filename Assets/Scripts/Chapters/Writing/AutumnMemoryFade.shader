Shader "MemoryWhitebox/AutumnMemoryFade"
{
    Properties { _Color ("Memory paper", Color) = (0.855,0.847,0.808,0) }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
