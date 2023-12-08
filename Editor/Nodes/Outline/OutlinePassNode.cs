using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("z3y", "Outline", "Pass Define")]
    class OutlinePassNode : CodeFunctionNode
    {
        public OutlinePassNode()
        {
            name = "Outline Pass";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("OutlinePassSwitch", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string OutlinePassSwitch(
            [Slot(0, Binding.None)] in DynamicDimensionVector True,
            [Slot(1, Binding.None)] in DynamicDimensionVector False,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
#if (SHADERPASS == SHADERPASS_OUTLINE) && !defined(SHADERGRAPH_PREVIEW)
Out = True;
#else
Out = False;
#endif
}
";
        }
    }
}