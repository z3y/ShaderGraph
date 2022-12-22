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

    #ifdef _ALPHATEST_ON
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    #if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAFADE_ON)
        half dither = Unity_Dither(surfaceDescription.Alpha, unpacked.positionCS.xy);
        if (dither < 0.0) discard;
    #endif

    return 0;
}
