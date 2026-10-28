# PSX first-person arms integration — 2026-10-28

## Evidence and design
Unity 2022.3.62f3, built-in rendering, legacy Input. No animation rigging package.
Initial Git status clean; loaded ParkScene not dirty. Existing Console error: Blender could not convert .blend to FBX. Root FBX imports successfully as Generic with 52 skin bones and 18 non-preview clips.
Keep complete model hierarchy. Camera-local wrapper compensates authored eye height 1.7432m. Preserve source assets. FirstPersonHands owns presentation and retains chapter target registration and shutter entry point. A shared prefab owns Animator, masked left/right grab/push layers and bounded rotation-only two-bone IK. No gameplay animation events, extra input owner or save writes.

## Implementation plan
- [x] Add shared rig component/configuration and asset builder; use relax, grab.L/R, push.L/R only.
- [x] Route existing FirstPersonHands through skeletal branch, disable legacy renderers and skip cloning/capsules. Read accepted tape/recording/park state.
- [x] Wire prefab below both existing cameras without touching targets or layout. Abort saves on dirty scenes or external file changes.
- [x] Validate compile, rig references, masks, independent gestures, bounded reach, release/reset and synchronous shutter evaluation in isolated editor objects. Inspect scene diffs.
- [x] Report manual gameplay and visual checks separately; never run save-writing gameplay to manufacture progress.

## Review focus
Cancellation must clear visual state; inspection must preserve layer culling; Animator must evaluate before IK and capture; side masks must isolate complete arm chains; scene edits must preserve concurrent layout work. Source clip inspection shows grab/push return at their end, so hold mid-clip (configurable normalized time), then blend back on release.

## Delivered assets and Inspector
- `Assets/Art/Characters/Player/PSXArms/FirstPersonArms.prefab`: complete nested root FBX, Animator and FirstPersonArmRig. Both existing scene cameras reference an instance through FirstPersonHands.skeletalRig.
- `Arms.controller`, `LeftArm.mask`, `RightArm.mask`, `BareArms.mat` in the same folder. Material references original `arms_01.png`.
- Select `Camera_Player/FirstPersonArms` below the existing player camera (both scenes) or open the shared prefab. FirstPersonArmRig exposes modelOffset/modelEuler/displayScale, blendSeconds/reachSeconds, grabHoldTime/pushHoldTime, contactOffsets/contactEuler/contactRotationWeight, maximumExtension, maximumBodyReach/reachLowering and elbowHints.
- Left/right masks contain only the corresponding shoulder descendants. No camera/root animation is taken from interaction clips. Source importer, FBX, Blend, GLB, textures and license files are retained. Unity generated missing import metadata, including metadata under Old Versions; those assets are not referenced by this implementation.

## Code changes
- `Assets/Scripts/Player/FirstPersonArmRig.cs`: manually evaluated Generic Animator followed by rotation-only two-bone IK. Maximum extension is 97%; whole-rig reach is limited to .45m with .18m shoulder lowering. Extreme/out-of-range targets remain clamped rather than stretching anatomy.
- `Assets/Scripts/Player/FirstPersonHands.cs` and `.Skeletal.cs`: retain registration/shutter entry points, bypass old cloning/forearm/Transform driver when configured, preserve pen and brush tool children, consume existing control state and target references.
- `Assets/Scripts/Chapters/Tape/HouseRadioDeck.Recording.cs`: read-only visual contact/press state based on existing accepted drag/recording input; no new gameplay action or input mapping.
- `Assets/Scripts/Park/ParkPhotoPrototype.Controls.cs`: read-only presentation gate and current instrument-drag target.
- `Assets/Editor/Player/FirstPersonArmAssets.cs`: explicit one-time asset creation and guarded per-scene connection; refuses to overwrite existing prefab or dirty scene. Does not rebuild scene content.
- `Assets/Editor/Player/FirstPersonArmChecks.cs`: isolated Editor preview checks. Invoke `MemoryWhitebox.EditorTools.FirstPersonArmChecks.Run()` in Editor code execution. Does not enter Play or access save storage.
- `Assets/Scenes/HouseScene.unity`, `Assets/Scenes/ParkScene.unity`: shared arm instance/reference and legacy camera display root disabled. House save also serialized existing unlockAllChaptersInEditor=true default; its behavior was not changed. Park/environment diffs include concurrent work (V11/V12 and materials); do not revert those as part of this change. Park was clean immediately before connection and is now dirty from subsequent work; it was not reloaded or saved again.

## Validation results
Final Unity compilation completed; final Console query returned zero errors/warnings. Initial .blend importer failure, an intermediate Animator state-name issue and a test-only SendMessage assertion were investigated; the implementation uses the working root FBX, correct state-machine paths and direct callback invocation in isolated tests.

PASS: prefab references, Generic (not Humanoid), all 52 skin bindings, bare texture, layer 27, independent left/right grab and push, accurate reachable contact, bounded unreachable contact, unchanged bone scales, cancellation/reset, sustained normal-frame convergence without drift, release recovery, camera raise/lower, shutter press feedback, synchronous joined-hand pose, focus loss, application pause and input handoff gate.

The joined-hand fixture reproduces current scene fallback geometry: chest height 1.24m, reference eye height 1.6m/forward distance 1.2m, contact .38m forward and .18m below chest. Wrist contact error is checked below .015m before rendering. Preview renders were inspected in `QA/PSXArms-idle.png`, `PSXArms-contact.png`, `PSXArms-shutter.png`, `PSXArms-meadow.png`. These are isolated visual checks, not captured gameplay negatives. Saved scene YAML confirms both scenes reference the same prefab and disable legacy display. Source code preserves inspection mask removal and production shutter evaluation before Camera.Render.

Not tested end-to-end in Play: tape drag/cancel with real mouse, recording buttons, actual camera operations, SafeInspection entry/exit, meadow negative persistence/film count, Esc and OS focus handling, chapter/scene transitions, writing/painting/darkroom tool feel, player build. Current unsaved parallel scene work and save-writing startup were not disturbed for testing. No existing saves were overwritten and no real story prerequisite was completed.

## Animation coverage / remaining art
Connected imported `ArmsRig|relax` (1.9667s), `ArmsRig|grab.L/R` (.7s), `ArmsRig|push.L/R` (.7667s), excluding preview duplicates. Grasp/reach/hold/release use masked source motion sampled to configurable hold phase and blended back to relax. Camera holding, lift/lower, shutter and hand joining reuse these basic poses with target following. Writing, painting, rewinding, darkroom processing and tailored camera/handshake finger contact still need dedicated authored animation and per-prop polish. No jab/knife/finger-gun animation is used.
