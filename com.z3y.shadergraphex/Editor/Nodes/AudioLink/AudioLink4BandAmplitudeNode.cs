using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "4Band Amplitude")]
    class AudioLink4BandAmplitudeNode : CodeFunctionNode, IMayRequireTime
    {
        public AudioLink4BandAmplitudeNode()
        {
            name = "4 Band Amplitude";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AudioLink_4BandAmplitudeNode", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string AudioLink_4BandAmplitudeNode(
            [Slot(0, Binding.None)] Vector1 Band,
            [Slot(1, Binding.None)] Vector1 Delay,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = AudioLinkData(ALPASS_AUDIOLINK + uint2( Delay, Band ) ).r;
}
";
        }

        public bool RequiresTime() => true;
    }
}