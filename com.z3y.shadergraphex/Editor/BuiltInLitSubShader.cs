using System;
using System.Collections.Generic;
using System.Linq;
using Data.Util;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = System.Diagnostics.Debug;

namespace z3y.ShaderGraphExtended
{
    [Serializable]
    class BuiltInLitSubShaderExtended : IPBRSubShader
    {
#region Passes
        ShaderPass m_ForwardBasePass = new ShaderPass
        {
            // Definition
            displayName = "FORWARDBASE",
            referenceName = "SHADERPASS_FORWARDBASE",
            lightMode = "ForwardBase",
            passInclude = "Packages/com.z3y.shadergraphex/hlsl/LitPass.hlsl",
            varyingsInclude = "Packages/com.z3y.shadergraphex/hlsl/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
                PBRMasterNode.ReflectanceSlotID,
                PBRMasterNode.AnisotropyTangentSlotID,
                PBRMasterNode.AnisotropyLevelSlotID,
                PBRMasterNode.GSAAThresholdSlotID,
                PBRMasterNode.GSAAVarienceSlotID
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "AutoLight.cginc",
                "Lighting.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/ShaderGraph.hlsl",
            },
            pragmas = new List<string>()
            {
                "target 4.5",
                "multi_compile_fog",
                "multi_compile_instancing",
                "multi_compile_fwdbase",
                "skip_variants LIGHTPROBE_SH" // doesnt seem to be needed at all
            },
            keywords = new KeywordDescriptor[]
            {
                defaultModeKeywords,
                bakeryMonoSHKeyword,
                lightmappedSpecularKeyword,
                reflectionsKeyword,
                specularHighlightsKeyword,
                nonLinearLightProbeSHKeyword
            },
            
            requiredVaryings = new List<string>()
            {
                "Varyings.positionCS",
                "Varyings.positionWS",
                "Varyings.normalWS",
                "Varyings.tangentWS",
                "Varyings.texCoord1",
                "Varyings.texCoord2",
                "Varyings.shadowCoord"
            },
            
            requiredAttributes = new List<string>()
            {
                "Attributes.positionOS",
                "Attributes.normalOS",
                "Attributes.tangentOS",
                "Attributes.uv1",
            },

        };
        
        ShaderPass m_ForwardAddPass = new ShaderPass
        {
            // Definition
            displayName = "FORWARDADD",
            referenceName = "SHADERPASS_FORWARDADD",
            lightMode = "ForwardAdd",
            passInclude = "Packages/com.z3y.shadergraphex/hlsl/LitPass.hlsl",
            varyingsInclude = "Packages/com.z3y.shadergraphex/hlsl/Varyings.hlsl",
            useInPreview = false,

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
                PBRMasterNode.ReflectanceSlotID,
                PBRMasterNode.AnisotropyTangentSlotID,
                PBRMasterNode.AnisotropyLevelSlotID,
                PBRMasterNode.GSAAThresholdSlotID,
                PBRMasterNode.GSAAVarienceSlotID
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "AutoLight.cginc",
                "Lighting.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/ShaderGraph.hlsl",
            },
            pragmas = new List<string>()
            {
                "target 4.5",
                "multi_compile_fog",
                "multi_compile_instancing",
                "multi_compile_fwdadd_fullshadows"
            },
            keywords = new KeywordDescriptor[]
            {
                defaultModeKeywords,
                specularHighlightsKeyword
            },
            
            requiredVaryings = new List<string>()
            {
                "Varyings.positionCS",
                "Varyings.positionWS",
                "Varyings.normalWS",
                "Varyings.tangentWS",
                "Varyings.texCoord1",
                "Varyings.shadowCoord"
            },
            
            requiredAttributes = new List<string>()
            {
                "Attributes.positionOS",
                "Attributes.normalOS",
                "Attributes.tangentOS",
                "Attributes.uv1",
            },
            
        };
        
        ShaderPass m_ShadowCaster = new ShaderPass
        {
            // Definition
            displayName = "SHADOWCASTER",
            referenceName = "SHADERPASS_SHADOWCASTER",
            passInclude = "Packages/com.z3y.shadergraphex/hlsl/ShadowCasterPass.hlsl",
            varyingsInclude = "Packages/com.z3y.shadergraphex/hlsl/Varyings.hlsl",
            useInPreview = false,
            
            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
                PBRMasterNode.MetallicSlotId
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/ShaderGraph.hlsl",
            },
            pragmas = new List<string>()
            {
                "target 4.5",
                "multi_compile_instancing",
                "multi_compile_shadowcaster"
            },
            keywords = new KeywordDescriptor[]
            {
                defaultModeKeywordsShadowCaster
            },

            lightMode = "ShadowCaster"
        };
        
        ShaderPass m_Meta = new ShaderPass
        {
            // Definition
            displayName = "META_BAKERY",
            referenceName = "SHADERPASS_META",
            passInclude = "Packages/com.z3y.shadergraphex/hlsl/MetaPass.hlsl",
            varyingsInclude = "Packages/com.z3y.shadergraphex/hlsl/Varyings.hlsl",
            useInPreview = false,
            
            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.EmissionSlotId,
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "UnityMetaPass.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/ShaderGraph.hlsl",
            },
            pragmas = new List<string>()
            {
                "target 4.5"
            },
            keywords = new KeywordDescriptor[]
            {
                editorVisualizationKeyword
            },
            
            requiredVaryings = new List<string>()
            {
                "Varyings.positionCS",
                "Varyings.positionWS",
                "Varyings.lightCoord",
                "Varyings.vizUV"
            },
            
            requiredAttributes = new List<string>()
            {
                "Attributes.positionOS",
                "Attributes.uv0",
                "Attributes.uv1",
                "Attributes.uv2",
            },
            
            CullOverride = "Cull Off",
            
            lightMode = "Meta"
        };
        
#endregion

#region Keywords

    private static KeywordDescriptor vertexLightsFragment = new KeywordDescriptor()
    {
        displayName = "Editor Visualization",
        referenceName = "EDITOR_VISUALIZATION",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Global,
    };

    private static KeywordDescriptor editorVisualizationKeyword = new KeywordDescriptor()
    {
        displayName = "Editor Visualization",
        referenceName = "EDITOR_VISUALIZATION",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Global,
    };


    private static KeywordDescriptor defaultModeKeywords = new KeywordDescriptor()
    {
        displayName = "Mode Keywords",
        referenceName = "_ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };

    private static KeywordDescriptor bakeryMonoSHKeyword = new KeywordDescriptor()
    {
        displayName = "Mono SH Keyword",
        referenceName = "BAKERY_MONOSH",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };
    
    private static KeywordDescriptor nonLinearLightProbeSHKeyword = new KeywordDescriptor()
    {
        displayName = "Non-linear Light Probe SH",
        referenceName = "NONLINEAR_LIGHTPROBESH",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };
    
    private static KeywordDescriptor lightmappedSpecularKeyword = new KeywordDescriptor()
    {
        displayName = "Lightmap Specular Keyword",
        referenceName = "_LIGHTMAPPED_SPECULAR",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };
    
    private static KeywordDescriptor reflectionsKeyword = new KeywordDescriptor()
    {
        displayName = "Reflections Keyword",
        referenceName = "_GLOSSYREFLECTIONS_OFF",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };
    
    private static KeywordDescriptor specularHighlightsKeyword = new KeywordDescriptor()
    {
        displayName = "Specular Highlights Keyword",
        referenceName = "_SPECULARHIGHLIGHTS_OFF",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };
    
    private static KeywordDescriptor defaultModeKeywordsShadowCaster = new KeywordDescriptor()
    {
        displayName = "Mode Keywords",  
        referenceName = "_ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAFADE_ON",
        type = KeywordType.Boolean,
        definition = KeywordDefinition.ShaderFeature,
        scope = KeywordScope.Local,
    };


        #endregion

        public int GetPreviewPassIndex() { return 0; }

        private static ActiveFields GetActiveFieldsFromMasterNode(PBRMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(PBRMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                baseActiveFields.Add("AlphaClip");
            }

            baseActiveFields.AddAll("Anisotropy");
            baseActiveFields.AddAll("Tangent");


            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                // transparent-only defines
                baseActiveFields.Add("SurfaceType.Transparent");

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }
                else if (masterNode.alphaMode == AlphaMode.Premultiply)
                {
                    baseActiveFields.Add("BlendMode.Premultiply");
                }
            }
            
            if (ShaderGraphExtendedUtils.AudioLinkExists)
            {
                baseActiveFields.Add("features.AudioLink");
            }
            
            if (pass.lightMode == "ForwardBase" || pass.lightMode == "ForwardAdd")
            {
                if (masterNode.gsaa) baseActiveFields.Add("features.GSAA");
                if (masterNode.anisotropy) baseActiveFields.Add("features.Anisotropy");
                if (masterNode.alphaToCoverage) baseActiveFields.Add("features.A2C");
                
                if (masterNode.flatLit) baseActiveFields.Add("features.FlatLit");
                
            }

            if (pass.lightMode == "ForwardBase")
            {
                if (masterNode.bicubicLightmap) baseActiveFields.Add("features.BicubicLightmap");
                
                if (ShaderGraphExtendedUtils.LTCGIExists)
                {
                    baseActiveFields.Add("features.LTCGI");
                }
                
                baseActiveFields.Add("features.DisableLPPV");
            }

            return activeFields;
        }

        /*
        private static bool GenerateShaderPass(PBRMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            // use standard shader pass generation
            return GenerationUtilsBuiltIn.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                BuiltInLitGraphResources.s_Dependencies, BuiltInLitGraphResources.s_ResourceClassName, BuiltInLitGraphResources.s_AssemblyName);
        }
        */
        
        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // BuiltInUnlitSubShader.cs
                //sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("2cb9502eccfabce4a9a5f3678bbd4486"));
            }

            // Master Node data
            var pbrMasterNode = masterNode as PBRMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);

            
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(pbrMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                
                surfaceTags.GetTags(tagsBuilder, "");
                if (ShaderGraphExtendedUtils.LTCGIExists)
                {
                    tagsBuilder.ReplaceInCurrentMapping("}", "");
                    tagsBuilder.AppendLine(" \"LTCGI\" = \"_LTCGI\"");
                    tagsBuilder.AppendLine("}");
                }
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                // forwardbase pass
                ShaderGraphExtendedUtils.SetRenderStateForwardPass(pbrMasterNode, ref m_ForwardBasePass, ref subShader);
                
                var activeFields = GetActiveFieldsFromMasterNode(pbrMasterNode, m_ForwardBasePass);
                GenerationUtilsBuiltIn.GenerateShaderPass(pbrMasterNode, m_ForwardBasePass, mode, activeFields, subShader,
                    sourceAssetDependencyPaths,
                    BuiltInGraphResources.s_Dependencies,
                    BuiltInGraphResources.s_ResourceClassName,
                    BuiltInGraphResources.s_AssemblyName);

                if (pbrMasterNode.additionalPass != null && pbrMasterNode.additionalPass is Shader additionalPassShader)
                {
                    var shaderName = additionalPassShader.name;
                    subShader.AddShaderChunk($"UsePass \"{shaderName}/FORWARDBASE\"", true);
                }
                
                //fwd add
                ShaderGraphExtendedUtils.SetRenderStateForwardPass(pbrMasterNode, ref m_ForwardAddPass, ref subShader);
                var activeFieldsFwdAdd = GetActiveFieldsFromMasterNode(pbrMasterNode, m_ForwardAddPass);
                GenerationUtilsBuiltIn.GenerateShaderPass(pbrMasterNode, m_ForwardAddPass, mode, activeFieldsFwdAdd, subShader,
                    sourceAssetDependencyPaths,
                    BuiltInGraphResources.s_Dependencies,
                    BuiltInGraphResources.s_ResourceClassName,
                    BuiltInGraphResources.s_AssemblyName);


                // shadowcaster pass
                ShaderGraphExtendedUtils.SetRenderStateShadowCasterPass(pbrMasterNode.surfaceType, pbrMasterNode.alphaMode, pbrMasterNode.twoSided.isOn, ref m_ShadowCaster, ref subShader);
                var activeFieldsShadowCaster = GetActiveFieldsFromMasterNode(pbrMasterNode, m_ShadowCaster);
                GenerationUtilsBuiltIn.GenerateShaderPass(pbrMasterNode, m_ShadowCaster, mode, activeFieldsShadowCaster, subShader,
                    sourceAssetDependencyPaths,
                    BuiltInGraphResources.s_Dependencies,
                    BuiltInGraphResources.s_ResourceClassName,
                    BuiltInGraphResources.s_AssemblyName);
                
                var activeFieldsMeta = GetActiveFieldsFromMasterNode(pbrMasterNode, m_Meta);
                GenerationUtilsBuiltIn.GenerateShaderPass(pbrMasterNode, m_Meta, mode, activeFieldsMeta, subShader,
                    sourceAssetDependencyPaths,
                    BuiltInGraphResources.s_Dependencies,
                    BuiltInGraphResources.s_ResourceClassName,
                    BuiltInGraphResources.s_AssemblyName);
            }

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            if (masterNode is ICanChangeShaderGUI canChangeShaderGui && !canChangeShaderGui.OverrideEnabled)
            {
                subShader.AddShaderChunk("CustomEditor \"z3y.ShaderGraphExtended.DefaultInspector\" ");
            }

            return subShader.GetShaderString(0);
        }

        private static string RemovePipelineName()
        {
            return "";
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return true;
        }



        public BuiltInLitSubShaderExtended() { }
    }
}
