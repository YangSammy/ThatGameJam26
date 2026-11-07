Shader "Hidden/Park/Golden Backlight"
{
    Properties
    {
        _MainTex("Scene",2D)="white"{}
        _GoldenColor("Golden sunlight",Color)=(1,.68,.27,1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex,_PreviousVisibility,_SolarVisibility;
        float4 _MainTex_TexelSize,_SunPosition,_GoldenParams,_GoldenColor;
        float _VisibilityBlend;
        float2 SunUV()
        {
            float2 uv=_SunPosition.xy;
            #if UNITY_UV_STARTS_AT_TOP
            if(_MainTex_TexelSize.y<0)uv.y=1-uv.y;
            #endif
            return uv;
        }
        float VisibleSolarSample(float2 uv)
        {
            if(any(uv<0)||any(uv>1))return 0;
            float3 c=tex2D(_MainTex,uv).rgb;
            // Ordinary sky/clouds and material highlights should not create a sun
            // veil. The authored HDR disc remains bright before tone mapping.
            return smoothstep(1.5,8,dot(c,float3(.2126,.7152,.0722)));
        }
        float4 Visibility(v2f_img i):SV_Target
        {
            float2 uv=SunUV(),r=_SunPosition.zw;
            float value=VisibleSolarSample(uv);
            value+=VisibleSolarSample(uv+float2(r.x,0));
            value+=VisibleSolarSample(uv-float2(r.x,0));
            value+=VisibleSolarSample(uv+float2(0,r.y));
            value+=VisibleSolarSample(uv-float2(0,r.y));
            value+=VisibleSolarSample(uv+r*.7071);
            value+=VisibleSolarSample(uv-r*.7071);
            value+=VisibleSolarSample(uv+float2(r.x,-r.y)*.7071);
            value+=VisibleSolarSample(uv+float2(-r.x,r.y)*.7071);
            float previous=tex2D(_PreviousVisibility,float2(.5,.5)).r;
            return lerp(previous,value/9,saturate(_VisibilityBlend)).xxxx;
        }
        float Glow(float2 delta,float radius)
        {
            delta.x*=_GoldenParams.y;
            return exp(-dot(delta,delta)/(radius*radius));
        }
        float4 Composite(v2f_img i):SV_Target
        {
            float4 scene=tex2D(_MainTex,i.uv);
            float strength=_GoldenParams.x*tex2D(_SolarVisibility,float2(.5,.5)).r;
            float2 sun=SunUV();
            float broad=Glow(i.uv-sun,_GoldenParams.z);
            float core=Glow(i.uv-sun,.075);
            float2 axis=float2(.5,.5)-sun;
            float spill=Glow(i.uv-(sun+axis*.55),.28);
            float ghost=Glow(i.uv-(sun+axis*1.35),.027)*.3;
            float veil=(broad*.55+spill*.18)*strength;
            // Spatially localized optical wash, not a global orange color grade.
            // Keep scene texture beneath the veil and leave alpha untouched.
            scene.rgb*=lerp(float3(1,1,1),float3(1.02,.98,.91),saturate(veil)*_GoldenParams.w);
            scene.rgb+=_GoldenColor.rgb*(veil+core*.9*strength+ghost*strength);
            return scene;
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Visibility
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}
