# Player hand refinement — 2026-11-07

Status: ready with validation limits.

## Scope and result

User requested Blender refinement of player knuckles, thumb web and wrist, preserving the skeleton and handhold logic and updating the dedicated left-only mesh.

Blender 4.2.9 imported the existing arms_rig.fbx into an authoring copy. Locally subdivided hand/wrist topology, relaxed angular cross sections, added subtle joint volume, smoothed existing weight transitions and rebuilt the wrist blend over 6 cm. Original vertex groups, skeleton matrices and animation source are retained. UVs are interpolated from the original map, with continuous smooth normals across UV seams. No changes to girl assets, materials or runtime code were made by this task.

The mesh-only transfer to Unity retains the original renderer's 52 binding indices and exact bind-pose matrices. The original FBX is unchanged. The full-arm prefab now references a standalone refined mesh; its full-arm cache is explicitly set. The existing Resources left-arm mesh is updated in place, preserving its GUID and resource path.

| Mesh | Before | After |
|---|---|---|
| Both arms | 758 vertices / 1,176 triangles | 8,090 vertices / 14,988 triangles |
| Left arm | 379 vertices / 588 triangles | 4,045 vertices / 7,494 triangles |

This increases geometry cost for the close-up first-person mesh. No runtime deformation system or new per-frame processing was added.

## Validation

- Existing LeftHandGestureChecks.Run(false): PASS before and after. Final palm anchor separation remains 0.0391 m, finger envelope clearance 0.0014 m.
- Existing LeftHandGestureChecks.Run(true): PASS after. Final palm anchor separation 0.0391 m, finger envelope clearance 0.0011 m.
- Existing FirstPersonArmChecks.Run(): PASS after final mesh. Includes Generic rig, 52 bindings, independent left/right grab and push, bone scale invariance, contact, reset, camera/shutter/fallback and release behavior.
- Full and left meshes: exact equality of all 52 bind poses to source, normalized weights, finite vertices, UV/normal counts equal vertex counts. Left-only mesh has exclusively .L bone influences.
- Blender source coordinate match: maximum error 1.3328e-7 metres; original Blender skeleton matrices unchanged.
- SHA-256 checks confirm all FirstPersonHands partials, FirstPersonArmRig.cs and Arms.controller unchanged from task start.
- Loaded ParkScene uses FirstPersonArmsRefined.asset, scene remains not dirty; no scene save was needed.
- Visually reviewed before/after contact close-ups, front open reach/release, meadow close-up and full-arm grasp render. Knuckle contours are rounder; wrist ridge reduced through a second geometry/weight pass.
- Independent focused review found no actionable issues; static geometry, normals, weights, triangle/bone indices and left-only influences checked. Final Console query returned zero warnings/errors. Unity-generated YAML contains its customary empty-value trailing spaces; no code whitespace changes were introduced.

## Artifacts

- Editable Blender: `output/Characters/PlayerHandsRefined/PlayerHandsRefined.blend` (packed source texture, source rig preserved).
- Reproducible authoring/import: `output/Characters/PlayerHandsRefined/Scripts/refine_hands.py`, `import_mesh.csx`.
- Baseline/backups: `output/Characters/PlayerHandsRefined/before/`.
- Final front/meadow reach/contact/release images and checks: `output/Characters/PlayerHandsRefined/after/`.
- Integration checks: `output/Characters/PlayerHandsRefined/integration-checks.txt`.
- Runtime full mesh: `Assets/Art/Characters/Player/PSXArms/FirstPersonArmsRefined.asset`.
- Updated left mesh: `Assets/Resources/Player/FirstPersonLeftArm.asset`.

## Limits

These checks run actual rigs and procedural gestures in isolated Unity preview scenes, with baked render snapshots. They are not a full ParkScene input playthrough, target-platform build or triangle-surface collision proof. Existing collision checks use bone envelopes. The original 512x512 painted texture and material are intentionally retained; geometry refinement does not remove all painted low-poly shading. Baseline Console included an unused `dwell` warning and MCP transport warning; final Console is checked separately without clearing it.
