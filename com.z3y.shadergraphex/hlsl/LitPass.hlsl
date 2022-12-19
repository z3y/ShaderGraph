PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

#include "LightFunctions.hlsl"

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #ifdef _ALPHATEST_ON
        #ifdef ALPHATOCOVERAGE_ON
            surfaceDescription.Alpha = (surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold) / max(fwidth(surfaceDescription.Alpha), 0.01f) + 0.5f;
        #else
            clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
        #endif
    #endif

    #ifdef _ALPHAPREMULTIPLY_ON
        surfaceDescription.Albedo *= surfaceDescription.Alpha;
    #endif

    #ifdef _ALPHAMODULATE_ON
        surfaceDescription.Albedo = lerp(1.0f, surfaceDescription.Albedo, surfaceDescription.Alpha);
    #endif

    //TODO: define in generator
    #define _NORMAL_DROPOFF_TS 1

    #if _NORMAL_DROPOFF_TS
	    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
        float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
        float3 normalWS = TransformTangentToWorld(surfaceDescription.Normal, half3x3(unpacked.tangentWS.xyz, bitangent, unpacked.normalWS.xyz));
    #elif _NORMAL_DROPOFF_OS
        float3 normalWS = TransformObjectToWorldNormal(surfaceDescription.Normal);
    #elif _NORMAL_DROPOFF_WS
        float3 normalWS = surfaceDescription.Normal;
    #endif

    half perceptualRoughness = 1.0f - surfaceDescription.Smoothness;
    half roughness = perceptualRoughness * perceptualRoughness;

    half3 indirectSpecular = 0.0;
    half3 directSpecular = 0.0;
    half3 indirectDiffuse = 0.0;

    float3 viewDirectionWS = normalize(UnityWorldSpaceViewDir(unpacked.positionWS));
    half NoV = abs(dot(normalWS, viewDirectionWS)) + 1e-5f;

    // main light
    float3 lightDirection = normalize(UnityWorldSpaceLightDir(unpacked.positionWS));
    float3 lightHalfVector = Unity_SafeNormalize(lightDirection + viewDirectionWS);
    half lightNoL = saturate(dot(normalWS, lightDirection));
    half lightLoH = saturate(dot(lightDirection, lightHalfVector));
    half lightNoH = saturate(dot(normalWS, lightHalfVector));
    
    #if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN)
        half lightAttenuation = 1.0;
    #else
        #define _ShadowCoord shadowCoord
        #define pos positionCS
        UNITY_LIGHT_ATTENUATION(lightAttenuation, unpacked, unpacked.positionWS.xyz);
        #undef pos
        #undef _ShadowCoord
    #endif
    half3 lightColor = lightAttenuation * _LightColor0.rgb;
    half3 lightFinalColor = lightNoL * lightColor;


    #ifndef SHADER_API_MOBILE
        lightFinalColor *= Fd_Burley(perceptualRoughness, NoV, lightNoL, lightLoH);
    #endif

    // #if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    //     lightData.FinalColor *= UnityComputeForwardShadows(input.uv01.zw * unity_LightmapST.xy + unity_LightmapST.zw, input.worldPos, input._ShadowCoord);
    // #endif

    // main light end





    half4 finalColor = half4(surfaceDescription.Albedo * (1.0 - surfaceDescription.Metallic) * (indirectDiffuse * surfaceDescription.Occlusion + (lightFinalColor))
                     + indirectSpecular + directSpecular + surfaceDescription.Emission, surfaceDescription.Alpha);

    #ifdef FOG_ANY
        UNITY_APPLY_FOG(unpacked.fogCoord, finalColor);
    #endif

    return finalColor;
}