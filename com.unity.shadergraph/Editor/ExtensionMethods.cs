using UnityEngine;

namespace z3y.ShaderGraphExtended
{
    public static class ExtensionMethods
    {
        public static void ToggleKeyword(this Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
                return;
            }
            material.DisableKeyword(keyword);
        }
    }
}