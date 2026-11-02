Shader "Taspir/Lobby/Morning Light"
{
    Properties
    {
        _BaseMap("Original balcony (alpha mask)", 2D) = "white" {}
        _Tint("Morning warmth", Color) = (1, 0.83, 0.56, 1)
        _Intensity("Soft light opacity", Range(0, 0.25)) = 0.085
        _Sway("Sway in image UV", Range(0, 0.01)) = 0.0025
        _Period("Sway period (seconds)", Range(20, 160)) = 65
        [HideInInspector] _PreviewTime("Preview time (-1 = realtime)", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "LobbyQuad.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Intensity, _Sway, _Period, _PreviewTime;
            CBUFFER_END
            float Beam(float2 p, float start, float width)
            {
                // Slanted broad strips on the right wall, softly fading at both ends.
                float axis = p.x + 0.46 * p.y;
                float edge = 1 - smoothstep(width * 0.35, width, abs(axis - start));
                return edge * smoothstep(0.39, 0.53, p.y) * (1 - smoothstep(0.77, 0.83, p.y));
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1);
                float2 p = input.uv + _Sway * float2(sin(phase), 0.5 * sin(phase * 0.79));
                float beams = Beam(p, 1.055, 0.037) + Beam(p, 1.17, 0.052) * 0.7
                    + Beam(p, 1.3, 0.04) * 0.45;
                // Protect the framed picture and keep light below the ceiling line.
                float picture = smoothstep(0.667, 0.683, input.uv.x)
                    * (1 - smoothstep(0.801, 0.817, input.uv.x))
                    * smoothstep(0.639, 0.655, input.uv.y)
                    * (1 - smoothstep(0.816, 0.832, input.uv.y));
                float wall = smoothstep(0.598, 0.62, input.uv.x) * (1 - picture);
                float2 poolUV = (p - float2(0.29, 0.31)) / float2(0.31, 0.085);
                float pool = exp(-dot(poolUV, poolUV) * 2) * 0.3;
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                float light = (beams * wall + pool) * alpha * _Intensity
                    * (0.95 + 0.05 * sin(phase + 0.8));
                return half4(_Tint.rgb, light);
            }
            ENDHLSL
        }
    }
}
