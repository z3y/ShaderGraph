using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("AudioLink", "Available")]
    sealed class AudioLinkExistsNode : AbstractMaterialNode, IMayRequireTime, IGeneratesFunction
    {

        public AudioLinkExistsNode()
        {
            name = "Audio Link Available";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new BooleanMaterialSlot(0, "Out", "Out", SlotType.Output, false));
            RemoveSlotsNameNotMatching(new[] { 0 });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return @"AudioLinkIsAvailable()";
        }

        public bool RequiresTime()
        {
            return true;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            AddAudioLinkInclude(registry);
        }

        public static void AddAudioLinkInclude(FunctionRegistry registry)
        {
            registry.RequiresIncludePath("Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc");
        }
    }
}