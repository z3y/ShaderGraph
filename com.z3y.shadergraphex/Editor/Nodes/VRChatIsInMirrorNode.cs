using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "In Mirror")]
    class VRChatIsInMirrorNode : CodeFunctionNode
    {
        public VRChatIsInMirrorNode()
        {
            name = "VRC Mirror";
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("VRChat_IsInMirrorNode", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string VRChat_IsInMirrorNode(
            [Slot(0, Binding.None)] out Boolean NotInMirror,
            [Slot(1, Binding.None)] out Boolean InMirrorVR,
            [Slot(2, Binding.None)] out Boolean InMirrorDesktop)
        {
            return
                @"
{
    NotInMirror = _VRChatMirrorMode == 0;
    InMirrorVR = _VRChatMirrorMode == 1;
    InMirrorDesktop = _VRChatMirrorMode == 2;
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("_VRChatMirrorMode", builder =>
            {
                builder.Append("half _VRChatMirrorMode;");
            });
            base.GenerateNodeFunction(registry, generationMode);
        }
    }
}