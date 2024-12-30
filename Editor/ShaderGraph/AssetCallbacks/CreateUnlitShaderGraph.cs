using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace z3y.BuiltIn.ShaderGraph
{
    static class CreateUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/BuiltIn VRC/Unlit Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateUnlitGraph()
        {
            //var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            var target = new BuiltInTarget();
            target.TrySetActiveSubTarget(typeof(BuiltInUnlitSubTarget));

            target.vrcFallbackTags.type = VRCFallbackTags.ShaderType.Unlit;
            target.allowMaterialOverride = true;

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
