Shader "Taspir/Lobby/Morning Light"
{
    Properties
    {
        _BaseMap("Original balcony (alpha mask)", 2D) = "white" {}
        _Tint("Morning warmth", Color) = (1, 0.83, 0.56, 1)
        _Intensity("Soft light intensity", Range(0, 0.4)) = 0.19
        _Sway("Sway in image UV", Range(0, 0.025)) = 0.009
        _Period("Sway period (seconds)", Range(10, 160)) = 24
        _RaySlope("Downward ray slope", Range(0.2, 1.5)) = 0.72
        _Shimmer("Soft shimmer strength", Range(0, 1)) = 0.35
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
                float _Intensity, _Sway, _Period, _PreviewTime, _RaySlope, _Shimmer;
            CBUFFER_END
            float Beam(float2 p, float originHeight, float width, float slope)
            {
                // Project from the window's right edge diagonally down the wooden wall.
                // Rays widen with distance, with a broad feather rather than a hard edge.
                float distance = max(0, p.x - 0.595);
                float axis = p.y + slope * distance;
                float radius = width * (0.6 + distance * 2.5);
                float edge = 1 - smoothstep(0, radius, abs(axis - originHeight));
                return edge * smoothstep(0.59, 0.64, p.x)
                    * (1 - smoothstep(0.97, 1.06, p.x));
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1);
                float2 p = input.uv + _Sway * float2(sin(phase), 0.5 * sin(phase * 0.79));
                float slope = _RaySlope + 0.055 * sin(phase * 0.87);
                // Different phases make each shaft breathe; slow waves travel along it.
                float shimmerA = 1 - _Shimmer * (0.5 + 0.5 * sin(t * 0.72 - p.x * 12));
                float shimmerB = 1 - _Shimmer * (0.5 + 0.5 * sin(t * 0.61 - p.x * 9 + 2.1));
                float shimmerC = 1 - _Shimmer * (0.5 + 0.5 * sin(t * 0.81 - p.x * 11 + 4.2));
                float beams = Beam(p, 0.8, 0.035, slope) * shimmerA
                    + Beam(p, 0.705, 0.026, slope) * shimmerB * 0.85
                    + Beam(p, 0.61, 0.038, slope) * shimmerC * 0.65;
                // Protect the framed picture and keep light below the ceiling line.
                float picture = smoothstep(0.667, 0.683, input.uv.x)
                    * (1 - smoothstep(0.801, 0.817, input.uv.x))
                    * smoothstep(0.639, 0.655, input.uv.y)
                    * (1 - smoothstep(0.816, 0.832, input.uv.y));
                float wall = smoothstep(0.598, 0.62, input.uv.x) * (1 - picture)
                    * smoothstep(0.40, 0.47, input.uv.y)
                    * (1 - smoothstep(0.81, 0.835, input.uv.y - 0.35 * (input.uv.x - 0.6)));
                float2 poolUV = (p - float2(0.29, 0.31)) / float2(0.31, 0.085);
                float pool = exp(-dot(poolUV, poolUV) * 2) * 0.55;
                float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                float light = (beams * wall + pool) * alpha * _Intensity
                    * (0.88 + 0.12 * sin(phase + 0.8));
                return half4(_Tint.rgb, light);
            }
            ENDHLSL
        }
    }
}
