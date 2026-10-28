# Park photography tasks — v3 implementation handoff

New rolls use three ordered exposures: Entrance, Bridge, Meadow. Existing version 1/2 rolls retain their previous capacity and task behavior. Photographs remain unavailable until development in HouseScene.

## Route setup

On three existing `ParkPhotoWaypoint` components set `photoTask` to `Entrance`, `Bridge`, and `Meadow`, in walking order. No route or scene positions are modified by the photography code.

If no explicit task is assigned, the current fallback uses the first route point before the first `watersideMoment`, that waterside point, and the last point after it. Missing task points block exposure and log a warning rather than consuming film. Explicit assignments are recommended for the final park layout.

Each point can reference an authored `referenceView` (camera position and direction), `referencePhoto`, and `referenceEv100`. Without them a temporary composition is captured beside the companion; this fallback is not a guaranteed walkable camera position. Final layout work must place reachable reference anchors. The meadow composition must put the player's camera within two metres of `handContact`.

`handContact` is an optional scene transform for the meadow hand contact. The fallback is near the companion's chest. Model proportions and final animation still require authoring; no scene assets or positioning objects were changed in this iteration.

## Interaction

- At the active point the companion waits. A reference appears. Position, camera direction, focus and exposure must match for the alignment interval.
- Entrance: shutter applies a short head tilt and captures the actual rendered frame.
- Bridge: alignment triggers a turn toward the player; shutter requires the turn to settle and camera conditions to remain valid.
- Meadow: alignment triggers the offered arm. Move the viewfinder pointer onto the contact point and click to join hands; this click does not consume a frame. A subsequent click captures when camera conditions are valid. The player's hand layer is included in this exposure.
- Each successful exposure advances the active task. Three exposures seal the roll and use the existing darkroom return flow.

## Persistence and limitations

Reference images, completed alignment and joined-hand response are saved per task. Exposure bytes are written before the manifest; failed saves do not consume an exposure. Completed exposures cannot be repeated on the same roll. Reloading repeats the companion's walk from the existing spawn, but completed task stops are not held again; the pending task resumes when reached. Walking away temporarily removes the held-hand pose without erasing the saved response.

These are whitebox action hooks, not final hair movement, face shielding, finger contact or character animation. Exposure uses the existing meter/settings qualification; this iteration does not add physically simulated film exposure rendering.

Implementation only, per request: no compilation, Play, build or automated tests were run.
