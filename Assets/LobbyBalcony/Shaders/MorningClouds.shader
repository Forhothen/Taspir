Shader "Taspir/Lobby/Morning Clouds"
{
    Properties
    {
        _SkyTop("Upper sky", Color) = (0.43, 0.64, 0.8, 1)
        _SkyHorizon("Morning haze", Color) = (0.88, 0.86, 0.79, 1)
        _CloudColor("Sunlit clouds", Color) = (1, 0.97, 0.9, 1)
        _Sway("Sway in image UV", Range(0, 0.06)) = 0.025
        _Period("Sway period (seconds)", Range(10, 160)) = 32
        [HideInInspector] _PreviewTime("Preview time (-1 = realtime)", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "LobbyQuad.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTop, _SkyHorizon, _CloudColor;
                float _Sway, _Period, _PreviewTime;
            CBUFFER_END
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float n = 0, weight = 0.55;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    n += weight * Noise(p);
                    p = p * 2.03 + float2(12.1, 7.7);
                    weight *= 0.5;
                }
                return n;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float t = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                float phase = t * 6.2831853 / max(_Period, 1);
                float2 uv = input.uv + _Sway * float2(sin(phase), 0.55 * sin(phase * 0.83));
                half3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, smoothstep(0.3, 1, uv.y));
                // Two broad, soft banks below the viewer, plus a few higher wisps.
                float low = Fbm(uv * float2(6, 15) + float2(3.4, 1.7));
                float bank = 1 - smoothstep(0.46, 0.78, uv.y);
                float cloud = smoothstep(0.35, 0.67, low + bank * 0.25);
                cloud *= 1 - smoothstep(0.72, 0.97, uv.y);
                float distant = smoothstep(0.49, 0.7,
                    Fbm(uv * float2(9, 24) + float2(17.2, 4) + _Sway * sin(phase * 0.67) * 8));
                cloud = saturate(cloud + distant * 0.25);
                half3 shade = lerp(_CloudColor.rgb * half3(0.77, 0.84, 0.92),
                    _CloudColor.rgb, smoothstep(0.37, 0.7, low));
                return half4(lerp(sky, shade, cloud), 1);
            }
            ENDHLSL
        }
    }
}
