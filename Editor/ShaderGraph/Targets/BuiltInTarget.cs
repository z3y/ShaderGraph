using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using Toggle = UnityEngine.UIElements.Toggle;

namespace z3y.BuiltIn.ShaderGraph
{
    enum MaterialType
    {
        Lit,
        UnLit
    }

    enum SurfaceType
    {
        Opaque,
        Transparent,
    }

    enum ZWriteControl
    {
        Auto = 0,
        ForceEnabled = 1,
        ForceDisabled = 2
    }

    enum ZTestMode  // the values here match UnityEngine.Rendering.CompareFunction
    {
        // Disabled = 0, "Disabled" option is invalid for actual use in shaders
        Never = 1,
        Less = 2,
        Equal = 3,
        LEqual = 4,     // default for most rendering
        Greater = 5,
        NotEqual = 6,
        GEqual = 7,
        Always = 8,
    }

    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply,
    }
    public enum RenderFace
    {
        Front = 2,      // = CullMode.Back -- render front face only
        Back = 1,       // = CullMode.Front -- render back face only
        Both = 0        // = CullMode.Off -- render both faces
    }

    sealed class BuiltInTarget : Target, IHasMetadata
    {
        public override int latestVersion => 2;

        // Constants
        static readonly GUID kSourceCodeGuid = new GUID("3d0a134923fab594581784302f860efb"); // BuiltInTarget.cs
        public const string kPipelineTag = "BuiltInPipeline";
        public const string kLitMaterialTypeTag = "\"BuiltInMaterialType\" = \"Lit\"";
        public const string kUnlitMaterialTypeTag = "\"BuiltInMaterialType\" = \"Unlit\"";
        public static readonly string[] kSharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories().Union(new string[] { "Packages/io.z3y.github.shadergraph/Editor/ShaderGraph/Templates" }).ToArray();
        public const string kTemplatePath = "Packages/io.z3y.github.shadergraph/Editor/ShaderGraph/Templates/ShaderPass.template";
        public const string kGrabPassTemplatePath = "Packages/io.z3y.github.shadergraph/Editor/ShaderGraph/Templates/GrabPass.template";

        public const string kDefaultGUI = "z3y.BuiltIn.ShaderGraph.BuiltInLitGUI";

        // SubTarget
        List<SubTarget> m_SubTargets;
        List<string> m_SubTargetNames;
        int activeSubTargetIndex => m_SubTargets.IndexOf(m_ActiveSubTarget);

        // View
        PopupField<string> m_SubTargetField;
        TextField m_CustomGUIField;

        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        // When checked, allows the material to control ALL surface settings (uber shader style)
        [SerializeField]
        bool m_AllowMaterialOverride = true;

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;

        [SerializeField]
        ZWriteControl m_ZWriteControl = ZWriteControl.Auto;

        [SerializeField]
        ZTestMode m_ZTestMode = ZTestMode.LEqual;

        [SerializeField]
        AlphaMode m_AlphaMode = AlphaMode.Alpha;

        [SerializeField]
        RenderFace m_RenderFace = RenderFace.Front;

        [SerializeField]
        bool m_AlphaClip = false;

        [SerializeField]
        string m_CustomEditorGUI;

        [SerializeField] public VRCFallbackTags vrcFallbackTags = new();
        [SerializeField] public bool generateOutlinePass = false;
        [SerializeField] public bool generateOutlineEarly = false;
        [SerializeField] public bool stencilEnabled = false;
        [SerializeField] public bool stencilOutlineEnabled = false;
        [SerializeField] public bool stencilFrontBack = false;
        [SerializeField] public bool alphaToMask = false;
        [SerializeField] public bool grabPass = false;

        [Serializable]
        public class FoldoutState
        {
            public FoldoutState(string name)
            {
                this.name = name;
            }
            public string name;
            public bool value;

            public void SetValue(bool value)
            {
                this.value = value;
            }
        }
        [SerializeField] public List<FoldoutState> foldoutStates = new();

        internal override bool ignoreCustomInterpolators => false;
        internal override int padCustomInterpolatorLimit => 4;

        public BuiltInTarget()
        {
            displayName = "Built-In VRC";
            m_SubTargets = TargetUtils.GetSubTargets(this);
            m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
        }

        public string renderType
        {
            get
            {
                if (surfaceType == SurfaceType.Transparent)
                    return $"{RenderType.Transparent}";
                else
                    return $"{RenderType.Opaque}";
            }
        }

        public string AppendAdditionalTags(string tags)
        {
            string vrctag = vrcFallbackTags.ToString();
            if (!string.IsNullOrEmpty(vrctag)) tags += "\n" + vrctag;

            return tags;
        }

        public string renderQueue
        {
            get
            {
                if (grabPass)
                {
                    return $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}+100";
                }
                if (surfaceType == SurfaceType.Transparent)
                    return $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}";
                else if (alphaClip)
                    return $"{UnityEditor.ShaderGraph.RenderQueue.AlphaTest}";
                else
                    return $"{UnityEditor.ShaderGraph.RenderQueue.Geometry}";
            }
        }

        public SubTarget activeSubTarget
        {
            get => m_ActiveSubTarget.value;
            set => m_ActiveSubTarget = value;
        }
        public bool allowMaterialOverride
        {
            get => m_AllowMaterialOverride;
            set => m_AllowMaterialOverride = value;
        }

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public ZWriteControl zWriteControl
        {
            get => m_ZWriteControl;
            set => m_ZWriteControl = value;
        }

        public ZTestMode zTestMode
        {
            get => m_ZTestMode;
            set => m_ZTestMode = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public RenderFace renderFace
        {
            get => m_RenderFace;
            set => m_RenderFace = value;
        }

        public bool alphaClip
        {
            get => m_AlphaClip;
            set => m_AlphaClip = value;
        }

        public string customEditorGUI
        {
            get => m_CustomEditorGUI;
            set => m_CustomEditorGUI = value;
        }

        public override bool IsActive()
        {
            bool isBuiltInRenderPipeline = GraphicsSettings.currentRenderPipeline == null;
            return isBuiltInRenderPipeline && activeSubTarget.IsActive();
        }

        public override bool IsNodeAllowedByTarget(Type nodeType)
        {
            return base.IsNodeAllowedByTarget(nodeType);
        }

        public override void Setup(ref TargetSetupContext context)
        {
            // Setup the Target
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            //BuiltInStructFields.ApplyCentroidVertexColor();

            // Setup the active SubTarget
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            if (m_ActiveSubTarget.value == null)
                return;
            m_ActiveSubTarget.value.target = this;
            m_ActiveSubTarget.value.Setup(ref context);

            // Override EditorGUI
            if (!string.IsNullOrEmpty(m_CustomEditorGUI))
            {
#if UNITY_2022_1_OR_NEWER
                context.AddCustomEditorForRenderPipeline(m_CustomEditorGUI, "");
#else
                context.customEditorForRenderPipelines.Add((m_CustomEditorGUI, ""));
#endif
            }

        }

        public override void OnAfterMultiDeserialize(string json)
        {
            TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            m_ActiveSubTarget.value.target = this;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            // Always force vertex as the shim between built-in cginc files and hlsl files requires this
            context.AddField(Fields.GraphVertex);
            context.AddField(Fields.GraphPixel);

            // SubTarget fields
            m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Core blocks
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);

            // SubTarget blocks
            m_ActiveSubTarget.value.GetActiveBlocks(ref context);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);
            activeSubTarget.CollectShaderProperties(collector, generationMode);
            // collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsArray);
            // collector.AddShaderProperty(LightmappingShaderProperties.kLightmapsIndirectionArray);
            // collector.AddShaderProperty(LightmappingShaderProperties.kShadowMasksArray);

            if (alphaToMask)
            {
                collector.AddFloatProperty(/*"[HideInInspector]*/"[Enum(Off, 0, On, 1)]_AlphaToMask", 0, "Alpha To Coverage");
            }

            if (stencilEnabled)
            {
                collector.AddFloatSliderProperty("[Header(Stencil)][IntRange]_StencilRef", 0, "Reference", new Vector2(0, 255));
                collector.AddFloatSliderProperty("[IntRange]_StencilReadMask", 255, "Read Mask", new Vector2(0, 255));
                collector.AddFloatSliderProperty("[IntRange]_StencilWriteMask", 255, "Write Mask", new Vector2(0, 255));
                if (stencilFrontBack)
                {
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_StencilCompBack", 8, "Compare Function Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilPassBack", 0, "Pass Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilFailBack", 0, "Fail Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilZFailBack", 0, "ZFail Back");

                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_StencilCompFront", 8, "Compare Function Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilPassFront", 0, "Pass Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilFailFront", 0, "Fail Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilZFailFront", 0, "ZFail Front");
                }
                else
                {
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp", 8, "Compare Function");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilPass", 0, "Pass");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilFail", 0, "Fail");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_StencilZFail", 0, "ZFail");
                }
            }
            if (stencilOutlineEnabled)
            {
                collector.AddFloatSliderProperty("[Header(Stencil Outline)][IntRange]_OutlineStencilRef", 0, "Reference", new Vector2(0, 255));
                collector.AddFloatSliderProperty("[IntRange]_OutlineStencilReadMask", 255, "Read Mask", new Vector2(0, 255));
                collector.AddFloatSliderProperty("[IntRange]_OutlineStencilWriteMask", 255, "Write Mask", new Vector2(0, 255));
                if (stencilFrontBack)
                {
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_OutlineStencilCompBack", 8, "Compare Function Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilPassBack", 0, "Pass Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilFailBack", 0, "Fail Back");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilZFailBack", 0, "ZFail Back");

                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_OutlineStencilCompFront", 8, "Compare Function Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilPassFront", 0, "Pass Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilFailFront", 0, "Fail Front");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilZFailFront", 0, "ZFail Front");
                }
                else
                {
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.CompareFunction)]_OutlineStencilComp", 8, "Compare Function");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilPass", 0, "Pass");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilFail", 0, "Fail");
                    collector.AddFloatProperty("[Enum(UnityEngine.Rendering.StencilOp)]_OutlineStencilZFail", 0, "ZFail");
                }
            }
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            m_ActiveSubTarget.value.ProcessPreviewMaterial(material);
        }

        public override object saveContext => m_ActiveSubTarget.value?.saveContext;

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            if (m_ActiveSubTarget.value == null)
                return;

            // Core properties
            m_SubTargetField = new PopupField<string>(m_SubTargetNames, activeSubTargetIndex);
            context.AddProperty("Material", m_SubTargetField, (evt) =>
            {
                if (Equals(activeSubTargetIndex, m_SubTargetField.index))
                    return;

                registerUndo("Change Material");
                m_ActiveSubTarget = m_SubTargets[m_SubTargetField.index];
                onChange();
            });


            // SubTarget properties
            m_ActiveSubTarget.value.GetPropertiesGUI(ref context, onChange, registerUndo);

            // Custom Editor GUI
            // Requires FocusOutEvent
            m_CustomGUIField = new TextField("") { value = customEditorGUI };
            m_CustomGUIField.RegisterCallback<FocusOutEvent>(s =>
            {
                if (Equals(customEditorGUI, m_CustomGUIField.value))
                    return;

                registerUndo("Change Custom Editor GUI");
                customEditorGUI = m_CustomGUIField.value;
                onChange();
            });

            // custom props
            /*AddAlphaToMaskToggleGUI(ref context, onChange, registerUndo);
            AddGrabpassToggleGUI(ref context, onChange, registerUndo);
            AddOutlineToggleGUI(ref context, onChange, registerUndo);
            if (generateOutlinePass)
            {
                context.globalIndentLevel++;
                AddOutlineEarlyToggleGUI(ref context, onChange, registerUndo);
                context.globalIndentLevel--;
            }

            AddStencilToggleGUI(ref context, onChange, registerUndo);
            if (stencilEnabled)
            {
                context.globalIndentLevel++;
                if (generateOutlinePass) AddStencilOutlineToggleGUI(ref context, onChange, registerUndo);
                AddStencilFrontBackToggleGUI(ref context, onChange, registerUndo);
                context.globalIndentLevel--;
            }

            AddVRCTagsToggleGUI(ref context, onChange, registerUndo);*/
            //var properties = new GraphInspectorGUI(context, onChange, registerUndo);
            //properties.DrawDefaultProperties(this);




            context.AddProperty("Custom Editor GUI", m_CustomGUIField, (evt) => { });
        }

        public void AddDefaultMaterialOverrideGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // At some point we may want to convert this to be a per-property control
            // or Unify the UX with the upcoming "lock" feature of the Material Variant properties
            context.AddProperty("Allow Material Override", new Toggle() { value = allowMaterialOverride }, (evt) =>
            {
                if (Equals(allowMaterialOverride, evt.newValue))
                    return;

                registerUndo("Change Allow Material Override");
                allowMaterialOverride = evt.newValue;
                onChange();
            });
        }

        // public void GetDefaultSurfacePropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        // {
        //     context.AddProperty("Surface Type", new EnumField(SurfaceType.Opaque) { value = surfaceType }, (evt) =>
        //     {
        //         if (Equals(surfaceType, evt.newValue))
        //             return;

        //         registerUndo("Change Surface");
        //         surfaceType = (SurfaceType)evt.newValue;
        //         onChange();
        //     });

        //     context.AddProperty("Blending Mode", new EnumField(AlphaMode.Alpha) { value = alphaMode }, surfaceType == SurfaceType.Transparent, (evt) =>
        //     {
        //         if (Equals(alphaMode, evt.newValue))
        //             return;

        //         registerUndo("Change Blend");
        //         alphaMode = (AlphaMode)evt.newValue;
        //         onChange();
        //     });

        //     context.AddProperty("Render Face", new EnumField(RenderFace.Front) { value = renderFace }, (evt) =>
        //     {
        //         if (Equals(renderFace, evt.newValue))
        //             return;

        //         registerUndo("Change Render Face");
        //         renderFace = (RenderFace)evt.newValue;
        //         onChange();
        //     });

        //     context.AddProperty("Depth Write", new EnumField(ZWriteControl.Auto) { value = zWriteControl }, (evt) =>
        //     {
        //         if (Equals(zWriteControl, evt.newValue))
        //             return;

        //         registerUndo("Change Depth Write Control");
        //         zWriteControl = (ZWriteControl)evt.newValue;
        //         onChange();
        //     });

        //     context.AddProperty("Depth Test", new EnumField(ZTestMode.LEqual) { value = zTestMode }, (evt) =>
        //     {
        //         if (Equals(zTestMode, evt.newValue))
        //             return;

        //         registerUndo("Change Depth Test");
        //         zTestMode = (ZTestMode)evt.newValue;
        //         onChange();
        //     });

        //     context.AddProperty("Alpha Clipping", new Toggle() { value = alphaClip }, (evt) =>
        //     {
        //         if (Equals(alphaClip, evt.newValue))
        //             return;

        //         registerUndo("Change Alpha Clip");
        //         alphaClip = evt.newValue;
        //         onChange();
        //     });
        // }

        public bool TrySetActiveSubTarget(Type subTargetType)
        {
            if (!subTargetType.IsSubclassOf(typeof(SubTarget)))
                return false;

            foreach (var subTarget in m_SubTargets)
            {
                if (subTarget.GetType().Equals(subTargetType))
                {
                    m_ActiveSubTarget = subTarget;
                    return true;
                }
            }

            return false;
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return scriptableRenderPipeline == null;
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);

            if (this.sgVersion < latestVersion)
            {
                // Version 0 didn't have m_AllowMaterialOverride but acted as if it was true
                if (this.sgVersion <= 1)
                {
                    this.m_AllowMaterialOverride = true;
                }
                ChangeVersion(latestVersion);
            }
        }

        #region Metadata
        string IHasMetadata.identifier
        {
            get
            {
                // defer to subtarget
                if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                    return subTargetHasMetaData.identifier;
                return null;
            }
        }

        ScriptableObject IHasMetadata.GetMetadataObject(GraphDataReadOnly graph)
        {
            // defer to subtarget
            if (m_ActiveSubTarget.value is IHasMetadata subTargetHasMetaData)
                return subTargetHasMetaData.GetMetadataObject(graph);
            return null;
        }

        #endregion
    }

    #region Passes
    static class CorePasses
    {
        internal static void AddSurfaceTypeControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (pass.keywords == null) return;
            if (target.allowMaterialOverride)
            {
                pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
            }
            else if (target.surfaceType == SurfaceType.Transparent)
            {
                pass.defines.Add(CoreKeywordDescriptors.SurfaceTypeTransparent, 1);
            }
        }

        internal static void AddAlphaPremultiplyControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (pass.keywords == null) return;
            if (target.allowMaterialOverride)
            {
                pass.keywords.Add(CoreKeywordDescriptors.AlphaPremultiplyOn);
            }
            else if (target.alphaMode == AlphaMode.Premultiply)
            {
                pass.defines.Add(CoreKeywordDescriptors.AlphaPremultiplyOn, 1);
            }
        }

        internal static void AddAlphaClipControlToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            if (pass.keywords == null) return;
            if (target.allowMaterialOverride)
            {
                //pass.keywords.Add(CoreKeywordDescriptors.AlphaClip);
                pass.keywords.Add(CoreKeywordDescriptors.AlphaTestOn);
            }
            else if (target.alphaClip)
            {
                //pass.defines.Add(CoreKeywordDescriptors.AlphaClip, 1);
                pass.defines.Add(CoreKeywordDescriptors.AlphaTestOn, 1);
            }
        }

        internal static void AddTargetSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            AddSurfaceTypeControlToPass(ref pass, target);
            AddAlphaPremultiplyControlToPass(ref pass, target);
            AddAlphaClipControlToPass(ref pass, target);
        }

        internal static void AddCommonPassSurfaceControlsToPass(ref PassDescriptor pass, BuiltInTarget target)
        {
            AddSurfaceTypeControlToPass(ref pass, target);
            AddAlphaClipControlToPass(ref pass, target);
        }

        public static PassDescriptor ShadowCaster(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWCASTER",
                lightMode = "ShadowCaster",

                // Template
                passTemplatePath = BuiltInTarget.kTemplatePath,
                sharedTemplateDirectories = BuiltInTarget.kSharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = CoreBlockMasks.FragmentAlphaOnly,

                // Fields
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.ShadowCaster,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = CoreRenderStates.ShadowCaster(target),
                pragmas = CorePragmas.ShadowCaster,
                defines = new DefineCollection() { CoreDefines.BuiltInTargetAPI },
                includes = CoreIncludes.ShadowCaster,

                // Custom Interpolator Support
                customInterpolators = CoreCustomInterpDescriptors.Common
            };

            AddCommonPassSurfaceControlsToPass(ref result, target);
            result.keywords = new KeywordCollection
            {
                CoreKeywordDescriptors.AlphaTestOn,
                CoreKeywordDescriptors.AlphaPremultiplyOn,
                CoreKeywordDescriptors.SurfaceTypeTransparent,
            };

            return result;
        }
        
        public static PassDescriptor OutlineDescriptor(PassDescriptor basePass, BuiltInTarget target)
        {
            /*var vertexBlocks = basePass.validVertexBlocks.ToList();
            vertexBlocks.Remove(BlockFields.VertexDescription.Position);
            vertexBlocks.Add(CoreBlockMasks.VertexDescriptionOutline.Position);
            
            var pixelBlocks = basePass.validPixelBlocks.ToList();
            pixelBlocks.Remove(BlockFields.SurfaceDescription.BaseColor);
            pixelBlocks.Add(CoreBlockMasks.SurfaceDescriptionOutline.BaseColor);*/

            var renderStates = CoreRenderStates.CopyAndModifyCull(basePass.renderStates);
            if (target.stencilEnabled)
            {
                renderStates = CoreRenderStates.CopyAndAppendStencil(target, renderStates, target.stencilOutlineEnabled ? "Outline" : "");
            }

            var result = new PassDescriptor()
            {
                // Definition
                displayName = basePass.displayName,
                referenceName = "SHADERPASS_OUTLINE",
                lightMode = basePass.lightMode,

                // Template
                passTemplatePath = basePass.passTemplatePath,
                sharedTemplateDirectories = basePass.sharedTemplateDirectories,

                // Port Mask
                validVertexBlocks = basePass.validVertexBlocks,
                validPixelBlocks = basePass.validPixelBlocks,

                // Fields
                structs = basePass.structs,
                requiredFields = basePass.requiredFields,
                fieldDependencies = basePass.fieldDependencies,

                // Conditional State
                renderStates = renderStates,
                pragmas = basePass.pragmas,
                defines = basePass.defines,
                includes = basePass.includes,

                // Custom Interpolator Support
                customInterpolators = basePass.customInterpolators
            };

            AddCommonPassSurfaceControlsToPass(ref result, target);
            if (target.activeSubTarget is BuiltInLitSubTarget lit)
            {
                BuiltInLitSubTarget.LitKeywords.DeclareAndAppend(ref basePass, "FLATLIT", true, lit.flatLit);
            }

            return result;
        }
        
        public static PassDescriptor GrabPassDescriptor(BuiltInTarget target)
        {
            var result = new PassDescriptor()
            {
                // Definition
                displayName = "GrabPass",
                referenceName = "",
                lightMode = "",

                // Template
                passTemplatePath = BuiltInTarget.kGrabPassTemplatePath,
                //sharedTemplateDirectories = new string[0];

                // Port Mask
                //validVertexBlocks = new BlockFieldDescriptor[0],
                //validPixelBlocks = new BlockFieldDescriptor[0],

                // Fields
                structs = new StructCollection(),
                requiredFields = new FieldCollection(),
                fieldDependencies = new DependencyCollection(),

                // Conditional State
                renderStates = new RenderStateCollection(),
                pragmas = new PragmaCollection(),
                defines = new DefineCollection(),
                includes = new IncludeCollection(),

                // Custom Interpolator Support
                // customInterpolators = CoreCustomInterpDescriptors.Common
            };

            //AddCommonPassSurfaceControlsToPass(ref result, target);

            return result;
        }
    }
    #endregion

    #region PortMasks
    class CoreBlockMasks
    {
        /*[GenerateBlocks]
        public struct VertexDescriptionOutline
        {
            public static string name = "VertexDescription";
            public static BlockFieldDescriptor Position = new BlockFieldDescriptor(name, "PositionOutline", "Outline Position","VERTEXDESCRIPTION_POSITION_OUTLINE",
                new PositionControl(CoordinateSpace.Object), ShaderStage.Vertex);
        }

        [GenerateBlocks]
        public struct SurfaceDescriptionOutline
        {
            public static string name = "SurfaceDescription";

            public static BlockFieldDescriptor BaseColor = new BlockFieldDescriptor(name,
                "BaseColorOutline", "Base Color Outline", "SURFACEDESCRIPTION_BASECOLOR_OUTLINE",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
        }*/

        public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
        {
            BlockFields.VertexDescription.Position,
            BlockFields.VertexDescription.Normal,
            BlockFields.VertexDescription.Tangent,
        };

        public static readonly BlockFieldDescriptor[] FragmentAlphaOnly = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };

        public static readonly BlockFieldDescriptor[] FragmentColorAlpha = new BlockFieldDescriptor[]
        {
            BlockFields.SurfaceDescription.BaseColor,
            BlockFields.SurfaceDescription.Alpha,
            BlockFields.SurfaceDescription.AlphaClipThreshold,
        };
    }
    #endregion

    #region StructCollections
    static class CoreStructCollections
    {
        public static readonly StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { BuiltInStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
    #endregion

    #region RequiredFields
    static class CoreRequiredFields
    {
        public static readonly FieldCollection ShadowCaster = new FieldCollection()
        {
            StructFields.Attributes.normalOS,
        };
    }
    #endregion

    #region FieldDependencies
    static class CoreFieldDependencies
    {
        public static readonly DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID),
            new FieldDependency(BuiltInStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID),
        };
    }
    #endregion

    #region RenderStates
    static class CoreRenderStates
    {
        public static class Uniforms
        {
            public static readonly string srcBlend = "[" + Property.SG_SrcBlend + "]";
            public static readonly string dstBlend = "[" + Property.SG_DstBlend + "]";
            public static readonly string cullMode = "[" + Property.SG_Cull + "]";
            public static readonly string zWrite = "[" + Property.SG_ZWrite + "]";
            public static readonly string zTest = "[" + Property.SG_ZTest + "]";
        }

        public static Cull RenderFaceToCull(RenderFace renderFace)
        {
            switch (renderFace)
            {
                case RenderFace.Back:
                    return Cull.Front;
                case RenderFace.Front:
                    return Cull.Back;
                case RenderFace.Both:
                    return Cull.Off;
            }
            return Cull.Back;
        }

        public static ZWrite ZWriteControlToZWrite(ZWriteControl zWriteControl, SurfaceType surfaceType)
        {
            if (zWriteControl == ZWriteControl.ForceEnabled)
                return ZWrite.On;
            else if (zWriteControl == ZWriteControl.ForceDisabled)
                return ZWrite.Off;
            else // ZWriteControl.Auto
            {
                if (surfaceType == SurfaceType.Opaque)
                    return ZWrite.On;
                else
                    return ZWrite.Off;
            }
        }

        public static void AddUberSwitchedZTest(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZTest(Uniforms.zTest));
            else
                renderStates.Add(RenderState.ZTest(target.zTestMode.ToString()));
        }

        public static void AddUberSwitchedZWrite(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.ZWrite(Uniforms.zWrite));
            else
                renderStates.Add(RenderState.ZWrite(ZWriteControlToZWrite(target.zWriteControl, target.surfaceType)));
        }

        public static void AddUberSwitchedCull(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
                renderStates.Add(RenderState.Cull(Uniforms.cullMode));
            else
                renderStates.Add(RenderState.Cull(RenderFaceToCull(target.renderFace)));
        }

        public static void AddUberSwitchedBlend(BuiltInTarget target, RenderStateCollection renderStates)
        {
            if (target.allowMaterialOverride)
            {
                renderStates.Add(RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend));
            }
            else
            {
                if (target.surfaceType == SurfaceType.Opaque)
                {
                    renderStates.Add(RenderState.Blend(Blend.One, Blend.Zero));
                }
                else
                {
                    if (target.alphaMode == AlphaMode.Alpha)
                        renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    else if (target.alphaMode == AlphaMode.Premultiply)
                        renderStates.Add(RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha));
                    else if (target.alphaMode == AlphaMode.Additive)
                        renderStates.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
                    else if (target.alphaMode == AlphaMode.Multiply)
                        renderStates.Add(RenderState.Blend(Blend.DstColor, Blend.Zero));
                }
            }
        }

        public static readonly RenderStateCollection MaterialControlledDefault = new RenderStateCollection
        {
            { RenderState.ZTest(Uniforms.zTest) },
            { RenderState.ZWrite(Uniforms.zWrite) },
            { RenderState.Cull(Uniforms.cullMode) },
            { RenderState.Blend(Uniforms.srcBlend, Uniforms.dstBlend) },
        };

        public static RenderStateCollection Default(BuiltInTarget target)
        {
            var result = new RenderStateCollection();
            if (target.allowMaterialOverride)
                return result.Add(MaterialControlledDefault);
            else
            {
                AddUberSwitchedZTest(target, result);
                AddUberSwitchedZWrite(target, result);
                AddUberSwitchedCull(target, result);
                AddUberSwitchedBlend(target, result);
                if (target.surfaceType != SurfaceType.Opaque)
                    result.Add(RenderState.ColorMask("ColorMask RGB"));
                return result;
            }
        }

        public static RenderStateCollection ForwardAdd(BuiltInTarget target)
        {
            var result = new RenderStateCollection();

            result.Add(RenderState.ZWrite(ZWrite.Off));
            if (target.surfaceType != SurfaceType.Opaque)
            {
                result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One));
                result.Add(RenderState.ColorMask("ColorMask RGB"));
            }
            else
            {
                result.Add(RenderState.Blend(Blend.SrcAlpha, Blend.One, Blend.One, Blend.One));
            }
            return result;
        }

        public static readonly RenderStateCollection Meta = new RenderStateCollection
        {
            { RenderState.Cull(Cull.Off) },
        };

        public static RenderStateCollection ShadowCaster(BuiltInTarget target)
        {
            var result = new RenderStateCollection();
            result.Add(RenderState.ZTest(ZTest.LEqual));
            result.Add(RenderState.ZWrite(ZWrite.On));
            AddUberSwitchedCull(target, result);
            AddUberSwitchedBlend(target, result);
            result.Add(RenderState.ColorMask("ColorMask 0"));
            if (target.stencilEnabled)
            {
                return CopyAndAppendStencil(target, result);
            }
            return result;
        }

        public static RenderStateCollection CopyAndAppendStencil(BuiltInTarget target, RenderStateCollection renderStateCollection, string prefix = "")
        {
            var rsc = new RenderStateCollection();

            foreach (var item in renderStateCollection)
            {
                if (item.descriptor.type == RenderStateType.Stencil)
                {
                    continue;
                }

                rsc.Add(item.descriptor, item.fieldConditions);
            }

            var descriptor = new StencilDescriptor();
            descriptor.Ref =
                $"[_{prefix}StencilRef]\n" +
                $"ReadMask [_{prefix}StencilReadMask]\n" +
                $"WriteMask [_{prefix}StencilWriteMask]\n";

            if (target.stencilFrontBack)
            {
                descriptor.Ref +=
                    $"\nCompBack[_{prefix}StencilCompBack]" +
                    $"\nPassBack[_{prefix}StencilPassBack]" +
                    $"\nFailBack[_{prefix}StencilFailBack]" +
                    $"\nZFailBack[_{prefix}StencilZFailBack]" +

                    $"\nCompFront[_{prefix}StencilCompFront]" +
                    $"\nPassFront[_{prefix}StencilPassFront]" +
                    $"\nFailFront[_{prefix}StencilFailFront]" +
                    $"\nZFailFront[_{prefix}StencilZFailFront]";
            }
            else
            {
                descriptor.Ref +=
                    $"\nComp[_{prefix}StencilComp]" +
                    $"\nPass[_{prefix}StencilPass]" +
                    $"\nFail[_{prefix}StencilFail]" +
                    $"\nZFail[_{prefix}StencilZFail]";
            }

            rsc.Add(RenderState.Stencil(descriptor));

            return rsc;
        }

        public static void AppendAlphaToMask(BuiltInTarget target, RenderStateCollection renderStateCollection)
        {
            renderStateCollection.Add(RenderState.AlphaToMask("[_AlphaToMask]"));
        }

       public static RenderStateCollection CopyAndModifyCull(RenderStateCollection renderStateCollection, Cull cullMode = Cull.Front)
       {
            var rsc = new RenderStateCollection();

            foreach (var item in renderStateCollection)
            {
                if (item.descriptor.type == RenderStateType.Cull)
                {
                    continue;
                }

                rsc.Add(item.descriptor, item.fieldConditions);
            }

            rsc.Add(RenderState.Cull(cullMode));

            return rsc;
        }
    }
    #endregion

    #region Pragmas

    static class CorePragmas
    {
        public static PragmaDescriptor skipEmission => new PragmaDescriptor { value = "skip_variants _EMISSION" };
        
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.MultiCompileForwardBase },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
        
        public static readonly PragmaCollection ForwardUnlit = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection ForwardAdd = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.MultiCompileForwardAddFullShadowsBase },
            { skipEmission },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection ShadowCaster = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.MultiCompileShadowCaster },
            { Pragma.MultiCompileInstancing },
            { skipEmission },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
    #endregion

    #region Includes
    static class CoreIncludes
    {
        //const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        //const string kTexture = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl";
        //const string kCore = "Packages/io.z3y.github.shadergraph/ShaderLibrary/Core.hlsl";
        //const string kLighting = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/io.z3y.github.shadergraph/ShaderLibrary/ShaderGraphFunctions.hlsl";
        //const string kVaryings = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl";
        //const string kShaderPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        //const string kDepthOnlyPass = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        
        const string kVertex = "Packages/io.z3y.github.shadergraph/ShaderLibrary/Vertex.hlsl";
        //const string kFragmentForward = "Packages/io.z3y.github.shadergraph/ShaderLibrary/Fragment.hlsl";
        const string kFragmentShadowCaster = "Packages/io.z3y.github.shadergraph/ShaderLibrary/FragmentShadowCaster.hlsl";
        //const string kFragmentMeta = "Packages/io.z3y.github.shadergraph/ShaderLibrary/FragmentMeta.hlsl";

        //const string kShims = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl";
        //const string kLegacySurfaceVertex = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl";
        const string kMetaPassInclude = "UnityMetaPass.cginc";

        private const string kShaderPass = "Packages/io.z3y.github.shadergraph/ShaderLibrary/ShaderPass.hlsl";

        public static readonly IncludeCollection CorePregraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Pregraph },
            //{ kColor, IncludeLocation.Pregraph },
            //{ kCore, IncludeLocation.Pregraph },
            //{ kTexture, IncludeLocation.Pregraph },
            //{ kLighting, IncludeLocation.Pregraph },
            //{ kLegacySurfaceVertex, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            { kGraphFunctions, IncludeLocation.Pregraph },
        };

        public static readonly IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Postgraph },
            { kVertex, IncludeLocation.Postgraph },
            //{ kVaryings, IncludeLocation.Postgraph },
        };
        
        public static readonly IncludeCollection ShadowCaster = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kFragmentShadowCaster, IncludeLocation.Postgraph },
        };
    }
    #endregion

    #region Defines
    static class CoreDefines
    {
        public static readonly DefineCollection BuiltInTargetAPI = new DefineCollection
        {
        };
    }
    #endregion

    #region KeywordDescriptors

    static class CoreKeywordDescriptors
    {
        public static readonly KeywordDescriptor AlphaTestOn = new KeywordDescriptor()
        {
            displayName = Keyword.SG_AlphaTestOn,
            referenceName = Keyword.SG_AlphaTestOn,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        /*
        public static readonly KeywordDescriptor AlphaClip = new KeywordDescriptor()
        {
            displayName = "Alpha Clipping",
            referenceName = Keyword.SG_AlphaClip,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };
        */

        public static readonly KeywordDescriptor SurfaceTypeTransparent = new KeywordDescriptor()
        {
            displayName = Keyword.SG_SurfaceTypeTransparent,
            referenceName = Keyword.SG_SurfaceTypeTransparent,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };

        public static readonly KeywordDescriptor AlphaPremultiplyOn = new KeywordDescriptor()
        {
            displayName = Keyword.SG_AlphaPremultiplyOn,
            referenceName = Keyword.SG_AlphaPremultiplyOn,
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            stages = KeywordShaderStage.Fragment,
        };
    }
    #endregion

    #region Keywords
    static class CoreKeywords
    {

    }
    #endregion

    #region FieldDescriptors
    static class CoreFields
    {
        public static readonly FieldDescriptor UseLegacySpriteBlocks = new FieldDescriptor("BuiltIn", "UseLegacySpriteBlocks", "BUILTIN_USELEGACYSPRITEBLOCKS");
    }
    #endregion

    #region CustomInterpolators
    static class CoreCustomInterpDescriptors
    {
        public static readonly CustomInterpSubGen.Collection Common = new CustomInterpSubGen.Collection
        {
            // Custom interpolators are not explicitly defined in the SurfaceDescriptionInputs template.
            // This entry point will let us generate a block of pass-through assignments for each field.
            CustomInterpSubGen.Descriptor.MakeBlock(CustomInterpSubGen.Splice.k_spliceCopyToSDI, "output", "input"),

            // sgci_PassThroughFunc is called from BuildVaryings in Varyings.hlsl to copy custom interpolators from vertex descriptions.
            // this entry point allows for the function to be defined before it is used.
            CustomInterpSubGen.Descriptor.MakeFunc(CustomInterpSubGen.Splice.k_splicePreSurface, "CustomInterpolatorPassThroughFunc", "Varyings", "VertexDescription", "CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC", "FEATURES_GRAPH_VERTEX")
        };
    }
    #endregion
}
