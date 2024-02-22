#ifndef DZRP_LIGHT_INCLUDED
#define DZRP_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightCols[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirs[MAX_DIRECTIONAL_LIGHT_COUNT];
    //灯光阴影信息，x：阴影强度 y:级联阴影的数量*有阴影灯光的数量 z:Light组件里的normalBias
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightCols[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPos[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDir[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END


struct  Light
{
    float3 color;
    float3 direction;
    float attenuation;
};


int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}
int GetOtherLightCount()
{
    return _OtherLightCount;
}
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength=_DirectionalLightShadowData[lightIndex].x;
    data.tileIndex=_DirectionalLightShadowData[lightIndex].y+shadowData.cascadeIndex;
    data.normalBias=_DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel=_DirectionalLightShadowData[lightIndex].w;
    return data;
}

OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength=_OtherLightShadowData[lightIndex].x;
    data.isPoint= _OtherLightShadowData[lightIndex].z==1.0;
    data.tileIndex=_OtherLightShadowData[lightIndex].y;
    data.ShadowMaskChannel=_OtherLightShadowData[lightIndex].w;
    data.lightPosWS=0.0;
    data.lightDirWS=0.0;
    data.spotDirWS=0.0;
    return data;
}

Light GetDirectionLight(int index,Surface surfaceWS,ShadowData data)
{
    Light light;
    light.color=_DirectionalLightCols[index].rgb;
    light.direction=_DirectionalLightDirs[index].xyz;
    DirectionalShadowData dirShadowData=GetDirectionalShadowData(index,data);
    light.attenuation=GetDirectionalShadowAttenuation(dirShadowData,data,surfaceWS);
    // //debug
    // light.attenuation=data.cascadeIndex*0.25; 
    return light;
}

Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    light.color = _OtherLightCols[index].rgb;
    float3 position =_OtherLightPos[index].xyz;
    float3 ray = position- surfaceWS.posWS;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPos[index].w)));

    float4 spotAngles = _OtherLightSpotAngles[index];
    float3 spotDir=_OtherLightDir[index].xyz;
    float spotAttenuation = Square(saturate(dot(spotDir, light.direction) *
        spotAngles.x + spotAngles.y));
    
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    otherShadowData.lightPosWS=position;
    otherShadowData.spotDirWS=spotDir;
    otherShadowData.lightDirWS=light.direction;

    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) *
        spotAttenuation * rangeAttenuation / distanceSqr;
    return light;
}


#endif