using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;


namespace z3y.ShaderGraphExtended
{
    internal class ShaderGraphExtendedUtils
    {
        public static void SetRenderStateForwardBasePass(SurfaceType surfaceType, AlphaMode alphaMode, bool twoSided, ref ShaderPass pass, ref ShaderGenerator result)
        {
            var options = ShaderGenerator.GetMaterialOptions(surfaceType, alphaMode, twoSided);
            

            pass.ZWriteOverride = "ZWrite [_ZWrite]";

            if (string.IsNullOrEmpty(pass.ZTestOverride))
            {
                pass.ZTestOverride = "ZTest " + options.zTest.ToString();
            }
            
            pass.CullOverride = "Cull [_Cull]";


            pass.BlendOverride = "Blend [_SrcBlend] [_DstBlend]";

        }
    }
}