using UnityEngine;

namespace z3y.BuiltIn.ShaderGraph
{
    // Currently this is just the base shader gui, but was put in place in case they're separate later
    public class BuiltInLitGUI : BuiltInBaseShaderGUI
    {
        public static void UpdateMaterial(Material material)
        {
            SetupSurface(material);
        }
    }
}
