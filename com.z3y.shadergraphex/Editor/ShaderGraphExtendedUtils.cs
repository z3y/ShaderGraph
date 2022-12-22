using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


namespace z3y.ShaderGraphExtended
{
    internal class ShaderGraphExtendedUtils
    {
        public static void SetRenderStateForwardPass<T>(T masterNode, ref ShaderPass pass, ref ShaderGenerator result)
            where T : MasterNode
        {
            //var options = ShaderGenerator.GetMaterialOptions(surfaceType, alphaMode, twoSided);

            pass.ZWriteOverride = "ZWrite [_ZWrite]";

            if (masterNode.cullingOverride != MasterNode.CullingOverrideMode.None)
            {
                pass.CullOverride =
                    "Cull " + Enum.GetName(typeof(MasterNode.CullingOverrideMode), masterNode.cullingOverride);
            }
            else
            {
                pass.CullOverride = "Cull [_Cull]";
            }




            pass.BlendOverride = "Blend [_SrcBlend] [_DstBlend]";

            if (!string.IsNullOrEmpty(pass.lightMode) && pass.lightMode.Equals("ForwardAdd"))
            {
                pass.ZWriteOverride = "ZWrite Off";
                pass.ZTestOverride = "ZTest LEqual";
                pass.BlendOverride = "Blend [_SrcBlend] One";

                pass.BlendOverride += "\nFog { Color (0,0,0,0) }";
            }


            if (masterNode.alphaToCoverage)
            {
                pass.ZWriteOverride += "\nAlphaToMask [_AlphaToMask]";
            }
        }

        public static void SetRenderStateShadowCasterPass(SurfaceType surfaceType, AlphaMode alphaMode, bool twoSided,
            ref ShaderPass pass, ref ShaderGenerator result)
        {
            // var options = ShaderGenerator.GetMaterialOptions(surfaceType, alphaMode, twoSided);

            pass.ZWriteOverride = "ZWrite On";
            pass.CullOverride = "Cull [_Cull]";
            pass.ZTestOverride = "ZTest LEqual";
        }

        public const string AudioLinkPath = "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc";
        public static bool AudioLinkExists => File.Exists(AudioLinkPath);

        public const string LTCGIPath = "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc";
        public static bool LTCGIExists => File.Exists(LTCGIPath);
    }
}