#ifndef DZRP_SHADOW_CASTER_PASS_INCLUDE
#define DZRP_SHADOW_CASTER_PASS_INCLUDE


struct Attributes
{
    float4 PosOS:POSITION;
    float3 nDirOS:NORMAL;
    float2 UV0:TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varings
{
    float4 posCS:SV_POSITION;
    float2 UV0:VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;

Varings ShadowCasterPassVertex(Attributes input)
{
    Varings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 posWS = TransformObjectToWorld(input.PosOS).xyz;
    output.posCS = TransformWorldToHClip(posWS);
    if (_ShadowPancaking)
    {
        #if UNITY_REVERSED_Z
        output.posCS.z = min(output.posCS.z, output.posCS.w * UNITY_NEAR_CLIP_VALUE);
        #else
        output.PosCS.z=max(output.PosCS.z,output.PosCS.w*UNITY_NEAR_CLIP_VALUE);
        #endif
    }
    output.UV0 = TransformBaseUV(input.UV0);
    return output;
}

void ShadowCasterPassFragment(Varings input)
{
    UNITY_SETUP_INSTANCE_ID(input)

    float4 base = GetBase(input.UV0);
    #if defined(_SHADOWS_CLIP)
        clip(base.a-GetAlphaClip(input.UV0));
    #elif defined(_SHADOWS_DITHER)
        float dither = InterleavedGradientNoise(input.posCS.xy, 0);
        clip(base.a - dither);
    #endif
    ClipLOD(input.posCS.xy, unity_LODFade.x);
}
#endif
