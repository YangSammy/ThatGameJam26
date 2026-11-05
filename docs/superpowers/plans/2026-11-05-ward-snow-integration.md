# Winter ward snow integration

Approved scope: soften the snow-melt edge and integrate the effect into the actual Unity winter chapter. Prior user direction removes movement-dependent reveal. Use a 12-second automatic reveal, existing white-entry transition, live paper actor/line, stable eye mask, and same-tick old-house cut.

- [x] Increase snow softness from 0.035 to 0.060 and broaden its faint halo.
- [x] Add runtime material referencing the clean-paper concept texture. Composite live album ink over the paper; crossfade the outgoing view into the image during the existing white transition. Keep legacy ward objects inactive.
- [x] Persist reveal time in checkpoint v4; migrate v2/v3 distance into equivalent time. Left/right movement remains independent of reveal. Completion follows time, not actor distance.
- [x] Validate shader, migrated saves, idle/backwards reveal, runtime entry and old-house cleanup in the isolated acceptance project. Preserve main editor Play state and existing GUIDs.

Image cleanup used the built-in image tool: only remove the baked boy and horizontal ink line from the foreground sheet, preserve the hospital composition. Source: exec-b22101a9-67be-4ade-89ac-4f7ce9e3a807.png. The in-game paper renderer supplies the live ink.

Results: `docs/validation/WardSnowIntegration-2026-11-05.md`.
