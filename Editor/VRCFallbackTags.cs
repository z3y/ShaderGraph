using System;
using System.Text;

namespace z3y.BuiltIn.ShaderGraph
{
    [Serializable]
    public class VRCFallbackTags
    {
        public enum ShaderType
        {
            Standard,
            Unlit,
            VertexLit,
            Toon,
            Particle,
            Sprite,
            Matcap,
            MobileToon,
            Hidden
        };
            
        public enum ShaderMode
        {
            Opaque,
            Cutout,
            Transparent,
            Fade
        };

        public ShaderType type = ShaderType.Standard;
        public ShaderMode mode = ShaderMode.Opaque;
        public bool doubleSided = false;
            
        public override string ToString()
        {
            if (type == 0 && mode == 0 && !doubleSided)
            {
                return string.Empty;
            }
            
            var sb = new StringBuilder();
            sb.Append("\"VRCFallback\" = \"");
            if (type != 0) sb.Append(Enum.GetName(typeof(ShaderType), type));
            if (mode != 0) sb.Append(Enum.GetName(typeof(ShaderMode), mode));
            if (doubleSided) sb.Append("DoubleSided");
            sb.Append("\"");
            
            return sb.ToString();
        }
    }
}