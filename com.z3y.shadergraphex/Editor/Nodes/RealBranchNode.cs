using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("Utility", "Logic", "Real Branch")]
    class RealBranchNode : CodeFunctionNode
    {
        public RealBranchNode()
        {
            name = "Real Branch";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Real_BranchNode", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string Real_BranchNode(
            [Slot(0, Binding.None)] in Boolean Predicate,
            [Slot(1, Binding.None, 1,1,1,1)] in DynamicDimensionVector True,
            [Slot(2, Binding.None)] in DynamicDimensionVector False,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    UNITY_BRANCH
    if (Predicate) Out = True;
    else Out = False;
}
";
        }
    }
}