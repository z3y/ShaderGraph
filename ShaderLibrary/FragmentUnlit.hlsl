#include "Alpha.hlsl"

half4 frag (PackedVaryings packedInput) : SV_Target
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    staticVaryings = unpacked;

#ifdef LOD_FADE_CROSSFADE
    LODDitheringTransition(input.positionCS.xy, unity_LODFade.x);
#endif

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    
    ApplyAlphaClip(surfaceDescription);
    ApplyAlphaSurface(surfaceDescription);
    half alpha = GetAlphaValue(surfaceDescription);

    half4 result = half4(surfaceDescription.BaseColor, alpha);

    UNITY_APPLY_FOG(unpacked.fogCoord, result);

    return result;
}