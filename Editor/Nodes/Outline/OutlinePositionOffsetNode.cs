using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("z3y", "Outline", "Scale")]
    class OutlinePositionScaleNode : CodeFunctionNode
    {
        public OutlinePositionScaleNode()
        {
            name = "Outline Scale";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("OutlinePositionScaleFunction", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public override PreviewMode previewMode => PreviewMode.Preview3D;

        static string OutlinePositionScaleFunction(
            [Slot(0, Binding.ObjectSpaceNormal)] in Vector3 Normal,
            [Slot(1, Binding.ObjectSpacePosition)] in Vector3 Position,
            [Slot(2, Binding.None, 0.1f, 0.1f, 0.1f, 0.1f)] in Vector1 Scale,
            [Slot(3, Binding.None)] out Vector3 PositionOS)
        {
            PositionOS = default;
            return
                @"
{
#if ((SHADERPASS == SHADERPASS_OUTLINE) && !defined(SHADERGRAPH_PREVIEW)) || defined(UNITY_PASS_SHADOWCASTER)
PositionOS = Position + (Normal * Scale);
#else
PositionOS = Position;
#endif
}
";
        }
    }
}