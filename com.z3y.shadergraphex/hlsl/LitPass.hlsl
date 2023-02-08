PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

#include "LightFunctions.hlsl"
#include "Poi.hlsl"

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    LegacyVaryings legacyVaryings = (LegacyVaryings)0;

    legacyVaryings.pos = unpacked.positionCS;
    legacyVaryings._ShadowCoord = unpacked.shadowCoord;

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
        half3x3 tangentToWorld = half3x3(unpacked.tangentWS.xyz, bitangent, unpacked.normalWS.xyz);
        float3 normalWS = TransformTangentToWorld(surfaceDescription.Normal, tangentToWorld);
        float3 tangent = unpacked.tangentWS.xyz;

        #ifdef _ANISOTROPY
            tangent = TransformTangentToWorld(surfaceDescription.Tangent, tangentToWorld);
            tangent = Orthonormalize(tangent, normalWS);
            bitangent = normalize(cross(normalWS, tangent));
        #endif

    #elif _NORMAL_DROPOFF_OS
        float3 normalWS = TransformObjectToWorldNormal(surfaceDescription.Normal);
    #elif _NORMAL_DROPOFF_WS
        float3 normalWS = surfaceDescription.Normal;
    #endif


    

    half perceptualRoughness = 1.0f - surfaceDescription.Smoothness;
    #ifdef _GEOMETRICSPECULAR_AA
        perceptualRoughness = GSAA_Filament(normalWS, perceptualRoughness, surfaceDescription.GSAAVariance, surfaceDescription.GSAAThreshold);
        surfaceDescription.Smoothness = 1.0f - perceptualRoughness;
    #endif

    half roughness = perceptualRoughness * perceptualRoughness;
    half clampedRoughness = max(roughness, 0.002);
    half reflectance = surfaceDescription.Reflectance;

    normalWS = normalize(normalWS);

    float2 lightmapUV = unpacked.texCoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

    half3 f0 = 0.16 * reflectance * reflectance * (1.0 - surfaceDescription.Metallic) + surfaceDescription.Albedo * surfaceDescription.Metallic;

    half3 indirectSpecular = 0.0;
    half3 directSpecular = 0.0;
    half3 indirectDiffuse = 0.0;

    float3 viewDirectionWS = normalize(UnityWorldSpaceViewDir(unpacked.positionWS));
    half NoV = abs(dot(normalWS, viewDirectionWS)) + 1e-5f;

    half3 brdf;
    half3 energyCompensation;
    EnvironmentBRDF(NoV, perceptualRoughness, f0, brdf, energyCompensation);


    // main light
    float3 lightDirection = Unity_SafeNormalize(UnityWorldSpaceLightDir(unpacked.positionWS));
    float3 lightHalfVector = normalize(lightDirection + viewDirectionWS);
    half lightNoL = saturate(dot(normalWS, lightDirection));
    half lightLoH = saturate(dot(lightDirection, lightHalfVector));
    half lightNoH = saturate(dot(normalWS, lightHalfVector));
    
    UNITY_LIGHT_ATTENUATION(lightAttenuation, legacyVaryings, unpacked.positionWS.xyz);
    #if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN)
        lightAttenuation = 1.0;
    #endif

    half3 lightColor = lightAttenuation * _LightColor0.rgb;
    half3 lightFinalColor = lightNoL * lightColor;

    #ifndef SHADER_API_MOBILE
        lightFinalColor *= Fd_Burley(perceptualRoughness, NoV, lightNoL, lightLoH);
    #endif

    #if defined(LIGHTMAP_SHADOW_MIXING) && defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
         lightFinalColor *= UnityComputeForwardShadows(lightmapUV, unpacked.positionWS, unpacked.shadowCoord);
    #endif



    #ifdef FLAT_LIT
    //#define _SPECULARHIGHLIGHTS_OFF
    //#define _GLOSSYREFLECTIONS_OFF
    {
        // based on poiyomi flat lit because im bad at toon
        half3 magic = max(BetterSH9(normalize(unity_SHAr + unity_SHAg + unity_SHAb)), 0);
        half3 normalLight = _LightColor0.rgb + BetterSH9(float4(0, 0, 0, 1));
        
        half magiLumi = calculateluminance(magic);
        half normaLumi = calculateluminance(normalLight);
        half maginormalumi = magiLumi + normaLumi;
        
        half magiratio = magiLumi / maginormalumi;
        half normaRatio = normaLumi / maginormalumi;
        
        half target = calculateluminance(magic * magiratio + normalLight * normaRatio);
        half3 properLightColor = magic + normalLight;
        half properLuminance = calculateluminance(magic + normalLight);
        lightFinalColor = properLightColor * max(0.0001, (target / properLuminance));

        

        lightFinalColor = min(lightFinalColor, 1.0) * lightAttenuation;
    }
    #endif


    half3 lightSpecular = 0;
    #ifndef _SPECULARHIGHLIGHTS_OFF
    #ifdef _ANISOTROPY
    {
        half at = max(clampedRoughness * (1.0 + surfaceDescription.Anisotropy), 0.001);
        half ab = max(clampedRoughness * (1.0 - surfaceDescription.Anisotropy), 0.001);

        float3 l = lightDirection;
        float3 t = tangent;
        float3 b = bitangent;
        float3 v = viewDirectionWS;

        half ToV = dot(t, v);
        half BoV = dot(b, v);
        half ToL = dot(t, l);
        half BoL = dot(b, l);
        half ToH = dot(t, lightHalfVector);
        half BoH = dot(b, lightHalfVector);

        half3 F = F_Schlick(lightLoH, f0) * energyCompensation;
        half D = D_GGX_Anisotropic(lightNoH, lightHalfVector, t, b, at, ab);
        half V = V_SmithGGXCorrelated_Anisotropic(at, ab, ToV, BoV, ToL, BoL, NoV, lightNoL);

        lightSpecular = max(0.0, (D * V) * F) * lightFinalColor * UNITY_PI;
    }
    #else
    {
        half3 F = F_Schlick(lightLoH, f0) * energyCompensation;
        half D = D_GGX(lightNoH, clampedRoughness);
        half V = V_SmithGGXCorrelated(NoV, lightNoL, clampedRoughness);
        lightSpecular = max(0.0, (D * V) * F) * lightFinalColor * UNITY_PI;
    }
    #endif
    #endif
    // main light end


    half3 lightmappedSpecular = 0;
    {
    #ifdef UNITY_PASS_FORWARDBASE
        #if defined(LIGHTMAP_ON)

            half4 bakedColorTex = SampleBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV, GetTexelSize(unity_Lightmap));
            half3 lightMap = DecodeLightmap(bakedColorTex);

            #if defined(DIRLIGHTMAP_COMBINED)
                float4 lightMapDirection = unity_LightmapInd.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0);
                #ifndef BAKERY_MONOSH
                    lightMap = DecodeDirectionalLightmap(lightMap, lightMapDirection, normalWS);
                #endif
            #endif

            #if defined(BAKERY_MONOSH)
                BakeryMonoSH(lightMap, lightmappedSpecular, lightmapUV, normalWS, viewDirectionWS, clampedRoughness, surfaceDescription, tangent, bitangent);
            #endif

            indirectDiffuse = lightMap;
        #endif

        #if defined(DYNAMICLIGHTMAP_ON)
            float2 realtimeUV = unpacked.texCoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
            indirectDiffuse += RealtimeLightmap(realtimeUV, normalWS);
        #endif
        
        #ifdef LIGHTMAP_ON
            #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
                lightFinalColor = 0.0;
                lightSpecular = 0.0;
                indirectDiffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap (indirectDiffuse, lightAttenuation, bakedColorTex, normalWS);
            #endif
        #endif

        #if !defined(DYNAMICLIGHTMAP_ON) && !defined(LIGHTMAP_ON)
            #ifdef LIGHTPROBE_VERTEX
               // indirectDiffuse = ShadeSHPerPixel(normalWS, i.lightProbe, i.worldPos.xyz);
            #else
                #ifdef FLAT_LIT
                    indirectDiffuse = 0.0;
                #else
                    indirectDiffuse = GetLightProbes(normalWS, unpacked.positionWS);
                #endif
            #endif
        #endif

        indirectDiffuse = max(0.0, indirectDiffuse);

        // #if defined(_LIGHTMAPPED_SPECULAR)
        // {
        //     float3 bakedDominantDirection = 1.0;
        //     half3 bakedSpecularColor = 0.0;

        //     #if defined(DIRLIGHTMAP_COMBINED) && defined(LIGHTMAP_ON) && !defined(BAKERY_SH) && !defined(BAKERY_RNM) && !defined(BAKERY_MONOSH)
        //         bakedDominantDirection = (lightMapDirection.xyz) * 2.0 - 1.0;
        //         bakedSpecularColor = indirectDiffuse;
        //     #endif

        //     #ifndef LIGHTMAP_ON
        //         bakedSpecularColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
        //         bakedDominantDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
        //     #endif

        //     bakedDominantDirection = normalize(bakedDominantDirection);
        // lightmappedSpecular += SpecularHighlights(normalWS, bakedSpecularColor, bakedDominantDirection, f0, viewDir, PerceptualRoughnessToRoughnessClamped(surf.perceptualRoughness), NoV, DFGEnergyCompensation);
        // }
        // #endif

    #endif
    }
    // indirect diffuse end



    // reflections
    half3 reflectionSpecular = 0;
    #ifndef _GLOSSYREFLECTIONS_OFF
    {
        #if defined(UNITY_PASS_FORWARDBASE)

            float3 reflDir = reflect(-viewDirectionWS, normalWS);

            #ifdef _ANISOTROPY
                float3 anisotropicDirection = surfaceDescription.Anisotropy >= 0.0 ? bitangent : tangent;
                float3 anisotropicTangent = cross(anisotropicDirection, viewDirectionWS);
                float3 anisotropicNormal = cross(anisotropicTangent, anisotropicDirection);
                float bendFactor = abs(surfaceDescription.Anisotropy) * saturate(1.0 - (Pow5(1.0 - perceptualRoughness)));
                float3 bentNormal = normalize(lerp(normalWS, anisotropicNormal, bendFactor));
                reflDir = reflect(-viewDirectionWS, bentNormal);
            #endif

            #ifndef SHADER_API_MOBILE
                reflDir = lerp(reflDir, normalWS, roughness * roughness);
            #endif

            Unity_GlossyEnvironmentData envData;
            envData.roughness = perceptualRoughness;

            #ifdef FORCE_SPECCUBE_BOX_PROJECTION
                #define UNITY_SPECCUBE_BOX_PROJECTION
            #endif
            
            #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                envData.reflUVW = BoxProjectedCubemapDirection(reflDir, unpacked.positionWS.xyz, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
            #else
                envData.reflUVW = reflDir;
            #endif

            half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
            reflectionSpecular = probe0;

            #if defined(UNITY_SPECCUBE_BLENDING)
                UNITY_BRANCH
                if (unity_SpecCube0_BoxMin.w < 0.99999)
                {
                    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                        envData.reflUVW = BoxProjectedCubemapDirection(reflDir, unpacked.positionWS.xyz, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
                    #else
                        envData.reflUVW = reflDir;
                    #endif

                    float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                    reflectionSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
                }
            #endif

            float horizon = min(1.0 + dot(reflDir, normalWS), 1.0);
            reflectionSpecular *= horizon * horizon;
            
            //surfaceDescription.Occlusion *= lerp(1.0, saturate(dot(indirectDiffuse, 1.0)), 1.0);
            #ifdef LIGHTMAP_ON
            half specularOcclusion = saturate(sqrt(dot(indirectDiffuse, 1.0)) * surfaceDescription.Occlusion);
            #else
            half specularOcclusion = surfaceDescription.Occlusion;
            #endif
           // surfaceDescription.Occlusion *= saturate(dot(indirectDiffuse, 1.0));

            reflectionSpecular *= computeSpecularAO(NoV, specularOcclusion, roughness);

            indirectSpecular += reflectionSpecular;
        #endif
    }
    #endif
    // reflections end

    #ifdef LTCGI
        #if defined(LIGHTMAP_ON)
            float2 ltcgi_lmuv = i.coord0.zw;
        #else
            float2 ltcgi_lmuv = float2(0, 0);
        #endif

        float3 ltcgiSpecular = 0;
        LTCGI_Contribution(unpacked.positionWS, normalWS, viewDirectionWS, perceptualRoughness, ltcgi_lmuv, indirectDiffuse, ltcgiSpecular);

        #ifndef _SPECULARHIGHLIGHTS_OFF
            indirectSpecular += ltcgiSpecular;
        #endif
    #endif

    directSpecular += lightSpecular;
    indirectSpecular += lightmappedSpecular;

    indirectSpecular = indirectSpecular * energyCompensation * brdf;

    half4 finalColor = half4(surfaceDescription.Albedo * (1.0 - surfaceDescription.Metallic) * (indirectDiffuse * surfaceDescription.Occlusion + (lightFinalColor))
                     + indirectSpecular + directSpecular + surfaceDescription.Emission, surfaceDescription.Alpha);

    #ifdef FOG_ANY
        UNITY_APPLY_FOG(unpacked.fogCoord, finalColor);
    #endif

    return finalColor;
}
