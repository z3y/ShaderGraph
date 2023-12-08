using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph
{
    static class BuiltInStructFields
    {
        public static void ApplyCentroidVertexColor()
        {
            StructFields.Varyings.color = new FieldDescriptor(Varyings.name, StructFields.Varyings.color.define, StructFields.Varyings.color.semantic, StructFields.Varyings.color.type,// preprocessor: "defined(VARYINGS_NEED_COLOR)",
                subscriptOptions: StructFields.Varyings.color.subscriptOptions, interpolation: "centroid");
        }

        public struct Varyings
        {
            public static string name = "Varyings";
            public static FieldDescriptor lightmapUV = new FieldDescriptor(name, "lightmapUV", "", ShaderValueType.Float2, preprocessor: "defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)", subscriptOptions: StructFieldOptions.Optional, interpolation: "centroid");
            public static FieldDescriptor lightmapUVAndRT = new FieldDescriptor(name, "lightmapUV", "", ShaderValueType.Float4, preprocessor: "defined(LIGHTMAP_ON) && defined(DYNAMICLIGHTMAP_ON)", subscriptOptions: StructFieldOptions.Optional, interpolation: "centroid");
            //public static FieldDescriptor sh = new FieldDescriptor(Varyings.name, "sh", "", ShaderValueType.Float3, preprocessor: "!defined(LIGHTMAP_ON)", subscriptOptions: StructFieldOptions.Optional);
            //public static FieldDescriptor fogFactorAndVertexLight = new FieldDescriptor(Varyings.name, "fogFactorAndVertexLight", "VARYINGS_NEED_FOG_AND_VERTEX_LIGHT", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor vertexLight = new FieldDescriptor(name, "vertexLight", "", ShaderValueType.Float3, subscriptOptions: StructFieldOptions.Optional, preprocessor:"defined(VERTEXLIGHT_ON) && !defined(VERTEXLIGHT_PS)");
            public static FieldDescriptor fogCoord = new FieldDescriptor(name, "fogCoord", "", ShaderValueType.Float, subscriptOptions: StructFieldOptions.Optional, preprocessor:"defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)");
            public static FieldDescriptor shadowCoord = new FieldDescriptor(name, "shadowCoord", "VARYINGS_NEED_SHADOWCOORD", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional);
            public static FieldDescriptor editorVisualizationLightCoord = new FieldDescriptor(name, "lightCoord", "", ShaderValueType.Float4, subscriptOptions: StructFieldOptions.Optional, preprocessor:"defined(EDITOR_VISUALIZATION)");
            public static FieldDescriptor editorVisualizationVizUV = new FieldDescriptor(name, "vizUV ", "", ShaderValueType.Float2, subscriptOptions: StructFieldOptions.Optional, preprocessor:"defined(EDITOR_VISUALIZATION)");

            public static FieldDescriptor stereoTargetEyeIndexAsRTArrayIdx = new FieldDescriptor(name, "stereoTargetEyeIndexAsRTArrayIdx", "", ShaderValueType.Uint, "SV_RenderTargetArrayIndex", "(defined(UNITY_STEREO_INSTANCING_ENABLED))", StructFieldOptions.Generated);
            public static FieldDescriptor stereoTargetEyeIndexAsBlendIdx0 = new FieldDescriptor(name, "stereoTargetEyeIndexAsBlendIdx0", "", ShaderValueType.Uint, "BLENDINDICES0", "(defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))");
/*            public static FieldDescriptor color = new FieldDescriptor(Varyings.name, "color", "VARYINGS_NEED_COLOR", ShaderValueType.Float4, preprocessor: "defined(VARYINGS_NEED_COLOR)",
                subscriptOptions: StructFieldOptions.Static, interpolation:"centroid");*/
        }
    }
}
