# Park afternoon iteration implementation plan

> For agentic workers: use superpowers:executing-plans inline. User explicitly approved implementation of the five-part minimal combination.

**Goal:** Preserve ParkScene layout while introducing selected Flooded Grounds exterior houses/props, a second tree silhouette, inexpensive creek shading and a fixed warm afternoon.
**Architecture:** Unity 2022.3 Built-in; scene-local assets in Assets/Art/Environments/Park/V08. Keep gameplay, collisions and project-wide pipeline settings intact. Snapshot the scene before editing. Import only selected package dependencies after inspection.
**Spec:** User's five bullets in the 2026-10-28 conversation.

## Constraints and review focus
- Free commercial-use sources only; preserve source/license records.
- No playable-layout changes. Decorative additions must not obstruct the route.
- No real-time reflection camera; use baked probes and depth-aware water.
- Shared materials, bounded texture sizes, LOD trees and distant exterior-only buildings.
- Do not claim FPS or final build size without measurements.
- Account login/asset acquisition must succeed before claiming Flooded Grounds integration.

## Tasks
- [x] Inspect current scene, cache and backup; establish layout baseline.
- [x] Acquire Flooded Grounds via official account flow; inspect archive; choose 2–3 intact exterior designs and a few props. Import only dependencies, strip inaccessible interiors in derived prefabs, preserve license.
- [x] Add a contrasting CC0 tree species to selected existing perimeter positions, retain other trees, configure LOD/materials.
- [x] Create scene-local low-cost depth/normal/probe water, no extra reflection pass. Validate shore edges and bridge views.
- [x] Set fixed afternoon sun and restrained grading; bake lighting and probes. Verify shaders, missing assets, preserved route/colliders, source size and scene previews.
- [x] Save scene, source records and before/after previews; report any acquisition blocker separately.

Execution notes: Official cached Flooded Grounds used. Final selection is two exterior designs; expensive Villa excluded. Tall_C reused from same package rather than another CC0 download. Color treatment uses materials and lighting, no added screen effect. Final validation and limitations are in output/park-blockout/v08/README.md.

