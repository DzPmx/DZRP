#ifndef DZRP_LIT_PASS_INCLUDED
#define DZRP_LIT_PASS_INCLUDED

#include "ShaderLibrary/DZRP_Surface.hlsl"
#include "ShaderLibrary/DZRP_Shadows.hlsl"
#include "ShaderLibrary/DZRP_Light.hlsl"
#include "ShaderLibrary/DZRP_BRDF.hlsl"
#include "ShaderLibrary/DZRP_GI.hlsl"
#include "ShaderLibrary/DZRP_Lighting.hlsl"

struct Attributes
{
    float4 posOS:POSITION;
    float3 nDirOS:NORMAL;
    float2 UV0:TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS:SV_POSITION;
    float3 posWS:TEXCOORD0;
    float3 nDirWS:VAR_NORMAL;
    float2 UV0:VAR_BASE_UV;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings DZLitPassVertex(Attributes input)
{
    Varyings output;
    //获取实例ID 需要定义在矩阵变换前
    UNITY_SETUP_INSTANCE_ID(input);
    //将ID从Input传到Output
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.UV0 = TransformBaseUV(input.UV0);
    output.posWS = TransformObjectToWorld(input.posOS.xyz);
    output.posCS = TransformWorldToHClip(output.posWS.xyz);
    output.nDirWS = TransformObjectToWorldNormal(input.nDirOS);
    return output;
}

float4 DZLitPassFrag(Varyings input):SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input); //提取实例ID
    float4 base = GetBase(input.UV0);

    #if defined(_CLIPPING)
        clip(base.a-GetAlphaClip(input.UV0));
    #endif

    Surface surface;
    surface.posWS = input.posWS;
    surface.normal = normalize(input.nDirWS);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.posWS.xyz);
    surface.depth = -TransformWorldToView(input.posWS).z;
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = GetMetallic(input.UV0);
    surface.smoothness = GetSmoothness(input.UV0);
    surface.dither = InterleavedGradientNoise(input.posCS.xy, 0);
    surface.fresnelStrength = GetFresnel(input.UV0);
    surface.interpolatedNormal = input.nDirWS;

    #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
    #else
    BRDF brdf=GetBRDF(surface);
    #endif

    GI gi = getGI(GI_FRAGMENT_DATA(input), surface, brdf);

    float3 color = GetLighting(surface, brdf, gi);
    color += GetEmssion(input.UV0);
    ClipLOD(input.posCS.xy, unity_LODFade.x);
    return float4(color, surface.alpha);
}
#endif
