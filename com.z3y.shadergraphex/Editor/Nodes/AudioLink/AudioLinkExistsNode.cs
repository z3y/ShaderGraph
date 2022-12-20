using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace z3y.ShaderGraphExtended
{
    [Title("VRChat", "AudioLink", "Available")]
    sealed class AudioLinkExistsNode : AbstractMaterialNode, IMayRequireTime
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
    }
}