using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{

    [Title("Input", "Texture", "Texel Size Array")]
    class Texture2DArrayPropertiesNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotWId = 0;
        public const int OutputSlotHId = 2;
        public const int OutputSlotH2Id = 3;
        public const int OutputSlotW2Id = 4;
        
        public const int TextureInputId = 1;
        const string kOutputSlotWName = "Width";
        const string kOutputSlotHName = "Height";
        const string kTextureInputName = "Texture";
        const string kOutputSlotW2Name = "1/Width";
        const string kOutputSlotH2Name = "1/Height";

        public override bool hasPreview { get { return false; } }

        public Texture2DArrayPropertiesNode()
        {
            name = "Texel Size";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWId, kOutputSlotWName, kOutputSlotWName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotHId, kOutputSlotHName, kOutputSlotHName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            
            AddSlot(new Vector1MaterialSlot(OutputSlotW2Id, kOutputSlotW2Name, kOutputSlotW2Name, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotH2Id, kOutputSlotH2Name, kOutputSlotH2Name, SlotType.Output, 0, ShaderStageCapability.Fragment));
            
            AddSlot(new Texture2DArrayInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            RemoveSlotsNameNotMatching(new[] { OutputSlotWId, OutputSlotHId, TextureInputId, OutputSlotW2Id, OutputSlotH2Id});
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.z;", GetVariableNameForSlot(OutputSlotWId), GetSlotValue(TextureInputId, generationMode)));
			sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.w;", GetVariableNameForSlot(OutputSlotHId), GetSlotValue(TextureInputId, generationMode)));
            
            sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.x;", GetVariableNameForSlot(OutputSlotW2Id), GetSlotValue(TextureInputId, generationMode)));
            sb.AppendLine(string.Format("$precision {0} = {1}_TexelSize.y;", GetVariableNameForSlot(OutputSlotH2Id), GetSlotValue(TextureInputId, generationMode)));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
