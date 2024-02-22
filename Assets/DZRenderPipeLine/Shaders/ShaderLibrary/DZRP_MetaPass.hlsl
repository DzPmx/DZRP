#ifndef DZRP_META_Pass_INCLUDED
#define DZRP_META_Pass_INCLUDED

#include "DZRP_Surface.hlsl"
#include "DZRP_Shadows.hlsl"
#include "DZRP_Light.hlsl"
#include "DZRP_BRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes 
{
    float4 posOS:POSITION;
    float2 UV0:TEXCOORD0;
    float2 lightMapUV:TEXCOORD1;
};

struct Varyings
{
    float4 posCS:SV_POSITION;
    float2 UV0:VAR_BASE_UV;
};

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
    input.posOS.xy=input.lightMapUV*unity_LightmapST.xy+unity_LightmapST.zw;
    input.posOS.z=input.posOS.z>0.0?FLT_MIN:0.0;
    output.posCS=TransformWorldToHClip(input.posOS);
    output.UV0=TransformBaseUV(input.UV0);
    return output;
}

float4 MetaPassFragment(Varyings input):SV_TARGET
{
    float4 base=GetBase(input.UV0);
    Surface surface;
    ZERO_INITIALIZE(Surface,surface);
    surface.color=base.rgb;
    surface.metallic=GetMetallic(input.UV0);
    surface.smoothness=GetSmoothness(input.UV0);
    BRDF brdf=GetBRDF(surface);
    float4 meta=0;
    
    if (unity_MetaFragmentControl.x)
    {
        meta=float4(brdf.diffuse,1.0);
        meta.rgb+=brdf.specular*brdf.roughness*0.5;
        meta.rgb=min(PositivePow(meta.rgb,unity_OneOverOutputBoost),unity_MaxOutputValue);
    }
    else if(unity_MetaFragmentControl.y)
    {
        meta=float4(GetEmssion(input.UV0),1.0);
    }
    return meta;
}
#endif