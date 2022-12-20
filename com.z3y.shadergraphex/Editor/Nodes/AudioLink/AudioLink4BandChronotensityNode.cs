using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "4Band Chronotensity")]
    class AudioLink4BandChronotensityNode : CodeFunctionNode, IMayRequireTime
    {
        public AudioLink4BandChronotensityNode()
        {
            name = "4 Band Chronotensity";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AudioLink_4BandChronotensityNode", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string AudioLink_4BandChronotensityNode(
            [Slot(0, Binding.None)] Vector1 Mode,
            [Slot(1, Binding.None)] Vector1 Band,
            [Slot(5, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = (AudioLinkDecodeDataAsUInt( ALPASS_CHRONOTENSITY + int2(Mode, Band)) % 628319 ) / 100000.0;
}
";
        }

        public bool RequiresTime() => true;
    }
}