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
    class BuiltInUnlitSubShaderExtended : IUnlitSubShader
    {
#region Passes
        ShaderPass m_UnlitPass = new ShaderPass
        {
            // Definition
            displayName = "FORWARDBASE",
            referenceName = "SHADERPASS_UNLIT",
            passInclude = "Packages/com.z3y.shadergraphex/hlsl/UnlitPass.hlsl",
            varyingsInclude = "Packages/com.z3y.shadergraphex/hlsl/Varyings.hlsl",
            useInPreview = true,

            // Port mask
            vertexPorts = new List<int>()
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId
            },
            pixelPorts = new List<int>
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
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
                "multi_compile_fog",
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                defaultModeKeywords
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
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
            },
            pixelPorts = new List<int>
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
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

        private static ActiveFields GetActiveFieldsFromMasterNode(UnlitMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            // Graph Vertex
            if(masterNode.IsSlotConnected(UnlitMasterNode.PositionSlotId) || 
               masterNode.IsSlotConnected(UnlitMasterNode.VertNormalSlotId) || 
               masterNode.IsSlotConnected(UnlitMasterNode.VertTangentSlotId))
            {
                baseActiveFields.Add("features.graphVertex");
            }

            // Graph Pixel (always enabled)
            baseActiveFields.Add("features.graphPixel");

            if (masterNode.IsSlotConnected(UnlitMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == UnlitMasterNode.AlphaThresholdSlotId).value > 0.0f)
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
            
            if (pass.referenceName == "SHADERPASS_UNLIT")
            {
                if (masterNode.alphaToCoverage) baseActiveFields.Add("features.A2C");
            }
            
            if (ShaderGraphExtendedUtils.AudioLinkExists)
            {
                baseActiveFields.Add("features.AudioLink");
            }

            return activeFields;
        }

        private static bool GenerateShaderPass(UnlitMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            // use standard shader pass generation
            return GenerationUtilsBuiltIn.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths,
                BuiltInGraphResources.s_Dependencies, BuiltInGraphResources.s_ResourceClassName, BuiltInGraphResources.s_AssemblyName);
        }
        
        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // BuiltInUnlitSubShader.cs
                //sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("2cb9502eccfabce4a9a5f3678bbd4486"));
            }

            // Master Node data
            var unlitMasterNode = masterNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(unlitMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                // unlit pass
                ShaderGraphExtendedUtils.SetRenderStateForwardPass(unlitMasterNode, ref m_UnlitPass, ref subShader);
                GenerateShaderPass(unlitMasterNode, m_UnlitPass, mode, subShader, sourceAssetDependencyPaths);

                if (unlitMasterNode.additionalPass)
                {
                    var shaderName = unlitMasterNode.additionalPass.name;
                    subShader.AddShaderChunk($"UsePass \"{shaderName}/FORWARDBASE\"", true);
                }
                
                
                // shadowcaster pass
                if (unlitMasterNode.generateShadowCaster)
                {
                    ShaderGraphExtendedUtils.SetRenderStateShadowCasterPass(unlitMasterNode.surfaceType, unlitMasterNode.alphaMode, unlitMasterNode.twoSided.isOn, ref m_ShadowCaster, ref subShader);
                    GenerateShaderPass(unlitMasterNode, m_ShadowCaster, mode, subShader, sourceAssetDependencyPaths);
                }
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



        public BuiltInUnlitSubShaderExtended() { }
    }
}
