#pragma warning (disable : 1519)
// #define QUALITY_LOW

#define SHADERPASS_UNLIT 1
#define SHADERPASS_FORWARDBASE 2
#define SHADERPASS_FORWARDADD 3
#define SHADERPASS_SHADOWCASTER 4
#define SHADERPASS_META 5
#define SHADERPASS_OUTLINE 6

#if defined(UNITY_INSTANCING_ENABLED) || defined(STEREO_INSTANCING_ON) || defined(INSTANCING_ON)
    #define UNITY_ANY_INSTANCING_ENABLED 1
#endif

#ifdef STEREO_INSTANCING_ON
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// #ifdef BUILD_TARGET_ANDROID
// #define UNITY_PBS_USE_BRDF1
// #define QUALITY_LOW
// #endif

#ifndef UNITY_PBS_USE_BRDF1
    #define QUALITY_LOW
#endif

#ifndef QUALITY_LOW
    #define VERTEXLIGHT_PS
#endif

#ifdef SHADER_API_MOBILE
    #define QUALITY_LOW
#endif

#if (SHADERPASS == SHADERPASS_OUTLINE)
    #undef _SSR
    #undef REQUIRE_DEPTH_TEXTURE
    #undef REQUIRE_OPAQUE_TEXTURE
    #undef LTCGI
    #undef _PARALLAXMAP
    #undef _AREALIT
    #define _NORMAL_DROPOFF_TS 0
    #define _NORMAL_DROPOFF_OS 0
    #define _NORMAL_DROPOFF_WS 0
    #define _GLOSSYREFLECTIONS_OFF
    #define _SPECULARHIGHLIGHTS_OFF
#endif

#ifdef QUALITY_LOW
    #undef _SSR
    #undef REQUIRE_DEPTH_TEXTURE
    #undef REQUIRE_OPAQUE_TEXTURE
    #undef LTCGI
    #undef _GEOMETRICSPECULAR_AA
    #undef NONLINEAR_LIGHTMAP_SH
    #undef UNITY_SPECCUBE_BLENDING
    #undef NONLINEAR_LIGHTPROBESH
    #define DISABLE_LIGHT_PROBE_PROXY_VOLUME
    #undef _PARALLAXMAP
    #undef _AREALIT

    #if defined(LIGHTMAP_ON) && !defined(SHADOWS_SHADOWMASK) && !defined(LIGHTMAP_SHADOW_MIXING)
    #undef DIRECTIONAL
    #endif
    #define DISABLE_NONIMPORTANT_LIGHTS_PER_PIXEL
#endif

#ifdef _SSR
    #define REQUIRE_DEPTH_TEXTURE
    #define REQUIRE_OPAQUE_TEXTURE
#endif
//
#ifdef LTCGI_DIFFUSE_OFF
    #define LTCGI_DIFFUSE_DISABLED
    #undef LTCGI_DIFFUSE_OFF
#endif

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    #define FOG_ANY
#endif

#if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)
    #define UNITY_PASS_FORWARD
#endif

#if defined(SHADOWS_SCREEN) || defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
#define VARYINGS_NEED_SHADOWCOORD
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/io.z3y.github.shadergraph/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// prevent redefinition warnings in the shader importer
#undef GLOBAL_CBUFFER_START
#undef SAMPLE_DEPTH_TEXTURE
#undef SAMPLE_DEPTH_TEXTURE_LOD

#include "Packages/io.z3y.github.shadergraph/ShaderLibrary/UnityCG/ShaderVariablesMatrixDefsLegacyUnity.hlsl"

// #undef GLOBAL_CBUFFER_START // dont need reg
// #define GLOBAL_CBUFFER_START(name) CBUFFER_START(name)

#define Unity_SafeNormalize SafeNormalize

#include "UnityShaderVariables.cginc"
half4 _LightColor0;
half4 _SpecColor;

#include "Packages/io.z3y.github.shadergraph/ShaderLibrary/UnityCG/UnityCG.hlsl"
#include "AutoLight.cginc"
// #include "Packages/io.z3y.github.shadergraph/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "UnityShaderUtilities.cginc"

#ifdef UNITY_PASS_META
    #define IsGammaSpace() false
    #include "UnityMetaPass.cginc"
    #undef IsGammaSpace
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// #define CustomLightData UnityLightData
struct UnityLightData
{
    half3 color;
    float3 direction;
    half attenuation;
};


inline float4 ComputeGrabScreenPos(float4 pos)
{
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	float4 o = pos * 0.5f;
	o.xy = float2(o.x, o.y*scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
	o.xy = TransformStereoScreenSpaceTexBuiltIn(o.xy, pos.w);
#endif
	o.zw = pos.zw;
	return o;
}

#define UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED
#define BUILTIN_TARGET_API
float4 _ScaledScreenParams;
float4 _ScaleBiasRt;
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#undef BUILTIN_TARGET_API

// unity macros need workaround
struct LegacyAttributes
{
    float4 vertex;
    float3 normal;
};

struct LegacyVaryings
{
    float4 pos;
    float4 _ShadowCoord;
};

    
struct ShaderData
{
    float3 normalWS;
    float3 bitangentWS;
    float3 tangentWS;
    float3 viewDirectionWS;
    half perceptualRoughness;
    half clampedRoughness;
    half NoV;
    half3 f0;
    half3 brdf;
    half3 energyCompensation;
    float3 reflectionDirection;
};

struct GIData
{
    half3 IndirectDiffuse;
    half3 Light;
    half3 Reflections;
    half3 Specular;
};

#define UNITY_PI PI
#define UNITY_HALF_PI PI/2.
#define UNITY_TWO_PI PI*2