using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace z3y.BuiltIn.ShaderGraph
{
    static class CreateLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/BuiltIn (z3y)/Lit Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateLitGraph()
        {
            //var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            BuiltInTarget target;
            BlockFieldDescriptor[] blockDescriptors;
            CreateBaseLitShader(out target, out blockDescriptors);

            var lit = (BuiltInLitSubTarget)target.activeSubTarget;
            lit.specular = true;
            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }

        [MenuItem("Assets/Create/Shader Graph/BuiltIn (z3y)/Flat-Lit Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateFlatLitGraph()
        {
            //var target = (BuiltInTarget)Activator.CreateInstance(typeof(BuiltInTarget));
            BuiltInTarget target;
            BlockFieldDescriptor[] blockDescriptors;
            CreateBaseLitShader(out target, out blockDescriptors);

            var lit = (BuiltInLitSubTarget)target.activeSubTarget;
            lit.specular = false;
            lit.flatLit = true;

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }


        private static void CreateBaseLitShader(out BuiltInTarget target, out BlockFieldDescriptor[] blockDescriptors)
        {
            target = new BuiltInTarget();
            target.TrySetActiveSubTarget(typeof(BuiltInLitSubTarget));
            target.allowMaterialOverride = true;

            blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.Alpha,
                BuiltInLitSubTarget.AdditionalSurfaceDescription.Reflectance,
                BuiltInLitSubTarget.AdditionalSurfaceDescription.SpecularOcclusion,
            };
        }
    }
}
