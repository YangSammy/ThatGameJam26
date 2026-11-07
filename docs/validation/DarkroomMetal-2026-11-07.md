# Darkroom metal highlights

Six local Standard material variants preserve original albedo, normal and metallic/smoothness textures. Smoothness map scale increased to 1 for satin metal and .88 for brass; emission disabled. Scene renderer material slots replaced only for these metals; plastic, rubber and markings retained. SummerPhotoRoom applies the same replacements to host runtime cameras on entry and restores original renderer materials on leaving. Original shared materials remain unchanged.

Added neutral spot key and a box-projected 128px HDR custom baked reflection probe confined to the annex. Bloom remains .8, threshold 1.05, saturation 1.05. Reflection is static, with no runtime probe refresh.

Validation: six material references and no emission verified, reflection cubemap present, Editor Console errors empty. Offscreen view inspected at output/QA/DarkroomLighting-20261107/metal-highlights.png. No Play-mode walkthrough or performance benchmark performed.
