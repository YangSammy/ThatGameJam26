# Lightweight autumn memory bedroom — 2026-11-06

Implemented in the existing WritingWorld3D right viewport. No additional bedroom scene is loaded. Left writing set and scene assets are unchanged by this task.

Static environment: Assets/Art/Environments/House/MemoryBedroom/AutumnMemoryBedroom.prefab. Authored by MemoryBedroomAuthoring.Build; eight meshes grouped by existing autumn bedroom shared materials; no static environment colliders, scripts, lights or realtime shadow casting. Runtime retains existing actors and collectible prefabs. Memory illumination uses an isolated non-shadow directional light; existing farewell rim remains.

Layout: west bed and bedside pendant dish, north window/flower stand, east desk/letter, southeast wardrobe/luggage, south entrance. Replaced old middle partition movement rule with furniture footprint blockers. Preserved collectible IDs, bit order, timer and checkpoint schema. Old checkpoints inside furniture fall back to clear center.

Validation in Unity 2022.3.62f3 Editor:
- MemoryBedroomChecks.Run: continuous WASD route with writing/E collects all four and reaches arrival; arrival freezes timer; checkpoint restores; cabinet blocks passage; old checkpoint inside bed recovers; E without writing cannot collect; stopping writing consumes time.
- NarrativeRegressionChecks.Run: 12 passed, 0 failed (QA/narrative-p1-regression.txt).
- AutumnRestorationChecks.RunModel and RunPresentation passed: tutorial, retry, deadline arrival, persistent state, independent vase, character presentation, timeout fade.
- Production WritingWorld3D constructed in a temporary preview scene and rendered by its actual right camera at 960x1080. Image: output/QA/MemoryBedroom/right-room.png. Preview scene and temporary objects disposed.
- No compilation errors. MCP connection warning occurred during script reload; tools recovered.

Limits: not a Player build or native-input full split-screen playthrough. No target-hardware profiling, FPS claim, or memory measurement. Existing movement speed and timer remain unchanged; scripted perfect-input route is much faster than a human dual-task run and is not evidence of the design's proposed 35–45 second pacing. Static room count excludes existing actors, interactables and effects.

## Detail and lighting revision

Added gathered curtain meshes, soft pillow and folded cover, turned bedposts, bed rails, cabinet panel trim, brass knobs, bedside drawers, tucked chair, orange task lamp, stacked books and wall panel beading. Four pickup locations and movement footprints are unchanged.

Lighting: one memory-layer-only directional key with soft shadows, 1024 shadow resolution; a non-shadow sky fill and the existing non-shadow farewell rim. No global quality settings changed. Window material gets mild emission. Static room now casts and receives shadows, so rendering costs are greater than the first shadowless version; no measured FPS claim.

Validated: 9 static mesh renderers, 14,604 static triangles; 3 memory lights with exactly 1 shadow caster. Narrative regression 12/12, MemoryBedroomChecks and AutumnRestorationChecks.RunPresentation passed; Console errors 0. Production right camera preview at output/QA/MemoryBedroom/right-room-detailed-final.png.

Fixed authoring regeneration to explicitly update saved mesh vertex/index data and mark assets dirty, ensuring GPU meshes and persisted assets both update. Target device profiling and full native-input split-screen playthrough remain unverified.
