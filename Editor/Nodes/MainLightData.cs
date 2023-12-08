using System.Reflection;
using UnityEditor.ShaderGraph;

namespace z3y.BuiltIn.ShaderGraph.Nodes
{
    [Title("z3y", "Main Light Data")]
    class MainLightDataNode : CodeFunctionNode
    {
        public MainLightDataNode()
        {
            name = "Main Light";
        }

        public override bool hasPreview => false;

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Node_GetMainLightData", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Node_GetMainLightData(
            [Slot(0, Binding.None)] out UnityEngine.Vector3 Color,
            [Slot(1, Binding.None)] out UnityEngine.Vector3 Direction,
            [Slot(3, Binding.None)] out UnityEngine.Vector3 AverageDirection,
            [Slot(2, Binding.None)] out Vector1 Attenuation)
        {
            Color = default;
            Direction = default;
            AverageDirection = default;
            return
@"
{
    #if (defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)) && !defined(SHADERGRAPH_PREVIEW)
        UnityLightData lightData = GetCustomMainLightData(staticVaryings);
        Color = lightData.color;
        Direction = lightData.direction;
        Attenuation = lightData.attenuation;
        #if !defined(LIGHTMAP_ON) && defined(UNITY_PASS_FORWARDBASE)
            half3 lightProbeDirection = unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz;
            //AverageDirection = normalize(lerp(Direction + lightProbeDirection, Direction, saturate(dot(Color, 1.0))));
            AverageDirection = normalize(Direction + lightProbeDirection);
        #else
            AverageDirection = Direction;
        #endif
    #elif defined(SHADERGRAPH_PREVIEW)
        Color = 0.5;
        Direction = normalize(float3(1, 1, 0));
        AverageDirection = normalize(float3(1, 1, 0));
        Attenuation = 1.0;
    #else
        Color = 0;
        Direction = 0;
        AverageDirection = 0;
        Attenuation = 0;
    #endif
}
";
        }
    }
}