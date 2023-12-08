using z3y.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using System;

namespace z3y.BuiltIn
{
    internal static class Property
    {
        public static string SpecularWorkflowMode() { return SG_SpecularWorkflowMode; }
        public static string Surface() { return SG_Surface; }
        public static string Blend() { return SG_Blend; }
        public static string AlphaClip() { return SG_AlphaClip; }
        public static string SrcBlend() { return SG_SrcBlend; }
        public static string DstBlend() { return SG_DstBlend; }
        public static string ZWrite() { return SG_ZWrite; }
        public static string ZWriteControl() { return SG_ZWriteControl; }
        public static string ZTest() { return SG_ZTest; }   // no HW equivalent
        public static string Cull() { return SG_Cull; }
        public static string CastShadows() { return SG_CastShadows; }
        public static string ReceiveShadows() { return SG_ReceiveShadows; }
        public static string QueueOffset() { return SG_QueueOffset; }
        public static string QueueControl() { return SG_QueueControl; }

        // for shadergraph shaders (renamed more uniquely to avoid potential naming collisions with HDRP properties and user properties)
        public static readonly string SG_SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string SG_Surface = "_Surface";
        public static readonly string SG_Blend = "_Blend";
        public static readonly string SG_AlphaClip = "_AlphaClip";
        public static readonly string SG_SrcBlend = "_SrcBlend";
        public static readonly string SG_DstBlend = "_DstBlend";
        public static readonly string SG_ZWrite = "_ZWrite";
        public static readonly string SG_ZWriteControl = "_ZWriteControl";
        public static readonly string SG_ZTest = "_ZTest";
        public static readonly string SG_Cull = "_CullMode";
        public static readonly string SG_CastShadows = "_CastShadows";
        public static readonly string SG_ReceiveShadows = "_ReceiveShadows";
        public static readonly string SG_QueueOffset = "_QueueOffset";
        public static readonly string SG_QueueControl = "_QueueControl";

        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
    }

    internal static class Keyword
    {
        // These should be used to control the above (currently in the template)
        public static readonly string SG_ReceiveShadowsOff = "_BUILTIN_RECEIVE_SHADOWS_OFF";
        public static readonly string SG_Emission = "_EMISSION";
        public static readonly string SG_AlphaTestOn = "_ALPHATEST_ON";
        //public static readonly string SG_AlphaClip = "_AlphaClip";
        public static readonly string SG_SurfaceTypeTransparent = "_SURFACE_TYPE_TRANSPARENT";
        public static readonly string SG_AlphaPremultiplyOn = "_ALPHAPREMULTIPLY_ON";
        public static readonly string SG_AlphaModulateOn = "_ALPHAMODULATE_ON";
    }

    internal static class BuiltInMaterialInspectorUtilities
    {
        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, HLSLDeclaration declarationType = HLSLDeclaration.DoNotDeclare)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = declarationType,
                value = defaultValue,
                overrideReferenceName = referenceName,
            });
        }

        internal static void AddFloatProperty(this PropertyCollector collector, string referenceName, float defaultValue, string displayName)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = defaultValue,
                overrideReferenceName = referenceName,
                displayName = displayName,
            });
        }

        internal static void AddFloatSliderProperty(this PropertyCollector collector, string referenceName, float defaultValue, string displayName, UnityEngine.Vector2 rangeValues)
        {
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Slider,
                hidden = false,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = defaultValue,
                overrideReferenceName = referenceName,
                displayName = displayName,
                rangeValues = rangeValues
            });
        }
    }
}
