# Darkroom material separation

Added ten scene-local material variants: matte camera grips/cavity and rubber (smoothness .16, metal 0), rough labels/paper (.1, metal 0), glossy amber bottle (.88, metal 0, retained opaque thick glass approximation). Preserved albedo/normal textures. Existing metal variants retain narrow reflections. Runtime source/replacement bindings extended to 16 pairs; restored on leaving Summer room. Shared source materials untouched.

Reference appearance: reduced exposure to .36, Bloom .3 and threshold 1.35. Rebuilt local cubemap excluding other loaded scene renderers, lights, reflection probes AND terrain/vegetation. This removed visible yellow-green reflection contamination from the additive ParkScene. Reflection strength .8.

Verified Editor renders at the same camera angle, material bindings present and Console errors empty. Preview output/QA/DarkroomLighting-20261107/material-separation.png. Appearance approaches the supplied reference but is not pixel-identical. No Play-mode walkthrough or build test. Bottle edge highlights are specular, not simulated refraction/transmission.
