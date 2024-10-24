Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexDescription description = VertexDescriptionFunction(BuildVertexDescriptionInputs(input));
    input.positionOS = description.Position;
    input.normalOS.xyz = description.Normal.xyz;
    input.tangentOS.xyz = description.Tangent.xyz;

    #if defined(CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC)
        CustomInterpolatorPassThroughFunc(output, description);
    #endif

    float3 positionWS = TransformObjectToWorld(input.positionOS);
#if defined(ATTRIBUTES_NEED_NORMAL)
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
#if defined(ATTRIBUTES_NEED_TANGENT)
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

    LegacyAttributes v = (LegacyAttributes)0;
    LegacyVaryings o = (LegacyVaryings)0;
    v.vertex = float4(input.positionOS.xyz, 1);
#if defined(ATTRIBUTES_NEED_NORMAL)
    v.normal = input.normalOS.xyz;
#endif

#if defined(UNITY_PASS_META)
    output.positionCS = UnityMetaVertexPosition(float4(TransformWorldToObject(positionWS), 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
#elif defined(UNITY_PASS_SHADOWCASTER)
    output.positionCS = TransformWorldToHClip(ApplyShadowBiasNormal(positionWS, normalWS));
    output.positionCS = UnityApplyLinearShadowBias(output.positionCS);
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif

#if defined(VARYINGS_NEED_NORMAL_WS)
    output.normalWS = normalWS;
#endif
#if defined(VARYINGS_NEED_TANGENT_WS)
    output.tangentWS = tangentWS;
#endif
#if defined(VARYINGS_NEED_POSITION_WS)
    output.positionWS = positionWS;
#endif

#if defined(VARYINGS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2)
    output.texCoord2 = input.uv2;
#endif
#if defined(VARYINGS_NEED_TEXCOORD3)
    output.texCoord3 = input.uv3;
#endif

    
#if defined(LIGHTMAP_ON) && (SHADERPASS != SHADERPASS_UNLIT)
    output.lightmapUV.xy = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
#if defined(DYNAMICLIGHTMAP_ON) && (SHADERPASS != SHADERPASS_UNLIT)
    output.lightmapUV.zw = input.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;;
#endif

#if defined(VARYINGS_NEED_COLOR)
    output.color = input.color;
#endif

#if defined(FOG_EXP) || defined(FOG_EXP2) || defined(FOG_LINEAR)
    UNITY_TRANSFER_FOG(output, output.positionCS);
#endif

#if defined(VARYINGS_NEED_SHADOWCOORD)
    o.pos = output.positionCS;
    o._ShadowCoord = output.shadowCoord;
    UNITY_TRANSFER_SHADOW(o, input.uv1.xy);
    output.shadowCoord = o._ShadowCoord;
    output.positionCS = o.pos;
#endif

#if defined(EDITOR_VISUALIZATION)
    output.vizUV = 0;
    output.lightCoord = 0;
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
    {
        output.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, input.uv0.xy, input.uv1.xy, input.uv2.xy, unity_EditorViz_Texture_ST);
    }
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        output.vizUV = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        output.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1)));
    }
#endif

    // #if defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)
	// 	output.vertexLight = Shade4PointLights
    //     (
	// 		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
	// 		unity_LightColor[0].rgb, unity_LightColor[1].rgb,
	// 		unity_LightColor[2].rgb, unity_LightColor[3].rgb,
	// 		unity_4LightAtten0, output.positionWS, output.normalWS
	// 	);
	// #endif

    return output;
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}