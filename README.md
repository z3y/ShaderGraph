# ShaderGraph
ShaderGraph with Built-in pipeline support for Unity 2019 for VRChat (Forward Rendering only)

## Features
- Bakery Mono SH
- Mono SH Lightmapped Specular
- Bakery Alpha Meta
- Geometric Specular AA
- Specular Occlusion
- Alpha To Coverage
- Accurate PBR Shading
- Additional Pass
- Bicubic Lightmap
- Anisotropy
- LTCGI
- Audio Link
- Node Hotkeys 

## Future Updates
Since this is based on an older shader graph version (8.3.1) **things are likely going to break**  with the next Shader Graph update. A new version will be made once VRChat moves to a newer Unity version, until then treat this as **experimental**


## Installation
If you have unity's shader graph in your project remove it first

To install the latest version open the [Package Manager](https://user-images.githubusercontent.com/33181641/210658098-851627b9-c67d-4fab-a493-94e2c8bb53e3.png), select `Add package from git url` and them in this order:

```
https://github.com/z3y/ShaderGraph.git#2019?path=/com.unity.render-pipelines.core
```

```
https://github.com/z3y/ShaderGraph.git#2019?path=/com.unity.shadergraph
```
```
https://github.com/z3y/ShaderGraph.git#2019?path=/com.z3y.shadergraphex
```

To install a specific version add #VersionNumber from the release tags to the end of the urls


## How to use
Create a new Shader Graph shader `Create > Shader > PBR or Unlit Graph`

[Documentation](https://github.com/z3y/ShaderGraph/blob/main/Documentation.md)

##

[Discord](https://discord.gg/bw46tKgRFT)
