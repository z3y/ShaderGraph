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

#if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)
    #define UNITY_PASS_FORWARD
#endif

Varyings BuildVaryings(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(FEATURES_GRAPH_VERTEX)
    // Evaluate Vertex Graph
    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);



    // Assign modified vertex attributes
    input.positionOS = vertexDescription.VertexPosition;
    #if defined(VARYINGS_NEED_NORMAL_WS)
        input.normalOS = vertexDescription.VertexNormal;
    #endif //FEATURES_GRAPH_NORMAL
    #if defined(VARYINGS_NEED_TANGENT_WS)
        input.tangentOS.xyz = vertexDescription.VertexTangent.xyz;
    #endif //FEATURES GRAPH TANGENT
#endif //FEATURES_GRAPH_VERTEX

    LegacyAttributes v = (LegacyAttributes)0;
    LegacyVaryings o = (LegacyVaryings)0;
    v.vertex = float4(input.positionOS.xyz, 1);
    #ifdef ATTRIBUTES_NEED_NORMAL
    v.normal = input.normalOS.xyz;
    #endif

    // Returns the camera relative position (if enabled)
    float3 positionWS = TransformObjectToWorld(input.positionOS);

#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#else
    // Required to compile ApplyVertexModification that doesn't use normal.
    float3 normalWS = float3(0.0, 0.0, 0.0);
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

    // TODO: Change to inline ifdef
    // Do vertex modification in camera relative space (if enabled)
#if defined(HAVE_VERTEX_MODIFICATION)
    ApplyVertexModification(input, normalWS, positionWS, _TimeParameters.xyz);
#endif

#if defined(VARYINGS_NEED_POSITION_WS)
    output.positionWS = positionWS;
#endif

#if defined(VARYINGS_NEED_NORMAL_WS) || defined(UNITY_PASS_FORWARDBASE)
    output.normalWS = normalWS;			// normalized in TransformObjectToWorldNormal()
#endif

#ifdef VARYINGS_NEED_TANGENT_WS
    output.tangentWS = tangentWS;		// normalized in TransformObjectToWorldDir()
#endif

#if defined(SHADERPASS_SHADOWCASTER)
    TRANSFER_SHADOW_CASTER_NOPOS(output, output.positionCS);
#elif defined(SHADERPASS_META)
    output.positionCS = UnityMetaVertexPosition(float4(input.positionOS, 0), input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.texCoord0 = input.uv0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.texCoord1 = input.uv1;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2) || defined(DYNAMICLIGHTMAP_ON)
    output.texCoord2 = input.uv2;
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
    output.texCoord3 = input.uv3;
#endif

#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.color = input.color;
#endif

#ifdef VARYINGS_NEED_VIEWDIRECTION_WS
    output.viewDirectionWS = _WorldSpaceCameraPos.xyz - positionWS;
#endif

#ifdef VARYINGS_NEED_SCREENPOSITION
    output.screenPosition = ComputeScreenPos(output.positionCS, _ProjectionParams.x);
#endif

// #if defined(SHADERPASS_FORWARD)
//     OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.lightmapUV);
//     OUTPUT_SH(normalWS, output.sh);
// #endif

// #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
//     half3 vertexLight = VertexLighting(positionWS, normalWS);
//     half fogFactor = ComputeFogFactor(output.positionCS.z);
//     output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
// #endif

#ifdef FOG_ANY
    UNITY_TRANSFER_FOG(output, output.positionCS);
#endif

// #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
#if defined(UNITY_PASS_FORWARD)
    //output.shadowCoord = GetShadowCoord(input);
    o.pos = output.positionCS;
    o._ShadowCoord = output.shadowCoord;
    UNITY_TRANSFER_SHADOW(o, input.uv1.xy);
    output.shadowCoord = o._ShadowCoord;
    output.positionCS = o.pos;
#endif

#ifdef EDITOR_VISUALIZATION
    output.vizUV = 0;
    output.lightCoord = 0;
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        output.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, input.uv0.xy, input.uv1.xy, input.uv2.xy, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        output.vizUV = input.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        output.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1)));
    }
#endif

    return output;
}
