Shader "Taspir/Lobby/Curtain"
{
    Properties
    {
        _BaseMap("Replaceable curtain PNG", 2D) = "white" {}
        _TextureRect("Texture crop (X, Y, Width, Height)", Vector) = (0, 0, 1, 1)
        _Tint("Tulle tint and opacity", Color) = (1, 0.97, 0.9, 0.3)
        _Amplitude("Fabric sway (world units)", Range(0, 0.3)) = 0.22
        _Period("Sway period (seconds)", Range(3, 30)) = 6.5
        [HideInInspector] _PreviewTime("Preview time (-1 = realtime)", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex CurtainVert
            #pragma fragment Frag
            #include "LobbyQuad.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _TextureRect;
                half4 _Tint;
                float _Amplitude, _Period, _PreviewTime;
            CBUFFER_END
            struct CurtainInput
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 motion : TEXCOORD1; // perspective scale and independent panel phase
            };
            struct CurtainOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 imageUV : TEXCOORD1;
            };
            float Breeze(float2 uv, float phase)
            {
                // A broad gust starts at the free hem and travels toward the fixed header.
                float free = 1 - uv.y;
                return 0.78 * sin(phase + free * 2.4 - uv.x * 3)
                    + 0.22 * sin(phase * 1.47 + free * 4.2 + uv.x * 7);
            }
            CurtainOutput CurtainVert(CurtainInput input)
            {
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1) + input.motion.y;
                float free = 1 - input.uv.y;
                float weight = free * free;
                float wave = Breeze(input.uv, phase);
                float perspective = input.motion.x * lerp(1, 0.4, input.uv.x);
                float billow = _Amplitude * perspective * weight;
                input.positionOS.x += billow * wave * 0.38
                    * sin(input.uv.x * 3.14159265);
                input.positionOS.z += billow * wave;
                // Orthographic depth alone is invisible. Project the lifting hem into the
                // painting too, so forward/back billowing reads from the fixed lobby camera.
                input.positionOS.y += billow * (0.5 + 0.5 * wave) * 0.9;
                float imageX = input.positionOS.x / 13.3333333 + 0.5;
                float sillY = (0.4733116 + 0.08744038 * imageX - 0.5) * 10;
                input.positionOS.y = max(input.positionOS.y, sillY);
                CurtainOutput output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.imageUV = input.positionOS.xy / float2(13.3333333, 10) + 0.5;
                return output;
            }
            half4 Frag(CurtainOutput input) : SV_Target
            {
                // The near panel continues beyond the artwork; keep aspect-ratio bars clear.
                clip(min(min(input.imageUV.x, input.imageUV.y),
                    min(1 - input.imageUV.x, 1 - input.imageUV.y)));
                float2 uv = _TextureRect.xy + input.uv * _TextureRect.zw;
                half4 fabric = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1);
                float free = 1 - input.uv.y;
                float wave = Breeze(input.uv, phase);
                float strength = _Amplitude / 0.22;
                float foldU = input.uv.x + 0.019 * strength * free * free * wave;
                float folds = 0.5 + 0.5 * cos(foldU * 6.2831853 * 19);
                float smallFolds = 0.5 + 0.5 * cos(foldU * 6.2831853 * 37 + 0.8);
                // A subtle weave, antialiased as its threads become subpixel in the distance.
                float2 weavePhase = input.uv * float2(660, 390) * 6.2831853;
                float2 weaveFade = 1 - smoothstep(1, 3.14, fwidth(weavePhase));
                float weave = dot(sin(weavePhase) * weaveFade, float2(0.012, 0.012));
                float header = smoothstep(0.97, 0.986, input.uv.y);
                float hem = 1 - smoothstep(0.006, 0.016, input.uv.y);
                float turning = strength * free * free * wave;
                fabric.rgb *= _Tint.rgb * (0.87 + 0.10 * folds + 0.03 * smallFolds + weave
                    + 0.045 * turning);
                fabric.a *= saturate(_Tint.a * (0.66 + 0.60 * folds + 0.10 * smallFolds)
                    + 0.1 * header + 0.07 * hem + 0.025 * turning);
                return fabric;
            }
            ENDHLSL
        }
    }
}
