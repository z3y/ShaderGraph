static Varyings staticVaryings;

#define GetCustomMainLightData GetUnityLightData
UnityLightData GetCustomMainLightData(Varyings unpacked)
{
    UnityLightData data = (UnityLightData)0;

#if defined(PIPELINE_BUILTIN) && defined(USING_LIGHT_MULTI_COMPILE)
    data.direction = Unity_SafeNormalize(UnityWorldSpaceLightDir(unpacked.positionWS));
    data.color = _LightColor0.rgb;

    // attenuation
    // my favorite macro from UnityCG /s
    LegacyVaryings legacyVaryings = (LegacyVaryings)0;
    legacyVaryings.pos = unpacked.positionCS;
#ifdef VARYINGS_NEED_SHADOWCOORD
    legacyVaryings._ShadowCoord = unpacked.shadowCoord;
#endif
    UNITY_LIGHT_ATTENUATION(lightAttenuation, legacyVaryings, unpacked.positionWS.xyz);

    #if defined(HANDLE_SHADOWS_BLENDING_IN_GI) && defined(SHADOWS_SCREEN) && defined(LIGHTMAP_ON)
        half bakedAtten = UnitySampleBakedOcclusion(unpacked.lightmapUV, unpacked.positionWS);
        float zDist = dot(_WorldSpaceCameraPos -  unpacked.positionWS, UNITY_MATRIX_V[2].xyz);
        float fadeDist = UnityComputeShadowFadeDistance(unpacked.positionWS, zDist);
        lightAttenuation = UnityMixRealtimeAndBakedShadows(lightAttenuation, bakedAtten, UnityComputeShadowFade(fadeDist));
    #endif
    
#if defined(UNITY_PASS_FORWARDBASE) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SHADOWMASK)
    lightAttenuation = 1.0;
#endif
    data.attenuation = lightAttenuation;

#if defined(LIGHTMAP_SHADOW_MIXING) && defined(LIGHTMAP_ON)
    data.color *= UnityComputeForwardShadows(unpacked.lightmapUV.xy, unpacked.positionWS, unpacked.shadowCoord);
#endif

#endif

#if defined(PIPELINE_URP)

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord = unpacked.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    float4 shadowCoord = TransformWorldToShadowCoord(unpacked.positionWS);
#else
    float4 shadowCoord = float4(0, 0, 0, 0);
#endif

    Light mainLight = GetMainLight(shadowCoord);

    data.color = mainLight.color;
    data.direction = mainLight.direction;
    data.attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
#endif

    return data;
}

float3 GetViewDirectionWS(float3 positionWS)
{
#ifdef PIPELINE_BUILTIN
    return normalize(UnityWorldSpaceViewDir(positionWS));
#else
    return normalize(GetCameraPositionWS() - positionWS);
#endif
}


#ifdef GENERATION_CODE

// WS - WorldSpace
// OS - ObjectSpace
// TS - TangentSpace
// VS - ViewSpace

struct VertexDescriptionInputs
{
    float3 normalWS; // ATTRIBUTES_NEED_NORMAL
    float3 normalOS; // ATTRIBUTES_NEED_NORMAL
    float3 normalVS; // ATTRIBUTES_NEED_NORMAL
    float3 normalTS; // ATTRIBUTES_NEED_NORMAL
    float3 tangentWS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 tangentOS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 tangentVS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 tangentTS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 bitangentWS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 bitangentOS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 bitangentVS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 bitangentTS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 viewDirectionWS;
    float3 viewDirectionOS;
    float3 viewDirectionVS;
    float3 viewDirectionTS; // ATTRIBUTES_NEED_TANGENT && ATTRIBUTES_NEED_NORMAL
    float3 positionWS;
    float3 positionOS;
    float3 positionVS;
    float3 positionTS;
    float3 absolutePositionWS;
    float4 screenPosition;
    float4 uv0; // ATTRIBUTES_NEED_TEXCOORD0
    float4 uv1; // ATTRIBUTES_NEED_TEXCOORD1
    float4 uv2; // ATTRIBUTES_NEED_TEXCOORD2
    float4 uv3; // ATTRIBUTES_NEED_TEXCOORD3
    float3 vertexColor; // ATTRIBUTES_NEED_COLOR
};

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output = (VertexDescriptionInputs)0;

    #if defined(SHADER_STAGE_VERTEX)
        #if defined(ATTRIBUTES_NEED_NORMAL)
            output.normalOS = input.normalOS;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.normalVS = TransformWorldToViewDir(output.normalWS);
            output.normalTS = float3(0.0f, 0.0f, 1.0f);

            #if defined(ATTRIBUTES_NEED_TANGENT)
                output.tangentOS = input.tangentOS;
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.tangentVS = TransformWorldToViewDir(output.tangentWS);
                output.tangentTS = float3(1.0f, 0.0f, 0.0f);

                output.bitangentOS = normalize(cross(input.normalOS, input.tangentOS) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
                output.bitangentWS = TransformObjectToWorldDir(output.bitangentOS);
                output.bitangentVS = TransformWorldToViewDir(output.bitangentWS);
                output.bitangentTS = float3(0.0f, 1.0f, 0.0f);
            #endif
        #endif

        output.positionOS = input.positionOS;
        output.positionWS = TransformObjectToWorld(input.positionOS);
        output.positionVS = TransformWorldToView(output.positionWS);
        output.positionTS = float3(0.0f, 0.0f, 0.0f);
        output.absolutePositionWS = GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));

        output.viewDirectionWS = GetViewDirectionWS(output.positionWS);
        output.viewDirectionOS = TransformWorldToObjectDir(output.viewDirectionWS);
        output.viewDirectionVS = TransformWorldToViewDir(output.viewDirectionWS);

        #ifdef ATTRIBUTES_NEED_TANGENT
            #if defined(ATTRIBUTES_NEED_NORMAL)
                float3x3 tangentSpaceTransform = float3x3(output.tangentWS, output.bitangentWS, output.normalWS);
                output.viewDirectionTS = mul(tangentSpaceTransform, output.viewDirectionWS);
            #endif
        #endif

        output.screenPosition = ComputeScreenPos(TransformWorldToHClip(output.positionWS), _ProjectionParams.x);

        #if defined(ATTRIBUTES_NEED_TEXCOORD0)
            output.uv0 = input.uv0;
        #endif
        #if defined(ATTRIBUTES_NEED_TEXCOORD1)
            output.uv1 = input.uv1;
        #endif
        #if defined(ATTRIBUTES_NEED_TEXCOORD2)
            output.uv2 = input.uv2;
        #endif
        #if defined(ATTRIBUTES_NEED_TEXCOORD3)
            output.uv3 = input.uv3;
        #endif

        #if defined(ATTRIBUTES_NEED_COLOR)
            output.vertexColor = input.color;
        #endif

        // output.boneWeights = input.weights;
        // output.boneIndices = input.indices;

    #endif

    return output;
}

struct SurfaceDescriptionInputs
{
    float3 normalWS; // VARYINGS_NEED_NORMAL
    float3 normalOS; // VARYINGS_NEED_NORMAL
    float3 normalVS; // VARYINGS_NEED_NORMAL
    float3 normalTS; // VARYINGS_NEED_NORMAL
    float3 tangentWS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 tangentOS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 tangentVS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 tangentTS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 bitangentWS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 bitangentOS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 bitangentVS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 bitangentTS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 viewDirectionWS;
    float3 viewDirectionOS;
    float3 viewDirectionVS;
    float3 viewDirectionTS; // VARYINGS_NEED_TANGENT && VARYINGS_NEED_NORMAL
    float3 positionWS;
    float3 positionOS;
    float3 positionVS;
    float3 positionTS;
    float3 absolutePositionWS;
    float4 screenPosition;
    float4 uv0; // VARYINGS_NEED_TEXCOORD0
    float4 uv1; // VARYINGS_NEED_TEXCOORD1
    float4 uv2; // VARYINGS_NEED_TEXCOORD2
    float4 uv3; // VARYINGS_NEED_TEXCOORD3
    float3 vertexColor; // VARYINGS_NEED_COLOR
    bool faceSign; // VARYINGS_NEED_CULLFACE
};

SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output = (SurfaceDescriptionInputs)0;

    #if defined(SHADER_STAGE_FRAGMENT)

        #if defined(VARYINGS_NEED_NORMAL)
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);

            output.normalWS = renormFactor * input.normalWS.xyz; // we want a unit length Normal Vector node in shader graph
            output.normalOS = normalize(mul(output.normalWS, (float3x3)UNITY_MATRIX_M)); // transposed multiplication by inverse matrix to handle normal scale
            output.normalVS = mul(output.normalWS, (float3x3)UNITY_MATRIX_I_V); // transposed multiplication by inverse matrix to handle normal scale
            output.normalTS = float3(0.0f, 0.0f, 1.0f);

            #if defined(VARYINGS_NEED_TANGENT)
                // to preserve mikktspace compliance we use same scale renormFactor as was used on the normal.
                // This is explained in section 2.2 in "surface gradient based bump mapping framework"
                output.tangentWS = renormFactor * input.tangentWS.xyz;
                output.tangentOS = TransformWorldToObjectDir(output.tangentWS);
                output.tangentVS = TransformWorldToViewDir(output.tangentWS);
                output.tangentTS = float3(1.0f, 0.0f, 0.0f);

                // use bitangent on the fly like in hdrp
                // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
                float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

                output.bitangentWS = renormFactor * bitang;
                output.bitangentOS = TransformWorldToObjectDir(output.bitangentWS);
                output.bitangentVS = TransformWorldToViewDir(output.bitangentWS);
                output.bitangentTS = float3(0.0f, 1.0f, 0.0f);
            #endif
        #endif

        output.viewDirectionWS = GetViewDirectionWS(input.positionWS);
        output.viewDirectionOS = TransformWorldToObjectDir(output.viewDirectionWS);
        output.viewDirectionVS = TransformWorldToViewDir(output.viewDirectionWS);

        #ifdef VARYINGS_NEED_TANGENT
            #if defined(VARYINGS_NEED_NORMAL)
                float3x3 tangentSpaceTransform = float3x3(output.tangentWS, output.bitangentWS, output.normalWS);
                output.viewDirectionTS = mul(tangentSpaceTransform, output.viewDirectionWS);
            #endif
        #endif

        output.positionWS = input.positionWS;
        output.positionOS = TransformWorldToObject(input.positionWS);
        output.positionVS = TransformWorldToView(input.positionWS);
        output.positionTS = float3(0.0f, 0.0f, 0.0f);
        output.absolutePositionWS = GetAbsolutePositionWS(input.positionWS);

        output.screenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);

        #if defined(VARYINGS_NEED_TEXCOORD0)
            output.uv0 = input.texCoord0;
        #endif
        #if defined(VARYINGS_NEED_TEXCOORD1)
            output.uv1 = input.texCoord1;
        #endif
        #if defined(VARYINGS_NEED_TEXCOORD2)
            output.uv2 = input.texCoord2;
        #endif
        #if defined(VARYINGS_NEED_TEXCOORD3)
            output.uv3 = input.texCoord3;
        #endif

        #if defined(VARYINGS_NEED_COLOR)
            output.vertexColor = input.color;
        #endif

        #if defined(VARYINGS_NEED_CULLFACE)
            output.faceSign = IS_FRONT_VFACE(input.cullFace, true, false);
        #else
            output.faceSign = true;
        #endif

    #endif
    return output;
}

#endif