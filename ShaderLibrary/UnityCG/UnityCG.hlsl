#define UNITY_PREV_MATRIX_M 0
#define UNITY_PREV_MATRIX_I_M 0



#define Unity_SafeNormalize SafeNormalize
    
#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE) || defined (POINT) || defined (SPOT) || defined (POINT_NOATT) || defined (POINT_COOKIE)
#define USING_LIGHT_MULTI_COMPILE
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch
// Real-support for depth-format cube shadow map.
#define SHADOWS_CUBE_IN_DEPTH_TEX
#endif

    #ifdef UNITY_COLORSPACE_GAMMA
    #define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
    #define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
    #define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
    #define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
    #else // Linear values
    #define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
    #define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
    #define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
    #define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
    #endif

    // Legacy for compatibility with existing shaders
    #ifndef GENERATION_GRAPH
    inline bool IsGammaSpace()
    {
        #ifdef UNITY_COLORSPACE_GAMMA
            return true;
        #else
            return false;
        #endif
    }
    #endif
    
    inline float GammaToLinearSpaceExact (float value)
    {
        if (value <= 0.04045F)
            return value / 12.92F;
        else if (value < 1.0F)
            return pow((value + 0.055F)/1.055F, 2.4F);
        else
            return pow(value, 2.2F);
    }
    
    inline half3 GammaToLinearSpace (half3 sRGB)
    {
        // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
        return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
    
        // Precise version, useful for debugging.
        //return half3(GammaToLinearSpaceExact(sRGB.r), GammaToLinearSpaceExact(sRGB.g), GammaToLinearSpaceExact(sRGB.b));
    }
    
    inline float LinearToGammaSpaceExact (float value)
    {
        if (value <= 0.0F)
            return 0.0F;
        else if (value <= 0.0031308F)
            return 12.92F * value;
        else if (value < 1.0F)
            return 1.055F * pow(value, 0.4166667F) - 0.055F;
        else
            return pow(value, 0.45454545F);
    }
    
    // Convert rgb to luminance
    // with rgb in linear space with sRGB primaries and D65 white point
    half LinearRgbToLuminance(half3 linearRgb)
    {
        return dot(linearRgb, half3(0.2126729f,  0.7151522f, 0.0721750f));
    }

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
 
// ------------------------------------------------------------------
    //  Fog helpers
    //
    //  multi_compile_fog Will compile fog variants.
    //  UNITY_FOG_COORDS(texcoordindex) Declares the fog data interpolator.
    //  UNITY_TRANSFER_FOG(outputStruct,clipspacePos) Outputs fog data from the vertex shader.
    //  UNITY_APPLY_FOG(fogData,col) Applies fog to color "col". Automatically applies black fog when in forward-additive pass.
    //  Can also use UNITY_APPLY_FOG_COLOR to supply your own fog color.
    
    // In case someone by accident tries to compile fog code in one of the g-buffer or shadow passes:
    // treat it as fog is off.
    #if defined(UNITY_PASS_PREPASSBASE) || defined(UNITY_PASS_DEFERRED) || defined(UNITY_PASS_SHADOWCASTER)
    #undef FOG_LINEAR
    #undef FOG_EXP
    #undef FOG_EXP2
    #endif
    
    #if defined(UNITY_REVERSED_Z)
        #if UNITY_REVERSED_Z == 1
            //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
            //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
            #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
        #else
            //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
            #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
        #endif
    #elif UNITY_UV_STARTS_AT_TOP
        //D3d without reversed z => z clip range is [0, far] -> nothing to do
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
    #else
        //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
    #endif
    
    #if defined(FOG_LINEAR)
        // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
        #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = (coord) * unity_FogParams.z + unity_FogParams.w
    #elif defined(FOG_EXP)
        // factor = exp(-density*z)
        #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.y * (coord); unityFogFactor = exp2(-unityFogFactor)
    #elif defined(FOG_EXP2)
        // factor = exp(-(density*z)^2)
        #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = unity_FogParams.x * (coord); unityFogFactor = exp2(-unityFogFactor*unityFogFactor)
    #else
        #define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = 0.0
    #endif
    
    #define UNITY_CALC_FOG_FACTOR(coord) UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE(coord))
    
    #define UNITY_FOG_COORDS_PACKED(idx, vectype) vectype fogCoord : TEXCOORD##idx;
    
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        #define UNITY_FOG_COORDS(idx) UNITY_FOG_COORDS_PACKED(idx, float1)
    
        #if (SHADER_TARGET < 30) || defined(SHADER_API_MOBILE)
            // mobile or SM2.0: calculate fog factor per-vertex
            #define UNITY_TRANSFER_FOG(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.fogCoord.x = unityFogFactor
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.tSpace1.y = tangentSign; o.tSpace2.y = unityFogFactor
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.worldPos.w = unityFogFactor
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos) UNITY_CALC_FOG_FACTOR((outpos).z); o.eyeVec.w = unityFogFactor
        #else
            // SM3.0 and PC/console: calculate fog distance per-vertex, and fog factor per-pixel
            #define UNITY_TRANSFER_FOG(o,outpos) o.fogCoord.x = (outpos).z
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos) o.tSpace2.y = (outpos).z
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos) o.worldPos.w = (outpos).z
            #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos) o.eyeVec.w = (outpos).z
        #endif
    #else
        #define UNITY_FOG_COORDS(idx)
        #define UNITY_TRANSFER_FOG(o,outpos)
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,outpos)
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,outpos)
        #define UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,outpos)
    #endif
    
    #define UNITY_FOG_LERP_COLOR(col,fogCol,fogFac) col.rgb = lerp((fogCol).rgb, (col).rgb, saturate(fogFac))
    
    
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        #if (SHADER_TARGET < 30) || defined(SHADER_API_MOBILE)
            // mobile or SM2.0: fog factor was already calculated per-vertex, so just lerp the color
            #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) UNITY_FOG_LERP_COLOR(col,fogCol,(coord).x)
        #else
            // SM3.0 and PC/console: calculate fog factor and lerp fog color
            #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol) UNITY_CALC_FOG_FACTOR((coord).x); UNITY_FOG_LERP_COLOR(col,fogCol,unityFogFactor)
        #endif
        #define UNITY_EXTRACT_FOG(name) float _unity_fogCoord = name.fogCoord
        #define UNITY_EXTRACT_FOG_FROM_TSPACE(name) float _unity_fogCoord = name.tSpace2.y
        #define UNITY_EXTRACT_FOG_FROM_WORLD_POS(name) float _unity_fogCoord = name.worldPos.w
        #define UNITY_EXTRACT_FOG_FROM_EYE_VEC(name) float _unity_fogCoord = name.eyeVec.w
    #else
        #define UNITY_APPLY_FOG_COLOR(coord,col,fogCol)
        #define UNITY_EXTRACT_FOG(name)
        #define UNITY_EXTRACT_FOG_FROM_TSPACE(name)
        #define UNITY_EXTRACT_FOG_FROM_WORLD_POS(name)
        #define UNITY_EXTRACT_FOG_FROM_EYE_VEC(name)
    #endif
    
    #ifdef UNITY_PASS_FORWARDADD
        #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,fixed4(0,0,0,0))
    #else
        #define UNITY_APPLY_FOG(coord,col) UNITY_APPLY_FOG_COLOR(coord,col,unity_FogColor)
    #endif
//  End fog helpers

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTexBuiltIn(float2 uv, float w)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}
#endif

inline float4 ComputeNonStereoScreenPos(float4 pos)
{
    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
    o.zw = pos.zw;
    return o;
}
inline float4 ComputeScreenPos(float4 pos)
{
    float4 o = ComputeNonStereoScreenPos(pos);
#if defined(UNITY_SINGLE_PASS_STEREO)
    o.xy = TransformStereoScreenSpaceTexBuiltIn(o.xy, pos.w);
#endif
    return o;
}

inline half3 LinearToGammaSpace (half3 linRGB)
{
    linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
    // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

    // Exact version, useful for debugging.
    //return half3(LinearToGammaSpaceExact(linRGB.r), LinearToGammaSpaceExact(linRGB.g), LinearToGammaSpaceExact(linRGB.b));
}

#if UNITY_LIGHT_PROBE_PROXY_VOLUME
        
    // normal should be normalized, w=1.0
    half3 SHEvalLinearL0L1_SampleProbeVolume (half4 normal, float3 worldPos)
    {
        const float transformToLocal = unity_ProbeVolumeParams.y;
        const float texelSizeX = unity_ProbeVolumeParams.z;

        //The SH coefficients textures and probe occlusion are packed into 1 atlas.
        //-------------------------
        //| ShR | ShG | ShB | Occ |
        //-------------------------

        float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;
        float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;
        texCoord.x = texCoord.x * 0.25f;

        // We need to compute proper X coordinate to sample.
        // Clamp the coordinate otherwize we'll have leaking between RGB coefficients
        float texCoordX = clamp(texCoord.x, 0.5f * texelSizeX, 0.25f - 0.5f * texelSizeX);

        // sampler state comes from SHr (all SH textures share the same sampler)
        texCoord.x = texCoordX;
        half4 SHAr = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

        texCoord.x = texCoordX + 0.25f;
        half4 SHAg = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

        texCoord.x = texCoordX + 0.5f;
        half4 SHAb = UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);

        // Linear + constant polynomial terms
        half3 x1;
        x1.r = dot(SHAr, normal);
        x1.g = dot(SHAg, normal);
        x1.b = dot(SHAb, normal);

        return x1;
    }
#endif


    // Used in ForwardBase pass: Calculates diffuse lighting from 4 point lights, with data packed in a special way.
float3 Shade4PointLights (
    float4 lightPosX, float4 lightPosY, float4 lightPosZ,
    float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
    float4 lightAttenSq,
    float3 pos, float3 normal)
{
    // to light vectors
    float4 toLightX = lightPosX - pos.x;
    float4 toLightY = lightPosY - pos.y;
    float4 toLightZ = lightPosZ - pos.z;
    // squared lengths
    float4 lengthSq = 0;
    lengthSq += toLightX * toLightX;
    lengthSq += toLightY * toLightY;
    lengthSq += toLightZ * toLightZ;
    // don't produce NaNs if some vertex position overlaps with the light
    lengthSq = max(lengthSq, 0.000001);

    // NdotL
    float4 ndotl = 0;
    ndotl += toLightX * normal.x;
    ndotl += toLightY * normal.y;
    ndotl += toLightZ * normal.z;
    // correct NdotL
    float4 corr = rsqrt(lengthSq);
    ndotl = max (float4(0,0,0,0), ndotl * corr);
    // attenuation
    float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
    float4 diff = ndotl * atten;
    // final color
    float3 col = 0;
    col += lightColor0 * diff.x;
    col += lightColor1 * diff.y;
    col += lightColor2 * diff.z;
    col += lightColor3 * diff.w;
    return col;
}

// normal should be normalized, w=1.0
half3 SHEvalLinearL0L1 (half4 normal)
{
    half3 x;

    // Linear (L1) + constant (L0) polynomial terms
    x.r = dot(unity_SHAr,normal);
    x.g = dot(unity_SHAg,normal);
    x.b = dot(unity_SHAb,normal);

    return x;
}

// normal should be normalized, w=1.0
half3 SHEvalLinearL2 (half4 normal)
{
    half3 x1, x2;
    // 4 of the quadratic (L2) polynomials
    half4 vB = normal.xyzz * normal.yzzx;
    x1.r = dot(unity_SHBr,vB);
    x1.g = dot(unity_SHBg,vB);
    x1.b = dot(unity_SHBb,vB);

    // Final (5th) quadratic (L2) polynomial
    half vC = normal.x*normal.x - normal.y*normal.y;
    x2 = unity_SHC.rgb * vC;

    return x1 + x2;
}

// normal should be normalized, w=1.0
// output in active color space
half3 ShadeSH9 (half4 normal)
{
    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1 (normal);

    // Quadratic polynomials
    res += SHEvalLinearL2 (normal);

#   ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToGammaSpace (res);
#   endif

    return res;
}

// Computes world space view direction, from object space position
inline float3 UnityWorldSpaceViewDir( in float3 worldPos )
{
    return _WorldSpaceCameraPos.xyz - worldPos;
}

    // Decodes HDR textures
    // handles dLDR, RGBM formats
    inline half3 DecodeLightmapRGBM (half4 data, half4 decodeInstructions)
    {
        // If Linear mode is not supported we can skip exponent part
        #if defined(UNITY_COLORSPACE_GAMMA)
        # if defined(UNITY_FORCE_LINEAR_READ_FOR_RGBM)
            return (decodeInstructions.x * data.a) * sqrt(data.rgb);
        # else
            return (decodeInstructions.x * data.a) * data.rgb;
        # endif
        #else
            return (decodeInstructions.x * pow(data.a, decodeInstructions.y)) * data.rgb;
        #endif
    }
    
    // Decodes doubleLDR encoded lightmaps.
    inline half3 DecodeLightmapDoubleLDR( fixed4 color, half4 decodeInstructions)
    {
        // decodeInstructions.x contains 2.0 when gamma color space is used or pow(2.0, 2.2) = 4.59 when linear color space is used on mobile platforms
        return decodeInstructions.x * color.rgb;
    }
    
    inline half3 DecodeLightmap( fixed4 color, half4 decodeInstructions)
    {
    #if defined(UNITY_LIGHTMAP_DLDR_ENCODING)
        return DecodeLightmapDoubleLDR(color, decodeInstructions);
    #elif defined(UNITY_LIGHTMAP_RGBM_ENCODING)
        return DecodeLightmapRGBM(color, decodeInstructions);
    #else //defined(UNITY_LIGHTMAP_FULL_HDR)
        return color.rgb;
    #endif
    }
    
    half4 unity_Lightmap_HDR;
    
    inline half3 DecodeLightmap( fixed4 color )
    {
        return DecodeLightmap( color, unity_Lightmap_HDR );
    }
    
    half4 unity_DynamicLightmap_HDR;
    
    // Decodes Enlighten RGBM encoded lightmaps
    // NOTE: Enlighten dynamic texture RGBM format is _different_ from standard Unity HDR textures
    // (such as Baked Lightmaps, Reflection Probes and IBL images)
    // Instead Enlighten provides RGBM texture in _Linear_ color space with _different_ exponent.
    // WARNING: 3 pow operations, might be very expensive for mobiles!
    inline half3 DecodeRealtimeLightmap( fixed4 color )
    {
        //@TODO: Temporary until Geomerics gives us an API to convert lightmaps to RGBM in gamma space on the enlighten thread before we upload the textures.
    #if defined(UNITY_FORCE_LINEAR_READ_FOR_RGBM)
        return pow ((unity_DynamicLightmap_HDR.x * color.a) * sqrt(color.rgb), unity_DynamicLightmap_HDR.y);
    #else
        return pow ((unity_DynamicLightmap_HDR.x * color.a) * color.rgb, unity_DynamicLightmap_HDR.y);
    #endif
    }
    
    inline half3 DecodeDirectionalLightmap (half3 color, fixed4 dirTex, half3 normalWorld)
    {
        // In directional (non-specular) mode Enlighten bakes dominant light direction
        // in a way, that using it for half Lambert and then dividing by a "rebalancing coefficient"
        // gives a result close to plain diffuse response lightmaps, but normalmapped.
    
        // Note that dir is not unit length on purpose. Its length is "directionality", like
        // for the directional specular lightmaps.
    
        half halfLambert = dot(normalWorld, dirTex.xyz - 0.5) + 0.5;
    
        return color * halfLambert / max(1e-4h, dirTex.w);
    }

// Decodes HDR textures
    // handles dLDR, RGBM formats
    inline half3 DecodeHDR (half4 data, half4 decodeInstructions)
    {
        // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
        half alpha = decodeInstructions.w * (data.a - 1.0) + 1.0;
    
        // If Linear mode is not supported we can skip exponent part
        #if defined(UNITY_COLORSPACE_GAMMA)
            return (decodeInstructions.x * alpha) * data.rgb;
        #else
        #   if defined(UNITY_USE_NATIVE_HDR)
                return decodeInstructions.x * data.rgb; // Multiplier for future HDRI relative to absolute conversion.
        #   else
                return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * data.rgb;
        #   endif
        #endif
    }

    

    // Encoding/decoding [0..1) floats into 8 bit/channel RGBA. Note that 1.0 will not be encoded properly.
    inline float4 EncodeFloatRGBA( float v )
    {
        float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
        float kEncodeBit = 1.0/255.0;
        float4 enc = kEncodeMul * v;
        enc = frac (enc);
        enc -= enc.yzww * kEncodeBit;
        return enc;
    }
    inline float DecodeFloatRGBA( float4 enc )
    {
        float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
        return dot( enc, kDecodeDot );
    }
    
    // Encoding/decoding [0..1) floats into 8 bit/channel RG. Note that 1.0 will not be encoded properly.
    inline float2 EncodeFloatRG( float v )
    {
        float2 kEncodeMul = float2(1.0, 255.0);
        float kEncodeBit = 1.0/255.0;
        float2 enc = kEncodeMul * v;
        enc = frac (enc);
        enc.x -= enc.y * kEncodeBit;
        return enc;
    }
    inline float DecodeFloatRG( float2 enc )
    {
        float2 kDecodeDot = float2(1.0, 1/255.0);
        return dot( enc, kDecodeDot );
    }
    
    
    // Encoding/decoding view space normals into 2D 0..1 vector
    inline float2 EncodeViewNormalStereo( float3 n )
    {
        float kScale = 1.7777;
        float2 enc;
        enc = n.xy / (n.z+1);
        enc /= kScale;
        enc = enc*0.5+0.5;
        return enc;
    }
    inline float3 DecodeViewNormalStereo( float4 enc4 )
    {
        float kScale = 1.7777;
        float3 nn = enc4.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
        float g = 2.0 / dot(nn.xyz,nn.xyz);
        float3 n;
        n.xy = g*nn.xy;
        n.z = g-1;
        return n;
    }
    
    inline float4 EncodeDepthNormal( float depth, float3 normal )
    {
        float4 enc;
        enc.xy = EncodeViewNormalStereo (normal);
        enc.zw = EncodeFloatRG (depth);
        return enc;
    }
    
    inline void DecodeDepthNormal( float4 enc, out float depth, out float3 normal )
    {
        depth = DecodeFloatRG (enc.zw);
        normal = DecodeViewNormalStereo (enc);
    }
    
    // Z buffer to linear 0..1 depth
    inline float Linear01Depth( float z )
    {
        return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
    }
    // Z buffer to linear depth
    inline float LinearEyeDepth( float z )
    {
        return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
    }


inline half3 SubtractMainLightWithRealtimeAttenuationFromLightmap (half3 lightmap, half attenuation, half4 bakedColorTex, half3 normalWorld)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    half3 shadowColor = unity_ShadowColor.rgb;
    half shadowStrength = _LightShadowData.x;
 
    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.
 
 
    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    //    Preserves bounce and other baked lights
    //    No shadows on the geometry facing away from the light
    half ndotl = saturate(dot(normalWorld, _WorldSpaceLightPos0.xyz));
    half3 estimatedLightContributionMaskedByInverseOfShadow = ndotl * (1- attenuation) * _LightColor0.rgb;
    half3 subtractedLightmap = lightmap - estimatedLightContributionMaskedByInverseOfShadow;
 
    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    half3 realtimeShadow = max(subtractedLightmap, shadowColor);
    realtimeShadow = lerp(realtimeShadow, lightmap, shadowStrength);
 
    // 3) Pick darkest color
    return min(lightmap, realtimeShadow);
}


//-----------------------------------------------------------------------------
// Helper to convert smoothness to roughness
//-----------------------------------------------------------------------------

// float PerceptualRoughnessToRoughness(float perceptualRoughness)
// {
//     return perceptualRoughness * perceptualRoughness;
// }

// half RoughnessToPerceptualRoughness(half roughness)
// {
//     return sqrt(roughness);
// }

// Smoothness is the user facing name
// it should be perceptualSmoothness but we don't want the user to have to deal with this name
half SmoothnessToRoughness(half smoothness)
{
    return (1 - smoothness) * (1 - smoothness);
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
    return (1 - smoothness);
}

// Define Specular cubemap constants
#ifndef UNITY_SPECCUBE_LOD_EXPONENT
#define UNITY_SPECCUBE_LOD_EXPONENT (1.5)
#endif
#ifndef UNITY_SPECCUBE_LOD_STEPS
#define UNITY_SPECCUBE_LOD_STEPS (6)
#endif

half3 BoxProjectedCubemapDirection(half3 reflectionWS, float3 positionWS, float4 cubemapPositionWS, float4 boxMin, float4 boxMax)
{
    #ifndef UNITY_SPECCUBE_BOX_PROJECTION
        return reflectionWS;
    #endif
    // Is this probe using box projection?
    
    #ifdef USE_URP_BOX_PROJECTION
    // Cursed way to get unity to send correct box min and max
    if (cubemapPositionWS.w <= 0.0f)
    {
    #else
    if (cubemapPositionWS.w > 0.0f)
    {
    #endif

        float3 boxMinMax = (reflectionWS > 0.0f) ? boxMax.xyz : boxMin.xyz;
        half3 rbMinMax = half3(boxMinMax - positionWS) / reflectionWS;

        half fa = half(min(min(rbMinMax.x, rbMinMax.y), rbMinMax.z));

        half3 worldPos = half3(positionWS - cubemapPositionWS.xyz);

        half3 result = worldPos + reflectionWS * fa;
        return result;
    }
    else
    {
        return reflectionWS;
    }
}

// ----------------------------------------------------------------------------
// GlossyEnvironment - Function to integrate the specular lighting with default sky or reflection probes
// ----------------------------------------------------------------------------
struct Unity_GlossyEnvironmentData
{
    // - Deferred case have one cubemap
    // - Forward case can have two blended cubemap (unusual should be deprecated).

    // Surface properties use for cubemap integration
    half    roughness; // CAUTION: This is perceptualRoughness but because of compatibility this name can't be change :(
    half3   reflUVW;
};

// ----------------------------------------------------------------------------

Unity_GlossyEnvironmentData UnityGlossyEnvironmentSetup(half Smoothness, half3 worldViewDir, half3 Normal, half3 fresnel0)
{
    Unity_GlossyEnvironmentData g;

    g.roughness /* perceptualRoughness */   = SmoothnessToPerceptualRoughness(Smoothness);
    g.reflUVW   = reflect(-worldViewDir, Normal);

    return g;
}

// ----------------------------------------------------------------------------
half perceptualRoughnessToMipmapLevel(half perceptualRoughness)
{
    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

// ----------------------------------------------------------------------------
half mipmapLevelToPerceptualRoughness(half mipmapLevel)
{
    return mipmapLevel / UNITY_SPECCUBE_LOD_STEPS;
}

// ----------------------------------------------------------------------------
half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBE(tex), half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
    half perceptualRoughness = glossIn.roughness /* perceptualRoughness */ ;

// TODO: CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!
// For now disabled
#if 0
    float m = PerceptualRoughnessToRoughness(perceptualRoughness); // m is the real roughness parameter
    const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
    float n =  (2.0/max(fEps, m*m))-2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

    n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

    perceptualRoughness = pow( 2/(n+2), 0.25);      // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
#else
    // MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
    perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif


    half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
    half3 R = glossIn.reflUVW;
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

    return DecodeHDR(rgbm, hdr);
}


// Transforms direction from object to world space
    inline float3 UnityObjectToWorldDir( in float3 dir )
    {
        return normalize(mul((float3x3)unity_ObjectToWorld, dir));
    }
    
    // Transforms direction from world to object space
    inline float3 UnityWorldToObjectDir( in float3 dir )
    {
        return normalize(mul((float3x3)unity_WorldToObject, dir));
    }
    
    // Transforms normal from object to world space
    inline float3 UnityObjectToWorldNormal( in float3 norm )
    {
    #ifdef UNITY_ASSUME_UNIFORM_SCALING
        return UnityObjectToWorldDir(norm);
    #else
        // mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
        return normalize(mul(norm, (float3x3)unity_WorldToObject));
    #endif
    }
    
    // Computes world space light direction, from world space position
    inline float3 UnityWorldSpaceLightDir( in float3 worldPos )
    {
        #ifndef USING_LIGHT_MULTI_COMPILE
            return _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
        #else
            #ifndef USING_DIRECTIONAL_LIGHT
            return _WorldSpaceLightPos0.xyz - worldPos;
            #else
            return _WorldSpaceLightPos0.xyz;
            #endif
        #endif
    }
    
    // Computes world space light direction, from object space position
    // *Legacy* Please use UnityWorldSpaceLightDir instead
    inline float3 WorldSpaceLightDir( in float4 localPos )
    {
        float3 worldPos = mul(unity_ObjectToWorld, localPos).xyz;
        return UnityWorldSpaceLightDir(worldPos);
    }
    
    // Computes object space light direction
    inline float3 ObjSpaceLightDir( in float4 v )
    {
        float3 objSpaceLightPos = mul(unity_WorldToObject, _WorldSpaceLightPos0).xyz;
        #ifndef USING_LIGHT_MULTI_COMPILE
            return objSpaceLightPos.xyz - v.xyz * _WorldSpaceLightPos0.w;
        #else
            #ifndef USING_DIRECTIONAL_LIGHT
            return objSpaceLightPos.xyz - v.xyz;
            #else
            return objSpaceLightPos.xyz;
            #endif
        #endif
    }

    // Computes object space view direction
    inline float3 ObjSpaceViewDir( in float4 v )
    {
        float3 objSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
        return objSpaceCameraPos - v.xyz;
    }

// Shadow caster pass helpers
    
    float4 UnityEncodeCubeShadowDepth (float z)
    {
        #ifdef UNITY_USE_RGBA_FOR_POINT_SHADOWS
        return EncodeFloatRGBA (min(z, 0.999));
        #else
        return z;
        #endif
    }
    
    float UnityDecodeCubeShadowDepth (float4 vals)
    {
        #ifdef UNITY_USE_RGBA_FOR_POINT_SHADOWS
        return DecodeFloatRGBA (vals);
        #else
        return vals.r;
        #endif
    }
    
    
    float4 UnityClipSpaceShadowCasterPos(float4 vertex, float3 normal)
    {
        float4 wPos = mul(unity_ObjectToWorld, vertex);
    
        if (unity_LightShadowBias.z != 0.0)
        {
            float3 wNormal = UnityObjectToWorldNormal(normal);
            float3 wLight = normalize(UnityWorldSpaceLightDir(wPos.xyz));
    
            // apply normal offset bias (inset position along the normal)
            // bias needs to be scaled by sine between normal and light direction
            // (http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/)
            //
            // unity_LightShadowBias.z contains user-specified normal offset amount
            // scaled by world space texel size.
    
            float shadowCos = dot(wNormal, wLight);
            float shadowSine = sqrt(1-shadowCos*shadowCos);
            float normalBias = unity_LightShadowBias.z * shadowSine;
    
            wPos.xyz -= wNormal * normalBias;
        }
    
        return mul(UNITY_MATRIX_VP, wPos);
    }

    float3 ApplyShadowBiasNormal(float3 positionWS, float3 normalWS)
    {
        float3 wPos = positionWS;
    
        if (unity_LightShadowBias.z != 0.0)
        {
            float3 wNormal = normalWS;
            float3 wLight = normalize(UnityWorldSpaceLightDir(wPos.xyz));
    
            // apply normal offset bias (inset position along the normal)
            // bias needs to be scaled by sine between normal and light direction
            // (http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/)
            //
            // unity_LightShadowBias.z contains user-specified normal offset amount
            // scaled by world space texel size.
    
            float shadowCos = dot(wNormal, wLight);
            float shadowSine = sqrt(1-shadowCos*shadowCos);
            float normalBias = unity_LightShadowBias.z * shadowSine;
    
            wPos.xyz -= wNormal * normalBias;
        }
    
        return wPos;
    }

    // Legacy, not used anymore; kept around to not break existing user shaders
    float4 UnityClipSpaceShadowCasterPos(float3 vertex, float3 normal)
    {
        return UnityClipSpaceShadowCasterPos(float4(vertex, 1), normal);
    }
    
    
    float4 UnityApplyLinearShadowBias(float4 clipPos)
    
    {
        // For point lights that support depth cube map, the bias is applied in the fragment shader sampling the shadow map.
        // This is because the legacy behaviour for point light shadow map cannot be implemented by offseting the vertex position
        // in the vertex shader generating the shadow map.
    #if !(defined(SHADOWS_CUBE) && defined(SHADOWS_CUBE_IN_DEPTH_TEX))
        #if defined(UNITY_REVERSED_Z)
            // We use max/min instead of clamp to ensure proper handling of the rare case
            // where both numerator and denominator are zero and the fraction becomes NaN.
            clipPos.z += max(-1, min(unity_LightShadowBias.x / clipPos.w, 0));
        #else
            clipPos.z += saturate(unity_LightShadowBias.x/clipPos.w);
        #endif
    #endif
    
    #if defined(UNITY_REVERSED_Z)
        float clamped = min(clipPos.z, clipPos.w*UNITY_NEAR_CLIP_VALUE);
    #else
        float clamped = max(clipPos.z, clipPos.w*UNITY_NEAR_CLIP_VALUE);
    #endif
        clipPos.z = lerp(clipPos.z, clamped, unity_LightShadowBias.y);
        return clipPos;
    }
    
    
    #if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
        // Rendering into point light (cubemap) shadows
        #define V2F_SHADOW_CASTER_NOPOS float3 vec : TEXCOORD0;
        #define TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,opos) o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz; opos = UnityObjectToClipPos(v.vertex);
        #define TRANSFER_SHADOW_CASTER_NOPOS(o,opos) o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz; opos = UnityObjectToClipPos(v.vertex);
        #define SHADOW_CASTER_FRAGMENT(i) return UnityEncodeCubeShadowDepth ((length(i.vec) + unity_LightShadowBias.x) * _LightPositionRange.w);
    
    #else
        // Rendering into directional or spot light shadows
        #define V2F_SHADOW_CASTER_NOPOS
        // Let embedding code know that V2F_SHADOW_CASTER_NOPOS is empty; so that it can workaround
        // empty structs that could possibly be produced.
        #define V2F_SHADOW_CASTER_NOPOS_IS_EMPTY
        #define TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,opos) \
            opos = UnityObjectToClipPos(v.vertex.xyz); \
            opos = UnityApplyLinearShadowBias(opos);
        #define TRANSFER_SHADOW_CASTER_NOPOS(o,opos) \
            opos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal); \
            opos = UnityApplyLinearShadowBias(opos);
        #define SHADOW_CASTER_FRAGMENT(i) return 0;
    #endif
    
    // Declare all data needed for shadow caster pass output (any shadow directions/depths/distances as needed),
    // plus clip space position.
    #define V2F_SHADOW_CASTER V2F_SHADOW_CASTER_NOPOS UNITY_POSITION(pos)
    
    // Vertex shader part, with support for normal offset shadows. Requires
    // position and normal to be present in the vertex input.
    #define TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) TRANSFER_SHADOW_CASTER_NOPOS(o,o.pos)
    
    // Vertex shader part, legacy. No support for normal offset shadows - because
    // that would require vertex normals, which might not be present in user-written shaders.
    #define TRANSFER_SHADOW_CASTER(o) TRANSFER_SHADOW_CASTER_NOPOS_LEGACY(o,o.pos)

half3 ShadeSHPerVertex (half3 normal, half3 ambient)
{
    #if UNITY_SAMPLE_FULL_SH_PER_PIXEL
        // Completely per-pixel
        // nothing to do here
    #elif (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        // Completely per-vertex
        ambient += max(half3(0,0,0), ShadeSH9 (half4(normal, 1.0)));
    #else
        // L2 per-vertex, L0..L1 & gamma-correction per-pixel

        // NOTE: SH data is always in Linear AND calculation is split between vertex & pixel
        // Convert ambient to Linear and do final gamma-correction at the end (per-pixel)
        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = GammaToLinearSpace (ambient);
        #endif
        ambient += SHEvalLinearL2 (half4(normal, 1.0));     // no max since this is only L2 contribution
    #endif

    return ambient;
}

half3 ShadeSHPerPixel(half3 normal, half3 ambient, float3 worldPos)
{
    half3 ambient_contrib = 0.0;

    #if UNITY_SAMPLE_FULL_SH_PER_PIXEL
        // Completely per-pixel
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            if (unity_ProbeVolumeParams.x == 1.0)
                ambient_contrib = SHEvalLinearL0L1_SampleProbeVolume(half4(normal, 1.0), worldPos);
            else
                ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
        #else
            ambient_contrib = SHEvalLinearL0L1(half4(normal, 1.0));
        #endif

            ambient_contrib += SHEvalLinearL2(half4(normal, 1.0));

            ambient += max(half3(0, 0, 0), ambient_contrib);

        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = LinearToGammaSpace(ambient);
        #endif
    #elif (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        // Completely per-vertex
        // nothing to do here. Gamma conversion on ambient from SH takes place in the vertex shader, see ShadeSHPerVertex.
    #else
        // L2 per-vertex, L0..L1 & gamma-correction per-pixel
        // Ambient in this case is expected to be always Linear, see ShadeSHPerVertex()
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            if (unity_ProbeVolumeParams.x == 1.0)
                ambient_contrib = SHEvalLinearL0L1_SampleProbeVolume (half4(normal, 1.0), worldPos);
            else
                ambient_contrib = SHEvalLinearL0L1 (half4(normal, 1.0));
        #else
            ambient_contrib = SHEvalLinearL0L1 (half4(normal, 1.0));
        #endif

        ambient = max(half3(0, 0, 0), ambient+ambient_contrib);     // include L2 contribution in vertex shader before clamp.
        #ifdef UNITY_COLORSPACE_GAMMA
            ambient = LinearToGammaSpace (ambient);
        #endif
    #endif

    return ambient;
}