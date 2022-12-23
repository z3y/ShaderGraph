# Shader Graph


## Naming Conventions
By default new properties added to the blackboard have randomized reference names. In order for them to fallback properly use standard naming conventions. All of the properties below are reference names, display names can be anything


### VRChat
For materials to fallback properly on avatars: https://docs.vrchat.com/docs/shader-fallback-system.
Some of the common property names are `_MainTex`, `_Color`, `_EmissionColor`, `_BumpMap`, `_Cutoff`

### Unity
Baked GI has some hard coded property names requered to function properly.

For emission to affect GI declare `float _Emission`, `color _EmissionColor` and `texture _EmissionMap` even if you dont use them at all.

For the alpha clip threshold `_Cutoff`


## Shader Keywords
Keywords generate a new shader variant that completely strips the other branch, meaning it wont be possible to toggle them while in VRChat. Use boolean toggles for runtime toggles and keywords for editor only


## Node Settings
Settings for the master node https://i.imgur.com/WnaD7Yc.png


## Audio Link
Audio link cginc is automatically included if its found in the project. Requieres the Creator Companion version (0.3.1+). All of the nodes will function live in the graph preview when in play mode


## LTCGI
LTCGI is automatically added if found in the project


## Additional Pass
Mostly made so outlines are possible. Imports another shader's pass named `FORWARDBASE` and adds it after the main forward base. You can create 2 shader graph shaders, either lit or unlit, expand the vertex position in the normal space, change culling override and add it to the other shader to get outlines in shader graph.


## Bakery Alpha Meta Pass
Meta pass outputs dithered transparency for the meta pass when using bakery

## Anisotropy
Supports tangent maps and anisotropy from `(-1 to 1)`, same as the HDRP Lit shader graph

## Node Hotkeys
Same hotkeys as Amplify shader editor or Unreal Engine