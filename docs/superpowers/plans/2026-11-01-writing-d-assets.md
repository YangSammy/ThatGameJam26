# Writing D asset production plan

Goal: Deliver ART-033–038 as editable Blender, FBX, PBR, independent Unity prefabs, deterministic presentation API and isolated preview scene. User's detailed request is the approved spec; proceed directly as requested.

Architecture: new files only in Assets/Art/Props/WritingD and output/D_WritingProps_v1. Reuse original C/B authoring recipes, not their asset GUIDs. No production-scene saves, global SaveAssets, gameplay changes, commits, packages or input. Current checkout required for live Unity inspection; isolated directories rather than another worktree. Preserve concurrent changes.

- [x] Inspect design, actual writing/autumn/painting/hand code and xlsx ART-016/033–038; record baseline.
- [x] Build six groups including specified decor/painted variants, true thickness and named anchors.
- [x] Import normals/PBR and normalize mesh/shape coordinates; create clips and prefabs.
- [x] Add presentation-only deterministic driver with capture/restore, no Update/input/time/save. Test partial/invalid input, multi-channel cancellation, root stability, instance independence.
- [x] Save standalone preview scene, render default/back/bottom and sampled states; inspect closeups and mechanical clearances.
- [x] Fresh read-only review; no critical/important defect. Clarify timer face normal and strengthen combined state tests.
- [x] Final evidence, refreshed manifest and verified package.

Review focus: intermediate crumple geometry and ink-follow; 0.5m chain with endpoints; handle grip moving with hinge; clock clockwise stable pause; all variants remain editable and runtime-independent. Paper/flower backs must exist; no enabled blocking hit boxes.

Project findings: WritingChapter ItemIds pendant/flower/luggage/letter, bitmask collection and remaining time own gameplay. WritingWorld3D scales sphere paperball and draws dynamic LineRenderer ink; new assets do not rewrite this. Painting shares pendant silhouette for recess, luggage for crate and flower for wheel. Existing arm rig expects wrist targets with configured contact Euler; this delivery supplies contact anchors only, no fitted finger animation. design.html contains obsolete door/scarf scenario; current user and xlsx autumn requests take precedence.

Progress and validation records: output/D_WritingProps_v1/validation. Source and manifests retained as reproducible ledger; no commit/reset of shared work.

Rulings: User explicitly requested direct execution, current project and no commits of others' work; used isolated new folders in current checkout without approval loop/worktree/commits. Unity MCP initially connected, later Editor closed/tool vanished; continued using installed same-version Unity batch Editor with real rendering (no -nographics). No gameplay scene was opened/saved by asset authoring. Paper first trial had sharp peaks, replaced with thick continuous folded sheet with an irregular crease network. Envelope thin bevel degeneracy fixed by retaining geometric edges without bevel. Initial untitled batch scene prevented additive scene creation; preview creator uses a new empty single scene only in fresh batch with an untitled active scene. Initial errors retained in raw logs, final checks separately rerun.
