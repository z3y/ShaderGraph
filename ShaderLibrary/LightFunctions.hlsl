#ifndef LIGHTFUNCTIONS_INCLUDED
#define LIGHTFUNCTIONS_INCLUDED

namespace Filament
{
    // License included at FilamentLicense.md
    float pow5(float x)
    {
        float x2 = x * x;
        return x2 * x2 * x;
    }

    float sq(float x)
    {
        return x * x;
    }

    half3 F_Schlick(half u, half3 f0)
    {
        return f0 + (1.0 - f0) * pow(1.0 - u, 5.0);
    }

    float F_Schlick(float f0, float f90, float VoH)
    {
        return f0 + (f90 - f0) * pow5(1.0 - VoH);
    }

    half Fd_Burley(half roughness, half NoV, half NoL, half LoH)
    {
        // Burley 2012, "Physically-Based Shading at Disney"
        half f90 = 0.5 + 2.0 * roughness * LoH * LoH;
        float lightScatter = F_Schlick(1.0, f90, NoL);
        float viewScatter  = F_Schlick(1.0, f90, NoV);
        return lightScatter * viewScatter;
    }

    half computeSpecularAO(half NoV, half ao, half roughness)
    {
        return clamp(pow(NoV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao, 0.0, 1.0);
    }

    half D_GGX(half NoH, half roughness)
    {
        half a = NoH * roughness;
        half k = roughness / (1.0 - NoH * NoH + a * a);
        return k * k * (1.0 / PI);
    }

    float D_GGX_Anisotropic(float NoH, float3 h, float3 t, float3 b, float at, float ab)
    {
        half ToH = dot(t, h);
        half BoH = dot(b, h);
        half a2 = at * ab;
        float3 v = float3(ab * ToH, at * BoH, a2 * NoH);
        float v2 = dot(v, v);
        half w2 = a2 / v2;
        return a2 * w2 * w2 * (1.0 / PI);
    }


    float V_SmithGGXCorrelatedFast(half NoV, half NoL, half roughness)
    {
        half a = roughness;
        float GGXV = NoL * (NoV * (1.0 - a) + a);
        float GGXL = NoV * (NoL * (1.0 - a) + a);
        return 0.5 / (GGXV + GGXL);
    }

    float V_SmithGGXCorrelated(half NoV, half NoL, half roughness)
    {
        #ifdef QUALITY_LOW
            return V_SmithGGXCorrelatedFast(NoV, NoL, roughness);
        #else
            half a2 = roughness * roughness;
            float GGXV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
            float GGXL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
            return 0.5 / (GGXV + GGXL);
        #endif
    }

    float V_SmithGGXCorrelated_Anisotropic(float at, float ab, float ToV, float BoV, float ToL, float BoL, float NoV, float NoL)
    {
        float lambdaV = NoL * length(float3(at * ToV, ab * BoV, NoV));
        float lambdaL = NoV * length(float3(at * ToL, ab * BoL, NoL));
        float v = 0.5 / (lambdaV + lambdaL);
        return saturate(v);
    }

    half V_Kelemen(half LoH)
    {
        // Kelemen 2001, "A Microfacet Based Coupled Specular-Matte BRDF Model with Importance Sampling"
        return saturate(0.25 / (LoH * LoH));
    }

    
    half GeometricSpecularAA(float3 worldNormal, half perceptualRoughness, half varianceIn, half thresholdIn)
    {
        // Kaplanyan 2016, "Stable specular highlights"
        // Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
        // Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"

        // This implementation is meant for deferred rendering in the original paper but
        // we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
        // 2019). The main reason is that the forward version requires an expensive transform
        // of the half vector by the tangent frame for every light. This is therefore an
        // approximation but it works well enough for our needs and provides an improvement
        // over our original implementation based on Vlachos 2015, "Advanced VR Rendering".

        float3 du = ddx(worldNormal);
        float3 dv = ddy(worldNormal);

        half variance = varianceIn * (dot(du, du) + dot(dv, dv));

        half roughness = perceptualRoughness * perceptualRoughness;
        half kernelRoughness = min(2.0 * variance, thresholdIn);
        half squareRoughness = saturate(roughness * roughness + kernelRoughness);

        return sqrt(sqrt(squareRoughness));
    }
}

float Unity_Dither(float In, float2 ScreenPosition)
{
    float2 uv = ScreenPosition * _ScreenParams.xy;
    const half4 DITHER_THRESHOLDS[4] =
    {
        half4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
        half4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
        half4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
        half4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0)
    };

    return In - DITHER_THRESHOLDS[uint(uv.x) % 4][uint(uv.y) % 4];
}

half3 EnvironmentBRDFApproximation(half perceptualRoughness, half NoV, half3 f0)
{
    // original code from https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_notes.pdf
    half g = 1 - perceptualRoughness;
    half4 t = half4(1 / 0.96, 0.475, (0.0275 - 0.25 * 0.04) / 0.96, 0.25);
    t *= half4(g, g, g, g);
    t += half4(0.0, 0.0, (0.015 - 0.75 * 0.04) / 0.96, 0.75);
    half a0 = t.x * min(t.y, exp2(-9.28 * NoV)) + t.z;
    half a1 = t.w;
    return saturate(lerp(a0, a1, f0));
}

#ifndef QUALITY_LOW
// TEXTURE2D(_DFG);
// SAMPLER(sampler_DFG);
#endif

void EnvironmentBRDF(half NoV, half perceptualRoughness, half3 f0, out half3 brdf, out half3 energyCompensation)
{
    #if defined(QUALITY_LOW)// || defined(GENERATION_GRAPH)
        energyCompensation = 1.0;
        brdf = EnvironmentBRDFApproximation(perceptualRoughness, NoV, f0);
    #else
        float2 dfg = SAMPLE_TEXTURE2D_LOD(_DFG, sampler_DFG, float2(NoV, perceptualRoughness), 0).rg;
        brdf = lerp(dfg.xxx, dfg.yyy, f0);
        energyCompensation = 1.0 + f0 * (1.0 / dfg.y - 1.0);
    #endif
}

SamplerState custom_bilinear_clamp_sampler;

#include "Bicubic.hlsl"

#ifdef DYNAMICLIGHTMAP_ON
half3 RealtimeLightmap(float2 uv, float3 worldNormal)
{   
    //half4 bakedCol = SampleBicubic(unity_DynamicLightmap, custom_bilinear_clamp_sampler, uv);
    half4 bakedCol = BicubicSampling::SampleBicubic(unity_DynamicLightmap, custom_bilinear_clamp_sampler, uv, BicubicSampling::GetTexelSize(unity_DynamicLightmap));
    
    half3 realtimeLightmap = DecodeRealtimeLightmap(bakedCol);
    #ifdef DIRLIGHTMAP_COMBINED
        float4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, uv);
        realtimeLightmap += DecodeDirectionalLightmap(realtimeLightmap, realtimeDirTex, worldNormal);
    #endif
    return realtimeLightmap;
}
#endif

float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
{
    // average energy
    float R0 = L0;
    
    // avg direction of incoming light
    float3 R1 = 0.5f * L1;
    
    // directional brightness
    float lenR1 = length(R1);
    
    // linear angle between normal and direction 0-1
    //float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
    //float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
    float q = dot(normalize(R1), n) * 0.5 + 0.5;
    q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
    
    // power for q
    // lerps from 1 (linear) to 3 (cubic) based on directionality
    float p = 1.0f + 2.0f * lenR1 / R0;
    
    // dynamic range constant
    // should vary between 4 (highly directional) and 0 (ambient)
    float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
    
    return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
}

#ifdef DISABLE_LIGHT_PROBE_PROXY_VOLUME
    #define UNITY_LIGHT_PROBE_PROXY_VOLUME 0
#endif

half3 GetLightProbes(float3 normalWS, float3 positionWS, half3 ambient)
{
    #ifdef FLATLIT
        float3 sh9Dir = (unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz) * 0.333333;
        float3 sh9DirAbs = float3(sh9Dir.x, abs(sh9Dir.y), sh9Dir.z);
        half3 N = normalize(sh9DirAbs) * 0.6667;
        UNITY_FLATTEN
        if (!any(unity_SHC.xyz))
        {
            N = 0;
        }
        half3 l0l1 = SHEvalLinearL0L1(float4(N, 1));
        half3 l2 = SHEvalLinearL2(float4(N, 1));
        return max(l0l1 + l2, 0.0);
    #endif

    #if !defined(UNITY_PASS_FORWARDBASE) && defined(PIPELINE_BUILTIN)
        return 0.0f;
    #endif

    #ifdef LIGHTMAP_ON
        return 0.0f;
    #endif

    half3 indirectDiffuse = 0;

    #ifdef NONLINEAR_LIGHTPROBESH
        float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
        indirectDiffuse.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normalWS);
        indirectDiffuse.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normalWS);
        indirectDiffuse.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normalWS);
    #else
        indirectDiffuse = ShadeSHPerPixel(normalWS, ambient, positionWS);
    #endif

    #if defined(PIPELINE_URP)
        indirectDiffuse = SampleSH(normalWS);
    #endif

    indirectDiffuse = max(0.0, indirectDiffuse);
    return indirectDiffuse;
}

#ifdef PIPELINE_BUILTIN


#endif

#include "Bakery.hlsl"

#define UNIVERSAL_SPEEDTREE_UTILITY
uint2 ComputeFadeMaskSeed(float3 V, uint2 positionSS)
{
    uint2 fadeMaskSeed;

    // Is this a reasonable quality gate?
#if defined(SHADER_QUALITY_HIGH)
    if (IsPerspectiveProjection())
    {
        // Start with the world-space direction V. It is independent from the orientation of the camera,
        // and only depends on the position of the camera and the position of the fragment.
        // Now, project and transform it into [-1, 1].
        float2 pv = PackNormalOctQuadEncode(V);
        // Rescale it to account for the resolution of the screen.
        pv *= _ScreenParams.xy;
        // The camera only sees a small portion of the sphere, limited by hFoV and vFoV.
        // Therefore, we must rescale again (before quantization), roughly, by 1/tan(FoV/2).
        pv *= UNITY_MATRIX_P._m00_m11;
        // Truncate and quantize.
        fadeMaskSeed = asuint((int2)pv);
    }
    else
#endif
    {
        // Can't use the view direction, it is the same across the entire screen.
        fadeMaskSeed = positionSS;
    }

    return fadeMaskSeed;
}

#if defined(VERTEXLIGHT_ON)
void NonImportantLightsPerPixel(inout half3 lightColor, inout half3 directSpecular, float3 positionWS, float3 normalWS, float3 viewDir, half NoV, half3 f0, ShaderData sd)
{
    half clampedRoughness = max(sd.perceptualRoughness * sd.perceptualRoughness, 0.002);

    // Original code by Xiexe
    // https://github.com/Xiexe/Xiexes-Unity-Shaders

    // MIT License

    // Copyright (c) 2019 Xiexe

    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:

    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.

    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.

    float4 toLightX = unity_4LightPosX0 - positionWS.x;
    float4 toLightY = unity_4LightPosY0 - positionWS.y;
    float4 toLightZ = unity_4LightPosZ0 - positionWS.z;

    float4 lengthSq = 0.0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;

    #if 0
        float4 attenuation = 1.0 / (1.0 + lengthSq * unity_4LightAtten0);
        float4 atten2 = saturate(1 - (lengthSq * unity_4LightAtten0 / 25.0));
        attenuation = min(attenuation, atten2 * atten2);
    #else
        // https://forum.unity.com/threads/point-light-in-v-f-shader.499717/
        float4 range = 5.0 * (1.0 / sqrt(unity_4LightAtten0));
        float4 attenUV = sqrt(lengthSq) / range;
        float4 attenuation = saturate(1.0 / (1.0 + 25.0 * attenUV * attenUV) * saturate((1 - attenUV) * 5.0));
    #endif

    [unroll(4)]
    for (uint i = 0; i < 4; i++)
    {
        UNITY_BRANCH
        if (attenuation[i] > 0.0)
        {
            float3 direction = normalize(float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]) - positionWS);
            half NoL = saturate(dot(normalWS, direction));
            half3 color = NoL * attenuation[i] * unity_LightColor[i];
            lightColor += color;

            #ifndef _SPECULARHIGHLIGHTS_OFF
                float3 halfVector = Unity_SafeNormalize(direction + viewDir);
                half vNoH = saturate(dot(normalWS, halfVector));
                half vLoH = saturate(dot(direction, halfVector));

                half3 Fv = Filament::F_Schlick(vLoH, f0);
                half Dv = Filament::D_GGX(vNoH, clampedRoughness);
                half Vv = Filament::V_SmithGGXCorrelatedFast(NoV, NoL, clampedRoughness);
                directSpecular += max(0.0, (Dv * Vv) * Fv) * color;
            #endif
        }
    }
}
#endif // #if defined(VERTEXLIGHT_ON)


// Box Projection from URP
// Copyright © 2020 Unity Technologies ApS
// Licensed under the Unity Companion License for Unity-dependent projects--see Unity Companion License.
// Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.
real PerceptualRoughnessToMipmapLevel(real perceptualRoughness, uint maxMipLevel)
{
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);

    return perceptualRoughness * maxMipLevel;
}

real PerceptualRoughnessToMipmapLevel(real perceptualRoughness)
{
    return PerceptualRoughnessToMipmapLevel(perceptualRoughness, UNITY_SPECCUBE_LOD_STEPS);
}

float CalculateProbeWeight(float3 positionWS, float4 probeBoxMin, float4 probeBoxMax)
{
    float blendDistance = probeBoxMax.w;
    float3 weightDir = min(positionWS - probeBoxMin.xyz, probeBoxMax.xyz - positionWS) / blendDistance;
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

half CalculateProbeVolumeSqrMagnitude(float4 probeBoxMin, float4 probeBoxMax)
{
    half3 maxToMin = half3(probeBoxMax.xyz - probeBoxMin.xyz);
    return dot(maxToMin, maxToMin);
}

real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half probe0Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    half probe1Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    half volumeDiff = probe0Volume - probe1Volume;
    float importanceSign = unity_SpecCube1_BoxMin.w;

    // A probe is dominant if its importance is higher
    // Or have equal importance but smaller volume
    bool probe0Dominant = importanceSign > 0.0f || (importanceSign == 0.0f && volumeDiff < -0.0001h);
    bool probe1Dominant = importanceSign < 0.0f || (importanceSign == 0.0f && volumeDiff > 0.0001h);

    float desiredWeightProbe0 = CalculateProbeWeight(positionWS, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    float desiredWeightProbe1 = CalculateProbeWeight(positionWS, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    // Subject the probes weight if the other probe is dominant
    float weightProbe0 = probe1Dominant ? min(desiredWeightProbe0, 1.0f - desiredWeightProbe1) : desiredWeightProbe0;
    float weightProbe1 = probe0Dominant ? min(desiredWeightProbe1, 1.0f - desiredWeightProbe0) : desiredWeightProbe1;

    float totalWeight = weightProbe0 + weightProbe1;

    // If either probe 0 or probe 1 is dominant the sum of weights is guaranteed to be 1.
    // If neither is dominant this is not guaranteed - only normalize weights if totalweight exceeds 1.
    weightProbe0 /= max(totalWeight, 1.0f);
    weightProbe1 /= max(totalWeight, 1.0f);

    // Sample the first reflection probe
    if (weightProbe0 > 0.01f)
    {
        half3 reflectVector0 = reflectVector;
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
        reflectVector0 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
#endif // UNITY_SPECCUBE_BOX_PROJECTION

        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector0, mip));

        irradiance += weightProbe0 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    }

    // Sample the second reflection probe
#ifdef UNITY_SPECCUBE_BLENDING
    UNITY_BRANCH
    if (weightProbe1 > 0.01f)
    {
        half3 reflectVector1 = reflectVector;
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
        reflectVector1 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
#endif // UNITY_SPECCUBE_BOX_PROJECTION
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube0, reflectVector1, mip));

        irradiance += weightProbe1 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube1_HDR);
    }
#endif

    return irradiance;
}

half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion)
{
    half3 irradiance;

#if defined(UNITY_SPECCUBE_BLENDING)
    irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness);
#else
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
    reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
#endif // UNITY_SPECCUBE_BOX_PROJECTION
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
#endif // UNITY_SPECCUBE_BLENDING
    return irradiance * occlusion;
}

#endif