Shader "Memory/WardSoftReveal"
{
    Properties
    {
        _MainTex ("Ward concept", 2D) = "white" {}
        _SceneTex ("Outgoing camera", 2D) = "black" {}
        _SceneBlend ("Ward image blend", Range(0,1)) = 1
        _PageTex ("Live paper ink", 2D) = "black" {}
        _UsePageInk ("Composite live ink", Float) = 0
        _PageRect ("Paper ink UV rectangle", Vector) = (0.10,0.055,0.80,0.15)
        _WhiteColor ("Warm white", Color) = (0.973, 0.957, 0.925, 1)
        _Progress ("Reveal progress", Range(0,1)) = 0
        _Feather ("Soft edge width", Range(0.005,0.3)) = 0.12
        _NoiseAmount ("Edge irregularity", Range(0,0.3)) = 0.17
        _NoiseScale ("Noise scale", Range(1,30)) = 4.3
        _FlowAmount ("Edge flow strength", Range(0,2)) = 0.8
        [Enum(Flow,0,SnowMelt,1)] _RevealStyle ("Reveal style", Float) = 0
        _MeltScale ("Snow patch scale", Range(3,24)) = 8
        _MeltSpread ("Snow melt spread", Range(0.05,0.7)) = 0.58
        _MeltSoftness ("Snow edge softness", Range(0.005,0.15)) = 0.060
        _RevealTop ("Final reveal height (UV)", Range(0.4,1)) = 0.70
        _FaceEllipse ("Protected face: center XY, radii ZW", Vector) = (0.496,0.78,0.065,0.092)
        _FaceFeather ("Face mask feather", Range(0.01,0.8)) = 0.3
        _ImageAspect ("Source aspect ratio", Float) = 1.776833
        _ViewportAspect ("Viewport aspect ratio", Float) = 1.776833
        _OrderedReveal ("Use calibrated body order", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _SceneTex, _PageTex;
            float4 _WhiteColor, _FaceEllipse, _PageRect, _SceneTex_TexelSize;
            float _SceneBlend, _UsePageInk;
            float _Progress, _Feather, _NoiseAmount, _NoiseScale, _FlowAmount;
            float _RevealTop, _FaceFeather, _ImageAspect, _ViewportAspect;
            float _RevealStyle, _MeltScale, _MeltSpread, _MeltSoftness;
            float _OrderedReveal;

            float Hash(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                            lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float MeltDistance(float2 p)
            {
                // Jittered, warped cell centers create rounded melt openings. Fixed
                // positions let snow islands shrink continuously without swimming.
                float2 cell=floor(p), f=frac(p);
                float distance=2;
                [unroll] for(int y=-1;y<=1;y++)
                [unroll] for(int x=-1;x<=1;x++)
                {
                    float2 offset=float2(x,y);
                    float2 seed=cell+offset;
                    float2 center=0.1+0.8*float2(Hash(seed),Hash(seed+73.7));
                    distance=min(distance,length(offset+center-f));
                }
                return distance;
            }
            // UV calibration belongs to WardRuntimeBlankPaper. These semantic regions
            // defer low hands until after the torso; a plain vertical wipe cannot do that.
            float OrderedThreshold(float2 uv, float melt)
            {
                float y=uv.y+(melt-0.5)*0.12;
                float threshold;
                if(y<0.32)threshold=lerp(0.02,0.20,saturate((y-0.22)/0.10));
                else if(y<0.40)threshold=lerp(0.20,0.38,(y-0.32)/0.08);
                else if(y<0.56)threshold=lerp(0.38,0.60,(y-0.40)/0.16);
                else threshold=lerp(0.60,1.01,saturate((y-0.56)/0.12));
                // Soft semantic mattes keep the actual melting contour continuous.
                // They delay the body and hands without revealing rectangular crop edges.
                float torso=length((uv-float2(0.496,0.505))/float2(0.13,0.13))+(melt-.5)*.32;
                float bodyWeight=1-smoothstep(0.82,1.12,torso);
                threshold=lerp(threshold,max(threshold,lerp(0.63,0.75,saturate(melt))),bodyWeight);
                float hand=length((uv-float2(0.496,0.445))/float2(0.115,0.045))+(melt-.5)*.8;
                threshold=lerp(threshold,max(threshold,lerp(0.81,0.85,saturate(melt))),1-smoothstep(0.65,1.45,hand));
                float shoulder=(1-smoothstep(0.115,0.15,abs(uv.x-.496)))*smoothstep(.55,.59,uv.y);
                threshold=lerp(threshold,max(threshold,lerp(.91,.98,saturate(melt))),shoulder);
                return threshold;
            }
            float4 frag(v2f_img input):SV_Target
            {
                float2 sceneUV=input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if(_SceneTex_TexelSize.y<0)sceneUV.y=1-sceneUV.y;
                #endif
                float4 scene=tex2D(_SceneTex,sceneUV);
                float2 uv=input.uv;
                float ratio=max(_ViewportAspect,0.01)/max(_ImageAspect,0.01);
                if(ratio>1) uv.x=(uv.x-0.5)*ratio+0.5;
                else uv.y=(uv.y-0.5)/ratio+0.5;
                if(any(uv<0)||any(uv>1)) return lerp(scene,_WhiteColor,_SceneBlend);

                float4 source=tex2D(_MainTex,uv);
                // Progress is time-driven by the preview. Flow evolves continuously with
                // that progress, without random reseeding or unrelated wall-clock time.
                float2 p=uv*float2(_ImageAspect,1)*_NoiseScale;
                float flow=saturate(_Progress)*_FlowAmount;
                float2 warp=float2(Noise(p*0.65+float2(flow,13.1)),
                                   Noise(p*0.65+float2(27.3,-flow)))-0.5;
                p+=warp*1.8;
                float noise=Noise(p+float2(flow*0.9,-flow*0.6))*0.65
                           +Noise(p*2.07+float2(17.1-flow,17.1+flow*0.5))*0.25
                           +Noise(p*5.13+43.7)*0.10;
                float threshold=uv.y+(noise-0.5)*2*_NoiseAmount;
                float feather=max(_Feather,0.001);
                float front=lerp(0.12-_NoiseAmount-feather,_RevealTop,saturate(_Progress));
                float reveal=smoothstep(threshold-feather,threshold+feather,front);
                if(_RevealStyle>0.5)
                {
                    float2 snowUV=uv*float2(_ImageAspect,1);
                    float2 bend=float2(Noise(snowUV*3.1+19),Noise(snowUV*3.1+47))-0.5;
                    float2 snowP=snowUV*_MeltScale+bend*1.6;
                    float holes=MeltDistance(snowP)*0.22
                               +Noise(snowP*1.3+20.7)*0.58
                               +Noise(snowP*3.7+51.3)*0.20;
                    float uneven=Noise(snowUV*3.3+float2(5,31))-0.5;
                    float meltThreshold=uv.y+(holes-0.46)*_MeltSpread+uneven*0.19;
                    float meltFront=lerp(-0.12,_RevealTop,saturate(_Progress));
                    float soft=max(_MeltSoftness,0.001);
                    // Rounded openings merge into a larger clear area. A wide, faint
                    // halo keeps the melting margins soft rather than cut-out edges.
                    float core=smoothstep(meltThreshold-soft,meltThreshold+soft,meltFront);
                    float halo=smoothstep(meltThreshold-soft*3,meltThreshold+soft*3,meltFront);
                    reveal=core*0.72+halo*0.28;
                    if(_OrderedReveal>0.5)
                    {
                        float ordered=OrderedThreshold(uv,saturate(holes));
                        float width=min(0.022,soft*0.36);
                        reveal=smoothstep(ordered-width,ordered+width,saturate(_Progress));
                        // The image's nose line is an upper bound, including the soft halo.
                        float cap=_RevealTop-saturate(holes)*.035;
                        reveal*=1-smoothstep(cap-.018,cap,uv.y);
                    }
                }
                // The starting image must contain no early isolated hospital pixels.
                if(_Progress<=0) reveal=0;

                // Opaque core covers both eyes. Feather only extends OUTSIDE the core.
                float faceDistance=length((uv-_FaceEllipse.xy)/max(_FaceEllipse.zw,float2(0.001,0.001)));
                reveal*=smoothstep(1,1+max(_FaceFeather,0.001),faceDistance);

                // Trapezoid fitted to this concept's desk, not a reveal-order texture.
                float deskTop=min(0.227,min(0.167+uv.x*0.923,0.17+(1-uv.x)*1.425));
                float foreground=1-smoothstep(deskTop-0.001,deskTop,uv.y);
                reveal=max(reveal,foreground);
                float4 result=float4(lerp(_WhiteColor.rgb,source.rgb,reveal),1);
                if(_UsePageInk>0.5)
                {
                    float2 pageUV=(uv-_PageRect.xy)/max(_PageRect.zw,float2(0.001,0.001));
                    if(all(pageUV>=0)&&all(pageUV<=1))
                    {
                        float4 ink=tex2D(_PageTex,pageUV);
                        float margin=smoothstep(0,0.012,min(min(pageUV.x,1-pageUV.x),min(pageUV.y,1-pageUV.y)));
                        result.rgb=lerp(result.rgb,ink.rgb,ink.a*margin);
                    }
                }
                return lerp(scene,result,_SceneBlend);
            }
            ENDCG
        }
    }
}
