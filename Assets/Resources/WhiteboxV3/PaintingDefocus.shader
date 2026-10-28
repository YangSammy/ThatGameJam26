Shader "Hidden/MemoryWhitebox/V3/PaintingDefocus"
{
    Properties { _MainTex("Scene",2D)="white"{} _BlurTex("Defocused scene",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _BlurTex;
        float4 _MainTex_TexelSize;
        float2 _Direction;
        float _Radius, _Amount;
        fixed4 Blur(v2f_img i):SV_Target
        {
            float2 d=_MainTex_TexelSize.xy*_Direction*_Radius;
            fixed4 c=tex2D(_MainTex,i.uv)*.227027;
            c+=(tex2D(_MainTex,i.uv+d)+tex2D(_MainTex,i.uv-d))*.1945946;
            c+=(tex2D(_MainTex,i.uv+d*2)+tex2D(_MainTex,i.uv-d*2))*.1216216;
            c+=(tex2D(_MainTex,i.uv+d*3)+tex2D(_MainTex,i.uv-d*3))*.054054;
            c+=(tex2D(_MainTex,i.uv+d*4)+tex2D(_MainTex,i.uv-d*4))*.016216;
            return c;
        }
        fixed4 Composite(v2f_img i):SV_Target
        {return lerp(tex2D(_MainTex,i.uv),tex2D(_BlurTex,i.uv),saturate(_Amount));}
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}
