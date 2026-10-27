Shader "Hidden/ParkPhoto/ManualFocus"
{
    Properties { _MainTex("Image",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _MainTex_TexelSize;
            float _FocusDistance;
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 depthUV=i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0)depthUV.y=1-depthUV.y;
                #endif
                float depth=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,depthUV));
                float blur=saturate(abs(depth-_FocusDistance)/max(.35,_FocusDistance*.22));
                float2 radius=abs(_MainTex_TexelSize.xy)*blur*8;
                fixed4 sum=tex2D(_MainTex,i.uv)*.2;
                sum+=tex2D(_MainTex,i.uv+radius*float2(1,0))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-1,0))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,1))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(0,-1))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(.7,.7))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-.7,.7))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(.7,-.7))*.1;
                sum+=tex2D(_MainTex,i.uv+radius*float2(-.7,-.7))*.1;
                return sum;
            }
            ENDCG
        }
    }
}
