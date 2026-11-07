Shader "Park/Solar Afternoon Sky"
{
    Properties
    {
        _Top("Upper Sky",Color)=(.3,.47,.65,1)
        _Horizon("Horizon",Color)=(.75,.77,.73,1)
        _Cloud("Cloud",Color)=(.8,.77,.7,1)
        _SunDirection("Direction toward sun",Vector)=(-.7,.52,.49,0)
        [HDR] _SunRadiance("Sun radiance",Color)=(24,19,13,1)
        _SunRadius("Sun angular radius (degrees)",Range(.1,2))=.7
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
            float4 _Top,_Horizon,_Cloud,_SunDirection,_SunRadiance;
            float _SunRadius;
            v2f vert(appdata i){v2f o;o.pos=UnityObjectToClipPos(i.vertex);o.dir=i.vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            half4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.dir);float h=saturate(d.y);
                float3 color=lerp(_Horizon.rgb,_Top.rgb,pow(h,.48));
                float2 uv=d.xz/(max(.12,d.y)+.5)*4;
                float n=noise(uv)*.55+noise(uv*2.13+3.7)*.27+noise(uv*4.31)*.13+noise(uv*8.2)*.05;
                float cloud=smoothstep(.44,.67,n)*smoothstep(.015,.15,d.y);
                color=lerp(color,_Cloud.rgb*(.91+.09*smoothstep(.45,.7,n)),cloud*.85);
                float alignment=dot(d,normalize(_SunDirection.xyz));
                float radius=radians(_SunRadius);
                float disc=smoothstep(cos(radius),cos(radius*.72),alignment);
                float halo=exp(-max(0,1-alignment)/.0015)*.025;
                float horizon=smoothstep(-.01,.02,_SunDirection.y);
                color+=_SunRadiance.rgb*(disc+halo)*(1-cloud*.65)*horizon;
                return half4(color,1);
            }
            ENDCG
        }
    }
}
