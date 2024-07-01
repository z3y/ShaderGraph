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

namespace z3y.BuiltIn.ShaderGraph
{
    sealed class BuiltInUnlitSubTarget : BuiltInSubTarget
    {
        static readonly GUID kSourceCodeGuid = new GUID("665766aefd1f8ca4c99e8bf6e4c4a988"); // BuiltInUnlitSubTarget.cs

        public BuiltInUnlitSubTarget()
        {
            displayName = "Unlit";
        }

        protected override ShaderID shaderID => ShaderID.SG_Unlit;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            // If there is a custom GUI, we need to leave out the subtarget GUI or it will override the custom one due to ordering
            // (Subtarget setup happens first, so it would always "win")
            var biTarget = target as BuiltInTarget;
            /*if (!context.HasCustomEditorForRenderPipeline(null) && string.IsNullOrEmpty(biTarget.customEditorGUI))
                context.customEditorForRenderPipelines.Add((typeof(BuiltInUnlitGUI).FullName, ""));*/
            
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
            context.AddSubShader(SubShaders.Unlit(target, target.renderType, target.renderQueue));
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
            BuiltInUnlitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.Alpha, (target.surfaceType == SurfaceType.Transparent || target.alphaClip) || target.allowMaterialOverride);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip || target.allowMaterialOverride);

            /*context.AddBlock(CoreBlockMasks.VertexDescriptionOutline.Position, target.generateOutlinePass);
            context.AddBlock(CoreBlockMasks.SurfaceDescriptionOutline.BaseColor, target.generateOutlinePass);*/
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

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
            collector.AddFloatProperty(Property.QueueControl(), -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // show the target default surface properties
            var builtInTarget = (target as BuiltInTarget);
            builtInTarget?.AddDefaultMaterialOverrideGUI(ref context, onChange, registerUndo);
            //builtInTarget?.GetDefaultSurfacePropertiesGUI(ref context, onChange, registerUndo);
            var properties = new GraphInspectorGUI(context, onChange, registerUndo);
            properties.Draw(target);
        }

        #region SubShader
        static class SubShaders
        {
            public static SubShaderDescriptor Unlit(BuiltInTarget target, string renderType, string renderQueue)
            {
                var result = new SubShaderDescriptor()
                {
                    //pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = target.AppendAdditionalTags(BuiltInTarget.kUnlitMaterialTypeTag),
                    renderType = renderType,
                    renderQueue = renderQueue,
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                if (target.grabPass) // TODO: check and disable for android
                {
                    result.passes.Add(CorePasses.GrabPassDescriptor(target));
                }

                var basePass = UnlitPasses.Unlit(target);
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
                result.passes.Add(CorePasses.ShadowCaster(target));

                return result;
            }
        }
        #endregion

        #region Pass
        static class UnlitPasses
        {
            public static PassDescriptor Unlit(BuiltInTarget target)
            {
                var renderStates = CoreRenderStates.Default(target);
                if (target.stencilEnabled)
                {
                    renderStates = CoreRenderStates.CopyAndAppendStencil(target, renderStates);
                }

                var requiredFields =  new FieldCollection()
                {
                    BuiltInStructFields.Varyings.fogCoord,
                    StructFields.Varyings.positionWS
                };

                var result = new PassDescriptor
                {
                    // Definition
                    displayName = "Pass",
                    referenceName = "SHADERPASS_UNLIT",
                    lightMode = "ForwardBase",
                    useInPreview = true,

                    // Template
                    passTemplatePath = BuiltInTarget.kTemplatePath,
                    sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = CoreBlockMasks.Vertex,
                    validPixelBlocks = CoreBlockMasks.FragmentColorAlpha,

                    // Fields
                    structs = CoreStructCollections.Default,
                    fieldDependencies = CoreFieldDependencies.Default,
                    requiredFields = requiredFields,

                    // Conditional State
                    renderStates = renderStates,
                    pragmas = CorePragmas.Forward, // i guess we need keywords for certain things to work, so not ForwardUnlit
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = UnlitIncludes.Unlit,

                    // Custom Interpolator Support
                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
                BuiltInLitSubTarget.LitKeywords.DeclareAndAppend(ref result, "ALPHATOCOVERAGE_ON", true, target.alphaToMask);

                CorePasses.AddTargetSurfaceControlsToPass(ref result, target);
                return result;
            }
        }
        #endregion

        #region Keywords
        static class UnlitKeywords
        {
            public static KeywordCollection Unlit(BuiltInTarget target)
            {
                var result = new KeywordCollection
                {

                };

                return result;
            }
        }
        #endregion

        #region Includes
        static class UnlitIncludes
        {
            const string kUnlitFragment = "Packages/io.z3y.github.shadergraph/ShaderLibrary/FragmentUnlit.hlsl";

            public static IncludeCollection Unlit = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kUnlitFragment, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
