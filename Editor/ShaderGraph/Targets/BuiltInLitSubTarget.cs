using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.ShaderGraph.Internal;
using System.Runtime.CompilerServices;
using static UnityEditor.ShaderData;
using NUnit.Framework;

namespace z3y.BuiltIn.ShaderGraph
{
    sealed class BuiltInLitSubTarget : BuiltInSubTarget
    {
        public BuiltInLitSubTarget()
        {
            displayName = "Lit";
        }

        static readonly GUID kSourceCodeGuid = new GUID("abffa6af2d0406140b707b5cf48edc42"); // BuiltInLitSubTarget.cs

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        [SerializeField] public bool surfaceOverride = false;
        [SerializeField] public bool bakeryMonoSH = false;
        [SerializeField] public bool nonLinearLightMapSH = false;
        [SerializeField] public bool nonLinearLightProbeSH = false;
        [SerializeField] public bool gsaa = false;
        [SerializeField] public bool bicubicLightmap = false;
        [SerializeField] public bool anisotropy = false;
        [SerializeField] public bool screenSpaceReflections = false;
        [SerializeField] public bool specular = true;
        [SerializeField] public bool lightmappedSpecular = false;
        [SerializeField] public bool flatLit = false;
        protected override ShaderID shaderID => ShaderID.SG_Lit;

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            // If there is a custom GUI, we need to leave out the subtarget GUI or it will override the custom one due to ordering
            // (Subtarget setup happens first, so it would always "win")
            var biTarget = target as BuiltInTarget;
            /*if (!context.HasCustomEditorForRenderPipeline(null) && string.IsNullOrEmpty(biTarget.customEditorGUI))
                context.customEditorForRenderPipelines.Add((typeof(BuiltInLitGUI).FullName, ""));*/
            // Override EditorGUI
#if UNITY_2022_1_OR_NEWER
            bool hasCustomEditorForRenderPipeline = context.HasCustomEditorForRenderPipeline("");
#else
            bool hasCustomEditorForRenderPipeline = context.HasCustomEditorForRenderPipeline(null);
#endif
            if (!hasCustomEditorForRenderPipeline && string.IsNullOrEmpty(biTarget.customEditorGUI))
            {
#if UNITY_2022_1_OR_NEWER
                context.AddCustomEditorForRenderPipeline(BuiltInTarget.kDefaultGUI, "");
#else
                context.customEditorForRenderPipelines.Add((BuiltInTarget.kDefaultGUI, ""));
#endif
            }

            // Process SubShaders
            context.AddSubShader(SubShaders.Lit(target, target.renderType, target.renderQueue));
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                material.SetFloat(Property.Surface(), (float)target.surfaceType);
                material.SetFloat(Property.Blend(), (float)target.alphaMode);
                material.SetFloat(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
                material.SetFloat(Property.Cull(), (int)target.renderFace);
                material.SetFloat(Property.ZWriteControl(), (float)target.zWriteControl);
                material.SetFloat(Property.ZTest(), (float)target.zTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            material.SetFloat(Property.QueueOffset(), 0.0f);
            material.SetFloat(Property.QueueControl(), (float)BuiltInBaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            BuiltInLitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);

            // Lit
            context.AddField(BuiltInFields.NormalDropOffOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddField(BuiltInFields.NormalDropOffTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddField(BuiltInFields.NormalDropOffWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddField(BuiltInFields.Normal, descs.Contains(BlockFields.SurfaceDescription.NormalOS) ||
                                                   descs.Contains(BlockFields.SurfaceDescription.NormalTS) ||
                                                   descs.Contains(BlockFields.SurfaceDescription.NormalWS));
        }
        [GenerateBlocks]
        public struct AdditionalSurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor Reflectance = new BlockFieldDescriptor(name, "Reflectance", "Reflectance", "SURFACEDESCRIPTION_REFLECTANCE",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularOcclusion = new BlockFieldDescriptor(name, "SpecularOcclusion", "SpecularOcclusion", "SURFACEDESCRIPTION_SPECULAROCCLUSION",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor GSAAVariance = new BlockFieldDescriptor(name, "GSAAVariance", "GSAA Variance", "SURFACEDESCRIPTION_GSAAVARIANCE",
                new FloatControl(0.15f), ShaderStage.Fragment);
            public static BlockFieldDescriptor GSAAThreshold = new BlockFieldDescriptor(name, "GSAAThreshold", "GSAA Threshold", "SURFACEDESCRIPTION_GSAATHRESHOLD",
                new FloatControl(0.1f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Tangent = new BlockFieldDescriptor(name, "Tangent", "Tangent", "SURFACEDESCRIPTION_TANGENT",
                new TangentControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(name, "Anisotropy", "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY",
                new FloatControl(0.0f), ShaderStage.Fragment);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.NormalOS, normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS, normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.NormalWS, normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
            context.AddBlock(AdditionalSurfaceDescription.Reflectance);
            context.AddBlock(AdditionalSurfaceDescription.SpecularOcclusion);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, (target.alphaClip) || target.allowMaterialOverride);

            context.AddBlock(AdditionalSurfaceDescription.GSAAVariance, gsaa || surfaceOverride);
            context.AddBlock(AdditionalSurfaceDescription.GSAAThreshold, gsaa || surfaceOverride);

            context.AddBlock(AdditionalSurfaceDescription.Tangent, anisotropy || surfaceOverride);
            context.AddBlock(AdditionalSurfaceDescription.Anisotropy, anisotropy || surfaceOverride);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
            {
                base.CollectShaderProperties(collector, generationMode);

                // setup properties using the defaults
                collector.AddFloatProperty(Property.Surface(), (float)target.surfaceType);
                collector.AddFloatProperty(Property.Blend(), (float)target.alphaMode);
                collector.AddFloatProperty(Property.AlphaClip(), target.alphaClip ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.SrcBlend(), 1.0f);    // always set by material inspector (TODO : get src/dst blend and set here?)
                collector.AddFloatProperty(Property.DstBlend(), 0.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.ZWrite(), (target.surfaceType == SurfaceType.Opaque) ? 1.0f : 0.0f);
                collector.AddFloatProperty(Property.ZWriteControl(), (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest(), (float)target.zTestMode);    // ztest mode is designed to directly pass as ztest
                collector.AddFloatProperty(Property.Cull(), (float)target.renderFace);    // render face enum is designed to directly pass as a cull mode

            }

            if (surfaceOverride)
            {
                collector.AddFloatProperty("[Toggle(BAKERY_MONOSH)]_BakeryMonoSH", bakeryMonoSH ? 1 : 0, "Bakery MonoSH");
                collector.AddFloatProperty("[Toggle(BICUBIC_LIGHTMAP)]_BicubicLightmap", bicubicLightmap ? 1 : 0, "Bicubic Lightmap");
                collector.AddFloatProperty("[Toggle(_LIGHTMAPPED_SPECULAR)]_LightmappedSpecular", lightmappedSpecular ? 1 : 0, "Lightmapped Specular");
                collector.AddFloatProperty("[Toggle(NONLINEAR_LIGHTMAP_SH)]_NonLinearLightMapSH", nonLinearLightMapSH ? 1 : 0, "Non Linear Lightmap SH");
                collector.AddFloatProperty("[Toggle(NONLINEAR_LIGHTPROBESH)]_NonLinearLightProbeSH", nonLinearLightProbeSH ? 1 : 0, "Non Linear Lightprobe SH");
                collector.AddFloatProperty("[Toggle(_GEOMETRICSPECULAR_AA)]_GeometricSpecularAAToggle", gsaa ? 1 : 0, "Geometric Specular AA");
                collector.AddFloatProperty("[Toggle(_ANISOTROPY)]_AnisotropyToggle", anisotropy ? 1 : 0, "Anisotropy");
            }

            if (specular)
            {
                collector.AddFloatProperty("[ToggleOff(_SPECULARHIGHLIGHTS_OFF)]_SpecularHighlights", 1, "Specular Highlights");
                collector.AddFloatProperty("[ToggleOff(_GLOSSYREFLECTIONS_OFF)]_GlossyReflections", 1, "Reflections");
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
            collector.AddFloatProperty(Property.QueueControl(), -1.0f);

            var dfgTex = new SerializableTexture
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/io.z3y.github.shadergraph/ShaderLibrary/dfg-multiscatter.exr"),
            };

            collector.AddShaderProperty(new Texture2DShaderProperty()
            {
                overrideReferenceName = "_DFG",
                modifiable = false,
                hidden = true,
                value = dfgTex,
            });

            if (screenSpaceReflections)
            {
                var blueNoiseTex = new SerializableTexture
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/io.z3y.github.shadergraph/ShaderLibrary/LDR_LLL1_0.png"),
                };
                collector.AddShaderProperty(new Texture2DShaderProperty()
                {
                    overrideReferenceName = "BlueNoise",
                    modifiable = false,
                    hidden = true,
                    value = blueNoiseTex,
                });
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // Temporarily remove the workflow mode until specular is supported
            //context.AddProperty("Workflow", new EnumField(WorkflowMode.Metallic) { value = workflowMode }, (evt) =>
            //{
            //    if (Equals(workflowMode, evt.newValue))
            //        return;

            //    registerUndo("Change Workflow");
            //    workflowMode = (WorkflowMode)evt.newValue;
            //    onChange();
            //});

            // show the target default surface properties
            var builtInTarget = (target as BuiltInTarget);
            builtInTarget?.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            var properties = new GraphInspectorGUI(context, onChange, registerUndo);
            properties.Draw(target);
//            builtInTarget?.GetDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo);

            // context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = normalDropOffSpace }, (evt) =>
            // {
            //     if (Equals(normalDropOffSpace, evt.newValue))
            //         return;

            //     registerUndo("Change Fragment Normal Space");
            //     normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
            //     onChange();
            // });

            //properties.DrawLitProperties(this);
        }

        #region SubShader
        static class SubShaders
        {
            // Overloads to do inline PassDescriptor modifications
            // NOTE: param order should match PassDescriptor field order for consistency
            #region PassVariant
            private static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
            {
                var result = source;
                result.pragmas = pragmas;
                return result;
            }

            private static PassDescriptor PassVariant(in PassDescriptor source, BlockFieldDescriptor[] vertexBlocks, BlockFieldDescriptor[] pixelBlocks, PragmaCollection pragmas, DefineCollection defines)
            {
                var result = source;
                result.validVertexBlocks = vertexBlocks;
                result.validPixelBlocks = pixelBlocks;
                result.pragmas = pragmas;
                result.defines = defines;
                return result;
            }

            #endregion

            // SM 2.0
            public static SubShaderDescriptor Lit(BuiltInTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor()
                {
                    //pipelineTag = BuiltInTarget.kPipelineTag,
                    customTags = target.AppendAdditionalTags(BuiltInTarget.kLitMaterialTypeTag),
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection(),
                };

                if (target.grabPass) // TODO: check and disable for android
                {
                    result.passes.Add(CorePasses.GrabPassDescriptor(target));
                }

                var basePass = LitPasses.Forward(target);
                if (target.alphaToMask)
                {
                    CoreRenderStates.AppendAlphaToMask(target, basePass.renderStates);
                }
                if (target.generateOutlinePass && target.generateOutlineEarly)
                {
                    result.passes.Add(CorePasses.OutlineDescriptor(basePass, target));
                }
                result.passes.Add(basePass);
                if (target.generateOutlinePass && !target.generateOutlineEarly)
                {
                    result.passes.Add(CorePasses.OutlineDescriptor(basePass, target));
                }

                result.passes.Add(LitPasses.ForwardAdd(target));
                result.passes.Add(CorePasses.ShadowCaster(target));
                result.passes.Add(LitPasses.Meta(target));
                return result;
            }
        }
        #endregion

        #region Passes
        static class LitPasses
        {
            public static PassDescriptor Forward(BuiltInTarget target)
            {
                var renderStates = CoreRenderStates.Default(target);
                if (target.stencilEnabled)
                {
                    renderStates = CoreRenderStates.CopyAndAppendStencil(target, renderStates);
                }

                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn Forward",
                    referenceName = "SHADERPASS_FORWARD",
                    lightMode = "ForwardBase",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = renderStates,
                    pragmas = CorePragmas.Forward,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.Forward },
                    includes = LitIncludes.Forward,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                AddForwardSurfaceControlsToPass(ref result, target);

                if (target.activeSubTarget is BuiltInLitSubTarget lit)
                {
                    LitKeywords.DeclareAndAppend(ref result, "FLATLIT", true, lit.flatLit);


                    result.keywords.Add(LitKeywords.VertexLightsFragment);
                    bool predefined = !lit.surfaceOverride;

                    LitKeywords.DeclareAndAppend(ref result, "_GLOSSYREFLECTIONS_OFF", !lit.specular, true);
                    LitKeywords.DeclareAndAppend(ref result, "_SPECULARHIGHLIGHTS_OFF", !lit.specular, true);
                    LitKeywords.DeclareAndAppend(ref result, "_LIGHTMAPPED_SPECULAR", predefined, lit.lightmappedSpecular);
                    LitKeywords.DeclareAndAppend(ref result, "ALPHATOCOVERAGE_ON", true, target.alphaToMask);

                    LitKeywords.DeclareAndAppend(ref result, "BAKERY_MONOSH", predefined, lit.bakeryMonoSH);
                    LitKeywords.DeclareAndAppend(ref result, "BICUBIC_LIGHTMAP", predefined, lit.bicubicLightmap);
                    LitKeywords.DeclareAndAppend(ref result, "NONLINEAR_LIGHTMAP_SH", predefined, lit.nonLinearLightMapSH);
                    LitKeywords.DeclareAndAppend(ref result, "NONLINEAR_LIGHTPROBESH", predefined, lit.nonLinearLightProbeSH);
                    LitKeywords.DeclareAndAppend(ref result, "_GEOMETRICSPECULAR_AA", predefined, lit.gsaa);
                    LitKeywords.DeclareAndAppend(ref result, "_ANISOTROPY", predefined, lit.anisotropy);

                    LitKeywords.DeclareAndAppend(ref result, "_SSR", lit.screenSpaceReflections, true);
                }
                return result;
            }

            public static PassDescriptor ForwardAdd(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "BuiltIn ForwardAdd",
                    referenceName = "SHADERPASS_FORWARD_ADD",
                    lightMode = "ForwardAdd",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentLit,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Forward,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.ForwardAdd(target),
                    pragmas = CorePragmas.ForwardAdd,     // NOTE: SM 2.0 only GL
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection { LitKeywords.ForwardAdd },
                    includes = LitIncludes.ForwardAdd,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                AddForwardAddControlsToPass(ref result, target);
                if (target.activeSubTarget is BuiltInLitSubTarget lit)
                {
                    LitKeywords.DeclareAndAppend(ref result, "FLATLIT", true, lit.flatLit);

                    LitKeywords.DeclareAndAppend(ref result, "_SPECULARHIGHLIGHTS_OFF", !lit.specular, true);
                    LitKeywords.DeclareAndAppend(ref result, "ALPHATOCOVERAGE_ON", true, target.alphaToMask);
                    LitKeywords.DeclareAndAppend(ref result, "_GEOMETRICSPECULAR_AA", !lit.surfaceOverride, lit.gsaa);
                    LitKeywords.DeclareAndAppend(ref result, "_ANISOTROPY", !lit.surfaceOverride, lit.anisotropy);
                }

                return result;
            }

            public static PassDescriptor Meta(BuiltInTarget target)
            {
                var result = new PassDescriptor()
                {
                    // Definition
                    displayName = "Meta",
                    referenceName = "SHADERPASS_META",
                    lightMode = "Meta",

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = LitBlockMasks.FragmentMeta,

                    // Fields
                    structs = CoreStructCollections.Default,
                    requiredFields = LitRequiredFields.Meta,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = CoreRenderStates.Meta,
                    pragmas = CorePragmas.Default,
                    defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                    keywords = new KeywordCollection(),
                    
                    includes = LitIncludes.Meta,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };

                result.keywords.Add(LitKeywords.EditorVis);
                result.fieldDependencies.Add(new FieldDependency(StructFields.Varyings.positionWS, BuiltInStructFields.Varyings.editorVisualizationVizUV));
                result.fieldDependencies.Add(new FieldDependency(StructFields.Varyings.positionWS, BuiltInStructFields.Varyings.editorVisualizationLightCoord));


                AddMetaControlsToPass(ref result, target);
                return result;
            }

            internal static void AddForwardSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddForwardAddControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddSurfaceTypeControlToPass(ref pass, target);
                CorePasses.AddAlphaClipControlToPass(ref pass, target);
            }

            internal static void AddForwardOnlyControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddDeferredControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddTargetSurfaceControlsToPass(ref pass, target);
            }

            internal static void AddMetaControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
            {
                CorePasses.AddSurfaceTypeControlToPass(ref pass, target);
                CorePasses.AddAlphaClipControlToPass(ref pass, target);
            }
        }
        #endregion

        #region PortMasks
        public static class LitBlockMasks
        {
            public static readonly BlockFieldDescriptor[] FragmentLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                //BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                AdditionalSurfaceDescription.Reflectance,
                AdditionalSurfaceDescription.SpecularOcclusion,
                AdditionalSurfaceDescription.GSAAVariance,
                AdditionalSurfaceDescription.GSAAThreshold,
                AdditionalSurfaceDescription.Tangent,
                AdditionalSurfaceDescription.Anisotropy,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static readonly BlockFieldDescriptor[] FragmentMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };
        }
        #endregion

        #region RequiredFields
        static class LitRequiredFields
        {
            public static readonly FieldCollection Forward = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,                        // needed for vertex lighting
                BuiltInStructFields.Varyings.lightmapUV,
                BuiltInStructFields.Varyings.lightmapUVAndRT,
                BuiltInStructFields.Varyings.vertexLight,
                BuiltInStructFields.Varyings.fogCoord,
                BuiltInStructFields.Varyings.shadowCoord,             // shadow coord, vert input is dependency
            };

            public static readonly FieldCollection Meta = new FieldCollection()
            {
                StructFields.Attributes.uv1,                            // needed for meta vertex position
                StructFields.Attributes.uv2,                            //needed for meta vertex position
                BuiltInStructFields.Varyings.editorVisualizationVizUV,
                BuiltInStructFields.Varyings.editorVisualizationLightCoord,
            };
        }
        #endregion

        #region Defines

        #endregion

        #region Keywords
        public static class LitKeywords
        {
            public static readonly KeywordCollection Forward = new KeywordCollection
            {
                /*{ CoreKeywordDescriptors.Lightmap },
                { CoreKeywordDescriptors.DirectionalLightmapCombined },
                { CoreKeywordDescriptors.MainLightShadows },
                { CoreKeywordDescriptors.AdditionalLights },
                { CoreKeywordDescriptors.AdditionalLightShadows },
                { CoreKeywordDescriptors.ShadowsSoft },
                { CoreKeywordDescriptors.LightmapShadowMixing },
                { CoreKeywordDescriptors.ShadowsShadowmask },*/
            };

            public static readonly KeywordCollection ForwardAdd = new KeywordCollection
            {
            };

            public static readonly KeywordDescriptor VertexLightsFragment = new KeywordDescriptor()
            {
                displayName = "VERTEXLIGHT_ON",
                referenceName = "VERTEXLIGHT_ON",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
                stages = KeywordShaderStage.Fragment,
            };

            public static readonly KeywordDescriptor EditorVis = new KeywordDescriptor()
            {
                displayName = "EDITOR_VISUALIZATION",
                referenceName = "EDITOR_VISUALIZATION",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
                stages = KeywordShaderStage.All,
            };

            public static void DeclareAndAppend(ref PassDescriptor pass, string keyword, bool isPredefined, bool enabled)
            {
                var keyword1 = new KeywordDescriptor()
                {
                    displayName = keyword,
                    referenceName = keyword,
                    type = KeywordType.Boolean,
                    definition = isPredefined ? KeywordDefinition.Predefined : KeywordDefinition.ShaderFeature,
                    scope = KeywordScope.Local,
                    stages = KeywordShaderStage.All,
                };
                if (isPredefined)
                {
                    pass.defines.Add(keyword1, enabled ? 1: 0);
                }
                else
                {
                    pass.keywords.Add(keyword1);
                }
            }

        }
        #endregion

        #region Includes
        static class LitIncludes
        {
            //const string kShadows = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shadows.hlsl";
            //const string kMetaInput = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/MetaInput.hlsl";
            //const string kForwardPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl";
            //const string kForwardAddPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardAddPass.hlsl";
            //const string kDeferredPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRDeferredPass.hlsl";
            //const string kGBuffer = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/UnityGBuffer.hlsl";
            //const string kPBRGBufferPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl";
            //const string kLightingMetaPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl";

            private const string kForwardFragment = "Packages/io.z3y.github.shadergraph/ShaderLibrary/Fragment.hlsl";
            private const string kMetaFragment = "Packages/io.z3y.github.shadergraph/ShaderLibrary/FragmentMeta.hlsl";

            public static readonly IncludeCollection Forward = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardFragment, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection ForwardAdd = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kForwardFragment, IncludeLocation.Postgraph },
            };

            public static readonly IncludeCollection Meta = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kMetaFragment, IncludeLocation.Postgraph },
            };

        }
        #endregion
    }
}
