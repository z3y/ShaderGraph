void ApplyAlphaClip(inout SurfaceDescription surfaceDescription)
{
#ifndef UNITY_PASS_FORWARDBASE
#undef ALPHATOCOVERAGE_ON
#endif

    float alphaClipSharpness = 0.001;
    #if defined(_ALPHATEST_ON) && defined(ALPHATOCOVERAGE_ON)
        surfaceDescription.Alpha = (surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold) / max(fwidth(surfaceDescription.Alpha), alphaClipSharpness) + 0.5f;
    #endif

    #if defined(_ALPHATEST_ON) && !defined(ALPHATOCOVERAGE_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif
}

void ApplyAlphaSurface(inout SurfaceDescription surfaceDescription)
{
    #if defined(_ALPHAPREMULTIPLY_ON)
        surfaceDescription.BaseColor *= surfaceDescription.Alpha;

        #if (SHADERPASS == FORWARDBASE) || (SHADERPASS == FORWARDADD)
            surfaceDescription.BaseColor = lerp(surfaceDescription.Alpha, 1.0, surfaceDescription.Metallic);
        #endif
    #endif

    #if defined(_ALPHAMODULATE_ON)
        surfaceDescription.BaseColor.rgb = lerp(1.0, surfaceDescription.Albedo.rgb, surfaceDescription.Alpha);
    #endif
}

half GetAlphaValue(SurfaceDescription surfaceDescription)
{
    #if !defined(_ALPHAFADE_ON) && !defined(_ALPHATEST_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAMODULATE_ON)
        return 1.0;
    #else
        return surfaceDescription.Alpha;
    #endif
}