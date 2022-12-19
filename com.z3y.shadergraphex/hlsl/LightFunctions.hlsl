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
    return k * k * (1.0 / UNITY_PI);
}

float D_GGX_Anisotropic(float NoH, float3 h, float3 t, float3 b, float at, float ab)
{
    half ToH = dot(t, h);
    half BoH = dot(b, h);
    half a2 = at * ab;
    float3 v = float3(ab * ToH, at * BoH, a2 * NoH);
    float v2 = dot(v, v);
    half w2 = a2 / v2;
    return a2 * w2 * w2 * (1.0 / UNITY_PI);
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
    #ifdef SHADER_API_MOBILE
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

// static float2 DFGLut;
// static half3 DFGEnergyCompensation;

// half4 SampleDFG(half NoV, half perceptualRoughness)
// {
//     return _DFG.SampleLevel(sampler_DFG, float2(NoV, perceptualRoughness), 0);
// }

// half3 EnvBRDF(half2 dfg, half3 f0)
// {
//     return f0 * dfg.x + dfg.y;
// }

// half3 EnvBRDFMultiscatter(half2 dfg, half3 f0)
// {
//     return lerp(dfg.xxx, dfg.yyy, f0);
// }

// half3 EnvironmentBRDFEnergyCompensation(half2 dfg, half3 f0)
// {
//     return 1.0 + f0 * (1.0 / dfg.y - 1.0);
// }

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

#ifndef SHADER_API_MOBILE
TEXTURE2D(_DFG);
SAMPLER(sampler_DFG);
#endif

void EnvironmentBRDF(half NoV, half perceptualRoughness, half3 f0, out half3 brdf, out half3 energyCompensation)
{
    #ifdef SHADER_API_MOBILE
        energyCompensation = 1.0;
        brdf = EnvironmentBRDFApproximation(perceptualRoughness, NoV, f0);
    #else
        float2 dfg = _DFG.SampleLevel(sampler_DFG, float2(NoV, perceptualRoughness), 0);
        brdf = lerp(dfg.xxx, dfg.yyy, f0);
        energyCompensation = 1.0 + f0 * (1.0 / dfg.y - 1.0);
    #endif
}