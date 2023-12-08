// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// #define TERRAIN_STANDARD_SHADER
#ifndef TERRAIN_SPLATMAP_COMMON_HLSL_INCLUDED
#define TERRAIN_SPLATMAP_COMMON_HLSL_INCLUDED


CBUFFER_START(_Terrain)
    float4 _Splat0_ST;
    float4 _Splat1_ST;
    float4 _Splat2_ST;
    float4 _Splat3_ST;

    float4 _Control_ST;
    float4 _Control_TexelSize;
    
    half _NormalScale0;
    half _NormalScale1;
    half _NormalScale2;
    half _NormalScale3;

    half _Metallic0;
    half _Metallic1;
    half _Metallic2;
    half _Metallic3;

    half _Smoothness0;
    half _Smoothness1;
    half _Smoothness2;
    half _Smoothness3;

    half _HeightTransition;

    #if defined(UNITY_INSTANCING_ENABLED)
    float4 _TerrainHeightmapRecipSize; // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale; // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
    #endif
CBUFFER_END

UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData) // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

#if defined(UNITY_INSTANCING_ENABLED)
    TEXTURE2D(_TerrainHeightmapTexture);
    SAMPLER(sampler_TerrainHeightmapTexture);
    TEXTURE2D(_TerrainNormalmapTexture);
    SAMPLER(sampler_TerrainNormalmapTexture);
#endif


TEXTURE2D(_Control); SAMPLER(sampler_Control);
TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

#ifdef _NORMALMAP
TEXTURE2D(_Normal0); SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
#endif

#ifdef _MASKMAP
TEXTURE2D(_Mask0); SAMPLER(sampler_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);
#endif

#ifdef _ALPHATEST_ON
    TEXTURE2D(_TerrainHolesTexture);
    SAMPLER(sampler_TerrainHolesTexture);

    void ClipHoles(float2 uv)
    {
        float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
        clip(hole == 0.0f ? -1 : 1);
    }
#endif

void TerrainInstancing(inout float3 positionOS, inout float3 normalOS, inout float2 uv)
{
    #ifdef UNITY_INSTANCING_ENABLED
        float2 patchVertex = positionOS.xy;
        float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
    
        float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
        float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));
    
        positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
        positionOS.y = height * _TerrainHeightmapScale.y;
    
        #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
            normalOS = float3(0, 1, 0);
        #else
            normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
        #endif
            uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
    #endif
}

float4 ComputeTerrainTangent(float3 normalOS)
{
    float4 vertexTangent = float4(cross(normalOS, float3(0.0, 0.0, 1.0)), 1.0);
    return vertexTangent;
}

void NormalMapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, inout half3 mixedNormal)
{
    #if defined(_NORMALMAP)
        half3 nrm = half(0.0);
        nrm += splatControl.r * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy), _NormalScale0);
        nrm += splatControl.g * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw), _NormalScale1);
        nrm += splatControl.b * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy), _NormalScale2);
        nrm += splatControl.a * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw), _NormalScale3);
 
        // avoid risk of NaN when normalizing.
        #if HAS_HALF
            nrm.z += half(0.01);
        #else
            nrm.z += 1e-5f;
        #endif
 
        mixedNormal = normalize(nrm.xyz);
    #endif
}

void SplatmapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness, inout half3 mixedNormal)
{
    half4 diffAlbedo[4];
 
    diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
    diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
    diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
    diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);
 
    // This might be a bit of a gamble -- the assumption here is that if the diffuseMap has no
    // alpha channel, then diffAlbedo[n].a = 1.0 (and _DiffuseHasAlphaN = 0.0)
    // Prior to coming in, _SmoothnessN is actually set to max(_DiffuseHasAlphaN, _SmoothnessN)
    // This means that if we have an alpha channel, _SmoothnessN is locked to 1.0 and
    // otherwise, the true slider value is passed down and diffAlbedo[n].a == 1.0.
    defaultSmoothness = half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a);
    defaultSmoothness *= half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
 
#ifndef _TERRAIN_BLEND_HEIGHT // density blending
    // // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    // half4 opacityAsDensity = saturate((half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a) - (1 - splatControl)) * 20.0);
    // opacityAsDensity += 0.001h * splatControl;      // if all weights are zero, default to what the blend mask says
    // half4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
    // splatControl = lerp(opacityAsDensity, splatControl, useOpacityAsDensityParam);
#endif
 
    // Now that splatControl has changed, we can compute the final weight and normalize
    weight = dot(splatControl, 1.0);
 
#ifdef TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0 : 1.0);
#endif
 
#ifndef _TERRAIN_BASEMAP_GEN
    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);
#endif
 
    // mixedDiffuse = 0.0;
    // mixedDiffuse += diffAlbedo[0] * half4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0);
    // mixedDiffuse += diffAlbedo[1] * half4(_DiffuseRemapScale1.rgb * splatControl.ggg, 1.0);
    // mixedDiffuse += diffAlbedo[2] * half4(_DiffuseRemapScale2.rgb * splatControl.bbb, 1.0);
    // mixedDiffuse += diffAlbedo[3] * half4(_DiffuseRemapScale3.rgb * splatControl.aaa, 1.0);
    mixedDiffuse = 0.0;
    mixedDiffuse += diffAlbedo[0] * half4(splatControl.rrr, 1.0);
    mixedDiffuse += diffAlbedo[1] * half4(splatControl.ggg, 1.0);
    mixedDiffuse += diffAlbedo[2] * half4(splatControl.bbb, 1.0);
    mixedDiffuse += diffAlbedo[3] * half4(splatControl.aaa, 1.0);
 
    NormalMapMix(uvSplat01, uvSplat23, splatControl, mixedNormal);
}

void HeightBasedSplatModify(inout half4 splatControl, half4 masks)
{
    half4 splatHeight = masks * splatControl.rgba;
    half maxHeight = max(splatHeight.r, max(splatHeight.g, max(splatHeight.b, splatHeight.a)));

    // Ensure that the transition height is not zero.
    half transition = max(_HeightTransition, 1e-5);

    // This sets the highest splat to "transition", and everything else to a lower value relative to that, clamping to zero
    // Then we clamp this to zero and normalize everything
    half4 weightedHeights = splatHeight + transition - maxHeight.xxxx;
    weightedHeights = max(0, weightedHeights);

    // We need to add an epsilon here for active layers (hence the blendMask again)
    // so that at least a layer shows up if everything's too low.
    weightedHeights = (weightedHeights + 1e-6) * splatControl;

    // Normalize (and clamp to epsilon to keep from dividing by zero)
    half sumHeight = max(dot(weightedHeights, half4(1, 1, 1, 1)), 1e-6);
    splatControl = weightedHeights / sumHeight.xxxx;
}

#endif
