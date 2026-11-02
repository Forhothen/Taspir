Shader "Taspir/Lobby/Curtain"
{
    Properties
    {
        _BaseMap("Replaceable curtain PNG", 2D) = "white" {}
        _TextureRect("Texture crop (X, Y, Width, Height)", Vector) = (0.265625, 0.065625, 0.32421875, 0.8666667)
        _Tint("Fabric tint", Color) = (1, 0.94, 0.83, 1)
        _Amplitude("Hem sway (world units)", Range(0, 0.3)) = 0.13
        _Period("Sway period (seconds)", Range(3, 30)) = 9
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
            CurtainOutput CurtainVert(CurtainInput input)
            {
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1) + input.motion.y;
                float free = 1 - input.uv.y;
                float weight = free * free;
                float wave = sin(phase - free * 1.8) * 0.75
                    + sin(phase * 1.63 + input.uv.x * 5 - free * 3) * 0.25;
                input.positionOS.x += _Amplitude * input.motion.x * weight * wave;
                input.positionOS.y += _Amplitude * input.motion.x * weight
                    * sin(phase * 1.13 + input.uv.x * 4) * 0.18;
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
                // Warm the neutral placeholder; retain its photographed folds.
                float luminance = dot(fabric.rgb, half3(0.2126, 0.7152, 0.0722));
                fabric.rgb = (lerp(fabric.rgb, luminance.xxx, 0.65) * 0.7 + 0.23) * _Tint.rgb;
                fabric.a *= _Tint.a;
                return fabric;
            }
            ENDHLSL
        }
    }
}
