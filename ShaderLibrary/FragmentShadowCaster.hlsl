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

    #ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(unpacked.positionCS.xy, unity_LODFade.x);
    #endif

    #ifdef GENERATION_CODE
        SurfaceDescription surfaceDescription = InitializeSurfaceDescription();
        #ifdef USE_SURFACEDESCRIPTION
        SurfaceDescriptionFunction(unpacked, surfaceDescription);
        #endif
    #else
        SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
        SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    #endif

    #if defined(_ALPHATEST_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    #ifdef _ALPHAPREMULTIPLY_ON
        surfaceDescription.Alpha = lerp(surfaceDescription.Alpha, 1.0, surfaceDescription.Metallic);
    #endif

    #if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAFADE_ON)
        half dither = Unity_Dither(surfaceDescription.Alpha, unpacked.positionCS.xy);
        if (dither < 0.0) discard;
    #endif

    return 0;
}