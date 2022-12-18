using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Instance ID")]
    sealed class InstanceIDNode : AbstractMaterialNode
    {

        public InstanceIDNode()
        {
            name = "Instance ID";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(0, "Out", "Out", SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { 0 });

        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return @"unity_InstanceID";
        }
    }
}