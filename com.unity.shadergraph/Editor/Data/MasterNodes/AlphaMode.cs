using System;

namespace UnityEditor.ShaderGraph
{
    enum SurfaceType
    {
        Opaque,
        Transparent
    }
    
    enum RenderMode
    {
        None,
        Opaque,
        Cutout,
        Fade,
        Transparent,
        Additive,
        Multiply
    }

    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }
}
