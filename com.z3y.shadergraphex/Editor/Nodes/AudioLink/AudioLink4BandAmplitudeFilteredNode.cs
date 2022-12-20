using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "4Band Amplitude Filtered")]
    class AudioLink4BandAmplitudeFilteredNode : CodeFunctionNode, IMayRequireTime
    {
        public AudioLink4BandAmplitudeFilteredNode()
        {
            name = "4 Band Amplitude Filtered";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AudioLink_4BandAmplitudeNodeFiltered", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string AudioLink_4BandAmplitudeNodeFiltered(
            [Slot(0, Binding.None)] Vector1 Band,
            [Slot(1, Binding.None)] Vector1 FilterAmount,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = AudioLinkLerp( ALPASS_FILTEREDAUDIOLINK + float2( FilterAmount, Band ) ).r;
}
";
        }

        public bool RequiresTime() => true;
    }
}