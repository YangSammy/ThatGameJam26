# Darkroom C01–C11 asset implementation plan

**Goal:** Deliver the user's specified C01–C11 as editable, original, mechanically separated Blender/FBX/PBR assets and independent Unity prefabs.
**Architecture:** Stable metre-unit roots; child pivots for hard parts, blendshapes for cloth/paper/film. Deterministic sampleable animation clips and optional presentation-only material helper. No production scene, input, save or gameplay changes.
**Tech stack:** Blender 3.6 LTS; FBX; Unity 2022.3.62f3 Built-in; existing Unity MCP, isolated preview scenes.
**Spec:** User's detailed C01–C11 module requirements in this conversation, continued by “继续制作暗室所需要的模型”; approved B01–B04 material direction. Current docs/design.html and docs/AI/DarkroomPhysicalPrint.md and DarkroomPrintSceneBinding define future integration constraints.

## Boundaries and decisions
- Write only Assets/Art/Props/DarkroomC and output/C_Darkroom_v1, plus this plan.
- Preserve current HouseScene/ParkScene and existing B01–B04 integration work. Baseline hashes of existing scenes/scripts/settings/packages/art before authoring.
- Main deliverable is all eleven modules, including chemical/tray variants and separate paper, negative and clips. No model replaces authored scene props.
- Choose 240×180 mm paper inside 300×240 mm frame; 320×270 mm trays. Three 36×24 mm frames on a 35 mm film strip, independently replaceable UV/content areas; no sample photographs.
- Main tank lid and liquid port are independent; a light-trap baffle remains in place during filling/draining. Bag stays opaque and closed during work.
- Clip sampling, not a playback state machine, provides pause/cancel/reverse at any normalized progress. Root and operation anchors never animated. No timer/chemistry/gameplay assertions in art.
- Use existing layered paint/metal direction and new original cotton weave, paper fibre, warm-grey plastic and old wood. Wear localized to contact edges and handles.

## Tasks
- [x] Capture baseline and inspect actual scene bindings and dimensions.
- [x] Author closed dark bag with separate cuff/seal and blendshape hand impressions; true two-disc spiral reel; hollow tank with separated port and labyrinth funnel.
- [x] Author three bottles, sink/tap/flow, three-frame perforated negative and spring clip.
- [x] Author hinged transparent contact frame/latch, exposure lamp/held switch, thick paper with independent content face, four trays and separate liquids, drying rack/clips.
- [x] Export per-asset editable .blend and FBX, PBR source layers and original source scripts. Validate dimensions/face normals/UV/full surfaces.
- [x] Import in actual Unity, normalize pivots including blendshape deltas, build independent prefabs/clips, keep all suggested hit/physics colliders disabled.
- [x] Validate all clips at endpoints/intermediates/reverse/restored poses; root invariance, required child axes, material assignments, three content areas, receiver clearance and closed-body geometry. Render default and all supplied states in isolated preview scenes; inspect contact sheets and close-up/back views.
- [x] Fresh independent review, repair verified issues, write Chinese handoff including exact budgets, axes, anchor positions, source/license and honest integration limitations. Package with manifest/hash checks.

## Review focus
- Open lids/closures and long moving objects intersecting each other.
- Blendshape deltas or normals corrupted by FBX axis conversion.
- Material image reveal accidentally exposing placeholder/sample photos.
- Thin transparent geometry becoming an interaction blocker.
- Root/operation anchor changes, dirty production scenes, or damage to concurrent integration work.

## Execution record
- Inspected real HouseScene bindings: serialized bag/tank/chemicals and print props already exist; currently editor idle, HouseScene clean, Built-in. Existing scene/gameplay changes belong to other work.
- Read DarkroomPrintSceneBinding, SceneProcessing, DarkroomInteraction and PrintObjects. Current gameplay stretches bag and uses broad authored-lid motion; future integration must adapt to new deformable bag and separate liquid cap without copying those shortcuts.
- Ruling: keep working in the active project with new isolated asset folder, since actual Unity import/preview and existing uncommitted integration need the current editor; do not create a divergent checkout or commit unrelated work.

- Completed: 17 independent prefabs, 17 FBX and editable Blender sources, 88 clips; 7,506 general Editor assertions plus 42 mechanical assertions passed. Existing 894 protected files unchanged. No production scene edits or gameplay integration. Final read-only review resolved; all state sheets regenerated after fixes.
- Actual geometry checks caught and repaired partial Vector3 animation reset, nested pivot offsets, degenerate bevel faces, thin-material shrink/thickness, UV mirroring, and stale GPU mesh/skin previews. Source textures and meshes remain editable; no third-party art used.

