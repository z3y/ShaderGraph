using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("z3y", "Luminance")]
    class LuminanceNode : CodeFunctionNode
    {
        public LuminanceNode()
        {
            name = "Luminance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Luminance_Node", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string Luminance_Node(
            [Slot(0, Binding.None)] in UnityEngine.Vector3 Color,
            [Slot(1, Binding.None)] out Vector1 Out
            )
        {
            return
                @"
{
    Out = dot(Color, float3(0.0396819152, 0.458021790, 0.00609653955));
}
";
        }
    }
}