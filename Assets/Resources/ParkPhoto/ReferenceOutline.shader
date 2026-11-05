Shader "Hidden/ParkPhoto/ReferenceOutline"
{
    Properties { _MainTex("Reference",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float luminance(float2 uv){return dot(tex2D(_MainTex,uv).rgb,float3(.299,.587,.114));}
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 d=_MainTex_TexelSize.xy*1.4;
                float gx=luminance(i.uv+float2(d.x,0))-luminance(i.uv-float2(d.x,0));
                float gy=luminance(i.uv+float2(0,d.y))-luminance(i.uv-float2(0,d.y));
                float edge=smoothstep(.045,.22,length(float2(gx,gy)));
                return fixed4(1,1,1,edge);
            }
            ENDCG
        }
    }
}
