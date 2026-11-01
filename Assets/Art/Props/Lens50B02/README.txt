B02 50 mm prime lens — independent sample, 2026-10-31
Use Prefabs/PF_Lens50_B02.prefab. No scene or gameplay integration.
Unity 2022.3.62f3 Built-in Standard, root at mounting face, +Z lensward.
61.30 mm diameter, 57.59 mm overall length (7.10 mm behind / 50.49 mm ahead).
69,984 imported triangles, 12 meshes/renderers, 7 materials, 9 clips.
Attach later at B01 Visual/LensMountAnchor with zero local position/rotation and unit scale.

Focus: Visual/FocusRing local Z 0..160 degrees, infinity..0.45 m.
No translations/scales/optical element motion. Focal length remains 50 mm.
Aperture: ring +15 degrees per stop; f2,2.8,4,5.6,8,11,16.
AN_B02_ApertureSweep controls ring + all 7 independent leaves together; stop i at i/6 sec.
Seven constant stop clips support stable state restoration. Keep focus and aperture independent.
Pause by retaining normalized sample time; cancel/restore the selected channel or pose snapshot.
No Animator, scripts, input owner or Camera. Hit trigger proposals and solid proxy are disabled.

BaseColor sRGB; normal imported as NormalMap; MetallicSmoothness linear R=metallic,A=1-roughness.
Optics uses transparent Standard, alpha .18; not a calibrated optical simulation.
Preserve Bfont-License.txt for the mechanical number glyphs.
Full source, motion table, actual renders and 163-check isolated Unity report:
output/B02_Lens50_v1/ at project root. No real-mouse, save, blur or player-build claim.
