#ifndef DZRP_GI_INCLUDED
#define DZRP_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#if defined(LIGHTMAP_ON)
#define GI_ATTRIBUTE_DATA   float2 lightMapUV :TEXCOORD1;
#define GI_VARYINGS_DATA    float2 lightMapUV :VAR_LIGHT_MAP_UV;
#define TRANSFER_GI_DATA(input,output) output.lightMapUV=input.lightMapUV*unity_LightmapST.xy+unity_LightmapST.zw;
#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

TEXTURE2D(unity_Lightmap);SAMPLER(samplerunity_Lightmap);
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);SAMPLER(samplerunity_ProbeVolumeSH);
TEXTURE2D(unity_ShadowMask);SAMPLER(samplerunity_ShadowMask);
TEXTURECUBE(unity_SpecCube0);SAMPLER(samplerunity_SpecCube0);

struct GI
{
    float3 diffuse;
    float3 specular;
    ShadowMask shadowMask;
};

float3 SamplerLightMap(float2 lightMapUV)
{
    #if defined(LIGHTMAP_ON)
        return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap,samplerunity_Lightmap),
            lightMapUV,float4(1.0,1.0,0.0,0.0),
            #if defined(UNITY_LIGHTMAP_FULL_HDR)//是否使用不压缩lightMap
                false,
            #else
                true,
            #endif
                float4 (LIGHTMAP_HDR_MULTIPLIER,LIGHTMAP_HDR_EXPONENT,0.0,0.0));//解码指令
   #else
        return 0.0;
   #endif
    
}

float3 SamplerLightProbe(Surface surfaceWS)
{
    #if defined(LIGHTMAP_ON)
        return 0.0;
    #else
        if (unity_ProbeVolumeParams.x)//x决定是否适用lppv
        {
            return SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),
                surfaceWS.posWS,surfaceWS.normal,unity_ProbeVolumeWorldToObject, unity_ProbeVolumeParams.y,
                unity_ProbeVolumeParams.z,unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz);
        }
        else
        {
            float4 coefficents[7];
            coefficents[0] = unity_SHAr;
            coefficents[1] = unity_SHAg;
            coefficents[2] = unity_SHAb;
            coefficents[3] = unity_SHBr;
            coefficents[4] = unity_SHBg;
            coefficents[5] = unity_SHBb;
            coefficents[6] = unity_SHC;
            return max(0.0, SampleSH9(coefficents, surfaceWS.normal));
        }
    #endif
}

float3 SampleEnvironment(Surface surfaceWS,BRDF brdf)
{
    float3 uvw=reflect(-surfaceWS.viewDirection,surfaceWS.normal);
    float mip=PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness);
    float4 environment=SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0,uvw,mip);
    return DecodeHDREnvironment(environment,unity_SpecCube0_HDR);
}

float4 SampleBakedShadows(float2 lightMapUV,Surface surfaceWS)
{
    #if defined(LIGHTMAP_ON)
        return SAMPLE_TEXTURE2D(unity_ShadowMask,samplerunity_ShadowMask,lightMapUV);
    #else
    {
        if (unity_ProbeVolumeParams.x)//x决定是否适用lppv
        {
            return SampleProbeOcclusion(TEXTURE3D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),
                surfaceWS.posWS,unity_ProbeVolumeWorldToObject,unity_ProbeVolumeParams.y,unity_ProbeVolumeParams.z,
                unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz);
        }
        else
        {
            return unity_ProbesOcclusion;
        }
    }
    #endif
}

GI getGI(float2 lightMapUV,Surface surfaceWS,BRDF brdf)
{
    GI gi;
    gi.diffuse=SamplerLightMap(lightMapUV)+SamplerLightProbe(surfaceWS);
    gi.specular=SampleEnvironment(surfaceWS,brdf);
    gi.shadowMask.distance=false;
    gi.shadowMask.always=false;
    gi.shadowMask.shadows=1.0;
    #if defined(_SHADOW_MASK_ALWAYS)
        gi.shadowMask.always=true;
        gi.shadowMask.shadows=SampleBakedShadows(lightMapUV,surfaceWS);
    #elif defined(_SHADOW_MASK_DISTANCE)
        gi.shadowMask.distance=true;
        gi.shadowMask.shadows=SampleBakedShadows(lightMapUV,surfaceWS);
    #endif
    return gi;
}


#endif