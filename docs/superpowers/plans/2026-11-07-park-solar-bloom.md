# Park solar Bloom implementation

User approved the reference-image proposal: HDR sun aligned with main light, selective Bloom, solar ghosts/halo/star rays with occlusion, subtle walking optics and stronger viewfinder optics, retain sampling safety, modest atmospheric warmth.

Use Unity feature implementation and verification workflow in current checkout. Preserve unrelated work, player route/gameplay and original sun direction/shadow layout.

- [x] Baseline: existing layered sky has no sun-direction or HDR disc property; backup scene and touched runtime files.
- [x] Add dedicated sky shader/material and explicit sun synchronization component, retaining cloud pattern with lower brightness.
- [x] Gate lens features by sun viewport position; tune walking/viewfinder profiles and retain capture guard.
- [x] Preserve HDR before photographic output conversion if target precision requires it.
- [x] Verify sun occlusion with real opaque geometry, offscreen attenuation and capture equivalence; rerun Bloom and rim checks.
- [x] Inspect native screenshots from sunny and shaded compositions, save scene/materials and document limitations.

No volumetric rays in this pass. Existing linear fog gets modest warmth only. Native sky geometry occlusion provides tree/building masking; the Bloom plugin remains the image-effect renderer.

Validation: Docs/validation/ParkSolarBloom-2026-11-07.md. 33 checks passed; no volumetric rays or main sun/route repositioning.

