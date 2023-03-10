using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using z3y.ShaderGraphExtended;

namespace UnityEditor.ShaderGraph
{
    abstract class MasterNode : AbstractMaterialNode, IMasterNode, IHasSettings
    {
        public override bool hasPreview
        {
            get { return false; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        [SerializeField]
        bool m_DOTSInstancing = false;

        [Serializable]
        public enum CullingOverrideMode
        {
            None,
            Back,
            Front,
            Off
        }
        
        [SerializeField] public Shader additionalPass;
        [SerializeField] public bool alphaToCoverage = true;


        [SerializeField]
        public CullingOverrideMode cullingOverride = CullingOverrideMode.None;

        public ToggleData dotsInstancing
        {
            get { return new ToggleData(m_DOTSInstancing); }
            set
            {
                if (m_DOTSInstancing == value.isOn)
                    return;

                m_DOTSInstancing = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        public abstract string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null);
        public abstract bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
        public abstract int GetPreviewPassIndex();

        public VisualElement CreateSettingsElement()
        {
            var container = new VisualElement();
            var commonSettingsElement = CreateCommonSettingsElement();
            if (commonSettingsElement != null)
                container.Add(commonSettingsElement);

            return container;
        }

        protected virtual VisualElement CreateCommonSettingsElement()
        {
            return null;
        }

        public static Texture2D dfgLut => Resources.Load<Texture2D>("dfg-multiscatter");
        public virtual object saveContext => null;

        public virtual void ProcessPreviewMaterial(Material Material) {}
    }

    [Serializable]
    abstract class MasterNode<T> : MasterNode
        where T : class, ISubShader
    {
        [NonSerialized]
        List<T> m_SubShaders = new List<T>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSubShaders = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<T> subShaders => m_SubShaders;

        public void AddSubShader(T subshader)
        {
            if (m_SubShaders.Contains(subshader))
                return;

            m_SubShaders.Add(subshader);
            Dirty(ModificationScope.Graph);
        }

        public void RemoveSubShader(T subshader)
        {
            m_SubShaders.RemoveAll(x => x == subshader);
            Dirty(ModificationScope.Graph);
        }

        public ISubShader GetActiveSubShader()
        {
            foreach (var subShader in m_SubShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return subShader;
            }
            return null;
        }

        private void AppendDefaultShaderProperties(ref PropertyCollector propertyCollector)
        {

            var modeProperty = new Vector1ShaderProperty
            {
                displayName = "Rendering Mode",
                overrideReferenceName = "_Mode",
                attributes = "[Enum(Opaque, 0, Cutout, 1, Fade, 2, Transparent, 3, Additive, 4, Multiply, 5)]",
                value = (int)renderModeOverride
            };
            
            
            var scrBlendProperty = new Vector1ShaderProperty
            {
                displayName = "Source Blend",
                overrideReferenceName = "_SrcBlend",
                attributes = "[Enum(UnityEngine.Rendering.BlendMode)]",
                value = 1
            };
            
            var dstBlendProperty = new Vector1ShaderProperty
            {
                displayName = "Destination Blend",
                overrideReferenceName = "_DstBlend",
                attributes = "[Enum(UnityEngine.Rendering.BlendMode)]",
                value = 0
            };
            
            var zWriteProperty = new Vector1ShaderProperty
            {
                displayName = "ZWrite",
                overrideReferenceName = "_ZWrite",
                attributes = "[Enum(Off, 0, On, 1)]",
                value = 1
            };
            
            var alphaToMaskProperty = new Vector1ShaderProperty
            {
                displayName = "AlphaToMask",
                overrideReferenceName = "_AlphaToMask",
                attributes = "[Enum(Off, 0, On, 1)]",
                value = 0
            };
            
            var cullProperty = new Vector1ShaderProperty
            {
                displayName = "Cull",
                overrideReferenceName = "_Cull",
                attributes = "[Enum(UnityEngine.Rendering.CullMode)]",
                value = 2
            };

            var dfgTexture = new SerializableTexture
            {
                texture = MasterNode.dfgLut
            };

            var dfgLut = new Texture2DShaderProperty()
            {
                overrideReferenceName = "_DFG",
                modifiable = false,
                value = dfgTexture,
                hidden = true
            };

            
    

            

            propertyCollector.AddShaderProperty(modeProperty);
            propertyCollector.AddShaderProperty(scrBlendProperty);
            propertyCollector.AddShaderProperty(dstBlendProperty);
            propertyCollector.AddShaderProperty(zWriteProperty);
            propertyCollector.AddShaderProperty(alphaToMaskProperty);
            propertyCollector.AddShaderProperty(dfgLut);
            
            if (this is PBRMasterNode)
            {
                var reflectionsToggle = new Vector1ShaderProperty
                {
                    displayName = "Reflections",
                    overrideReferenceName = "_GlossyReflections",
                    attributes = "[ToggleOff(_GLOSSYREFLECTIONS_OFF)]",
                    value = 1
                };
                var specularHighlightsToggle = new Vector1ShaderProperty
                {
                    displayName = "Specular Highlights",
                    overrideReferenceName = "_SpecularHighlights",
                    attributes = "[ToggleOff(_SPECULARHIGHLIGHTS_OFF)]",
                    value = 1
                };
                
                var bakeryMonoSHProp = new Vector1ShaderProperty
                {
                    displayName = "Mono SH",
                    overrideReferenceName = "_BakeryMonoSH",
                    attributes = "[Toggle(BAKERY_MONOSH)]",
                    value = 0
                };
                var lightmappedSpecularProp = new Vector1ShaderProperty
                {
                    displayName = "Lightmapped Specular",
                    overrideReferenceName = "_LightmappedSpecular",
                    attributes = "[Toggle(_LIGHTMAPPED_SPECULAR)]",
                    value = 0
                };
                
                var nonLinearLightprobeSH = new Vector1ShaderProperty
                {
                    displayName = "Non-linear Light Probe SH",
                    overrideReferenceName = "_NonLinearLightProbeSH",
                    attributes = "[Toggle(NONLINEAR_LIGHTPROBESH)]",
                    value = 0
                };
                
                var ltcgi = new Vector1ShaderProperty
                {
                    displayName = "LTCGI",
                    overrideReferenceName = "_LTCGI",
                    attributes = "[Toggle(LTCGI)]",
                    value = 0
                };
                var ltcgiSpec = new Vector1ShaderProperty
                {
                    displayName = "LTCGI Disable Diffuse",
                    overrideReferenceName = "_LTCGI_DIFFUSE_OFF",
                    attributes = "[Toggle(LTCGI_DIFFUSE_OFF)]",
                    value = 0
                };
                
                var bakeryAlphaMetaEnable = new Vector1ShaderProperty
                {
                    displayName = "Enable Bakery alpha meta pass",
                    overrideReferenceName = "BAKERY_META_ALPHA_ENABLE",
                    hidden = true,
                    value = 1
                };

                propertyCollector.AddShaderProperty(bakeryAlphaMetaEnable);
                propertyCollector.AddShaderProperty(bakeryMonoSHProp);
                propertyCollector.AddShaderProperty(lightmappedSpecularProp);
                propertyCollector.AddShaderProperty(nonLinearLightprobeSH);

                if (ShaderGraphExtendedUtils.LTCGIExists)
                {
                    propertyCollector.AddShaderProperty(ltcgi);
                    propertyCollector.AddShaderProperty(ltcgiSpec);
                }
                
                propertyCollector.AddShaderProperty(reflectionsToggle);
                propertyCollector.AddShaderProperty(specularHighlightsToggle);

            }

            
            // cull last marks the end of rendering properties
            propertyCollector.AddShaderProperty(cullProperty);
        }

        public sealed override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            var activeNodeList = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();
            var shaderKeywords = new KeywordCollector();
            if (owner != null)
            {
                owner.CollectShaderProperties(shaderProperties, mode);
                owner.CollectShaderKeywords(shaderKeywords, mode);
            }

            if(owner.GetKeywordPermutationCount() > ShaderGraphPreferences.variantLimit)
            {
                owner.AddValidationError(tempId, ShaderKeyword.kVariantLimitWarning, Rendering.ShaderCompilerMessageSeverity.Error);

                configuredTextures = shaderProperties.GetConfiguredTexutres();
                return ShaderGraphImporter.k_ErrorShader;
            }

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            AppendDefaultShaderProperties(ref shaderProperties);

            var finalShader = new ShaderStringBuilder();
            finalShader.AppendLine(@"Shader ""{0}""", outputName);
            using (finalShader.BlockScope())
            {
                SubShaderGenerator.GeneratePropertiesBlock(finalShader, shaderProperties, shaderKeywords, mode);

                foreach (var subShader in m_SubShaders)
                {
                    if (mode != GenerationMode.Preview || subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                        finalShader.AppendLines(subShader.GetSubshader(this, mode, sourceAssetDependencyPaths));
                }

                // Either grab the pipeline default for the active master node or the user override
                ICanChangeShaderGUI canChangeShaderGui = this as ICanChangeShaderGUI;
                if (canChangeShaderGui != null && canChangeShaderGui.OverrideEnabled)
                {
                    string customEditor = GenerationUtils.FinalCustomEditorString(canChangeShaderGui);

                    if (customEditor != null)
                    {
                        finalShader.AppendLine("CustomEditor \"" + customEditor + "\"");
                    }
                }

                //finalShader.AppendLine(@"FallBack ""Hidden/Shader Graph/FallbackError""");
            }
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.ToString();
        }

        public sealed override bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            foreach (var subShader in m_SubShaders)
            {
                if (subShader.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                    return true;
            }
            return false;
        }

        public sealed override int GetPreviewPassIndex()
        {
            return GetActiveSubShader()?.GetPreviewPassIndex() ?? 0;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableSubShaders = SerializationHelper.Serialize<T>(m_SubShaders);
        }

        public override void OnAfterDeserialize()
        {
            m_SubShaders = SerializationHelper.Deserialize<T>(m_SerializableSubShaders, GraphUtil.GetLegacyTypeRemapping());
            m_SubShaders.RemoveAll(x => x == null);
            m_SerializableSubShaders = null;
            base.OnAfterDeserialize();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypesOrNothing())
                {
                    var isValid = !type.IsAbstract && !type.IsGenericType && type.IsClass && typeof(T).IsAssignableFrom(type);
                    if (isValid && !subShaders.Any(s => s.GetType() == type))
                    {
                        try
                        {
                            var subShader = (T)Activator.CreateInstance(type);
                            AddSubShader(subShader);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}
