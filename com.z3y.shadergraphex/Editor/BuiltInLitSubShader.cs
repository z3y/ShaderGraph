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
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "AutoLight.cginc",
                "Lighting.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/Shims.hlsl",
            },
            pragmas = new List<string>()
            {
                "target 4.5",
                "multi_compile_fog",
                "multi_compile_instancing",
                "multi_compile_fwdbase"
            },
            keywords = new KeywordDescriptor[]
            {
                defaultModeKeywords
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
            }
            
            
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
            },

            // Pass setup
            includes = new List<string>()
            {
                "UnityCG.cginc",
                "AutoLight.cginc",
                "Lighting.cginc",
                "Packages/com.z3y.shadergraphex/hlsl/Shims.hlsl",
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
                defaultModeKeywords
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
            }
            
            
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
                "Packages/com.z3y.shadergraphex/hlsl/Shims.hlsl",
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
        
#endregion
    private static KeywordDescriptor defaultModeKeywords = new KeywordDescriptor()
    {
        displayName = "Mode Keywords",
        referenceName = "_ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON",
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
#region Keywords


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
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                // forwardbase pass
                ShaderGraphExtendedUtils.SetRenderStateForwardPass(pbrMasterNode, ref m_ForwardBasePass, ref subShader);
                
                var activeFields = GetActiveFieldsFromMasterNode(pbrMasterNode, m_ForwardBasePass);
                GenerationUtilsBuiltIn.GenerateShaderPass(pbrMasterNode, m_ForwardBasePass, mode, activeFields, subShader,
                    sourceAssetDependencyPaths,
                    BuiltInGraphResources.s_Dependencies,
                    BuiltInGraphResources.s_ResourceClassName,
                    BuiltInGraphResources.s_AssemblyName);

                if (pbrMasterNode.additionalPass)
                {
                    var shaderName = pbrMasterNode.additionalPass.name;
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
