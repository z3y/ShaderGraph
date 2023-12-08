#ifdef BAKERY_MONOSH
void BakeryMonoSH(ShaderData sd, SurfaceDescription surf, float2 lmUV, inout half3 diffuseColor, inout half3 specularColor, out half3 bentLight)
{
    half roughness = max(sd.perceptualRoughness * sd.perceptualRoughness, 0.002);
    half3 L0 = diffuseColor;

    //float3 dominantDir = unity_LightmapInd.SampleLevel(custom_bilinear_clamp_sampler, lmUV, 0).xyz;
    float3 dominantDir = BicubicSampling::SampleBicubic(unity_LightmapInd, custom_bilinear_clamp_sampler, lmUV, BicubicSampling::GetTexelSize(unity_LightmapInd)).xyz;
    

    float3 nL1 = dominantDir * 2 - 1;
    float3 L1x = nL1.x * L0 * 2;
    float3 L1y = nL1.y * L0 * 2;
    float3 L1z = nL1.z * L0 * 2;
    half3 sh;

    bentLight = (dot(nL1, sd.reflectionDirection) + 1.0) * L0 * 2;
    bentLight = max(bentLight, 0.0);

#ifdef NONLINEAR_LIGHTMAP_SH
    float lumaL0 = dot(L0, 1);
    float lumaL1x = dot(L1x, 1);
    float lumaL1y = dot(L1y, 1);
    float lumaL1z = dot(L1z, 1);
    float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), sd.normalWS);

    sh = L0 + sd.normalWS.x * L1x + sd.normalWS.y * L1y + sd.normalWS.z * L1z;
    float regularLumaSH = dot(sh, 1);
    //sh *= regularLumaSH < 0.001 ? 1 : (lumaSH / regularLumaSH);
    sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH*16));

    //sh.r = shEvaluateDiffuseL1Geomerics(L0.r, float3(L1x.r, L1y.r, L1z.r), sd.normalWS);
    //sh.g = shEvaluateDiffuseL1Geomerics(L0.g, float3(L1x.g, L1y.g, L1z.g), sd.normalWS);
    //sh.b = shEvaluateDiffuseL1Geomerics(L0.b, float3(L1x.b, L1y.b, L1z.b), sd.normalWS);

#else
    sh = L0 + sd.normalWS.x * L1x + sd.normalWS.y * L1y + sd.normalWS.z * L1z;
#endif


    diffuseColor = max(sh, 0.0);

#ifdef APPROXIMATE_AREALIGHT_SPECULAR
    half smoothness = 1.0f - roughness;
    half3 directionLength = saturate(length(nL1));
    smoothness *= sqrt(directionLength);
    roughness = 1.0f - smoothness;
#endif

    specularColor = 0;
    #ifdef _LIGHTMAPPED_SPECULAR
        dominantDir = nL1;
        //float focus = saturate(length(dominantDir));
        half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + sd.viewDirectionWS);
        half nh = saturate(dot(sd.normalWS, halfDir));
        half spec = Filament::D_GGX(nh, roughness);

        sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
        
        //specularColor = max(spec * sh, 0.0);

        #ifdef _ANISOTROPY
            half at = max(roughness * (1.0 + surf.Anisotropy), 0.001);
            half ab = max(roughness * (1.0 - surf.Anisotropy), 0.001);

            specularColor = max(Filament::D_GGX_Anisotropic(nh, halfDir, sd.tangentWS, sd.bitangentWS, at, ab) * sh, 0.0);

        #else
           specularColor = max(spec * sh, 0.0);
        #endif
        

    #endif

    
}
#endif