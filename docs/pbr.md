### Phong shaders

The [phong_shader.vert](../Visualisation.Core/Shaders/phong_shader.vert)
and [phong_shader.frag](../Visualisation.Core/Shaders/phong_shader.frag)
are the shaders responsible for handing the scene drawing which does include CSM and PBR.

### PBR

Currently, the PBR is done with two inputs in mind. The properties

- Albedo
- Normal
- Roughness
- Metallic
- Ao

can be specified as textures or as single values. The single values approach may be useful for the scene to load
instantly
and can achieve better visual results for objects which are very big, which would make the texture
appear to be low resolution. This could ex. be the case for very big or infinite planes.

#### Performance

There should be done profiling later to determine if it is better to use two fragment shaders

- one for the textures approach
- one for the single values approach
  to determine if this gives good performance increase.
  Some other optimizations can be done instead, ex. treat values as 1x1 textures which couold
  improve the speed.

This should be done after implementing the PBR fully. 

