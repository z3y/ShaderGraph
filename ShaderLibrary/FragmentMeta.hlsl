half _BakeryAlphaDither;
#include "Alpha.hlsl"

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

#if defined(PIPELINE_BUILTIN)
inline half OneMinusReflectivityFromMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline half3 DiffuseAndSpecularFromMetallic(half3 albedo, half metallic, out half3 specColor, out half oneMinusReflectivity)
{
    specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, albedo, metallic);
    oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
    return albedo * oneMinusReflectivity;
}

half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half smoothness)
{
    half roughness = 1.0 - smoothness;
    half3 res = diffuse;
    res += specular * roughness * 0.5;
    return res;
}


#ifdef GENERATION_CODE
    half4 frag (Varyings unpacked) : SV_Target
    {
#else
    half4 frag (PackedVaryings packedInput) : SV_Target
    {
        Varyings unpacked = UnpackVaryings(packedInput);
#endif
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);


    #ifdef GENERATION_CODE
        SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
        #ifdef USE_SURFACEDESCRIPTION
        SurfaceDescriptionFunction(unpacked, surfaceDescription);
        #endif
    #else
        SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
        SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    #endif

    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

    half3 specColor;
    half oneMinisReflectivity;
    half metallic = surfaceDescription.Metallic;
    half3 diffuseColor = DiffuseAndSpecularFromMetallic(surfaceDescription.BaseColor, metallic, specColor, oneMinisReflectivity);

    #ifdef EDITOR_VISUALIZATION
        o.Albedo = diffuseColor;
        o.VizUV = unpacked.vizUV;
        o.LightCoord = unpacked.lightCoord;
    #else
        o.Albedo = UnityLightmappingAlbedo(diffuseColor, specColor, surfaceDescription.Smoothness);
        // o.Albedo = surfaceDescription.BaseColor;
    #endif
        o.SpecularColor = specColor;
        o.Emission = surfaceDescription.Emission;

    #ifndef EDITOR_VISUALIZATION

    ApplyAlphaClip(surfaceDescription);
    ApplyAlphaSurface(surfaceDescription);
    half alpha = GetAlphaValue(surfaceDescription);

    // bakery alpha
    if (unity_MetaFragmentControl.w != 0)
    {
        #ifdef _ALPHAPREMULTIPLY_ON
        if (_BakeryAlphaDither > 0.5)
        {
            half dither = Unity_Dither(alpha, unpacked.positionCS.xy);
            return dither < 0.0 ? 0 : 1;
        }
        #endif
        return alpha;
    }
    #endif
    
    return UnityMetaFragment(o);
}
#endif

// #if defined(PIPELINE_URP)
// half4 frag (Varyings input) : SV_Target
// {
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);


//     SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
//     #ifdef USE_SURFACEDESCRIPTION
//     SurfaceDescriptionFunction(input, surfaceDescription);
//     #endif

//     #if !defined(_ALPHAFADE_ON) && !defined(_ALPHATEST_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAMODULATE_ON)
//         surfaceDescription.Alpha = 1.0f;
//     #endif
    
//     half specular = 0;
//     BRDFData brdfData;
//     half metallic = 0;// surfaceDescription.Metallic
//     InitializeBRDFData(surfaceDescription.BaseColor, metallic, specular, surfaceDescription.Smoothness, surfaceDescription.Alpha, brdfData);
 
//     MetaInput metaInput = (MetaInput)0;
//     metaInput.BaseColor = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
//     //metaInput.SpecularColor = specular;
//     metaInput.Emission = surfaceDescription.Emission;

//     #ifdef EDITOR_VISUALIZATION
//         metaInput.VizUV = input.vizUV;
//         metaInput.LightCoord = input.lightCoord;
//     #endif
        
//     // bakery alpha
//     if (unity_MetaFragmentControl.w != 0)
//     {
//         #ifdef _ALPHAPREMULTIPLY_ON
//         if (_BakeryAlphaDither > 0.5)
//         {
//             half dither = Unity_Dither(surfaceDescription.Alpha, input.positionCS.xy);
//             return dither < 0.0 ? 0 : 1;
//         }
//         #endif
//         return surfaceDescription.Alpha;
//     }

//     return MetaFragment(metaInput);
// }
// #endif