# ShaderGraph

Modified ShaderGraph Built-in pipeline target for Unity 2022 for VRChat (Forward Rendering only).

## Installation

- Add the listing `Settings > Packages > Add Repository` in VRChat Creator Companion

```
https://z3y.github.io/vpm-package-listing/
```

- Select your project (Manage Project) and import the `Shader Graph VRC` package

For non VRChat projects you can import the unity package from releases instead

## How to use

- Create a new Shader Graph shader `Create > Shader Graph > BuiltIn VRC`
- For already existing shaders swap the built-in active target in graph settings
- In some cases `Enable Material Override` needs to be checked in order for rendering modes to work properly

## Features

- Fixed GPU instancing and rendering in VR
- Bakery Mono SH
- Grab Pass (enable Grab Pass toggle when using Scene Color node)
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

- If you run into more bugs you can try out [Graphlit](https://github.com/z3y/Graphlit), a free custom node editor for the built-in pipeline, it is more reliable than this project

## Preview

![image](https://github.com/z3y/ShaderGraph/assets/33181641/5dc732c9-5518-4661-985c-073d067f412d)

## For Unity 2019

https://github.com/z3y/ShaderGraph/tree/2019

## License

Shader Graph target code licensed under the Unity Companion License for Unity-dependent projects [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License).

##

[Discord](https://discord.gg/bw46tKgRFT)
