using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "4Band Amplitude Lerp")]
    class AudioLink4BandAmplitudeLerpNode : CodeFunctionNode, IMayRequireTime
    {
        public AudioLink4BandAmplitudeLerpNode()
        {
            name = "4 Band Amplitude Lerp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AudioLink_4BandAmplitudeNodeLerp", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string AudioLink_4BandAmplitudeNodeLerp(
            [Slot(0, Binding.None)] Vector1 Band,
            [Slot(1, Binding.None)] Vector1 Delay,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = AudioLinkLerp( ALPASS_AUDIOLINK + float2( Delay, Band ) ).r;
}
";
        }

        public bool RequiresTime() => true;
    }
}