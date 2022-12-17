using System;

namespace UnityEditor.ShaderGraph
{
    enum SurfaceType
    {
        Opaque,
        Transparent
    }
    
    enum SurfaceMode
    {
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
