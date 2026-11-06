# User-adjusted darkroom layout verification

Verified the saved Bedroom_Summer layout on 2026-11-06 after the user moved and
rotated the authored item parents. No gameplay code or layout was changed.

## Result

The current layout can complete the full darkroom workflow. The final runtime
print stage was `Complete`; the completed roll saved and reloaded successfully
with the same roll ID. Scene SHA-256 before/after verification matched. The
pre-test EditorPlaySession was backed up and restored after testing.

Passed: shortcut 2 ready input and visible/reachable orange rewind outline;
rewind, open back, extract film, bag, reel/tank; all three pour/agitate/drain
cycles; negative rinse, hanging and drying; paper and negative in contact frame;
close/expose/open; paper developer, stop bath, fixer, rotated wash tray/tap,
raised drying rack, finished photograph, save and reload.

## Paths that require care

The previous automated straight-path regression stopped at `Pour chemical 1`.
This was a collision assertion, not an exception in gameplay: the stop-bath
bottle's direct path to the tank crosses the developer bottle. The fixer bottle
also needs to be moved out of the bottle row first.

Using normal `Press/Move/Release` with the collision system enabled succeeded:

- Stop/fixer bottles: first move toward the front of the bench to approximately
  world X=0.95 while retaining the bottle's Z (4.28 / 4.48), then onto the tank.
- Dried negative: directly dropping from the raised clip toward the frame hits
  the stop-bath bottle. First move to approximately X=0.95, Z=4.05, then onto the
  frame. X=0.95, Z=4.21 is already inside the frame's receiving tolerance and
  starts the descent too early.
- Tank draining continues to use the tested short side/lift path around props;
  collision sweeps remain active.

No colliders were disabled to make these paths pass, and no material stage was
forced forward. Processing timers were accelerated via the normal tick method.

## Reachability checks

Real `Pick` rays from valid player standing positions found the film rinse tap,
rotated paper wash tap, exposure switch, frame lid, negative clip and photo rack.
The initial rewind control also passed a ray test from the shortcut camera.
All 32 serialized prop/placement bindings still belong to their authoring groups.

## Evidence and scope

- Initial straight-path failure and scene/save backup:
  `output/QA/DarkroomLayoutCheck-20261106-204935/`.
- Blocked bottle screenshot: `output/QA/darkroom-layout-stop-blocked.png`.
- Completed flow screenshot: `output/QA/darkroom-current-layout-complete.png`.
- The continuation used live Editor calls into the regular interaction methods;
  it was not a physical mouse/keyboard playthrough or a standalone-build test.
- The old 34-check straight-path suite is not recorded as passing this new layout;
  completion was verified by continuing with the collision-safe paths above.
