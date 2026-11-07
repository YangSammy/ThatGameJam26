# Sun-aware park rim light

User requested strong backlight in sun and weak contour lighting in shade.

ParkCharacterRimLight now samples five head/shoulder/torso points toward Light_Sun every .12 seconds. Opaque collider occlusion is combined with cached triangle tests against the two authored shadow-only canopy meshes, which do not have colliders. Self/viewer colliders, triggers, invisible boundary colliders and disabled/non-shadow renderers are excluded. Fully lit intensity 60; fully shaded 3; partial visibility interpolates. Runtime exponential smoothing time constant .35 seconds. Missing/disabled sun falls back to shade. No collision layers or materials changed.

Scene references saved in ParkScene. Scene delta against this revision's backup: 10 added lines, 1 changed line. Original at Backups/ParkSunRim-2026-11-07/.

Verification: ParkSunRimChecks.Run returned 10/10. Covers open sky, opaque cover, ignored triggers, rotated/nonuniform canopy without collider, inactive/non-shadow renderer, self collider, disabled/missing sun and inactive actor. Actual entrance visibility 0 and intensity 3; sampled open route points visibility 1, waterside point 0. Entrance native screenshot inspected at output/ParkSunRim-2026-11-07/entrance-shade.png. Unity compilation has no project errors; MCP reconnect warning during domain reload. Independent review found no blockers.

Limits: CPU occlusion approximates sunlight rather than reading GPU shadow maps. Non-colliding shadow geometry beyond the two explicitly bound canopy meshes, alpha-cutout detail and shader-only deformation are not sampled. Runtime smoothing still requires Play Mode visual assessment; no standalone build or performance benchmark.
