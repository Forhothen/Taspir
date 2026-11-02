Shader "Taspir/Lobby/Balcony Unlit"
{
    Properties { _BaseMap("Original balcony PNG", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "LobbyQuad.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
            }
            ENDHLSL
        }
    }
}
