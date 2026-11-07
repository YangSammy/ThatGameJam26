# Park Bloom implementation plan

User approved the preceding Bloom proposal for ParkScene on 2026-11-07.

Goal: subtle daylight highlight bloom, preserving photography readability.
Architecture: serialized Amplify Bloom 1.3.2 components on Camera_Player in the existing Built-in pipeline. Retain ParkPhotoFocus and capture behavior unless rendering comparison demonstrates a needed correction.

- [x] Back up scene; record clean editor and Console baseline.
- [x] Add effect with explicit shader references, HDR, Realistic/Half, four mips, threshold 1.2 and intensity 0.22. Disable all additional lens features and temporal filtering.
- [x] Render entrance, meadow and waterside with effect off/on; tune from actual images.
- [x] Check focus/exposure rendering and ARGB32 photograph output against screen-equivalent rendering. No save-game writes.
- [x] Save only ParkScene; inspect scene diff, shader errors and Console; document settings and validation limitations.

Constraints: preserve lighting/materials/gameplay, unrelated MainMenu changes and imported assets. No runtime bootstrap, package changes, extra camera effects or scene-wide reauthoring. Temporal filtering stays off to avoid cross-sample history during long-exposure photographs.

Review focus: clipped sky; veiled shadows; blur of task subjects; target-texture precision; shader references retained for player builds.

Validation and tuning decisions: Docs/validation/ParkBloom-2026-11-07.md. Final threshold 0.72, intensity 0.25; no source changes needed. Full Play Mode/build/performance validation remains outside this pass.

