#ifndef DZRP_COMMON_INCLUDED
#define DZRP_COMMON_INCLUDED

#include "DZRP_Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

/**
 * \brief 定义SpaceTransforms中的矩阵源
 */
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject

#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP

#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM

#define UNITY_MATRIX_P glstate_matrix_projection


float Square(float v)
{
 return v * v;
}

float DistaceSquared(float3 pA, float3 pB)
{
 return dot(pA - pB, pA - pB);
}

void ClipLOD(float2 posCS, float fade)
{
 #if defined(LOD_FADE_CROSSFADE)
 float dither=InterleavedGradientNoise(posCS.xy,0);
 clip(fade+(fade<0.0?dither:-dither));
 #endif
}

#if defined(_SHADOW_MASK_ALWAYS) || defined (_SHADOW_MASK_DISTANCE)
#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif