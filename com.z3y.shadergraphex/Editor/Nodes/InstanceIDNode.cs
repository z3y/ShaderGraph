using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace z3y.ShaderGraphExtended
{
    [Title("Input", "Scene", "Instance ID")]
    sealed class InstanceIDNode : AbstractMaterialNode, IGeneratesFunction
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
            return @"Unity_GetInstanceID()";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction($"Unity_GetInstanceID", s => s.Append(
                @"
uint Unity_GetInstanceID()
{
#ifdef UNITY_INSTANCING_ENABLED
    return unity_InstanceID;
#else
    return 0;
#endif
}
        "));
        }
    }
}