#ifndef DZRP_UNLIT_PASS_INCLUDE
#define DZRP_UNLIT_PASS_INCLUDE

#include "ShaderLibrary/DZRP_Common.hlsl"

struct Attributes 
{
    float4 posOS:POSITION;
    float2 UV0:TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS:SV_POSITION;
    float2 UV0:VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings DZUnlitPassVertex(Attributes input)
{
    Varyings output;
    //获取实例ID 需要定义在矩阵变换前
    UNITY_SETUP_INSTANCE_ID(input);
    //将ID从Input传到Output
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    output.UV0=TransformBaseUV(input.UV0);
    output.posCS=TransformObjectToHClip(input.posOS.xyz);
    return output;
}
float4 DZUnlitPassFrag(Varyings input):SV_TARGET
{

    //提取实例ID
    UNITY_SETUP_INSTANCE_ID(input);
    float4 base=GetBase(input.UV0);
    //获取每个实例的属性数据
    
    #if defined(_CLIPING)
        clip(base.a-GetAlphaClip(input.UV0));
    #endif
    
    return base;

}
#endif
