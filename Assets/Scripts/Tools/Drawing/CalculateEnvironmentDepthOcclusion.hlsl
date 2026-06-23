#pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"
#endif
#include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

void CalculateEnvironmentDepthOcclusion_float(float3 posWorld, out float occlusionValue)
{
    #ifndef SHADERGRAPH_PREVIEW
    occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(posWorld, 0.0);
    #else
    occlusionValue = 1;
    #endif
}

void CalculateEnvironmentDepthOcclusion_half(float3 posWorld, out half occlusionValue)
{
    #ifndef SHADERGRAPH_PREVIEW
    occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(posWorld, 0.0);
    #else
    occlusionValue = 1;
    #endif
}
