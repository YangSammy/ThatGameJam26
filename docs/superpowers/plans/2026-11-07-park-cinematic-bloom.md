# Strong park cinematic Bloom

User requests exaggerated character backlight, viewfinder optics and stable fine highlights.

Implementation: replace the plain image-effect adapter with a park-specific render adapter calling the imported Amplify Bloom renderer after ParkPhotoFocus. Use existing suppressFocusAid capture scope to bypass lens overlays and temporal history for every photo sample. Add a warm, narrow, shadowless character backlight with explicit subject/view references. Preserve actor layers/materials, saves and exposure logic.

- [x] Inspect state and capture signals; baseline test confirms no capture-aware controller exists.
- [x] Add renderer controller and character backlight, compile.
- [x] Configure serialized scene references, Bloom quality and optics.
- [x] Compare walking/viewfinder/capture renders; test black occlusion and history reset.
- [x] Save scene, inspect diff and Console, record validation limits.

Acceptance: visibly stronger actor separation and viewfinder image; ordinary walking has no lens artifacts; capture samples have no temporal contamination; disable/re-enable and camera cuts clear history. Shader and material vendor source remains unchanged.

Results: Docs/validation/ParkCinematicBloom-2026-11-07.md.

