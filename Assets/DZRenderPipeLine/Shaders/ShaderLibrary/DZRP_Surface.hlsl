#ifndef DZRP_SURFACE_INCLUDED
#define DZRP_SURFACE_INCLUDED

/**
 * \brief 表面属性
 */
struct Surface
{
    float3 posWS;
    float3 normal;
    float3 interpolatedNormal;
    float3 viewDirection;
    half3 color;
    half alpha;
    half metallic;
    half smoothness;
    float depth;
    float dither;
    float fresnelStrength;
};

#endif