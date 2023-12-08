using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph
{
    static class BuiltInStructs
    {
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            populateWithCustomInterpolators = true,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                //BuiltInStructFields.Varyings.color,
                //StructFields.Varyings.viewDirectionWS,
                StructFields.Varyings.screenPosition,
                BuiltInStructFields.Varyings.lightmapUV,
                BuiltInStructFields.Varyings.lightmapUVAndRT,
                BuiltInStructFields.Varyings.editorVisualizationLightCoord,
                BuiltInStructFields.Varyings.editorVisualizationVizUV,
                //BuiltInStructFields.Varyings.sh,
                //BuiltInStructFields.Varyings.fogFactorAndVertexLight,
                BuiltInStructFields.Varyings.vertexLight,
                BuiltInStructFields.Varyings.fogCoord,
                BuiltInStructFields.Varyings.shadowCoord,
                StructFields.Varyings.instanceID,
                BuiltInStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                BuiltInStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
                StructFields.Varyings.cullFace,
            }
        };
    }
}
