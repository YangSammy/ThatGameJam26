# Park Girl Tripo intake and isolated Unity integration

> Execution: inline, following unity-feature-implementation and unity-build-validation. Existing approved route: inspect generated art, preserve appearance, targeted cleanup, then determine rig readiness. Do not replace production characters.

Goal: inspect the supplied FBX and create a genuinely imported, reviewable Unity sample with an explicit readiness assessment.

Architecture: keep original FBX/fbm immutable. New editable Blender source and reports in output/Characters/ParkGirlTripo; isolated preview assets in Assets/Art/Characters/ParkGirlTripo. Preserve existing Rain sample, all gameplay code, scene bindings, input, render pipeline, project settings and save data.

Tech: Blender 4.2.9, Unity 2022.3.62f3 Built-in Standard, current project checkout dev-Nicole with unrelated user edits. No new worktree needed for isolated art folder; no commits authorized or planned.

- [x] Record protected hashes and inspect project integration points.
- [x] Audit topology, components, UVs, materials, dimensions, bones and animation.
- [x] Visually inspect all source components before choosing a candidate. Confirmed three complete figures; selected the front figure, preserving other figures in the source.
- [x] Create reversible normalized visual asset, material channel conversion and editable source. High-poly retained; 80k static candidate, packed Blender textures.
- [x] Import using actual Unity; create independent prefab and review scene; render multi-angle evidence and validate references, scale, shader support and normals. Corrected lost FBX axis rotation and inherited flat-shading patches; final checks pass.
- [x] Record license basis, original hashes, source/derived stats, readiness limitations and recommended next step. See output/Characters/ParkGirlTripo/Reports/Intake.md.
- [x] Verify protected assets remain byte-identical and inspect scoped changes. 281/281 protected files unchanged; HouseScene active and clean; no production actor replacement.

Initial findings: 971175 vertices / 1942417 triangles; 1 material, 4096px maps, 0 bones/weights/actions. Three substantial components plus one triangle. Need views to distinguish valid character from reconstruction of the three-panel sheet. User confirmed paid-plan generation on 2026-11-03; commercial basis checked against official terms, account receipt not independently inspected.

Review focus: do not conflate three disconnected bodies with a usable single body; preserve generated UV/material detail; roughness must invert into Standard smoothness alpha; correct scale/orientation only on derived assets; do not call static render verification animation/gameplay acceptance.

Final status: intake and static review integration complete. Final art acceptance, animation-ready topology, hair/clothing separation, rig/weights, all-angle face protection, gameplay adapter and runtime/build validation remain future character work. No claim of a completed production girl. Local Unity MCP connection became available after initial UI validation; final render invocation and Console/scene checks used that existing bridge. No new packages or settings changes.
