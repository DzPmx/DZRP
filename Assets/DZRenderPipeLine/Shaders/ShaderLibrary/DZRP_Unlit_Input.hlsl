#ifndef DZRP_UNLIT_INPUT_INCLUDED
#define DZRP_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4,_BaseCol)
    UNITY_DEFINE_INSTANCED_PROP(half,_AlphaClip)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformBaseUV (float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase (float2 baseUV) {
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseCol);
    return map * color;
}

float GetAlphaClip (float2 baseUV) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlphaClip);
}

float GetMetallic (float2 baseUV) {
    return 0.0;
}

float GetSmoothness (float2 baseUV) {
    return 0.0;
}

float GetEmssion(float2 baseUV)
{
    return GetBase(baseUV).rgb;
}
float GetFresnel (float2 baseUV) {
    return 0.0;
}
#endif