PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);


    float3 viewDirectionWS = normalize(UnityWorldSpaceViewDir(unpacked.positionWS));
    half NoV = abs(dot(unpacked.normalWS, viewDirectionWS)) + 1e-5f;
    float3 bitangent = normalize(cross(unpacked.normalWS, unpacked.tangentWS.xyz));


    // main light
    float3 lightDirection = normalize(UnityWorldSpaceLightDir(unpacked.positionWS));
    float3 lightHalfVector = Unity_SafeNormalize(lightDirection + viewDirectionWS);
    half lightNoL = saturate(dot(unpacked.normalWS, lightDirection));
    half lightLoH = saturate(dot(lightDirection, lightHalfVector));
    half lightNoH = saturate(dot(unpacked.normalWS, lightHalfVector));
    
    #if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN)
        half lightAttenuation = 1.0;
    #else
        #define _ShadowCoord shadowCoord
        UNITY_LIGHT_ATTENUATION(lightAttenuation, unpacked, unpacked.positionWS.xyz);
        #undef _ShadowCoord
    #endif
    half3 lightColor = lightAttenuation * _LightColor0.rgb;
    half3 lightFinalColor = lightNoL * lightColor;

    //return lightFinalColor.xyzz * surfaceDescription.Albedo.xyzz;


    // #ifndef SHADER_API_MOBILE
    //     lightData.FinalColor *= Fd_Burley(perceptualRoughness, NoV, lightNoL, lightLoH);
    // #endif

    // #if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
    //     lightData.FinalColor *= UnityComputeForwardShadows(input.uv01.zw * unity_LightmapST.xy + unity_LightmapST.zw, input.worldPos, input._ShadowCoord);
    // #endif

    // main light end


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


    half4 finalColor = half4(surfaceDescription.Albedo, surfaceDescription.Alpha);

    #ifdef FOG_ANY
        UNITY_APPLY_FOG(unpacked.fogCoord, finalColor);
    #endif

    return finalColor;
}
