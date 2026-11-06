Shader "Hidden/Memory/TapeHoverOutline"
{
    Properties
    {
        [HideInInspector] _EdgeWidth ("Edge width in pixels", Float) = 1.5
        [HideInInspector] _InkColor ("Edge color", Color) = (1,1,1,1)
        [HideInInspector] _EdgeDepthTest ("Edge depth test", Float) = 4
    }
    SubShader
    {
        Cull Off ZWrite Off
        CGINCLUDE
        #include "UnityCG.cginc"
        float3 _OutlineCenter;
        float _FillOpenings, _EdgeWidth, _Dashed;
        fixed4 _InkColor;
        struct Vertex { float4 pos : SV_POSITION; };
        Vertex vert(appdata_base input)
        {
            Vertex o; o.pos=UnityObjectToClipPos(input.vertex); return o;
        }
        // The cassette shell and hubs have internal openings. Stamp each triangle's
        // fan towards the centre so these openings do not receive a second outline.
        [maxvertexcount(5)]
        void footprint(triangle Vertex v[3], inout TriangleStream<Vertex> stream)
        {
            if (_FillOpenings < 0.5)
            {
                stream.Append(v[0]);stream.Append(v[1]);stream.Append(v[2]);return;
            }
            Vertex center;center.pos=mul(UNITY_MATRIX_VP,float4(_OutlineCenter,1));
            stream.Append(v[0]);stream.Append(v[1]);stream.Append(center);stream.Append(v[2]);stream.Append(v[0]);
        }
        [maxvertexcount(24)]
        void expand(triangle Vertex v[3], inout TriangleStream<Vertex> stream)
        {
            const float diagonal=.70710678;
            const float2 offsets[8]={float2(1,0),float2(diagonal,diagonal),float2(0,1),float2(-diagonal,diagonal),
                float2(-1,0),float2(-diagonal,-diagonal),float2(0,-1),float2(diagonal,-diagonal)};
            [unroll] for(int i=0;i<8;i++)
            {
                [unroll] for(int j=0;j<3;j++)
                {
                    Vertex o=v[j];
                    o.pos.xy+=offsets[i]*(2.0*_EdgeWidth/_ScreenParams.xy)*o.pos.w;
                    stream.Append(o);
                }
                stream.RestartStrip();
            }
        }
        fixed4 white(Vertex v):SV_Target { return _InkColor; }
        fixed4 edge(Vertex v):SV_Target { if(_Dashed>.5)clip(frac((v.pos.x+v.pos.y)/14)-.4); return _InkColor; }
        ENDCG

        Pass
        {
            ZTest Always ColorMask 0
            Stencil { Ref 1 ReadMask 1 WriteMask 1 Comp Always Pass Replace }
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry footprint
            #pragma fragment white
            ENDCG
        }
        Pass
        {
            ZTest [_EdgeDepthTest]
            Stencil { Ref 1 ReadMask 1 WriteMask 0 Comp NotEqual }
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry expand
            #pragma fragment edge
            ENDCG
        }
        Pass
        {
            ZTest Always ColorMask 0
            Stencil { Ref 0 ReadMask 1 WriteMask 1 Comp Always Pass Zero }
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry footprint
            #pragma fragment white
            ENDCG
        }
    }
    Fallback Off
}
