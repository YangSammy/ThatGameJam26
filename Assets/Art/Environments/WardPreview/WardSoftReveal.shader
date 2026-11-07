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
        _MeltEase ("Melt deceleration", Range(1,4)) = 2.8
        _Feather ("Soft edge width", Range(0.005,0.3)) = 0.12
        _NoiseAmount ("Edge irregularity", Range(0,0.3)) = 0.17
        _NoiseScale ("Noise scale", Range(1,30)) = 4.3
        _FlowAmount ("Edge flow strength", Range(0,2)) = 0.8
        [Enum(Flow,0,SnowMelt,1)] _RevealStyle ("Reveal style", Float) = 0
        _MeltScale ("Snow patch scale", Range(3,24)) = 5
        _MeltSpread ("Snow melt spread", Range(0.05,0.7)) = 0.58
        _MeltSoftness ("Snow edge softness", Range(0.001,0.15)) = 0.008
        _SnowRelief ("Snow edge relief", Range(0,1)) = 0.35
        _SmokeStrength ("Smoke overlay strength", Range(0,1)) = 0.72
        _SmokeWidth ("Smoke transition width", Range(0.01,0.4)) = 0.18
        _SmokeScale ("Large smoke noise scale", Range(0.5,8)) = 2.2
        _SmokeSpeed ("Smoke scrolling speed", Range(0,2)) = 0.35
        _SmokeShadow ("Cold smoke shadow", Range(0,1)) = 0.65
        _SmokeGrain ("Edge grain strength", Range(0,0.3)) = 0.10
        _SmokeTime ("Smoke animation clock", Float) = 0
        _RevealTop ("Final reveal height (UV)", Range(0.4,1)) = 0.70
        _FaceEllipse ("Protected face: center XY, radii ZW", Vector) = (0.496,0.78,0.065,0.092)
        _FaceFeather ("Face mask feather", Range(0.01,0.8)) = 0.3
        _ImageAspect ("Source aspect ratio", Float) = 1.776833
        _ViewportAspect ("Viewport aspect ratio", Float) = 1.776833
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
            float _MeltEase;
            float _Progress, _Feather, _NoiseAmount, _NoiseScale, _FlowAmount;
            float _RevealTop, _FaceFeather, _ImageAspect, _ViewportAspect;
            float _RevealStyle, _MeltScale, _MeltSpread, _MeltSoftness, _SnowRelief;

            float _SmokeStrength, _SmokeWidth, _SmokeScale, _SmokeSpeed;
            float _SmokeShadow, _SmokeGrain, _SmokeTime;

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
                // Ease-out maps travel progress to faster early / slower late melting.
                // Fixed spatial noise keeps the base reveal monotonic.
                float meltProgress=1-pow(1-saturate(_Progress),max(1,_MeltEase));
                float2 contourUV=uv*float2(_ImageAspect,1);
                float2 contourWarp=float2(Noise(contourUV*3.1+8),Noise(contourUV*3.1+29))-.5;
                float contour=Noise(contourUV*5.3+contourWarp*2.1+41)*.7
                             +Noise(contourUV*11.7+contourWarp+73)*.3;
                float reveal;
                float snowEdge=0;
                float grain=0;
                float smokeBand=0;
                float cloud=0;
                float smokeGrain=0;
                if(_RevealStyle>0.5)
                {
                    // A fixed erosion field: advancing progress expands openings,
                    // without sliding noise or refilling already melted regions.
                    float2 snowUV=uv*float2(_ImageAspect,1);
                    float2 bend=float2(Noise(snowUV*2.7+19),Noise(snowUV*2.7+47))-0.5;
                    float2 snowP=snowUV*_MeltScale+bend*1.7;
                    float broad=Noise(snowP*.72+20.7);
                    float islands=MeltDistance(snowP*1.25+7.3);
                    float medium=Noise(snowP*2.3+31.9)-.5;
                    float detail=Noise(snowP*9.1+51.3)-.5;
                    // Broad, irregular sheets dominate; fine detail only roughens rims.
                    float erosion=(broad-.5)*.70+medium*.65+(islands-.45)*.55+detail*.07;
                    float meltThreshold=uv.y*.28+erosion*_MeltSpread*1.30;
                    // Start below the picture, then melt upward to the nose.
                    // Extra travel clears residual islands below the final cutoff.
                    float meltFront=lerp(-.24,_RevealTop*.28+_MeltSpread*.28,meltProgress);
                    float distance=meltFront-meltThreshold;
                    float soft=max(_MeltSoftness,fwidth(meltThreshold)*.8);
                    reveal=smoothstep(-soft,soft,distance);
                    // A very thin cold rim, not the previous broad translucent halo.
                    snowEdge=(1-smoothstep(soft,soft*4,abs(distance)));
                    grain=(Noise(snowUV*380+11)-.5)*.006;
                    // Independent drifting veil over the stable snow silhouette.
                    // Clock is supplied by the caller so pauses/previews are controllable.
                    float t=_SmokeTime*_SmokeSpeed;
                    float2 drift=float2(t*.13,-t*.09);
                    float2 smokeP=snowUV*_SmokeScale+drift;
                    float2 smokeWarp=float2(Noise(smokeP+13+t*.08),Noise(smokeP+39-t*.06))-.5;
                    smokeP+=smokeWarp*2.4;
                    float2 curl=float2(Noise(smokeP*.8+float2(7,t*.10)),
                                       Noise(smokeP*.8+float2(t*.08,61)))-.5;
                    smokeP+=curl*1.2;
                    cloud=Noise(smokeP)*.65+Noise(smokeP*2.13+17)*.25+Noise(smokeP*4.7+53)*.10;
                    float width=max(_SmokeWidth,.001);
                    smokeBand=1-smoothstep(width*.35,width*1.8,abs(distance));
                    // Coverage depends only on position. Animated cloud noise changes
                    // smoke shading/grain, never exposes new pixels while standing still.
                    float smokyReveal=smoothstep(-width,width,distance);
                    float smokeMix=_SmokeStrength*smokeBand*smoothstep(0,.08,_Progress);
                    reveal=lerp(reveal,smokyReveal,smokeMix);
                    // Fine grains advect slowly rather than reseeding each frame.
                    smokeGrain=(Noise((snowUV+drift*.012+smokeWarp*.008)*340)-.5)*_SmokeGrain;

                }
                else
                {
                    float2 p=uv*float2(_ImageAspect,1)*_NoiseScale;
                    float flow=meltProgress*_FlowAmount;
                    float2 warp=float2(Noise(p*.65+float2(flow,13.1)),
                                       Noise(p*.65+float2(27.3,-flow)))-.5;
                    p+=warp*1.8;
                    float noise=Noise(p+float2(flow*.9,-flow*.6))*.65
                               +Noise(p*2.07+float2(17.1-flow,17.1+flow*.5))*.25
                               +Noise(p*5.13+43.7)*.10;
                    float threshold=uv.y+(noise-.5)*2*_NoiseAmount;
                    float feather=max(_Feather,.001);
                    float front=lerp(.12-_NoiseAmount-feather,_RevealTop,meltProgress);
                    reveal=smoothstep(threshold-feather,threshold+feather,front);
                }
                // Warp the bottom-entry timing itself instead of imposing a horizontal
                // wipe over the holes. Multiplying by height keeps first openings low.
                float entryThreshold=uv.y*(.55+contour*.95);
                float entryFront=meltProgress*(_RevealTop+.42);
                float entryWidth=.025;
                float bottomGate=1-smoothstep(entryFront-entryWidth,entryFront,entryThreshold);
                // Bottom bias only governs entry. Afterwards independent patches can
                // open throughout the image instead of following a rising wipe.
                bottomGate=lerp(bottomGate,1,smoothstep(.08,.30,saturate(_Progress)));
                reveal*=bottomGate;
                // Uneven stopping edge stays entirely below the hard nose limit.
                float noseCap=_RevealTop-contour*.075;
                reveal*=1-smoothstep(noseCap-.018,noseCap,uv.y);
                // The starting image must contain no early isolated hospital pixels.
                if(_Progress<=0) reveal=0;

                // Opaque core covers both eyes. Feather only extends OUTSIDE the core.
                float faceDistance=length((uv-_FaceEllipse.xy)/max(_FaceEllipse.zw,float2(0.001,0.001)));
                reveal*=smoothstep(1,1+max(_FaceFeather,0.001),faceDistance);

                // Both layers obey the same eye/nose and bottom-entry constraints.
                float reliefGate=bottomGate*smoothstep(1,1+max(_FaceFeather,.001),faceDistance)
                    *(1-smoothstep(noseCap-.035,noseCap,uv.y))*step(.00001,_Progress);
                float rim=snowEdge*_SnowRelief*reliefGate;
                float3 snowColor=_WhiteColor.rgb-float3(.045,.035,.022)*rim+grain*rim;
                float smokeShade=smokeBand*_SmokeStrength*reliefGate;
                float breath=1+sin(_SmokeTime*_SmokeSpeed*.7+cloud*4)*.035;
                float shadow=smokeShade*_SmokeShadow*(.45+.55*cloud)*breath;
                snowColor-=float3(.32,.25,.17)*shadow;
                snowColor+=smokeGrain*smokeShade;
                float3 imageColor=source.rgb*(1-rim*.045);
                imageColor=lerp(imageColor,imageColor*float3(.72,.80,.90),shadow);
                imageColor+=smokeGrain*smokeShade*.5;
                float4 result=float4(lerp(snowColor,imageColor,reveal),1);
                if(_UsePageInk>0.5)
                {
                    float2 pageUV=(uv-_PageRect.xy)/max(_PageRect.zw,float2(0.001,0.001));
                    if(all(pageUV>=0)&&all(pageUV<=1))
                    {
                        float4 ink=tex2D(_PageTex,pageUV);
                        float margin=smoothstep(0,0.012,min(min(pageUV.x,1-pageUV.x),min(pageUV.y,1-pageUV.y)));
                        result.rgb=lerp(result.rgb,ink.rgb,ink.a*margin*reveal);
                    }
                }
                return lerp(scene,result,_SceneBlend);
            }
            ENDCG
        }
    }
}
