# ShaderGraph
Modified ShaderGraph Built-in pipeline target for Unity 2022 for VRChat (Forward Rendering only).

## Features
- Fixed GPU instancing and rendering in VR
- Bakery Mono SH
- Mono SH Lightmapped Specular
- Geometric Specular AA
- Anisotropy
- Specular Occlusion
- Alpha To Coverage
- Bicubic Lightmap
- Anisotropy
- Audio Link
- SSR
- More

## Known issues

- Grab Pass / Screen Color node is broken in VR
- If you run into more bugs you can try out [ShaderGraphZ](https://github.com/z3y/ShaderGraphZ), a free custom node editor for the built-in pipeline, it is more reliable than this project

## Installation

First, open the package manager and install Shader Graph.

To install the latest version open the [Package Manager](https://user-images.githubusercontent.com/33181641/210658098-851627b9-c67d-4fab-a493-94e2c8bb53e3.png), select `Add package from git url` and add:

```
https://github.com/z3y/ShaderGraph.git
```

To install a specific version add #VersionNumber from the release tags to the end of the urls

(2019 Version https://github.com/z3y/ShaderGraph/tree/2019)

## How to use
Create a new Shader Graph shader `Create > Shader Graph > BuiltIn (z3y)`

[Documentation](https://github.com/z3y/ShaderGraph/blob/main/Documentation.md)

![image](https://github.com/z3y/ShaderGraph/assets/33181641/5dc732c9-5518-4661-985c-073d067f412d)

Shader Graph target code licensed under the Unity Companion License for Unity-dependent projects [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License). 

##

[Discord](https://discord.gg/bw46tKgRFT)
