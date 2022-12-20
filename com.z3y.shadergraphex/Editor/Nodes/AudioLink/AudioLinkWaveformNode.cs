using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "Waveform Preview")]
    class AudioLinkWaveformNode : CodeFunctionNode, IMayRequireTime
    {
        public AudioLinkWaveformNode()
        {
            name = "Waveform Preview";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("AudioLink_WaveformPreviewNode", BindingFlags.Static | BindingFlags.NonPublic);
        }
        

        static string AudioLink_WaveformPreviewNode(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    float Sample = AudioLinkLerpMultiline( ALPASS_WAVEFORM + float2( 200. * UV.x, 0 ) ).r;
    Out = 1 - 50 * abs( Sample - UV.y * 2. + 1 );
}
";
        }

        public bool RequiresTime() => true;
    }
}