#ifndef TASPIR_LOBBY_QUAD_INCLUDED
#define TASPIR_LOBBY_QUAD_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};
Varyings Vert(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}
#endif
