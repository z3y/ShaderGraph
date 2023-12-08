using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("z3y", "fwidth")]
    class FwidthNode : CodeFunctionNode
    {
        public FwidthNode()
        {
            name = "fwidth";
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Node_fwidth", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Node_fwidth(
            [Slot(0, Binding.None)] in DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out
            )
        {
            return
@"
{
    Out = fwidth(In);
}
";
        }
    }
}